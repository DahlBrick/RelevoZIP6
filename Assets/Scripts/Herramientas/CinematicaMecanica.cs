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
/// Cinemáticas de las mecánicas del Patio de Bombas, grabadas a MP4 (1920x1080, 30 fps) en Play
/// con una cámara de cine propia (no la del juego): planos detalle, planos en primera persona
/// desde los ojos de Nilo, profundidad de campo, cámara en mano sutil, bandas 2,39:1, cámara
/// lenta en el momento clave y fundidos a negro.
///
///  - "apagado": Nilo se aleja de la batería, la reserva se agota y se apaga.
///  - "cajas":   Nilo empuja una caja hasta su placa y se despliega el puente de la placa.
///
/// La acción la hace el propio juego con teclas simuladas; el director solo elige el plano según
/// lo que está pasando (fase de la acción), así que los cortes caen siempre en el momento justo.
/// Herramienta de editor: no entra en la build.
/// </summary>
public sealed class CinematicaMecanica : MonoBehaviour
{
    const int Ancho = 1920, Alto = 1080, Fps = 30;

    public static string Estado = "";
    string modo, ruta;

    public static string Iniciar(string modo, string ruta)
    {
        GameObject go = new GameObject("Cinematica mecanica");
        CinematicaMecanica c = go.AddComponent<CinematicaMecanica>();
        c.modo = modo;
        c.ruta = ruta;
        Estado = "iniciando " + modo;
        return ruta;
    }

    // ------------------------------------------------------------------ contexto común

    LevelLayout mapa;
    PlayerController nilo;
    ReservaEnergia reserva;
    Transform hueso;            // hueso de la cabeza
    Camera cam;
    DepthOfField dof;
    Vignette vineta;
    ColorAdjustments color;
    ChromaticAberration aberracion;
    FilmGrain grano;
    Canvas[] lienzos;
    Camera principal;
    GameObject goVolumen;
    InputSettings.EditorInputBehaviorInPlayMode previoEditor;
    InputSettings.BackgroundBehavior previoFondo;

    MediaEncoder codificador;
    RenderTexture lienzo;
    Texture2D fotograma;
    int cuenta;
    float relojVideo;            // segundos de video grabados
    readonly List<Key> teclas = new List<Key>();

    IEnumerator Start()
    {
        mapa = RelevoGame.Layout;
        nilo = RelevoGame.Jugador;
        reserva = RelevoGame.Reserva;
        foreach (Transform t in nilo.GetComponentsInChildren<Transform>())
            if (t.name == "Head" && t.GetComponent<MeshRenderer>() == null) { hueso = t; break; }

        DialogoUI d = FindFirstObjectByType<DialogoUI>();
        if (d != null && d.Abierto) d.Cerrar();

        var ajustes = InputSystem.settings;
        previoEditor = ajustes.editorInputBehaviorInPlayMode;
        previoFondo = ajustes.backgroundBehavior;
        ajustes.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        ajustes.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        if (Keyboard.current != null && !Keyboard.current.enabled) InputSystem.EnableDevice(Keyboard.current);

        PrepararCamara();
        yield return null;

        if (modo == "apagado") yield return Apagado();
        else yield return Cajas();

        Terminar();
    }

    void PrepararCamara()
    {
        lienzos = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in lienzos) c.enabled = false;

        principal = Camera.main;
        GameObject go = new GameObject("Camara de cine");
        cam = go.AddComponent<Camera>();
        cam.CopyFrom(principal);
        cam.orthographic = false;
        cam.nearClipPlane = 0.08f;
        cam.farClipPlane = 200f;
        cam.depth = principal.depth + 1;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = RenderSettings.fogColor;
        var datos = go.AddComponent<UniversalAdditionalCameraData>();
        datos.renderPostProcessing = true;
        datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        datos.antialiasingQuality = AntialiasingQuality.High;
        principal.enabled = false;

