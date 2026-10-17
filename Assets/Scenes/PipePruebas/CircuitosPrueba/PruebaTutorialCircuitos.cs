using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PROTOTIPO - Circuitos de energia. Tutorial inicial de la escena de pruebas.
///
/// Vive unicamente en Assets/Scenes/PipePruebas/NivelPruebaMecanica1.unity y no toca nada
/// compartido: no modifica TutorialMaia, ni IlustracionTutorial, ni MaiaTerminal. Se limita a
/// encolar tres mensajes de Maia en el DialogoUI de siempre y a acompanarlos con una tarjeta
/// dibujada con el mismo lenguaje visual que las tarjetas del Patio de Bombas (panel oscuro
/// redondeado, borde de acento, titulo, diagrama y leyenda).
///
/// La tarjeta se ancla abajo a la izquierda, en la misma columna que las burbujas de Maia y
/// justo por encima de ellas: nunca tapa los dialogos inferiores ni el retrato de Nilo. Asi deja
/// libre el centro del tablero, donde estan los tramos que hay que girar.
///
/// Se cuenta una sola vez por sesion, aunque se reinicie el nivel con R.
/// </summary>
public class PruebaTutorialCircuitos : MonoBehaviour
{
    static bool contado;

    const float Ancho = 640f, Alto = 380f;

    // Reposo de la tarjeta y margen que se deja siempre por encima de los dialogos.
    const float ReposoY = 300f;
    const float MargenDialogos = 26f;
    const float TechoPantalla = 1080f;

    // Columna izquierda: la tarjeta se alinea con el borde izquierdo de las burbujas de Maia.
    const float MargenIzquierdo = 176f;

    static readonly Color Turquesa = new Color(0.30f, 0.86f, 0.80f);
    static readonly Color Ambar = new Color(1.00f, 0.71f, 0.27f);
    static readonly Color Coral = new Color(1.00f, 0.42f, 0.33f);
    static readonly Color Cobre = new Color(0.79f, 0.56f, 0.33f);
    static readonly Color Hueso = new Color(0.93f, 0.90f, 0.82f, 0.95f);
    static readonly Color Fondo = new Color(0.02f, 0.05f, 0.06f, 0.97f);

    enum Diagrama { Tramos, Fuentes, Corte }

    struct Tarjeta
    {
        public Diagrama diagrama;
        public string titulo;
        public string leyenda;
        public string voz;
    }

    static readonly Tarjeta[] Tarjetas =
    {
        new Tarjeta
        {
            diagrama = Diagrama.Tramos,
            titulo = "CIRCUITOS DE ENERGÍA",
            leyenda = "Cada tramo gira un cuarto de vuelta. Conecta la fuente con el receptor.",
            voz = "Nilo, la energía solo corre por los tramos que están conectados. Gira cada tramo con E hasta que la fuente llegue al receptor."
        },
        new Tarjeta
        {
            diagrama = Diagrama.Fuentes,
            titulo = "DOS FUENTES",
            leyenda = "Los paneles alimentan en turquesa. La batería alimenta en dorado.",
            voz = "El mismo gesto sirve para las dos energías. Turquesa es la que da el panel; dorada, la que guarda la batería."
        },
        new Tarjeta
        {
            diagrama = Diagrama.Corte,
            titulo = "CONEXIÓN COMPLETA",
            leyenda = "La energía solo avanza por los tramos conectados. Si el camino se corta, el puente o la compuerta se repliega.",
            voz = "Si un tramo queda en contra, el camino se corta y el puente se repliega. Vuelve a girarlo y cruza."
        }
    };

    struct Flujo
    {
        public RectTransform rt;
        public Image img;
        public float x0, x1, y, fase;
        public Color color;
    }

    int indice;
    GameObject tarjeta;
    RectTransform raiz;
    RectTransform lienzo;
    RectTransform lienzoTarjeta;
    RectTransform pilaDialogo;
    Image borde;
    Text titulo;
    Text leyenda;
    Image tramoGiratorio;

    readonly List<Flujo> flujos = new List<Flujo>();

    // Reutilizado en Colocar() para no reservar memoria en cada fotograma.
    readonly Vector3[] esquinas = new Vector3[4];

