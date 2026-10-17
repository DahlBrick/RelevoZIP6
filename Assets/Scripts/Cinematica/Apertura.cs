using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.IO;
using UnityEditor.Media;
#endif

/// <summary>
/// Cinemática con la que empieza RELEVO (docs/narrativa/acto_1_el_viaje_del_heroe.md, prólogo).
///
/// Parte A · La ciudad que se apaga. Liria de noche, encendida y viva: la panorámica, un farol, la
/// rueda de una bomba, una barca con una vela, sombras en una ventana. La canción suena como desde
/// una radio lejana. Después las luces se apagan en cadena, de lejos a cerca, al ritmo de la
/// música, el agua sube por el malecón y el último farol chisporrotea y muere. Título.
///
/// Parte B · El Despertar. La terminal de Nilo arrancando (con la pista "Último acceso: FORJA"),
/// sus ojos que se abren en el suelo inundado de la Subestación, la mano en el agua, Nilo que se
/// levanta y resbala, el panel con la quemadura en forma de mano (otra pista, sin detenerse), la
/// radio que se enciende sola antes de que la toque y, por fin, la voz de Maia.
///
/// Dirección: cámara de cine propia (perspectiva, profundidad de campo, cámara en mano sutil,
/// bandas 2,39:1, grano, bloom), con la interfaz en una cámara superpuesta para que el desenfoque
/// no la toque. Toda la puesta en escena es una función del tiempo de la cinemática (T): los planos,
/// los cortes de luz, el agua y la animación de Nilo se calculan a partir de T, así que la
/// cinemática se puede revisar desde cualquier segundo (Saltar a).
///
/// Se juega en tiempo real al abrir el juego y se salta manteniendo Esc. Desde el editor también
/// se graba a MP4 (Grabar), igual que las maquetas.
/// </summary>
public sealed class Apertura : MonoBehaviour
{
    [Header("Entrada")]
    [Tooltip("Assets/Input/Relevo.inputactions")]
    [SerializeField] InputActionAsset acciones;

    [Header("Arte")]
    [Tooltip("Modelos de los kits de terceros; se buscan por nombre.")]
    [SerializeField] List<GameObject> modelos = new List<GameObject>();
    [Tooltip("Bloques de la Liria nueva (Assets/Prefabs/Liria). Vacío: la Liria antigua de LiriaNocturna.")]
    [SerializeField] List<GameObject> bloquesLiria = new List<GameObject>();
    [Tooltip("Cielo nocturno (Skybox/Procedural). Vacío: fondo liso del color de la niebla.")]
    [SerializeField] Material cielo;

    [Header("Sonido (opcional)")]
    [Tooltip("La canción definitiva. Vacío: se usa la provisional generada por código.")]
    [SerializeField] AudioClip cancion;

    [Header("Al terminar")]
    [SerializeField] string escenaSiguiente = "Nivel1_MuelleDeCobre";

    /// <summary>Segundo desde el que empieza (solo para revisar en el editor).</summary>
    public static float InicioRevision;
    public static string Estado = "";

    // ------------------------------------------------------------------ línea de tiempo

    // Parte A
    // Versión de 56 s sobre la Liria nueva: mismos planos, más apretados.
    // Solo la noche de Liria y su apagón; el despertar se hará en otra escena. Al terminar el
    // título se carga el primer nivel.
    const float A_Negro = 0f, A_Panoramica = 2.5f, A_Farol = 10f, A_Bomba = 12.5f, A_Barca = 15f,
        A_Ventana = 17.5f, A_Apagon = 20f, A_Agua = 24.5f, A_Ultimo = 26.5f, A_Titulo = 29f;
    // Parte B
    const float B_Terminal = 132f, B_Ojos = 135.5f, B_Mano = 139f, B_Levantarse = 141f, B_Panel = 144.5f,
        B_Hombro = 146f, B_Radio = 149f, B_Final = 153f;   // fuera de la cinemática por ahora
    const float Fin = A_Titulo + 5f;

    const float CorteInicio = 20.7f, CorteFin = 25.5f;   // la cadena de apagones
    const float CorteUltimo = 28.2f;

    float T;
    bool listo;
    bool terminado;

    // ------------------------------------------------------------------ piezas

    readonly Dictionary<string, GameObject> porNombre = new Dictionary<string, GameObject>();
    LiriaNocturna liria;
    SubestacionEscena sub;
    RelevoInput entrada;

    Camera cam, camUI;
    DepthOfField dof;
    Vignette vineta;
    ColorAdjustments color;
    ChromaticAberration aberracion;
    FilmGrain grano;
    Bloom bloom;

    Canvas lienzo;
    Image fundido, bandaArriba, bandaAbajo, parpadoArriba, parpadoAbajo;
    Text titulo, rotulo, terminal, aviso;
    Image avisoBarra;
    DialogoUI dialogo;

    AudioSource musica, estatica, ambiente, efectos;
    AudioLowPassFilter pasaBajos;
    AudioHighPassFilter pasaAltos;
    AudioDistortionFilter distorsion;
    AudioClip clipEstatica, clipAgua, clipZumbido, clipRele, clipGolpe, clipArranque, clipPitido;

    PlayerController nilo;
    Animator animNilo;
    Transform cabeza, mano;
    const float GiroInicial = -60f;
    readonly List<Renderer> antena = new List<Renderer>();
    float inclinacion;
    Vector3 inicioNilo;

    Light luna;
    float mantenerSalto;
    float avisoVisible;

    // ------------------------------------------------------------------ arranque

    IEnumerator Start()
    {
        foreach (GameObject m in modelos) if (m != null) porNombre[m.name] = m;

        Application.targetFrameRate = 60;
        GameEvents.Limpiar();
        RelevoGame.Limpiar();
        entrada = acciones != null ? new RelevoInput(acciones) : null;

        Ambiente();
        KitRelevo kit = KitRelevo.Activo;
        liria = bloquesLiria.Count > 0
            ? LiriaNocturna.ConstruirNueva(transform, Modelo, kit, bloquesLiria)
            : LiriaNocturna.Construir(transform, Modelo, kit);
        sub = SubestacionEscena.Construir(transform, Modelo, kit);
        PrepararCamara();
        PrepararInterfaz();
        PrepararSonido();
        yield return PrepararNilo();

        // Mismo estilo que Liria en todo: escenarios, Nilo y la radio pasan a RELEVO/Comic, y la
        // cámara de cine dibuja el contorno de tinta. La parte A (noche) tiñe las sombras de un
        // azul más frío; la B (día) usa el violeta de Liria.
        EstiloComic.Aplicar(liria.Raiz, new Color(0.36f, 0.4f, 0.62f));
        EstiloComic.Aplicar(transform);
        EstiloComic.RendererTinta(cam);

        T = InicioRevision;
        listo = true;
        IniciarGrabacion();
        Evaluar(T - 0.0001f, T, 0f);
    }

