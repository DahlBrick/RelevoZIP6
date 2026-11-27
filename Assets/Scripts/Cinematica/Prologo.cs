using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Prólogo de RELEVO con Timeline + Cinemachine (docs/narrativa/prologo_planos.md).
///
/// La Timeline (Assets/Cinematicas/Prologo.playable) lleva el reloj y los cortes entre cámaras;
/// cada cámara recorre su raíl con PlanoPrologo. Este componente lee el tiempo de la Timeline y
/// mueve todo lo demás: el apagón en cadena, la rueda que se para, la ropa al viento, el humo, el
/// agua que sube, el título 3D que sale del mar, la frase de Maia y el sonido de la radio.
///
/// Los momentos clave son campos del inspector ("Tiempos"): si mueves los planos en la Timeline,
/// ajusta aquí los tiempos del apagón para que sigan cayendo en su plano.
/// </summary>
public sealed class Prologo : MonoBehaviour
{
    [Header("Escena")]
    public PlayableDirector director;
    public Camera camara;
    public Transform bloquesLiria;
    public Light luna;
    public Material cielo;
    public InputActionAsset acciones;
    public string escenaSiguiente = "Nivel1_MuelleDeCobre";

    [Header("Vida de la ciudad")]
    public Transform[] prendas;
    public ParticleSystem[] humos;

    [Header("Título 3D")]
    public Transform[] letras;
    public Renderer[] frentesTitulo;
    public float alturaTitulo = 26f;

    [Header("Sonido")]
    [Tooltip("La canción que suena por la radio. Vacío: la provisional generada por código.")]
    public AudioClip cancion;

    [Header("Tiempos (segundos de la Timeline)")]
    public float maiaInicio = 1.2f;
    public float maiaFin = 5.4f;
    public float apagonLejos = 17.2f;
    public float apagonCalle = 20.2f;
    public float apagonCalleFin = 21.8f;
    public float ruedaSePara = 22.0f;
    public float apagonCerca = 25.0f;
    public float ultimaLuz = 26.6f;
    public float aguaInicio = 26.4f;
    public float aguaFin = 30f;
    public float tituloSube = 30.6f;
    public float fin = 38f;

    const string FraseMaia = "Buenas noches a quienes siguen de turno.";

    LiriaNocturna liria;
    RelevoInput entrada;
    DepthOfField dof;
    ColorAdjustments color;
    Bloom bloom;
    Image fundido, avisoBarra;
    Text subtitulo, aviso;
    AudioSource musica, estatica, ambiente, agua, efectos;
    AudioLowPassFilter pasaBajos;
    AudioHighPassFilter pasaAltos;
    AudioDistortionFilter distorsion;
    AudioClip clipRele, clipGolpe, clipEstatica;
    readonly List<float> cortes = new List<float>();
    readonly List<Quaternion> giroPrendas = new List<Quaternion>();
    float T, previo = -1f, mantenerSalto, avisoVisible;
    bool terminado;

    // ------------------------------------------------------------------ arranque

    void Start()
    {
        Application.targetFrameRate = 60;
        GameEvents.Limpiar();
        RelevoGame.Limpiar();
        acciones = KitRelevo.Acciones(acciones);
        entrada = acciones != null ? new RelevoInput(acciones) : null;

        Ambiente();
        KitRelevo kit = KitRelevo.Activo;
        liria = LiriaNocturna.ConstruirSobre(transform, bloquesLiria, kit);
        Color sombraNoche = new Color(0.36f, 0.4f, 0.62f);
        EstiloComic.Aplicar(bloquesLiria, sombraNoche);
        EstiloComic.Aplicar(liria.Raiz, sombraNoche);
        foreach (Transform p in prendas) if (p != null) EstiloComic.Aplicar(p, sombraNoche);
        EstiloComic.RendererTinta(camara);
        foreach (Transform p in prendas) giroPrendas.Add(p != null ? p.localRotation : Quaternion.identity);

        PrepararPosproceso();
        PrepararInterfaz();
        PrepararSonido();
        RepartirApagon();

        director.timeUpdateMode = DirectorUpdateMode.GameTime;
        director.extrapolationMode = DirectorWrapMode.Hold;
        director.time = 0;
        director.Play();
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
        RenderSettings.fogStartDistance = 60f;
        RenderSettings.fogEndDistance = 420f;
        if (cielo != null) RenderSettings.skybox = cielo;
        DynamicGI.UpdateEnvironment();
        camara.clearFlags = cielo != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        camara.backgroundColor = RenderSettings.fogColor;
    }

