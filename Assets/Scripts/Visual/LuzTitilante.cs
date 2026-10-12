using UnityEngine;

/// <summary>
/// Llama de farol: la intensidad respira despacio y de vez en cuando titila. Solo toca la
/// intensidad, nunca el color ni el alcance, así que no altera la lectura del nivel.
/// </summary>
[RequireComponent(typeof(Light))]
public sealed class LuzTitilante : MonoBehaviour
{
    [SerializeField] float variacion = 0.18f;
    [SerializeField] float velocidad = 1.3f;

    Light luz;
    float intensidadBase;
    float semilla;

    void Start()
    {
        luz = GetComponent<Light>();
        intensidadBase = luz.intensity;
        semilla = transform.position.x * 1.7f + transform.position.z * 0.9f;
    }

    void Update()
    {
        if (luz == null) return;

        float t = Time.time * velocidad + semilla;
        float respiro = Mathf.Sin(t) * 0.5f + Mathf.Sin(t * 2.7f) * 0.3f;
        float titileo = Mathf.PerlinNoise(t * 3.1f, semilla) - 0.5f;

        luz.intensity = intensidadBase * (1f + (respiro * 0.5f + titileo) * variacion);
    }
}
