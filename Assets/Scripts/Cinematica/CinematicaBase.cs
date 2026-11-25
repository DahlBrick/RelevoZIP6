using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Base de las cinemáticas de RELEVO (La antena, El renglón y El relevo; El banco de fundición es
/// anterior y lleva su propia copia de esto). Monta la cámara de cine con su volumen, las bandas
/// 2,39:1, los rótulos, el diálogo, el sonido y la entrada, y ofrece piezas de decorado comunes:
/// Nilo quieto, la radio de Maia, el muelle y una Liria de barrios con ventanas que se encienden.
///
/// Toda la puesta en escena es función del tiempo T (Evaluar), como la apertura. El shader cómic
/// solo atiende a la luz principal: la iluminación de cada plano se hace con Luna() y los brillos
/// con materiales que emiten (Brillo).
/// </summary>
public abstract class CinematicaBase : MonoBehaviour
{
    [SerializeField] protected InputActionAsset acciones;

    public static string Estado = "";

    protected float T;
    protected bool terminado;
    protected bool esperandoEleccion;
    protected int eleccion = -1;

    protected RelevoInput entrada;
    protected Camera cam;
    protected Vignette vineta;
    protected ColorAdjustments color;
    protected Bloom bloom;
    protected Light luna;
    protected Image fundido, destello, avisoBarra;
    protected Text rotulo, pie, cita, aviso;
    protected DialogoUI dialogo;
    protected AudioSource agua, estatica, efectos, musica;
    protected AudioClip clipGolpe, clipRele, clipPitido, clipArranque;

    float mantenerSalto, avisoVisible, sacudida;
    bool flanco, listo;
    Vector3 basePos;

    // ------------------------------------------------------------------ lo que pone cada cinemática

    /// <summary>Duración total; al llegar se llama a Terminar.</summary>
    protected abstract float Fin { get; }
    /// <summary>Monta el decorado (se llama antes de aplicar el estilo cómic).</summary>
    protected abstract void Construir();
    protected abstract void Evaluar(float previo, float t, float dt);
    /// <summary>Qué hace mantener Esc. Por defecto, terminar.</summary>
    protected virtual void Saltar() { Terminar(); }
    /// <summary>Verdadero mientras la cinemática deba quedarse quieta (un diálogo abierto, por ejemplo).</summary>
    protected virtual bool Retener() { return esperandoEleccion; }
    /// <summary>Escena a la que se va al terminar.</summary>
    protected abstract string Destino();
    /// <summary>Último momento antes de salir (guardar una decisión, por ejemplo).</summary>
    protected virtual void AlTerminar() { }

    // ------------------------------------------------------------------ arranque

    System.Collections.IEnumerator Start()
    {
        Application.targetFrameRate = 60;
        Time.timeScale = 1f;
        GameEvents.Limpiar();
        RelevoGame.Limpiar();
        acciones = KitRelevo.Acciones(acciones);
        entrada = acciones != null ? new RelevoInput(acciones) : null;

        AmbienteNoche();
        PrepararCamara();
        PrepararInterfaz();
        PrepararSonido();
        Construir();
        yield return null;   // un fotograma para que los Animator de Nilo coloquen su pose

        EstiloComic.Aplicar(transform, new Color(0.3f, 0.32f, 0.5f));
        EstiloComic.RendererTinta(cam);
        DespuesDelEstilo();
        listo = true;
        Evaluar(0f, 0f, 0f);
    }

    /// <summary>Tras pasar los materiales a cómic (para buscar los materiales ya convertidos).</summary>
    protected virtual void DespuesDelEstilo() { }

    void AmbienteNoche()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.22f, 0.26f, 0.42f);
        RenderSettings.ambientEquatorColor = new Color(0.14f, 0.15f, 0.24f);
        RenderSettings.ambientGroundColor = new Color(0.06f, 0.06f, 0.09f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.04f, 0.06f, 0.11f);
        RenderSettings.fogStartDistance = 25f;
        RenderSettings.fogEndDistance = 140f;
        RenderSettings.skybox = null;

        luna = new GameObject("Luna").AddComponent<Light>();
        luna.transform.SetParent(transform, false);
        luna.type = LightType.Directional;
        luna.shadows = LightShadows.Soft;
        Luna(1.05f, new Color(0.6f, 0.68f, 0.95f), new Vector3(42f, -35f, 0f));
    }

    void PrepararCamara()
    {
        var go = new GameObject("Camara de cine");
        go.transform.SetParent(transform, false);
        go.tag = "MainCamera";
        cam = go.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 600f;
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
        destello = Bloque(r, "Destello", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), 0f, Color.clear);
        destello.rectTransform.offsetMin = destello.rectTransform.offsetMax = Vector2.zero;
        Bloque(r, "Banda arriba", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), banda, Color.black);
        Bloque(r, "Banda abajo", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), banda, Color.black);

        rotulo = RelevoUI.Texto(r, "Rotulo", Vector2.zero, Vector2.zero, new Vector2(110f, banda + 46f), new Vector2(1500f, 50f), 34, TextAnchor.LowerLeft, Transparente(RelevoUI.Tinta));
        rotulo.font = RelevoUI.FuenteTitulo();
        pie = RelevoUI.Texto(r, "Pie", Vector2.zero, Vector2.zero, new Vector2(110f, banda + 8f), new Vector2(1600f, 36f), 24, TextAnchor.LowerLeft, Transparente(RelevoUI.Tinta));
        cita = RelevoUI.Texto(r, "Cita", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -250f), new Vector2(1600f, 60f), 40, TextAnchor.MiddleCenter, Transparente(RelevoUI.Ambar));
        cita.font = RelevoUI.Fuente();
        cita.fontStyle = FontStyle.Bold;

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
        clipGolpe = SonidosApertura.Golpe();
        clipRele = SonidosApertura.Rele();
        clipPitido = SonidosApertura.Pitido(880f);
        clipArranque = SonidosApertura.Arranque();
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
        if (!listo || terminado) return;
        Salto();
        if (terminado) return;
        ManejarDialogo();
        float previo = T;
        float dt = Retener() ? 0f : Time.deltaTime;
        T += dt;
        Evaluar(previo, T, dt);
        AplicarSacudida(dt);
        Estado = name + " T " + T.ToString("F1") + (esperandoEleccion ? " eligiendo" : "") + (eleccion >= 0 ? " eleccion " + eleccion : "");
        if (T >= Fin) Terminar();
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

    void Salto()
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
        Saltar();
    }

    /// <summary>Para revisar desde el editor: avanza hasta 'hasta' sin esperar; 'opcion' responde a la elección.</summary>
    public void Revisar(float hasta, int opcion = -1)
    {
        const float paso = 1f / 30f;
        while (T < hasta && !terminado)
        {
            if (esperandoEleccion)
            {
                if (opcion < 0) return;
                dialogo.Cerrar();
                ElegirOpcion(opcion);
            }
            if (dialogo.Abierto && !esperandoEleccion) dialogo.Cerrar();
            float p = T;
            T = Mathf.Min(hasta, T + paso);
            Evaluar(p, T, paso);
            if (T >= Fin) Terminar();
        }
    }

    protected void Terminar()
    {
        if (terminado) return;
        AlTerminar();
        terminado = true;
        if (entrada != null) entrada.Desactivar();
        SceneManager.LoadScene(Destino());
    }

    void OnDestroy()
    {
        if (entrada != null) entrada.Desactivar();
    }

    // ------------------------------------------------------------------ elección y voces

    /// <summary>Abre la elección una sola vez y detiene la cinemática hasta que se responda.</summary>
    protected void OfrecerEleccion(string titulo, string texto, string[] opciones)
    {
        if (esperandoEleccion || eleccion >= 0) return;
        esperandoEleccion = true;
        dialogo.Encolar(new DialogoUI.Mensaje
        {
            titulo = titulo, texto = texto, acento = RelevoUI.Ambar, opciones = opciones,
            alElegir = ElegirOpcion
        });
    }

    protected virtual void ElegirOpcion(int opcion)
    {
        eleccion = opcion;
        esperandoEleccion = false;
    }

    /// <summary>Una línea de Maia en el diálogo (ya escrita con la interferencia que toque).</summary>
    protected void Maia(string texto)
    {
        dialogo.Encolar(new DialogoUI.Mensaje { titulo = "Maia", texto = texto, acento = RelevoUI.Verde });
    }

    // ------------------------------------------------------------------ utilidades de puesta en escena

    protected static float S(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
    protected static float Tramo(float t, float a, float b) { return Mathf.Clamp01((t - a) / (b - a)); }
    protected static bool Cruza(float previo, float ahora, float marca) { return previo < marca && ahora >= marca; }
    protected static string Escribir(string s, float k) { return s.Substring(0, Mathf.Clamp(Mathf.RoundToInt(s.Length * k), 0, s.Length)); }
    /// <summary>Sube en [a, a+1] y baja en [b-1, b].</summary>
    protected static float Ventana(float t, float a, float b, float rampa = 1f) { return Tramo(t, a, a + rampa) * (1f - Tramo(t, b - rampa, b)); }

    /// <summary>Valores de cada fotograma; las cinemáticas los cambian después en su plano.</summary>
    protected void Base()
    {
        vineta.intensity.Override(0.36f);
        color.postExposure.Override(0.15f);
        color.saturation.Override(-4f);
        color.contrast.Override(12f);
        color.colorFilter.Override(Color.white);
        bloom.intensity.Override(1.3f);
        Fundido(0f);
        destello.color = Color.clear;
        Alfa(rotulo, 0f); Alfa(pie, 0f); Alfa(cita, 0f);
        rotulo.color = Transparente(RelevoUI.Tinta);
        pie.color = Transparente(RelevoUI.Tinta);
        cita.color = Transparente(RelevoUI.Ambar);
        cam.backgroundColor = RenderSettings.fogColor;
        RenderSettings.fog = true;
        Luna(1.05f, new Color(0.6f, 0.68f, 0.95f), new Vector3(42f, -35f, 0f));
    }

    protected void Rotulo(string texto, float alfa) { rotulo.text = texto; Alfa(rotulo, alfa); }
    protected void Pie(string texto, float alfa) { pie.text = texto; Alfa(pie, alfa); }
    protected void Cita(string texto, float alfa, Color c) { cita.text = texto; c.a = Mathf.Clamp01(alfa); cita.color = c; }

    protected void Luna(float intensidad, Color c, Vector3 giro)
    {
        luna.intensity = intensidad;
        luna.color = c;
        luna.transform.rotation = Quaternion.Euler(giro);
    }

    protected void Alfa(Graphic g, float a) { Color c = g.color; c.a = Mathf.Clamp01(a); g.color = c; }
    protected void Fundido(float a) { fundido.color = new Color(0f, 0f, 0f, Mathf.Clamp01(a)); }

    protected void Colocar(Vector3 pos, Vector3 mira, float fov, float mano = 1f)
    {
        cam.fieldOfView = fov;
        float t = T * 0.8f;
        var giro = Quaternion.Euler((Mathf.PerlinNoise(t, 3.1f) - 0.5f) * 1.4f * mano, (Mathf.PerlinNoise(7.7f, t) - 0.5f) * 1.4f * mano, 0f);
        basePos = pos;
        cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(mira - pos) * giro);
    }

    protected void Sacudir(float fuerza) { sacudida = Mathf.Max(sacudida, fuerza); }

    void AplicarSacudida(float dt)
    {
        if (sacudida <= 0f) return;
        sacudida = Mathf.Max(0f, sacudida - dt * 0.6f);
        cam.transform.position = basePos + new Vector3(Mathf.PerlinNoise(T * 25f, 1f) - 0.5f, Mathf.PerlinNoise(2f, T * 25f) - 0.5f, 0f) * sacudida * 0.25f;
    }

    protected void Sonar(AudioClip c, float v) { if (c != null) efectos.PlayOneShot(c, v); }

    // ------------------------------------------------------------------ piezas de decorado

    protected static Material M(ClaveMaterial c) { return RelevoPalette.MaterialDe(c); }

    /// <summary>Material propio que brilla (el cómic conserva la emisión).</summary>
    protected static Material Brillo(Color baseColor, Color emision)
    {
        var m = new Material(M(ClaveMaterial.Ambar));
        m.color = baseColor;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emision);
        return m;
    }

    protected static GameObject Caja(string n, Transform p, Vector3 c, Vector3 s, Material m) { return DioramaBuilder.CrearCaja(n, p, c, s, m, false); }
    protected static GameObject Cilindro(string n, Transform p, Vector3 c, float r, float h, Material m) { return DioramaBuilder.CrearCilindro(n, p, c, r, h, m); }
    protected static GameObject Esfera(string n, Transform p, Vector3 c, float d, Material m) { return DioramaBuilder.CrearEsfera(n, p, c, d, m); }

    protected Transform Raiz(string nombre, Vector3 pos)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(transform, false);
        t.position = pos;
        return t;
    }

    /// <summary>Nilo sin control del jugador: se coloca a mano y anda cuando se le mueve.</summary>
    protected PlayerController CrearNilo(Transform padre, Vector3 pos, float giro)
    {
        PlayerController n = ActorFactory.CrearNilo(padre, Vector3.zero, giro);
        n.transform.position = pos;
        n.enabled = false;
        var interactor = n.GetComponent<PlayerInteractor>();
        if (interactor != null) interactor.enabled = false;
        var cc = n.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        return n;
    }

    protected static void Poner(PlayerController n, Vector3 pos, float giroY, float inclinacion = 0f)
    {
        n.transform.position = pos;
        n.transform.rotation = Quaternion.Euler(inclinacion, giroY, 0f);
    }

    /// <summary>La batería de Nilo: un cilindro ámbar que brilla.</summary>
    protected static Transform Bateria(Transform padre, Vector3 local, out Material brillo)
    {
        var b = new GameObject("Bateria").transform;
        b.SetParent(padre, false);
        b.localPosition = local;
        Cilindro("Carcasa", b, Vector3.zero, 0.2f, 0.42f, M(ClaveMaterial.Cobre));
        brillo = Brillo(RelevoUI.Ambar, RelevoUI.Ambar * 1.6f);
        Cilindro("Nucleo", b, Vector3.zero, 0.15f, 0.46f, brillo);
        return b;
    }

    /// <summary>La radio de Maia. Devuelve el material del piloto para encenderlo o apagarlo.</summary>
    protected static Transform Radio(Transform padre, Vector3 pos, float giro, out Material piloto, out Transform antena)
    {
        var r = new GameObject("Radio de Maia").transform;
        r.SetParent(padre, false);
        r.localPosition = pos;
        r.localRotation = Quaternion.Euler(0f, giro, 0f);
        Caja("Caja", r, new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.6f, 0.45f), M(ClaveMaterial.MaiaCarcasa));
        Caja("Altavoz", r, new Vector3(-0.15f, 0.4f, -0.23f), new Vector3(0.4f, 0.4f, 0.02f), M(ClaveMaterial.MaiaPantalla));
        piloto = Brillo(RelevoUI.Verde, RelevoUI.Verde * 1.5f);
        Esfera("Piloto", r, new Vector3(0.28f, 0.55f, -0.23f), 0.08f, piloto);
        antena = new GameObject("Antena").transform;
        antena.SetParent(r, false);
        antena.localPosition = new Vector3(0.3f, 0.7f, 0f);
        antena.localRotation = Quaternion.Euler(0f, 0f, -18f);
        Caja("Varilla", antena, new Vector3(0f, 0.3f, 0f), new Vector3(0.03f, 0.6f, 0.03f), M(ClaveMaterial.Cobre));
        return r;
    }

    /// <summary>
    /// Una Liria para mirar desde lejos: barrios de casas con ventanas, entre canales. Cada barrio
    /// tiene su propio material de ventana para encenderlo por separado (EncenderBarrio).
    /// </summary>
    /// <summary>La Liria real (bloques de Blender), si el kit los tiene. Null con la ciudad de cajas.</summary>
    protected LiriaNocturna liriaReal;
    static readonly Dictionary<Material, List<Material>> grupos = new Dictionary<Material, List<Material>>();

    protected List<Material> Ciudad(Transform padre, int barrios, int semilla, out List<Vector3> centros)
    {
        KitRelevo kit = KitRelevo.Activo;
        if (kit != null && kit.bloquesLiria != null && kit.bloquesLiria.Length > 0)
            return CiudadReal(padre, barrios, kit, out centros);
        var ventanas = new List<Material>();
        centros = new List<Vector3>();
        var rnd = new System.Random(semilla);
        Caja("Agua", padre, new Vector3(0f, -0.8f, 0f), new Vector3(400f, 0.2f, 400f), M(ClaveMaterial.Agua));
        int columnas = 4;
        for (int b = 0; b < barrios; b++)
        {
            int cx = b % columnas, cz = b / columnas;
            Vector3 c = new Vector3((cx - (columnas - 1) * 0.5f) * 22f, 0f, cz * 22f);
            centros.Add(c);
            var m = Brillo(new Color(0.08f, 0.09f, 0.12f), Color.black);
            ventanas.Add(m);
            Caja("Isla", padre, c + new Vector3(0f, -0.4f, 0f), new Vector3(17f, 0.8f, 17f), M(ClaveMaterial.PiedraBorde));
            for (int i = 0; i < 7; i++)
            {
                float alto = 3f + (float)rnd.NextDouble() * 6f;
                Vector3 p = c + new Vector3(-6f + (i % 4) * 4f + (float)rnd.NextDouble(), alto * 0.5f, -5f + (i / 4) * 8f + (float)rnd.NextDouble());
                Caja("Casa", padre, p, new Vector3(3.2f, alto, 3.2f), M(i % 3 == 0 ? ClaveMaterial.PiedraCrema : ClaveMaterial.Maqueta));
                Caja("Tejado", padre, p + new Vector3(0f, alto * 0.5f + 0.2f, 0f), new Vector3(3.5f, 0.4f, 3.5f), M(ClaveMaterial.Coral));
                for (float y = 1.2f; y < alto - 0.6f; y += 1.6f)
                {
                    Caja("Ventana", padre, new Vector3(p.x - 0.7f, y, p.z - 1.62f), new Vector3(0.6f, 0.8f, 0.05f), m);
                    Caja("Ventana", padre, new Vector3(p.x + 0.7f, y, p.z - 1.62f), new Vector3(0.6f, 0.8f, 0.05f), m);
                }
            }
            // Puente hacia el barrio siguiente.
            if (cx < columnas - 1) Caja("Puente", padre, c + new Vector3(11f, 0f, 0f), new Vector3(5f, 0.3f, 2f), M(ClaveMaterial.MaderaOscura));
            if (b + columnas < barrios) Caja("Puente", padre, c + new Vector3(0f, 0f, 11f), new Vector3(2f, 0.3f, 5f), M(ClaveMaterial.MaderaOscura));
        }
        return ventanas;
    }

    /// <summary>
    /// Liria de Liria_Prueba: se levanta en el origen (sus piezas usan coordenadas de mundo) y luego
    /// cuelga de 'padre'. Sus luces (ventanas de cada bloque y faroles) se reparten en 'barrios'
    /// tandas, de lejos a cerca, como en el apagón de la apertura.
    /// </summary>
    List<Material> CiudadReal(Transform padre, int barrios, KitRelevo kit, out List<Vector3> centros)
    {
        grupos.Clear();
        liriaReal = LiriaNocturna.ConstruirNueva(transform, n => null, kit, kit.bloquesLiria);
        liriaReal.Raiz.SetParent(padre, false);
        var ventanas = new List<Material>();
        centros = new List<Vector3>();
        var sumas = new List<Vector3>();
        var cuentas = new List<int>();
        for (int b = 0; b < barrios; b++) { sumas.Add(Vector3.zero); cuentas.Add(0); }
        int n = liriaReal.Luces.Count;
        for (int i = 0; i < n; i++)
        {
            LiriaNocturna.Luz l = liriaReal.Luces[i];
            if (l.luz != null) l.luz.enabled = false;   // el estilo cómic no usa luces puntuales
            int b = Mathf.Min(barrios - 1, i * barrios / Mathf.Max(1, n));
            sumas[b] += padre.TransformPoint(l.posicion);
            cuentas[b]++;
            foreach (Material m in l.materiales)
            {
                if (b >= ventanas.Count) { ventanas.Add(m); grupos[m] = new List<Material>(); }
                else grupos[ventanas[b]].Add(m);
            }
        }
        while (ventanas.Count < barrios) { var m = Brillo(Color.black, Color.black); ventanas.Add(m); }
        for (int b = 0; b < barrios; b++) centros.Add(cuentas[b] > 0 ? sumas[b] / cuentas[b] - padre.position : Vector3.zero);
        return ventanas;
    }

    /// <summary>Punto del suelo de Liria bajo 'p' (mundo), o 'p' si no hay nada debajo.</summary>
    protected static Vector3 Suelo(Vector3 p)
    {
        RaycastHit h;
        return Physics.Raycast(p + Vector3.up * 40f, Vector3.down, out h, 80f) ? h.point : p;
    }

    protected static void EncenderBarrio(Material ventana, float k)
    {
        Color c = RelevoUI.Ambar * 1.8f * Mathf.Clamp01(k);
        ventana.SetColor("_EmissionColor", c);
        List<Material> otros;
        if (grupos.TryGetValue(ventana, out otros)) foreach (Material m in otros) m.SetColor("_EmissionColor", c);
    }

    /// <summary>El muelle del Puerto 1: tablado, baranda y la radio. 'rayas' marca la baranda.</summary>
    protected Transform Muelle(Vector3 pos, int rayas, out Material piloto, out Transform antena)
    {
        Transform r = Raiz("Muelle", pos);
        Caja("Agua", r, new Vector3(0f, -0.7f, 0f), new Vector3(120f, 0.2f, 120f), M(ClaveMaterial.Agua));
        for (int i = 0; i < 10; i++)
            Caja("Tablon", r, new Vector3(0f, -0.1f, -4.5f + i * 1.02f), new Vector3(8f, 0.2f, 0.96f), M(ClaveMaterial.MaderaOscura));
        for (int i = 0; i < 5; i++)
            Caja("Poste", r, new Vector3(-3.6f + i * 1.8f, 0.5f, 5.3f), new Vector3(0.14f, 1.1f, 0.14f), M(ClaveMaterial.MaderaOscura));
        Caja("Baranda", r, new Vector3(0f, 1f, 5.3f), new Vector3(7.6f, 0.12f, 0.16f), M(ClaveMaterial.MaderaOscura));
        var marca = M(ClaveMaterial.PiedraCrema);
        for (int i = 0; i < rayas; i++)
            Caja("Raya", r, new Vector3(-1.4f + i * 0.2f + (i % 5 == 4 ? 0.06f : 0f), 1f, 5.21f), new Vector3(0.035f, 0.1f, 0.01f), marca);
        Radio(r, new Vector3(2.4f, 0f, 3.6f), 20f, out piloto, out antena);
        Caja("Farol", r, new Vector3(-3.2f, 1.5f, 4.6f), new Vector3(0.12f, 3f, 0.12f), M(ClaveMaterial.MaderaOscura));
        return r;
    }
}
