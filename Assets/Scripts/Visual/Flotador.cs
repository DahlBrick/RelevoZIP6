using UnityEngine;

/// <summary>
/// Balanceo de un objeto que flota: sube y baja con la marea y cabecea/alabea un poco. Para
/// barcas, boyas y cualquier cosa que esté en el agua. El desfase sale de la posición, así que
/// dos barcas vecinas nunca se mueven a la vez.
/// </summary>
[DisallowMultipleComponent]
public sealed class Flotador : MonoBehaviour
{
    [SerializeField] float altura = 0.06f;
    [SerializeField] float periodo = 3.2f;
    [SerializeField] float cabeceo = 3.5f;
    [SerializeField] float alabeo = 4.5f;
    [SerializeField] float deriva = 0f;

    Vector3 posicionBase;
    Quaternion rotacionBase;
    float fase;

    public void Configurar(float alturaOla, float periodoOla, float gradosCabeceo, float gradosAlabeo, float giroDeriva = 0f)
    {
        altura = alturaOla;
        periodo = periodoOla;
        cabeceo = gradosCabeceo;
        alabeo = gradosAlabeo;
        deriva = giroDeriva;
    }

    void Start()
    {
        posicionBase = transform.localPosition;
        rotacionBase = transform.localRotation;
        fase = transform.position.x * 0.73f + transform.position.z * 1.37f;
    }

    void Update()
    {
        float t = Time.time * Mathf.PI * 2f / Mathf.Max(0.1f, periodo) + fase;

        transform.localPosition = posicionBase + Vector3.up * (Mathf.Sin(t) * altura);
        transform.localRotation = rotacionBase * Quaternion.Euler(
            Mathf.Sin(t + 0.9f) * cabeceo,
            Mathf.Sin(t * 0.13f) * deriva,
            Mathf.Sin(t * 0.8f + 2.1f) * alabeo);
    }
}
