using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Identidad visual de la interfaz de RELEVO: el cuaderno técnico de mantenimiento de una ciudad
/// de canales. Reúne la paleta de la UI, las formas y los constructores de UGUI.
///
/// Todo se genera por código y no depende de ningún asset externo: los iconos (hexágonos, hojas,
/// cápsulas) son texturas de un solo canal alfa creadas en memoria y tintadas después con el color
/// de cada Image, de modo que una misma forma sirve para todos los estados.
///
/// Los bordes y marcos se hacen con franjas ancladas a los lados del padre (anchorMin/anchorMax a
/// 0 y 1), no con posiciones absolutas: así el marco sigue al panel en cualquier resolución.
/// </summary>
public static class RelevoUI
{
    // ------------------------------------------------------------------ paleta

    /// <summary>Azul verdoso muy oscuro y semitransparente: el fondo de todos los paneles.</summary>
    public static readonly Color Fondo = new Color(0.039f, 0.110f, 0.122f, 0.88f);
    /// <summary>Un punto más oscuro, para el fondo de las tarjetas y de las filas de menú.</summary>
    public static readonly Color FondoHondo = new Color(0.024f, 0.067f, 0.075f, 0.94f);
    /// <summary>Realce de la tarjeta o de la fila que tiene el foco.</summary>
    public static readonly Color FondoAlto = new Color(0.078f, 0.196f, 0.208f, 0.92f);
    /// <summary>Fondo de la opción seleccionada en un menú.</summary>
    public static readonly Color FondoSeleccion = new Color(0.129f, 0.290f, 0.286f, 0.95f);
    /// <summary>Velo que cubre el escenario cuando hay un menú abierto.</summary>
    public static readonly Color Velo = new Color(0.008f, 0.027f, 0.033f, 0.86f);

    /// <summary>Cobre: el color del metal de Liria. Es el de los bordes y los filetes finos.</summary>
    public static readonly Color Cobre = new Color(0.788f, 0.561f, 0.333f, 1f);
    public static readonly Color CobreTenue = new Color(0.788f, 0.561f, 0.333f, 0.40f);

    /// <summary>Ámbar: batería, energía, selección y objetivos importantes.</summary>
    public static readonly Color Ambar = RelevoPalette.ColorDe(ClaveMaterial.Ambar);
    public static readonly Color AmbarSuave = RelevoPalette.ColorDe(ClaveMaterial.AmbarSuave);
    /// <summary>Turquesa: pasarelas y mecanismos manuales.</summary>
    public static readonly Color Turquesa = RelevoPalette.ColorDe(ClaveMaterial.Turquesa);
    /// <summary>Coral: solo advertencias y acciones inválidas.</summary>
    public static readonly Color Coral = RelevoPalette.ColorDe(ClaveMaterial.Coral);
    /// <summary>Verde de los ojos de Maia, para identificar sus diálogos.</summary>
    public static readonly Color Verde = RelevoPalette.ColorDe(ClaveMaterial.MaiaOjos);

    /// <summary>Tinta de lectura: la piedra crema de Liria, cálida y legible sobre el panel oscuro.</summary>
    public static readonly Color Tinta = RelevoPalette.ColorDe(ClaveMaterial.PiedraCrema);

    public static Color ConAlfa(Color color, float alfa)
    {
        return new Color(color.r, color.g, color.b, alfa);
    }

    /// <summary>Aclara un color hacia el blanco manteniendo su alfa.</summary>
    public static Color Aclarar(Color color, float cantidad)
    {
        return new Color(
            Mathf.Lerp(color.r, 1f, cantidad),
            Mathf.Lerp(color.g, 1f, cantidad),
            Mathf.Lerp(color.b, 1f, cantidad),
            color.a);
    }

    // ------------------------------------------------------------------ tipografía

    static Font fuente;

    /// <summary>
    /// Fuente del sistema: el proyecto no incluye ninguna fuente propia. LegacyRuntime.ttf es la
    /// que trae Unity y cubre el español completo (acentos y eñes).
    /// </summary>
    public static Font Fuente()
    {
        if (fuente != null) return fuente;

        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (fuente == null) fuente = Font.CreateDynamicFontFromOSFont("Arial", 16);
        return fuente;
    }

    // ------------------------------------------------------------------ formas

    /// <summary>
    /// Formas de la interfaz, creadas una vez y compartidas. Todas son blancas con alfa: el color
    /// lo pone la Image que las usa.
    /// </summary>
    public static class Formas
    {
        const int Lado = 96;          // resolución del hexágono y de la hoja
        const int CapsulaAncho = 48;
        const int CapsulaAlto = 30;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Cache(string clave, System.Func<Sprite> crear)
        {
            Sprite sprite;
            if (cache.TryGetValue(clave, out sprite) && sprite != null) return sprite;

            sprite = crear();
            cache[clave] = sprite;
            return sprite;
        }