    /// <summary>Cuenta de veces que se ha mostrado; sirve para comprobar que solo sale una vez.</summary>
    public int VecesMostrado { get; private set; }

    void Start()
    {
        if (contado) return;
        StartCoroutine(EsperarYContar());
    }

    IEnumerator EsperarYContar()
    {
        // El UIManager y su dialogo los crea LevelBootstrap en Awake; se espera por si acaso.
        float espera = 0f;
        while (espera < 5f && (RelevoGame.UI == null || RelevoGame.UI.Dialogo == null))
        {
            espera += Time.unscaledDeltaTime;
            yield return null;
        }

        if (RelevoGame.UI == null || RelevoGame.UI.Dialogo == null) yield break;
        if (contado) yield break;

        contado = true;

        // Un fotograma de margen para que la intro del nivel ocupe el primer mensaje.
        yield return null;

        Construir();
        Encolar();
    }

    // ------------------------------------------------------------------ construccion

    void Construir()
    {
        lienzo = RelevoGame.UI.Dialogo.transform.parent as RectTransform;
        if (lienzo == null) lienzo = RelevoGame.UI.Dialogo.transform.parent.GetComponent<RectTransform>();
        if (lienzo == null) return;

        // Pila de burbujas de Maia: su altura decide cuanto puede bajar la tarjeta.
        Transform pila = RelevoGame.UI.Dialogo.transform.Find("Burbujas");
        pilaDialogo = pila as RectTransform;

        tarjeta = new GameObject("Tarjeta circuitos");
        tarjeta.transform.SetParent(lienzo, false);

        raiz = tarjeta.AddComponent<RectTransform>();
        // Anclada abajo a la izquierda: crece hacia arriba y hacia la derecha, nunca hacia los
        // dialogos ni hacia el centro del tablero.
        raiz.anchorMin = raiz.anchorMax = new Vector2(0f, 0f);
        raiz.pivot = new Vector2(0f, 0f);
        raiz.sizeDelta = new Vector2(Ancho, Alto);
        raiz.anchoredPosition = new Vector2(MargenIzquierdo, ReposoY);

        CanvasGroup grupo = tarjeta.AddComponent<CanvasGroup>();
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        Image fondo = tarjeta.AddComponent<Image>();
        fondo.sprite = Formas.Redondeado();
        fondo.type = Image.Type.Sliced;
        fondo.color = Fondo;
        fondo.raycastTarget = false;

        borde = Img(raiz, "Borde", Formas.RedondeadoContorno(), Vector2.zero, Vector2.zero, Turquesa);
        borde.type = Image.Type.Sliced;
        borde.rectTransform.anchorMin = Vector2.zero;
        borde.rectTransform.anchorMax = Vector2.one;
        borde.rectTransform.offsetMin = borde.rectTransform.offsetMax = Vector2.zero;

        // Jerarquia clara: titulo arriba, diagrama en el centro, leyenda abajo.
        titulo = RelevoUI.Texto(raiz, "Titulo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 159f), new Vector2(Ancho - 40f, 34f), 24, TextAnchor.MiddleCenter, Turquesa);
        titulo.font = RelevoUI.FuenteTitulo();

        leyenda = RelevoUI.Texto(raiz, "Leyenda", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -142f), new Vector2(Ancho - 44f, 64f), 18, TextAnchor.MiddleCenter, Hueso);
        leyenda.font = RelevoUI.Fuente();

        GameObject l = new GameObject("Lienzo");
        l.transform.SetParent(raiz, false);
        lienzoTarjeta = l.AddComponent<RectTransform>();
        lienzoTarjeta.anchorMin = lienzoTarjeta.anchorMax = lienzoTarjeta.pivot = new Vector2(0.5f, 0.5f);
        lienzoTarjeta.anchoredPosition = new Vector2(0f, 16f);
        lienzoTarjeta.sizeDelta = new Vector2(Ancho - 40f, 236f);

        tarjeta.SetActive(false);
    }

    // ------------------------------------------------------------------ presentacion

    void Encolar()
    {
        // La primera tarjeta se muestra ya; las siguientes las enciende el cierre de la anterior.
        Mostrar(0);

        for (int i = 0; i < Tarjetas.Length; i++)
        {
            int siguiente = i;
            RelevoGame.UI.Dialogo.Encolar(new DialogoUI.Mensaje
            {
                titulo = "Maia",
                texto = Tarjetas[i].voz,
                acento = RelevoUI.Verde,
                alCerrar = delegate { AvanzarDesde(siguiente); }
            });
        }
    }

