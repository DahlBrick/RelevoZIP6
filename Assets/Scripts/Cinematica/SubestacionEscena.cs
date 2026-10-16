using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// La Subestación inundada donde despierta Nilo: segunda parte de la cinemática de apertura.
///
/// Un cuarto de máquinas en penumbra, con dos dedos de agua en el suelo, el sol de la mañana
/// entrando por una ventana alta (el apagón fue de noche; Nilo despierta de día, como empieza el
/// primer nivel), generadores y tuberías, la radio vieja en la pared y un panel con una
/// quemadura en forma de mano. Esa quemadura es una pista: es la mano de Nilo apagando la red.
///
/// Todo cuelga de un origen propio, lejos de la Liria nocturna, para que las dos partes no se
/// vean entre sí.
/// </summary>
public sealed class SubestacionEscena
{
    public static readonly Vector3 Origen = new Vector3(300f, 0f, 0f);
    const float Ancho = 12f, Fondo = 10f, Alto = 5f;

    public Transform Raiz { get; private set; }
    public Vector3 PosicionNilo { get { return Origen + new Vector3(-0.6f, 0f, -0.8f); } }
    public Vector3 Radio { get; private set; }
    public Vector3 FrenteRadio { get; private set; }
    public Vector3 Quemadura { get; private set; }
    /// <summary>El centro del haz de sol, entre la ventana y el suelo.</summary>
    public Vector3 Haz { get { return Origen + new Vector3(3.6f, 1.9f, -0.3f); } }
    public AudioSource Altavoz { get; private set; }

    readonly System.Func<string, GameObject> modelo;
    readonly KitRelevo kit;
    Material dialRadio;
    Light luzRadio;
    float encendidoRadio;
    readonly List<OndaAgua> ondas = new List<OndaAgua>();
    Material materialOnda;

    sealed class OndaAgua { public Transform t; public float inicio; public float escala; }

    SubestacionEscena(System.Func<string, GameObject> modelo, KitRelevo kit)
    {
        this.modelo = modelo;
        this.kit = kit;
    }

    public static SubestacionEscena Construir(Transform padre, System.Func<string, GameObject> modelo, KitRelevo kit)
    {
        SubestacionEscena s = new SubestacionEscena(modelo, kit);
        s.Levantar(padre);
        return s;
    }

    void Levantar(Transform padre)
    {
        Raiz = new GameObject("Subestacion").transform;
        Raiz.SetParent(padre, false);
        Raiz.position = Origen;

        Material muro = Liso(new Color(0.24f, 0.27f, 0.27f));
        Material suelo = Liso(new Color(0.16f, 0.17f, 0.17f), 0.55f);
        Caja("Suelo", new Vector3(0f, -0.25f, 0f), new Vector3(Ancho, 0.5f, Fondo), suelo);
        Caja("Techo", new Vector3(0f, Alto + 0.2f, 0f), new Vector3(Ancho, 0.4f, Fondo), muro);
        Caja("Muro norte", new Vector3(0f, Alto * 0.5f, Fondo * 0.5f), new Vector3(Ancho, Alto, 0.4f), muro);
        Caja("Muro sur", new Vector3(0f, Alto * 0.5f, -Fondo * 0.5f), new Vector3(Ancho, Alto, 0.4f), muro);
        Caja("Muro oeste", new Vector3(-Ancho * 0.5f, Alto * 0.5f, 0f), new Vector3(0.4f, Alto, Fondo), muro);
        // El muro este se parte para dejar la ventana alta.
        Caja("Muro este bajo", new Vector3(Ancho * 0.5f, 1.6f, 0f), new Vector3(0.4f, 3.2f, Fondo), muro);
        Caja("Muro este alto", new Vector3(Ancho * 0.5f, 4.6f, 0f), new Vector3(0.4f, 0.8f, Fondo), muro);
        Caja("Muro este izq", new Vector3(Ancho * 0.5f, 3.7f, -2.9f), new Vector3(0.4f, 1.0f, Fondo * 0.5f - 0.4f), muro);
        Caja("Muro este der", new Vector3(Ancho * 0.5f, 3.7f, 2.9f), new Vector3(0.4f, 1.0f, Fondo * 0.5f - 0.4f), muro);

        // Zócalo de baldosas y franja de humedad: lo que dejó el agua al subir y bajar.
        Caja("Mancha de humedad", new Vector3(0f, 0.6f, Fondo * 0.5f - 0.21f), new Vector3(Ancho, 1.2f, 0.02f), Liso(new Color(0.15f, 0.18f, 0.17f)));

        Agua();
        Ventana();
        Maquinas();
        CrearRadio();
        PanelQuemado();
        Luces();
    }