        public static Sprite HexagonoRelleno()
        {
            return Cache("hexagono_relleno", delegate
            {
                Texture2D t = PintarNormalizado(Lado, Lado, delegate (float u, float v)
                {
                    return Cobertura(Hexagono(u, v, 0.46f), 0.035f);
                });
                return ASprite(t, Vector4.zero);
            });
        }

        public static Sprite HexagonoAnillo()
        {
            return Cache("hexagono_anillo", delegate
            {
                Texture2D t = PintarNormalizado(Lado, Lado, delegate (float u, float v)
                {
                    return Cobertura(Mathf.Abs(Hexagono(u, v, 0.44f)) - 0.055f, 0.035f);
                });
                return ASprite(t, Vector4.zero);
            });
        }

        public static Sprite Hoja()
        {
            return Cache("hoja", delegate
            {
                Texture2D t = PintarNormalizado(Lado, Lado, delegate (float u, float v)
                {
                    // Lente de dos círculos: una hoja apuntada arriba y abajo.
                    float a = 0.52f, r = 0.98f;
                    float d1 = new Vector2(u + a, v).magnitude - r;
                    float d2 = new Vector2(u - a, v).magnitude - r;
                    float d = Mathf.Max(d1, d2);
                    // Nervio central: se vacía un triángulo fino para que se lea como hoja.
                    float nervio = Mathf.Abs(u) * 2.2f - 0.06f + Mathf.Abs(v) * 0.30f;
                    return Mathf.Min(Cobertura(d, 0.035f), 1f - Cobertura(nervio, 0.03f));
                });
                return ASprite(t, Vector4.zero);
            });
        }

