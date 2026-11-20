using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CobachJMMT.Pages;

public class IndexModel : PageModel
{
    private const int Limite = 12;
    private static readonly string[] Extensiones = { ".jpg", ".jpeg", ".png", ".webp" };
    private readonly IWebHostEnvironment _env;

    public List<string> Imagenes { get; private set; } = new();

    public IndexModel(IWebHostEnvironment env) => _env = env;

    public void OnGet()
    {
        // Reemplaza a galeria.json + generar_imgs.sh: se lee la carpeta directamente
        var carpeta = Path.Combine(_env.WebRootPath, "imgs", "galeria");
        if (!Directory.Exists(carpeta)) return;

        Imagenes = Directory.EnumerateFiles(carpeta)
            .Where(f => Extensiones.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Select(f => "/imgs/galeria/" + Path.GetFileName(f))
            .OrderBy(_ => Random.Shared.Next())
            .Take(Limite)
            .ToList();
    }
}