    GameObject Modelo(string nombre)
    {
        GameObject m;
        return porNombre.TryGetValue(nombre, out m) ? m : null;
    }

    void Ambiente()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.34f, 0.4f, 0.62f);
        RenderSettings.ambientEquatorColor = new Color(0.22f, 0.24f, 0.36f);
        RenderSettings.ambientGroundColor = new Color(0.1f, 0.1f, 0.13f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.06f, 0.08f, 0.15f);
        if (cielo != null) RenderSettings.skybox = cielo;
        // Niebla larga: funde la ciudad lejana sin borrar la luna ni las estrellas.
        RenderSettings.fogStartDistance = 60f;
        RenderSettings.fogEndDistance = 420f;

        // Luz de luna: fría, baja y suave. En la Subestación entra por la ventana con su propio foco.
        Light luna = new GameObject("Luna").AddComponent<Light>();
        luna.transform.SetParent(transform, false);
        luna.transform.rotation = Quaternion.Euler(28f, 200f, 0f);
        luna.type = LightType.Directional;
        luna.color = new Color(0.55f, 0.65f, 0.95f);
        luna.intensity = 1.1f;
        this.luna = luna;
        luna.shadows = LightShadows.Soft;
        luna.shadowStrength = 0.6f;
        luna.cullingMask = ~(1 << 5);
    }

    void PrepararCamara()
    {
        GameObject go = new GameObject("Camara de cine");
        go.transform.SetParent(transform, false);
        go.tag = "MainCamera";
        cam = go.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 1500f;   // la cordillera de la Liria nueva está a 330-820 m
        cam.clearFlags = cielo != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        cam.backgroundColor = RenderSettings.fogColor;
        cam.cullingMask = ~(1 << 5);
        go.AddComponent<AudioListener>();
        var datos = go.AddComponent<UniversalAdditionalCameraData>();
        datos.renderPostProcessing = true;
        datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        datos.antialiasingQuality = AntialiasingQuality.High;

        // Cámara de la interfaz, apilada sobre la de cine y sin posproceso.
        GameObject goUI = new GameObject("Camara interfaz");
        goUI.transform.SetParent(go.transform, false);
        camUI = goUI.AddComponent<Camera>();
        camUI.cullingMask = 1 << 5;
        camUI.clearFlags = CameraClearFlags.Depth;
        var datosUI = goUI.AddComponent<UniversalAdditionalCameraData>();
        datosUI.renderType = CameraRenderType.Overlay;
        datosUI.renderPostProcessing = false;
        datos.cameraStack.Add(camUI);

        GameObject goV = new GameObject("Volumen de cine");
        goV.transform.SetParent(transform, false);
        Volume v = goV.AddComponent<Volume>();
        v.isGlobal = true;
        v.priority = 100f;
        VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        Tonemapping tono = perfil.Add<Tonemapping>(true);
        tono.mode.Override(TonemappingMode.ACES);
        bloom = perfil.Add<Bloom>(true);
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(1.4f);
        bloom.scatter.Override(0.72f);
        dof = perfil.Add<DepthOfField>(true);
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focalLength.Override(60f);
        dof.aperture.Override(2.8f);
        dof.focusDistance.Override(10f);
        vineta = perfil.Add<Vignette>(true);
        vineta.smoothness.Override(0.5f);
        color = perfil.Add<ColorAdjustments>(true);
        aberracion = perfil.Add<ChromaticAberration>(true);
        grano = perfil.Add<FilmGrain>(true);
        grano.type.Override(FilmGrainLookup.Thin1);
        grano.intensity.Override(0.22f);
        v.sharedProfile = perfil;
    }

    void PrepararInterfaz()
    {
        GameObject go = new GameObject("Interfaz de la cinematica");
        go.layer = 5;
        go.transform.SetParent(transform, false);
        lienzo = go.AddComponent<Canvas>();
        lienzo.renderMode = RenderMode.ScreenSpaceCamera;
        lienzo.worldCamera = camUI;
        lienzo.planeDistance = 1f;
        CanvasScaler esc = go.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 1f;
        Transform r = go.transform;

        float banda = (1080f - 1920f / 2.39f) * 0.5f;
        parpadoArriba = Bloque(r, "Parpado arriba", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0f), 560f);
        parpadoAbajo = Bloque(r, "Parpado abajo", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f), 560f);
        bandaArriba = Bloque(r, "Banda arriba", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), banda);
        bandaAbajo = Bloque(r, "Banda abajo", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), banda);

        rotulo = RelevoUI.Texto(r, "Rotulo", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(110f, banda + 46f),
            new Vector2(900f, 50f), 34, TextAnchor.LowerLeft, new Color(0.95f, 0.9f, 0.82f, 0f));
        rotulo.font = RelevoUI.FuenteTitulo();
        rotulo.text = "Liria";

        terminal = RelevoUI.Texto(r, "Terminal", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f),
            new Vector2(980f, 420f), 30, TextAnchor.MiddleLeft, new Color(0.45f, 1f, 0.6f));
        terminal.font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New", "Lucida Console" }, 30);
        terminal.text = "";

        fundido = Bloque(r, "Fundido", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), 0f);
        fundido.rectTransform.offsetMin = fundido.rectTransform.offsetMax = Vector2.zero;
        terminal.transform.SetAsLastSibling();

        titulo = RelevoUI.Texto(r, "Titulo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f),
            new Vector2(1600f, 220f), 150, TextAnchor.MiddleCenter, new Color(0.96f, 0.9f, 0.8f, 0f));
        titulo.font = RelevoUI.FuenteTitulo();
        titulo.text = "R E L E V O";

        dialogo = DialogoUI.Crear(r);

        aviso = RelevoUI.Texto(r, "Aviso saltar", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 40f),
            new Vector2(520f, 36f), 22, TextAnchor.LowerRight, new Color(0.95f, 0.9f, 0.82f, 0f));
        aviso.text = "Mantén Esc para saltar";
        avisoBarra = Bloque(r, "Aviso barra", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), 3f);
        avisoBarra.rectTransform.anchorMin = avisoBarra.rectTransform.anchorMax = new Vector2(1f, 0f);
        avisoBarra.rectTransform.anchoredPosition = new Vector2(-60f, 30f);
        avisoBarra.rectTransform.sizeDelta = new Vector2(0f, 3f);
        avisoBarra.color = RelevoUI.Ambar;

        foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;
    }

    Image Bloque(Transform padre, string nombre, Vector2 min, Vector2 max, Vector2 pivote, float alto)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        Image i = g.AddComponent<Image>();
        i.color = Color.black;
        i.raycastTarget = false;
        RectTransform rt = i.rectTransform;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivote;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, alto);
        return i;
    }

    void PrepararSonido()
    {
        musica = Fuente("Musica", true);
        musica.clip = cancion != null ? cancion : SonidosApertura.Cancion();
        pasaAltos = musica.gameObject.AddComponent<AudioHighPassFilter>();
        pasaBajos = musica.gameObject.AddComponent<AudioLowPassFilter>();
        distorsion = musica.gameObject.AddComponent<AudioDistortionFilter>();
        distorsion.distortionLevel = 0f;

        clipEstatica = SonidosApertura.Estatica();
        clipAgua = SonidosApertura.Agua();
        clipZumbido = SonidosApertura.Zumbido();
        clipRele = SonidosApertura.Rele();
        clipGolpe = SonidosApertura.Golpe();
        clipArranque = SonidosApertura.Arranque();
        clipPitido = SonidosApertura.Pitido(1250f);

        estatica = Fuente("Estatica", true);
        estatica.clip = clipEstatica;
        ambiente = Fuente("Ambiente", true);
        efectos = Fuente("Efectos", false);
        sub.Altavoz.clip = clipEstatica;
    }

    AudioSource Fuente(string nombre, bool bucle)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(transform, false);
        AudioSource a = g.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = bucle;
        a.spatialBlend = 0f;
        a.volume = 0f;
        return a;
    }

    IEnumerator PrepararNilo()
    {
        inicioNilo = sub.PosicionNilo;
        nilo = ActorFactory.CrearNilo(sub.Raiz, Vector3.zero, 0f);
        nilo.transform.position = inicioNilo;
        nilo.transform.rotation = Quaternion.Euler(0f, -60f, 0f);
        nilo.enabled = false;
        PlayerInteractor interactor = nilo.GetComponent<PlayerInteractor>();
        if (interactor != null) interactor.enabled = false;
        CharacterController cc = nilo.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        animNilo = nilo.GetComponentInChildren<Animator>();

        foreach (Transform t in nilo.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Head" && cabeza == null && t.GetComponent<MeshRenderer>() == null) cabeza = t;
            if (t.name == "Palm2.R" && mano == null) mano = t;
        }

        // Se le deja un fotograma de pie (el Animator coloca la pose) y después se le tumba.
        yield return null;
        PrepararAntena();
        Postura(0f);
    }

    /// <summary>
    /// Nilo tumbado o incorporándose: gira entero sobre los talones, rígido, como una máquina que
    /// arranca. 'inclinacionAtras' en grados (88 = boca arriba); 'avance' lo desplaza hacia delante.
    /// </summary>
    void PosturaNilo(float giro, float inclinacionAtras, float avance)
    {
        Quaternion rumbo = Quaternion.Euler(0f, giro, 0f);
        nilo.transform.rotation = rumbo * Quaternion.Euler(-inclinacionAtras, 0f, 0f);
        // La espalda tiene grosor: al tumbarse, se levanta un poco para no hundirse en el suelo.
        nilo.transform.position = inicioNilo + rumbo * Vector3.forward * avance
            + Vector3.up * (0.3f * Mathf.Clamp01(inclinacionAtras / 88f));
    }

    /// <summary>Postura de Nilo en el instante t de la parte B, antes de echar a andar.</summary>
    void Postura(float t)
    {
        const float Tumbado = 88f, Sentado = 40f;
        float L = B_Levantarse;
        float atras = Tumbado;
        if (t >= L + 0.5f)
        {
            // Se incorpora en dos tiempos: hasta medio cuerpo, un temblor de esfuerzo, y arriba.
            atras = Mathf.Lerp(Tumbado, Sentado, S(Tramo(t, L + 0.5f, L + 1.7f)));
            if (t >= L + 1.7f && t < L + 2.2f) atras += Mathf.Sin(t * 70f) * 1.6f;
            if (t >= L + 2.2f) atras = Mathf.Lerp(Sentado, 0f, S(Tramo(t, L + 2.2f, L + 3.2f)));
            if (t >= L + 3.2f && t < L + 3.7f) atras = -4f * Mathf.Sin(Mathf.PI * Tramo(t, L + 3.2f, L + 3.7f));
        }
        float avance = 0f;
        if (t >= L + 4.1f && t < L + 4.9f)
        {
            // El resbalón: se va hacia delante, recupera y se pasa un poco hacia atrás.
            float u = Tramo(t, L + 4.1f, L + 4.9f);
            atras = -16f * Mathf.Sin(Mathf.PI * u) + 6f * Mathf.Sin(2f * Mathf.PI * u);
            avance = 0.25f * S(u);
        }
        else if (t >= L + 4.9f) avance = 0.25f;

        float giro = GiroInicial;
        if (t >= L + 5.2f)
        {
            Vector3 dir = sub.Radio - inicioNilo; dir.y = 0f;
            float haciaRadio = Quaternion.LookRotation(dir).eulerAngles.y;
            giro = Mathf.LerpAngle(GiroInicial, haciaRadio, S(Tramo(t, L + 5.2f, B_Hombro)));
        }
        PosturaNilo(giro, atras, avance);

        // Apagado no se mueve: el Animator arranca con la antena.
        if (animNilo != null) animNilo.speed = t >= B_Mano + 1.4f ? 1f : 0f;
    }

    void PrepararAntena()
    {
        foreach (Renderer r in nilo.GetComponentsInChildren<Renderer>(true))
            if (r.name == "Antena punta" || r.name == "Aro de antena") antena.Add(r);
    }

    /// <summary>
    /// Nilo "abre los ojos": no lleva ojos luminosos (en las cinemáticas desentonaban con el resto
    /// del diseño); lo que se enciende al despertar es la luz de su antena.
    /// </summary>
    void Despierto(float k)
    {
        foreach (Renderer r in antena) r.enabled = k > 0.05f;
    }

    void OnDestroy()
    {
        if (entrada != null) entrada.Desactivar();
    }

    // ------------------------------------------------------------------ bucle

    void Update()
    {
        if (!listo || terminado) return;
        Salto();
        if (terminado) return;
        float previo = T;
        float dt = Time.deltaTime;

        // El diálogo con Maia retiene la cinemática hasta que termina.
        if (T >= B_Final - 0.5f && T < B_Final && dialogo != null && (dialogo.Abierto || mensajes < 1)) dt = 0f;
        T += dt;
        Evaluar(previo, T, dt);
        Estado = "T " + T.ToString("F1");
    }

    void LateUpdate()
    {
        if (cabeza != null && Mathf.Abs(inclinacion) > 0.01f)
            cabeza.localRotation = cabeza.localRotation * Quaternion.Euler(0f, 0f, inclinacion);
    }

    /// <summary>Mantener Esc salta la cinemática. Cualquier tecla muestra el aviso.</summary>
    void Salto()
    {
        Keyboard k = Keyboard.current;
        bool algo = k != null && k.anyKey.isPressed;
        bool esc = (k != null && k.escapeKey.isPressed) || (entrada != null && (entrada.Pausa.IsPressed() || entrada.Cancelar.IsPressed()));
        if (algo) avisoVisible = 3f;
        avisoVisible -= Time.unscaledDeltaTime;
        mantenerSalto = esc ? mantenerSalto + Time.unscaledDeltaTime : Mathf.Max(0f, mantenerSalto - Time.unscaledDeltaTime * 2f);

        float alfa = Mathf.Clamp01(avisoVisible) * 0.85f;
        aviso.color = new Color(aviso.color.r, aviso.color.g, aviso.color.b, alfa);
        avisoBarra.rectTransform.sizeDelta = new Vector2(240f * Mathf.Clamp01(mantenerSalto / 1.1f), 3f);

        // Interactuar completa la frase de Maia que se está escribiendo.
        if (entrada != null && entrada.Pulsado(entrada.Interactuar) && dialogo != null && dialogo.Abierto && !dialogo.TerminoDeEscribir)
            dialogo.Avanzar();

        if (mantenerSalto > 1.1f) Terminar();
    }

    void Terminar()
    {
        if (terminado) return;
        terminado = true;
        TerminarGrabacion();
#if UNITY_EDITOR
        if (grabado) return;
#endif
        if (!string.IsNullOrEmpty(escenaSiguiente) && Application.CanStreamedLevelBeLoaded(escenaSiguiente))
            SceneManager.LoadScene(escenaSiguiente);
    }

    /// <summary>
    /// Para revisar en el editor: avanza la cinemática hasta 'hasta' en pasos de 1/30 s, sin esperar
    /// a que pase el tiempo. Con Time.timeScale = 0 la imagen queda quieta en ese instante.
    /// </summary>
    public void Revisar(float hasta)
    {
        const float paso = 1f / 30f;
        while (T < hasta)
        {
            float previo = T;
            T = Mathf.Min(hasta, T + paso);
            Evaluar(previo, T, paso);
        }
    }

    // ------------------------------------------------------------------ evaluación

    bool Cruza(float previo, float ahora, float marca) { return previo < marca && ahora >= marca; }
    static float S(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
    static float Tramo(float t, float a, float b) { return Mathf.Clamp01((t - a) / (b - a)); }

    void Evaluar(float previo, float t, float dt)
    {
        EfectosBase();
        Sonido(previo, t);
        liria.Animar(t, dt);
        sub.Animar(t);

        if (t < B_Terminal) ParteA(previo, t);
        else ParteB(previo, t);

        if (t >= Fin) Terminar();
    }

    void EfectosBase()
    {
        vineta.intensity.Override(0.32f);
        vineta.color.Override(Color.black);
        color.postExposure.Override(0.35f);
        color.saturation.Override(6f);
        color.contrast.Override(10f);
        color.colorFilter.Override(Color.white);
        aberracion.intensity.Override(0.06f);
        dof.active = true;
        dof.aperture.Override(2.8f);
        dof.focalLength.Override(60f);
        bloom.intensity.Override(1.4f);
        Fundido(0f);
        float banda = (1080f - 1920f / 2.39f) * 0.5f;
        bandaArriba.rectTransform.sizeDelta = new Vector2(0f, banda);
        bandaAbajo.rectTransform.sizeDelta = new Vector2(0f, banda);
        parpadoArriba.rectTransform.sizeDelta = new Vector2(0f, 0f);
        parpadoAbajo.rectTransform.sizeDelta = new Vector2(0f, 0f);
        terminal.color = new Color(terminal.color.r, terminal.color.g, terminal.color.b, 0f);
        terminal.enabled = false;
        titulo.color = new Color(titulo.color.r, titulo.color.g, titulo.color.b, 0f);
        rotulo.color = new Color(rotulo.color.r, rotulo.color.g, rotulo.color.b, 0f);
        inclinacion = 0f;
    }

    /// <summary>Cámara en mano: un temblor lento y pequeño.</summary>
    Quaternion Mano(float fuerza)
    {
        float t = T * 0.8f;
        float x = (Mathf.PerlinNoise(t, 3.1f) - 0.5f) * 1.4f * fuerza;
        float y = (Mathf.PerlinNoise(7.7f, t) - 0.5f) * 1.4f * fuerza;
        return Quaternion.Euler(x, y, 0f);
    }

    void Colocar(Vector3 pos, Vector3 mira, float fov, float mano = 1f)
    {
        cam.fieldOfView = fov;
        cam.transform.position = pos;
        cam.transform.rotation = Quaternion.LookRotation(mira - pos) * Mano(mano);
        dof.focusDistance.Override(Vector3.Distance(pos, mira));
        cam.backgroundColor = RenderSettings.fogColor;
    }

    void Fundido(float alfa) { fundido.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alfa)); }

    bool? ambienteDia;

    /// <summary>
    /// Luz ambiente de la noche (la ciudad) o del día (la Subestación). Al cambiarla en ejecución
    /// hay que recalcular la sonda de ambiente, o los objetos siguen con la luz anterior.
    /// </summary>
    void AmbienteDe(bool dia)
    {
        if (ambienteDia == dia) return;
        ambienteDia = dia;
        RenderSettings.ambientSkyColor = dia ? new Color(0.8f, 0.82f, 0.86f) : new Color(0.34f, 0.4f, 0.62f);
        RenderSettings.ambientEquatorColor = dia ? new Color(0.62f, 0.58f, 0.52f) : new Color(0.22f, 0.24f, 0.36f);
        RenderSettings.ambientGroundColor = dia ? new Color(0.34f, 0.3f, 0.26f) : new Color(0.1f, 0.1f, 0.13f);
        DynamicGI.UpdateEnvironment();
    }

    // ------------------------------------------------------------------ parte A: la ciudad

    void ParteA(float previo, float t)
    {
        AmbienteDe(false);
        // Cortes de luz: de lejos a cerca, uno por pulso de la canción. El último farol se guarda.
        float progreso = Tramo(t, CorteInicio, CorteFin);
        if (t >= CorteInicio)
        {
            float pulsoActual = Mathf.Floor((t - CorteInicio) / SonidosApertura.DuracionPulso);
            // En perspectiva lo lejano ocupa poco: la mitad lejana de la ciudad se apaga en el primer
            // cuarto y lo que llena el plano, despacio, en el resto.
            float hasta = progreso < 0.25f ? progreso * 2f : 0.5f + (progreso - 0.25f) / 0.75f * 0.47f;
            int cortadas = 0;
            foreach (LiriaNocturna.Luz l in liria.Luces)
            {
                if (l == liria.UltimoFarol || l.cortadaEn >= 0f || l.orden > hasta) continue;
                l.cortadaEn = CorteInicio + pulsoActual * SonidosApertura.DuracionPulso + (l.semilla % 1f) * 0.1f;
                cortadas++;
            }
            if (cortadas > 0 && pulsoActual > ultimoPulso && t < A_Ultimo)
            {
                ultimoPulso = pulsoActual;
                efectos.PlayOneShot(clipRele, Mathf.Lerp(0.25f, 0.9f, progreso));
            }
        }
        if (Cruza(previo, t, CorteUltimo - 0.2f)) liria.Chisporrotear(liria.UltimoFarol.posicion, 60);
        if (Cruza(previo, t, CorteUltimo)) { liria.Cortar(liria.UltimoFarol, t); efectos.PlayOneShot(clipRele, 1f); }
        liria.VelocidadRueda = 1f - S(Tramo(t, A_Apagon, A_Agua));
        // Sin faroles, la ciudad queda a la luz de la luna, más fría y más fuerte.
        luna.intensity = Mathf.Lerp(0.5f, 1.05f, progreso);
        luna.color = Color.Lerp(new Color(0.55f, 0.65f, 0.95f), new Color(0.45f, 0.58f, 1f), progreso);
        liria.SubirAgua(S(Tramo(t, A_Apagon + 1f, A_Agua + 2.2f)));

        // Coordenadas de la Liria nueva en el mundo = las de Blender (x, y de Blender -> x, z): el mar
        // abierto está hacia -z, la cabeza del muelle B01 en z = -14,5 y las casas de B01 entre z = 1 y 17;
        // la calle del barrio de piedra corre en x a z = 35, el canal (B03) entre x = 24 y 32 (z 2..14) y
        // el faro está en su peñasco hacia (-38, 40).
        const float A_Faro = 6.5f;
        if (t < A_Panoramica)
        {
            Fundido(1f);
            Colocar(new Vector3(-16f, 24f, -50f), new Vector3(0f, 2f, 18f), 40f, 0.2f);
        }
        else if (t < A_Faro)
        {
            // P1 · Panorámica desde el mar: la cámara baja despacio hacia el pueblo encendido.
            float s = S(Tramo(t, A_Panoramica, A_Faro));
            Colocar(Vector3.Lerp(new Vector3(-16f, 24f, -50f), new Vector3(-8f, 11f, -36f), s),
                Vector3.Lerp(new Vector3(0f, 2f, 18f), new Vector3(2f, 4f, 22f), s), Mathf.Lerp(40f, 44f, s), 0.25f);
            dof.focusDistance.Override(55f);
            dof.aperture.Override(8f);
            Fundido(1f - S(Tramo(t, A_Panoramica, A_Panoramica + 2.2f)));
            float r = Tramo(t, A_Panoramica + 1.2f, A_Panoramica + 2.0f) * (1f - Tramo(t, A_Faro - 0.9f, A_Faro - 0.2f));
            rotulo.color = new Color(rotulo.color.r, rotulo.color.g, rotulo.color.b, r);
        }
        else if (t < A_Farol)
        {
            // P2 · El faro encendido sobre su peñasco, visto desde el agua en contrapicado.
            float s = S(Tramo(t, A_Faro, A_Farol));
            Colocar(Vector3.Lerp(new Vector3(-20f, 2.5f, 20f), new Vector3(-24f, 3.5f, 24f), s),
                new Vector3(-38f, Mathf.Lerp(9f, 13f, s), 40f), 40f, 0.3f);
            dof.focusDistance.Override(24f);
            dof.aperture.Override(4f);
            rotulo.color = new Color(rotulo.color.r, rotulo.color.g, rotulo.color.b, 0f);
        }
        else if (t < A_Barca)
        {
            // P3 · Travelling por la calle del barrio de piedra: fachadas con ventanas encendidas a
            // los dos lados, faroles, ropa tendida. Ni una persona: la vida está en las luces.
            float s = S(Tramo(t, A_Farol, A_Barca));
            Colocar(Vector3.Lerp(new Vector3(-16f, 2.3f, 34.5f), new Vector3(-7f, 2.6f, 35.5f), s),
                Vector3.Lerp(new Vector3(4f, 3.6f, 37f), new Vector3(16f, 4.2f, 36f), s), 50f, 0.3f);
            dof.focusDistance.Override(12f);
            dof.aperture.Override(4f);
        }
        else if (t < A_Ventana)
        {
            // P4 · Por el canal, desde el agua: la rueda de la bomba girando y la barca con su vela.
            float s = S(Tramo(t, A_Barca, A_Ventana));
            Colocar(Vector3.Lerp(new Vector3(27.5f, 1.3f, -4f), new Vector3(27.5f, 1.5f, 1f), s),
                new Vector3(26.5f, 1.4f, 14f), 46f, 0.4f);
            dof.focusDistance.Override(9f);
            dof.aperture.Override(3f);
        }
        else if (t < A_Apagon)
        {
            // P5 · Una fachada con balcón entre ventanas encendidas, sin nadie a la vista.
            float s = S(Tramo(t, A_Ventana, A_Apagon));
            Vector3 fachada = liria.Balcon;
            Colocar(Vector3.Lerp(new Vector3(2f, 2.4f, 31.5f), new Vector3(0.5f, 2.8f, 32.5f), s), fachada, 36f, 0.4f);
            dof.focusDistance.Override(7f);
            dof.aperture.Override(2.2f);
        }
        else if (t < A_Ultimo)
        {
            // P6 · El apagón: plano general desde el mar por el eje del muelle. Las luces caen de
            // lejos (el barrio de piedra) a cerca (la cabeza del muelle); el agua sube bajo la luna.
            float s = S(Tramo(t, A_Apagon, A_Ultimo));
            Colocar(Vector3.Lerp(new Vector3(-2f, 6.5f, -32f), new Vector3(-1.5f, 5f, -26f), s), new Vector3(1f, 3f, 30f), 46f, 0.35f);
            dof.focusDistance.Override(35f);
            dof.aperture.Override(7f);
            color.postExposure.Override(-0.5f * progreso);
        }
        else if (t < A_Titulo)
        {
            // P6 · El último farol: primer plano, chisporrotea y muere. Negro.
            float s = S(Tramo(t, A_Ultimo, CorteUltimo + 0.6f));
            Vector3 f = liria.UltimoFarol.posicion;
            Colocar(f + Vector3.Lerp(new Vector3(-1.3f, -0.15f, 2.3f), new Vector3(-0.9f, -0.1f, 1.6f), s), f, 34f, 0.4f);
            dof.focusDistance.Override(1.8f);
            dof.aperture.Override(1.6f);
            Fundido(Tramo(t, CorteUltimo + 0.25f, CorteUltimo + 0.45f));
        }
        else
        {
            // Título sobre negro.
            Fundido(1f);
            Colocar(new Vector3(0f, 34f, -52f), new Vector3(0f, 0f, 30f), 40f, 0f);
            float a = S(Tramo(t, A_Titulo + 0.6f, A_Titulo + 2.2f)) * (1f - S(Tramo(t, B_Terminal - 1.6f, B_Terminal - 0.3f)));
            titulo.color = new Color(titulo.color.r, titulo.color.g, titulo.color.b, a);
            titulo.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.04f, 1f, Tramo(t, A_Titulo, B_Terminal));
        }
    }

    // ------------------------------------------------------------------ parte B: el despertar

    static readonly string[] Lineas =
    {
        "NILO · reinicio de emergencia",
        "Núcleo de energía ........ 1 batería",
        "Unidad de memoria ........ 0 bytes",
        "Último acceso ............ FORJA",
        "Red de Liria ............. sin respuesta",
    };

    int mensajes;
    float ultimoPulso = -1f;

    void ParteB(float previo, float t)
    {
        // El apagón fue de noche; Nilo despierta de día, como empieza el primer nivel.
        AmbienteDe(true);
        luna.intensity = 0f;
        Vector3 n = nilo.transform.position;
        Vector3 cab = cabeza != null ? cabeza.position : n + Vector3.up * 1.5f;

        // --- Nilo: tumbado, se levanta, resbala, camina hacia la radio.
        if (t < B_Hombro) Postura(t);
        if (Cruza(previo, t, B_Levantarse + 4.1f)) efectos.PlayOneShot(clipAgua, 0.6f);

        Vector3 frenteRadio = sub.Radio + sub.FrenteRadio * 1.15f;
        frenteRadio.y = inicioNilo.y;
        if (t >= B_Hombro)
        {
            Vector3 desde = inicioNilo + Quaternion.Euler(0f, GiroInicial, 0f) * Vector3.forward * 0.25f;
            Vector3 dir = frenteRadio - desde; dir.y = 0f;
            nilo.transform.position = Vector3.Lerp(desde, frenteRadio, S(Tramo(t, B_Hombro + 0.3f, B_Hombro + 3.2f)) * 0.82f);
            nilo.transform.rotation = Quaternion.LookRotation(dir);
        }
        n = nilo.transform.position;

        // --- Nilo despierta: se enciende la antena (y parpadea al final)
        float despierto = 0f;
        if (t >= B_Mano + 1.4f) despierto = S(Tramo(t, B_Mano + 1.4f, B_Mano + 2.4f)) * (Mathf.PerlinNoise(t * 9f, 4f) > 0.25f || t > B_Mano + 2.4f ? 1f : 0.2f);
        if (t >= B_Final + 1.2f && t < B_Final + 1.45f) despierto = 0.05f;   // parpadeo
        if (t >= B_Final + 1.9f && t < B_Final + 2.05f) despierto = 0.05f;
        Despierto(despierto);
        if (Cruza(previo, t, B_Mano + 1.4f)) efectos.PlayOneShot(clipArranque, 0.8f);

        // --- radio: se enciende sola cuando Nilo aún no ha llegado.
        float radio = S(Tramo(t, B_Hombro + 2.2f, B_Hombro + 2.9f));
        sub.EncenderRadio(radio);
        if (Cruza(previo, t, B_Hombro + 2.2f)) { sub.Altavoz.Play(); }
        sub.Altavoz.volume = 0.45f * radio * (1f - 0.6f * Tramo(t, B_Radio + 0.9f, B_Radio + 1.3f));
        if (Cruza(previo, t, B_Hombro + 3.2f) && animNilo != null) animNilo.SetTrigger("No");

        // --- Maia
        if (Cruza(previo, t, B_Radio + 1.0f))
        {
            mensajes = 0;
            dialogo.Encolar("Maia", "¿Hola? ¿Hay alguien en la red? ...¡Nilo! Sabía que ibas a despertar.", RelevoUI.Verde);
            relojMensaje = 0f;
        }
        // Mientras la cinemática espera al diálogo, T no avanza: el reloj de lectura usa el tiempo real.
        if (t >= B_Radio + 1.0f && dialogo != null) GestionarDialogo(Mathf.Max(t - previo, Time.deltaTime));

        // --- ondas en el agua
        if (mano != null && (Cruza(previo, t, B_Mano + 0.6f) || Cruza(previo, t, B_Mano + 1.5f) || Cruza(previo, t, B_Mano + 2.7f)))
            sub.Onda(mano.position, 0.9f, t);
        if (Cruza(previo, t, B_Levantarse + 1.2f)) sub.Onda(n + nilo.transform.forward * 0.3f, 1.8f, t);
        if (Cruza(previo, t, B_Levantarse + 4.4f)) sub.Onda(n + nilo.transform.right * 0.2f, 1.6f, t);
        if (t > B_Hombro + 0.3f && t < B_Hombro + 3.2f && Mathf.Repeat(t, 0.55f) < Mathf.Repeat(previo, 0.55f)) sub.Onda(n, 1f, t);

        // --- planos
        if (t < B_Ojos)
        {
            // B0 · Terminal sobre negro.
            Fundido(1f);
            float k = t - B_Terminal - 0.6f;
            terminal.enabled = true;
            terminal.text = Escribir(k);
            float glitch = Tramo(t, B_Ojos - 0.6f, B_Ojos);
            terminal.color = new Color(0.45f, 1f, 0.6f, glitch > 0f && Mathf.PerlinNoise(t * 40f, 0f) > 0.5f ? 0.2f : 1f - glitch);
            Colocar(cab + new Vector3(0f, 0.3f, 0f), sub.Radio, 50f, 0f);
            for (int i = 0; i < Lineas.Length; i++)
                if (Cruza(previo, t, B_Terminal + 0.6f + InicioLinea(i))) efectos.PlayOneShot(clipPitido, 0.5f);
        }
        else if (t < B_Mano)
        {
            // B1 · Primera persona en el suelo: el obturador de los ojos se abre a trompicones.
            float u = t - B_Ojos;
            Fundido(1f - S(Tramo(t, B_Ojos, B_Ojos + 0.4f)));
            float apertura = Parpados(u);
            parpadoArriba.rectTransform.sizeDelta = new Vector2(0f, 560f * (1f - apertura));
            parpadoAbajo.rectTransform.sizeDelta = new Vector2(0f, 560f * (1f - apertura));
            Vector3 mira = Vector3.Lerp(sub.Haz, sub.Radio, S(Tramo(u, 2.8f, 5.2f)));
            // Por delante de la cabeza, para no ver por dentro las lentes.
            Vector3 ojo = cab + Vector3.up * 0.15f + (mira - cab).normalized * 0.6f;
            Colocar(ojo, mira, 58f, 0.8f);
            float enfoque = S(Tramo(u, 1.2f, 4.6f));
            dof.focusDistance.Override(Mathf.Lerp(0.25f, Vector3.Distance(ojo, sub.Radio), enfoque));
            dof.aperture.Override(Mathf.Lerp(1f, 3.2f, enfoque));
            aberracion.intensity.Override(Mathf.Lerp(1f, 0.15f, enfoque));
            color.saturation.Override(Mathf.Lerp(-60f, -10f, enfoque));
            vineta.intensity.Override(0.5f);
            if (Cruza(previo, t, B_Ojos + 0.2f)) efectos.PlayOneShot(clipArranque, 0.4f);
        }
        else if (t < B_Levantarse)
        {
            // B2 · Detalle: la mano en el agua; los dedos tiemblan y la antena se enciende.
            float s = S(Tramo(t, B_Mano, B_Levantarse));
            Vector3 m = mano != null ? mano.position : n;
            Vector3 lateral = Vector3.Cross(Vector3.up, (cab - m).normalized).normalized;
            Colocar(m + lateral * Mathf.Lerp(0.9f, 0.7f, s) + Vector3.up * 0.18f - (cab - m).normalized * 0.4f, m + Vector3.up * 0.02f, 34f, 0.4f);
            dof.aperture.Override(1.4f);
            color.saturation.Override(-10f);
        }
        else if (t < B_Panel)
        {
            // B3 · Plano medio a la altura de la rodilla: se levanta, resbala, se recompone.
            float s = S(Tramo(t, B_Levantarse, B_Panel));
            Vector3 lado = Quaternion.Euler(0f, -60f, 0f) * Vector3.right;
            Vector3 desde = inicioNilo + lado * 3.4f + Vector3.up * 0.7f + Vector3.back * 0.8f;
            // La cámara sigue la cabeza: tumbado, levantándose y en el resbalón.
            Vector3 mira = Vector3.Lerp(cab, n + Vector3.up * 0.9f, 0.45f);
            Colocar(Vector3.Lerp(desde, desde + Vector3.up * 0.3f + lado * -0.2f, s), mira, 44f, 0.6f);
            if (t > B_Levantarse + 4.1f && t < B_Levantarse + 4.9f) cam.transform.rotation *= Quaternion.Euler(Mathf.Sin(t * 50f) * 0.4f, 0f, 0f);
        }
        else if (t < B_Hombro)
        {
            // B4 · El foco pasa por el panel quemado (en primer término) y se va a Nilo.
            float s = S(Tramo(t, B_Panel, B_Hombro));
            Vector3 q = sub.Quemadura;
            Vector3 desde = q + new Vector3(-1.4f, 0.1f, -0.9f), hasta = q + new Vector3(0.9f, -0.05f, -1.0f);
            Vector3 pos = Vector3.Lerp(desde, hasta, s);
            Vector3 mira = Vector3.Lerp(q, n + Vector3.up * 1.2f, S(Tramo(t, B_Panel + 0.9f, B_Panel + 2.6f)));
            Colocar(pos, mira, 38f, 0.5f);
            float foco = S(Tramo(t, B_Panel + 0.8f, B_Panel + 2.1f));
            dof.focusDistance.Override(Mathf.Lerp(Vector3.Distance(pos, q), Vector3.Distance(pos, n), foco));
            dof.aperture.Override(1.7f);
        }
        else if (t < B_Radio)
        {
            // B5 · Sobre el hombro: Nilo avanza hacia la radio y la radio se enciende antes de que llegue.
            Vector3 hacia = (sub.Radio - n); hacia.y = 0f; hacia.Normalize();
            Vector3 hombro = n - hacia * 2.3f + Vector3.Cross(Vector3.up, hacia) * 1.05f + Vector3.up * 2.15f;
            Colocar(hombro, sub.Radio + Vector3.down * 0.15f, 38f, 0.7f);
            dof.focusDistance.Override(Vector3.Distance(hombro, sub.Radio));
            dof.aperture.Override(2.4f);
        }
        else if (t < B_Final)
        {
            // B6 · Primer plano de la radio: estática, la canción que se cuela y la voz de Maia.
            float s = S(Tramo(t, B_Radio, B_Final));
            Vector3 r = sub.Radio;
            Vector3 frente = sub.FrenteRadio;
            Colocar(r + frente * Mathf.Lerp(1.25f, 0.95f, s) + Vector3.right * 0.35f + Vector3.up * 0.05f, r + Vector3.right * 0.05f, 30f, 0.35f);
            dof.aperture.Override(1.8f);
            if (mensajes >= 1) inclinacion = Mathf.Sin(Mathf.PI * Tramo(t, B_Radio + 2.2f, B_Radio + 3.6f)) * 14f;
        }
        else
        {
            // B7 · Nilo inclina la cabeza; la cámara sube hasta la vista del juego. Fundido.
            float s = S(Tramo(t, B_Final, Fin - 1f));
            Vector3 frente = (sub.Radio - n); frente.y = 0f; frente.Normalize();
            Vector3 cerca = n + frente * 1.7f + Vector3.up * 1.45f + Vector3.Cross(Vector3.up, frente) * 0.9f;
            Vector3 alto = n + new Vector3(-7f, 9f, -7f);
            Colocar(Vector3.Lerp(cerca, alto, S(Tramo(t, B_Final + 2.4f, Fin - 0.6f))), n + Vector3.up * Mathf.Lerp(1.5f, 0.8f, s), Mathf.Lerp(36f, 30f, s), 0.3f);
            inclinacion = Mathf.Sin(Mathf.PI * Tramo(t, B_Final + 0.2f, B_Final + 2.2f)) * 18f;
            Fundido(S(Tramo(t, Fin - 1.6f, Fin - 0.2f)));
            // Al subir, las bandas se retiran: es el paso a la vista del juego.
            float b = 1f - S(Tramo(t, B_Final + 2.6f, Fin - 1f));
            float banda = (1080f - 1920f / 2.39f) * 0.5f * b;
            bandaArriba.rectTransform.sizeDelta = new Vector2(0f, banda);
            bandaAbajo.rectTransform.sizeDelta = new Vector2(0f, banda);
        }
    }

    float relojMensaje;

    void GestionarDialogo(float dt)
    {
        if (dialogo.Abierto && dialogo.TerminoDeEscribir) relojMensaje += dt;
        if (!dialogo.Abierto || relojMensaje < 2.6f) return;
        relojMensaje = 0f;
        mensajes++;
        dialogo.Avanzar();
    }

    float InicioLinea(int i)
    {
        float t = 0f;
        for (int k = 0; k < i; k++) t += Lineas[k].Length / 70f + 0.45f;
        return t;
    }

    /// <summary>El texto de la terminal a los 'k' segundos de empezar a escribirse.</summary>
    string Escribir(float k)
    {
        if (k < 0f) return Mathf.Repeat(k, 0.8f) < 0.4f ? "_" : "";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < Lineas.Length; i++)
        {
            float inicio = InicioLinea(i);
            if (k < inicio) break;
            int letras = Mathf.Min(Lineas[i].Length, Mathf.FloorToInt((k - inicio) * 34f));
            string linea = Lineas[i].Substring(0, letras);
            if (i == Lineas.Length - 1 && letras == Lineas[i].Length) linea = "Red de Liria ............. <color=#FF9A4D>sin respuesta</color>";
            if (i == 3 && letras == Lineas[i].Length) linea = "Último acceso ............ <color=#FFD27A>FORJA</color>";
            sb.Append(linea).Append('\n');
        }
        if (Mathf.Repeat(k, 0.8f) < 0.4f) sb.Append('_');
        return sb.ToString();
    }

    /// <summary>Apertura de los párpados (0 cerrados, 1 abiertos): dos intentos y luego se abren.</summary>
    static float Parpados(float u)
    {
        if (u < 0.5f) return 0f;
        if (u < 1.3f) return 0.25f * Mathf.Sin(Mathf.PI * (u - 0.5f) / 0.8f);
        if (u < 1.8f) return 0f;
        if (u < 2.8f) return 0.55f * S((u - 1.8f) / 0.5f) * (u < 2.5f ? 1f : 1f - S((u - 2.5f) / 0.3f));
        if (u < 3.1f) return 0f;
        return S((u - 3.1f) / 0.9f);
    }

    // ------------------------------------------------------------------ sonido

    void Sonido(float previo, float t)
    {
        // Música: empieza como una radio lejana y se abre al bajar sobre la ciudad.
        if (Cruza(previo, t, 0.5f) || (previo <= 0f && t > 0.5f && t < A_Ultimo && !musica.isPlaying))
        {
            musica.time = Mathf.Max(0f, t - 0.5f) % musica.clip.length;
            musica.Play();
        }
        float apertura = S(Tramo(t, A_Panoramica, A_Panoramica + 6f));
        pasaAltos.cutoffFrequency = Mathf.Lerp(900f, 10f, apertura);
        pasaBajos.cutoffFrequency = Mathf.Lerp(2400f, 22000f, apertura);
        float vol = 0.75f * S(Tramo(t, 0.5f, 3f));

        // Durante el apagón la canción se deforma: baja de tono como una cinta que se frena.
        float deformar = S(Tramo(t, A_Apagon + 1f, CorteUltimo));
        musica.pitch = Mathf.Lerp(1f, 0.62f, deformar * deformar);
        distorsion.distortionLevel = Mathf.Lerp(0f, 0.55f, deformar);
        pasaBajos.cutoffFrequency = Mathf.Min(pasaBajos.cutoffFrequency, Mathf.Lerp(22000f, 1600f, deformar));
        if (t >= CorteUltimo) vol = 0f;
        if (Cruza(previo, t, CorteUltimo)) musica.Stop();
        musica.volume = vol;

        // Estática al principio, sobre negro.
        if (Cruza(previo, t, 0.01f) || (t < A_Panoramica + 2f && !estatica.isPlaying && t > 0f)) estatica.Play();
        estatica.volume = t < B_Terminal ? 0.4f * (1f - S(Tramo(t, A_Panoramica, A_Panoramica + 3f))) : 0f;

        // Ambiente: la ciudad zumba mientras tiene luz; en la Subestación, agua y goteo.
        if (t < B_Terminal)
        {
            if (ambiente.clip != clipZumbido) { ambiente.clip = clipZumbido; ambiente.Play(); }
            ambiente.volume = 0.12f * (1f - ProgresoApagon(t)) * S(Tramo(t, A_Panoramica, A_Panoramica + 2f));
        }
        else
        {
            if (ambiente.clip != clipAgua) { ambiente.clip = clipAgua; ambiente.Play(); }
            ambiente.volume = 0.55f * S(Tramo(t, B_Ojos, B_Ojos + 1.5f)) * (1f - S(Tramo(t, Fin - 1.5f, Fin)));
        }

        if (Cruza(previo, t, A_Titulo + 0.5f)) efectos.PlayOneShot(clipGolpe, 0.9f);
        efectos.volume = 1f;

        // La canción se cuela por la estática de la radio de la Subestación, un instante.
        if (Cruza(previo, t, B_Radio + 1.2f))
        {
            musica.pitch = 1f;
            distorsion.distortionLevel = 0.2f;
            musica.time = 0f;
            musica.Play();
        }
        if (t >= B_Radio + 1.2f)
        {
            musica.pitch = 1f;
            pasaAltos.cutoffFrequency = 1200f;
            pasaBajos.cutoffFrequency = 2600f;
            distorsion.distortionLevel = 0.25f;
            float fragmento = S(Tramo(t, B_Radio + 1.2f, B_Radio + 1.9f)) * (1f - S(Tramo(t, B_Radio + 3.4f, B_Radio + 4.4f)));
            musica.volume = 0.45f * fragmento * (Mathf.PerlinNoise(t * 3f, 9f) > 0.3f ? 1f : 0.3f);
            if (t > B_Radio + 4.5f && musica.isPlaying) musica.Stop();
        }
    }

    float ProgresoApagon(float t) { return S(Tramo(t, CorteInicio, CorteUltimo)); }

    // ------------------------------------------------------------------ grabación (solo editor)

