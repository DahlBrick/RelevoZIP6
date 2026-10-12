using UnityEngine;

/// <summary>
/// Animación ligera de los paneles de la interfaz: interpola el alfa de un CanvasGroup y un
/// pequeño desplazamiento del RectTransform, con tiempo NO escalado, de modo que funciona igual
/// con el juego en marcha y con Time.timeScale = 0 (pausa, cuaderno).
///
/// No hay animaciones de esqueleto ni curvas: es un lerp con suavizado, que es todo lo que
/// necesita una UI de cuaderno. La posición de reposo se captura la primera vez que se anima el
/// objeto, así que el panel se coloca donde diga el constructor y la animación solo lo desplaza
/// respecto a ese punto.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class RelevoUIAnim : MonoBehaviour
{
    CanvasGroup grupo;
    RectTransform recto;

    Vector2 reposo;
    bool reposoCapturado;

    float alfaDesde, alfaHasta, transcurrido, duracion;
    Vector2 posicionDesde, posicionHasta;
    bool animando;
    bool saliendo;
    System.Action alTerminar;

    void Awake()
    {
        Asegurar();
    }

    void Asegurar()
    {
        if (grupo == null)
        {
            grupo = GetComponent<CanvasGroup>();
            if (grupo == null) grupo = gameObject.AddComponent<CanvasGroup>();
        }

        if (recto == null) recto = GetComponent<RectTransform>();

        if (!reposoCapturado && recto != null)
        {
            reposo = recto.anchoredPosition;
            reposoCapturado = true;
        }
    }

    void Update()
    {
        if (!animando) return;

        transcurrido += Time.unscaledDeltaTime;
        float t = duracion > 0f ? Mathf.Clamp01(transcurrido / duracion) : 1f;
        float suave = t * t * (3f - 2f * t);

        grupo.alpha = Mathf.Lerp(alfaDesde, alfaHasta, suave);
        if (recto != null) recto.anchoredPosition = Vector2.Lerp(posicionDesde, posicionHasta, suave);

        if (t < 1f) return;

        animando = false;
        System.Action terminar = alTerminar;
        alTerminar = null;

        if (saliendo)
        {
            saliendo = false;
            gameObject.SetActive(false);
        }

        if (terminar != null) terminar();
    }

    // ------------------------------------------------------------------ API

    /// <summary>
    /// Muestra el objeto fundiéndolo desde el desplazamiento indicado. Si ya está visible y no se
    /// pide repetir, solo se asegura de que esté encendido y opaco (el aviso contextual cambia de
    /// texto a cada paso y no debe reanimarse cada vez).
    /// </summary>
    public static void Entrada(GameObject go, float duracion = 0.20f, float desplazamiento = -16f, bool repetir = false)
    {
        if (go == null) return;

        RelevoUIAnim anim = Preparar(go);
        go.SetActive(true);

        if (!repetir && anim.animando == false && anim.grupo.alpha >= 0.999f)
        {
            anim.grupo.alpha = 1f;
            if (anim.recto != null) anim.recto.anchoredPosition = anim.reposo;
            return;
        }

        anim.saliendo = false;
        anim.alTerminar = null;
        anim.alfaDesde = anim.grupo.alpha;
        anim.alfaHasta = 1f;
        anim.posicionDesde = anim.reposo + new Vector2(0f, desplazamiento);
        anim.posicionHasta = anim.reposo;
        anim.duracion = duracion;
        anim.transcurrido = 0f;
        anim.animando = true;

        if (anim.recto != null) anim.recto.anchoredPosition = anim.posicionDesde;
    }

    /// <summary>Oculta el objeto y lo apaga al terminar. Si ya estaba apagado, no hace nada.</summary>
    public static void Salida(GameObject go, float duracion = 0.18f, System.Action alTerminar = null)
    {
        if (go == null || !go.activeSelf)
        {
            if (alTerminar != null) alTerminar();
            return;
        }

        RelevoUIAnim anim = Preparar(go);

        anim.saliendo = true;
        anim.alTerminar = alTerminar;
        anim.alfaDesde = anim.grupo.alpha;
        anim.alfaHasta = 0f;
        anim.posicionDesde = anim.recto != null ? anim.recto.anchoredPosition : Vector2.zero;
        anim.posicionHasta = anim.reposo + new Vector2(0f, -10f);
        anim.duracion = duracion;
        anim.transcurrido = 0f;
        anim.animando = true;
    }

    /// <summary>Fija el alfa sin animar (el HUD se atenúa de golpe cuando hay un menú abierto).</summary>
    public static void FijarAlfa(GameObject go, float alfa)
    {
        if (go == null) return;

        RelevoUIAnim anim = Preparar(go);
        anim.animando = false;
        anim.saliendo = false;
        anim.alTerminar = null;
        anim.grupo.alpha = Mathf.Clamp01(alfa);
        if (anim.recto != null) anim.recto.anchoredPosition = anim.reposo;
    }

    /// <summary>Recoloca el punto de reposo: se usa cuando el constructor mueve el panel después.</summary>
    public static void FijarReposo(GameObject go)
    {
        if (go == null) return;

        RelevoUIAnim anim = Preparar(go);
        if (anim.recto != null) anim.reposo = anim.recto.anchoredPosition;
        anim.reposoCapturado = true;
    }

    static RelevoUIAnim Preparar(GameObject go)
    {
        RelevoUIAnim anim = go.GetComponent<RelevoUIAnim>();
        if (anim == null) anim = go.AddComponent<RelevoUIAnim>();
        anim.Asegurar();
        return anim;
    }
}
