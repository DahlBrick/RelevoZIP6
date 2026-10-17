using UnityEngine;

/// <summary>
/// PROTOTIPO - Circuitos de energia. Pieza giratoria de un tramo.
///
/// Vive unicamente en Assets/Scenes/PipePruebas/NivelPruebaMecanica1.unity. No tiene efectos
/// estaticos al cargarse: se registra como interactuable en OnEnable, igual que EnergySocket.
///
/// La geometria (los cubos 'Trazo') viene serializada en la escena, ya construida en la
/// orientacion de la solucion: con 'pasos' = 0 el tramo esta correcto. 'pasos' cuenta los
/// cuartos de vuelta de 90 grados aplicados sobre esa geometria, de modo que la orientacion se
/// reconstruye siempre a partir de un entero y nunca de la rotacion del Transform.
///
/// Puertos: 0 = Este (+X), 1 = Sur (-Z), 2 = Oeste (-X), 3 = Norte (+Z).
/// Girar +90 grados en Y lleva el puerto i al puerto (i + 1) % 4.
/// </summary>
public sealed class PruebaCircuitoPieza : MonoBehaviour, IInteractable
{
    public enum Forma { Recta, Curva, T }

    [Header("Circuito")]
    [SerializeField] Vector2Int casilla;

    [Tooltip("Solo informativo, para leer la escena. La logica usa 'puertosBase'.")]
    [SerializeField] Forma forma = Forma.Curva;

    [Tooltip("Mascara de bits de los puertos con pasos = 0 (solucion). E=1, S=2, O=4, N=8.")]
    [SerializeField] int puertosBase = 0x9;

    [Tooltip("Cuartos de vuelta iniciales. 0 = el tramo ya esta resuelto.")]
    [SerializeField] int pasosIniciales = 1;

    [Header("Interaccion")]
    [SerializeField] Transform ancla;

    [Header("Visual")]
    [SerializeField] Renderer[] trazos;

    PruebaCircuitoControlador controlador;

    int pasos;
    float angulo;
    float anguloObjetivo;
    bool girando;

    public Vector2Int Casilla { get { return casilla; } }
    public int Pasos { get { return pasos; } }
    public Renderer[] Trazos { get { return trazos; } }
    public Forma FormaActual { get { return forma; } }

    /// <summary>Mascara de bits con un bit por puerto ocupado, ya girada a 'pasos'.</summary>
    public int MascaraPuertos
    {
        get
        {
            int mascara = puertosBase & 0xF;
            int p = pasos & 3;
            if (p == 0) return mascara;
            return ((mascara << p) | (mascara >> (4 - p))) & 0xF;
        }
    }

    /// <summary>Lo llama el controlador una sola vez, al empezar.</summary>
    public void Enganchar(PruebaCircuitoControlador dueno)
    {
        controlador = dueno;
        pasos = pasosIniciales & 3;
        angulo = pasos * 90f;
        anguloObjetivo = angulo;
        girando = false;
        transform.localRotation = Quaternion.Euler(0f, angulo, 0f);
    }

    void OnEnable() { PlayerInteractor.Registrar(this); }
    void OnDisable() { PlayerInteractor.Desregistrar(this); }

    public Transform Ancla { get { return ancla != null ? ancla : transform; } }

    /// <summary>
    /// Siempre disponible: el tramo nunca muestra un motivo de bloqueo. Mientras gira, la pulsacion
    /// de E se ignora en silencio dentro de Interactuar, sin mensaje, sonido ni dialogo.
    /// </summary>
    public bool Disponible { get { return true; } }

    public string Verbo { get { return "Girar tramo"; } }

    public string Motivo { get { return "El tramo todavia esta girando"; } }

    public void Interactuar(PlayerController jugador)
    {
        if (girando) return;

        pasos = (pasos + 3) & 3;          // 90 grados exactos, siempre en el mismo sentido
        anguloObjetivo -= 90f;
        girando = true;

        AudioDirector.Pedir(Tono.Empujar);
    }

    void Update()
    {
        if (!girando) return;

        angulo = Mathf.MoveTowards(angulo, anguloObjetivo, 720f * Time.deltaTime);

        if (Mathf.Approximately(angulo, anguloObjetivo))
        {
            girando = false;
            angulo = pasos * 90f;         // cuantizado a 0, 90, 180 o 270
            anguloObjetivo = angulo;

            // La red se recalcula cuando el tramo ya ha terminado de girar.
            if (controlador != null) controlador.AvisarGiro();
        }

        transform.localRotation = Quaternion.Euler(0f, angulo, 0f);
    }
}