#if UNITY_EDITOR
    MediaEncoder codificador;
    RenderTexture lienzoVideo;
    Texture2D imagen;
    string ruta;
    bool grabando, grabado;
    int fotograma;

    /// <summary>Deja pedida la grabación a MP4 de la próxima vez que se juegue la escena.</summary>
    public static string Grabar(string ruta)
    {
        UnityEditor.SessionState.SetString("RelevoAperturaRuta", ruta);
        Estado = "pendiente";
        return ruta;
    }

    void IniciarGrabacion()
    {
        ruta = UnityEditor.SessionState.GetString("RelevoAperturaRuta", "");
        UnityEditor.SessionState.EraseString("RelevoAperturaRuta");
        if (string.IsNullOrEmpty(ruta)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        if (File.Exists(ruta)) File.Delete(ruta);
        codificador = new MediaEncoder(ruta, new VideoTrackAttributes
        {
            frameRate = new MediaRational(30), width = 1920, height = 1080,
            includeAlpha = false, bitRateMode = UnityEditor.VideoBitrateMode.High
        });
        lienzoVideo = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
        imagen = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
        cam.targetTexture = lienzoVideo;
        Time.captureFramerate = 30;
        grabando = true;
        StartCoroutine(Fotogramas());
    }

    IEnumerator Fotogramas()
    {
        while (grabando)
        {
            yield return new WaitForEndOfFrame();
            if (!grabando) break;
            RenderTexture previa = RenderTexture.active;
            RenderTexture.active = lienzoVideo;
            imagen.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0, false);
            imagen.Apply(false);
            RenderTexture.active = previa;
            codificador.AddFrame(imagen);
            if (fotograma % 30 == 0)
                File.WriteAllBytes(Path.ChangeExtension(ruta, null) + "_" + fotograma.ToString("0000") + ".jpg", imagen.EncodeToJPG(85));
            fotograma++;
        }
    }

    void TerminarGrabacion()
    {
        if (!grabando) return;
        grabando = false;
        Time.captureFramerate = 0;
        codificador.Dispose();
        codificador = null;
        cam.targetTexture = null;
        lienzoVideo.Release();
        Estado = "grabado " + fotograma + " fotogramas (" + (fotograma / 30f).ToString("F1") + " s) " + ruta;
        grabado = true;    // tras grabar no se cambia de escena: el video se revisa en el editor
    }
#else
    void IniciarGrabacion() { }
    void TerminarGrabacion() { }
#endif
}