    void AvanzarDesde(int cerrada)
    {
        // Solo avanza la tarjeta del mensaje que se acaba de cerrar, no la de otro.
        if (cerrada != indice) return;

        if (indice + 1 < Tarjetas.Length) Mostrar(indice + 1);
        else Ocultar();
    }

    void Mostrar(int i)
    {
        if (tarjeta == null) return;

        indice = i;
        Tarjeta t = Tarjetas[i];

        titulo.text = t.titulo;
        leyenda.text = t.leyenda;

        if (lienzoTarjeta != null)
            foreach (Transform hijo in lienzoTarjeta) Destroy(hijo.gameObject);
        flujos.Clear();
        tramoGiratorio = null;

        switch (t.diagrama)
        {
            case Diagrama.Tramos: DibujarTramos(lienzoTarjeta); break;
            case Diagrama.Fuentes: DibujarFuentes(lienzoTarjeta); break;
            case Diagrama.Corte: DibujarCorte(lienzoTarjeta); break;
        }

        Colocar();
        tarjeta.SetActive(true);
        tarjeta.transform.SetAsLastSibling();
        VecesMostrado++;
    }

    void Ocultar()
    {
        if (tarjeta != null && tarjeta.activeSelf) tarjeta.SetActive(false);
    }

    /// <summary>Sube la tarjeta lo justo para no rozar la pila de burbujas de Maia y la alinea con su columna.</summary>
    void Colocar()
    {
        if (raiz == null) return;

        float y = ReposoY;

        if (pilaDialogo != null)
        {
            // La pila cuelga del borde inferior del lienzo: su techo es posicion + alto.
            float techoBurbujas = pilaDialogo.anchoredPosition.y + pilaDialogo.sizeDelta.y;
            y = Mathf.Max(y, techoBurbujas + MargenDialogos);
        }

        // Nunca por encima del borde superior de la pantalla de referencia.
        y = Mathf.Min(y, TechoPantalla - Alto - 18f);

        // X: el borde izquierdo de la tarjeta coincide con el de las burbujas, para que ambas
        // formen una sola columna. Si aun no hay pila, se usa el margen de reposo.
        float x = MargenIzquierdo;

        if (pilaDialogo != null && lienzo != null)
        {
            pilaDialogo.GetWorldCorners(esquinas);
            float izquierda = lienzo.InverseTransformPoint(esquinas[0]).x - lienzo.rect.xMin;
            if (izquierda > 0f) x = izquierda;
        }

        raiz.anchoredPosition = new Vector2(x, y);
    }

    // ------------------------------------------------------------------ diagramas

    /// <summary>Diagrama 1: fuente, tres tramos giratorios, receptor.</summary>
    void DibujarTramos(RectTransform lienzoTarjeta)
    {
        const float fila = 30f;

        // Fuente de energia.
        Img(lienzoTarjeta, "Fuente", Formas.Redondeado(), new Vector2(-218f, fila), new Vector2(50f, 50f), Turquesa)
            .type = Image.Type.Sliced;
        Etiqueta(lienzoTarjeta, "fuente", new Vector2(-218f, -14f), Turquesa);

        // Enlace fuente -> primer tramo.
        Img(lienzoTarjeta, "Enlace", Formas.Redondeado(), new Vector2(-155f, fila), new Vector2(58f, 8f),
            new Color(Turquesa.r, Turquesa.g, Turquesa.b, 0.45f)).type = Image.Type.Sliced;

        // Tres tramos: el central gira sin parar, asi se ve el cuarto de vuelta y el desalineado.
        for (int i = 0; i < 3; i++)
        {
            float x = -84f + i * 84f;

            Image casilla = Img(lienzoTarjeta, "Casilla", Formas.RedondeadoContorno(),
                new Vector2(x, fila), new Vector2(66f, 66f), new Color(Cobre.r, Cobre.g, Cobre.b, 0.55f));
            casilla.type = Image.Type.Sliced;

            Image barra = Img(lienzoTarjeta, "Barra", Formas.Redondeado(),
                Vector2.zero, new Vector2(56f, 12f), Turquesa);
            barra.type = Image.Type.Sliced;
            barra.rectTransform.SetParent(casilla.rectTransform, false);
            barra.rectTransform.anchoredPosition = Vector2.zero;

            if (i == 1) tramoGiratorio = barra;
        }

        // Enlace ultimo tramo -> receptor.
        Img(lienzoTarjeta, "Enlace", Formas.Redondeado(), new Vector2(155f, fila), new Vector2(58f, 8f),
            new Color(Cobre.r, Cobre.g, Cobre.b, 0.45f)).type = Image.Type.Sliced;

        // Receptor: la compuerta o el puente que se abre al cerrar el circuito.
        Img(lienzoTarjeta, "Receptor", Formas.Redondeado(), new Vector2(218f, fila), new Vector2(50f, 50f), Cobre)
            .type = Image.Type.Sliced;
        Etiqueta(lienzoTarjeta, "receptor", new Vector2(218f, -14f), Cobre);
    }

