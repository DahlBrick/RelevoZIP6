using UnityEngine;

/// <summary>
/// La única batería del juego. Es un objeto persistente: nunca se instancia ni se
/// destruye, solo cambia de estado y de padre (EnManos, EnBase, EnSuelo).
/// Solo se registra como interactuable mientras está en el suelo, para que no
/// secuestre el aviso contextual cuando Nilo la lleva encima.
/// </summary>
public class BatteryController : MonoBehaviour, IInteractable
{
    public EstadoBateria Estado { get; private set; }
    public EnergySocket BaseActual { get; private set; }

    [Header("Anclajes")]
    [SerializeField] Transform anclajeMano;
    [SerializeField] Transform anclajeSuelo;
    [SerializeField] Transform anclaAviso;

    Vector3 escalaOriginal;
    bool registrado;

    public bool EnManos { get { return Estado == EstadoBateria.EnManos; } }

    public Transform Ancla { get { return anclaAviso != null ? anclaAviso : transform; } }
    public bool Disponible { get { return Estado == EstadoBateria.EnSuelo; } }
    public string Verbo { get { return "Recoger batería"; } }

    public string Motivo
    {
        get
        {
            return Estado == EstadoBateria.EnManos
                ? "Ya llevas la batería"
                : "La batería está acoplada a una base";
        }
    }

    public void Inicializar(Transform anclajeMano, Transform anclajeSuelo, Transform anclaAviso)
    {
        this.anclajeMano = anclajeMano;
        this.anclajeSuelo = anclajeSuelo;
        this.anclaAviso = anclaAviso;
        escalaOriginal = transform.localScale;
        RelevoGame.Bateria = this;
    }

    /// <summary>
    /// Reengancha el estado de ejecución cuando la escena ya viene horneada. Los anclajes
    /// llegan serializados, pero el estado no: su valor por defecto es EnManos, así que
    /// hay que devolver explícitamente la batería al suelo.
    /// </summary>
    public void Vincular(Transform mano)
    {
        if (mano != null) anclajeMano = mano;
        escalaOriginal = transform.localScale;
        RelevoGame.Bateria = this;

        // El horneado deja esta marca en true sin que exista alta real: en modo edición
        // PlayerInteractor ignora los registros. Se parte de cero para que la alta sea efectiva.
        registrado = false;

        ColocarEnSuelo(transform.position);
    }

    /// <summary>Estado inicial: sobre su pedestal del muelle.</summary>
    public void ColocarEnSuelo(Vector3 posicion)
    {
        BaseActual = null;
        Estado = EstadoBateria.EnSuelo;

        Transform padre = anclajeSuelo != null ? anclajeSuelo : transform.parent;
        transform.SetParent(padre, true);
        transform.position = posicion;
        transform.localScale = escalaOriginal;

        Registrar(true);
        GameEvents.RaiseBateriaCambiada(Estado, "");
    }

    public void Sujetar(PlayerController jugador)
    {
        BaseActual = null;
        Estado = EstadoBateria.EnManos;
        Registrar(false);

        if (anclajeMano != null)
        {
            transform.SetParent(anclajeMano, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = escalaOriginal * 0.8f;
        }

        AudioDirector.Pedir(Tono.Recoger);
        GameEvents.RaiseBateriaCambiada(Estado, "");
    }

    public void AcoplarA(EnergySocket baseEnergia)
    {
        BaseActual = baseEnergia;
        Estado = EstadoBateria.EnBase;
        Registrar(false);

        Transform anclaje = baseEnergia != null ? baseEnergia.AnclajeBateria : null;
        if (anclaje != null)
        {
            transform.SetParent(anclaje, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = escalaOriginal;
        }

        GameEvents.RaiseBateriaCambiada(Estado, baseEnergia != null ? baseEnergia.Etiqueta : "");
    }

    public void LiberarDeBase()
    {
        BaseActual = null;
    }

    public void Interactuar(PlayerController jugador)
    {
        if (Estado != EstadoBateria.EnSuelo) return;
        Sujetar(jugador);
    }

    void Registrar(bool activo)
    {
        if (activo == registrado) return;
        registrado = activo;

        if (activo) PlayerInteractor.Registrar(this);
        else PlayerInteractor.Desregistrar(this);
    }

    void OnDisable()
    {
        Registrar(false);
    }
}