        /// <summary>
        /// Cápsula de las teclas. Se crea con bordes (9-slice) para poder estirarla en horizontal
        /// sin deformar los extremos redondos.
        /// </summary>
        public static Sprite Capsula()
        {
            return Cache("capsula", delegate
            {
                float radio = CapsulaAlto * 0.5f;
                Texture2D t = PintarPixeles(CapsulaAncho, CapsulaAlto, delegate (float x, float y)
                {
                    float cx = Mathf.Clamp(x, radio, CapsulaAncho - radio);
                    float medio = CapsulaAlto * 0.5f;
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - medio) * (y - medio)) - radio;
                    return Cobertura(d, 1.4f);
                });
                return ASprite(t, new Vector4(radio, 0f, radio, 0f));
            });
        }

        /// <summary>Distancia con signo a un hexágono regular de radio circunscrito 'r' (punta arriba).</summary>
        static float Hexagono(float u, float v, float r)
        {
            Vector2 k = new Vector2(-0.8660254f, 0.5f);
            float px = Mathf.Abs(u);
            float py = Mathf.Abs(v);

            float dot = k.x * px + k.y * py;
            if (dot < 0f)
            {
                px -= 2f * dot * k.x;
                py -= 2f * dot * k.y;
            }

            px -= Mathf.Clamp(px, -k.x * r, k.x * r);
            py -= r;
            return Mathf.Sqrt(px * px + py * py) * (py < 0f ? -1f : 1f);
        }

        /// <summary>Alfa a partir de una distancia con signo, con un borde suave de 'grosor'.</summary>
        static float Cobertura(float distancia, float grosor)
        {
            return Mathf.Clamp01(0.5f - distancia / grosor);
        }

        static Texture2D PintarNormalizado(int ancho, int alto, System.Func<float, float, float> cobertura)
        {
            return Pintar(ancho, alto, delegate (float x, float y)
            {
                // Coordenadas centradas e isotrópicas: 1 es el borde largo.
                float u = ((x + 0.5f) / ancho * 2f - 1f) * ((float)ancho / Mathf.Max(ancho, alto));
                float v = ((y + 0.5f) / alto * 2f - 1f) * ((float)alto / Mathf.Max(ancho, alto));
                return cobertura(u, v);
            });
        }

        static Texture2D PintarPixeles(int ancho, int alto, System.Func<float, float, float> cobertura)
        {
            return Pintar(ancho, alto, cobertura);
        }

        static Texture2D Pintar(int ancho, int alto, System.Func<float, float, float> cobertura)
        {
            Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            textura.hideFlags = HideFlags.DontSave;
            textura.filterMode = FilterMode.Bilinear;
            textura.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixeles = new Color32[ancho * alto];
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    float alfa = Mathf.Clamp01(cobertura(x, y));
                    pixeles[y * ancho + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alfa * 255f));
                }
            }

            textura.SetPixels32(pixeles);
            textura.Apply();
            return textura;
        }

        static Sprite ASprite(Texture2D textura, Vector4 bordes)
        {
            Sprite sprite = Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, bordes);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }

    // ------------------------------------------------------------------ constructores

    /// <summary>Caja anclada a una esquina o a un punto del padre. Sin posición absoluta de pantalla.</summary>
    public static GameObject Panel(Transform padre, string nombre, Vector2 ancla, Vector2 pivote,
        Vector2 posicion, Vector2 tamano, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);

        Image imagen = go.AddComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;

        Anclar(imagen.rectTransform, ancla, pivote, posicion, tamano);
        return go;
    }

    public static Image Franja(Transform padre, string nombre, Vector2 ancla, Vector2 pivote,
        Vector2 posicion, Vector2 tamano, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);

        Image imagen = go.AddComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;

        Anclar(imagen.rectTransform, ancla, pivote, posicion, tamano);
        return imagen;
    }

    /// <summary>
    /// Marco fino de cobre que sigue al tamaño del padre: cuatro franjas ancladas a sus cuatro
    /// lados, de modo que el panel se puede redimensionar sin tocar el marco.
    /// </summary>
    public static void Marco(Transform padre, Color color, float grosor)
    {
        Marco(padre, color, grosor, null);
    }

    /// <summary>
    /// Igual que el marco normal, pero devolviendo las cuatro franjas para poder recolorearlas
    /// después (es lo que hace un menú al mover el foco de una opción a otra).
    /// </summary>
    public static void Marco(Transform padre, Color color, float grosor, List<Image> registro)
    {
        Registra(FranjaBorde(padre, "Marco arriba", new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, grosor), color), registro);
        Registra(FranjaBorde(padre, "Marco abajo", new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, grosor), color), registro);
        Registra(FranjaBorde(padre, "Marco izquierda", new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(grosor, 0f), color), registro);
        Registra(FranjaBorde(padre, "Marco derecha", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(grosor, 0f), color), registro);
    }

    static void Registra(Image franja, List<Image> registro)
    {
        if (registro != null) registro.Add(franja);
    }

    static Image FranjaBorde(Transform padre, string nombre, Vector2 extremo, Vector2 pivote, Vector2 grosor, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);

        Image imagen = go.AddComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;

        RectTransform rt = imagen.rectTransform;
        if (Mathf.Approximately(grosor.x, 0f))
        {
            // Franja horizontal: se estira con el ancho del padre.
            rt.anchorMin = new Vector2(0f, extremo.y);
            rt.anchorMax = new Vector2(1f, extremo.y);
            rt.sizeDelta = grosor;
        }
        else
        {
            // Franja vertical: se estira con el alto del padre.
            rt.anchorMin = new Vector2(extremo.x, 0f);
            rt.anchorMax = new Vector2(extremo.x, 1f);
            rt.sizeDelta = grosor;
        }

        rt.pivot = pivote;
        rt.anchoredPosition = Vector2.zero;
        return imagen;
    }

    /// <summary>Cuatro esquinas en ángulo: el remate de las tarjetas del cuaderno.</summary>
    public static void Esquinas(Transform padre, float largo, float grosor, Color color)
    {
        Esquina(padre, "Esquina SO", new Vector2(0f, 1f), new Vector2(0f, 1f), largo, grosor, color);
        Esquina(padre, "Esquina SE", new Vector2(1f, 1f), new Vector2(1f, 1f), largo, grosor, color);
        Esquina(padre, "Esquina NO", new Vector2(0f, 0f), new Vector2(0f, 0f), largo, grosor, color);
        Esquina(padre, "Esquina NE", new Vector2(1f, 0f), new Vector2(1f, 0f), largo, grosor, color);
    }

    static void Esquina(Transform padre, string nombre, Vector2 ancla, Vector2 pivote, float largo, float grosor, Color color)
    {
        // El grupo es un rectángulo de tamaño cero apoyado en la esquina; con el pivote en la
        // propia esquina, los dos tramos crecen hacia dentro del panel sin ningún desplazamiento.
        GameObject grupo = new GameObject(nombre);
        grupo.transform.SetParent(padre, false);
        RectTransform rt = grupo.AddComponent<RectTransform>();
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        Franja(grupo.transform, "Horizontal", ancla, pivote, Vector2.zero, new Vector2(largo, grosor), color);
        Franja(grupo.transform, "Vertical", ancla, pivote, Vector2.zero, new Vector2(grosor, largo), color);
    }

    public static Text Texto(Transform padre, string nombre, Vector2 ancla, Vector2 pivote,
        Vector2 posicion, Vector2 tamano, int tamanoFuente, TextAnchor alineacion, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);

        Text texto = go.AddComponent<Text>();
        texto.font = Fuente();
        texto.fontSize = tamanoFuente;
        texto.alignment = alineacion;
        texto.color = color;
        texto.raycastTarget = false;
        texto.horizontalOverflow = HorizontalWrapMode.Wrap;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.supportRichText = true;
        texto.lineSpacing = 1.12f;

        Anclar(texto.rectTransform, ancla, pivote, posicion, tamano);
        return texto;
    }

    /// <summary>Conector hexagonal: la forma que identifica energía y selección en Liria.</summary>
    public static Image Hexagono(Transform padre, string nombre, Vector2 ancla, Vector2 pivote,
        Vector2 posicion, Vector2 tamano, Color color, bool relleno)
    {
        Image imagen = Forma(padre, nombre, ancla, pivote, posicion, tamano,
            relleno ? Formas.HexagonoRelleno() : Formas.HexagonoAnillo(), color);
        return imagen;
    }

    /// <summary>Cápsula: la tecla y el recuerdo comparten esta forma.</summary>
    public static Image Capsula(Transform padre, string nombre, Vector2 ancla, Vector2 pivote,
        Vector2 posicion, Vector2 tamano, Color color)
    {
        Image imagen = Forma(padre, nombre, ancla, pivote, posicion, tamano, Formas.Capsula(), color);
        imagen.type = Image.Type.Sliced;
        return imagen;
    }

    public static Image Hoja(Transform padre, string nombre, Vector2 ancla, Vector2 pivote,
        Vector2 posicion, Vector2 tamano, Color color)
    {
        return Forma(padre, nombre, ancla, pivote, posicion, tamano, Formas.Hoja(), color);
    }

    static Image Forma(Transform padre, string nombre, Vector2 ancla, Vector2 pivote,
        Vector2 posicion, Vector2 tamano, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);

        Image imagen = go.AddComponent<Image>();
        imagen.sprite = sprite;
        imagen.color = color;
        imagen.raycastTarget = false;
        imagen.preserveAspect = false;

        Anclar(imagen.rectTransform, ancla, pivote, posicion, tamano);
        return imagen;
    }

    /// <summary>
    /// Tramo de cable de cobre entre dos puntos, en coordenadas relativas al centro del padre, con
    /// un remache hexagonal en cada punta.
    /// </summary>
    public static void Cable(Transform padre, string nombre, Vector2 desde, Vector2 hasta, float grosor, Color color)
    {
        GameObject grupo = new GameObject(nombre);
        grupo.transform.SetParent(padre, false);

        RectTransform rt = grupo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = (desde + hasta) * 0.5f;
        rt.sizeDelta = Vector2.zero;

        Vector2 delta = hasta - desde;
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        Franja(grupo.transform, "Tramo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(delta.magnitude, grosor), color);

        Vector2 radial = delta.normalized;
        float mitad = delta.magnitude * 0.5f;
        Remache(grupo.transform, "Remache A", -radial * mitad, grosor, color);
        Remache(grupo.transform, "Remache B", radial * mitad, grosor, color);
    }

    static void Remache(Transform padre, string nombre, Vector2 posicion, float grosor, Color color)
    {
        // Los remaches giran con el cable, así que se contra-rotan para quedar siempre verticales.
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);

        Image imagen = go.AddComponent<Image>();
        imagen.sprite = Formas.HexagonoRelleno();
        imagen.color = color;
        imagen.raycastTarget = false;

        RectTransform rt = imagen.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = new Vector2(grosor * 2.6f, grosor * 2.6f);
        rt.localRotation = Quaternion.Euler(0f, 0f, -padre.localRotation.eulerAngles.z);
    }

    /// <summary>
    /// Tecla en cápsula: la forma y el texto van juntos y se devuelve el texto para poder recolorearlo.
    /// </summary>
    public static Text Tecla(Transform padre, string etiqueta, Vector2 ancla, Vector2 pivote,
        Vector2 posicion, float ancho, string nombre, Color colorCapsula, Color colorTexto)
    {
        float alto = 30f;

        Capsula(padre, nombre + " capsula", ancla, pivote, posicion,
            new Vector2(ancho, alto), colorCapsula);

        return Texto(padre, nombre, ancla, pivote, posicion,
            new Vector2(ancho, alto), 16, TextAnchor.MiddleCenter, colorTexto);
    }

    static void Anclar(RectTransform rt, Vector2 ancla, Vector2 pivote, Vector2 posicion, Vector2 tamano)
    {
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;
    }
}
