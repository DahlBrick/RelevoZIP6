using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.IO;
using UnityEditor.Media;
#endif

/// <summary>
/// Maqueta de muestra con un adelanto de la siguiente versión de la beta. Al darle Play, el nivel
/// se construye pieza a pieza mientras pasa el día (un timelapse) y después se ve en marcha la
/// mecánica nueva:
///
///  - Mareas: el agua del canal baja y aparecen pasos de piedra; Nilo cruza; la marea sube y el
///    camino desaparece bajo el agua.
///  - Condensadores: un condensador junto a la base se carga; al retirar la batería el puente
///    aguanta unos segundos mientras el condensador se vacía, y Nilo cruza justo a tiempo.
///
/// Son maquetas: no se juegan, solo muestran el diseño. Desde el editor pueden grabarse a MP4
/// (MaquetaFutura.Grabar), sin que eso entre en la build.
/// </summary>
public sealed class MaquetaFutura : MonoBehaviour
{
    public enum Modo { Mareas, Condensadores }

    [SerializeField] Modo modo = Modo.Mareas;

    public static string Estado = "";
    const int Fps = 30;

    // ------------------------------------------------------------------ mapas

    static LevelLayout Mapa(Modo m)
    {
        if (m == Modo.Mareas)
            return new LevelLayout("Maqueta - Mareas de Liria", 2f,
                "v####wwww##v#",
                "#####wwww##E#",
                "##m##wwww####",
                "#####wwww#v##",
                "#v###wwww####",
                "#####wwww#m##",
                "##c##wwww####",
                "v####wwww##v#");

        return new LevelLayout("Maqueta - Los Condensadores", 2f,
            "v###wwwww##v#",
            "####wwwww####",
            "####wwwww##E#",
            "###ABBBBB####",
            "#c##wwwww####",
            "#v##wwwww####",
            "####wwwww####",
            "v###wwwww##v#");
    }

    // ------------------------------------------------------------------ estado

    LevelLayout mapa;
    Bounds limites;
    Transform raiz, actores;
    Camera cam;
    Light sol;
    PlayerController nilo;
    readonly List<Transform> aguas = new List<Transform>();
    readonly List<Vector3> aguasBase = new List<Vector3>();

    struct Pieza { public Transform t; public Vector3 escala, posicion; public float inicio; }
    readonly List<Pieza> piezas = new List<Pieza>();

    Text titulo, subtitulo, leyenda;
    CanvasGroup grupoTexto;
    int fotograma;
    float Video { get { return fotograma / (float)Fps; } }

    // Condensador
    TemporaryBridge puente;
    EnergySocket baseA;
    BatteryController bateria;
    readonly List<Renderer> barras = new List<Renderer>();

    IEnumerator Start()
    {
        ActorFactory.ConfigurarEntorno();
        mapa = Mapa(modo);
        RelevoGame.Layout = mapa;

        GameObject goNivel = new GameObject("Nivel");
        raiz = goNivel.transform;
        goNivel.AddComponent<AmbienteRelevo>();
        limites = DioramaBuilder.Construir(mapa, raiz);

        // La CameraRig deja el volumen global con el perfil del juego; su cámara se apaga.
        CameraRig rig = new GameObject("Camara").AddComponent<CameraRig>();
        rig.Configurar(limites);
        rig.Camara.enabled = false;

        actores = new GameObject("Actores maqueta").transform;
        sol = ActorFactory.CrearSol(actores);
        CrearActores();
        PrepararCamara();
        PrepararTexto();
        PrepararTimelapse();

        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            Renderer r = t.GetComponent<Renderer>();
            if (r == null) continue;
            if (t.name == "Agua" || t.name == "Mar") { aguas.Add(t); aguasBase.Add(t.localPosition); }
        }

        yield return null;
        IniciarGrabacion();

        yield return Construccion();
        if (modo == Modo.Mareas) yield return Mareas();
        else yield return Condensadores();
        yield return Final();

