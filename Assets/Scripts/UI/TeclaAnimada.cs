using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tecla del teclado dibujada en dos capas (sprite de Kenney Input Prompts): la tapa y, debajo, la
/// misma tecla oscurecida y desplazada, que hace de canto. Al "presionarse", la tapa baja hasta
/// tapar el canto y se achata un poco, que es lo que se lee como una tecla física hundiéndose.
///
///  - En reposo hace una pulsación suave cada pocos segundos: invita a pulsar sin distraer.
///  - Presionar() hace la pulsación de verdad (más honda y rápida) y suelta una onda.
///
/// Usa tiempo sin escalar, para animarse también con el juego en pausa.
/// </summary>
public sealed class TeclaAnimada : MonoBehaviour
{
    // Canto visible bajo la tapa, en proporción al tamaño de la tecla (14 %).
    float Hundimiento { get { return lado * 0.14f; } }
    float lado = 58f;

    RectTransform tapa;
    Image imagenTapa;
    Image imagenCanto;
    Image onda;
    Color color = Color.white;

    float periodo = 1.6f;
    float relojReposo;
    float relojPulsacion = -1f;
    float relojOnda = -1f;
    bool fuerte;

    public static TeclaAnimada Crear(Transform padre, string nombre, Sprite sprite, Vector2 ancla, Vector2 pivote,
        Vector2 posicion, float lado, Color color, bool conOnda)
    {
        GameObject raiz = new GameObject(nombre);
        raiz.transform.SetParent(padre, false);
        RectTransform rt = raiz.AddComponent<RectTransform>();
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = new Vector2(lado, lado);

        TeclaAnimada tecla = raiz.AddComponent<TeclaAnimada>();
        tecla.lado = lado;

        if (conOnda)
        {
            tecla.onda = Capa(raiz.transform, "Onda", RelevoUI.Formas.HexagonoAnillo(), lado * 1.2f);
            tecla.onda.color = Color.clear;
        }

        tecla.imagenCanto = Capa(raiz.transform, "Canto", sprite, lado);
        tecla.imagenCanto.rectTransform.anchoredPosition = new Vector2(0f, -tecla.Hundimiento);

        tecla.imagenTapa = Capa(raiz.transform, "Tapa", sprite, lado);
        tecla.tapa = tecla.imagenTapa.rectTransform;

        tecla.Tenir(color);
        return tecla;
    }

    static Image Capa(Transform padre, string nombre, Sprite sprite, float lado)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        Image imagen = go.AddComponent<Image>();
        imagen.sprite = sprite;
        imagen.preserveAspect = true;
        imagen.raycastTarget = false;
        RectTransform rt = imagen.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(lado, lado);
        return imagen;
    }

    /// <summary>Color de la tecla; el canto es el mismo color oscurecido.</summary>
    public void Tenir(Color nuevo)
    {
        color = nuevo;
        if (imagenTapa != null) imagenTapa.color = nuevo;
        if (imagenCanto != null) imagenCanto.color = new Color(nuevo.r * 0.32f, nuevo.g * 0.26f, nuevo.b * 0.22f, nuevo.a);
    }

    /// <summary>Cada cuántos segundos hace la pulsación suave de reposo (0 la desactiva).</summary>
    public void Reposo(float segundos)
    {
        periodo = segundos;
    }

    /// <summary>Pulsación real: más honda, más rápida y con onda.</summary>
    public void Presionar()
    {
        relojPulsacion = 0f;
        fuerte = true;
        relojOnda = 0f;
        relojReposo = 0f;
    }

    void OnEnable()
    {
        relojReposo = 0f;
        relojPulsacion = -1f;
        if (tapa != null) tapa.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        if (periodo > 0f && relojPulsacion < 0f)
        {
            relojReposo += dt;
            if (relojReposo >= periodo)
            {
                relojReposo = 0f;
                relojPulsacion = 0f;
                fuerte = false;
            }
        }

        AnimarPulsacion(dt);
        AnimarOnda(dt);
    }

    void AnimarPulsacion(float dt)
    {
        if (tapa == null || relojPulsacion < 0f) return;

        // Bajar rápido, sostener un instante y volver con un pequeño rebote.
        float bajada = fuerte ? 0.06f : 0.1f;
        float sosten = fuerte ? 0.08f : 0.05f;
        float subida = fuerte ? 0.22f : 0.2f;
        float total = bajada + sosten + subida;

        relojPulsacion += dt;
        float t = relojPulsacion;
        float hondo;

        if (t < bajada) hondo = Suave(t / bajada);
        else if (t < bajada + sosten) hondo = 1f;
        else if (t < total)
        {
            float s = (t - bajada - sosten) / subida;
            hondo = 1f - Suave(s) - Mathf.Sin(s * Mathf.PI) * 0.18f;
        }
        else
        {
            hondo = 0f;
            relojPulsacion = -1f;
        }

        float profundidad = fuerte ? 1f : 0.7f;
        tapa.anchoredPosition = new Vector2(0f, -Hundimiento * hondo * profundidad);
        tapa.localScale = new Vector3(1f + 0.05f * hondo, 1f - 0.1f * hondo * profundidad, 1f);

        if (imagenTapa != null)
        {
            float luz = 1f - 0.28f * hondo * profundidad;
            imagenTapa.color = new Color(color.r * luz, color.g * luz, color.b * luz, color.a);
        }
    }

    void AnimarOnda(float dt)
    {
        if (onda == null || relojOnda < 0f) return;

        relojOnda += dt;
        float t = Mathf.Clamp01(relojOnda / 0.45f);
        onda.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.9f, 2.4f, Suave(t));
        onda.color = new Color(color.r, color.g, color.b, (1f - t) * 0.85f);

        if (t >= 1f)
        {
            relojOnda = -1f;
            onda.color = Color.clear;
        }
    }

    static float Suave(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }
}