        goVolumen = new GameObject("Volumen de cine");
        Volume v = goVolumen.AddComponent<Volume>();
        v.isGlobal = true;
        v.priority = 100f;
        VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        dof = perfil.Add<DepthOfField>(true);
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focalLength.Override(60f);
        dof.aperture.Override(2.4f);
        vineta = perfil.Add<Vignette>(true);
        vineta.smoothness.Override(0.55f);
        color = perfil.Add<ColorAdjustments>(true);
        color.postExposure.Override(0f);
        color.saturation.Override(0f);
        color.contrast.Override(8f);
        aberracion = perfil.Add<ChromaticAberration>(true);
        aberracion.intensity.Override(0f);
        grano = perfil.Add<FilmGrain>(true);
        grano.type.Override(FilmGrainLookup.Thin1);
        grano.intensity.Override(0.18f);
        v.sharedProfile = perfil;

        Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        if (File.Exists(ruta)) File.Delete(ruta);
        codificador = new MediaEncoder(ruta, new VideoTrackAttributes
        {
            frameRate = new MediaRational(Fps), width = Ancho, height = Alto,
            includeAlpha = false, bitRateMode = UnityEditor.VideoBitrateMode.High
        });
        lienzo = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        lienzo.antiAliasing = 8;
        fotograma = new Texture2D(Ancho, Alto, TextureFormat.RGBA32, false);
        cam.targetTexture = lienzo;
        Time.captureFramerate = Fps;
    }

    void Terminar()
    {
        if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        Time.timeScale = 1f;
        Time.captureFramerate = 0;
        if (codificador != null) codificador.Dispose();
        cam.targetTexture = null;
        principal.enabled = true;
        foreach (Canvas c in lienzos) if (c != null) c.enabled = true;
        Destroy(cam.gameObject);
        Destroy(goVolumen);
        lienzo.Release();
        var ajustes = InputSystem.settings;
        ajustes.editorInputBehaviorInPlayMode = previoEditor;
        ajustes.backgroundBehavior = previoFondo;
        Estado = "terminado " + modo + " (" + cuenta + " fotogramas, " + (cuenta / (float)Fps).ToString("F1") + " s) " + ruta;
        Destroy(gameObject);
    }

    /// <summary>Envía las teclas del fotograma, espera a que se dibuje y lo graba.</summary>
    IEnumerator Fotograma()
    {
        if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(teclas.ToArray()));
        yield return new WaitForEndOfFrame();

        RenderTexture previa = RenderTexture.active;
        RenderTexture.active = lienzo;
        fotograma.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0, false);
        RenderTexture.active = previa;

        int banda = (Alto - Mathf.RoundToInt(Ancho / 2.39f)) / 2;
        Color32[] negro = new Color32[Ancho * banda];
        for (int i = 0; i < negro.Length; i++) negro[i] = new Color32(0, 0, 0, 255);
        fotograma.SetPixels32(0, 0, Ancho, banda, negro);
        fotograma.SetPixels32(0, Alto - banda, Ancho, banda, negro);
        fotograma.Apply(false);
        codificador.AddFrame(fotograma);

        if (cuenta % 10 == 0)
            File.WriteAllBytes(Path.ChangeExtension(ruta, null) + "_" + cuenta.ToString("000") + ".jpg", fotograma.EncodeToJPG(85));
        cuenta++;
        relojVideo = cuenta / (float)Fps;
    }

    // ------------------------------------------------------------------ utilidades de cámara

    /// <summary>Efectos por defecto de cada fotograma; cada plano los ajusta después.</summary>
    void EfectosBase()
    {
        vineta.intensity.Override(0.3f);
        vineta.color.Override(Color.black);
        color.postExposure.Override(0f);
        color.saturation.Override(4f);
        color.colorFilter.Override(Color.white);
        aberracion.intensity.Override(0.05f);
        dof.active = true;
        dof.aperture.Override(2.4f);
        dof.focalLength.Override(60f);
    }

    /// <summary>Cámara en mano: un temblor lento y pequeño.</summary>
    Quaternion Mano(float fuerza)
    {
        float t = Time.time * 0.9f;
        float x = (Mathf.PerlinNoise(t, 3.1f) - 0.5f) * 1.6f * fuerza;
        float y = (Mathf.PerlinNoise(7.7f, t) - 0.5f) * 1.6f * fuerza;
        return Quaternion.Euler(x, y, 0f);
    }

    void Colocar(Vector3 pos, Vector3 mira, float fov, float mano = 1f)
    {
        cam.fieldOfView = fov;
        cam.transform.position = pos;
        cam.transform.rotation = Quaternion.LookRotation(mira - pos) * Mano(mano);
        dof.focusDistance.Override(Vector3.Distance(pos, mira));
    }

    /// <summary>Plano en primera persona: los ojos de Nilo, por delante de las lentes.</summary>
    void PrimeraPersona(float cabeceo, float giro, float fov)
    {
        Transform n = nilo.transform;
        // Por delante de las lentes y de los ojos luminosos, para no verlos desde dentro.
        Vector3 ojos = n.position + Vector3.up * 1.45f + n.forward * 0.9f;
        // El paso de Nilo se nota en un vaivén suave.
        float paso = Mathf.Sin(Time.time * 9f) * 0.012f * Mathf.Clamp01(nilo.GetComponent<CharacterController>().velocity.magnitude / 3f);
        cam.fieldOfView = fov;
        cam.transform.position = ojos + Vector3.up * paso;
        cam.transform.rotation = Quaternion.LookRotation(n.forward) * Quaternion.Euler(cabeceo, giro, 0f) * Mano(0.6f);
        dof.focusDistance.Override(4f);
        dof.aperture.Override(4f);
    }

    Transform puntaAntena;
    Vector3 Antena
    {
        get
        {
            if (puntaAntena == null)
                foreach (Transform t in nilo.GetComponentsInChildren<Transform>())
                    if (t.name == "Antena punta") { puntaAntena = t; break; }
            return puntaAntena != null ? puntaAntena.position : Cabeza + Vector3.up * 0.7f;
        }
    }

    Vector3 Cabeza { get { return hueso != null ? hueso.position + Vector3.up * 0.12f : nilo.transform.position + Vector3.up * 1.5f; } }
    Vector3 C(int col, int fila) { return mapa.CentroTile(col, fila); }

    static float Suave(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

    /// <summary>Teclas para caminar hacia un punto; devuelve true al llegar.</summary>
    bool Caminar(Vector3 objetivo, float parada)
    {
        teclas.Clear();
        Vector3 d = objetivo - nilo.transform.position;
        d.y = 0f;
        if (d.magnitude < parada) return true;
        Transform referencia = principal.transform;
        Vector3 adelante = referencia.forward; adelante.y = 0f; adelante.Normalize();
        Vector3 derecha = referencia.right; derecha.y = 0f; derecha.Normalize();
        Vector2 local = new Vector2(Vector3.Dot(d, derecha), Vector3.Dot(d, adelante)).normalized;
        if (local.x > 0.38f) teclas.Add(Key.D);
        if (local.x < -0.38f) teclas.Add(Key.A);
        if (local.y > 0.38f) teclas.Add(Key.W);
        if (local.y < -0.38f) teclas.Add(Key.S);
        return false;
    }

    EnergySocket Base(string id)
    {
        foreach (EnergySocket b in FindObjectsByType<EnergySocket>(FindObjectsSortMode.None))
            if (b.Identificador == id) return b;
        return null;
    }

    // ------------------------------------------------------------------ cinemática: apagado

    IEnumerator Apagado()
    {
        // Preparación (sin grabar): batería en la base A, puente desplegado, Nilo a su lado.
        BatteryController bateria = RelevoGame.Bateria;
        bateria.Sujetar(nilo);
        Base("A").Interactuar(nilo);
        nilo.Colocar(C(3, 3));
        nilo.transform.rotation = Quaternion.LookRotation(new Vector3(1f, 0f, 1f));
        for (int i = 0; i < 40; i++) yield return null;

        Vector3 posBateria = bateria.transform.position;
        Vector3[] ida = { C(4, 2), C(6, 2), C(7, 3) };
        int tramo = 0;
        int fase = 0;               // 0 caminar, 1 detenerse, 2 caminar hasta apagarse, 3 apagado
        float relojFase = 0f;
        float relojApagado = -1f;

        while (true)
        {
            float dt = Time.deltaTime;
            relojFase += dt;
            EfectosBase();
            teclas.Clear();

            // --- acción
            if (fase == 0)
            {
                if (Caminar(ida[tramo], 0.45f)) tramo++;
                if (tramo >= ida.Length) { fase = 1; relojFase = 0f; teclas.Clear(); reserva.FijarReserva(2.6f); }
            }
            else if (fase == 1)
            {
                // Se detiene, mira atrás hacia la batería y vuelve a mirar al frente.
                Vector3 atras = posBateria - nilo.transform.position; atras.y = 0f;
                Vector3 frente = Vector3.back;
                if (relojFase > 1.1f && relojFase < 2.2f)
                    nilo.transform.rotation = Quaternion.Slerp(nilo.transform.rotation, Quaternion.LookRotation(atras), 0.07f);
                else if (relojFase >= 2.2f)
                    nilo.transform.rotation = Quaternion.Slerp(nilo.transform.rotation, Quaternion.LookRotation(frente), 0.12f);
                if (relojFase > 2.7f) { fase = 2; relojFase = 0f; }
            }
            else if (fase == 2)
            {
                Caminar(C(7, 9), 0.4f);
                if (reserva.Apagado) { fase = 3; relojFase = 0f; teclas.Clear(); relojApagado = 0f; }
            }
            else
            {
                teclas.Clear();
                relojApagado += dt;
            }

            // --- dirección de cámara
            Vector3 n = nilo.transform.position;
            if (fase == 0 && relojVideo < 1.3f)
            {
                // P1: detalle de la batería en su base; Nilo se aleja desenfocado.
                float s = Suave(relojVideo / 1.3f);
                Vector3 desde = posBateria + new Vector3(-2.3f, 0.75f, 1.0f);
                Vector3 mira = posBateria + Vector3.up * 0.25f + Vector3.right * 0.6f;
                Colocar(Vector3.Lerp(desde, desde + new Vector3(0.25f, 0.1f, -0.2f), s), mira, 34f, 0.5f);
                float aBateria = Vector3.Distance(cam.transform.position, posBateria);
                dof.focusDistance.Override(Mathf.Lerp(aBateria, Vector3.Distance(cam.transform.position, n), Suave((relojVideo - 0.5f) / 0.8f)));
            }
            else if (fase == 0)
            {
                // P2: primera persona cruzando el puente, con el canal a los lados.
                PrimeraPersona(14f, 0f, 68f);
            }
            else if (fase == 1 && relojFase < 1.2f)
            {
                // P3: plano detalle de la antena parpadeando en coral.
                Vector3 antena = Antena;
                float a = Mathf.Lerp(-35f, -10f, Suave(relojFase / 1.2f)) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(a), 0.05f, -Mathf.Cos(a));
                Colocar(antena + dir.normalized * 1.1f + Vector3.down * 0.15f, antena + Vector3.down * 0.2f, 28f, 0.7f);
                dof.aperture.Override(1.8f);
                vineta.intensity.Override(0.38f);
            }
            else if (fase == 1)
            {
                // P4: primera persona mirando atrás: la batería, lejos, al otro lado del canal.
                PrimeraPersona(6f, 0f, 46f);
                dof.focusDistance.Override(Vector3.Distance(cam.transform.position, posBateria));
                dof.aperture.Override(3.2f);
                vineta.intensity.Override(0.42f);
                vineta.color.Override(new Color(0.35f, 0.05f, 0.03f));
            }
            else if (fase == 2)
            {
                // P5: plano medio lateral a la altura de la rodilla; camina con la antena en alarma.
                Vector3 lado = nilo.transform.right;
                Colocar(n + lado * 3.6f + Vector3.up * 1.0f + nilo.transform.forward * 1.2f, n + Vector3.up * 1.15f, 36f, 0.9f);
            }
            else if (relojApagado < 1.9f)
            {
                // P6: primer plano frontal, empuje lento: destellos rojos, ojos que se apagan.
                float s = Suave(relojApagado / 1.9f);
                Vector3 cara = n + Vector3.up * 1.35f;
                Vector3 frente = nilo.transform.forward;
                Colocar(cara + frente * Mathf.Lerp(2.3f, 1.7f, s) + nilo.transform.right * 0.25f + Vector3.down * 0.15f, cara + Vector3.down * 0.1f * s, 30f, 0.4f);
                dof.aperture.Override(2f);
            }
            else if (relojApagado < 3.3f)
            {
                // P7: primera persona mientras cae la cabeza: la vista se va al suelo y se apaga.
                float s = Suave((relojApagado - 1.9f) / 1.4f);
                PrimeraPersona(14f + 60f * reserva.Caida, 0f, 60f);
                bool parpadeo = Mathf.Repeat(relojApagado * 7f, 1f) < 0.25f;
                color.saturation.Override(Mathf.Lerp(-20f, -100f, s));
                color.postExposure.Override(Mathf.Lerp(0f, -6f, s) - (parpadeo ? 1.2f : 0f));
                aberracion.intensity.Override(Mathf.Lerp(0.3f, 1f, s));
                vineta.intensity.Override(Mathf.Lerp(0.45f, 0.75f, s));
                vineta.color.Override(new Color(0.25f, 0f, 0f));
            }
            else if (relojApagado < 6.2f)
            {
                // P8: plano general que sube: Nilo apagado, el humo, la batería lejos. Fundido final.
                float s = Suave((relojApagado - 3.3f) / 2.9f);
                Vector3 desde = n + new Vector3(2.2f, 0.6f, -2.4f);
                Vector3 hasta = n + new Vector3(4.5f, 5.5f, -6.5f);
                Colocar(Vector3.Lerp(desde, hasta, s), Vector3.Lerp(n + Vector3.up * 1f, n + Vector3.up * 0.4f, s), 40f, 0.35f);
                float entrada = Suave((relojApagado - 3.3f) / 0.5f);
                float salida = Suave((relojApagado - 5.4f) / 0.8f);
                color.postExposure.Override(Mathf.Lerp(-8f, 0f, entrada) - 8f * salida);
                color.saturation.Override(-35f);
            }
            else break;

            Estado = "apagado fase " + fase + " video " + relojVideo.ToString("F1") + " s";
            yield return Fotograma();
            if (relojVideo > 25f) break;
        }
    }

    // ------------------------------------------------------------------ cinemática: cajas

    IEnumerator Cajas()
    {
        // Preparación: la reserva no interviene en esta escena.
        if (reserva != null) reserva.enabled = false;
        nilo.Colocar(C(11, 2));
        nilo.transform.rotation = Quaternion.LookRotation(Vector3.back);
        CajaEmpujable caja = null;
        foreach (CajaEmpujable c in CajaEmpujable.Todas) if (c.Casilla == new Vector2Int(11, 4)) caja = c;
        PlacaPresion placa = null;
        foreach (PlacaPresion p in PlacaPresion.Todas) if (p.Puente != null) placa = p;
        for (int i = 0; i < 30; i++) yield return null;

        Vector3 posPlaca = C(11, 6);
        Vector3 puente = (C(9, 9) + C(10, 9)) * 0.5f;
        Vector3[] despues = { C(12, 5), C(12, 8), C(11, 9), C(9, 9) };
        int fase = 0;            // 0 acercarse, 1 empujar, 2 placa (cámara lenta), 3 cruzar
        int tramo = 0;
        float relojFase = 0f;
        float relojPlaca = -1f;
        int empujes = 0;
        Vector2Int ultima = caja.Casilla;

        while (true)
        {
            relojFase += Time.unscaledDeltaTime > 0f ? Time.deltaTime : 0f;
            EfectosBase();
            teclas.Clear();
            if (caja.Casilla != ultima) { empujes++; ultima = caja.Casilla; }

            // --- acción
            if (fase == 0)
            {
                if (relojVideo > 0.6f && Caminar(C(11, 3), 0.25f)) { fase = 1; relojFase = 0f; }
            }
            else if (fase == 1)
            {
                Caminar(C(11, 5), 0.5f);
                if (placa.PorCaja) { fase = 2; relojFase = 0f; relojPlaca = 0f; teclas.Clear(); }
            }
            else if (fase == 2)
            {
                relojPlaca += Time.deltaTime / Mathf.Max(0.01f, Time.timeScale);
                if (relojPlaca > 2.2f) { fase = 3; relojFase = 0f; Time.timeScale = 1f; }
            }
            else
            {
                if (tramo < despues.Length && Caminar(despues[tramo], 0.45f)) tramo++;
            }

            // --- dirección de cámara
            Vector3 n = nilo.transform.position;
            Vector3 pc = caja.transform.position;
            if (fase == 0)
            {
                // P1: detalle a ras de suelo desde detrás de la placa: la caja enfocada, Nilo llega.
                float s = Suave(relojVideo / 1.6f);
                Vector3 desde = posPlaca + new Vector3(0.9f, 0.25f, -0.7f);
                Colocar(Vector3.Lerp(desde, desde + new Vector3(-0.3f, 0.05f, 0.15f), s), pc + Vector3.up * 0.5f, 34f, 0.5f);
                dof.aperture.Override(1.8f);
            }
            else if (fase == 1 && empujes == 0)
            {
                // P2: lateral a ras de suelo: Nilo apoya el peso y la caja empieza a deslizarse.
                Vector3 medio = (n + pc) * 0.5f;
                Colocar(medio + Vector3.right * 3.2f + Vector3.up * 0.45f, medio + Vector3.up * 0.7f, 34f, 0.8f);
            }
            else if (fase == 1)
            {
                // P3: primera persona detrás de la caja; al fondo, el rombo turquesa de la placa.
                PrimeraPersona(22f, 0f, 62f);
                dof.focusDistance.Override(Vector3.Distance(cam.transform.position, pc));
            }
            else if (fase == 2 && relojPlaca < 0.9f)
            {
                // P4: macro de la placa en cámara lenta: la caja asienta y la placa se enciende.
                Time.timeScale = 0.35f;
                Vector3 borde = posPlaca + new Vector3(0.75f, 0f, -0.95f);
                Colocar(borde + new Vector3(0.55f, 0.18f, -0.35f) + Vector3.left * 0.15f * relojPlaca, borde + Vector3.up * 0.12f, 30f, 0.3f);
                dof.aperture.Override(1.6f);
            }
            else if (fase == 2)
            {
                // P5: el cable encendido lleva al puente de la placa, que sube tramo a tramo.
                Time.timeScale = Mathf.Lerp(0.45f, 1f, Suave((relojPlaca - 0.9f) / 1.3f));
                float s = Suave((relojPlaca - 0.9f) / 1.3f);
                Vector3 desde = C(11, 8) + new Vector3(0.7f, 0.4f, 0.8f);
                Vector3 hasta = C(11, 9) + new Vector3(0.9f, 1.3f, -1.6f);
                Colocar(Vector3.Lerp(desde, hasta, s), Vector3.Lerp(C(11, 8), puente, s) + Vector3.up * 0.1f, 40f, 0.5f);
            }
            else if (relojFase < 4.2f)
            {
                // P6: grúa: Nilo baja por el muelle y cruza el puente turquesa. Fundido final.
                float s = Suave(relojFase / 4.2f);
                Vector3 desde = C(13, 7) + new Vector3(0.5f, 1.4f, 1f);
                Vector3 hasta = C(13, 11) + new Vector3(1.5f, 6.5f, -3.5f);
                Colocar(Vector3.Lerp(desde, hasta, s), Vector3.Lerp(n + Vector3.up, puente, s * 0.6f), 42f, 0.35f);
                color.postExposure.Override(-8f * Suave((relojFase - 3.4f) / 0.8f));
            }
            else break;

            Estado = "cajas fase " + fase + " empujes " + empujes + " video " + relojVideo.ToString("F1") + " s";
            yield return Fotograma();
            if (relojVideo > 25f) break;
        }

        if (reserva != null) reserva.enabled = true;
    }
}
#endif
