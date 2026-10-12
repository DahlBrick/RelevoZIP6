#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Graba los clips del teaser a MP4 (H.264, 1920x1080, 30 fps) en Play, fotograma a fotograma:
/// Time.captureFramerate fija el paso del tiempo, así que el resultado no depende de lo rápido que
/// renderice el editor.
///
///  - "cinematica": 6 s en cuatro planos con corte seco: primer plano de Nilo, detalle de la
///    batería, Maia y una grúa que sube desde el agua y descubre el nivel. Sin HUD, con
///    profundidad de campo y bandas de cine.
///  - "nivel": el Nivel 1 completo, de la introducción al menú final, con el recuerdo opcional.
///  - "jugabilidad": 5 s del Nivel 1 con la cámara y el HUD del juego. Un piloto automático pulsa
///    teclas simuladas: Nilo recoge la batería, la coloca en la base y cruza el puente.
///
/// Herramienta de editor: no entra en la build.
/// </summary>
public sealed class GrabadorTeaser : MonoBehaviour
{
    const int Ancho = 1920, Alto = 1080, Fps = 30;

    string ruta;
    string modo;
    public static string Estado = "";

    float velocidad = 1f;

    /// <summary>'velocidad' acelera el clip: con 1,2, cada segundo de video muestra 1,2 s de juego.</summary>
    public static string Iniciar(string modo, string ruta, float velocidad = 1f)
    {
        GameObject go = new GameObject("Grabador teaser");
        GrabadorTeaser g = go.AddComponent<GrabadorTeaser>();
        g.modo = modo;
        g.ruta = ruta;
        g.velocidad = velocidad;
        Estado = "iniciando " + modo;
        return ruta;
    }

    void Start()
    {
        StartCoroutine(modo == "cinematica" ? Cinematica() : Jugabilidad());
    }

    // ------------------------------------------------------------------ codificación

    MediaEncoder codificador;
    RenderTexture lienzo;
    Texture2D fotograma;
    bool bandas;
    int cuenta;

