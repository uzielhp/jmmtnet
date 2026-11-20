using System.Net.Http.Json;
using System.Text.Json;
using CobachJMMT.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CobachJMMT.Services;

public sealed class AppsScriptOptions
{
    public string ConfigUrl { get; set; } = "";
    public string ExamenUrl { get; set; } = "";
    public int CacheSegundos { get; set; } = 30;
}

/// <summary>Comunicación con los Google Apps Script (configuración y hojas de examen).</summary>
public sealed class AppsScriptService(HttpClient http, IMemoryCache cache, IOptions<AppsScriptOptions> opciones)
{
    private const string CacheKey = "config-admin";
    private static readonly SemaphoreSlim Candado = new(1, 1);
    private readonly AppsScriptOptions _o = opciones.Value;

    // ---------- Configuración ----------
    public async Task<ConfigAdmin?> GetConfigAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out ConfigAdmin? cfg)) return cfg;

        // Evita que varios alumnos disparen la misma petición a la vez
        await Candado.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue(CacheKey, out cfg)) return cfg;
            try
            {
                var json = await http.GetStringAsync(_o.ConfigUrl, ct);
                using var doc = JsonDocument.Parse(json);
                var r = doc.RootElement;
                cfg = new ConfigAdmin
                {
                    S100 = Texto(r, "s100"), S200 = Texto(r, "s200"), S300 = Texto(r, "s300"),
                    S400 = Texto(r, "s400"), S500 = Texto(r, "s500"), S600 = Texto(r, "s600"),
                    Contra = Texto(r, "Contra"),
                    Menu = Texto(r, "Menu").Equals("true", StringComparison.OrdinalIgnoreCase),
                    Imagenes = int.TryParse(Texto(r, "Imagenes"), out var n) ? n : 0
                };
                cache.Set(CacheKey, cfg, TimeSpan.FromSeconds(Math.Max(1, _o.CacheSegundos)));
                return cfg;
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                // Falla recordada unos segundos para no bloquear cada página con reintentos
                cache.Set(CacheKey, (ConfigAdmin?)null, TimeSpan.FromSeconds(5));
                return null;
            }
        }
        finally { Candado.Release(); }
    }

    public async Task<bool> GuardarConfigAsync(ConfigAdmin c, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                primero = c.S100, segundo = c.S200, tercero = c.S300,
                cuarto = c.S400, quinto = c.S500, sexto = c.S600, menu = c.Menu
            };
            using var resp = await http.PostAsJsonAsync(_o.ConfigUrl, payload, ct);
            var ok = await EsYes(resp, ct);
            if (ok) cache.Remove(CacheKey);
            return ok;
        }
        catch (Exception) when (!ct.IsCancellationRequested) { return false; }
    }

    // ---------- Examen ----------
    public async Task<List<List<string>>?> GetExamenAsync(string hojaId, CancellationToken ct = default)
    {
        try
        {
            var json = await http.GetStringAsync(UrlExamen(hojaId), ct);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null; // {status:"error"}

            var filas = new List<List<string>>();
            foreach (var fila in doc.RootElement.EnumerateArray())
            {
                if (fila.ValueKind != JsonValueKind.Array) continue;
                filas.Add(fila.EnumerateArray()
                    .Select(c => c.ValueKind switch
                    {
                        JsonValueKind.String => c.GetString() ?? "",
                        JsonValueKind.Null => "",
                        _ => c.GetRawText()
                    })
                    .ToList());
            }
            return filas;
        }
        catch (Exception) when (!ct.IsCancellationRequested) { return null; }
    }

    public async Task<bool> EnviarExamenAsync(string hojaId, Dictionary<string, string> datos, CancellationToken ct = default)
    {
        try
        {
            using var resp = await http.PostAsJsonAsync(UrlExamen(hojaId), datos, ct);
            return await EsYes(resp, ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested) { return false; }
    }

    // ---------- Utilidades ----------
    private string UrlExamen(string hojaId) => $"{_o.ExamenUrl}?spreadsheetId={Uri.EscapeDataString(hojaId)}";

    private static string Texto(JsonElement r, string prop) =>
        r.TryGetProperty(prop, out var v)
            ? (v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.ToString())
            : "";

    private static async Task<bool> EsYes(HttpResponseMessage resp, CancellationToken ct)
    {
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("status", out var s) && s.GetString() == "yes";
    }
}
