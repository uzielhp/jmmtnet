using Microsoft.Extensions.Caching.Memory;

namespace CobachJMMT.Services;

public sealed class GaleriaService(IWebHostEnvironment env, IMemoryCache cache)
{
    private const int Limite = 12;
    private static readonly HashSet<string> Extensiones =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    public string[] Aleatorias()
    {
        var todas = cache.GetOrCreate("galeria", e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return Listar();
        }) ?? [];

        var copia = (string[])todas.Clone();
        Random.Shared.Shuffle(copia);
        return copia.Length > Limite ? copia[..Limite] : copia;
    }

    private string[] Listar()
    {
        if (string.IsNullOrEmpty(env.WebRootPath)) return [];
        var carpeta = Path.Combine(env.WebRootPath, "imgs", "galeria");
        if (!Directory.Exists(carpeta)) return [];

        return Directory.EnumerateFiles(carpeta)
            .Where(f => Extensiones.Contains(Path.GetExtension(f)))
            .Select(f => "imgs/galeria/" + Path.GetFileName(f))
            .ToArray();
    }
}
