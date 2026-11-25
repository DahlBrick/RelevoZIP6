using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cinemática de decisión 2 · El banco de fundición (docs/narrativa/plan_cinematicas.md, §2).
/// Se juega al pulsar «Siguiente nivel» tras el Puerto 12 y termina cargando el Puerto 13.
///
/// 1. El varadero en silencio: las dos campanas se mecen sin sonar; al fondo brilla el banco.
/// 2. Nilo se acerca. El molde con forma de disco de memoria y lo raspado en el borde.
/// 3. Montaje en el vacío: los recuerdos 7-10 como objetos (los que faltan, huecos negros).
/// 4. El reloj a las 00:17 y el agua que sube. Negro.
/// 5. La radio de Maia, muy rota.
/// 6. Elección: Recordarlo todo / Fundir el disco otra vez (SaveManager.Decidir(2, ...)).
/// 7. Resolución de cada opción y, a lo lejos, la subestación que ruge.
///
/// Como la apertura, toda la puesta en escena es función del tiempo T. Se salta manteniendo Esc:
/// antes de la elección salta a la elección (la decisión no se puede saltar); después, termina.
/// Sin personas ni sombras de habitantes; Nilo no habla; Maia es una radio.
/// </summary>
public sealed class CinematicaFundicion : MonoBehaviour
{
    public const string Escena = "Cinematica_Fundicion";

    [SerializeField] InputActionAsset acciones;
    [SerializeField] string escenaSiguiente = "Nivel13_LaSalaDelRelevo";

    // ------------------------------------------------------------------ línea de tiempo
    const float P_Varadero = 0f, P_Acercarse = 9f, P_Molde = 17f, P_Montaje = 24f, DuracionRecuerdo = 5f,
        P_Reloj = 44f, P_Radio = 52f, P_Vuelta = 61f, P_Eleccion = 65f, P_Resolucion = 65.2f, Fin = 78f;

    public static string Estado = "";
    float T;
    bool esperandoEleccion, terminado;
    int eleccion = -1;            // 0 recordar, 1 borrar
    bool radioDicha;

    // ------------------------------------------------------------------ piezas
    RelevoInput entrada;
    Camera cam;
    Vignette vineta;
    ColorAdjustments color;
    Bloom bloom;
    Light luna, brasa, caraNilo, foco, pilotoRadio, horizonte;
    Transform campanaA, campanaB, molde, disco, aguaReloj, agujaHora, agujaMinuto, radio;
    Material matBrasa, matDisco, matSubestacion;
    readonly List<Material> pantallaNilo = new List<Material>();
    PlayerController nilo;
    Vector3 inicioNilo, frenteBanco;
    readonly List<GameObject> recuerdos = new List<GameObject>();
    readonly bool[] tieneRecuerdo = new bool[4];
    Image fundido, destello, bandaArriba, bandaAbajo, avisoBarra;
    Text rotulo, cita, pie, aviso;
    DialogoUI dialogo;
    AudioSource agua, estatica, efectos, musica;
    AudioClip clipGolpe, clipRele, clipPitido;
    float mantenerSalto, avisoVisible, sacudida;
    bool flanco;

    static readonly Vector3 Vacio = new Vector3(200f, 0f, 0f);   // donde ocurre el montaje
    static readonly string[] TitulosRecuerdo =
    {
        "Recuerdo 7 · Las dos etiquetas", "Recuerdo 8 · Cinta de las 00:17",
        "Recuerdo 9 · Ficha de embarque partida", "Recuerdo 10 · El renglón del operador"
    };
    static readonly string[] PiesRecuerdo =
    {
        "«RADIO», pegada encima de «BOMBA DE LA LONJA».",
        "Dos interruptores al bajar. Después, su voz.",
        "«Se fue la luz y no pude volver».",
        "«00:17. Carga de las bombas desviada a la emisora. Operador de guardia: NILO»."
    };

    // ------------------------------------------------------------------ arranque

    System.Collections.IEnumerator Start()
    {
        Application.targetFrameRate = 60;
        Time.timeScale = 1f;
        GameEvents.Limpiar();
        RelevoGame.Limpiar();
        entrada = acciones != null ? new RelevoInput(acciones) : null;

        for (int i = 0; i < 4; i++) tieneRecuerdo[i] = SaveManager.TieneRecuerdo("nivel" + (7 + i) + "_recuerdo");

        Ambiente();
        PrepararCamara();
        ConstruirVaradero();
        ConstruirVacio();
        PrepararInterfaz();
        PrepararSonido();

        inicioNilo = new Vector3(0f, 0f, -6.5f);
        nilo = ActorFactory.CrearNilo(transform, inicioNilo, 0f);
        nilo.enabled = false;
        var interactor = nilo.GetComponent<PlayerInteractor>();
        if (interactor != null) interactor.enabled = false;
        var cc = nilo.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        caraNilo = Luz("Pantalla de Nilo", nilo.transform, new Vector3(0f, 1.9f, 0.6f), RelevoUI.Turquesa, 0f, 2.2f);
        yield return null;

        EstiloComic.Aplicar(transform, new Color(0.3f, 0.32f, 0.5f));
        EstiloComic.RendererTinta(cam);
        // La pantalla de la cara de Nilo se enciende al recordar: se busca su material ya en cómic.
        foreach (Renderer rd in nilo.GetComponentsInChildren<Renderer>(true))
            foreach (Material m in rd.materials)
                if (m != null && m.name.Contains("Pantalla") && m.HasProperty("_EmissionColor")) pantallaNilo.Add(m);
        Evaluar(0f, 0f, 0f);
    }

