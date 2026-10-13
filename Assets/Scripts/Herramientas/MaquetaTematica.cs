using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.IO;
using UnityEditor.Media;
#endif

/// <summary>
/// Maquetas de diseños de nivel con otra ambientación, como adelanto de lo que viene:
///
///  - Ciénaga: "La Ciénaga de los Faroles Ahogados". Agua turbia y verdosa, niebla, piedra con
///    musgo, pinos retorcidos, lápidas, verjas, barcos naufragados, un fantasma, cuervos y peces
///    bajo el agua. Mecánica de adelanto: Nilo enciende faroles y la niebla retrocede.
///  - Forja: "La Forja de Ascuas". Ríos de lava, basalto, torres en ruinas, brasas en el aire y
///    salamandras de fuego que saltan de la lava. Mecánica de adelanto: losas de basalto emergen,
///    se enfrían y se vuelven a fundir.
///
/// El nivel se levanta con el mismo DioramaBuilder del juego y después se le cambia el tema:
/// materiales del suelo, agua, vegetación, barcos, fauna, luz y niebla. Al darle Play se ve el
/// timelapse de construcción y la mecánica. Desde el editor se graba a MP4 (Grabar).
/// </summary>
public sealed class MaquetaTematica : MonoBehaviour
{
    public enum Tema { Cienaga, Forja }

    [SerializeField] Tema tema = Tema.Cienaga;
    [Tooltip("Modelos de los kits de terceros; se buscan por nombre.")]
    [SerializeField] List<GameObject> modelos = new List<GameObject>();
    [Tooltip("Clip de nado de los peces de Quaternius.")]
    [SerializeField] AnimationClip clipNado;

    public static string Estado = "";
    const int Fps = 30;

    LevelLayout mapa;
    Bounds limites;
    Transform raiz, actores, decorado;
    Camera cam;
    Light sol;
    PlayerController nilo;
    Text subtitulo;
    CanvasGroup grupoTexto;
    int fotograma;
    float Video { get { return fotograma / (float)Fps; } }

    struct Pieza { public Transform t; public Vector3 escala, posicion; public float inicio; }
    readonly List<Pieza> piezas = new List<Pieza>();
    readonly Dictionary<string, GameObject> porNombre = new Dictionary<string, GameObject>();

    // ------------------------------------------------------------------ mapas

    static LevelLayout Mapa(Tema t)
    {
        if (t == Tema.Cienaga)
            return new LevelLayout("Ciénaga de los Faroles Ahogados", 2f,
                "v####wwww#v##",
                "#####wwww####",
                "#N#mmmmmmmmE#",
                "#####wwww####",
                "v####wwww##v#",
                "#####wwww####",
                "###c#wwww#c##",
                "v####wwww###v");

        return new LevelLayout("La Forja de Ascuas", 2f,
            "v####wwww##v#",
            "#####wwww####",
            "#N###wwww##E#",
            "#####wwww####",
            "#v###wwww#v##",
            "##c##wwww####",
            "#####wwww##c#",
            "v####wwww###v");
    }

    Vector3 C(float col, int fila) { return mapa.CentroTile(col, fila); }
    bool EsCienaga { get { return tema == Tema.Cienaga; } }

    // ------------------------------------------------------------------ arranque

    IEnumerator Start()
    {
        foreach (GameObject m in modelos) if (m != null) porNombre[m.name] = m;

        ActorFactory.ConfigurarEntorno();
        mapa = Mapa(tema);
        RelevoGame.Layout = mapa;

        GameObject goNivel = new GameObject("Nivel");
        raiz = goNivel.transform;
        goNivel.AddComponent<AmbienteRelevo>();
        limites = DioramaBuilder.Construir(mapa, raiz);

        CameraRig rig = new GameObject("Camara").AddComponent<CameraRig>();
        rig.Configurar(limites);
        rig.Camara.enabled = false;

        actores = new GameObject("Actores maqueta").transform;
        decorado = new GameObject("Decorado tematico").transform;
        decorado.SetParent(raiz, false);
        sol = ActorFactory.CrearSol(actores);

        AplicarTema();
        CrearActores();
        PrepararCamara();
        PrepararTexto();
        PrepararTimelapse();

        yield return null;
        IniciarGrabacion();
        yield return Construccion();
        if (EsCienaga) yield return Faroles();
        else yield return Basalto();
        yield return Final();
        TerminarGrabacion();
    }

    // ------------------------------------------------------------------ utilidades de modelos

    GameObject Poner(string nombre, Transform padre, Vector3 pos, float alto, float giro, Color? tinte = null)
    {
        GameObject modelo;
        if (!porNombre.TryGetValue(nombre, out modelo) || modelo == null) return null;
        GameObject go = Instantiate(modelo, padre);
        go.name = nombre;
        foreach (Collider c in go.GetComponentsInChildren<Collider>()) Destroy(c);
        Bounds b = Limites(go);
        float escala = alto / Mathf.Max(0.01f, b.size.y);
        go.transform.localScale = Vector3.one * escala;
        go.transform.localRotation = Quaternion.Euler(0f, giro, 0f);
        go.transform.position = pos + Vector3.up * (-b.min.y * escala);
        if (tinte.HasValue) Tenir(go, tinte.Value);
        return go;
    }

