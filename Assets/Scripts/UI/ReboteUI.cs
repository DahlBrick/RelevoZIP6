using UnityEngine;

/// <summary>
/// Lleva la escala de un elemento de UI hacia un objetivo con un pequeño rebote (un muelle
/// amortiguado). Lo usan las opciones de los menús: la que gana el foco crece y se asienta.
/// Funciona con tiempo sin escalar, así que también anima en pausa.
/// </summary>
public sealed class ReboteUI : MonoBehaviour
{
    float objetivo = 1f;
    float actual = 1f;
    float velocidad;

    public void Objetivo(float escala)
    {
        objetivo = escala;
    }

    void OnEnable()
    {
        actual = transform.localScale.x;
        velocidad = 0f;
    }

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        if (Mathf.Abs(actual - objetivo) < 0.0005f && Mathf.Abs(velocidad) < 0.001f) return;

        // Muelle: rígido y algo amortiguado, para que rebote una vez y se asiente.
        const float rigidez = 260f;
        const float amortiguacion = 16f;
        velocidad += ((objetivo - actual) * rigidez - velocidad * amortiguacion) * dt;
        actual += velocidad * dt;
        transform.localScale = new Vector3(actual, actual, 1f);
    }
}