    void Ambiente()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.22f, 0.26f, 0.42f);
        RenderSettings.ambientEquatorColor = new Color(0.14f, 0.15f, 0.24f);
        RenderSettings.ambientGroundColor = new Color(0.06f, 0.06f, 0.09f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.04f, 0.06f, 0.11f);
        RenderSettings.fogStartDistance = 18f;
        RenderSettings.fogEndDistance = 110f;
        RenderSettings.skybox = null;

        luna = new GameObject("Luna").AddComponent<Light>();
        luna.transform.SetParent(transform, false);
        luna.type = LightType.Directional;
        luna.color = new Color(0.55f, 0.62f, 0.9f);
        luna.intensity = 1.05f;
        luna.shadows = LightShadows.Soft;
        luna.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
    }

    void PrepararCamara()
    {
        var go = new GameObject("Camara de cine");
        go.transform.SetParent(transform, false);
        go.tag = "MainCamera";
        cam = go.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 400f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = RenderSettings.fogColor;
        go.AddComponent<AudioListener>();
        var datos = go.AddComponent<UniversalAdditionalCameraData>();
        datos.renderPostProcessing = true;
        datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        var v = new GameObject("Volumen de cine").AddComponent<Volume>();
        v.transform.SetParent(transform, false);
        v.isGlobal = true;
        v.priority = 100f;
        var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        perfil.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
        bloom = perfil.Add<Bloom>(true);
        bloom.threshold.Override(0.8f);
        bloom.intensity.Override(1.3f);
        bloom.scatter.Override(0.7f);
        vineta = perfil.Add<Vignette>(true);
        vineta.smoothness.Override(0.5f);
        color = perfil.Add<ColorAdjustments>(true);
        var grano = perfil.Add<FilmGrain>(true);
        grano.type.Override(FilmGrainLookup.Thin1);
        grano.intensity.Override(0.25f);
        v.sharedProfile = perfil;
    }

    // ------------------------------------------------------------------ decorados

    static Material M(ClaveMaterial c) { return RelevoPalette.MaterialDe(c); }

    /// <summary>Material propio que brilla (el comic conserva la emisión).</summary>
    static Material Brillo(Color baseColor, Color emision)
    {
        var m = new Material(M(ClaveMaterial.Ambar));
        m.color = baseColor;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emision);
        return m;
    }

    Light Luz(string nombre, Transform padre, Vector3 local, Color c, float intensidad, float alcance)
    {
        var l = new GameObject(nombre).AddComponent<Light>();
        l.transform.SetParent(padre, false);
        l.transform.localPosition = local;
        l.type = LightType.Point;
        l.color = c;
        l.intensity = intensidad;
        l.range = alcance;
        l.shadows = LightShadows.None;
        return l;
    }

    GameObject Caja(string n, Transform p, Vector3 c, Vector3 s, Material m) { return DioramaBuilder.CrearCaja(n, p, c, s, m, false); }

    void ConstruirVaradero()
    {
        Transform r = new GameObject("Varadero").transform;
        r.SetParent(transform, false);

        // Agua y tablado.
        Caja("Agua", r, new Vector3(0f, -0.6f, 0f), new Vector3(160f, 0.2f, 160f), M(ClaveMaterial.Agua));
        for (int i = 0; i < 16; i++)
            Caja("Tablon", r, new Vector3(0f, -0.1f, -8f + i * 1.02f), new Vector3(12f, 0.2f, 0.96f), M(ClaveMaterial.MaderaOscura));
        for (int i = 0; i < 6; i++)
        {
            float x = -5.8f + i * 2.32f;
            DioramaBuilder.CrearCilindro("Pilote", r, new Vector3(x, -0.6f, -8.3f), 0.18f, 1.2f, M(ClaveMaterial.MaderaOscura));
        }
        // Baranda baja en los bordes.
        Caja("Baranda izquierda", r, new Vector3(-6f, 0.5f, 0f), new Vector3(0.12f, 0.1f, 16f), M(ClaveMaterial.MaderaOscura));
        Caja("Baranda derecha", r, new Vector3(6f, 0.5f, 0f), new Vector3(0.12f, 0.1f, 16f), M(ClaveMaterial.MaderaOscura));

        campanaA = Campana(r, new Vector3(-4.2f, 0f, -1.5f));
        campanaB = Campana(r, new Vector3(4.2f, 0f, 1.5f));

        // Barcas varadas, vacías.
        Caja("Barca", r, new Vector3(-8.5f, -0.35f, 2f), new Vector3(1.4f, 0.45f, 3.6f), M(ClaveMaterial.Coral));
        Caja("Barca 2", r, new Vector3(8.8f, -0.35f, -3f), new Vector3(1.3f, 0.4f, 3.2f), M(ClaveMaterial.Turquesa));

        // El banco de fundición, al fondo.
        Transform banco = new GameObject("Banco de fundicion").transform;
        banco.SetParent(r, false);
        banco.localPosition = new Vector3(0f, 0f, 5.5f);
        Caja("Banco", banco, new Vector3(0f, 0.55f, 0f), new Vector3(2.8f, 1.1f, 1.4f), M(ClaveMaterial.PiedraBorde));
        Caja("Tapa", banco, new Vector3(0f, 1.14f, 0f), new Vector3(3f, 0.1f, 1.6f), M(ClaveMaterial.Cobre));
        Caja("Horno", banco, new Vector3(0f, 1.6f, 1.2f), new Vector3(2.2f, 2.2f, 1f), M(ClaveMaterial.PiedraBorde));
        matBrasa = Brillo(new Color(1f, 0.45f, 0.15f), new Color(2.2f, 0.7f, 0.15f));
        Caja("Boca del horno", banco, new Vector3(0f, 1.45f, 0.69f), new Vector3(1.1f, 0.7f, 0.04f), matBrasa);
        Caja("Chimenea", banco, new Vector3(0.6f, 3.4f, 1.3f), new Vector3(0.5f, 1.6f, 0.5f), M(ClaveMaterial.PiedraBorde));
        Caja("Tenazas", banco, new Vector3(-1.1f, 1.22f, -0.2f), new Vector3(0.7f, 0.05f, 0.08f), M(ClaveMaterial.NiloExtremidades));

        molde = DioramaBuilder.CrearCilindro("Molde", banco, new Vector3(0.2f, 1.24f, -0.2f), 0.42f, 0.12f, M(ClaveMaterial.MaderaOscura)).transform;
        matDisco = Brillo(new Color(1f, 0.6f, 0.25f), new Color(1.2f, 0.45f, 0.1f));
        disco = DioramaBuilder.CrearCilindro("Disco a medio enfriar", banco, new Vector3(0.2f, 1.31f, -0.2f), 0.3f, 0.03f, matDisco).transform;
        frenteBanco = banco.position + new Vector3(0f, 0f, -1.7f);

        brasa = Luz("Brasa", banco, new Vector3(0f, 1.6f, -0.6f), new Color(1f, 0.55f, 0.2f), 3f, 7f);

        // A lo lejos, la subestación: solo un resplandor en el horizonte.
        horizonte = Luz("Subestacion", r, new Vector3(0f, 10f, 75f), new Color(1f, 0.25f, 0.2f), 0f, 60f);
        Caja("Subestacion", r, new Vector3(6f, 6f, 80f), new Vector3(30f, 12f, 6f), M(ClaveMaterial.Fondo));
        Caja("Torre", r, new Vector3(-6f, 12f, 80f), new Vector3(2.5f, 24f, 2.5f), M(ClaveMaterial.Fondo));
        Caja("Torre 2", r, new Vector3(18f, 10f, 80f), new Vector3(2f, 20f, 2f), M(ClaveMaterial.Fondo));
        matSubestacion = Brillo(new Color(0.2f, 0.05f, 0.05f), Color.black);
        for (int i = 0; i < 7; i++)
            Caja("Ventanal", r, new Vector3(-6f + i * 4f, 7f, 76.9f), new Vector3(2.2f, 3.5f, 0.1f), matSubestacion);
    }

    Transform Campana(Transform padre, Vector3 pos)
    {
        Caja("Poste", padre, pos + new Vector3(-0.6f, 1.5f, 0f), new Vector3(0.18f, 3f, 0.18f), M(ClaveMaterial.MaderaOscura));
        Caja("Poste", padre, pos + new Vector3(0.6f, 1.5f, 0f), new Vector3(0.18f, 3f, 0.18f), M(ClaveMaterial.MaderaOscura));
        Caja("Travesano", padre, pos + new Vector3(0f, 3f, 0f), new Vector3(1.5f, 0.18f, 0.22f), M(ClaveMaterial.MaderaOscura));
        Transform eje = new GameObject("Campana").transform;
        eje.SetParent(padre, false);
        eje.localPosition = pos + new Vector3(0f, 2.9f, 0f);
        DioramaBuilder.CrearCilindro("Cuerpo", eje, new Vector3(0f, -0.45f, 0f), 0.32f, 0.55f, M(ClaveMaterial.Cobre));
        DioramaBuilder.CrearEsfera("Corona", eje, new Vector3(0f, -0.18f, 0f), 0.5f, M(ClaveMaterial.Cobre));
        DioramaBuilder.CrearEsfera("Badajo", eje, new Vector3(0f, -0.78f, 0f), 0.16f, M(ClaveMaterial.PiedraBorde));
        return eje;
    }

    /// <summary>El vacío del montaje: un suelo negro, un foco y los objetos de los recuerdos 7-10.</summary>
    void ConstruirVacio()
    {
        Transform r = new GameObject("Vacio").transform;
        r.SetParent(transform, false);
        r.position = Vacio;
        Caja("Suelo", r, new Vector3(0f, -0.1f, 0f), new Vector3(30f, 0.2f, 30f), M(ClaveMaterial.Fondo));

        foco = new GameObject("Foco").AddComponent<Light>();
        foco.transform.SetParent(r, false);
        foco.transform.localPosition = new Vector3(0f, 5f, -1f);
        foco.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
        foco.type = LightType.Spot;
        foco.spotAngle = 42f;
        foco.range = 12f;
        foco.intensity = 0f;
        foco.color = new Color(1f, 0.92f, 0.8f);
        foco.shadows = LightShadows.Soft;

        // 7 · Las dos etiquetas: un tablero con dos placas, una encima de la otra.
        var o = new GameObject("Recuerdo 7").transform; o.SetParent(r, false);
        Caja("Tablero", o, new Vector3(0f, 1f, 0f), new Vector3(1.6f, 1.2f, 0.12f), M(ClaveMaterial.Maqueta));
        Caja("Etiqueta de abajo", o, new Vector3(0.05f, 1.05f, -0.08f), new Vector3(0.9f, 0.22f, 0.02f), M(ClaveMaterial.PiedraCrema));
        var arriba = Caja("Etiqueta RADIO", o, new Vector3(0f, 1.12f, -0.1f), new Vector3(0.7f, 0.2f, 0.02f), M(ClaveMaterial.Coral));
        arriba.transform.localRotation = Quaternion.Euler(0f, 0f, 6f);
        for (int i = 0; i < 3; i++)
            DioramaBuilder.CrearCilindro("Borne", o, new Vector3(-0.5f + i * 0.5f, 0.6f, -0.1f), 0.06f, 0.1f, M(ClaveMaterial.Cobre)).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        recuerdos.Add(o.gameObject);

        // 8 · La cinta: dos carretes y la caja.
        o = new GameObject("Recuerdo 8").transform; o.SetParent(r, false);
        Caja("Magnetofono", o, new Vector3(0f, 0.25f, 0f), new Vector3(1.4f, 0.5f, 0.9f), M(ClaveMaterial.MaiaCarcasa));
        DioramaBuilder.CrearCilindro("Carrete A", o, new Vector3(-0.35f, 0.55f, 0f), 0.28f, 0.08f, M(ClaveMaterial.PiedraBorde));
        DioramaBuilder.CrearCilindro("Carrete B", o, new Vector3(0.35f, 0.55f, 0f), 0.28f, 0.08f, M(ClaveMaterial.PiedraBorde));
        Caja("Cinta", o, new Vector3(0f, 0.58f, 0.2f), new Vector3(0.7f, 0.02f, 0.02f), M(ClaveMaterial.MaderaOscura));
        recuerdos.Add(o.gameObject);

        // 9 · La ficha partida: dos mitades separadas.
        o = new GameObject("Recuerdo 9").transform; o.SetParent(r, false);
        var a = Caja("Mitad A", o, new Vector3(-0.32f, 0.05f, 0f), new Vector3(0.55f, 0.03f, 0.8f), M(ClaveMaterial.PiedraCrema));
        a.transform.localRotation = Quaternion.Euler(0f, -8f, 0f);
        var b = Caja("Mitad B", o, new Vector3(0.36f, 0.05f, 0.05f), new Vector3(0.55f, 0.03f, 0.8f), M(ClaveMaterial.PiedraCrema));
        b.transform.localRotation = Quaternion.Euler(0f, 10f, 0f);
        Caja("Trazo", o, new Vector3(-0.32f, 0.075f, 0f), new Vector3(0.05f, 0.01f, 0.5f), M(ClaveMaterial.Coral));
        recuerdos.Add(o.gameObject);

        // 10 · El libro de guardia abierto, con un renglón marcado.
        o = new GameObject("Recuerdo 10").transform; o.SetParent(r, false);
        var p1 = Caja("Pagina izquierda", o, new Vector3(-0.42f, 0.08f, 0f), new Vector3(0.8f, 0.05f, 1.1f), M(ClaveMaterial.PiedraCrema));
        p1.transform.localRotation = Quaternion.Euler(0f, 0f, 6f);
        var p2 = Caja("Pagina derecha", o, new Vector3(0.42f, 0.08f, 0f), new Vector3(0.8f, 0.05f, 1.1f), M(ClaveMaterial.PiedraCrema));
        p2.transform.localRotation = Quaternion.Euler(0f, 0f, -6f);
        for (int i = 0; i < 6; i++)
            Caja("Renglon", o, new Vector3(0.42f, 0.115f, 0.4f - i * 0.15f), new Vector3(0.6f, 0.005f, 0.02f), i == 3 ? Brillo(new Color(1f, 0.3f, 0.25f), new Color(1.5f, 0.2f, 0.15f)) : M(ClaveMaterial.MaderaOscura));
        recuerdos.Add(o.gameObject);

        // El reloj de las 00:17 y el agua que lo cubre.
        var reloj = new GameObject("Reloj").transform; reloj.SetParent(r, false);
        reloj.localPosition = new Vector3(0f, 0f, 40f);
        var esfera = DioramaBuilder.CrearCilindro("Esfera", reloj, new Vector3(0f, 1.2f, 0f), 0.9f, 0.08f, M(ClaveMaterial.PiedraCrema));
        esfera.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        DioramaBuilder.CrearCilindro("Marco", reloj, new Vector3(0f, 1.2f, 0.03f), 0.98f, 0.06f, M(ClaveMaterial.Cobre)).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        agujaHora = new GameObject("Aguja hora").transform; agujaHora.SetParent(reloj, false); agujaHora.localPosition = new Vector3(0f, 1.2f, -0.06f);
        Caja("Hora", agujaHora, new Vector3(0f, 0.22f, 0f), new Vector3(0.06f, 0.44f, 0.02f), M(ClaveMaterial.MaderaOscura));
        agujaMinuto = new GameObject("Aguja minuto").transform; agujaMinuto.SetParent(reloj, false); agujaMinuto.localPosition = new Vector3(0f, 1.2f, -0.07f);
        Caja("Minuto", agujaMinuto, new Vector3(0f, 0.33f, 0f), new Vector3(0.04f, 0.66f, 0.02f), M(ClaveMaterial.Coral));
        Caja("Mostrador", reloj, new Vector3(0f, 0.3f, 0.5f), new Vector3(3f, 0.6f, 0.6f), M(ClaveMaterial.MaderaOscura));
        aguaReloj = Caja("Agua que sube", reloj, new Vector3(0f, -0.2f, -0.5f), new Vector3(8f, 0.3f, 4f), M(ClaveMaterial.Agua)).transform;
        Luz("Luz del reloj", reloj, new Vector3(0f, 2.5f, -2f), new Color(0.7f, 0.8f, 1f), 2.5f, 8f);

        // La radio de Maia en la oscuridad.
        radio = new GameObject("Radio de Maia").transform; radio.SetParent(r, false);
        radio.localPosition = new Vector3(0f, 0f, 80f);
        Caja("Caja", radio, new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.6f, 0.45f), M(ClaveMaterial.MaiaCarcasa));
        Caja("Altavoz", radio, new Vector3(-0.15f, 0.4f, -0.23f), new Vector3(0.4f, 0.4f, 0.02f), M(ClaveMaterial.MaiaPantalla));
        DioramaBuilder.CrearEsfera("Piloto", radio, new Vector3(0.28f, 0.55f, -0.23f), 0.08f, Brillo(RelevoUI.Verde, RelevoUI.Verde * 1.5f));
        Caja("Antena", radio, new Vector3(0.3f, 0.95f, 0f), new Vector3(0.03f, 0.6f, 0.03f), M(ClaveMaterial.Cobre)).transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        pilotoRadio = Luz("Luz del piloto", radio, new Vector3(0.28f, 0.6f, -0.6f), RelevoUI.Verde, 0f, 3f);

        for (int i = 0; i < recuerdos.Count; i++) recuerdos[i].SetActive(false);
    }

    // ------------------------------------------------------------------ interfaz

    void PrepararInterfaz()
    {
        var go = new GameObject("Interfaz", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var esc = go.GetComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 1f;
        Transform r = go.transform;

        float banda = (1080f - 1920f / 2.39f) * 0.5f;
        bandaArriba = Bloque(r, "Banda arriba", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), banda, Color.black);
        bandaAbajo = Bloque(r, "Banda abajo", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), banda, Color.black);

        rotulo = RelevoUI.Texto(r, "Rotulo", Vector2.zero, Vector2.zero, new Vector2(110f, banda + 46f), new Vector2(1100f, 50f), 34, TextAnchor.LowerLeft, Transparente(RelevoUI.Tinta));
        rotulo.font = RelevoUI.FuenteTitulo();
        pie = RelevoUI.Texto(r, "Pie", Vector2.zero, Vector2.zero, new Vector2(110f, banda + 8f), new Vector2(1500f, 36f), 24, TextAnchor.LowerLeft, Transparente(RelevoUI.Tinta));
        cita = RelevoUI.Texto(r, "Cita", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -250f), new Vector2(1500f, 60f), 40, TextAnchor.MiddleCenter, Transparente(RelevoUI.Ambar));
        cita.font = RelevoUI.Fuente();
        cita.fontStyle = FontStyle.Bold;

        destello = Bloque(r, "Destello", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), 0f, Color.clear);
        destello.rectTransform.offsetMin = destello.rectTransform.offsetMax = Vector2.zero;
        destello.transform.SetSiblingIndex(2);   // bajo los textos: el recuerdo falso se lee encima
        fundido = Bloque(r, "Fundido", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), 0f, Color.black);
        fundido.rectTransform.offsetMin = fundido.rectTransform.offsetMax = Vector2.zero;

        dialogo = DialogoUI.Crear(r);

        aviso = RelevoUI.Texto(r, "Aviso saltar", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 40f), new Vector2(520f, 36f), 22, TextAnchor.LowerRight, Transparente(RelevoUI.Tinta));
        aviso.text = "Mantén Esc para saltar";
        avisoBarra = Bloque(r, "Aviso barra", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), 3f, RelevoUI.Ambar);
        avisoBarra.rectTransform.anchoredPosition = new Vector2(-60f, 30f);
        avisoBarra.rectTransform.sizeDelta = new Vector2(0f, 3f);
    }

    static Color Transparente(Color c) { c.a = 0f; return c; }

    Image Bloque(Transform padre, string nombre, Vector2 min, Vector2 max, Vector2 pivote, float alto, Color c)
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        var i = g.AddComponent<Image>();
        i.color = c;
        i.raycastTarget = false;
        var rt = i.rectTransform;
        rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivote;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, alto);
        return i;
    }

    void PrepararSonido()
    {
        agua = Fuente("Agua", true, SonidosApertura.Agua());
        estatica = Fuente("Estatica", true, SonidosApertura.Estatica());
        efectos = Fuente("Efectos", false, null);
        musica = Fuente("Musica", true, Resources.Load<AudioClip>("MareasDeLiria"));
        musica.pitch = 0.8f;
        musica.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = 900f;
        clipGolpe = SonidosApertura.Golpe();
        clipRele = SonidosApertura.Rele();
        clipPitido = SonidosApertura.Pitido(880f);
        foreach (var a in new[] { agua, estatica, musica }) if (a.clip != null) a.Play();
    }

    AudioSource Fuente(string nombre, bool bucle, AudioClip clip)
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(transform, false);
        var a = g.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = bucle;
        a.spatialBlend = 0f;
        a.volume = 0f;
        a.clip = clip;
        return a;
    }

    // ------------------------------------------------------------------ bucle

    void Update()
    {
        if (nilo == null || terminado) return;
        Saltar();
        if (terminado) return;
        ManejarDialogo();

        float previo = T;
        float dt = Time.deltaTime;
        // La radio y la elección retienen la cinemática hasta que el jugador responde.
        if (T >= P_Vuelta - 0.6f && T < P_Vuelta && dialogo.Abierto) dt = 0f;
        if (esperandoEleccion) dt = 0f;
        T += dt;
        Evaluar(previo, T, dt);
        Estado = "T " + T.ToString("F1") + (esperandoEleccion ? " eligiendo" : "") + (eleccion >= 0 ? " eleccion " + eleccion : "");
    }

    void ManejarDialogo()
    {
        if (entrada == null || dialogo == null || !dialogo.Abierto) return;
        if (dialogo.EligiendoOpcion)
        {
            Vector2 m = entrada.LeerMovimiento();
            if (Mathf.Abs(m.y) < 0.5f) flanco = false;
            else if (!flanco) { flanco = true; dialogo.MoverSeleccion(m.y > 0f ? -1 : 1); }
        }
        var raton = Mouse.current;
        if (entrada.Pulsado(entrada.Interactuar) || entrada.Pulsado(entrada.Confirmar) || (raton != null && raton.leftButton.wasPressedThisFrame))
            dialogo.Avanzar();
    }

    /// <summary>Mantener Esc: antes de elegir lleva a la elección; después, termina.</summary>
    void Saltar()
    {
        Keyboard k = Keyboard.current;
        bool algo = k != null && k.anyKey.isPressed;
        bool esc = (k != null && k.escapeKey.isPressed) || (entrada != null && entrada.Pausa != null && entrada.Pausa.IsPressed());
        if (algo) avisoVisible = 3f;
        avisoVisible -= Time.unscaledDeltaTime;
        mantenerSalto = esc ? mantenerSalto + Time.unscaledDeltaTime : Mathf.Max(0f, mantenerSalto - Time.unscaledDeltaTime * 2f);
        aviso.color = new Color(aviso.color.r, aviso.color.g, aviso.color.b, esperandoEleccion ? 0f : Mathf.Clamp01(avisoVisible) * 0.85f);
        avisoBarra.rectTransform.sizeDelta = new Vector2(240f * Mathf.Clamp01(mantenerSalto / 1.1f), 3f);
        if (mantenerSalto <= 1.1f || esperandoEleccion) return;
        mantenerSalto = 0f;
        if (eleccion >= 0) { Terminar(); return; }
        if (T < P_Vuelta) { dialogo.Cerrar(); radioDicha = true; T = P_Vuelta; }
    }

    /// <summary>Para revisar desde el editor: avanza hasta 'hasta' sin esperar.</summary>
    public void Revisar(float hasta, int opcion = -1)
    {
        if (opcion >= 0 && eleccion < 0 && hasta > P_Eleccion) { Elegir(opcion); esperandoEleccion = false; dialogo.Cerrar(); }
        radioDicha = radioDicha || hasta > P_Vuelta;
        const float paso = 1f / 30f;
        while (T < hasta) { float p = T; T = Mathf.Min(hasta, T + paso); Evaluar(p, T, paso); }
    }

    // ------------------------------------------------------------------ evaluación

    static float S(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
    static float Tramo(float t, float a, float b) { return Mathf.Clamp01((t - a) / (b - a)); }
    static bool Cruza(float previo, float ahora, float marca) { return previo < marca && ahora >= marca; }

    void Evaluar(float previo, float t, float dt)
    {
        Base(t);
        if (t < P_Montaje) Varadero(previo, t);
        else if (t < P_Reloj) Montaje(previo, t);
        else if (t < P_Radio) Reloj(previo, t);
        else if (t < P_Vuelta) Radio(previo, t);
        else if (t < P_Resolucion || eleccion < 0) Vuelta(previo, t);
        else if (eleccion == 0) Recordar(previo, t);
        else Borrar(previo, t);

        AplicarSacudida(dt);
        if (t >= Fin) Terminar();
    }

    void Base(float t)
    {
        vineta.intensity.Override(0.38f);
        color.postExposure.Override(0.1f);
        color.saturation.Override(-6f);
        color.contrast.Override(12f);
        color.colorFilter.Override(Color.white);
        bloom.intensity.Override(1.3f);
        Fundido(0f);
        destello.color = Color.clear;
        Alfa(rotulo, 0f); Alfa(pie, 0f); Alfa(cita, 0f);
        Luna(1.05f, new Color(0.6f, 0.68f, 0.95f), new Vector3(42f, -35f, 0f));
        foco.intensity = 0f;
        matSubestacion.SetColor("_EmissionColor", Color.black);
        foreach (Material m in pantallaNilo) m.SetColor("_EmissionColor", Color.black);
        pilotoRadio.intensity = 0f;
        horizonte.intensity = 0f;
        cam.backgroundColor = RenderSettings.fogColor;
        RenderSettings.fog = true;
        for (int i = 0; i < recuerdos.Count; i++) recuerdos[i].SetActive(false);

        // Las campanas se mecen sin sonar: no hay nadie que las toque.
        float meneo = Mathf.Sin(t * 0.9f) * 7f;
        campanaA.localRotation = Quaternion.Euler(0f, 0f, meneo);
        campanaB.localRotation = Quaternion.Euler(0f, 0f, -meneo * 0.8f + Mathf.Sin(t * 1.3f) * 2f);

        // Brasa viva.
        float parpadeo = 0.85f + 0.15f * Mathf.PerlinNoise(t * 3f, 0.4f);
        brasa.intensity = 3f * parpadeo;
        caraNilo.intensity = 0f;

        float pista = Mathf.Clamp01(t / 3f);
        agua.volume = 0.35f * pista;
        musica.volume = t < P_Montaje ? 0.22f * pista : 0.05f;
        estatica.volume = 0f;
    }

    void Luna(float intensidad, Color c, Vector3 giro)
    {
        luna.intensity = intensidad;
        luna.color = c;
        luna.transform.rotation = Quaternion.Euler(giro);
    }

    bool tieneRecuerdoActual()
    {
        int i = Mathf.Clamp(Mathf.FloorToInt((T - P_Montaje) / DuracionRecuerdo), 0, 3);
        return tieneRecuerdo[i];
    }

    void Alfa(Graphic g, float a) { Color c = g.color; c.a = Mathf.Clamp01(a); g.color = c; }
    void Fundido(float a) { fundido.color = new Color(0f, 0f, 0f, Mathf.Clamp01(a)); }

    Vector3 basePos; Quaternion baseRot;

    void Colocar(Vector3 pos, Vector3 mira, float fov, float mano = 1f)
    {
        cam.fieldOfView = fov;
        float t = T * 0.8f;
        var giro = Quaternion.Euler((Mathf.PerlinNoise(t, 3.1f) - 0.5f) * 1.4f * mano, (Mathf.PerlinNoise(7.7f, t) - 0.5f) * 1.4f * mano, 0f);
        basePos = pos;
        baseRot = Quaternion.LookRotation(mira - pos) * giro;
        cam.transform.SetPositionAndRotation(basePos, baseRot);
    }

    void Sacudir(float fuerza) { sacudida = Mathf.Max(sacudida, fuerza); }

    void AplicarSacudida(float dt)
    {
        if (sacudida <= 0f) return;
        sacudida = Mathf.Max(0f, sacudida - dt * 0.6f);
        float s = sacudida * 0.25f;
        cam.transform.position = basePos + new Vector3(Mathf.PerlinNoise(T * 25f, 1f) - 0.5f, Mathf.PerlinNoise(2f, T * 25f) - 0.5f, 0f) * s;
    }

    void Sonar(AudioClip c, float v) { if (c != null && efectos != null) efectos.PlayOneShot(c, v); }

    void NiloEn(Vector3 pos, float giroY, float inclinacion = 0f)
    {
        nilo.transform.position = pos;
        nilo.transform.rotation = Quaternion.Euler(inclinacion, giroY, 0f);
    }

    // 1-2 · El varadero y el molde
    void Varadero(float previo, float t)
    {
        Fundido(1f - Tramo(t, 0f, 2f));

        // Nilo camina hacia el banco entre P_Acercarse y P_Molde.
        float k = S(Tramo(t, P_Acercarse, P_Molde - 1.5f));
        Vector3 pos = Vector3.Lerp(inicioNilo, frenteBanco, k);
        NiloEn(pos, 0f);

        if (t < P_Acercarse)
        {
            // Plano general lento: el tablado vacío, las campanas, el brillo al fondo.
            float u = Tramo(t, 0f, P_Acercarse);
            Colocar(new Vector3(-9f + u * 3f, 4.2f - u * 0.8f, -13f), new Vector3(0f, 1.2f, 3f), 40f);
            Alfa(rotulo, Tramo(t, 2f, 3f) * (1f - Tramo(t, 7f, 8.5f)));
            rotulo.text = "Varadero de las Campanas";
            Alfa(pie, Tramo(t, 2.6f, 3.6f) * (1f - Tramo(t, 7f, 8.5f)));
            pie.text = "Las campanas se mecen. No suena ninguna.";
        }
        else if (t < P_Molde)
        {
            // Detrás de Nilo, por encima del hombro, mientras se acerca.
            Vector3 hombro = pos + new Vector3(1.7f, 3.6f, -4.2f);
            Colocar(hombro, pos + new Vector3(0f, 1f, 4f), 40f);
        }
        else
        {
            // Primer plano del molde.
            float u = Tramo(t, P_Molde, P_Montaje);
            Vector3 m = disco.position;
            Colocar(m + new Vector3(0.9f - u * 0.3f, 0.9f - u * 0.25f, -1.3f + u * 0.4f), m, 32f, 0.6f);
            Alfa(cita, Tramo(t, P_Molde + 1.5f, P_Molde + 2.5f) * (1f - Tramo(t, P_Montaje - 1.2f, P_Montaje - 0.2f)));
            cita.text = Escribir("«La esclusa nunca se abrió»", Tramo(t, P_Molde + 1.5f, P_Molde + 4f));
            Alfa(pie, Tramo(t, P_Molde + 0.5f, P_Molde + 1.5f) * (1f - Tramo(t, P_Montaje - 1.2f, P_Montaje - 0.2f)));
            pie.text = "Un molde con la forma exacta de un disco de memoria.";
            Fundido(Tramo(t, P_Montaje - 0.4f, P_Montaje));
        }
        if (Cruza(previo, t, P_Molde + 1.5f)) Sonar(clipRele, 0.5f);
    }

    static string Escribir(string s, float k) { return s.Substring(0, Mathf.Clamp(Mathf.RoundToInt(s.Length * k), 0, s.Length)); }

    // 3 · Montaje de los recuerdos 7-10
    void Montaje(float previo, float t)
    {
        RenderSettings.fog = false;
        cam.backgroundColor = Color.black;
        Luna(tieneRecuerdoActual() ? 1.5f : 0.15f, new Color(1f, 0.93f, 0.82f), new Vector3(58f, 25f, 0f));
        int i = Mathf.Clamp(Mathf.FloorToInt((t - P_Montaje) / DuracionRecuerdo), 0, 3);
        float u = (t - P_Montaje - i * DuracionRecuerdo) / DuracionRecuerdo;
        GameObject o = recuerdos[i];
        o.SetActive(tieneRecuerdo[i]);
        o.transform.localRotation = Quaternion.Euler(0f, -20f + u * 40f, 0f);
        foco.intensity = tieneRecuerdo[i] ? 6f * Tramo(u, 0.02f, 0.15f) : 0.6f;

        Vector3 centro = Vacio + new Vector3(0f, 0.6f, 0f);
        Colocar(centro + new Vector3(0.4f - u * 0.8f, 1.6f - u * 0.3f, -2.6f + u * 0.5f), centro, 38f, 0.5f);

        float entrar = Tramo(u, 0.1f, 0.25f), salir = 1f - Tramo(u, 0.85f, 0.97f);
        rotulo.text = tieneRecuerdo[i] ? TitulosRecuerdo[i] : "Recuerdo " + (7 + i);
        pie.text = tieneRecuerdo[i] ? PiesRecuerdo[i] : "Un hueco negro. Nunca lo encontraste.";
        Alfa(rotulo, entrar * salir);
        Alfa(pie, Tramo(u, 0.25f, 0.4f) * salir);

        // Cada recuerdo entra con un fogonazo blanco y un chasquido de estática.
        float f = 1f - Tramo(u, 0f, 0.08f);
        destello.color = new Color(1f, 1f, 1f, f * 0.8f);
        estatica.volume = 0.25f * f + 0.04f;
        if (Cruza(previo, t, P_Montaje + i * DuracionRecuerdo + 0.01f)) Sonar(clipRele, 0.7f);
        color.saturation.Override(-25f);
    }

    // 4 · El reloj y el agua
    void Reloj(float previo, float t)
    {
        RenderSettings.fog = false;
        cam.backgroundColor = Color.black;
        Luna(1.2f, new Color(0.7f, 0.8f, 1f), new Vector3(30f, 15f, 0f));
        float u = Tramo(t, P_Reloj, P_Radio - 1f);
        Vector3 c = Vacio + new Vector3(0f, 1.2f, 40f);
        Colocar(c + new Vector3(0f, 0.3f, -5.2f + u * 1.2f), c + new Vector3(0f, -0.3f, 0f), 34f, 0.4f);

        // 00:17: la aguja de las horas en las doce y el minutero en el 17.
        agujaHora.localRotation = Quaternion.Euler(0f, 0f, -0.5f * 17f);
        agujaMinuto.localRotation = Quaternion.Euler(0f, 0f, -6f * 17f - (u > 0.5f ? 0f : 0f));
        aguaReloj.localPosition = new Vector3(0f, Mathf.Lerp(-0.2f, 2.6f, S(Tramo(t, P_Reloj + 2f, P_Radio - 1f))), -0.5f);

        rotulo.text = "00:17";
        Alfa(rotulo, Tramo(t, P_Reloj + 0.5f, P_Reloj + 1.5f) * (1f - Tramo(t, P_Radio - 2f, P_Radio - 1f)));
        pie.text = "El agua del mercado no se detuvo.";
        Alfa(pie, Tramo(t, P_Reloj + 2f, P_Reloj + 3f) * (1f - Tramo(t, P_Radio - 2f, P_Radio - 1f)));
        agua.volume = 0.6f * Tramo(t, P_Reloj, P_Reloj + 2f);
        for (int s = 0; s < 7; s++) if (Cruza(previo, t, P_Reloj + 0.5f + s)) Sonar(clipPitido, 0.12f);
        Fundido(Tramo(t, P_Radio - 1f, P_Radio));
    }

    // 5 · La radio rota
    void Radio(float previo, float t)
    {
        RenderSettings.fog = false;
        cam.backgroundColor = Color.black;
        float brillo = Mathf.PerlinNoise(t * 4f, 0.2f) > 0.4f ? 1f : 0.3f;
        Luna(0.55f * brillo * Tramo(t, P_Radio + 0.5f, P_Radio + 1.5f), RelevoUI.Verde, new Vector3(25f, 20f, 0f));
        agua.volume = 0f;
        Vector3 c = radio.position + new Vector3(0f, 0.5f, 0f);
        Colocar(c + new Vector3(-0.6f, 0.4f, -2.2f), c, 30f, 0.3f);
        float latido = Mathf.PerlinNoise(t * 4f, 0.2f) > 0.4f ? 1f : 0.1f;
        pilotoRadio.intensity = 2.5f * latido * Tramo(t, P_Radio + 0.5f, P_Radio + 1.5f);
        estatica.volume = 0.3f;
        Fundido(1f - Tramo(t, P_Radio, P_Radio + 1.5f));

        if (!radioDicha && t >= P_Radio + 2.5f)
        {
            radioDicha = true;
            dialogo.Encolar(new DialogoUI.Mensaje
            {
                titulo = "Maia",
                texto = "…Nilo… —kssh— …no fue… no fue… —kssh— …culpa tuya… «buenas noches a quienes…»",
                acento = RelevoUI.Verde
            });
        }
        if (t > P_Vuelta - 1f) Fundido(Tramo(t, P_Vuelta - 1f, P_Vuelta - 0.6f) * (dialogo.Abierto ? 0f : 1f));
    }

    // 6 · De vuelta ante el molde: la elección
    void Vuelta(float previo, float t)
    {
        NiloEn(frenteBanco, 0f);
        float u = Tramo(t, P_Vuelta, P_Eleccion);
        // Tres cuartos desde el lado del banco: Nilo a la izquierda, el molde encendido a la derecha.
        Colocar(new Vector3(3.9f - u * 0.5f, 2.6f, 3f + u * 0.2f), new Vector3(-0.2f, 1.2f, 4.5f), 40f, 0.5f);
        Fundido(1f - Tramo(t, P_Vuelta, P_Vuelta + 1.2f));
        brasa.intensity *= 1.2f;

        if (!esperandoEleccion && eleccion < 0 && t >= P_Eleccion)
        {
            esperandoEleccion = true;
            dialogo.Encolar(new DialogoUI.Mensaje
            {
                titulo = Historia.TituloDecision(2),
                texto = "Podría fundirlo otra vez y despertar sin culpa, como la primera vez. O quedarse con todo y bajar a la subestación sabiendo quién es.",
                acento = RelevoUI.Ambar,
                opciones = Historia.OpcionesDecision(2),
                alElegir = Elegir
            });
        }
    }

    void Elegir(int opcion)
    {
        eleccion = opcion;
        esperandoEleccion = false;
        SaveManager.Decidir(2, Historia.ValorDecision(2, opcion));
        if (T < P_Resolucion) T = P_Resolucion;
    }

    // 7a · Recordarlo todo
    void Recordar(float previo, float t)
    {
        float u = S(Tramo(t, P_Resolucion, P_Resolucion + 3f));
        // Nilo aparta el molde y da un paso atrás.
        NiloEn(frenteBanco + new Vector3(0f, 0f, -0.9f * u), 0f);
        disco.localPosition = new Vector3(0.2f + 0.5f * u, 1.31f, -0.2f);
        matDisco.SetColor("_EmissionColor", Color.Lerp(new Color(1.2f, 0.45f, 0.1f), new Color(0.05f, 0.02f, 0f), u));
        float enciende = Tramo(t, P_Resolucion + 1f, P_Resolucion + 2.5f);
        caraNilo.intensity = 2.5f * enciende;
        foreach (Material m in pantallaNilo) m.SetColor("_EmissionColor", RelevoUI.Turquesa * 2f * enciende);

        if (t < P_Resolucion + 6f)
        {
            Vector3 cara = nilo.transform.position + new Vector3(0f, 1.7f, 0f);
            Colocar(cara + new Vector3(0.9f, 0.4f, 3.6f - u * 0.6f), cara + new Vector3(0f, -0.2f, 0f), 36f, 0.4f);
            rotulo.text = "OPERADOR DE GUARDIA · NILO";
            rotulo.color = new Color(RelevoUI.Turquesa.r, RelevoUI.Turquesa.g, RelevoUI.Turquesa.b, Tramo(t, P_Resolucion + 2f, P_Resolucion + 3f) * (1f - Tramo(t, P_Resolucion + 5.2f, P_Resolucion + 6f)));
            pie.text = "Esta vez se queda con todo.";
            Alfa(pie, Tramo(t, P_Resolucion + 3f, P_Resolucion + 4f) * (1f - Tramo(t, P_Resolucion + 5.2f, P_Resolucion + 6f)));
        }
        else Lejos(previo, t);
    }

    // 7b · Fundir el disco otra vez
    void Borrar(float previo, float t)
    {
        float u = S(Tramo(t, P_Resolucion, P_Resolucion + 2.5f));
        // Nilo se inclina sobre el molde; el metal se vuelve a encender.
        NiloEn(frenteBanco + new Vector3(0f, 0f, 0.4f * u), 0f, 18f * u);
        float ardor = Tramo(t, P_Resolucion + 1f, P_Resolucion + 3.5f);
        matDisco.SetColor("_EmissionColor", Color.Lerp(new Color(1.2f, 0.45f, 0.1f), new Color(6f, 2.6f, 0.6f), ardor));
        brasa.intensity = 3f + 9f * ardor;

        if (t < P_Resolucion + 3.6f)
        {
            Vector3 m = disco.position;
            Colocar(m + new Vector3(-1.2f, 1.1f, -1.4f), m + new Vector3(0f, 0.3f, 0f), 36f, 0.5f);
            destello.color = new Color(1f, 0.85f, 0.6f, Tramo(t, P_Resolucion + 3f, P_Resolucion + 3.6f));
        }
        else if (t < P_Resolucion + 7.5f)
        {
            // El recuerdo falso, nítido y cálido.
            destello.color = new Color(1f, 0.78f, 0.45f, 1f - 0.15f * Tramo(t, P_Resolucion + 3.6f, P_Resolucion + 5f));
            rotulo.text = "Recuerdo 6 · El héroe de la esclusa";
            rotulo.color = new Color(0.25f, 0.14f, 0.05f, Tramo(t, P_Resolucion + 4f, P_Resolucion + 4.8f) * (1f - Tramo(t, P_Resolucion + 6.8f, P_Resolucion + 7.5f)));
            pie.text = "Todas las barcas salieron antes de la crecida. Nadie se quedó atrás.";
            pie.color = new Color(0.25f, 0.14f, 0.05f, Tramo(t, P_Resolucion + 4.6f, P_Resolucion + 5.4f) * (1f - Tramo(t, P_Resolucion + 6.8f, P_Resolucion + 7.5f)));
            if (Cruza(previo, t, P_Resolucion + 3.6f)) Sonar(clipRele, 0.8f);
            Fundido(Tramo(t, P_Resolucion + 7.1f, P_Resolucion + 7.5f));
        }
        else Lejos(previo, t);
    }

    /// <summary>Final común: plano general, la subestación ruge a lo lejos y suenan las campanas.</summary>
    void Lejos(float previo, float t)
    {
        float a = eleccion == 0 ? P_Resolucion + 6f : P_Resolucion + 7.5f;
        float u = Tramo(t, a, Fin);
        Colocar(new Vector3(5f - u * 1.5f, 2.6f, -9f), new Vector3(2f, 5f, 40f), 44f, 0.8f);
        Fundido((1f - Tramo(t, a, a + 0.8f)) * (eleccion == 1 ? 1f : 0f) + Tramo(t, Fin - 1.5f, Fin));
        if (eleccion == 0) foreach (Material m in pantallaNilo) m.SetColor("_EmissionColor", RelevoUI.Turquesa * 2f);

        float rugido = Tramo(t, a + 1.5f, a + 2.5f);
        horizonte.intensity = 6f * rugido * (0.7f + 0.3f * Mathf.Sin(t * 9f));
        matSubestacion.SetColor("_EmissionColor", new Color(3f, 0.5f, 0.35f) * rugido * (0.7f + 0.3f * Mathf.Sin(t * 9f)));
        if (Cruza(previo, t, a + 1.5f)) { Sonar(clipGolpe, 1f); Sacudir(1f); }
        if (Cruza(previo, t, a + 2.6f)) { Sonar(clipGolpe, 0.7f); Sacudir(0.6f); }
        if (rugido > 0f)
        {
            // Ahora sí, las campanas suenan solas con el temblor.
            float golpe = Mathf.Sin(t * 5f) * 22f * rugido;
            campanaA.localRotation = Quaternion.Euler(0f, 0f, golpe);
            campanaB.localRotation = Quaternion.Euler(0f, 0f, -golpe);
        }
        rotulo.text = "Más allá del varadero, algo enorme se pone en pie.";
        rotulo.color = new Color(RelevoUI.Tinta.r, RelevoUI.Tinta.g, RelevoUI.Tinta.b, Tramo(t, a + 3f, a + 4f) * (1f - Tramo(t, Fin - 2f, Fin - 1.2f)));
        musica.volume = 0f;
    }

    // ------------------------------------------------------------------ salida

    void Terminar()
    {
        if (terminado) return;
        // La decisión no se salta: sin elegir se queda en «recordar» por defecto.
        if (eleccion < 0) Elegir(0);
        terminado = true;
        if (entrada != null) entrada.Desactivar();
        string destino = Application.CanStreamedLevelBeLoaded(escenaSiguiente) ? escenaSiguiente : LevelManager.NombreEscenaNivel(13);
        SceneManager.LoadScene(destino);
    }

    void OnDestroy()
    {
        if (entrada != null) entrada.Desactivar();
    }
}