    void Agua()
    {
        Material m = kit != null && kit.agua != null ? new Material(kit.agua) : Liso(new Color(0.05f, 0.08f, 0.1f));
        if (m.HasProperty("_ColorSomero"))
        {
            m.SetColor("_ColorSomero", new Color(0.1f, 0.14f, 0.16f));
            m.SetColor("_ColorProfundo", new Color(0.03f, 0.05f, 0.06f));
            m.SetColor("_ColorEspuma", new Color(0.3f, 0.35f, 0.38f));
            m.SetFloat("_Destellos", 1f);
            m.SetFloat("_Opacidad", 0.45f);
            m.SetFloat("_EscalaOndas", 1.2f);
            m.SetVector("_Velocidad", new Vector4(0.02f, 0.01f, 0f, 0f));
        }
        GameObject a = Caja("Agua", new Vector3(0f, 0.04f, 0f), new Vector3(Ancho - 0.4f, 0.06f, Fondo - 0.4f), m);
        a.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        materialOnda = new Material(kit != null && kit.particula != null ? kit.particula : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")));
        materialOnda.mainTexture = Anillo();
        if (materialOnda.HasProperty("_BaseMap")) materialOnda.SetTexture("_BaseMap", Anillo());
    }

    void Ventana()
    {
        // Cristal lleno de cielo de mañana, visto desde dentro, y una barra en medio.
        Material cielo = Emisivo(new Color(1f, 0.93f, 0.8f), 2.6f);
        GameObject v = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(v.GetComponent<Collider>());
        v.transform.SetParent(Raiz, false);
        v.transform.localPosition = new Vector3(Ancho * 0.5f + 0.1f, 3.7f, 0f);
        v.transform.localRotation = Quaternion.LookRotation(Vector3.left);
        v.transform.localScale = new Vector3(1.6f, 1f, 1f);
        v.GetComponent<Renderer>().sharedMaterial = cielo;
        Caja("Barrote", new Vector3(Ancho * 0.5f, 3.7f, 0f), new Vector3(0.45f, 1f, 0.08f), Liso(new Color(0.08f, 0.08f, 0.09f)));

        // Rayo de sol: un haz de polvo dorado, del cristal al suelo.
        Material haz = new Material(kit != null && kit.particula != null ? kit.particula : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")));
        if (haz.HasProperty("_BaseColor")) haz.SetColor("_BaseColor", new Color(1f, 0.86f, 0.6f, 0.11f));
        Vector3 desde = new Vector3(Ancho * 0.5f, 3.7f, 0f), hasta = new Vector3(1.2f, 0f, -0.6f);
        for (int i = 0; i < 3; i++)
        {
            GameObject h = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(h.GetComponent<Collider>());
            h.name = "Haz de sol";
            h.transform.SetParent(Raiz, false);
            h.transform.localPosition = (desde + hasta) * 0.5f;
            Vector3 eje = (hasta - desde).normalized;
            h.transform.localRotation = Quaternion.LookRotation(Vector3.Cross(eje, Vector3.forward), eje) * Quaternion.Euler(0f, i * 60f, 0f);
            h.transform.localScale = new Vector3(1.8f, (hasta - desde).magnitude * 1.1f, 1f);
            Renderer r = h.GetComponent<Renderer>();
            r.sharedMaterial = haz;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Polvo flotando en el haz.
        GameObject go = new GameObject("Polvo");
        go.transform.SetParent(Raiz, false);
        go.transform.localPosition = (desde + hasta) * 0.5f;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 9f;
        main.startSpeed = 0.03f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
        main.startColor = new Color(1f, 0.9f, 0.7f, 0.6f);
        main.maxParticles = 220;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.prewarm = true;
        var em = ps.emission; em.rateOverTime = 24f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(2.4f, 3.6f, 1.6f);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.04f; noise.frequency = 0.3f;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = kit != null ? kit.particula : null;
    }

    void Maquinas()
    {
        Color hierro = new Color(0.42f, 0.44f, 0.45f);
        Pieza("machine_generator", new Vector3(-4.6f, 0f, 2.6f), 90f, 2.6f, hierro);
        Pieza("machine_generator", new Vector3(-4.6f, 0f, -0.6f), 90f, 2.6f, hierro);
        Pieza("machine_barrelLarge", new Vector3(-4.3f, 0f, -3.6f), 0f, 1.9f, new Color(0.35f, 0.4f, 0.38f));
        Pieza("machine_barrel", new Vector3(3.9f, 0f, -3.8f), 30f, 1.1f, new Color(0.4f, 0.38f, 0.33f));
        Pieza("machine_barrel", new Vector3(4.6f, 0f, -3.1f), -10f, 1.1f, new Color(0.4f, 0.38f, 0.33f));
        // Tuberías a lo largo del techo.
        for (int i = 0; i < 2; i++)
        {
            GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(t.GetComponent<Collider>());
            t.transform.SetParent(Raiz, false);
            t.transform.localPosition = new Vector3(0f, 4.3f - i * 0.35f, Fondo * 0.5f - 0.5f - i * 0.25f);
            t.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            t.transform.localScale = new Vector3(0.18f, Ancho * 0.5f, 0.18f);
            t.GetComponent<Renderer>().sharedMaterial = Liso(new Color(0.3f, 0.24f, 0.2f), 0.5f);
        }
        // Cuadros de control, todos muertos, a lo largo del muro norte.
        for (int i = 0; i < 3; i++)
        {
            Vector3 c = new Vector3(-3.6f + i * 1.35f, 1.5f, Fondo * 0.5f - 0.3f);
            Caja("Cuadro", c, new Vector3(1.1f, 1.6f, 0.22f), Liso(new Color(0.26f, 0.3f, 0.31f)));
            for (int k = 0; k < 3; k++)
                Caja("Indicador apagado", c + new Vector3(-0.3f + k * 0.3f, 0.45f, -0.12f), new Vector3(0.12f, 0.12f, 0.03f), Liso(new Color(0.12f, 0.06f, 0.05f)));
            Caja("Palanca", c + new Vector3(0.25f, -0.2f, -0.15f), new Vector3(0.06f, 0.4f, 0.06f), Liso(new Color(0.15f, 0.15f, 0.16f)));
        }
    }

    void CrearRadio()
    {
        Vector3 c = new Vector3(1.6f, 1.5f, Fondo * 0.5f - 0.35f);
        Radio = Origen + c;
        FrenteRadio = Vector3.back;
        Caja("Repisa", c + new Vector3(0f, -0.3f, 0.02f), new Vector3(1.0f, 0.06f, 0.42f), Liso(new Color(0.25f, 0.18f, 0.12f)));
        // Maia: la misma radio que en los niveles (laleksic, CC0, en colores planos). El frente del
        // modelo mira a +Z; aquí tiene que mirar al cuarto (-Z).
        if (kit != null && kit.radio != null)
        {
            Bounds b = KitRelevo.Limites(kit.radio);
            float escala = 0.75f / Mathf.Max(0.01f, b.size.x);
            GameObject radio = KitRelevo.ColocarCentrado(kit.radio, Raiz, "Radio", c + new Vector3(0f, -0.27f, 0f), 180f, Vector3.one * escala);
            c.y = c.y - 0.27f + b.size.y * escala * 0.22f;   // a la altura del dial
            Radio = Origen + c;
        }
        else Caja("Radio", c, new Vector3(0.78f, 0.5f, 0.3f), Liso(new Color(0.38f, 0.24f, 0.15f), 0.45f));

        // Luz piloto: apagada hasta que la radio se enciende sola.
        dialRadio = Emisivo(new Color(1f, 0.55f, 0.18f), 0f);
        GameObject piloto = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(piloto.GetComponent<Collider>());
        piloto.name = "Piloto";
        piloto.transform.SetParent(Raiz, false);
        piloto.transform.localPosition = c + new Vector3(0.3f, 0.02f, -0.06f);
        piloto.transform.localScale = Vector3.one * 0.035f;
        piloto.GetComponent<Renderer>().sharedMaterial = dialRadio;

        luzRadio = new GameObject("Luz radio").AddComponent<Light>();
        luzRadio.transform.SetParent(Raiz, false);
        luzRadio.transform.localPosition = c + new Vector3(0.1f, 0.05f, -0.5f);
        luzRadio.type = LightType.Point;
        luzRadio.color = new Color(1f, 0.6f, 0.28f);
        luzRadio.range = 3.2f;
        luzRadio.intensity = 0f;
        luzRadio.shadows = LightShadows.None;

        Altavoz = new GameObject("Altavoz").AddComponent<AudioSource>();
        Altavoz.transform.SetParent(Raiz, false);
        Altavoz.transform.localPosition = c;
        Altavoz.playOnAwake = false;
        Altavoz.loop = true;
        Altavoz.spatialBlend = 0f;
        Altavoz.volume = 0f;
    }

    void PanelQuemado()
    {
        Vector3 c = new Vector3(-0.25f, 1.6f, Fondo * 0.5f - 0.3f);
        Caja("Panel principal", c, new Vector3(1.3f, 1.9f, 0.26f), Liso(new Color(0.3f, 0.33f, 0.33f)));
        Caja("Placa", c + new Vector3(0f, 0.75f, -0.14f), new Vector3(0.6f, 0.12f, 0.01f), Liso(new Color(0.55f, 0.45f, 0.25f)));
        Quemadura = Origen + c + new Vector3(0.05f, -0.05f, -0.14f);

        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(q.GetComponent<Collider>());
        q.name = "Quemadura";
        q.transform.SetParent(Raiz, false);
        q.transform.localPosition = c + new Vector3(0.05f, -0.05f, -0.135f);
        q.transform.localRotation = Quaternion.LookRotation(Vector3.forward);
        q.transform.localScale = new Vector3(0.62f, 0.78f, 1f);
        q.GetComponent<Renderer>().sharedMaterial = MaterialQuemadura();
        q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void Luces()
    {
        // Sol de la mañana por la ventana: un foco cálido que cruza el cuarto en diagonal.
        Light luna = new GameObject("Sol por la ventana").AddComponent<Light>();
        luna.transform.SetParent(Raiz, false);
        luna.transform.localPosition = new Vector3(Ancho * 0.5f + 1.5f, 4.4f, 0.3f);
        luna.transform.localRotation = Quaternion.LookRotation(new Vector3(-0.85f, -0.75f, -0.1f));
        luna.type = LightType.Spot;
        luna.spotAngle = 48f;
        luna.innerSpotAngle = 22f;
        luna.range = 16f;
        luna.intensity = 42f;
        luna.color = new Color(1f, 0.86f, 0.66f);
        luna.shadows = LightShadows.Soft;

        // Rebote del haz en el agua del suelo: aclara a Nilo tumbado y lo que tiene alrededor.
        Light rebote = new GameObject("Rebote del sol").AddComponent<Light>();
        rebote.transform.SetParent(Raiz, false);
        rebote.transform.localPosition = new Vector3(1.6f, 0.5f, -0.5f);
        rebote.type = LightType.Point;
        rebote.range = 5f;
        rebote.intensity = 4.5f;
        rebote.color = new Color(1f, 0.84f, 0.62f);
        rebote.shadows = LightShadows.None;

        // Relleno muy débil para que las máquinas no sean manchas negras.
        Light relleno = new GameObject("Relleno").AddComponent<Light>();
        relleno.transform.SetParent(Raiz, false);
        relleno.transform.localPosition = new Vector3(-2f, 3.8f, -2f);
        relleno.type = LightType.Point;
        relleno.range = 12f;
        relleno.intensity = 3.6f;
        relleno.color = new Color(0.55f, 0.66f, 0.8f);
        relleno.shadows = LightShadows.None;
    }

    // ------------------------------------------------------------------ animación

    /// <summary>0 apagada, 1 encendida. La radio se enciende con un par de chispazos.</summary>
    public void EncenderRadio(float k)
    {
        encendidoRadio = k;
    }

    public void Animar(float t)
    {
        float k = encendidoRadio;
        float titileo = k > 0f && k < 1f ? (Mathf.PerlinNoise(t * 30f, 1f) > 0.45f ? 1f : 0.2f) : 1f;
        float latido = 0.85f + 0.15f * Mathf.PerlinNoise(t * 6f, 2f);
        if (dialRadio != null) dialRadio.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.15f) * (3f * k * titileo * latido));
        if (luzRadio != null) luzRadio.intensity = 0.9f * k * titileo * latido;

        for (int i = ondas.Count - 1; i >= 0; i--)
        {
            OndaAgua o = ondas[i];
            float d = (t - o.inicio) / 1.6f;
            if (d >= 1f) { Object.Destroy(o.t.gameObject); ondas.RemoveAt(i); continue; }
            o.t.localScale = Vector3.one * Mathf.Lerp(0.05f, o.escala, 1f - (1f - d) * (1f - d));
            if (materialOnda.HasProperty("_BaseColor")) o.t.GetComponent<Renderer>().material.SetColor("_BaseColor", new Color(0.75f, 0.85f, 1f, 0.5f * (1f - d)));
        }
    }

    /// <summary>Una onda que se abre en el agua del suelo.</summary>
    public void Onda(Vector3 donde, float escala, float t)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(g.GetComponent<Collider>());
        g.name = "Onda";
        g.transform.SetParent(Raiz, false);
        g.transform.position = new Vector3(donde.x, Origen.y + 0.085f, donde.z);
        g.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        g.transform.localScale = Vector3.one * 0.05f;
        Renderer r = g.GetComponent<Renderer>();
        r.sharedMaterial = materialOnda;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ondas.Add(new OndaAgua { t = g.transform, inicio = t, escala = escala });
    }

    // ------------------------------------------------------------------ utilidades

    void Pieza(string nombre, Vector3 pos, float giro, float alto, Color tinte)
    {
        GameObject m = modelo != null ? modelo(nombre) : null;
        if (m == null)
        {
            Caja(nombre, pos + Vector3.up * alto * 0.5f, new Vector3(alto * 0.8f, alto, alto * 0.8f), Liso(tinte * 0.6f));
            return;
        }
        GameObject g = Object.Instantiate(m, Raiz);
        g.name = nombre;
        foreach (Collider c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        g.transform.localRotation = Quaternion.Euler(0f, giro, 0f);
        g.transform.localPosition = pos;
        float escala = alto / Mathf.Max(0.01f, Limites(g).size.y);
        g.transform.localScale = Vector3.one * escala;
        Bounds b = Limites(g);
        Vector3 destino = Origen + pos;
        g.transform.position += new Vector3(destino.x - b.center.x, destino.y - b.min.y, destino.z - b.center.z);
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            Material[] ms = r.sharedMaterials;
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i] == null) continue;
                Material copia = new Material(ms[i]);
                if (copia.HasProperty("_BaseColor")) copia.SetColor("_BaseColor", copia.GetColor("_BaseColor") * tinte);
                if (copia.IsKeywordEnabled("_EMISSION")) copia.SetColor("_EmissionColor", Color.black);
                ms[i] = copia;
            }
            r.sharedMaterials = ms;
        }
    }

    static Bounds Limites(GameObject g)
    {
        Bounds b = new Bounds(g.transform.position, Vector3.zero);
        bool hay = false;
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            if (hay) b.Encapsulate(r.bounds); else { b = r.bounds; hay = true; }
        }
        return b;
    }

    GameObject Caja(string nombre, Vector3 centro, Vector3 tam, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(g.GetComponent<Collider>());
        g.name = nombre;
        g.transform.SetParent(Raiz, false);
        g.transform.localPosition = centro;
        g.transform.localScale = tam;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    readonly Dictionary<string, Material> lisos = new Dictionary<string, Material>();

    Material Liso(Color c, float brillo = 0.15f)
    {
        string clave = c + "|" + brillo;
        Material m;
        if (lisos.TryGetValue(clave, out m)) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", brillo);
        lisos[clave] = m;
        return m;
    }

    static Material Emisivo(Color c, float fuerza)
    {
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", Color.Lerp(c, Color.black, 0.7f));
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        m.SetColor("_EmissionColor", c * fuerza);
        return m;
    }

    /// <summary>
    /// Hollín con la forma de una mano robótica de tres dedos y pulgar, con el borde aún tibio.
    /// Se dibuja en una textura con transparencia y se pega sobre el panel.
    /// </summary>
    static Material MaterialQuemadura()
    {
        const int n = 256;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true);
        t.wrapMode = TextureWrapMode.Clamp;
        Texture2D brillo = new Texture2D(n, n, TextureFormat.RGBA32, true);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                float d = DistanciaMano(u, v);
                float ruido = Mathf.PerlinNoise(u * 18f, v * 18f) * 0.05f;
                float hollin = Mathf.Clamp01(1f - (d + ruido) / 0.09f);       // halo de humo
                float nucleo = Mathf.Clamp01(1f - (d + ruido * 0.5f) / 0.012f); // la huella
                float a = Mathf.Max(nucleo, hollin * 0.75f);
                Color c = Color.Lerp(new Color(0.12f, 0.1f, 0.08f), new Color(0.02f, 0.02f, 0.02f), nucleo);
                t.SetPixel(x, y, new Color(c.r, c.g, c.b, a));
                float borde = Mathf.Clamp01(1f - Mathf.Abs(d - 0.006f) / 0.01f) * (0.6f + Mathf.PerlinNoise(u * 40f, v * 40f));
                brillo.SetPixel(x, y, new Color(1f, 0.35f, 0.08f) * borde * 0.22f);
            }
        t.Apply();
        brillo.Apply();
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.SetTexture("_BaseMap", t);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0.05f);
        m.EnableKeyword("_EMISSION");
        m.SetTexture("_EmissionMap", brillo);
        m.SetColor("_EmissionColor", Color.white);
        return m;
    }

    /// <summary>Distancia (en la textura) a la silueta de la mano: palma redondeada, tres dedos y pulgar.</summary>
    static float DistanciaMano(float u, float v)
    {
        float d = Capsula(u, v, 0.5f, 0.32f, 0.5f, 0.42f, 0.13f);               // palma
        d = Mathf.Min(d, Capsula(u, v, 0.4f, 0.5f, 0.36f, 0.78f, 0.045f));     // dedo
        d = Mathf.Min(d, Capsula(u, v, 0.5f, 0.52f, 0.5f, 0.84f, 0.045f));     // dedo
        d = Mathf.Min(d, Capsula(u, v, 0.6f, 0.5f, 0.64f, 0.77f, 0.045f));     // dedo
        d = Mathf.Min(d, Capsula(u, v, 0.64f, 0.34f, 0.8f, 0.5f, 0.045f));     // pulgar
        // Arrastre: la mano resbaló hacia abajo al soltarse.
        d = Mathf.Min(d, Capsula(u, v, 0.5f, 0.3f, 0.52f, 0.08f, 0.06f) + 0.02f);
        return Mathf.Max(0f, d);
    }

    static float Capsula(float u, float v, float ax, float ay, float bx, float by, float radio)
    {
        Vector2 p = new Vector2(u, v), a = new Vector2(ax, ay), b = new Vector2(bx, by);
        Vector2 ab = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
        return (p - (a + ab * h)).magnitude - radio;
    }

    static Texture2D anillo;

    static Texture2D Anillo()
    {
        if (anillo != null) return anillo;
        const int n = 128;
        anillo = new Texture2D(n, n, TextureFormat.RGBA32, true);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n - 0.5f, v = (y + 0.5f) / n - 0.5f;
                float r = Mathf.Sqrt(u * u + v * v);
                float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.42f) / 0.04f);
                anillo.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        anillo.Apply();
        return anillo;
    }
}
