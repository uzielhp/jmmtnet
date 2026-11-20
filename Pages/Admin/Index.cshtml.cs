using System.Security.Cryptography;
using System.Text;
using CobachJMMT.Models;
using CobachJMMT.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CobachJMMT.Pages.Admin;

public class IndexModel : PageModel
{
    private const string SesionAdmin = "admin";
    private readonly AppsScriptService _script;

    public IndexModel(AppsScriptService script) => _script = script;

    public bool Autenticado { get; private set; }
    public string? ErrorLogin { get; private set; }
    [BindProperty] public ConfigAdmin Config { get; set; } = new();
    [TempData] public string? Mensaje { get; set; }

    public async Task OnGetAsync() => await Preparar();

    public async Task<IActionResult> OnPostLoginAsync(string? contra)
    {
        var cfg = await _script.GetConfigAsync();
        if (cfg is null) { ErrorLogin = "Error al conectarse Servidor"; return Page(); }

        var a = Encoding.UTF8.GetBytes(contra ?? "");
        var b = Encoding.UTF8.GetBytes(cfg.Contra);
        if (a.Length == b.Length && b.Length > 0 && CryptographicOperations.FixedTimeEquals(a, b))
        {
            HttpContext.Session.SetString(SesionAdmin, "1");
            return RedirectToPage();
        }
        ErrorLogin = "Por favor, escriba la contraseña correcta.";
        return Page();
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        if (HttpContext.Session.GetString(SesionAdmin) != "1") return RedirectToPage();

        var ok = await _script.GuardarConfigAsync(Config);
        Mensaje = ok ? "Configuraciones guardadas" : "Error al guardar Información";
        return RedirectToPage();
    }

    public IActionResult OnPostSalir()
    {
        HttpContext.Session.Remove(SesionAdmin);
        return RedirectToPage("/Index");
    }

    private async Task Preparar()
    {
        Autenticado = HttpContext.Session.GetString(SesionAdmin) == "1";
        if (Autenticado) Config = await _script.GetConfigAsync() ?? new ConfigAdmin();
    }
}
