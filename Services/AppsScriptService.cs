using System.Text;
using System.Text.Json;
using CobachJMMT.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CobachJMMT.Services;

public class AppsScriptOptions
{
    public string ConfigUrl { get; set; } = "";
    public string ExamenUrl { get; set; } = "";
    public int CacheSegundos { get; set; } = 30;
}

/// <summary>Reemplaza a proxy.php y a llamarAppScript/llamarHojaExamen de principal.js.</summary>
public class AppsScriptService
{
    private const string CacheKey = "config-admin";
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly AppsScriptOptions _o;

    public AppsScriptService(HttpClient http, IMemoryCache cache, IOptions<AppsScriptOptions> o)
    {
        _http = http; _cache = cache; _o = o.Value;
    }

    // ---------- Configuración (administración) ----------
    public async Task<ConfigAdmin?> GetConfigAsync()
    {
        if (_cache.TryGetValue(CacheKey, out ConfigAdmin? cfg)) return cfg;
        try
        {
            var json = await _http.GetStringAsync(_o.ConfigUrl);
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
            _cache.Set(CacheKey, cfg, TimeSpan.FromSeconds(_o.CacheSegundos));
            return cfg;
        }
        catch { return null; }
    }

    public async Task<bool> GuardarConfigAsync(ConfigAdmin c)
    {
        try
        {
            var payload = new
            {
                primero = c.S100, segundo = c.S200, tercero = c.S300,
                cuarto = c.S400, quinto = c.S500, sexto = c.S600, menu = c.Menu
            };
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(_o.ConfigUrl, content);
            var ok = await EsYes(resp);
            if (ok) _cache.Remove(CacheKey);
            return ok;
        }
        catch { return false; }
    }

    // ---------- Examen ----------
    public async Task<List<List<string>>?> GetExamenAsync(string hojaId)
    {
        try
        {
            var url = $"{_o.ExamenUrl}?spreadsheetId={Uri.EscapeDataString(hojaId)}";
            var json = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null; // {status:"error"}
            var filas = new List<List<string>>();
            foreach (var fila in doc.RootElement.EnumerateArray())
            {
                if (fila.ValueKind != JsonValueKind.Array) continue;
                filas.Add(fila.EnumerateArray()
                    .Select(c => c.ValueKind == JsonValueKind.String ? c.GetString() ?? "" :
                                 c.ValueKind == JsonValueKind.Null ? "" : c.GetRawText())
                    .ToList());
            }
            return filas;
        }
        catch { return null; }
    }

    public async Task<bool> EnviarExamenAsync(string hojaId, Dictionary<string, string> datos)
    {
        try
        {
            var url = $"{_o.ExamenUrl}?spreadsheetId={Uri.EscapeDataString(hojaId)}";
            using var content = new StringContent(JsonSerializer.Serialize(datos), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(url, content);
            return await EsYes(resp);
        }
        catch { return false; }
    }

    // ---------- Utilidades ----------
    private static string Texto(JsonElement r, string prop) =>
        r.TryGetProperty(prop, out var v)
            ? (v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.ToString())
            : "";

    private static async Task<bool> EsYes(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("status", out var s) && s.GetString() == "yes";
    }
}
