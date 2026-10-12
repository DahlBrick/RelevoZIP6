using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tarjeta ilustrada que acompaña a los diálogos de Maia cuando explica una mecánica. Son
/// pequeños diagramas animados en bucle, dibujados con formas de la interfaz:
///
///  - Reserva: Nilo se aleja de la batería, su barra baja paso a paso, y vuelve a recargar.
///  - Apagado: con la barra vacía, la antena se pone roja, los ojos se apagan y agacha la cabeza.
///  - Cajas: Nilo empuja una caja sobre una placa; el cable se enciende y la compuerta baja.
///
/// Todo usa tiempo sin escalar: se anima aunque el juego esté detenido por el diálogo.
/// </summary>
public sealed class IlustracionTutorial : MonoBehaviour
{
    public enum Tipo { Ninguna, Reserva, Apagado, Cajas }

    const float Ancho = 540f, Alto = 300f;

    static readonly Color ColAmbar = new Color(1f, 0.71f, 0.27f);
    static readonly Color ColTurquesa = new Color(0.3f, 0.86f, 0.8f);
    static readonly Color ColCoral = new Color(1f, 0.42f, 0.33f);
    static readonly Color ColRojo = new Color(1f, 0.12f, 0.08f);
    static readonly Color ColCuerpo = new Color(0.9f, 0.88f, 0.82f);
    static readonly Color ColOscuro = new Color(0.12f, 0.16f, 0.22f);
    static readonly Color ColCobre = new Color(0.79f, 0.56f, 0.33f);
    static readonly Color ColMadera = new Color(0.62f, 0.4f, 0.22f);

    RectTransform raiz;
    RectTransform lienzo;
    Text titulo;
    Text leyenda;
    Tipo tipo;
    float reloj;
    float aparicion = 1f;
    CanvasGroup grupo;

    // Piezas comunes.
    Nilo nilo;
    Image[] segmentos;
    RectTransform barra;
    Image bateria, aro;
    Image caja, placa, cable, barrotes;
    readonly List<Image> huellas = new List<Image>();
    RectTransform barrotesRt;
    float ultimaHuella;

    public static IlustracionTutorial Crear(Transform padre)
    {
        GameObject go = new GameObject("Ilustracion");
        go.transform.SetParent(padre, false);
        IlustracionTutorial il = go.AddComponent<IlustracionTutorial>();
        il.Construir();
        return il;
    }

    void Construir()
    {
        raiz = gameObject.AddComponent<RectTransform>();
        raiz.anchorMin = raiz.anchorMax = raiz.pivot = Vector2.zero;
        raiz.sizeDelta = new Vector2(Ancho, Alto);

        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.blocksRaycasts = false;

        Image fondo = gameObject.AddComponent<Image>();
        fondo.sprite = Formas.Redondeado();
        fondo.type = Image.Type.Sliced;
        fondo.color = new Color(0.02f, 0.05f, 0.06f, 1f);
        fondo.raycastTarget = false;

        Image borde = Img(raiz, "Borde", Formas.RedondeadoContorno(), Vector2.zero, Vector2.zero, ColTurquesa);
        borde.type = Image.Type.Sliced;
        Estirar(borde.rectTransform);

        titulo = RelevoUI.Texto(raiz, "Titulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -12f), new Vector2(Ancho - 40f, 30f), 24, TextAnchor.MiddleCenter, ColTurquesa);
        titulo.font = RelevoUI.FuenteTitulo();

        leyenda = RelevoUI.Texto(raiz, "Leyenda", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 12f), new Vector2(Ancho - 40f, 44f), 17, TextAnchor.MiddleCenter,
            new Color(0.93f, 0.9f, 0.82f, 0.95f));
        leyenda.font = RelevoUI.Fuente();

        GameObject l = new GameObject("Lienzo");
        l.transform.SetParent(raiz, false);
        lienzo = l.AddComponent<RectTransform>();
        lienzo.anchorMin = lienzo.anchorMax = lienzo.pivot = new Vector2(0.5f, 0.5f);
        lienzo.anchoredPosition = new Vector2(0f, 6f);
        lienzo.sizeDelta = new Vector2(Ancho - 40f, 190f);

        gameObject.SetActive(false);
    }