    void Abrir(bool conBandas)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        if (File.Exists(ruta)) File.Delete(ruta);
        var atributos = new VideoTrackAttributes
        {
            frameRate = new MediaRational(Fps),
            width = Ancho,
            height = Alto,
            includeAlpha = false,
            bitRateMode = UnityEditor.VideoBitrateMode.High
        };
        codificador = new MediaEncoder(ruta, atributos);
        lienzo = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        lienzo.antiAliasing = 8;
        fotograma = new Texture2D(Ancho, Alto, TextureFormat.RGBA32, false);
        bandas = conBandas;
        cuenta = 0;
        Time.captureFramerate = Fps;
    }

    float acumulado;

    IEnumerator Grabar()
    {
        yield return new WaitForEndOfFrame();

        // Acelerar sin tocar la simulación: se descartan fotogramas en vez de agrandar el paso de
        // tiempo (con pasos grandes Nilo se pasa de las esquinas).
        acumulado += 1f / velocidad;
        if (acumulado < 1f) yield break;
        acumulado -= 1f;

        RenderTexture previa = RenderTexture.active;
        RenderTexture.active = lienzo;
        fotograma.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0, false);
        RenderTexture.active = previa;

        if (bandas)
        {
            // Bandas 2,39:1.
            int banda = (Alto - Mathf.RoundToInt(Ancho / 2.39f)) / 2;
            Color32[] negro = new Color32[Ancho * banda];
            for (int i = 0; i < negro.Length; i++) negro[i] = new Color32(0, 0, 0, 255);
            fotograma.SetPixels32(0, 0, Ancho, banda, negro);
            fotograma.SetPixels32(0, Alto - banda, Ancho, banda, negro);
        }
        fotograma.Apply(false);
        codificador.AddFrame(fotograma);

        // Algunas fotos sueltas para la bitácora.
        if (cuenta % 15 == 0)
            File.WriteAllBytes(Path.ChangeExtension(ruta, null) + "_" + cuenta.ToString("000") + ".jpg", fotograma.EncodeToJPG(85));
        cuenta++;
    }

    void Cerrar()
    {
        if (codificador != null) codificador.Dispose();
        codificador = null;
        Time.captureFramerate = 0;
        if (lienzo != null) { lienzo.Release(); Destroy(lienzo); }
        Estado = "terminado " + modo + " (" + cuenta + " fotogramas) " + ruta;
        Destroy(gameObject);
    }

    static void CerrarDialogo()
    {
        DialogoUI d = FindFirstObjectByType<DialogoUI>();
        if (d != null && d.Abierto) d.Cerrar();
    }

    // ------------------------------------------------------------------ cinemática

    IEnumerator Cinematica()
    {
        CerrarDialogo();
        yield return null;

        PlayerController nilo = RelevoGame.Jugador;
        BatteryController bateria = FindFirstObjectByType<BatteryController>();
        MaiaTerminal maia = FindFirstObjectByType<MaiaTerminal>();
        Bounds nivel = LevelLayouts.Obtener(1).Limites();

        Canvas[] lienzos = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in lienzos) c.enabled = false;

        Camera principal = Camera.main;
        GameObject goCam = new GameObject("Camara cine");
        Camera cam = goCam.AddComponent<Camera>();
        cam.CopyFrom(principal);
        cam.orthographic = false;
        cam.nearClipPlane = 0.03f;
        cam.farClipPlane = 200f;
        cam.depth = principal.depth + 1;
        // La cámara del juego mira hacia abajo y nunca ve el cielo: aquí el fondo es el color de
        // la niebla, que funde el horizonte con el agua.
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = RenderSettings.fogColor;
        var datos = goCam.AddComponent<UniversalAdditionalCameraData>();
        datos.renderPostProcessing = true;
        datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        datos.antialiasingQuality = AntialiasingQuality.High;
        principal.enabled = false;

        // Profundidad de campo propia del plano detalle, sobre el perfil del nivel.
        GameObject goVol = new GameObject("Volumen cine");
        Volume volumen = goVol.AddComponent<Volume>();
        volumen.isGlobal = true;
        volumen.priority = 100f;
        VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        DepthOfField dof = perfil.Add<DepthOfField>(true);
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.aperture.Override(2.2f);
        dof.focalLength.Override(55f);
        Vignette vineta = perfil.Add<Vignette>(true);
        vineta.intensity.Override(0.28f);
        vineta.smoothness.Override(0.5f);
        volumen.sharedProfile = perfil;

        Abrir(true);
        cam.targetTexture = lienzo;

        int porPlano = Fps * 6 / 4;   // 45 fotogramas, 1,5 s
        for (int plano = 0; plano < 4; plano++)
        {
            Estado = "cinematica plano " + (plano + 1);
            for (int f = 0; f < porPlano; f++)
            {
                float t = f / (float)(porPlano - 1);
                float foco;
                Encuadrar(plano, t, cam, nilo, bateria, maia, nivel, out foco);
                dof.active = foco > 0f;
                if (foco > 0f) dof.focusDistance.Override(foco);
                yield return Grabar();
            }
        }

        cam.targetTexture = null;
        principal.enabled = true;
        foreach (Canvas c in lienzos) if (c != null) c.enabled = true;
        Destroy(goCam);
        Destroy(goVol);
        Cerrar();
    }

    static float EaseInOut(float t) { return t * t * (3f - 2f * t); }

    static Bounds LimitesDe(Transform raiz)
    {
        Bounds b = new Bounds(raiz.position, Vector3.zero);
        bool hay = false;
        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            if (hay) b.Encapsulate(r.bounds); else { b = r.bounds; hay = true; }
        }
        return b;
    }

    static Vector3 Plano(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward; }

    static void Encuadrar(int plano, float t, Camera cam, PlayerController nilo, BatteryController bateria,
        MaiaTerminal maia, Bounds nivel, out float foco)
    {
        float s = EaseInOut(t);
        foco = 0f;
        switch (plano)
        {
            case 0:
            {
                // Primer plano de Nilo: dolly lento hacia la cara, desde un poco más abajo (heroico).
                // Nilo se gira hacia el canal (este): detrás de la cámara no hay postes que tapen.
                Vector3 haciaCanal = new Vector3(1f, 0f, -0.25f).normalized;
                if (t == 0f) nilo.transform.rotation = Quaternion.LookRotation(haciaCanal);
                Bounds b = LimitesDe(nilo.transform);
                float h = b.max.y - nilo.transform.position.y;
                Vector3 cara = nilo.transform.position + Vector3.up * h * 0.78f;
                Vector3 dir = Quaternion.Euler(0f, Mathf.Lerp(-30f, -12f, s), 0f) * haciaCanal;
                float d = Mathf.Lerp(h * 1.6f, h * 1.15f, s);
                cam.fieldOfView = 30f;
                cam.transform.position = cara + dir * d + Vector3.up * (-h * 0.12f);
                cam.transform.LookAt(cara + Vector3.up * h * 0.03f);
                foco = d;
                break;
            }
            case 1:
            {
                // Detalle de la batería: cámara a ras de suelo que se desliza en arco.
                Bounds b = LimitesDe(bateria.transform);
                Vector3 centro = b.center;
                float r = Mathf.Max(b.size.y, 0.5f);
                float ang = Mathf.Lerp(200f, 235f, s) * Mathf.Deg2Rad;
                Vector3 off = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * r * Mathf.Lerp(2.1f, 1.7f, s);
                cam.fieldOfView = 34f;
                cam.transform.position = new Vector3(centro.x + off.x, b.min.y + r * 0.35f, centro.z + off.z);
                cam.transform.LookAt(centro + Vector3.up * r * 0.1f);
                foco = Vector3.Distance(cam.transform.position, centro);
                break;
            }
            case 2:
            {
                // Maia: arco frente a su pantalla, ligeramente contrapicado.
                Transform ojoI = null, ojoD = null;
                foreach (Transform tr in maia.GetComponentsInChildren<Transform>())
                {
                    if (tr.name == "Ojo izquierdo") ojoI = tr;
                    else if (tr.name == "Ojo derecho") ojoD = tr;
                }
                Vector3 ojos = ojoI != null && ojoD != null ? (ojoI.position + ojoD.position) * 0.5f
                    : maia.transform.position + Vector3.up * 1.3f;
                Vector3 frente = Plano(ojos - new Vector3(maia.transform.position.x, ojos.y, maia.transform.position.z));
                Vector3 dir = Quaternion.Euler(0f, Mathf.Lerp(28f, 6f, s), 0f) * frente;
                float d = Mathf.Lerp(2.0f, 1.55f, s);
                cam.fieldOfView = 32f;
                cam.transform.position = ojos + dir * d + Vector3.up * Mathf.Lerp(-0.35f, -0.2f, s);
                cam.transform.LookAt(ojos + Vector3.down * 0.15f);
                foco = d;
                break;
            }
            default:
            {
                // Grúa: sale casi a ras del agua, al sur del malecón, y sube hasta ver el nivel entero.
                Vector3 c = nivel.center;
                Vector3 desde = new Vector3(c.x - 3f, 0.7f, nivel.min.z - 6.5f);
                Vector3 hasta = new Vector3(c.x, 17f, nivel.min.z - 13f);
                cam.fieldOfView = Mathf.Lerp(44f, 40f, s);
                cam.transform.position = Vector3.Lerp(desde, hasta, s);
                Vector3 mira = Vector3.Lerp(new Vector3(c.x - 1f, 0.6f, nivel.min.z + 3f), c, s);
                cam.transform.LookAt(mira);
                break;
            }
        }
    }

    // ------------------------------------------------------------------ jugabilidad

    /// <summary>Un tramo del piloto: caminar hasta 'destino' y, si hay 'mirar', girarse hacia ahí y pulsar E.</summary>
    struct Paso
    {
        public Vector3 destino;
        public float parada;
        public Vector3? mirar;
    }

    static Paso Ir(Vector3 destino) { return new Paso { destino = destino, parada = 0.35f }; }
    static Paso Usar(Vector3 desde, Vector3 objeto) { return new Paso { destino = desde, parada = 0.3f, mirar = objeto }; }

    IEnumerator Jugabilidad()
    {
        LevelLayout m = LevelLayouts.Obtener(1);
        Vector3 bateria = FindFirstObjectByType<BatteryController>().transform.position;
        var pasos = new List<Paso>();
        bool completo = modo == "nivel";

        if (completo)
        {
            // Solución completa del Nivel 1, con el recuerdo opcional.
            pasos.Add(Usar(m.CentroTile(1, 4), bateria));                       // recoger la batería
            pasos.Add(Usar(m.CentroTile(2, 5), m.CentroTile(2, 4)));            // insertarla en A
            pasos.Add(Ir(m.CentroTile(3, 4)));
            pasos.Add(Ir(m.CentroTile(10, 4)));                                 // cruzar el puente
            pasos.Add(Ir(m.CentroTile(11, 4)));
            pasos.Add(Usar(m.CentroTile(11, 5), m.CentroTile(10, 5)));          // palanca
            pasos.Add(Ir(m.CentroTile(10, 5)));
            pasos.Add(Ir(m.CentroTile(10, 6)));
            pasos.Add(Ir(m.CentroTile(3, 6)));                                  // pasarela de vuelta
            pasos.Add(Ir(m.CentroTile(3, 8)));
            pasos.Add(Usar(m.CentroTile(1, 8), m.CentroTile(1, 9)));            // recuerdo
            pasos.Add(Ir(m.CentroTile(1, 5)));
            pasos.Add(Usar(m.CentroTile(2, 5), m.CentroTile(2, 4)));            // recuperar la batería
            pasos.Add(Ir(m.CentroTile(1, 5)));
            pasos.Add(Ir(m.CentroTile(1, 8)));
            pasos.Add(Ir(m.CentroTile(3, 8)));
            pasos.Add(Ir(m.CentroTile(3, 6)));
            pasos.Add(Ir(m.CentroTile(10, 6)));
            pasos.Add(Ir(m.CentroTile(10, 5)));
            pasos.Add(Ir(m.CentroTile(11, 4)));
            pasos.Add(Usar(m.CentroTile(11, 3), m.CentroTile(12, 3)));          // salida
        }
        else
        {
            pasos.Add(Usar(m.CentroTile(0, 4), bateria));
            pasos.Add(Usar(m.CentroTile(2, 5), m.CentroTile(2, 4)));
            pasos.Add(Ir(m.CentroTile(4, 4)));
            pasos.Add(Ir(m.CentroTile(9, 4)));
        }

        // El clip completo deja ver el diálogo de entrada; el corto lo cierra antes de grabar.
        if (!completo) CerrarDialogo();

        var ajustes = InputSystem.settings;
        var previo = ajustes.editorInputBehaviorInPlayMode;
        ajustes.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        // Sin foco, el Input System desactiva el teclado y descarta las teclas simuladas.
        var fondoPrevio = ajustes.backgroundBehavior;
        ajustes.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        if (Keyboard.current != null && !Keyboard.current.enabled) InputSystem.EnableDevice(Keyboard.current);
        yield return null;

        PlayerController nilo = RelevoGame.Jugador;
        Camera principal = Camera.main;
        UIManager ui = FindFirstObjectByType<UIManager>();

        // El HUD pasa a dibujarse con la cámara del juego para que entre en la textura.
        Canvas hud = ui.GetComponentInChildren<Canvas>();
        RenderMode modoPrevio = hud.renderMode;
        hud.renderMode = RenderMode.ScreenSpaceCamera;
        hud.worldCamera = principal;
        hud.planeDistance = principal.nearClipPlane + 0.5f;

        Abrir(false);
        principal.targetTexture = lienzo;

        int maximo = completo ? Fps * 40 : Fps * 5;
        int paso = 0;
        int esperar = completo ? 0 : 12;
        int pulsando = -1;
        int enDialogo = 0;
        int trasCompletar = -1;

        for (int f = 0; f < maximo; f++)
        {
            Estado = modo + " " + f + " paso " + paso + "/" + pasos.Count;
            Keyboard teclado = Keyboard.current;
            var teclas = new List<Key>();

            DialogoUI d = FindFirstObjectByType<DialogoUI>();
            bool hayDialogo = d != null && d.Abierto;

            if (hayDialogo)
            {
                // Se deja leer un momento y se cierra con E, como lo haría un jugador.
                enDialogo++;
                if (!completo) d.Cerrar();
                else if ((enDialogo >= 30 && enDialogo < 33) || (enDialogo >= 46 && enDialogo < 49)) teclas.Add(Key.E);
                else if (enDialogo > 75) { d.Avanzar(); enDialogo = 40; }   // respaldo si la pulsación no llegó
            }
            else
            {
                enDialogo = 0;

                if (pulsando >= 0)
                {
                    // Girarse hacia el objeto unos fotogramas y pulsar E.
                    Vector3 hacia = pasos[paso].mirar.Value - nilo.transform.position;
                    hacia.y = 0f;
                    if (hacia.sqrMagnitude > 0.01f)
                        nilo.transform.rotation = Quaternion.Slerp(nilo.transform.rotation, Quaternion.LookRotation(hacia), 0.35f);
                    if (pulsando >= 6 && pulsando < 9) teclas.Add(Key.E);
                    pulsando++;
                    if (pulsando > 16) { pulsando = -1; paso++; }
                }
                else if (esperar > 0) esperar--;
                else if (paso < pasos.Count)
                {
                    Vector3 delta = pasos[paso].destino - nilo.transform.position;
                    delta.y = 0f;
                    if (delta.magnitude < pasos[paso].parada)
                    {
                        if (pasos[paso].mirar.HasValue) pulsando = 0; else paso++;
                    }
                    else
                    {
                        Vector3 adelante = Plano(principal.transform.forward);
                        Vector3 derecha = Plano(principal.transform.right);
                        Vector2 local = new Vector2(Vector3.Dot(delta, derecha), Vector3.Dot(delta, adelante)).normalized;
                        if (local.x > 0.38f) teclas.Add(Key.D);
                        if (local.x < -0.38f) teclas.Add(Key.A);
                        if (local.y > 0.38f) teclas.Add(Key.W);
                        if (local.y < -0.38f) teclas.Add(Key.S);
                    }
                }
            }

            // Al completar el nivel se graba un momento del menú final y se corta.
            if (trasCompletar < 0 && paso >= pasos.Count && ui.BloqueaJuego) trasCompletar = 0;
            if (trasCompletar >= 0 && ++trasCompletar > Fps * 2) break;

            if (teclado != null) InputSystem.QueueStateEvent(teclado, new KeyboardState(teclas.ToArray()));
            yield return Grabar();
        }

        Keyboard k = Keyboard.current;
        if (k != null) InputSystem.QueueStateEvent(k, new KeyboardState());
        principal.targetTexture = null;
        hud.renderMode = modoPrevio;
        ajustes.editorInputBehaviorInPlayMode = previo;
        ajustes.backgroundBehavior = fondoPrevio;
        Cerrar();
    }
}
#endif
