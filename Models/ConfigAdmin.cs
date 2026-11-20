namespace CobachJMMT.Models;

public class ConfigAdmin
{
    public string S100 { get; set; } = "";
    public string S200 { get; set; } = "";
    public string S300 { get; set; } = "";
    public string S400 { get; set; } = "";
    public string S500 { get; set; } = "";
    public string S600 { get; set; } = "";
    public bool Menu { get; set; }
    public string Contra { get; set; } = "";
    public int Imagenes { get; set; }

    public string? HojaDe(string clave) => clave switch
    {
        "s100" => S100, "s200" => S200, "s300" => S300,
        "s400" => S400, "s500" => S500, "s600" => S600,
        _ => null
    };
}

public class PreguntaExamen
{
    public int Numero { get; set; }
    public string Texto { get; set; } = "";
    public List<string> Opciones { get; set; } = new();
}