    public void Mostrar(Tipo nuevo, Color acento)
    {
        tipo = nuevo;
        reloj = 0f;
        foreach (Transform hijo in lienzo) Destroy(hijo.gameObject);
        huellas.Clear();
        segmentos = null;
        nilo = null;

        if (nuevo == Tipo.Ninguna)
        {
            gameObject.SetActive(false);
            return;
        }

        switch (nuevo)
        {
            case Tipo.Reserva: ConstruirReserva(); break;
            case Tipo.Apagado: ConstruirApagado(); break;
            case Tipo.Cajas: ConstruirCajas(); break;
        }

        gameObject.SetActive(true);
        aparicion = 0f;
    }

    public void Ocultar()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ escenas

    void Suelo(float y)
    {
        Image s = Img(lienzo, "Suelo", null, new Vector2(0f, y), new Vector2(Ancho - 70f, 3f), new Color(ColCobre.r, ColCobre.g, ColCobre.b, 0.5f));
    }

    void ConstruirReserva()
    {
        titulo.text = "RESERVA DE ENERGÍA";
        leyenda.text = "Cerca de la batería, Nilo se recarga. Lejos, cada casilla que camina gasta un segmento.";
        Suelo(-62f);

        aro = Img(lienzo, "Zona", Formas.Anillo(), new Vector2(-165f, -40f), new Vector2(150f, 150f), new Color(ColAmbar.r, ColAmbar.g, ColAmbar.b, 0.55f));
        bateria = Bateria(new Vector2(-165f, -36f));
        Etiqueta("recarga", new Vector2(-165f, 52f), ColAmbar);

        nilo = new Nilo(lienzo, new Vector2(-110f, -62f));
        barra = BarraSegmentos(new Vector2(0f, 52f), 10);
    }

    void ConstruirApagado()
    {
        titulo.text = "SIN ENERGÍA";
        leyenda.text = "Si la reserva llega a cero lejos de la batería, Nilo se apaga y el patio vuelve a empezar.";
        Suelo(-62f);

        bateria = Bateria(new Vector2(-190f, -36f));
        aro = Img(lienzo, "Zona", Formas.Anillo(), new Vector2(-190f, -40f), new Vector2(110f, 110f), new Color(ColAmbar.r, ColAmbar.g, ColAmbar.b, 0.35f));
        nilo = new Nilo(lienzo, new Vector2(70f, -62f));
        barra = BarraSegmentos(new Vector2(70f, 52f), 10);
    }

    void ConstruirCajas()
    {
        titulo.text = "CAJAS Y PLACAS";
        leyenda.text = "Camina contra una caja para empujarla. Sobre la placa, mantiene abierta la compuerta.";

        // Cuatro casillas, la placa en la tercera y la compuerta al final.
        for (int i = 0; i < 4; i++)
            Img(lienzo, "Casilla", Formas.RedondeadoContorno(), new Vector2(-180f + i * 82f, -40f), new Vector2(76f, 76f),
                new Color(ColCobre.r, ColCobre.g, ColCobre.b, 0.35f)).type = Image.Type.Sliced;

        placa = Img(lienzo, "Placa", Formas.Redondeado(), new Vector2(-16f, -62f), new Vector2(64f, 12f), ColCobre);
        placa.type = Image.Type.Sliced;
        cable = Img(lienzo, "Cable", null, new Vector2(95f, -66f), new Vector2(150f, 4f), ColCobre);

        // Compuerta: dos postes y barrotes que se hunden.
        Img(lienzo, "Poste", null, new Vector2(150f, -20f), new Vector2(8f, 96f), ColCobre);
        Img(lienzo, "Poste", null, new Vector2(214f, -20f), new Vector2(8f, 96f), ColCobre);
        GameObject g = new GameObject("Barrotes");
        g.transform.SetParent(lienzo, false);
        barrotesRt = g.AddComponent<RectTransform>();
        barrotesRt.sizeDelta = new Vector2(60f, 90f);
        barrotesRt.anchoredPosition = new Vector2(182f, -20f);
        RectMask2D mascara = g.AddComponent<RectMask2D>();
        for (int i = 0; i < 4; i++)
            Img(barrotesRt, "Barrote", null, new Vector2(-22f + i * 15f, 0f), new Vector2(6f, 88f), ColTurquesa);

        caja = Img(lienzo, "Caja", Formas.Redondeado(), new Vector2(-98f, -26f), new Vector2(66f, 66f), ColMadera);
        caja.type = Image.Type.Sliced;
        Img(caja.rectTransform, "Aspa", null, Vector2.zero, new Vector2(80f, 7f), new Color(0.35f, 0.22f, 0.12f)).rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Img(caja.rectTransform, "Aspa", null, Vector2.zero, new Vector2(80f, 7f), new Color(0.35f, 0.22f, 0.12f)).rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);

