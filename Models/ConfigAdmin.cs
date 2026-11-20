namespace CobachJMMT.Models;

public sealed record ConfigAdmin
{
    public string S100 { get; init; } = "";
    public string S200 { get; init; } = "";
    public string S300 { get; init; } = "";
    public string S400 { get; init; } = "";
    public string S500 { get; init; } = "";
    public string S600 { get; init; } = "";
    public bool Menu { get; init; }
    public string Contra { get; init; } = "";
    public int Imagenes { get; init; }

    public string? HojaDe(string clave) => clave.ToLowerInvariant() switch
    {
        "s100" => S100, "s200" => S200, "s300" => S300,
        "s400" => S400, "s500" => S500, "s600" => S600,
        _ => null
    };

    public string Hoja(int semestre) => semestre switch
    {
        1 => S100, 2 => S200, 3 => S300, 4 => S400, 5 => S500, 6 => S600, _ => ""
    };
}

public sealed class PreguntaExamen
{
    public int Numero { get; init; }
    public string Texto { get; init; } = "";
    public List<string> Opciones { get; init; } = new();
}
