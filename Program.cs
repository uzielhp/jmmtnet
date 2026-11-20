using CobachJMMT.Components;
using CobachJMMT.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(o =>
    {
        // Si el alumno pierde la red unos minutos, su examen en pantalla se conserva
        o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(10);
    });

builder.Services.AddMemoryCache();
builder.Services.Configure<AppsScriptOptions>(builder.Configuration.GetSection("AppsScript"));
builder.Services.AddHttpClient<AppsScriptService>(c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddSingleton<SesionStore>();
builder.Services.AddSingleton<GaleriaService>();
builder.Services.AddScoped<ClienteActual>();
builder.Services.AddScoped<EstadoUi>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// Identificador anónimo de cliente (reemplaza a la Session de ASP.NET, que no funciona dentro de un circuito Blazor).
// Solo se aplica a las páginas: ni a archivos estáticos ni a SignalR.
app.UseWhen(
    ctx => !Path.HasExtension(ctx.Request.Path.Value)
           && !ctx.Request.Path.StartsWithSegments("/_blazor")
           && !ctx.Request.Path.StartsWithSegments("/_framework"),
    rama => rama.Use(async (ctx, next) =>
    {
        if (!ctx.Request.Cookies.TryGetValue("cid", out var id) || !SesionStore.IdValido(id))
        {
            id = Guid.NewGuid().ToString("N");
            ctx.Response.Cookies.Append("cid", id, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = ctx.Request.IsHttps,
                MaxAge = TimeSpan.FromHours(2)
            });
        }
        ctx.Items["cid"] = id;
        await next();
    }));

app.UseAntiforgery();
app.UseWhen(
    ctx => Path.HasExtension(ctx.Request.Path.Value),
    rama => rama.Use(async (ctx, next) =>
    {
        ctx.Request.Headers.Remove("Accept-Encoding");
        await next();
    }));
app.UseStaticFiles();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