        nilo = new Nilo(lienzo, new Vector2(-180f, -62f));
    }

    Image Bateria(Vector2 pos)
    {
        Image cuerpo = Img(lienzo, "Bateria", Formas.Redondeado(), pos, new Vector2(38f, 58f), ColAmbar);
        cuerpo.type = Image.Type.Sliced;
        Img(cuerpo.rectTransform, "Banda", null, new Vector2(0f, -4f), new Vector2(38f, 12f), new Color(1f, 0.95f, 0.75f));
        Img(cuerpo.rectTransform, "Borne", null, new Vector2(0f, 33f), new Vector2(14f, 8f), ColCobre);
        return cuerpo;
    }

    void Etiqueta(string texto, Vector2 pos, Color color)
    {
        Text t = RelevoUI.Texto(lienzo, "Etiqueta", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(160f, 22f), 15, TextAnchor.MiddleCenter, color);
        t.font = RelevoUI.Fuente();
        t.text = texto;
    }

    RectTransform BarraSegmentos(Vector2 pos, int n)
    {
        GameObject go = new GameObject("Barra");
        go.transform.SetParent(lienzo, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(n * 14f, 16f);
        segmentos = new Image[n];
        for (int i = 0; i < n; i++)
        {
            segmentos[i] = Img(rt, "Segmento", Formas.Redondeado(), new Vector2(-n * 7f + 7f + i * 14f, 0f), new Vector2(11f, 16f), ColTurquesa);
            segmentos[i].type = Image.Type.Sliced;
        }
        return rt;
    }

    void PintarBarra(float fraccion, float tiempo)
    {
        if (segmentos == null) return;
        int llenos = Mathf.CeilToInt(fraccion * segmentos.Length - 0.001f);
        Color c = fraccion > 0.5f ? ColTurquesa : (fraccion > 0.25f ? ColAmbar : ColCoral);
        bool parpadeo = fraccion <= 0.25f && Mathf.Sin(tiempo * 14f) < 0f;
        for (int i = 0; i < segmentos.Length; i++)
            segmentos[i].color = i < llenos ? (parpadeo ? c * 0.55f : c) : new Color(0.2f, 0.25f, 0.28f, 0.8f);
    }

    // ------------------------------------------------------------------ animación

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        reloj += dt;

        // Aparición propia (la tarjeta cambia de sitio en cada mensaje).
        if (aparicion < 1f)
        {
            aparicion = Mathf.Min(1f, aparicion + dt / 0.25f);
            float e = 1f - (1f - aparicion) * (1f - aparicion);
            grupo.alpha = e;
            raiz.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, e);
        }

        switch (tipo)
        {
            case Tipo.Reserva: AnimarReserva(); break;
            case Tipo.Apagado: AnimarApagado(); break;
            case Tipo.Cajas: AnimarCajas(); break;
        }

        for (int i = huellas.Count - 1; i >= 0; i--)
        {
            Color c = huellas[i].color;
            c.a -= dt * 0.35f;
            if (c.a <= 0f) { Destroy(huellas[i].gameObject); huellas.RemoveAt(i); }
            else huellas[i].color = c;
        }

        if (aro != null) aro.rectTransform.localRotation = Quaternion.Euler(0f, 0f, reloj * 20f);
    }

    void AnimarReserva()
    {
        // Ciclo de 7 s: sale de la zona, camina hasta el borde, vuelve y se recarga.
        const float ciclo = 7f;
        float t = reloj % ciclo;
        float x;
        float fraccion;
        const float xZona = -100f, xLejos = 190f;

        if (t < 3f)
        {
            float s = Suave(t / 3f);
            x = Mathf.Lerp(-120f, xLejos, s);
        }
        else if (t < 3.6f) x = xLejos;
        else if (t < 6.2f) x = Mathf.Lerp(xLejos, -120f, Suave((t - 3.6f) / 2.6f));
        else x = -120f;

        // Gasto: un segmento cada 29 px fuera de la zona, ida y vuelta.
        float ida = Mathf.Max(0f, Mathf.Min(x, xLejos) - xZona);
        float recorrido = t < 3.6f ? ida : (xLejos - xZona) + (xLejos - Mathf.Max(x, xZona));
        fraccion = Mathf.Clamp01(1f - recorrido / 29f / 10f * 0.5f);
        if (t >= 6.2f) fraccion = Mathf.Clamp01(Mathf.Lerp(fraccion, 1f, (t - 6.2f) / 0.6f));
        if (t < 3.6f && x <= xZona) fraccion = 1f;

        bool caminando = (t < 3f || (t > 3.6f && t < 6.2f));
        nilo.Colocar(new Vector2(x, -62f), caminando ? reloj : 0f, t > 3.6f && t < 6.2f);
        nilo.Antena(fraccion > 0.5f ? ColTurquesa : (fraccion > 0.25f ? ColAmbar : ColCoral), 1f);
        barra.anchoredPosition = new Vector2(x, 52f);
        PintarBarra(fraccion, reloj);

        if (caminando && x > xZona && reloj - ultimaHuella > 0.22f)
        {
            ultimaHuella = reloj;
            Image h = Img(lienzo, "Huella", Formas.Circulo(), new Vector2(x, -70f), new Vector2(8f, 5f), new Color(ColCoral.r, ColCoral.g, ColCoral.b, 0.8f));
            huellas.Add(h);
        }
    }

    void AnimarApagado()
    {
        // Ciclo de 4 s: parpadeo rojo, se apagan los ojos, cae la cabeza, pausa y se reinicia.
        const float ciclo = 4.2f;
        float t = reloj % ciclo;
        PintarBarra(0f, reloj);

        Color antena;
        float luz;
        if (t < 0.9f)
        {
            bool on = Mathf.Repeat(t, 0.3f) < 0.15f;
            antena = ColRojo;
            luz = on ? 1f : 0.25f;
            nilo.Ojos(on ? 1f : 0.3f);
            nilo.Caida(0f);
        }
        else if (t < 2f)
        {
            float s = Suave((t - 0.9f) / 1.1f);
            antena = Color.Lerp(ColRojo, new Color(0.3f, 0.05f, 0.04f), s);
            luz = 1f - s * 0.8f;
            nilo.Ojos(1f - s);
            nilo.Caida(s);
        }
        else
        {
            antena = new Color(0.3f, 0.05f, 0.04f);
            luz = 0.2f;
            nilo.Ojos(0f);
            nilo.Caida(1f);
        }
        nilo.Antena(antena, luz);
        nilo.Colocar(new Vector2(70f, -62f), 0f, false);
    }

    void AnimarCajas()
    {
        // Ciclo de 6 s: dos empujones hasta la placa, se enciende el cable y baja la compuerta.
        const float ciclo = 6f;
        float t = reloj % ciclo;
        const float paso = 82f;
        float avance;                 // casillas avanzadas por la caja (0..2)

        if (t < 0.6f) avance = 0f;
        else if (t < 1.4f) avance = Suave((t - 0.6f) / 0.8f);
        else if (t < 1.8f) avance = 1f;
        else if (t < 2.6f) avance = 1f + Suave((t - 1.8f) / 0.8f);
        else avance = 2f;

        float xCaja = -180f + paso * (1f + avance);
        caja.rectTransform.anchoredPosition = new Vector2(xCaja, -26f + (Mathf.Repeat(avance, 1f) > 0.05f && avance < 2f ? Mathf.Sin(avance * Mathf.PI) * 2f : 0f));
        bool empujando = (t > 0.5f && t < 1.4f) || (t > 1.7f && t < 2.6f);
        nilo.Colocar(new Vector2(xCaja - 66f, -62f), empujando ? reloj : 0f, false);
        nilo.Antena(ColTurquesa, 1f);

        bool encendida = avance >= 2f;
        Color estado = encendida ? ColTurquesa : ColCobre;
        placa.color = estado;
        placa.rectTransform.anchoredPosition = new Vector2(-16f, encendida ? -64f : -62f);
        cable.color = encendida ? ColTurquesa : new Color(ColCobre.r, ColCobre.g, ColCobre.b, 0.6f);

        float bajada = encendida ? Suave((t - 2.6f) / 0.6f) : 0f;
        if (t > 5.4f) bajada = 1f - Suave((t - 5.4f) / 0.5f);
        if (barrotesRt != null)
            foreach (Transform b in barrotesRt) ((RectTransform)b).anchoredPosition = new Vector2(((RectTransform)b).anchoredPosition.x, -86f * bajada);
    }

    static float Suave(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    // ------------------------------------------------------------------ Nilo dibujado

    sealed class Nilo
    {
        readonly RectTransform raiz, cabeza;
        readonly Image antenaPunta;
        readonly Image[] ojos = new Image[2];
        const float Escala = 0.8f;

        public Nilo(RectTransform padre, Vector2 pie)
        {
            GameObject go = new GameObject("Nilo");
            go.transform.SetParent(padre, false);
            raiz = go.AddComponent<RectTransform>();
            raiz.sizeDelta = new Vector2(60f, 0f);
            raiz.pivot = new Vector2(0.5f, 0f);
            raiz.anchoredPosition = pie;

            Img(raiz, "Pierna", null, new Vector2(-9f, 9f), new Vector2(8f, 18f), ColOscuro);
            Img(raiz, "Pierna", null, new Vector2(9f, 9f), new Vector2(8f, 18f), ColOscuro);
            Image cuerpo = Img(raiz, "Cuerpo", Formas.Redondeado(), new Vector2(0f, 36f), new Vector2(40f, 38f), ColCuerpo);
            cuerpo.type = Image.Type.Sliced;
            Img(raiz, "Bufanda", null, new Vector2(0f, 54f), new Vector2(42f, 7f), ColCoral);

            GameObject c = new GameObject("Cabeza");
            c.transform.SetParent(raiz, false);
            cabeza = c.AddComponent<RectTransform>();
            cabeza.anchoredPosition = new Vector2(0f, 58f);
            cabeza.pivot = new Vector2(0.5f, 0f);
            cabeza.sizeDelta = new Vector2(50f, 40f);
            Image casco = Img(cabeza, "Casco", Formas.Redondeado(), new Vector2(0f, 20f), new Vector2(50f, 40f), ColCuerpo);
            casco.type = Image.Type.Sliced;
            Img(cabeza, "Visor", null, new Vector2(0f, 18f), new Vector2(46f, 18f), ColOscuro);
            ojos[0] = Img(cabeza, "Ojo", Formas.Circulo(), new Vector2(-11f, 18f), new Vector2(12f, 12f), ColTurquesa);
            ojos[1] = Img(cabeza, "Ojo", Formas.Circulo(), new Vector2(11f, 18f), new Vector2(12f, 12f), ColTurquesa);
            Img(cabeza, "Antena", null, new Vector2(10f, 48f), new Vector2(3f, 18f), ColCobre);
            antenaPunta = Img(cabeza, "Punta", Formas.Circulo(), new Vector2(10f, 58f), new Vector2(11f, 11f), ColTurquesa);
        }

        public void Colocar(Vector2 pie, float fase, bool mirarIzquierda)
        {
            float rebote = fase > 0f ? Mathf.Abs(Mathf.Sin(fase * 9f)) * 3f : 0f;
            raiz.anchoredPosition = pie + Vector2.up * rebote;
            raiz.localScale = new Vector3(mirarIzquierda ? -Escala : Escala, Escala, 1f);
        }

        public void Antena(Color c, float luz)
        {
            antenaPunta.color = new Color(c.r * (0.4f + 0.6f * luz), c.g * (0.4f + 0.6f * luz), c.b * (0.4f + 0.6f * luz), 1f);
        }

        public void Ojos(float luz)
        {
            Color c = Color.Lerp(new Color(0.08f, 0.1f, 0.12f), ColTurquesa, luz);
            ojos[0].color = c;
            ojos[1].color = c;
        }

        public void Caida(float s)
        {
            cabeza.localRotation = Quaternion.Euler(0f, 0f, -24f * s);
            cabeza.anchoredPosition = new Vector2(4f * s, 58f - 9f * s);
            raiz.localScale = new Vector3(raiz.localScale.x, Escala * (1f - 0.04f * s), 1f);
        }
    }

    // ------------------------------------------------------------------ utilidades

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

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static class Formas
    {
        static Sprite circulo, anillo, redondeado, contorno;

        static Texture2D Nueva(int lado)
        {
            Texture2D t = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.DontSave;
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        static Sprite Hacer(int lado, System.Func<float, float, float> alfa, Vector4 bordes)
        {
            Texture2D t = Nueva(lado);
            Color32[] px = new Color32[lado * lado];
            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                    px[y * lado + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alfa(x + 0.5f, y + 0.5f)) * 255f));
            t.SetPixels32(px);
            t.Apply();
            Sprite s = Sprite.Create(t, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, bordes);
            s.hideFlags = HideFlags.DontSave;
            return s;
        }

        public static Sprite Circulo()
        {
            if (circulo == null) circulo = Hacer(64, (x, y) => Mathf.Clamp01(31f - new Vector2(x - 32f, y - 32f).magnitude), Vector4.zero);
            return circulo;
        }

        /// <summary>Anillo discontinuo: la zona de recarga.</summary>
        public static Sprite Anillo()
        {
            if (anillo == null) anillo = Hacer(128, (x, y) =>
            {
                Vector2 d = new Vector2(x - 64f, y - 64f);
                float r = d.magnitude;
                float banda = Mathf.Clamp01(1.5f - Mathf.Abs(r - 60f));
                float a = Mathf.Atan2(d.y, d.x);
                float trazo = Mathf.Sin(a * 14f) > -0.2f ? 1f : 0f;
                return banda * trazo;
            }, Vector4.zero);
            return anillo;
        }

        static float Caja(float x, float y, float lado, float radio)
        {
            float cx = Mathf.Clamp(x, radio, lado - radio);
            float cy = Mathf.Clamp(y, radio, lado - radio);
            return new Vector2(x - cx, y - cy).magnitude - radio;
        }

        public static Sprite Redondeado()
        {
            if (redondeado == null) redondeado = Hacer(48, (x, y) => Mathf.Clamp01(0.5f - Caja(x, y, 48f, 10f)), new Vector4(14f, 14f, 14f, 14f));
            return redondeado;
        }

        public static Sprite RedondeadoContorno()
        {
            if (contorno == null) contorno = Hacer(48, (x, y) =>
            {
                float d = Caja(x, y, 48f, 10f);
                return Mathf.Clamp01(0.5f - d) - Mathf.Clamp01(0.5f - (d + 2f));
            }, new Vector4(14f, 14f, 14f, 14f));
            return contorno;
        }
    }
}