        TerminarGrabacion();
    }

    // ------------------------------------------------------------------ actores

    Vector3 C(float col, int fila) { return mapa.CentroTile(col, fila); }

    void CrearActores()
    {
        KitRelevo kit = KitRelevo.Activo;
        nilo = ActorFactory.CrearNilo(actores, C(1, 1), 90f);
        nilo.enabled = false;
        CharacterController cc = nilo.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        ActorFactory.CrearSalida(actores, C(11, modo == Modo.Mareas ? 1 : 2), "la salida");

        if (modo == Modo.Mareas)
        {
            // Pasos de piedra en el canal: asoman con la marea baja.
            Material piedra = kit != null && kit.piedraJunta != null ? kit.piedraJunta : RelevoPalette.MaterialDe(ClaveMaterial.PiedraBorde);
            for (int c = 5; c <= 8; c++)
            {
                GameObject paso = DioramaBuilder.CrearCilindro("Paso de piedra", actores, C(c, 1) + new Vector3(0f, -0.5f, 0f), 0.62f, 0.7f, piedra);
                paso.transform.localRotation = Quaternion.Euler(0f, c * 37f, 0f);
                paso.transform.localScale = Vector3.Scale(paso.transform.localScale, new Vector3(1f + (c % 2) * 0.12f, 1f, 1f - (c % 2) * 0.08f));
            }

            // Marcador de marea: un poste con franjas, a la orilla.
            Transform poste = DioramaBuilder.CrearAncla("Marcador de marea", actores, C(4, 3) + new Vector3(0.7f, 0f, 0.6f));
            DioramaBuilder.CrearCilindro("Poste", poste, new Vector3(0f, -0.2f, 0f), 0.09f, 2.2f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
            for (int i = 0; i < 4; i++)
                DioramaBuilder.CrearCilindro("Franja", poste, new Vector3(0f, -0.9f + i * 0.35f, 0f), 0.11f, 0.08f,
                    RelevoPalette.MaterialDe(i % 2 == 0 ? ClaveMaterial.Turquesa : ClaveMaterial.Coral));
            return;
        }

        puente = ActorFactory.CrearPuente(actores, mapa, 'B', "B");
        baseA = ActorFactory.CrearBaseEnergia(actores, C(3, 3), "A", "base A", puente, C(4, 3));
        bateria = ActorFactory.CrearBateria(actores, C(2, 2), actores);
        ActorFactory.EngancharBateriaALaMano(bateria, nilo);

        // Condensador: torre oscura con ocho barras que se encienden y se vacían.
        Transform torre = DioramaBuilder.CrearAncla("Condensador", actores, C(2, 3) + new Vector3(0.1f, 0f, -0.2f));
        Material oscuro = kit != null && kit.oscuro != null ? kit.oscuro : RelevoPalette.MaterialDe(ClaveMaterial.MaderaOscura);
        DioramaBuilder.CrearCilindro("Cuerpo", torre, new Vector3(0f, 0.75f, 0f), 0.42f, 1.5f, oscuro);
        DioramaBuilder.CrearCilindro("Remate", torre, new Vector3(0f, 1.55f, 0f), 0.5f, 0.12f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
        DioramaBuilder.CrearCilindro("Base", torre, new Vector3(0f, 0.05f, 0f), 0.55f, 0.1f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
        for (int i = 0; i < 8; i++)
        {
            GameObject b = DioramaBuilder.CrearCaja("Barra", torre, new Vector3(0f, 0.25f + i * 0.15f, -0.43f),
                new Vector3(0.46f, 0.1f, 0.1f), RelevoPalette.MaterialDe(ClaveMaterial.Cobre), false);
            barras.Add(b.GetComponent<Renderer>());
        }
        DioramaBuilder.CrearCajaEntre("Cable", actores, C(2, 3) + new Vector3(0.5f, 0.05f, -0.2f), C(3, 3) + new Vector3(-0.5f, 0.05f, 0f), 0.08f,
            RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
    }

    void PintarBarras(float carga, bool parpadeo)
    {
        int encendidas = Mathf.CeilToInt(carga * barras.Count - 0.001f);
        for (int i = 0; i < barras.Count; i++)
        {
            bool on = i < encendidas && !(parpadeo && i == encendidas - 1 && Mathf.Repeat(Time.time * 6f, 1f) < 0.5f);
            barras[i].sharedMaterial = RelevoPalette.MaterialDe(on ? ClaveMaterial.Ambar : ClaveMaterial.Cobre);
        }
    }

    // ------------------------------------------------------------------ cámara y texto

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

        Transform r = go.transform;
        Text pre = RelevoUI.Texto(r, "Proximamente", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(70f, 150f),
            new Vector2(900f, 34f), 22, TextAnchor.LowerLeft, RelevoUI.Turquesa);
        pre.text = "PRÓXIMAMENTE EN LA BETA";
        titulo = RelevoUI.Texto(r, "Titulo", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(66f, 90f),
            new Vector2(1200f, 70f), 58, TextAnchor.LowerLeft, RelevoUI.AmbarSuave);
        titulo.font = RelevoUI.FuenteTitulo();
        titulo.text = modo == Modo.Mareas ? "Mareas de Liria" : "Los Condensadores";
        subtitulo = RelevoUI.Texto(r, "Subtitulo", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(70f, 52f),
            new Vector2(1400f, 34f), 24, TextAnchor.LowerLeft, new Color(0.95f, 0.92f, 0.85f));
        subtitulo.text = "";
        leyenda = RelevoUI.Texto(r, "Leyenda", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -50f),
            new Vector2(700f, 34f), 22, TextAnchor.UpperRight, new Color(0.95f, 0.92f, 0.85f, 0.85f));
        leyenda.text = "maqueta · nivel en construcción";
        foreach (Text t in new[] { pre, titulo, subtitulo, leyenda })
        {
            Shadow s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.75f);
            s.effectDistance = new Vector2(2f, -2f);
        }
    }

    // ------------------------------------------------------------------ timelapse de construcción

    static readonly HashSet<string> Grupos = new HashSet<string>
    {
        "Suelo", "Agua", "Bloqueos", "Detalles", "Fondo", "Vestido suelo", "Vestido plantas",
        "Vestido orillas", "Vestido puerto", "Vestido malecon", "Barcas"
    };

    void PrepararTimelapse()
    {
        var unidades = new HashSet<Transform>();
        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            Transform t = r.transform;
            if (t.name == "Mar" || t.name == "Zocalo") continue;
            while (t.parent != null && t.parent != raiz && !Grupos.Contains(t.parent.name)) t = t.parent;
            if (t.name == "Mar" || t.name == "Zocalo") continue;
            unidades.Add(t);
        }

        var lista = new List<Transform>(unidades);
        // Primero el suelo, luego el agua, luego lo demás; cada grupo barre de noroeste a sureste.
        System.Func<Transform, float> clave = t =>
        {
            Renderer r = t.GetComponentInChildren<Renderer>();
            float fase = t.name == "Terreno" || t.name == "Zocalo" ? 0f : (t.name == "Agua" ? 1f : 2f);
            Vector3 p = r != null ? r.bounds.center : t.position;
            return fase * 1000f + (p.x - p.z);
        };
        lista.Sort((a, b) => clave(a).CompareTo(clave(b)));

        int n = lista.Count;
        for (int i = 0; i < n; i++)
            piezas.Add(new Pieza { t = lista[i], escala = lista[i].localScale, posicion = lista[i].localPosition, inicio = 0.4f + 5.2f * i / n });

        // Las piezas del puzle llegan al final, en un último golpe.
        int k = 0;
        foreach (Transform t in actores)
        {
            if (t.GetComponent<Light>() != null) continue;
            piezas.Add(new Pieza { t = t, escala = t.localScale, posicion = t.localPosition, inicio = 5.8f + 0.06f * k++ });
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
        subtitulo.text = modo == Modo.Mareas ? "Un nivel que cambia con el agua" : "La energía que se queda unos segundos";

        // El tiempo del mundo corre más rápido: las barcas, el agua y el sol aceleran.
        Time.timeScale = 3f;
        while (Video < 7.2f)
        {
            float v = Video;
            foreach (Pieza p in piezas)
            {
                float s = (v - p.inicio) / 0.45f;
                if (s <= 0f) { p.t.localScale = Vector3.zero; continue; }
                float e = RebotarSalida(s);
                p.t.localScale = p.escala * Mathf.Max(0f, e);
                p.t.localPosition = p.posicion + Vector3.up * (1.6f * (1f - Suave(s)));
            }

            // Pasa el día: el sol barre de la mañana a la tarde dorada.
            float d = Suave(v / 7.2f);
            sol.transform.rotation = Quaternion.Euler(Mathf.Lerp(20f, 38f, d), Mathf.Lerp(-150f, -38f, d), 0f);
            sol.color = Color.Lerp(new Color(0.85f, 0.9f, 1f), new Color(1f, 0.87f, 0.7f), d);

            Orbita(Mathf.Lerp(-35f, 20f, d), Mathf.Lerp(55f, 42f, d), radio, centro + Vector3.down * 0.5f);
            grupoTexto.alpha = Suave(v / 0.8f);
            yield return Grabar();
        }

        foreach (Pieza p in piezas) { p.t.localScale = p.escala; p.t.localPosition = p.posicion; }
        Time.timeScale = 1f;
    }

    // ------------------------------------------------------------------ mareas

    void Marea(float desplazamiento)
    {
        for (int i = 0; i < aguas.Count; i++) aguas[i].localPosition = aguasBase[i] + Vector3.up * desplazamiento;
    }

    IEnumerator Mareas()
    {
        // La marea solo sube desde el nivel de juego: así el canal nunca queda tan poco hondo que
        // la espuma del agua lo cubra todo. Alta (+0,35) tapa los pasos; normal los descubre.
        const float Alta = 0.35f;
        float inicio = Video;
        Vector3[] ruta = { C(1, 1), C(4, 1), C(5, 1), C(8, 1), C(9, 1), C(10.6f, 1) };
        int tramo = 0;
        Vector3 pos = ruta[0];

        while (Video - inicio < 12.5f)
        {
            float t = Video - inicio;
            float marea;
            if (t < 2.2f) { marea = Mathf.Lerp(0f, Alta, Suave(t / 2.2f)); subtitulo.text = "Marea alta: el canal no se puede cruzar"; }
            else if (t < 3.2f) marea = Alta;
            else if (t < 5.4f) { marea = Mathf.Lerp(Alta, 0f, Suave((t - 3.2f) / 2.2f)); subtitulo.text = "Marea baja: asoman los pasos de piedra"; }
            else if (t < 9f) { marea = 0f; subtitulo.text = "Cruza mientras el agua lo permite"; }
            else { marea = Mathf.Lerp(0f, Alta, Suave((t - 9f) / 2.6f)); subtitulo.text = "La marea vuelve y el camino desaparece"; }
            Marea(marea);

            // La luz acompaña a la marea: azulada con el agua alta, dorada con la baja.
            float azul = marea / Alta;
            sol.color = Color.Lerp(new Color(1f, 0.82f, 0.62f), new Color(0.62f, 0.72f, 0.95f), azul);
            sol.intensity = Mathf.Lerp(1.65f, 1.15f, azul);

            if (t > 5.2f && tramo < ruta.Length - 1)
            {
                Vector3 objetivo = ruta[tramo + 1];
                pos = Vector3.MoveTowards(pos, objetivo, Time.deltaTime * 3.2f);
                if ((pos - objetivo).sqrMagnitude < 0.001f) tramo++;
                Vector3 dir = objetivo - pos; dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f) nilo.transform.rotation = Quaternion.Slerp(nilo.transform.rotation, Quaternion.LookRotation(dir), 0.2f);
            }
            bool sobrePiedra = pos.x > C(4.6f, 1).x && pos.x < C(8.4f, 1).x;
            nilo.transform.position = new Vector3(pos.x, sobrePiedra ? -0.15f : 0f, pos.z);

            // Cámara: de lado al canal, baja y cerca; sigue a Nilo y al final se abre.
            Vector3 foco = Vector3.Lerp(C(6.5f, 1), nilo.transform.position, t > 5.2f ? 0.5f : 0f) + Vector3.down * 0.3f;
            float abrir = Suave((t - 9.2f) / 3f);
            Orbita(Mathf.Lerp(-12f, -24f, t / 12.5f), Mathf.Lerp(24f, 44f, abrir), Mathf.Lerp(11f, 19f, abrir), foco + Vector3.back * 2f * abrir);
            yield return Grabar();
        }
    }

    // ------------------------------------------------------------------ condensadores

    IEnumerator Condensadores()
    {
        float inicio = Video;
        Vector3[] ruta = { C(2.6f, 2), C(4, 2), C(4.6f, 3), C(9.4f, 3), C(10, 3), C(11, 2) };
        int tramo = 0;
        Vector3 pos = C(2.2f, 2);
        nilo.transform.position = pos;
        bateria.Sujetar(nilo);
        bool insertada = false, retirada = false, caido = false;
        float carga = 0f;

        while (Video - inicio < 12f)
        {
            float t = Video - inicio;

            if (!insertada && t > 0.6f) { baseA.Interactuar(nilo); insertada = true; subtitulo.text = "La batería despliega el puente y carga el condensador"; }
            if (insertada && !retirada) carga = Mathf.Clamp01((t - 0.8f) / 2.2f);

            if (!retirada && t > 3.6f)
            {
                baseA.Interactuar(nilo);         // retirar: el puente se replegaría...
                puente.Activar(true);            // ...pero el condensador lo sostiene
                retirada = true;
                subtitulo.text = "Sin batería, el puente aguanta lo que dure la carga";
            }
            if (retirada && !caido)
            {
                carga = Mathf.Clamp01(1f - (t - 3.6f) / 4.2f);
                if (carga <= 0f) { puente.Activar(false); caido = true; subtitulo.text = "Planea la ruta: la carga no espera"; }
            }
            PintarBarras(carga, retirada && !caido && carga < 0.35f);

            // Nilo: se acerca, retira la batería y cruza a la carrera.
            if (t > 3.7f && tramo < ruta.Length - 1)
            {
                Vector3 objetivo = ruta[tramo + 1];
                pos = Vector3.MoveTowards(pos, objetivo, Time.deltaTime * 3.4f);
                if ((pos - objetivo).sqrMagnitude < 0.001f) tramo++;
                Vector3 dir = objetivo - pos; dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f) nilo.transform.rotation = Quaternion.Slerp(nilo.transform.rotation, Quaternion.LookRotation(dir), 0.25f);
            }
            else if (t <= 3.7f)
            {
                Vector3 haciaBase = C(3, 3) - pos; haciaBase.y = 0f;
                nilo.transform.rotation = Quaternion.Slerp(nilo.transform.rotation, Quaternion.LookRotation(haciaBase), 0.15f);
            }
            nilo.transform.position = pos;

            // Cámara: empieza en el condensador y acompaña el cruce desde el lado del canal.
            Vector3 torre = C(2, 3) + Vector3.up * 0.9f;
            if (t < 3.6f)
                Orbita(Mathf.Lerp(-20f, -5f, t / 3.6f), 20f, 7f, Vector3.Lerp(torre, C(3f, 3), 0.5f));
            else
            {
                float s = Suave((t - 3.6f) / 4f);
                Vector3 foco = Vector3.Lerp(C(4f, 3), C(8.5f, 3), s);
                float abrir = Suave((t - 8.5f) / 3f);
                Orbita(Mathf.Lerp(-15f, 10f, s), Mathf.Lerp(26f, 42f, abrir), Mathf.Lerp(12f, 21f, abrir), foco);
            }
            yield return Grabar();
        }
    }

    IEnumerator Final()
    {
        float inicio = Video;
        subtitulo.text = modo == Modo.Mareas ? "Mareas de Liria · siguiente versión de la beta" : "Los Condensadores · siguiente versión de la beta";
        while (Video - inicio < 2f)
        {
            float s = Suave((Video - inicio) / 2f);
            cam.transform.position += cam.transform.forward * -0.02f;
            grupoTexto.alpha = 1f;
            if (s > 0.6f) grupoTexto.alpha = 1f - Suave((s - 0.6f) / 0.4f);
            yield return Grabar();
        }
    }

    // ------------------------------------------------------------------ grabación (solo editor)

#if UNITY_EDITOR
    MediaEncoder codificador;
    RenderTexture lienzo;
    Texture2D imagen;
    string ruta;

    /// <summary>Graba la maqueta de la escena abierta en cuanto arranque (llamar antes de Play o justo después).</summary>
    public static string Grabar(string ruta)
    {
        // Sobrevive a la recarga de código al entrar en Play.
        UnityEditor.SessionState.SetString("RelevoMaquetaRuta", ruta);
        Estado = "pendiente";
        return ruta;
    }

    void IniciarGrabacion()
    {
        ruta = UnityEditor.SessionState.GetString("RelevoMaquetaRuta", "");
        UnityEditor.SessionState.EraseString("RelevoMaquetaRuta");
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
        Estado = "terminado " + modo + " (" + fotograma + " fotogramas, " + Video.ToString("F1") + " s) " + ruta;
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
