using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CobachJMMT.Pages.Actividades;

public class Act10Model : PageModel
{
    [BindProperty(SupportsGet = true)] public double Emails { get; set; } = 500;
    [BindProperty(SupportsGet = true)] public double Streaming { get; set; } = 2;
    [BindProperty(SupportsGet = true)] public double Cloud { get; set; } = 15;

    public double TotalCO2 { get; private set; }
    public int Arboles { get; private set; }
    public int KmCoche { get; private set; }

    public void OnGet()
    {
        Emails = Math.Clamp(Emails, 0, 10000);
        Streaming = Math.Clamp(Streaming, 0, 12);
        Cloud = Math.Clamp(Cloud, 0, 500);

        TotalCO2 = Emails * 0.01 + Streaming * 365 * 0.055 + Cloud * 2;
        Arboles = (int)Math.Ceiling(TotalCO2 / 21);
        KmCoche = (int)Math.Round(TotalCO2 / 0.12);
    }
}
