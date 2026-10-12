using UnityEngine;

/// <summary>
/// Palanca manual: despliega una pasarela permanente y no puede accionarse dos veces.
/// Es la pieza que enseña el contraste entre camino eléctrico (temporal, depende de la
/// batería) y camino manual (permanente, no depende de la energía).
/// </summary>
public class LeverInteractable : MonoBehaviour, IInteractable
{
    public string Identificador = "L";
    public PermanentWalkway Destino;

    public bool Accionada { get; private set; }

    [Header("Conexiones")]
    [SerializeField] Transform manija;
    [SerializeField] Transform ancla;
    [SerializeField] Renderer indicador;
    [SerializeField] ParticleSystem chispas;

    float anguloActual = -35f;
    float anguloObjetivo = -35f;

    public Transform Ancla { get { return ancla != null ? ancla : transform; } }
    public bool Disponible { get { return !Accionada; } }
    public string Verbo { get { return "Accionar palanca"; } }
    public string Motivo { get { return "La palanca ya está accionada"; } }

    void OnEnable() { PlayerInteractor.Registrar(this); }
    void OnDisable() { PlayerInteractor.Desregistrar(this); }

    public void Configurar(string identificador, PermanentWalkway destino, Transform manija, Transform ancla, Renderer indicador)
    {
        Identificador = identificador;
        Destino = destino;
        this.manija = manija;
        this.ancla = ancla;
        this.indicador = indicador;

        Accionada = false;
        anguloActual = -35f;
        anguloObjetivo = -35f;

        if (manija != null) manija.localRotation = Quaternion.Euler(0f, 0f, anguloActual);
        if (indicador != null) indicador.sharedMaterial = RelevoPalette.MaterialDe(ClaveMaterial.Coral);

        chispas = RelevoEfectos.CrearChispas(transform, new Vector3(0f, 0.8f, 0f), RelevoPalette.ColorDe(ClaveMaterial.Turquesa), 0.3f, 8);

        Vincular();
    }

    /// <summary>
    /// Devuelve la palanca a su estado inicial con las referencias ya serializadas:
    /// Accionada y los ángulos son estado de ejecución y no sobreviven al horneado.
    /// </summary>
    public void Vincular()
    {
        Accionada = false;
        anguloActual = -35f;
        anguloObjetivo = -35f;

        if (manija != null) manija.localRotation = Quaternion.Euler(0f, 0f, anguloActual);
        if (indicador != null) indicador.sharedMaterial = RelevoPalette.MaterialDe(ClaveMaterial.Coral);

        if (chispas != null)
        {
            ParticleSystem.EmissionModule emision = chispas.emission;
            emision.rateOverTime = 0f;
        }
    }

    public void Interactuar(PlayerController jugador)
    {
        if (Accionada) return;

        Accionada = true;
        anguloObjetivo = 35f;

        if (Destino != null) Destino.Abrir();
        if (indicador != null) indicador.sharedMaterial = RelevoPalette.MaterialDe(ClaveMaterial.Turquesa);

        if (chispas != null)
        {
            ParticleSystem.EmissionModule emision = chispas.emission;
            emision.rateOverTime = 10f;
        }

        GameEvents.RaiseMensajeMostrado("Mecanismo",
            "La pasarela manual ya está desplegada: ese camino no depende de la energía.");
    }

    void Update()
    {
        if (manija == null) return;
        if (Mathf.Approximately(anguloActual, anguloObjetivo)) return;

        anguloActual = Mathf.MoveTowards(anguloActual, anguloObjetivo, 110f * Time.deltaTime);
        manija.localRotation = Quaternion.Euler(0f, 0f, anguloActual);
    }
}