    static Bounds Limites(GameObject go)
    {
        Vector3 p = go.transform.position; Quaternion r = go.transform.rotation; Vector3 s = go.transform.localScale;
        go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity; go.transform.localScale = Vector3.one;
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        bool hay = false;
        foreach (Renderer rr in go.GetComponentsInChildren<Renderer>())
        {
            if (hay) b.Encapsulate(rr.bounds); else { b = rr.bounds; hay = true; }
        }
        go.transform.position = p; go.transform.rotation = r; go.transform.localScale = s;
        return b;
    }

    readonly Dictionary<string, Material> tenidos = new Dictionary<string, Material>();

    Material Tenido(Material origen, Color tinte)
    {
        if (origen == null) return null;
        Material m;
        string clave = origen.GetInstanceID() + "|" + tinte;
        if (tenidos.TryGetValue(clave, out m)) return m;
        m = new Material(origen);
        m.name = origen.name + " tema";
        if (m.HasProperty("_BaseColor"))
        {
            Color c = m.GetColor("_BaseColor");
            float gris = (c.r + c.g + c.b) / 3f;
            Color des = Color.Lerp(c, new Color(gris, gris, gris, c.a), 0.45f);
            m.SetColor("_BaseColor", new Color(des.r * tinte.r, des.g * tinte.g, des.b * tinte.b, c.a));
        }
        tenidos[clave] = m;
        return m;
    }

