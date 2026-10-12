using System;

/// <summary>Estados posibles de la única batería de RELEVO.</summary>
public enum EstadoBateria
{
    EnManos,
    EnBase,
    EnSuelo
}

/// <summary>
/// Canal central de eventos entre sistemas. Ningún sistema busca componentes con
/// FindObjectOfType durante Update: publica y escucha aquí.
/// GameEvents.Limpiar() se ejecuta al construir cada nivel para descartar los
/// suscriptores de la escena anterior.
/// </summary>
public static class GameEvents
{
    public static event Action<EstadoBateria, string> BateriaCambiada;
    public static event Action<EnergySocket, bool> BaseEnergiaCambiada;
    public static event Action<TemporaryBridge, bool> PuenteCambiado;
    public static event Action<PermanentWalkway> PasarelaAbierta;
    public static event Action<string> ObjetivoCambiado;
    public static event Action<string, bool> PromptCambiado;      // texto, disponible
    public static event Action<int, int> RecuerdosCambiados;      // encontrados, total del nivel
    public static event Action<string, string> MensajeMostrado;    // título, texto
    public static event Action<string> NivelCompletado;
    public static event Action<Tono> SonidoSolicitado;

    public static void RaiseBateriaCambiada(EstadoBateria estado, string etiquetaBase)
    {
        Action<EstadoBateria, string> h = BateriaCambiada;
        if (h != null) h(estado, etiquetaBase);
    }

    public static void RaiseBaseEnergiaCambiada(EnergySocket baseEnergia, bool activa)
    {
        Action<EnergySocket, bool> h = BaseEnergiaCambiada;
        if (h != null) h(baseEnergia, activa);
    }

    public static void RaisePuenteCambiado(TemporaryBridge puente, bool desplegado)
    {
        Action<TemporaryBridge, bool> h = PuenteCambiado;
        if (h != null) h(puente, desplegado);
    }

    public static void RaisePasarelaAbierta(PermanentWalkway pasarela)
    {
        Action<PermanentWalkway> h = PasarelaAbierta;
        if (h != null) h(pasarela);
    }

    public static void RaiseObjetivoCambiado(string objetivo)
    {
        Action<string> h = ObjetivoCambiado;
        if (h != null) h(objetivo);
    }

    public static void RaisePromptCambiado(string texto, bool disponible)
    {
        Action<string, bool> h = PromptCambiado;
        if (h != null) h(texto, disponible);
    }

    public static void RaiseRecuerdosCambiados(int encontrados, int total)
    {
        Action<int, int> h = RecuerdosCambiados;
        if (h != null) h(encontrados, total);
    }

    public static void RaiseMensajeMostrado(string titulo, string texto)
    {
        Action<string, string> h = MensajeMostrado;
        if (h != null) h(titulo, texto);
    }

    public static void RaiseNivelCompletado(string nombreNivel)
    {
        Action<string> h = NivelCompletado;
        if (h != null) h(nombreNivel);
    }

    public static void RaiseSonidoSolicitado(Tono tono)
    {
        Action<Tono> h = SonidoSolicitado;
        if (h != null) h(tono);
    }

    /// <summary>Se llama al inicio de cada nivel para no arrastrar suscriptores de escenas anteriores.</summary>
    public static void Limpiar()
    {
        BateriaCambiada = null;
        BaseEnergiaCambiada = null;
        PuenteCambiado = null;
        PasarelaAbierta = null;
        ObjetivoCambiado = null;
        PromptCambiado = null;
        RecuerdosCambiados = null;
        MensajeMostrado = null;
        NivelCompletado = null;
        SonidoSolicitado = null;
    }
}
