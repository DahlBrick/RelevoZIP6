using UnityEngine;

/// <summary>
/// Idle de Maia: un terminal encendido, no un personaje que gesticula. Respira —la cornisa sube y
/// baja, la antena se mece, la vegetación que crece a sus espaldas late— y abre y cierra los
/// brazos lo justo para que se lea que está viva desde el otro extremo del muelle.
///
/// No se mueve 'Cuerpo': es el ancla de interacción que MaiaTerminal guarda y su posición marca
/// dónde se planta Nilo para hablar. Todo lo animado son hijos suyos.
///
/// Las posiciones y escalas de reposo se capturan en Awake, así que la escena horneada es la
/// referencia y el idle es una desviación pequeña y continua alrededor de ella.
/// </summary>
public class AnimadorMaia : MonoBehaviour
{
    [Header("Ritmo")]
    [SerializeField] float velocidadRespiracion = 1.5f;
    [SerializeField] float amplitudCornisa = 0.018f;
    [SerializeField] float amplitudAntena = 3.5f;
    [SerializeField] float amplitudVegetacion = 0.05f;
    [SerializeField] float amplitudBrazos = 4f;

    Transform cornisa;
    Transform antena;
    Transform vegetacionA;
    Transform vegetacionB;
    Transform hombroIzquierdo;
    Transform hombroDerecho;

    Vector3 baseCornisa;
    Vector3 baseVegetacionA;
    Vector3 baseVegetacionB;
    Vector3 escalaVegetacionA;
    Vector3 escalaVegetacionB;

    float tiempo;

    void Awake()
    {
        Transform cuerpo = transform.Find("Cuerpo");
        if (cuerpo == null) return;

        cornisa = cuerpo.Find("Cornisa");
        antena = cuerpo.Find("Antena base");
        vegetacionA = cuerpo.Find("Vegetacion A");
        vegetacionB = cuerpo.Find("Vegetacion B");
        hombroIzquierdo = cuerpo.Find("Hombro izquierdo");
        hombroDerecho = cuerpo.Find("Hombro derecho");

        baseCornisa = PosicionDe(cornisa);
        baseVegetacionA = PosicionDe(vegetacionA);
        baseVegetacionB = PosicionDe(vegetacionB);
        escalaVegetacionA = EscalaDe(vegetacionA);
        escalaVegetacionB = EscalaDe(vegetacionB);
    }

    void Update()
    {
        if (cornisa == null && antena == null && vegetacionA == null && hombroIzquierdo == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        tiempo += dt * velocidadRespiracion;

        float respiracion = Mathf.Sin(tiempo);
        float balanceo = Mathf.Sin(tiempo + 1.2f);

        if (cornisa != null)
            cornisa.localPosition = baseCornisa + Vector3.up * respiracion * amplitudCornisa;

        if (antena != null)
            antena.localRotation = Quaternion.Euler(
                respiracion * amplitudAntena, 0f, balanceo * amplitudAntena);

        if (vegetacionA != null)
        {
            vegetacionA.localPosition = baseVegetacionA + Vector3.up * balanceo * 0.03f;
            vegetacionA.localScale = escalaVegetacionA * (1f + respiracion * amplitudVegetacion);
        }

        if (vegetacionB != null)
        {
            vegetacionB.localPosition = baseVegetacionB + Vector3.up * respiracion * 0.026f;
            vegetacionB.localScale = escalaVegetacionB * (1f + balanceo * amplitudVegetacion);
        }

        // Los brazos van en espejo: el izquierdo se abre cuando el derecho se cierra, y ninguno
        // pasa de unos pocos grados.
        if (hombroIzquierdo != null)
            hombroIzquierdo.localRotation = Quaternion.Euler(balanceo * 2f, 0f, -respiracion * amplitudBrazos);

        if (hombroDerecho != null)
            hombroDerecho.localRotation = Quaternion.Euler(balanceo * 2f, 0f, respiracion * amplitudBrazos);
    }

    static Vector3 PosicionDe(Transform objetivo)
    {
        return objetivo != null ? objetivo.localPosition : Vector3.zero;
    }

    static Vector3 EscalaDe(Transform objetivo)
    {
        return objetivo != null ? objetivo.localScale : Vector3.one;
    }
}
