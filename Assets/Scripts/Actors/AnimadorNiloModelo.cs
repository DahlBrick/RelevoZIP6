using UnityEngine;

/// <summary>
/// Animación de Nilo cuando lleva el modelo del kit visual (robot de Quaternius con esqueleto).
/// Sustituye a AnimadorNilo, que mueve las primitivas a mano; este solo alimenta el Animator:
///  - "Velocidad": la del CharacterController, que mezcla reposo, paso y carrera.
///  - Gestos por disparador, a partir de los sonidos que ya pide el juego: así la animación
///    reacciona exactamente a las mismas acciones que el audio, sin tocar la lógica.
///
/// PlayerController sigue siendo el único dueño del movimiento: el Animator no aplica root motion.
/// La bufanda y la antena con luz son piezas propias de Nilo que no trae el modelo; cuelgan del
/// hueso del cuello y se mecen con el paso.
/// </summary>
public sealed class AnimadorNiloModelo : MonoBehaviour
{
    static readonly int Velocidad = Animator.StringToHash("Velocidad");
    static readonly int Si = Animator.StringToHash("Si");
    static readonly int No = Animator.StringToHash("No");
    static readonly int Pulgar = Animator.StringToHash("Pulgar");
    static readonly int Golpe = Animator.StringToHash("Golpe");
    static readonly int Saludo = Animator.StringToHash("Saludo");
    static readonly int Celebrar = Animator.StringToHash("Celebrar");

    [SerializeField] Animator animador;
    [SerializeField] Transform bufandaCola;
    [SerializeField] Transform antena;

    CharacterController controlador;
    Vector3 posicionAnterior;
    float velocidadSuave;
    float fase;

    public void Configurar(Animator animator, Transform cola, Transform antenaBase)
    {
        animador = animator;
        bufandaCola = cola;
        antena = antenaBase;
    }

    void Awake()
    {
        controlador = GetComponent<CharacterController>();
        if (animador == null) animador = GetComponentInChildren<Animator>();
        posicionAnterior = transform.position;
    }

    void Start()
    {
        // GameEvents se limpia en el Awake de LevelBootstrap: suscribirse en Start garantiza que
        // la suscripción sobrevive a esa limpieza.
        GameEvents.SonidoSolicitado += AlSonido;
        GameEvents.NivelCompletado += AlCompletar;
    }

    void OnDestroy()
    {
        GameEvents.SonidoSolicitado -= AlSonido;
        GameEvents.NivelCompletado -= AlCompletar;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 actual = transform.position;
        float recorrido = new Vector2(actual.x - posicionAnterior.x, actual.z - posicionAnterior.z).magnitude;
        posicionAnterior = actual;

        float medida = Mathf.Max(recorrido / dt, controlador != null ? controlador.velocity.magnitude : 0f);
        velocidadSuave = Mathf.Lerp(velocidadSuave, medida, 1f - Mathf.Exp(-10f * dt));

        if (animador != null) animador.SetFloat(Velocidad, velocidadSuave);

        fase += dt * (1.6f + velocidadSuave * 1.4f);
        float marcha = Mathf.Clamp01(velocidadSuave / 5f);

        if (bufandaCola != null)
        {
            bufandaCola.localRotation = Quaternion.Euler(
                18f + marcha * 38f + Mathf.Sin(fase * 1.3f) * (4f + marcha * 6f),
                Mathf.Sin(fase * 0.9f) * 10f,
                Mathf.Sin(fase * 2.1f) * (3f + marcha * 8f));
        }

        if (antena != null)
        {
            antena.localRotation = Quaternion.Euler(
                -marcha * 14f + Mathf.Sin(fase * 2.4f) * 5f, 0f, Mathf.Cos(fase * 1.7f) * 6f);
        }
    }

    void AlSonido(Tono tono)
    {
        if (animador == null) return;

        switch (tono)
        {
            case Tono.Recoger: animador.SetTrigger(Si); break;
            case Tono.Insertar: animador.SetTrigger(Pulgar); break;
            case Tono.Palanca: animador.SetTrigger(Golpe); break;
            case Tono.Recuerdo: animador.SetTrigger(Saludo); break;
            case Tono.Bloqueado: animador.SetTrigger(No); break;
        }
    }

    void AlCompletar(string nivel)
    {
        if (animador != null) animador.SetTrigger(Celebrar);
    }
}
