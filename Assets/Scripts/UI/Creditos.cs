using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Créditos (escena "Creditos"). Suben despacio sobre el agua de Liria con el nombre del final
/// alcanzado y vuelven al título al terminar. Confirmar acelera; Cancelar sale.
/// </summary>
public class Creditos : MonoBehaviour
{
    public const string Escena = "Creditos";

    [SerializeField] InputActionAsset acciones;

    RelevoInput entrada;
    RectTransform rodillo;
    Image velo;
    AudioSource musica;
    float t, alto;
    bool saliendo;

    const float Velocidad = 42f;

    void Start()
    {
        Time.timeScale = 1f;
        GameEvents.Limpiar();
        RelevoGame.Limpiar();
        acciones = KitRelevo.Acciones(acciones);
        entrada = acciones != null ? new RelevoInput(acciones) : null;

        if (FindFirstObjectByType<Camera>() == null)
        {
            var cam = new GameObject("Camara").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.04f, 0.07f);
        }

        musica = gameObject.AddComponent<AudioSource>();
        musica.clip = Resources.Load<AudioClip>("MareasDeLiria");
        musica.loop = true;
        musica.volume = 0f;
        if (musica.clip != null) musica.Play();

        Construir();
    }

    void Construir()
    {
        var raiz = new GameObject("Lienzo", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        raiz.transform.SetParent(transform, false);
        raiz.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var escala = raiz.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1200, 760);
        escala.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        Transform r = raiz.transform;
        Vector2 c = new Vector2(0.5f, 0.5f);

        var fondo = RelevoUI.Franja(r, "Fondo", c, c, Vector2.zero, Vector2.zero, new Color(0.02f, 0.05f, 0.08f)).rectTransform;
        fondo.anchorMin = Vector2.zero; fondo.anchorMax = Vector2.one; fondo.offsetMin = fondo.offsetMax = Vector2.zero;

        rodillo = new GameObject("Rodillo", typeof(RectTransform)).GetComponent<RectTransform>();
        rodillo.SetParent(r, false);
        rodillo.anchorMin = rodillo.anchorMax = new Vector2(0.5f, 0f);
        rodillo.pivot = new Vector2(0.5f, 1f);
        rodillo.sizeDelta = new Vector2(900, 10);

        bool termino = SaveManager.NivelCompletado(LevelLayouts.Cantidad);
        Historia.Final final = Historia.FinalActual;
        int recuerdos = Historia.RecuerdosEncontrados;

        float y = 0f;
        Linea("RELEVO", 72, RelevoUI.Ambar, ref y, 110);
        if (termino)
        {
            Linea(Historia.TituloFinal(final), 24, RelevoUI.Turquesa, ref y, 44);
            Linea("Recuerdos encontrados: " + recuerdos + " de " + Historia.RecuerdosTotales, 16, RelevoUI.Tinta, ref y, 70);
        }
        else y -= 40f;

        Bloque("Una historia de Liria", "Nilo, operador de la subestación\nMaia, la voz de Radio Liria", ref y);
        Bloque("Idea, diseño y narrativa", "Equipo RELEVO", ref y);
        Bloque("Programación y niveles", "Equipo RELEVO", ref y);
        Bloque("Arte y estilo cómic", "Equipo RELEVO", ref y);
        Bloque("Música", "«Mareas de Liria»\n«El Eco del Apagón»\nEquipo RELEVO", ref y);
        Bloque("Hecho con", "Unity 6 · Universal Render Pipeline", ref y);
        Bloque("Los trece puertos", "Muelle de Cobre · Patio de Bombas · Faro de Liria\nAduana de la Marea · Dársena del Torno · Esclusa de las Dos Llaves\nLonja de las Dos Voces · Archivo de la Frecuencia · Astillero de la Maroma\nCasa de las Dos Guardias · Mercado de la Sal · Varadero de las Campanas\nLa Sala del Relevo", ref y);

        y -= 60f;
        if (termino && final == Historia.Final.Estatica)
            Linea("En la baranda del muelle hay trece rayas.", 18, RelevoUI.Verde, ref y, 40);
        else if (termino && final != Historia.Final.ElRelevo)
            Linea("Hay más de una forma de terminar el turno.", 16, RelevoUI.ConAlfa(RelevoUI.Tinta, 0.7f), ref y, 40);
        Linea("«Buenas noches a quienes siguen de turno»", 20, RelevoUI.Verde, ref y, 60);
        alto = -y;

        velo = RelevoUI.Franja(r, "Velo", c, c, Vector2.zero, Vector2.zero, Color.black);
        var vr = velo.rectTransform;
        vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one; vr.offsetMin = vr.offsetMax = Vector2.zero;
    }

    void Linea(string texto, int puntos, Color color, ref float y, float paso)
    {
        int lineas = texto.Split('\n').Length;
        float h = lineas * (puntos + 10f);
        var t = RelevoUI.Texto(rodillo, "Linea", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(900, h), puntos, TextAnchor.UpperCenter, color);
        t.text = texto;
        y -= Mathf.Max(paso, h + 8f);
    }

    void Bloque(string cabecera, string cuerpo, ref float y)
    {
        Linea(cabecera.ToUpperInvariant(), 14, RelevoUI.Cobre, ref y, 26);
        Linea(cuerpo, 18, RelevoUI.Tinta, ref y, 30);
        y -= 34f;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (!saliendo)
        {
            velo.color = new Color(0, 0, 0, Mathf.Clamp01(1f - t / 1.2f));
            if (musica != null) musica.volume = Mathf.Min(0.5f, t * 0.25f);
        }

        float rapido = entrada != null && entrada.Confirmar != null && entrada.Confirmar.IsPressed() ? 5f : 1f;
        Vector2 p = rodillo.anchoredPosition;
        p.y += Velocidad * rapido * Time.deltaTime;
        rodillo.anchoredPosition = p;

        bool cancelar = entrada != null && (entrada.Pulsado(entrada.Cancelar) || entrada.Pulsado(entrada.Pausa));
        if (!saliendo && (cancelar || p.y > alto + 820f)) StartCoroutine(Salir());
    }

    System.Collections.IEnumerator Salir()
    {
        saliendo = true;
        float a = 0f, vol = musica != null ? musica.volume : 0f;
        while (a < 1f)
        {
            a += Time.deltaTime / 1.2f;
            velo.color = new Color(0, 0, 0, a);
            if (musica != null) musica.volume = vol * (1f - a);
            yield return null;
        }
        if (entrada != null) entrada.Desactivar();
        SceneManager.LoadScene(Application.CanStreamedLevelBeLoaded(TituloRelevo.Escena) ? TituloRelevo.Escena : LevelManager.NombreEscenaNivel(1));
    }
}