    /// <summary>Diagrama 2: el mismo gesto con las dos energias, panel y bateria.</summary>
    void DibujarFuentes(RectTransform lienzoTarjeta)
    {
        FilaEnergia(lienzoTarjeta, 48f, Turquesa, "panel", false);
        FilaEnergia(lienzoTarjeta, -52f, Ambar, "batería", true);
    }

    void FilaEnergia(RectTransform padre, float y, Color color, string nombre, bool bateria)
    {
        Image fuente = Img(padre, nombre, Formas.Redondeado(), new Vector2(-232f, y),
            bateria ? new Vector2(48f, 58f) : new Vector2(54f, 54f), color);
        fuente.type = Image.Type.Sliced;

        if (bateria)
        {
            // Borne de la pila: dos trazos para que se lea como bateria y no como panel.
            Img(fuente.rectTransform, "Borne", null, new Vector2(-10f, 32f), new Vector2(10f, 8f), Cobre);
            Img(fuente.rectTransform, "Borne", null, new Vector2(10f, 32f), new Vector2(10f, 8f), Cobre);
        }

        Etiqueta(padre, nombre, new Vector2(-142f, y), color);

        // Cauce tenue + chispas que recorren el camino hasta el receptor.
        Img(padre, "Cauce", Formas.Redondeado(), new Vector2(46f, y), new Vector2(224f, 8f),
            new Color(color.r, color.g, color.b, 0.32f)).type = Image.Type.Sliced;

        for (int i = 0; i < 4; i++)
            Chispa(padre, -66f, 158f, y, i * 0.25f, color);

        Img(padre, "Receptor", Formas.Redondeado(), new Vector2(222f, y), new Vector2(40f, 40f), Cobre)
            .type = Image.Type.Sliced;
    }

    /// <summary>Diagrama 3: camino unido que alimenta el receptor, y camino cortado.</summary>
    void DibujarCorte(RectTransform lienzoTarjeta)
    {
        // ---- fila superior: conexion correcta, puente desplegado y energia cruzando
        Etiqueta(lienzoTarjeta, "unido", new Vector2(-232f, 86f), Turquesa);
        Orilla(lienzoTarjeta, -218f, 50f);
        Orilla(lienzoTarjeta, 218f, 50f);

        for (int i = 0; i < 3; i++)
        {
            Image tablon = Img(lienzoTarjeta, "Tablon", Formas.Redondeado(),
                new Vector2(-60f + i * 60f, 50f), new Vector2(54f, 16f), Turquesa);
            tablon.type = Image.Type.Sliced;
        }

        for (int i = 0; i < 4; i++)
            Chispa(lienzoTarjeta, -170f, 170f, 50f, i * 0.25f, Turquesa);

        // ---- fila inferior: el tramo en contra corta el camino y el puente se repliega
        Etiqueta(lienzoTarjeta, "cortado", new Vector2(-232f, -84f), Coral);
        Orilla(lienzoTarjeta, -218f, -50f);
        Orilla(lienzoTarjeta, 218f, -50f);

        for (int i = 0; i < 3; i++)
        {
            Image tablon = Img(lienzoTarjeta, "Tablon", Formas.Redondeado(),
                new Vector2(-150f + i * 42f, -50f), new Vector2(34f, 14f),
                new Color(Cobre.r, Cobre.g, Cobre.b, 0.85f));
            tablon.type = Image.Type.Sliced;
        }

        Img(lienzoTarjeta, "Corte", null, new Vector2(40f, -50f), new Vector2(76f, 8f), Coral)
            .rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Img(lienzoTarjeta, "Corte", null, new Vector2(40f, -50f), new Vector2(76f, 8f), Coral)
            .rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
    }

