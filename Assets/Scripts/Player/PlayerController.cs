using UnityEngine;

/// <summary>
/// Movimiento suave de Nilo sobre el plano XZ con CharacterController: sin gravedad
/// ni salto, la altura queda fija para evitar física caótica. El rumbo de la entrada
/// se toma de la cámara fija, así W avanza hacia el fondo de la pantalla y D a la derecha.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] float velocidad = 6f;
    [SerializeField] float suavizado = 16f;
    [SerializeField] float velocidadGiro = 12f;

    public bool PuedeMoverse { get; set; }
    public RelevoInput Entrada { get; private set; }
    public Camera Camara { get; private set; }

    CharacterController controlador;
    Vector3 velocidadActual;
    float alturaFija;

    void Awake()
    {
        PuedeMoverse = true;
        controlador = GetComponent<CharacterController>();
        alturaFija = transform.position.y;
    }

    public void Inicializar(RelevoInput entrada, Camera camara)
    {
        Entrada = entrada;
        Camara = camara;

        if (controlador != null)
        {
            controlador.stepOffset = 0.3f;
            controlador.slopeLimit = 45f;
            controlador.skinWidth = 0.03f;
            controlador.minMoveDistance = 0f;
        }

        alturaFija = transform.position.y;
    }

    void Update()
    {
        if (controlador == null) return;

        Vector2 entrada = PuedeMoverse && Entrada != null ? Entrada.LeerMovimiento() : Vector2.zero;

        Vector3 adelante = Camara != null
            ? Vector3.ProjectOnPlane(Camara.transform.forward, Vector3.up).normalized
            : Vector3.forward;
        Vector3 derecha = Camara != null
            ? Vector3.ProjectOnPlane(Camara.transform.right, Vector3.up).normalized
            : Vector3.right;

        Vector3 deseada = adelante * entrada.y + derecha * entrada.x;
        if (deseada.sqrMagnitude > 1f) deseada.Normalize();

        velocidadActual = Vector3.Lerp(velocidadActual, deseada * velocidad, 1f - Mathf.Exp(-suavizado * Time.deltaTime));
        controlador.Move(velocidadActual * Time.deltaTime);

        // Sin gravedad ni salto: la altura se mantiene constante.
        Vector3 posicion = transform.position;
        if (Mathf.Abs(posicion.y - alturaFija) > 0.0005f)
        {
            posicion.y = alturaFija;
            transform.position = posicion;
        }

        if (deseada.sqrMagnitude > 0.001f)
        {
            Quaternion objetivo = Quaternion.LookRotation(deseada, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, objetivo, 1f - Mathf.Exp(-velocidadGiro * Time.deltaTime));
        }
    }

    /// <summary>Teletransporte seguro (colocación inicial).</summary>
    public void Colocar(Vector3 posicion)
    {
        if (controlador != null) controlador.enabled = false;
        transform.position = posicion;
        alturaFija = posicion.y;
        velocidadActual = Vector3.zero;
        if (controlador != null) controlador.enabled = true;
    }

    public Vector3 DireccionPlano
    {
        get
        {
            Vector3 plana = new Vector3(transform.forward.x, 0f, transform.forward.z);
            return plana.sqrMagnitude > 0.0001f ? plana.normalized : Vector3.forward;
        }
    }
}
