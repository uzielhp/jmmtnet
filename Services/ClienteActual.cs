namespace CobachJMMT.Services;

/// <summary>Identificador del cliente del circuito actual (lo asigna Routes).</summary>
public sealed class ClienteActual
{
    public string Id { get; set; } = "";
}

/// <summary>Estado de interfaz compartido entre páginas y layout (reemplaza al JS que manipulaba el DOM).</summary>
public sealed class EstadoUi
{
    public bool Cargando { get; private set; }

    public event Action? Cambio;
    public event Action? ConfigCambiada;

    public void EstablecerCargando(bool valor)
    {
        if (Cargando == valor) return;
        Cargando = valor;
        Cambio?.Invoke();
    }

    public void NotificarConfig() => ConfigCambiada?.Invoke();
}
