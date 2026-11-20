using Microsoft.Extensions.Caching.Memory;

namespace CobachJMMT.Services;

/// <summary>
/// Estado por cliente (identificado por la cookie "cid"): sesión de administrador, exámenes enviados
/// y límite de intentos de login. Sustituye a HttpContext.Session.
/// </summary>
public sealed class SesionStore(IMemoryCache cache)
{
    private static readonly TimeSpan Vida = TimeSpan.FromHours(2);
    private static readonly TimeSpan Bloqueo = TimeSpan.FromMinutes(1);
    private const int MaxIntentos = 5;
    private readonly object _candado = new();

    public static bool IdValido(string? id) =>
        id is { Length: 32 } && id.All(Uri.IsHexDigit);

    // ----- Administrador -----
    public bool EsAdmin(string cid) => IdValido(cid) && cache.TryGetValue($"admin:{cid}", out _);

    public void EntrarAdmin(string cid)
    {
        if (!IdValido(cid)) return;
        cache.Set($"admin:{cid}", true, new MemoryCacheEntryOptions { SlidingExpiration = Vida });
    }

    public void SalirAdmin(string cid) => cache.Remove($"admin:{cid}");

    // ----- Intentos de login -----
    public bool LoginBloqueado(string cid) => cache.TryGetValue($"bloqueo:{cid}", out _);

    public void RegistrarFallo(string cid)
    {
        var clave = $"fallos:{cid}";
        var n = (cache.TryGetValue(clave, out int v) ? v : 0) + 1;
        if (n >= MaxIntentos)
        {
            cache.Set($"bloqueo:{cid}", true, Bloqueo);
            cache.Remove(clave);
        }
        else cache.Set(clave, n, Bloqueo);
    }

    public void LimpiarFallos(string cid) => cache.Remove($"fallos:{cid}");

    // ----- Exámenes -----
    public bool ExamenEnviado(string cid, string semestre) =>
        IdValido(cid) && cache.TryGetValue($"enviado:{cid}:{semestre}", out _);

    public void GuardarRespuestasExamen(string cid, string semestre, string grupo,
        string nombres, string apellidos, IReadOnlyDictionary<int, string> respuestas)
    {
        if (!IdValido(cid)) return;
        cache.Set($"respuestas:{cid}:{semestre}",
            new RespuestasExamen(grupo, nombres, apellidos,
                new Dictionary<int, string>(respuestas)),
            Vida);
    }

    public RespuestasExamen? ObtenerRespuestasExamen(string cid, string semestre)
    {
        if (!IdValido(cid)) return null;
        return cache.TryGetValue($"respuestas:{cid}:{semestre}", out RespuestasExamen? respuestas)
            ? respuestas
            : null;
    }

    /// <summary>Reserva el envío de forma atómica; false si ya estaba enviado o en curso.</summary>
    public bool TryMarcarEnviado(string cid, string semestre)
    {
        if (!IdValido(cid)) return false;
        var clave = $"enviado:{cid}:{semestre}";
        lock (_candado)
        {
            if (cache.TryGetValue(clave, out _)) return false;
            cache.Set(clave, true, Vida);
            return true;
        }
    }

    public void QuitarEnviado(string cid, string semestre) => cache.Remove($"enviado:{cid}:{semestre}");
}

public sealed record RespuestasExamen(
    string Grupo,
    string Nombres,
    string Apellidos,
    IReadOnlyDictionary<int, string> Respuestas);