    void PrepararPosproceso()
    {
        var datos = camara.GetUniversalAdditionalCameraData();
        datos.renderPostProcessing = true;
        datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        datos.antialiasingQuality = AntialiasingQuality.High;

        Volume v = new GameObject("Volumen de cine").AddComponent<Volume>();
        v.transform.SetParent(transform, false);
        v.isGlobal = true;
        v.priority = 100f;
        VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        perfil.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
        bloom = perfil.Add<Bloom>(true);
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(1.4f);
        bloom.scatter.Override(0.72f);
        dof = perfil.Add<DepthOfField>(true);
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focalLength.Override(55f);
        dof.aperture.Override(4f);
        dof.focusDistance.Override(20f);
        Vignette vineta = perfil.Add<Vignette>(true);
        vineta.intensity.Override(0.32f);
        vineta.smoothness.Override(0.5f);
        color = perfil.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.35f);
        color.saturation.Override(6f);
        color.contrast.Override(10f);
        perfil.Add<ChromaticAberration>(true).intensity.Override(0.06f);
        FilmGrain grano = perfil.Add<FilmGrain>(true);
        grano.type.Override(FilmGrainLookup.Thin1);
        grano.intensity.Override(0.22f);
        v.sharedProfile = perfil;
    }

    void PrepararInterfaz()
    {
        // Cámara de interfaz apilada sobre la de cine, sin posproceso (el desenfoque no la toca).
        Camera camUI = new GameObject("Camara interfaz").AddComponent<Camera>();
        camUI.transform.SetParent(camara.transform, false);
        camUI.cullingMask = 1 << 5;
        camUI.clearFlags = CameraClearFlags.Depth;
        var datosUI = camUI.GetUniversalAdditionalCameraData();
        datosUI.renderType = CameraRenderType.Overlay;
        datosUI.renderPostProcessing = false;
        camara.GetUniversalAdditionalCameraData().cameraStack.Add(camUI);
        camara.cullingMask &= ~(1 << 5);

        GameObject go = new GameObject("Interfaz del prologo") { layer = 5 };
        go.transform.SetParent(transform, false);
        Canvas lienzo = go.AddComponent<Canvas>();
        lienzo.renderMode = RenderMode.ScreenSpaceCamera;
        lienzo.worldCamera = camUI;
        lienzo.planeDistance = 1f;
        CanvasScaler esc = go.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 1f;
        Transform r = go.transform;

        float banda = (1080f - 1920f / 2.39f) * 0.5f;
        Bloque(r, "Banda arriba", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), banda);
        Bloque(r, "Banda abajo", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), banda);

        subtitulo = RelevoUI.Texto(r, "Subtitulo", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, banda + 40f),
            new Vector2(1500f, 60f), 36, TextAnchor.LowerCenter, new Color(0.95f, 0.9f, 0.82f, 0f));
        subtitulo.supportRichText = true;
        Outline borde = subtitulo.gameObject.AddComponent<Outline>();
        borde.effectColor = new Color(0f, 0f, 0f, 0.8f);
        borde.effectDistance = new Vector2(2f, -2f);

        fundido = Bloque(r, "Fundido", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), 0f);
        fundido.rectTransform.offsetMin = fundido.rectTransform.offsetMax = Vector2.zero;

        aviso = RelevoUI.Texto(r, "Aviso saltar", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 40f),
            new Vector2(520f, 36f), 22, TextAnchor.LowerRight, new Color(0.95f, 0.9f, 0.82f, 0f));
        aviso.text = "Mantén Esc para saltar";
        avisoBarra = Bloque(r, "Aviso barra", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), 3f);
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
        musica = Fuente("Radio", true);
        musica.clip = cancion != null ? cancion : SonidosApertura.Cancion();
        pasaAltos = musica.gameObject.AddComponent<AudioHighPassFilter>();
        pasaBajos = musica.gameObject.AddComponent<AudioLowPassFilter>();
        distorsion = musica.gameObject.AddComponent<AudioDistortionFilter>();
        clipEstatica = SonidosApertura.Estatica();
        clipRele = SonidosApertura.Rele();
        clipGolpe = SonidosApertura.Golpe();
        estatica = Fuente("Estatica", true);
        estatica.clip = clipEstatica;
        ambiente = Fuente("Zumbido de la ciudad", true);
        ambiente.clip = SonidosApertura.Zumbido();
        agua = Fuente("Agua", true);
        agua.clip = SonidosApertura.Agua();
        efectos = Fuente("Efectos", false);
        efectos.volume = 1f;
        musica.Play(); estatica.Play(); ambiente.Play(); agua.Play();
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

    /// <summary>
    /// Cuándo se apaga cada luz. Lejos (el fondo de la ciudad) primero; la calle del barrio de piedra
    /// en fila durante su plano; lo cercano (muelle y canal) al final; el último farol, el último.
    /// </summary>
    void RepartirApagon()
    {
        float pulso = SonidosApertura.DuracionPulso;
        float minX = float.MaxValue, maxX = float.MinValue;
        foreach (LiriaNocturna.Luz l in liria.Luces)
            if (EnLaCalle(l)) { minX = Mathf.Min(minX, l.posicion.x); maxX = Mathf.Max(maxX, l.posicion.x); }

        foreach (LiriaNocturna.Luz l in liria.Luces)
        {
            float t;
            if (l == liria.UltimoFarol) t = ultimaLuz;
            else if (EnLaCalle(l))
                t = Mathf.Lerp(apagonCalle, apagonCalleFin, Mathf.InverseLerp(minX, maxX, l.posicion.x));
            else if (l.orden < 0.5f)
                t = Cuantizar(Mathf.Lerp(apagonLejos, apagonCalle - 0.4f, l.orden / 0.5f), apagonLejos, pulso);
            else
                t = Cuantizar(Mathf.Lerp(apagonCerca, ultimaLuz - 0.5f, (l.orden - 0.5f) / 0.5f), apagonCerca, pulso * 0.5f);
            l.cortadaEn = t + (l.semilla % 1f) * 0.08f;
            cortes.Add(t);
        }
        cortes.Sort();
    }

    static float Cuantizar(float t, float origen, float paso) { return origen + Mathf.Floor((t - origen) / paso) * paso; }

    static bool EnLaCalle(LiriaNocturna.Luz l)
    {
        return Mathf.Abs(l.posicion.z - 35f) < 6f && l.posicion.x > -22f && l.posicion.x < 22f;
    }

    // ------------------------------------------------------------------ bucle

    void Update()
    {
        if (terminado || liria == null) return;
        Salto();
        if (terminado) return;
        T = (float)director.time;
        float dt = previo < 0f ? 0f : Mathf.Max(0f, T - previo);
        Evaluar(previo, T, dt);
        previo = T;
        if (T >= fin) Terminar();
    }

    static float S(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
    static float Tramo(float t, float a, float b) { return Mathf.Clamp01((t - a) / (b - a)); }
    static bool Cruza(float previo, float ahora, float marca) { return previo < marca && ahora >= marca; }

    void Evaluar(float previo, float t, float dt)
    {
        liria.Animar(t, dt);
        float apagon = S(Tramo(t, apagonLejos, ultimaLuz));

        // La rueda de la bomba gira hasta que se va la luz de su barrio; da un último tirón y se para.
        float frena = Tramo(t, ruedaSePara, ruedaSePara + 0.9f);
        liria.VelocidadRueda = (1f - S(frena)) * (frena > 0f && frena < 0.35f ? 1.6f : 1f);
        if (liria.Rueda != null)
        {
            if (t < ruedaSePara && Mathf.Repeat(t, 0.45f) < Mathf.Repeat(previo, 0.45f))
                liria.Chisporrotear(liria.Rueda.position + Vector3.up * 1.4f, 5);
            if (Cruza(previo, t, ruedaSePara + 0.15f)) liria.Chisporrotear(liria.Rueda.position + Vector3.up * 1.4f, 40);
        }

        // Ropa tendida: el viento no se apaga con la luz.
        for (int i = 0; i < prendas.Length; i++)
        {
            if (prendas[i] == null) continue;
            float viento = Mathf.Sin(t * 1.6f + i * 0.9f) * 10f + Mathf.Sin(t * 3.7f + i * 2.3f) * 4f;
            prendas[i].localRotation = giroPrendas[i] * Quaternion.Euler(viento, 0f, Mathf.Sin(t * 2.1f + i) * 3f);
        }

        // Humo de chimeneas: se apaga despacio cuando la casa se queda sin luz.
        foreach (ParticleSystem h in humos)
        {
            if (h == null) continue;
            var em = h.emission;
            em.rateOverTime = Mathf.Lerp(5f, 0.6f, Tramo(t, apagonCalle, apagonCalle + 4f));
        }

        // Sin luces, la ciudad queda a la luz de la luna, más fría.
        luna.intensity = Mathf.Lerp(0.5f, 1.05f, apagon);
        luna.color = Color.Lerp(new Color(0.55f, 0.65f, 0.95f), new Color(0.45f, 0.58f, 1f), apagon);
        color.postExposure.Override(Mathf.Lerp(0.35f, -0.15f, apagon));
        liria.SubirAgua(S(Tramo(t, aguaInicio, aguaFin)));
        if (Cruza(previo, t, ultimaLuz - 0.2f)) liria.Chisporrotear(liria.UltimoFarol.posicion, 60);

        // Enfoque: a lo que mira la cámara activa.
        CinemachineBrain cerebro = camara.GetComponent<CinemachineBrain>();
        ICinemachineCamera activa = cerebro != null ? cerebro.ActiveVirtualCamera : null;
        CinemachineCamera cc = activa as CinemachineCamera;
        if (cc != null && cc.LookAt != null)
            dof.focusDistance.Override(Vector3.Distance(camara.transform.position, cc.LookAt.position));

        Titulo(previo, t);
        Interfaz(t);
        Sonido(previo, t, apagon);
    }

    void Titulo(float previo, float t)
    {
        // La niebla se abre para el plano final: el título está lejos, detrás de la ciudad.
        RenderSettings.fogEndDistance = Mathf.Lerp(420f, 950f, S(Tramo(t, tituloSube - 3f, tituloSube)));
        for (int i = 0; i < letras.Length; i++)
        {
            if (letras[i] == null) continue;
            float u = Tramo(t, tituloSube + i * 0.22f, tituloSube + i * 0.22f + 3.4f);
            // Sube con un leve rebote, como algo pesado que emerge del agua y se asienta.
            float e = 1f - Mathf.Pow(1f - u, 3f);
            float rebote = Mathf.Sin(u * Mathf.PI) * 0.06f * (1f - u);
            float y = Mathf.Lerp(-alturaTitulo * 1.15f, 0f, e) + rebote * alturaTitulo;
            y += Mathf.Sin(t * 0.8f + i) * 0.15f * u;
            Vector3 p = letras[i].localPosition;
            letras[i].localPosition = new Vector3(p.x, y, p.z);
        }
        // La luna lo va iluminando: de silueta oscura a ámbar.
        float luz = S(Tramo(t, tituloSube + 0.8f, tituloSube + 4.5f));
        Color c = Color.Lerp(new Color(0.25f, 0.2f, 0.18f), Color.white, luz);
        foreach (Renderer r in frentesTitulo) if (r != null) r.material.SetColor("_BaseColor", c);
        if (Cruza(previo, t, tituloSube + 0.4f)) efectos.PlayOneShot(clipGolpe, 0.8f);
        if (Cruza(previo, t, tituloSube + 3.6f)) efectos.PlayOneShot(clipGolpe, 1f);
    }

    void Interfaz(float t)
    {
        fundido.color = new Color(0f, 0f, 0f, Mathf.Max(1f - S(Tramo(t, 0f, 1.6f)), S(Tramo(t, fin - 1.6f, fin))));

        // Maia por la radio: la frase se escribe y se queda un momento.
        float a = Tramo(t, maiaInicio, maiaInicio + 0.3f) * (1f - Tramo(t, maiaFin - 0.5f, maiaFin));
        int letrasVisibles = Mathf.RoundToInt(FraseMaia.Length * Tramo(t, maiaInicio + 0.2f, maiaInicio + 2.6f));
        subtitulo.text = "<color=#8CE8A0>MAIA</color>   " + FraseMaia.Substring(0, letrasVisibles);
        subtitulo.color = new Color(subtitulo.color.r, subtitulo.color.g, subtitulo.color.b, a);
    }

    void Sonido(float previo, float t, float apagon)
    {
        // La canción suena por la radio: sin graves ni agudos, con algo de ruido.
        float vol = 0.75f * S(Tramo(t, 0.2f, 2f));
        pasaAltos.cutoffFrequency = 650f;
        pasaBajos.cutoffFrequency = 3600f;
        float deformar = S(Tramo(t, apagonLejos + 1f, ultimaLuz - 0.4f));
        musica.pitch = Mathf.Lerp(1f, 0.62f, deformar * deformar);
        distorsion.distortionLevel = Mathf.Lerp(0.12f, 0.6f, deformar);
        pasaBajos.cutoffFrequency = Mathf.Lerp(3600f, 1500f, deformar);
        bool cortada = t >= ultimaLuz - 0.6f;
        musica.volume = cortada ? 0f : vol * AudioDirector.MezclaMusica;
        if (Cruza(previo, t, ultimaLuz - 0.6f)) musica.Stop();

        // Estática: un soplo cuando habla Maia y el chasquido de la radio que se corta.
        float soplo = Mathf.Sin(Mathf.PI * Tramo(t, maiaInicio - 0.2f, maiaInicio + 0.8f)) * 0.25f;
        float corte = Mathf.Sin(Mathf.PI * Tramo(t, ultimaLuz - 1.4f, ultimaLuz + 0.3f)) * 0.55f;
        estatica.volume = Mathf.Max(soplo, corte, 0.04f * (1f - apagon));

        ambiente.volume = 0.12f * (1f - apagon) * S(Tramo(t, 0.5f, 2.5f));
        agua.volume = Mathf.Lerp(0.08f, 0.5f, S(Tramo(t, aguaInicio - 1f, aguaFin))) * (1f - S(Tramo(t, fin - 1.6f, fin)));

        // Relés: un chasquido por cada tanda de luces que cae.
        foreach (float c in cortes)
            if (Cruza(previo, t, c)) { efectos.PlayOneShot(clipRele, Mathf.Lerp(0.25f, 0.9f, apagon)); break; }
    }

    // ------------------------------------------------------------------ salir

    void Salto()
    {
        Keyboard k = Keyboard.current;
        bool algo = k != null && k.anyKey.isPressed;
        bool esc = (k != null && k.escapeKey.isPressed) || (entrada != null && (entrada.Pausa.IsPressed() || entrada.Cancelar.IsPressed()));
        if (algo) avisoVisible = 3f;
        avisoVisible -= Time.unscaledDeltaTime;
        mantenerSalto = esc ? mantenerSalto + Time.unscaledDeltaTime : Mathf.Max(0f, mantenerSalto - Time.unscaledDeltaTime * 2f);
        aviso.color = new Color(aviso.color.r, aviso.color.g, aviso.color.b, Mathf.Clamp01(avisoVisible) * 0.85f);
        avisoBarra.rectTransform.sizeDelta = new Vector2(240f * Mathf.Clamp01(mantenerSalto / 1.1f), 3f);
        if (mantenerSalto > 1.1f) Terminar();
    }

    void Terminar()
    {
        if (terminado) return;
        terminado = true;
#if UNITY_EDITOR
        // Al grabar o revisar en el editor no se cambia de escena.
        if (UnityEditor.SessionState.GetBool("PrologoSinSalir", false)) return;
#endif
        if (!string.IsNullOrEmpty(escenaSiguiente) && Application.CanStreamedLevelBeLoaded(escenaSiguiente))
            SceneManager.LoadScene(escenaSiguiente);
    }

    void OnDestroy() { if (entrada != null) entrada.Desactivar(); }
}