    void Tenir(GameObject go, Color tinte)
    {
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            Material[] ms = r.sharedMaterials;
            for (int i = 0; i < ms.Length; i++) ms[i] = Tenido(ms[i], tinte);
            r.sharedMaterials = ms;
        }
    }

    static Material Emisivo(Color color, float fuerza)
    {
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", color);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color * fuerza);
        return m;
    }

    // ------------------------------------------------------------------ cambio de tema

    static readonly HashSet<string> Grupos = new HashSet<string>
    {
        "Suelo", "Agua", "Bloqueos", "Detalles", "Fondo", "Vestido suelo", "Vestido plantas",
        "Vestido orillas", "Vestido puerto", "Vestido malecon", "Barcas", "Decorado tematico"
    };

    Transform Unidad(Transform t)
    {
        while (t.parent != null && t.parent != raiz && !Grupos.Contains(t.parent.name)) t = t.parent;
        return t;
    }

    Texture2D texturaLava;
    readonly List<Material> lavas = new List<Material>();
    readonly List<Light> lucesLava = new List<Light>();

    void AplicarTema()
    {
        // 1. Fuera la vegetación de Liria (follaje) y los barcos y boyas.
        var borrar = new HashSet<Transform>();
        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material m in r.sharedMaterials)
                if (m != null && m.shader != null && m.shader.name.Contains("Follaje")) borrar.Add(Unidad(r.transform));
            if (r.transform.parent != null && Unidad(r.transform).parent != null && Unidad(r.transform).parent.name == "Barcas")
                borrar.Add(Unidad(r.transform));
        }
        foreach (Transform t in borrar) if (t != null && t != raiz) DestroyImmediate(t.gameObject);

        // 2. Suelos y agua.
        Color tinte = EsCienaga ? new Color(0.62f, 0.7f, 0.6f) : new Color(0.36f, 0.31f, 0.3f);
        Material agua = EsCienaga ? AguaTurbia() : null;
        if (!EsCienaga) texturaLava = TexturaLava();

        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;
            bool esAgua = r.name == "Agua" || r.name == "Mar";
            Material[] ms = r.sharedMaterials;
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i] == null) continue;
                if (esAgua) ms[i] = EsCienaga ? agua : Lava(r);
                else if (ms[i].IsKeywordEnabled("_EMISSION") && ms[i].GetColor("_EmissionColor").maxColorComponent > 0.3f)
                    ms[i] = EsCienaga ? Tenido(ms[i], new Color(0.8f, 1f, 0.7f)) : ms[i];
                else ms[i] = Tenido(ms[i], tinte);
            }
            r.sharedMaterials = ms;
        }

        // 3. Atmósfera.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        if (EsCienaga)
        {
            RenderSettings.fogColor = new Color(0.2f, 0.26f, 0.25f);
            RenderSettings.fogStartDistance = 12f;
            RenderSettings.fogEndDistance = 46f;
            RenderSettings.ambientSkyColor = new Color(0.3f, 0.36f, 0.4f);
            RenderSettings.ambientEquatorColor = new Color(0.22f, 0.26f, 0.25f);
            RenderSettings.ambientGroundColor = new Color(0.1f, 0.11f, 0.09f);
        }
        else
        {
            RenderSettings.fogColor = new Color(0.22f, 0.09f, 0.06f);
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance = 75f;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.24f, 0.18f);
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.15f, 0.1f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.12f, 0.05f);
        }

        // 4. Partículas: luciérnagas verdosas o brasas que suben.
        foreach (ParticleSystem p in raiz.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = p.main;
            if (EsCienaga) main.startColor = new Color(0.7f, 1f, 0.45f, 0.9f);
            else
            {
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.15f), new Color(1f, 0.25f, 0.05f));
                main.gravityModifier = -0.12f;
                var em = p.emission;
                em.rateOverTime = new ParticleSystem.MinMaxCurve(em.rateOverTime.constant * 3f);
            }
        }

        // 5. Gaviotas: cuervos negros en la ciénaga, murciélagos sobre la forja.
        foreach (Gaviotas g in raiz.GetComponentsInChildren<Gaviotas>(true))
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
                if (r.sharedMaterial != null) r.sharedMaterial.SetColor("_BaseColor", new Color(0.06f, 0.06f, 0.07f));

        // 6. Decorado propio del tema.
        if (EsCienaga) DecorarCienaga(); else DecorarForja();
    }

    Material AguaTurbia()
    {
        KitRelevo kit = KitRelevo.Activo;
        Material m = new Material(kit.agua);
        m.SetColor("_ColorSomero", new Color(0.3f, 0.34f, 0.2f));
        m.SetColor("_ColorProfundo", new Color(0.06f, 0.1f, 0.07f));
        m.SetColor("_ColorEspuma", new Color(0.5f, 0.55f, 0.42f));
        m.SetFloat("_Destellos", 0.35f);
        m.SetFloat("_Opacidad", 0.9f);
        m.SetFloat("_EscalaOndas", 0.35f);
        m.SetVector("_Velocidad", new Vector4(0.05f, 0.02f, 0f, 0f));
        return m;
    }

    // ------------------------------------------------------------------ lava

    static Texture2D TexturaLava()
    {
        const int n = 256;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true);
        t.wrapMode = TextureWrapMode.Repeat;
        Color costra = new Color(0.09f, 0.045f, 0.035f);
        Color brasa = new Color(1f, 0.38f, 0.06f);
        Color blanco = new Color(1f, 0.85f, 0.4f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (float)n, v = y / (float)n;
                // Ruido periódico (cuatro muestras mezcladas) para que la textura repita sin costuras.
                float f = 0f, amp = 0.55f, fr = 4f;
                for (int o = 0; o < 4; o++)
                {
                    float a = Mathf.PerlinNoise(u * fr, v * fr), b = Mathf.PerlinNoise((u - 1f) * fr, v * fr);
                    float c = Mathf.PerlinNoise(u * fr, (v - 1f) * fr), d = Mathf.PerlinNoise((u - 1f) * fr, (v - 1f) * fr);
                    float mx = Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
                    f += mx * amp; amp *= 0.5f; fr *= 2f;
                }
                float vena = 1f - Mathf.Abs(f * 2f - 1f);
                vena = Mathf.Pow(Mathf.Clamp01(vena), 4f);
                Color col = Color.Lerp(costra, brasa, Mathf.Clamp01(vena * 1.6f));
                col = Color.Lerp(col, blanco, Mathf.Clamp01(vena * 2f - 1.2f));
                t.SetPixel(x, y, col);
            }
        t.Apply(true);
        return t;
    }

    Material Lava(Renderer r)
    {
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetTexture("_BaseMap", texturaLava);
        m.SetColor("_BaseColor", Color.white);
        m.EnableKeyword("_EMISSION");
        m.SetTexture("_EmissionMap", texturaLava);
        m.SetFloat("_Smoothness", 0.25f);
        Vector3 s = r.transform.lossyScale;
        m.SetTextureScale("_BaseMap", new Vector2(Mathf.Max(1f, s.x / 7f), Mathf.Max(1f, s.z / 7f)));
        // El mar que rodea la maqueta es costra de magma casi fría: solo el canal es lava viva.
        bool mar = r.name == "Mar";
        m.SetColor("_BaseColor", mar ? new Color(0.22f, 0.16f, 0.15f) : new Color(0.85f, 0.7f, 0.6f));
        m.SetColor("_EmissionColor", mar ? new Color(0.35f, 0.12f, 0.05f) : new Color(1.3f, 0.55f, 0.25f));
        if (!mar) lavas.Add(m);
        return m;
    }

    void AnimarLava()
    {
        float t = Time.time;
        for (int i = 0; i < lavas.Count; i++)
        {
            lavas[i].SetTextureOffset("_BaseMap", new Vector2(t * 0.012f, t * 0.02f + i * 0.13f));
            float pulso = 1f + Mathf.Sin(t * 1.3f + i) * 0.18f;
            lavas[i].SetColor("_EmissionColor", new Color(1.3f, 0.55f, 0.25f) * pulso);
        }
        for (int i = 0; i < lucesLava.Count; i++)
            lucesLava[i].intensity = 2f + Mathf.PerlinNoise(t * 2.5f, i * 3.1f) * 1.5f;
    }

    void LuzLava(Vector3 pos)
    {
        GameObject g = new GameObject("Luz de lava");
        g.transform.SetParent(decorado, false);
        g.transform.position = pos;
        Light l = g.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.45f, 0.15f);
        l.range = 7f;
        l.shadows = LightShadows.None;
        lucesLava.Add(l);
    }

    // ------------------------------------------------------------------ decorados

    readonly List<Transform> fantasmas = new List<Transform>();

    void DecorarCienaga()
    {
        Color musgo = new Color(0.62f, 0.68f, 0.6f);
        string[] lapidas = { "gravestone-cross", "gravestone-round", "gravestone-broken", "gravestone-wide", "gravestone-bevel" };
        int k = 0;
        foreach (Vector2Int v in mapa.Casillas('v'))
        {
            Vector3 c = C(v.x, v.y);
            Poner(k % 2 == 0 ? "pine-crooked" : "pine-fall-crooked", decorado, c + new Vector3(-0.3f, 0f, 0.2f), 3.4f, k * 71f, new Color(0.3f, 0.36f, 0.3f));
            Poner(lapidas[k % lapidas.Length], decorado, c + new Vector3(0.55f, 0f, -0.5f), 0.75f, 180f + k * 13f, musgo);
            Poner("candle-multiple", decorado, c + new Vector3(-0.6f, 0f, -0.6f), 0.35f, 0f);
            k++;
        }

        // Lápidas y verjas sueltas por los muelles.
        Vector3[] sueltas = { C(1, 5), C(2, 6), C(10, 5), C(11, 6), C(0, 3), C(12, 3), C(3, 0), C(9, 7) };
        for (int i = 0; i < sueltas.Length; i++)
            Poner(lapidas[(i + 2) % lapidas.Length], decorado, sueltas[i] + new Vector3((i % 3 - 1) * 0.5f, 0f, 0.3f), 0.7f, 170f + i * 23f, musgo);
        Poner("iron-fence-damaged", decorado, C(1, 4) + new Vector3(0f, 0f, 0.9f), 1.1f, 0f, musgo);
        Poner("iron-fence", decorado, C(11, 4) + new Vector3(0f, 0f, 0.9f), 1.1f, 0f, musgo);
        Poner("crypt-small", decorado, C(1, 6) + new Vector3(0f, 0f, 0f), 2.2f, 90f, musgo);
        Poner("coffin-old", decorado, C(11, 1) + new Vector3(0.2f, 0f, -0.6f), 0.4f, 30f, musgo);
        Poner("trunk", decorado, C(10, 3) + new Vector3(0.3f, 0f, 0.2f), 0.5f, 40f, musgo);
        Poner("debris-wood", decorado, C(4, 5) + new Vector3(0f, 0f, 0f), 0.35f, 10f, musgo);

        // Barcos naufragados y un barco fantasma en el agua.
        Poner("ship-wreck", decorado, new Vector3(limites.min.x - 4.5f, -1.9f, C(0, 2).z), 2.6f, 70f, musgo);
        Poner("ship-wreck", decorado, new Vector3(limites.max.x + 4.5f, -1.9f, C(0, 6).z), 2.4f, -110f, musgo);
        GameObject barcoFantasma = Poner("ship-ghost", decorado, new Vector3(limites.max.x + 9f, -1.6f, C(0, 1).z), 5f, -100f);
        if (barcoFantasma != null) barcoFantasma.AddComponent<Flotador>().Configurar(0.08f, 2.4f, 2f, 3f, 2f);

        // Fantasma sobre el canal.
        GameObject fantasma = Poner("character-ghost", decorado, C(6.5f, 5) + Vector3.up * 0.4f, 1.1f, 200f);
        if (fantasma != null) fantasmas.Add(fantasma.transform);

        // Peces turbios bajo el agua.
        for (int i = 0; i < 5; i++) CrearPez(i % 2 == 0 ? "Fish2" : "Fish3", new Color(0.45f, 0.5f, 0.35f), -0.75f, i);
    }

    void DecorarForja()
    {
        Color basalto = new Color(0.42f, 0.36f, 0.35f);
        int k = 0;
        foreach (Vector2Int v in mapa.Casillas('v'))
        {
            Vector3 c = C(v.x, v.y);
            if (k % 2 == 0)
            {
                Poner("tower-square", decorado, c + new Vector3(-0.2f, 0f, 0.1f), 2.6f, k * 30f, basalto);
                Poner("flag-banner-long", decorado, c + new Vector3(0.7f, 0f, -0.4f), 1.6f, 180f, new Color(0.9f, 0.5f, 0.4f));
            }
            else
            {
                Poner("tree-trunk", decorado, c + new Vector3(-0.2f, 0f, 0.2f), 1.4f, k * 50f, new Color(0.18f, 0.14f, 0.13f));
                Poner("rocks-large", decorado, c + new Vector3(0.5f, 0f, -0.4f), 0.9f, k * 40f, basalto);
            }
            GameObject cesta = Poner("fire-basket", decorado, c + new Vector3(-0.65f, 0f, -0.65f), 0.7f, 0f, basalto);
            if (cesta != null) Llama(cesta.transform.position + Vector3.up * 0.75f);
            k++;
        }

        Vector3[] rocas = { C(1, 5), C(3, 1), C(10, 6), C(12, 3), C(2, 7), C(10, 1) };
        for (int i = 0; i < rocas.Length; i++)
            Poner(i % 2 == 0 ? "rocks-small" : "rocks-large", decorado, rocas[i] + new Vector3(0.4f, 0f, 0.4f), 0.6f, i * 60f, basalto);
        Poner("siege-catapult-demolished", decorado, C(2, 5) + new Vector3(0f, 0f, 0f), 1.2f, 30f, basalto);
        Poner("siege-tower-demolished", decorado, C(11, 7) + new Vector3(0f, 0f, 0f), 2.2f, -20f, basalto);
        Poner("tower-hexagon-base", decorado, new Vector3(limites.min.x - 4f, -1.4f, C(0, 1).z), 3f, 0f, basalto);
        Poner("tower-hexagon-mid", decorado, new Vector3(limites.max.x + 4f, -1.4f, C(0, 5).z), 3.4f, 0f, basalto);
        Poner("rocks-large", decorado, new Vector3(limites.min.x - 3f, -1.5f, C(0, 6).z), 1.6f, 20f, basalto);
        Poner("rocks-large", decorado, new Vector3(limites.max.x + 3.5f, -1.5f, C(0, 2).z), 1.4f, 120f, basalto);

        // Luces de la lava a lo largo del canal.
        for (int f = 0; f < mapa.FilasCuenta; f += 2) LuzLava(C(6.5f, f) + Vector3.up * 0.6f);
        LuzLava(new Vector3(limites.center.x, 0.5f, limites.min.z - 3f));

        // Salamandras de fuego.
        for (int i = 0; i < 4; i++) CrearPez(i % 2 == 0 ? "Fish1" : "Fish3", new Color(1f, 0.4f, 0.08f), -0.5f, i);
    }

    void Llama(Vector3 pos)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(g.GetComponent<Collider>());
        g.name = "Llama";
        g.transform.SetParent(decorado, false);
        g.transform.position = pos;
        g.transform.localScale = new Vector3(0.28f, 0.4f, 0.28f);
        g.GetComponent<Renderer>().sharedMaterial = Emisivo(new Color(1f, 0.5f, 0.12f), 4f);
        GameObject l = new GameObject("Luz llama");
        l.transform.SetParent(g.transform, false);
        Light luz = l.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.55f, 0.2f);
        luz.range = 4f;
        luz.intensity = 2.5f;
        lucesLava.Add(luz);
    }

    // ------------------------------------------------------------------ fauna

    sealed class Pez
    {
        public Transform t;
        public AnimationClipPlayable playable;
        public PlayableGraph grafo;
        public float fase, radio, velocidad, profundidad;
        public Vector3 centro;
    }
    readonly List<Pez> peces = new List<Pez>();

    void CrearPez(string nombre, Color color, float profundidad, int i)
    {
        Vector3 centro = C(6.5f, 1 + (i * 3) % 7);
        GameObject g = Poner(nombre, decorado, centro, 0.35f, 0f);
        if (g == null) return;
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            Material[] ms = r.sharedMaterials;
            for (int k = 0; k < ms.Length; k++)
                ms[k] = EsCienaga ? Tenido(ms[k], color) : Emisivo(Color.Lerp(color, new Color(1f, 0.85f, 0.3f), k * 0.3f), 1.6f);
            r.sharedMaterials = ms;
        }
        Pez p = new Pez { t = g.transform, fase = i * 1.7f, radio = 1.2f + (i % 3) * 0.5f, velocidad = 0.6f + (i % 2) * 0.3f, profundidad = profundidad, centro = centro };
        if (clipNado != null)
        {
            Animator a = g.GetComponentInChildren<Animator>();
            if (a == null) a = g.AddComponent<Animator>();
            p.playable = AnimationPlayableUtilities.PlayClip(a, clipNado, out p.grafo);
        }
        peces.Add(p);
    }

    void AnimarFauna(float t)
    {
        foreach (Pez p in peces)
        {
            if (p.t == null) continue;
            if (p.playable.IsValid()) p.playable.SetTime((t * 1.3f + p.fase) % clipNado.length);
            float a = t * p.velocidad + p.fase;
            Vector3 pos;
            if (EsCienaga)
            {
                // Nadan en óvalos bajo la superficie turbia.
                pos = p.centro + new Vector3(Mathf.Cos(a) * 0.9f, p.profundidad + Mathf.Sin(a * 2f) * 0.05f, Mathf.Sin(a) * p.radio);
            }
            else
            {
                // Saltan de la lava en arcos y vuelven a hundirse.
                float ciclo = Mathf.Repeat(a / 3f, 1f);
                float salto = ciclo < 0.35f ? Mathf.Sin(ciclo / 0.35f * Mathf.PI) : -0.3f;
                pos = p.centro + new Vector3(Mathf.Lerp(-1.2f, 1.2f, Mathf.Clamp01(ciclo / 0.35f)) , p.profundidad + salto * 1.4f, Mathf.Sin(p.fase) * 0.8f);
            }
            Vector3 dir = pos - p.t.position;
            if (dir.sqrMagnitude > 0.00001f) p.t.rotation = Quaternion.Slerp(p.t.rotation, Quaternion.LookRotation(dir), 0.2f);
            p.t.position = pos;
        }
        foreach (Transform f in fantasmas)
        {
            if (f == null) continue;
            f.position = C(6.5f, 5) + new Vector3(Mathf.Sin(t * 0.4f) * 1.5f, 0.5f + Mathf.Sin(t * 1.7f) * 0.15f, Mathf.Cos(t * 0.3f) * 1.2f);
            f.rotation = Quaternion.Euler(0f, 200f + Mathf.Sin(t * 0.4f) * 40f, Mathf.Sin(t * 1.1f) * 6f);
        }
        if (!EsCienaga) AnimarLava();
    }

    void Update() { AnimarFauna(Time.time); }

    void OnDestroy()
    {
        foreach (Pez p in peces) if (p.grafo.IsValid()) p.grafo.Destroy();
    }

    // ------------------------------------------------------------------ actores y mecánicas

    readonly List<Light> farolesLuz = new List<Light>();
    readonly List<Renderer> farolesLlama = new List<Renderer>();
    readonly List<Transform> losas = new List<Transform>();
    readonly List<Material> losasMat = new List<Material>();

    void CrearActores()
    {
        nilo = ActorFactory.CrearNilo(actores, C(1, 2), 90f);
        nilo.enabled = false;
        CharacterController cc = nilo.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        ActorFactory.CrearSalida(actores, C(11, 2), "la salida");

        if (EsCienaga)
        {
            // Faroles a lo largo de la pasarela, apagados.
            float[] cols = { 3f, 6.5f, 10f };
            foreach (float col in cols)
            {
                Vector3 p = C(col, 2) + new Vector3(0f, 0f, 0.85f);
                GameObject poste = Poner("lightpost-single", actores, p, 2.1f, 180f, new Color(0.6f, 0.62f, 0.58f));
                GameObject llama = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(llama.GetComponent<Collider>());
                llama.transform.SetParent(actores, false);
                llama.transform.position = p + Vector3.up * 1.75f;
                llama.transform.localScale = Vector3.one * 0.22f;
                Renderer r = llama.GetComponent<Renderer>();
                r.sharedMaterial = Emisivo(new Color(0.85f, 1f, 0.55f), 0f);
                farolesLlama.Add(r);
                GameObject l = new GameObject("Luz farol");
                l.transform.SetParent(llama.transform, false);
                Light luz = l.AddComponent<Light>();
                luz.type = LightType.Point;
                luz.color = new Color(0.8f, 1f, 0.55f);
                luz.range = 8f;
                luz.intensity = 0f;
                farolesLuz.Add(luz);
            }
            return;
        }

        // Losas de basalto, hundidas en la lava.
        for (int c = 5; c <= 8; c++)
        {
            GameObject losa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(losa.GetComponent<Collider>());
            losa.name = "Losa de basalto";
            losa.transform.SetParent(actores, false);
            losa.transform.position = C(c, 2) + Vector3.up * -1.4f;
            losa.transform.localScale = new Vector3(1.75f, 0.6f, 1.75f);
            losa.transform.localRotation = Quaternion.Euler(0f, (c % 2) * 6f - 3f, 0f);
            Material m = Emisivo(new Color(1f, 0.4f, 0.08f), 3f);
            losa.GetComponent<Renderer>().sharedMaterial = m;
            losas.Add(losa.transform);
            losasMat.Add(m);
        }
    }

    IEnumerator Faroles()
    {
        float inicio = Video;
        Vector3[] ruta = { C(1, 2), C(10.6f, 2) };
        Vector3 pos = ruta[0];
        float niebla0 = RenderSettings.fogEndDistance;

        while (Video - inicio < 13f)
        {
            float t = Video - inicio;
            if (t < 2f) subtitulo.text = "La niebla de la ciénaga lo cubre todo";
            else if (t < 9.5f) subtitulo.text = "Enciende los faroles para abrir el camino";
            else subtitulo.text = "Lo que la luz no alcanza, sigue oculto";

            // La niebla se cierra al empezar y se abre con cada farol encendido.
            int encendidos = 0;
            for (int i = 0; i < farolesLuz.Count; i++)
            {
                float x = farolesLuz[i].transform.position.x;
                bool on = t > 2f && nilo.transform.position.x > x - 0.6f;
                float actual = farolesLuz[i].intensity;
                farolesLuz[i].intensity = Mathf.MoveTowards(actual, on ? 6f : 0f, Time.deltaTime * 10f);
                float parpadeo = 1f + (Mathf.PerlinNoise(t * 4f, i) - 0.5f) * 0.3f;
                farolesLlama[i].sharedMaterial.SetColor("_EmissionColor", new Color(0.85f, 1f, 0.55f) * (farolesLuz[i].intensity * 0.8f * parpadeo));
                if (on) encendidos++;
            }
            float objetivo = Mathf.Lerp(t < 2f ? Mathf.Lerp(niebla0, 24f, t / 2f) : 24f, 70f, encendidos / 3f);
            RenderSettings.fogEndDistance = Mathf.Lerp(RenderSettings.fogEndDistance, objetivo, Time.deltaTime * 1.5f);
            RenderSettings.fogStartDistance = RenderSettings.fogEndDistance * 0.3f;

            if (t > 2f)
            {
                pos = Vector3.MoveTowards(pos, ruta[1], Time.deltaTime * 2.2f);
                nilo.transform.rotation = Quaternion.LookRotation(Vector3.right);
            }
            nilo.transform.position = pos;

            Vector3 foco = Vector3.Lerp(C(3f, 2), nilo.transform.position, 0.6f) + Vector3.up * 0.4f;
            float abrir = Mathf.Clamp01((t - 9.5f) / 3.5f);
            abrir = abrir * abrir * (3f - 2f * abrir);
            Orbita(Mathf.Lerp(-18f, -4f, t / 13f), Mathf.Lerp(22f, 40f, abrir), Mathf.Lerp(10f, 19f, abrir), foco);
            yield return Grabar();
        }
    }

    IEnumerator Basalto()
    {
        float inicio = Video;
        Vector3[] ruta = { C(1, 2), C(4, 2), C(9, 2), C(10.6f, 2) };
        int tramo = 0;
        Vector3 pos = ruta[0];

        while (Video - inicio < 13f)
        {
            float t = Video - inicio;
            if (t < 2f) subtitulo.text = "La lava corta el paso";
            else if (t < 5f) subtitulo.text = "El basalto emerge y se enfría";
            else if (t < 9.5f) subtitulo.text = "Cruza mientras la roca aguanta";
            else subtitulo.text = "La forja vuelve a fundirlo todo";

            for (int i = 0; i < losas.Count; i++)
            {
                float sube = Mathf.Clamp01((t - 2f - i * 0.35f) / 1.2f);
                float baja = Mathf.Clamp01((t - 9.8f - i * 0.3f) / 1.4f);
                float altura = Mathf.Lerp(-1.4f, -0.4f, sube * sube * (3f - 2f * sube)) - 1.1f * baja;
                losas[i].position = new Vector3(losas[i].position.x, altura, losas[i].position.z);
                // Se enfría: de brasa a basalto negro, y al fundirse vuelve a brillar.
                float frio = Mathf.Clamp01((t - 3f - i * 0.35f) / 2f) * (1f - baja);
                Color baseC = Color.Lerp(new Color(1f, 0.4f, 0.08f), new Color(0.12f, 0.1f, 0.1f), frio);
                losasMat[i].SetColor("_BaseColor", baseC);
                losasMat[i].SetColor("_EmissionColor", new Color(1f, 0.4f, 0.08f) * Mathf.Lerp(3f, 0.05f, frio));
            }

            if (t > 5f && tramo < ruta.Length - 1)
            {
                pos = Vector3.MoveTowards(pos, ruta[tramo + 1], Time.deltaTime * 3f);
                if ((pos - ruta[tramo + 1]).sqrMagnitude < 0.001f) tramo++;
                nilo.transform.rotation = Quaternion.LookRotation(Vector3.right);
            }
            else if (t <= 5f && tramo == 0)
            {
                pos = Vector3.MoveTowards(pos, ruta[1], Time.deltaTime * 2f);
                nilo.transform.rotation = Quaternion.LookRotation(Vector3.right);
            }
            bool sobreLosa = pos.x > C(4.6f, 2).x && pos.x < C(8.4f, 2).x;
            nilo.transform.position = new Vector3(pos.x, sobreLosa ? 0f : 0f, pos.z);

            Vector3 foco = Vector3.Lerp(C(6.5f, 2), nilo.transform.position, 0.4f) + Vector3.down * 0.2f;
            float abrir = Mathf.Clamp01((t - 9.5f) / 3.5f);
            abrir = abrir * abrir * (3f - 2f * abrir);
            Orbita(Mathf.Lerp(-14f, 6f, t / 13f), Mathf.Lerp(24f, 42f, abrir), Mathf.Lerp(11f, 20f, abrir), foco);
            yield return Grabar();
        }
    }

    // ------------------------------------------------------------------ cámara, rótulos, timelapse

    void PrepararCamara()
    {
        GameObject go = new GameObject("Camara maqueta");
        go.tag = "MainCamera";
        cam = go.AddComponent<Camera>();
        cam.fieldOfView = 38f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 250f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = RenderSettings.fogColor;
        var datos = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        datos.renderPostProcessing = true;
        datos.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
    }

    void Orbita(float angulo, float elevacion, float distancia, Vector3 centro)
    {
        float a = angulo * Mathf.Deg2Rad, e = elevacion * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Sin(a) * Mathf.Cos(e), Mathf.Sin(e), -Mathf.Cos(a) * Mathf.Cos(e));
        cam.transform.position = centro + dir * distancia;
        cam.transform.LookAt(centro);
        cam.backgroundColor = RenderSettings.fogColor;
    }

    void PrepararTexto()
    {
        GameObject go = new GameObject("Rotulos");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 0.5f;
        CanvasScaler esc = go.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 0.5f;
        grupoTexto = go.AddComponent<CanvasGroup>();

        Color acento = EsCienaga ? new Color(0.72f, 0.95f, 0.55f) : new Color(1f, 0.55f, 0.2f);
        Transform r = go.transform;
        Text pre = RelevoUI.Texto(r, "Proximamente", Vector2.zero, Vector2.zero, new Vector2(70f, 150f), new Vector2(900f, 34f), 22, TextAnchor.LowerLeft, acento);
        pre.text = "NUEVO DISEÑO DE NIVEL";
        Text titulo = RelevoUI.Texto(r, "Titulo", Vector2.zero, Vector2.zero, new Vector2(66f, 90f), new Vector2(1500f, 70f), 58, TextAnchor.LowerLeft, new Color(0.97f, 0.93f, 0.85f));
        titulo.font = RelevoUI.FuenteTitulo();
        titulo.text = EsCienaga ? "La Ciénaga de los Faroles Ahogados" : "La Forja de Ascuas";
        subtitulo = RelevoUI.Texto(r, "Subtitulo", Vector2.zero, Vector2.zero, new Vector2(70f, 52f), new Vector2(1400f, 34f), 24, TextAnchor.LowerLeft, new Color(0.95f, 0.92f, 0.85f));
        Text leyenda = RelevoUI.Texto(r, "Leyenda", Vector2.one, Vector2.one, new Vector2(-60f, -50f), new Vector2(700f, 34f), 22, TextAnchor.UpperRight, new Color(0.95f, 0.92f, 0.85f, 0.85f));
        leyenda.text = "maqueta · próxima versión de la beta";
        foreach (Text t in new[] { pre, titulo, subtitulo, leyenda })
        {
            Shadow s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.8f);
            s.effectDistance = new Vector2(2f, -2f);
        }
    }

    void PrepararTimelapse()
    {
        var unidades = new HashSet<Transform>();
        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            Transform t = Unidad(r.transform);
            if (t.name == "Mar" || t.name == "Zocalo" || t == raiz) continue;
            unidades.Add(t);
        }
        var lista = new List<Transform>(unidades);
        System.Func<Transform, float> clave = t =>
        {
            Renderer r = t.GetComponentInChildren<Renderer>();
            float fase = t.name == "Terreno" ? 0f : (t.name == "Agua" ? 1f : 2f);
            Vector3 p = r != null ? r.bounds.center : t.position;
            return fase * 1000f + (p.x - p.z);
        };
        lista.Sort((a, b) => clave(a).CompareTo(clave(b)));
        int n = lista.Count;
        for (int i = 0; i < n; i++)
            piezas.Add(new Pieza { t = lista[i], escala = lista[i].localScale, posicion = lista[i].localPosition, inicio = 0.4f + 5.2f * i / n });

        int k = 0;
        foreach (Transform t in actores)
        {
            if (t.GetComponent<Light>() != null) continue;
            piezas.Add(new Pieza { t = t, escala = t.localScale, posicion = t.localPosition, inicio = 5.8f + 0.05f * k++ });
        }
        foreach (Pieza p in piezas) p.t.localScale = Vector3.zero;
    }

    static float RebotarSalida(float x)
    {
        x = Mathf.Clamp01(x);
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    static float Suave(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

    IEnumerator Construccion()
    {
        Vector3 centro = limites.center;
        float radio = Mathf.Max(limites.size.x, limites.size.z) * 1.15f;
        subtitulo.text = EsCienaga ? "Agua estancada, niebla y faroles que nadie enciende" : "Ríos de lava entre muros de basalto";
        Time.timeScale = 3f;
        while (Video < 7.2f)
        {
            float v = Video;
            foreach (Pieza p in piezas)
            {
                float s = (v - p.inicio) / 0.45f;
                if (s <= 0f) { p.t.localScale = Vector3.zero; continue; }
                p.t.localScale = p.escala * Mathf.Max(0f, RebotarSalida(s));
                p.t.localPosition = p.posicion + Vector3.up * (1.6f * (1f - Suave(s)));
            }
            float d = Suave(v / 7.2f);
            if (EsCienaga)
            {
                sol.transform.rotation = Quaternion.Euler(Mathf.Lerp(28f, 14f, d), Mathf.Lerp(-120f, -40f, d), 0f);
                sol.color = Color.Lerp(new Color(0.75f, 0.82f, 0.85f), new Color(0.55f, 0.68f, 0.72f), d);
                sol.intensity = Mathf.Lerp(0.9f, 0.55f, d);
            }
            else
            {
                sol.transform.rotation = Quaternion.Euler(Mathf.Lerp(35f, 22f, d), Mathf.Lerp(-140f, -30f, d), 0f);
                sol.color = Color.Lerp(new Color(1f, 0.7f, 0.5f), new Color(1f, 0.45f, 0.28f), d);
                sol.intensity = Mathf.Lerp(1.1f, 0.75f, d);
            }
            Orbita(Mathf.Lerp(-35f, 20f, d), Mathf.Lerp(55f, 42f, d), radio, centro + Vector3.down * 0.5f);
            grupoTexto.alpha = Suave(v / 0.8f);
            yield return Grabar();
        }
        foreach (Pieza p in piezas) { p.t.localScale = p.escala; p.t.localPosition = p.posicion; }
        Time.timeScale = 1f;
    }

    IEnumerator Final()
    {
        float inicio = Video;
        subtitulo.text = (EsCienaga ? "La Ciénaga de los Faroles Ahogados" : "La Forja de Ascuas") + " · próximamente";
        while (Video - inicio < 2f)
        {
            float s = Suave((Video - inicio) / 2f);
            cam.transform.position += cam.transform.forward * -0.02f;
            grupoTexto.alpha = s > 0.6f ? 1f - Suave((s - 0.6f) / 0.4f) : 1f;
            yield return Grabar();
        }
    }

    // ------------------------------------------------------------------ grabación (solo editor)

#if UNITY_EDITOR
    MediaEncoder codificador;
    RenderTexture lienzo;
    Texture2D imagen;
    string ruta;

    public static string Grabar(string ruta)
    {
        UnityEditor.SessionState.SetString("RelevoMaquetaTemaRuta", ruta);
        Estado = "pendiente";
        return ruta;
    }

    void IniciarGrabacion()
    {
        ruta = UnityEditor.SessionState.GetString("RelevoMaquetaTemaRuta", "");
        UnityEditor.SessionState.EraseString("RelevoMaquetaTemaRuta");
        if (string.IsNullOrEmpty(ruta)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        if (File.Exists(ruta)) File.Delete(ruta);
        codificador = new MediaEncoder(ruta, new VideoTrackAttributes
        {
            frameRate = new MediaRational(Fps), width = 1920, height = 1080,
            includeAlpha = false, bitRateMode = UnityEditor.VideoBitrateMode.High
        });
        lienzo = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
        imagen = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
        cam.targetTexture = lienzo;
        Time.captureFramerate = Fps;
        Estado = "grabando";
    }

    void TerminarGrabacion()
    {
        Time.captureFramerate = 0;
        Time.timeScale = 1f;
        if (codificador == null) { Estado = "terminado sin grabar"; return; }
        codificador.Dispose();
        codificador = null;
        cam.targetTexture = null;
        lienzo.Release();
        Estado = "terminado " + tema + " (" + fotograma + " fotogramas, " + Video.ToString("F1") + " s) " + ruta;
    }
#else
    void IniciarGrabacion() { }
    void TerminarGrabacion() { }
#endif

    IEnumerator Grabar()
    {
        yield return new WaitForEndOfFrame();
#if UNITY_EDITOR
        if (codificador != null)
        {
            RenderTexture previa = RenderTexture.active;
            RenderTexture.active = lienzo;
            imagen.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0, false);
            imagen.Apply(false);
            RenderTexture.active = previa;
            codificador.AddFrame(imagen);
            if (fotograma % 15 == 0)
                File.WriteAllBytes(Path.ChangeExtension(ruta, null) + "_" + fotograma.ToString("000") + ".jpg", imagen.EncodeToJPG(85));
        }
#endif
        fotograma++;
    }
}
