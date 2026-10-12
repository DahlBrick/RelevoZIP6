using UnityEngine;

/// <summary>
/// Marcha e idle de Nilo. Vive en el propio Nilo y no toca PlayerController, que sigue siendo el
/// único dueño del movimiento: la velocidad se lee de CharacterController.velocity, que es
/// pública (la 'velocidadActual' de PlayerController es privada y no se expone) y se contrasta
/// con el desplazamiento real del transform por si el controlador informa de cero en algún tramo.
///
/// Todo lo que se mueve cuelga de 'Cuerpo/Figura'. Ni el CharacterController, ni 'Cuerpo', ni
/// 'ManoBateria' —que marca dónde se apoya la batería y se resuelve por la ruta
/// "Cuerpo/ManoBateria"— se tocan, y la sombra de contacto, que es hija de 'Cuerpo', se queda
/// pegada al suelo mientras el cuerpo cabecea.
///
/// Las caderas y los hombros son anclas vacías que crea ActorFactory en el eje de giro: sin
/// ellas, rotar una pierna la haría girar sobre su propio centro en vez de oscilar desde la
/// cadera. Sus medidas locales reproducen la pose de reposo al milímetro, así que parado la
/// silueta es exactamente la del Bloque 4B.
/// </summary>
public class AnimadorNilo : MonoBehaviour
{
    [Header("Ritmo")]
    [Tooltip("Velocidad a la que el trote alcanza su amplitud completa.")]
    [SerializeField] float velocidadDeTrote = 4.2f;
    [SerializeField] float amplitudPierna = 20f;
    [SerializeField] float amplitudBrazo = 14f;
    [SerializeField] float amplitudCabeceo = 0.035f;

    CharacterController controlador;
    Transform figura;
    Transform caderaIzquierda;
    Transform caderaDerecha;
    Transform hombroIzquierdo;
    Transform hombroDerecho;
    Transform bufandaCola;
    Transform antenaBase;

    Vector3 posicionAnterior;
    float trote;
    float faseMarcha;
    float faseIdle;

    void Awake()
    {
        controlador = GetComponent<CharacterController>();

        figura = transform.Find("Cuerpo/Figura");
        if (figura == null) return;

        caderaIzquierda = figura.Find("Cadera izquierda");
        caderaDerecha = figura.Find("Cadera derecha");
        hombroIzquierdo = figura.Find("Hombro pivote izquierdo");
        hombroDerecho = figura.Find("Hombro pivote derecho");
        bufandaCola = figura.Find("Bufanda cola");
        antenaBase = figura.Find("Antena base");

        posicionAnterior = transform.position;
    }

    void Update()
    {
        if (figura == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float velocidad = Velocidad();

        trote = Mathf.MoveTowards(trote,
            Mathf.Clamp01(velocidad / Mathf.Max(0.1f, velocidadDeTrote)), dt * 3.5f);

        // La fase de marcha avanza con la velocidad para que los pasos no patinen; la de idle
        // corre siempre y es la que sostiene la respiración y el cabeceo de la antena.
        faseMarcha += velocidad * 2.4f * dt;
        faseIdle += dt;

        float paso = Mathf.Sin(faseMarcha);
        float respiracion = Mathf.Sin(faseIdle * 1.9f);
        float vaiven = Mathf.Sin(faseIdle * 0.7f);
        float quieto = 1f - trote;

        if (caderaIzquierda != null)
            caderaIzquierda.localRotation = Quaternion.Euler(paso * amplitudPierna * trote, 0f, 0f);

        if (caderaDerecha != null)
            caderaDerecha.localRotation = Quaternion.Euler(-paso * amplitudPierna * trote, 0f, 0f);

        // El brazo del lado de 'ManoBateria' (x negativa) oscila menos de la mitad: cuando Nilo
        // lleva la celda, la batería se apoya en un punto fijo de 'Cuerpo' y ese brazo tiene que
        // quedarse cerca para no despegarse de ella.
        if (hombroIzquierdo != null)
            hombroIzquierdo.localRotation = Quaternion.Euler(-paso * amplitudBrazo * trote * 0.45f, 0f, 0f);

        if (hombroDerecho != null)
            hombroDerecho.localRotation = Quaternion.Euler(paso * amplitudBrazo * trote, 0f, 0f);

        // Cabeceo: dos apoyos por ciclo de marcha (de ahí el valor absoluto) más la respiración
        // del idle, que solo se nota cuando está parado.
        float flotacion = Mathf.Abs(paso) * amplitudCabeceo * trote + respiracion * 0.012f * quieto;
        figura.localPosition = new Vector3(0f, flotacion, 0f);

        figura.localRotation = Quaternion.Euler(3.5f * trote, vaiven * 3f * quieto, 0f);

        if (bufandaCola != null)
            bufandaCola.localRotation = Quaternion.Euler(
                14f + respiracion * 4f, 0f, -18f + paso * 7f * trote);

        if (antenaBase != null)
            antenaBase.localRotation = Quaternion.Euler(
                respiracion * 4f * quieto + paso * 5f * trote, 0f, Mathf.Cos(faseIdle * 1.3f) * 4f);
    }

    float Velocidad()
    {
        Vector3 actual = transform.position;
        float recorrido = Vector3.Distance(
            new Vector3(posicionAnterior.x, 0f, posicionAnterior.z),
            new Vector3(actual.x, 0f, actual.z));
        posicionAnterior = actual;

        float porDesplazamiento = Time.deltaTime > 0f ? recorrido / Time.deltaTime : 0f;
        float porControlador = controlador != null ? controlador.velocity.magnitude : 0f;

        return Mathf.Max(porControlador, porDesplazamiento);
    }
}
