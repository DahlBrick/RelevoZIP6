using UnityEngine;

/// <summary>
/// Base de energía. Recibe la batería, alimenta su puente eléctrico y permite
/// retirarla. El anillo y el cable pasan de cobre apagado a ámbar cuando la base
/// está alimentada: el estado siempre se lee de un vistazo.
/// </summary>
public class EnergySocket : MonoBehaviour, IInteractable
{
    public string Identificador = "A";
    public string Etiqueta = "base A";
    public TemporaryBridge Puente;

    BatteryController bateria;

    [Header("Conexiones")]
    [SerializeField] Transform ancla;
    [SerializeField] Transform anclajeBateria;
    [SerializeField] Renderer anillo;
    [SerializeField] Renderer cable;
    [SerializeField] Light luz;
    [SerializeField] ParticleSystem chispas;
    [Tooltip("Faro de Liria: sostiene el puente un rato después de retirar la batería.")]
    [SerializeField] Condensador condensador;

    float intensidadObjetivo;

    public Transform AnclajeBateria { get { return anclajeBateria != null ? anclajeBateria : transform; } }
    public bool TieneBateria { get { return bateria != null; } }
    public bool Energizada { get; private set; }

    public Transform Ancla { get { return ancla != null ? ancla : transform; } }
    public Condensador Condensador { get { return condensador; } }

    public void AsignarCondensador(Condensador c) { condensador = c; }

    public bool Disponible
    {
        get
        {
            // Retirar la batería desde su propio puente lo replegaría bajo los pies de Nilo. Con un
            // condensador cargado no se repliega al momento, así que se permite.
            if (TieneBateria) return !NiloSobreElPuente() || CondensadorSostendria();

            BatteryController enManos = RelevoGame.Bateria;
            return enManos != null && enManos.EnManos;
        }
    }

    public string Verbo
    {
        get
        {
            if (TieneBateria && condensador != null)
                return "Retirar batería de " + Etiqueta + " (condensador " + Mathf.RoundToInt(condensador.Carga * 100f) + " %)";
            return TieneBateria ? "Retirar batería de " + Etiqueta : "Insertar batería en " + Etiqueta;
        }
    }

    public string Motivo
    {
        get
        {
            return TieneBateria
                ? "No puedes retirar la batería desde el puente: se replegaría bajo tus pies"
                : "Necesitas llevar la batería";
        }
    }

    bool CondensadorSostendria()
    {
        return condensador != null && condensador.Carga > 0.05f;
    }

    bool NiloSobreElPuente()
    {
        PlayerController nilo = RelevoGame.Jugador;
        return Puente != null && nilo != null && Puente.Contiene(nilo.transform.position);
    }

    void OnEnable() { PlayerInteractor.Registrar(this); }
    void OnDisable() { PlayerInteractor.Desregistrar(this); }

    public void Configurar(string identificador, string etiqueta, TemporaryBridge puente, Transform ancla,
        Transform anclajeBateria, Renderer anillo, Renderer cable, Light luz)
    {
        Identificador = identificador;
        Etiqueta = etiqueta;
        Puente = puente;
        this.ancla = ancla;
        this.anclajeBateria = anclajeBateria;
        this.anillo = anillo;
        this.cable = cable;
        this.luz = luz;

        Energizada = false;
        intensidadObjetivo = 0f;

        chispas = RelevoEfectos.CrearChispas(transform, new Vector3(0f, 0.55f, 0f), RelevoPalette.ColorDe(ClaveMaterial.Ambar), 0.4f, 10);

        Vincular();
    }

    /// <summary>
    /// Deja la base apagada y su puente retraído usando las referencias que ya vienen
    /// serializadas. Sirve tanto al construir el actor como al cargar una escena horneada:
    /// Energizada y el nivel de chispas son estado de ejecución y no sobreviven al horneado.
    /// </summary>
    public void Vincular()
    {
        bateria = null;
        Energizada = false;
        intensidadObjetivo = 0f;

        FijarEmision(0f);
        PintarEnergia(false);

        if (luz != null) luz.intensity = 0f;
        if (Puente != null) Puente.Activar(false);
        if (condensador != null) condensador.Vincular();
    }

    void FijarEmision(float tasa)
    {
        if (chispas == null) return;
        ParticleSystem.EmissionModule emision = chispas.emission;
        emision.rateOverTime = tasa;
    }

    public void Interactuar(PlayerController jugador)
    {
        if (TieneBateria)
        {
            BatteryController retirada = bateria;
            bateria = null;

            Energizar(false);
            retirada.LiberarDeBase();
            retirada.Sujetar(jugador);
            AudioDirector.Pedir(Tono.Retirar);
            return;
        }

        BatteryController enManos = RelevoGame.Bateria;
        if (enManos == null || !enManos.EnManos) return;

        enManos.AcoplarA(this);
        bateria = enManos;
        if (condensador != null) condensador.AlInsertarBateria();
        Energizar(true);
        AudioDirector.Pedir(Tono.Insertar);
    }

    void Energizar(bool activa)
    {
        Energizada = activa;
        intensidadObjetivo = activa ? 2.4f : 0f;

        // Con el condensador cargado el puente aguanta: lo repliega él al vaciarse.
        bool sostenido = !activa && condensador != null && condensador.AlRetirarBateria();
        if (Puente != null && !sostenido) Puente.Activar(activa);

        FijarEmision(activa ? 12f : 0f);

        PintarEnergia(activa || sostenido);
        GameEvents.RaiseBaseEnergiaCambiada(this, activa);
    }

    /// <summary>El condensador se vació: el cable vuelve a cobre.</summary>
    public void PintarCondensadorVacio()
    {
        if (!TieneBateria) PintarEnergia(false);
        GameEvents.RaiseBaseEnergiaCambiada(this, false);
    }

    void PintarEnergia(bool activa)
    {
        Material material = RelevoPalette.MaterialDe(activa ? ClaveMaterial.Ambar : ClaveMaterial.Cobre);
        if (anillo != null) anillo.sharedMaterial = material;
        if (cable != null) cable.sharedMaterial = material;
    }

    void Update()
    {
        if (luz == null) return;
        luz.intensity = Mathf.Lerp(luz.intensity, intensidadObjetivo, 1f - Mathf.Exp(-6f * Time.deltaTime));
    }
}
