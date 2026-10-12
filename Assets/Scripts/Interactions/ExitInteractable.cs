using UnityEngine;

/// <summary>
/// Salida del nivel. Solo se completa si Nilo lleva la batería: intentar salir sin
/// ella muestra la causa en lugar de fallar en silencio.
///
/// Tiene dos formas. La salida normal es un pórtico que se cruza llevando la batería; la del
/// Nivel 3 es el faro, y allí la batería no se lleva: se entrega en su cuna, así que al completar
/// el nivel la batería vuelve físicamente al suelo, dentro del collar del faro.
/// </summary>
public class ExitInteractable : MonoBehaviour, IInteractable
{
    public string Etiqueta = "la salida";

    [Tooltip("Nivel 3: la salida es el faro y la batería se entrega en su cuna.")]
    [SerializeField] bool esFaro;

    [SerializeField] Transform ancla;
    [SerializeField] ParticleSystem chispas;

    public Transform Ancla { get { return ancla != null ? ancla : transform; } }

    /// <summary>
    /// Es un campo serializado y no solo una configuración de arranque a propósito: en una escena
    /// horneada Configurar() no se ejecuta, así que la variante tiene que venir guardada.
    /// </summary>
    public bool EsFaro { get { return esFaro; } }

    public bool Disponible
    {
        get
        {
            BatteryController bateria = RelevoGame.Bateria;
            return bateria != null && bateria.EnManos;
        }
    }

    public string Verbo { get { return esFaro ? "Entregar la batería al faro" : "Salir llevando la batería"; } }
    public string Motivo { get { return esFaro ? "El faro necesita la batería" : "Necesitas llevar la batería"; } }

    void OnEnable() { PlayerInteractor.Registrar(this); }
    void OnDisable() { PlayerInteractor.Desregistrar(this); }

    public void Configurar(string etiqueta, Transform ancla)
    {
        Configurar(etiqueta, ancla, false, null);
    }

    /// <summary>
    /// 'esFaro' decide si la salida entrega la batería en lugar de cruzarse, y 'chispasFaro' deja
    /// reaprovechar unas partículas ya situadas en la cuna del faro. Si llega null se crean como
    /// siempre, que es lo que mantiene idéntico el pórtico del Nivel 1.
    /// </summary>
    public void Configurar(string etiqueta, Transform ancla, bool esFaro, ParticleSystem chispasFaro)
    {
        Etiqueta = etiqueta;
        this.ancla = ancla;
        this.esFaro = esFaro;

        chispas = chispasFaro != null
            ? chispasFaro
            : RelevoEfectos.CrearChispas(transform, new Vector3(0f, 1.1f, 0f), RelevoPalette.ColorDe(ClaveMaterial.Turquesa), 0.35f, 8);

        Vincular();
    }

    /// <summary>Reengancha el umbral luminoso con la referencia ya serializada.</summary>
    public void Vincular()
    {
        if (chispas == null) return;
        ParticleSystem.EmissionModule emision = chispas.emission;
        emision.rateOverTime = 6f;
    }

    public void Interactuar(PlayerController jugador)
    {
        LevelManager nivel = RelevoGame.Nivel;
        if (nivel == null) return;

        nivel.CompletarNivel();

        // El faro no es un umbral que se cruza. Si el nivel se ha completado —lo que ya exige que
        // la batería esté en la mano—, la batería se queda entregada en la cuna. Si no se completó,
        // CompletarNivel() ya se ha encargado de mostrar el motivo.
        if (!esFaro || !nivel.Completado) return;

        BatteryController bateria = RelevoGame.Bateria;
        if (bateria == null) return;

        Vector3 cuna = Ancla.position;
        bateria.ColocarEnSuelo(new Vector3(cuna.x, 0f, cuna.z));
    }
}
