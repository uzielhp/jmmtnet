using System.Text.Json;
using CobachJMMT.Models;
using CobachJMMT.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CobachJMMT.Pages;

public class ExamenModel : PageModel
{
    public const string MensajeExito = "Tus respuestas se han enviado";

    private readonly AppsScriptService _script;
    private readonly IWebHostEnvironment _env;

    public ExamenModel(AppsScriptService script, IWebHostEnvironment env)
    {
        _script = script; _env = env;
    }

    [BindProperty(SupportsGet = true)] public string Semestre { get; set; } = "";
    public int NumSemestre => Semestre.Length > 1 ? Semestre[1] - '0' : 0;

    public List<PreguntaExamen> Preguntas { get; } = new();
    public List<string> Imagenes { get; } = new();
    public string? Error { get; private set; }
    public bool Enviado { get; private set; }

    private string KeyResp => $"resp_{Semestre}";
    private string KeyEnviado => $"enviado_{Semestre}";

    public async Task<IActionResult> OnGetAsync()
    {
        var r = await Cargar();
        if (r is not null) return r;
        Enviado = HttpContext.Session.GetString(KeyEnviado) == "1";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var ajax = Request.Headers["X-Requested-With"] == "fetch";
        var (ok, mensaje) = await Procesar();

        // Envío por fetch: solo respondemos JSON, la página no se recarga
        if (ajax) return new JsonResult(new { ok, mensaje });

        // Respaldo sin JavaScript: recarga normal
        if (ok) Enviado = true;
        else { Error = mensaje; await Cargar(); }
        return Page();
    }

    private async Task<(bool ok, string mensaje)> Procesar()
    {
        if (HttpContext.Session.GetString(KeyEnviado) == "1")
            return (false, "Este examen ya fue enviado.");

        var cfg = await _script.GetConfigAsync();
        if (cfg is null || !cfg.Menu) return (false, "Error al conectarse Servidor");
        var hoja = cfg.HojaDe(Semestre);
        if (string.IsNullOrWhiteSpace(hoja)) return (false, "Error al conectarse Hoja Semestre");

        var json = HttpContext.Session.GetString(KeyResp);
        var correctas = json is null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (correctas is null || correctas.Count == 0)
            return (false, "La sesión expiró, vuelve a cargar el examen.");

        var f = Request.Form;
        var datos = new Dictionary<string, string>
        {
            ["grupo"] = f["grupo"].ToString().Trim(),
            ["nombres"] = f["nombres"].ToString().Trim(),
            ["apellidos"] = f["apellidos"].ToString().Trim()
        };

        if (datos.Values.Any(string.IsNullOrWhiteSpace) ||
            correctas.Keys.Any(k => string.IsNullOrEmpty(f[k])))
            return (false, "Completa tus datos y responde todas las preguntas.");

        foreach (var k in correctas.Keys) datos[k] = f[k].ToString();

        var aciertos = 0;
        foreach (var (k, correcta) in correctas)
        {
            var ok = f[k].ToString().Trim().ToLowerInvariant() == correcta;
            if (ok) aciertos++;
            datos["eval_" + k] = ok ? "correcta" : "incorrecta";
        }
        datos["aciertos"] = aciertos.ToString();

        if (!await _script.EnviarExamenAsync(hoja, datos))
            return (false, "Error al enviar Respuestas");

        HttpContext.Session.SetString(KeyEnviado, "1");
        HttpContext.Session.Remove(KeyResp);
        return (true, MensajeExito);
    }

    private async Task<IActionResult?> Cargar()
    {
        var cfg = await _script.GetConfigAsync();
        if (cfg is null) { Error = "Error al conectarse Servidor"; return null; }
        if (!cfg.Menu) return RedirectToPage("/Index");

        var hoja = cfg.HojaDe(Semestre);
        if (string.IsNullOrWhiteSpace(hoja)) { Error = "Error al conectarse Hoja Semestre"; return null; }

        var filas = await _script.GetExamenAsync(hoja);
        if (filas is null) { Error = "Error al conectarse Hoja Semestre"; return null; }

        for (var i = 1; i <= cfg.Imagenes; i++)
            if (System.IO.File.Exists(Path.Combine(_env.WebRootPath, "imgs", "examen", $"{i}.jpg")))
                Imagenes.Add($"/imgs/examen/{i}.jpg");

        var inicio = filas.Count > 0 && filas[0].Count > 0 &&
                     filas[0][0].Contains("pregunta", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        var correctas = new Dictionary<string, string>();
        var n = 0;
        for (var i = inicio; i < filas.Count; i++)
        {
            var fila = filas[i];
            if (fila.Count == 0 || string.IsNullOrWhiteSpace(fila[0])) continue;
            n++;

            if (fila.Count > 1 && !string.IsNullOrWhiteSpace(fila[1]))
                correctas[$"pregunta{n}"] = fila[1].Trim().ToLowerInvariant();

            var opciones = fila.Skip(2).Where(o => !string.IsNullOrWhiteSpace(o))
                               .OrderBy(_ => Random.Shared.Next()).ToList();
            Preguntas.Add(new PreguntaExamen { Numero = n, Texto = fila[0], Opciones = opciones });
        }

        if (Preguntas.Count == 0) { Error = "Error al cargar Preguntas (No hay ninguna)"; return null; }

        HttpContext.Session.SetString(KeyResp, JsonSerializer.Serialize(correctas));
        return null;
    }
}