using UnityEngine;

/// <summary>
/// Placa de presión: está pisada mientras hay una caja asentada encima o mientras Nilo está de pie
/// sobre ella. Pisada, abre su compuerta o despliega su puente; suelta, los devuelve a su estado.
///
/// Es la "segunda energía" del Patio de Bombas: con una caja encima mantiene un camino abierto
/// sin ocupar la batería. Se pinta en turquesa, el color de los mecanismos manuales, y un cable
/// en el suelo lleva hasta lo que acciona.
/// </summary>
public sealed class PlacaPresion : MonoBehaviour
{
    public static readonly System.Collections.Generic.List<PlacaPresion> Todas = new System.Collections.Generic.List<PlacaPresion>();

    void OnEnable() { if (!Todas.Contains(this)) Todas.Add(this); }
    void OnDisable() { Todas.Remove(this); }

    [SerializeField] Vector2Int casilla;
    [SerializeField] Transform tapa;
    [SerializeField] Renderer[] encendidos;      // luz de la placa y tramos del cable
    [SerializeField] Compuerta compuerta;
    [SerializeField] TemporaryBridge puente;

    public bool Presionada { get; private set; }
    public bool PorCaja { get; private set; }
    public Vector2Int Casilla { get { return casilla; } }
    public TemporaryBridge Puente { get { return puente; } }
    public Compuerta Compuerta { get { return compuerta; } }

    float hundido;

    public void Configurar(Vector2Int casilla, Transform tapa, Renderer[] encendidos, Compuerta compuerta, TemporaryBridge puente)
    {
        this.casilla = casilla;
        this.tapa = tapa;
        this.encendidos = encendidos;
        this.compuerta = compuerta;
        this.puente = puente;
    }

    public void Vincular()
    {
        Presionada = false;
        PorCaja = false;
        hundido = 0f;
        Pintar(false);
        if (tapa != null) tapa.localPosition = Vector3.zero;
    }

    bool NiloEncima()
    {
        LevelLayout layout = RelevoGame.Layout;
        PlayerController nilo = RelevoGame.Jugador;
        if (layout == null || nilo == null) return false;
        Vector3 d = nilo.transform.position - layout.CentroTile(casilla.x, casilla.y);
        d.y = 0f;
        return d.magnitude < 0.8f;
    }

    void Update()
    {
        bool porCaja = CajaEmpujable.AsentadaEn(casilla) != null;
        bool pisada = porCaja || NiloEncima();
        PorCaja = porCaja;

        if (pisada != Presionada)
        {
            Presionada = pisada;
            if (compuerta != null) compuerta.Abrir(pisada);
            if (puente != null) puente.Activar(pisada);
            Pintar(pisada);
            AudioDirector.Pedir(pisada ? Tono.PlacaPisada : Tono.PlacaSuelta);
            if (RelevoGame.Nivel != null) RelevoGame.Nivel.ActualizarObjetivo();
        }

        hundido = Mathf.MoveTowards(hundido, pisada ? 1f : 0f, Time.deltaTime * 8f);
        if (tapa != null) tapa.localPosition = Vector3.down * 0.07f * hundido;
    }

    void Pintar(bool activa)
    {
        if (encendidos == null) return;
        Material m = RelevoPalette.MaterialDe(activa ? ClaveMaterial.Turquesa : ClaveMaterial.Cobre);
        for (int i = 0; i < encendidos.Length; i++)
            if (encendidos[i] != null) encendidos[i].sharedMaterial = m;
    }
}