    void Orilla(RectTransform padre, float x, float y)
    {
        Img(padre, "Orilla", null, new Vector2(x, y), new Vector2(84f, 22f), Cobre);
    }

    void Chispa(RectTransform padre, float x0, float x1, float y, float fase, Color color)
    {
        Image punto = Img(padre, "Chispa", Formas.Redondeado(), new Vector2(x0, y), new Vector2(12f, 12f), color);
        flujos.Add(new Flujo
        {
            rt = punto.rectTransform,
            img = punto,
            x0 = x0,
            x1 = x1,
            y = y,
            fase = fase,
            color = color
        });
    }

    void Update()
    {
        if (tarjeta == null || !tarjeta.activeSelf) return;

        // La tarjeta sigue a la pila de burbujas aunque esta crezca con el mensaje siguiente.
        Colocar();

        if (tramoGiratorio != null)
        {
            // Giro lento y continuo del tramo central: la tarjeta se mueve sin distraer.
            tramoGiratorio.rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, Mathf.Repeat(Time.unscaledTime * 45f, 90f));
        }

        for (int i = 0; i < flujos.Count; i++)
        {
            Flujo f = flujos[i];
            if (f.rt == null || f.img == null) continue;

            float t = Mathf.Repeat(Time.unscaledTime * 0.45f + f.fase, 1f);
            f.rt.anchoredPosition = new Vector2(Mathf.Lerp(f.x0, f.x1, t), f.y);

            Color c = f.color;
            c.a = Mathf.Sin(t * Mathf.PI) * 0.95f;
            f.img.color = c;
        }
    }

    // ------------------------------------------------------------------ utilidades

    void Etiqueta(Transform padre, string texto, Vector2 pos, Color color)
    {
        Text t = RelevoUI.Texto(padre, "Etiqueta", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            pos, new Vector2(200f, 22f), 15, TextAnchor.MiddleCenter, color);
        t.font = RelevoUI.Fuente();
        t.text = texto;
    }

    static Image Img(Transform padre, string nombre, Sprite sprite, Vector2 pos, Vector2 tam, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);

        Image i = go.AddComponent<Image>();
        i.sprite = sprite;
        i.color = color;
        i.raycastTarget = false;

        RectTransform rt = i.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return i;
    }

    /// <summary>Formas redondeadas dibujadas en memoria: no crean ningun asset en el proyecto.</summary>
    static class Formas
    {
        static Sprite redondeado, contorno;

        static float Caja(float x, float y, float lado, float radio)
        {
            float cx = Mathf.Clamp(x, radio, lado - radio);
            float cy = Mathf.Clamp(y, radio, lado - radio);
            return new Vector2(x - cx, y - cy).magnitude - radio;
        }

        static Sprite Hacer(System.Func<float, float, float> alfa)
        {
            const int lado = 48;
            Texture2D t = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.DontSave;
            t.wrapMode = TextureWrapMode.Clamp;

            Color32[] px = new Color32[lado * lado];
            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                    px[y * lado + x] = new Color32(255, 255, 255,
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(alfa(x + 0.5f, y + 0.5f)) * 255f));
            t.SetPixels32(px);
            t.Apply();

            Sprite s = Sprite.Create(t, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(14f, 14f, 14f, 14f));
            s.hideFlags = HideFlags.DontSave;
            return s;
        }

        public static Sprite Redondeado()
        {
            if (redondeado == null)
                redondeado = Hacer((x, y) => Mathf.Clamp01(0.5f - Caja(x, y, 48f, 10f)));
            return redondeado;
        }

        public static Sprite RedondeadoContorno()
        {
            if (contorno == null)
                contorno = Hacer((x, y) =>
                {
                    float d = Caja(x, y, 48f, 10f);
                    return Mathf.Clamp01(0.5f - d) - Mathf.Clamp01(0.5f - (d + 2f));
                });
            return contorno;
        }
    }
}
