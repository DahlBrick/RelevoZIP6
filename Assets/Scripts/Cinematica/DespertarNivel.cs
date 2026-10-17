using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// El despertar de Nilo al empezar el primer nivel (presente, de día). Dura unos 14 s y se salta
/// pulsando Esc. Toda la puesta en escena depende del tiempo T:
///
///  0,0–2,5  Negro que se abre en párpados: dos intentos y luego del todo.
///  1,5–5,5  Primer plano de Nilo tumbado en las tablas. La luz de la antena parpadea y se queda.
///  5,5–9,5  Plano a la altura de la rodilla: se incorpora rígido, como una máquina; tiembla a
///           media altura, resbala un poco hacia delante y se recompone.
///  9,5–11,5 Mira a un lado y a otro (aún no sabe dónde está). La cámara rodea.
/// 11,5–14,0 La cámara sube hasta la vista del juego, las bandas se retiran y empieza a jugar.
///
/// Nilo no tiene ojos luminosos: lo que se enciende es la antena. No aparece ninguna persona.
/// Al terminar se devuelven cámara, interfaz y control, y se llama a 'alTerminar' (la intro del
/// nivel y el tutorial de Maia).
/// </summary>
public sealed class DespertarNivel : MonoBehaviour
{
    // Tramos: párpados y Nilo tumbado (0-5,6), se incorpora (5,6-9,2), mira alrededor (9,6-11,6),
    // habla Maia (la cinemática espera en T_Dialogo mientras dura el diálogo) y la cámara llega a la
    // vista del juego sin corte (T_Dialogo-Duracion).
    const float T_Dialogo = 12f;
    const float Duracion = 15f;

    PlayerController nilo;
    Camera camaraJuego;
    Camera cine;
    Action alTerminar;
    Transform cuerpo;
    Vector3 pie;
    Quaternion giroInicial;
    readonly System.Collections.Generic.List<Renderer> antena = new System.Collections.Generic.List<Renderer>();
    Canvas[] interfaz;
    RectTransform bandaArriba, bandaAbajo;
    Image negro;
    float T;
    DialogoUI dialogo;
    bool dialogoLanzado;
    float relojDialogo;
    Animator animador;
    GameObject sombraContacto;
    AudioSource fuente, ambiente;
    AudioClip clipArranque, clipZumbido, clipGolpe, clipPitido, clipEstatica, clipRele;
    float tPrevio = -1f;
    float pulsadoEsc;
    bool terminado;

    public static DespertarNivel Empezar(PlayerController nilo, Camera camaraJuego, Action alTerminar)
    {
        var d = new GameObject("Despertar de Nilo").AddComponent<DespertarNivel>();
        d.nilo = nilo;
        d.camaraJuego = camaraJuego;
        d.alTerminar = alTerminar;
        d.Preparar();
        return d;
    }

    void Preparar()
    {
        cuerpo = nilo.transform;
        pie = cuerpo.position;
        giroInicial = cuerpo.rotation;
        nilo.PuedeMoverse = false;
        nilo.enabled = false;
        CharacterController cc = nilo.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        foreach (Renderer r in nilo.GetComponentsInChildren<Renderer>(true))
            if (r.name.Contains("Antena punta") || r.name.Contains("Aro de antena")) antena.Add(r);

        // Cámara de cine: perspectiva, tinta y posproceso, por encima de la del juego.
        cine = new GameObject("Camara despertar").AddComponent<Camera>();
        cine.fieldOfView = 40f;
        cine.nearClipPlane = 0.05f;
        cine.farClipPlane = 1500f;
        cine.clearFlags = camaraJuego != null ? camaraJuego.clearFlags : CameraClearFlags.Skybox;
        cine.backgroundColor = camaraJuego != null ? camaraJuego.backgroundColor : Color.black;
        cine.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        EstiloComic.RendererTinta(cine);
        if (camaraJuego != null) camaraJuego.enabled = false;

        // La interfaz del juego se oculta mientras dura.
        interfaz = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in interfaz) c.enabled = false;

        // Bandas 2,39:1 y negro de párpados en un lienzo propio (sin desenfoque).
        Canvas lienzo = new GameObject("Lienzo despertar").AddComponent<Canvas>();
        lienzo.transform.SetParent(transform, false);
        lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
        lienzo.sortingOrder = 500;
        negro = Rect(lienzo.transform, "Negro", Vector2.zero, Vector2.one).GetComponent<Image>();
        dialogo = DialogoUI.Crear(lienzo.transform);
        animador = nilo.GetComponentInChildren<Animator>();
        foreach (Transform t in nilo.GetComponentsInChildren<Transform>(true))
            if (t.name == "Sombra de contacto") sombraContacto = t.gameObject;
        if (sombraContacto != null) sombraContacto.SetActive(false);
        PrepararSonido();
        bandaArriba = Rect(lienzo.transform, "Banda arriba", new Vector2(0f, 1f), Vector2.one);
        bandaAbajo = Rect(lienzo.transform, "Banda abajo", Vector2.zero, new Vector2(1f, 0f));
        Postura(0f, 88f, 0f);
        Despierta(false);
    }

    static RectTransform Rect(Transform padre, string nombre, Vector2 min, Vector2 max)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = Color.black;
        return rt;
    }

    void PrepararSonido()
    {
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        ambiente = gameObject.AddComponent<AudioSource>();
        ambiente.clip = SonidosApertura.Agua();
        ambiente.loop = true;
        ambiente.volume = 0f;
        ambiente.Play();
        clipArranque = SonidosApertura.Arranque();
        clipZumbido = SonidosApertura.Zumbido();
        clipGolpe = SonidosApertura.Golpe();
        clipPitido = SonidosApertura.Pitido(1320f);
        clipEstatica = SonidosApertura.Estatica();
        clipRele = SonidosApertura.Rele();
    }

    bool Cruza(float t, float marca) { return tPrevio < marca && t >= marca; }

    /// <summary>Efectos ligados al tiempo T: suenan una vez al cruzar su marca.</summary>
    void Sonidos(float t)
    {
        // Mar y viento, que entran con los párpados.
        ambiente.volume = 0.35f * S(Tramo(t, 0.4f, 3f)) * (1f - 0.5f * S(Tramo(t, T_Dialogo + 0.5f, Duracion)));
        if (Cruza(t, 0.6f)) fuente.PlayOneShot(clipRele, 0.35f);          // primer intento de abrir
        if (Cruza(t, 1.0f)) fuente.PlayOneShot(clipRele, 0.45f);
        if (Cruza(t, 1.7f)) fuente.PlayOneShot(clipArranque, 0.7f);      // sistemas arrancando
        if (Cruza(t, 3.4f)) fuente.PlayOneShot(clipPitido, 0.25f);       // antena: parpadeo
        if (Cruza(t, 3.9f)) fuente.PlayOneShot(clipPitido, 0.25f);
        if (Cruza(t, 4.6f)) { fuente.PlayOneShot(clipPitido, 0.35f); fuente.PlayOneShot(clipZumbido, 0.4f); }
        if (Cruza(t, 5.6f)) fuente.PlayOneShot(clipZumbido, 0.5f);       // servos: empieza a incorporarse
        if (Cruza(t, 7.0f)) fuente.PlayOneShot(clipRele, 0.5f);          // se queda a medias, esfuerzo
        if (Cruza(t, 7.9f)) fuente.PlayOneShot(clipZumbido, 0.55f);
        if (Cruza(t, 9.2f)) fuente.PlayOneShot(clipGolpe, 0.6f);         // pie en las tablas
        if (Cruza(t, 9.7f)) fuente.PlayOneShot(clipGolpe, 0.35f);        // resbalón y se recompone
        if (Cruza(t, 10.2f)) fuente.PlayOneShot(clipZumbido, 0.3f);      // gira la cabeza
        if (Cruza(t, 11.2f)) fuente.PlayOneShot(clipZumbido, 0.3f);
        if (Cruza(t, T_Dialogo - 0.4f)) fuente.PlayOneShot(clipEstatica, 0.45f);  // Maia entra por la red
        tPrevio = t;
    }

    static float S(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
    static float Tramo(float t, float a, float b) { return Mathf.Clamp01((t - a) / (b - a)); }

    void Update()
    {
        if (terminado) return;
        var teclado = Keyboard.current;
        if (teclado != null && teclado.escapeKey.isPressed) pulsadoEsc += Time.deltaTime; else pulsadoEsc = 0f;
        if (pulsadoEsc > 0.6f) { Terminar(); return; }

        // Diálogo de Maia: se lanza en T_Dialogo y la cinemática se queda quieta hasta que termina.
        // E (o Intro / Espacio) completa la frase o pasa a la siguiente; si no, avanza sola.
        if (!dialogoLanzado && T >= T_Dialogo)
        {
            dialogoLanzado = true;
            dialogo.Encolar("Maia", "¿Hola? ¿Hay alguien en la red? ...¡Nilo! Sabía que ibas a despertar.", RelevoUI.Verde);
            dialogo.Encolar("Maia", "Llevas... déjame ver... mucho rato dormido. No te digo cuánto, porque te vas a sentir mal.", RelevoUI.Verde);
        }
        bool hablando = dialogoLanzado && dialogo.Abierto;
        if (hablando)
        {
            bool tecla = teclado != null && (teclado.eKey.wasPressedThisFrame || teclado.enterKey.wasPressedThisFrame || teclado.spaceKey.wasPressedThisFrame);
            if (dialogo.TerminoDeEscribir) relojDialogo += Time.deltaTime; else relojDialogo = 0f;
            if (tecla || relojDialogo > 3.5f) { relojDialogo = 0f; dialogo.Avanzar(); }
        }
        else T += Time.deltaTime;

        if (T >= Duracion) { Terminar(); return; }
        Sonidos(T);
        Escena(T);
    }

    void Escena(float t)
    {
        Vector3 adelante = giroInicial * Vector3.forward;
        Vector3 derecha = giroInicial * Vector3.right;

        // --- párpados: dos intentos y luego se abren del todo
        float abre = t < 0.6f ? 0f
                   : t < 1.0f ? 0.35f * Mathf.Sin(Mathf.PI * Tramo(t, 0.6f, 1.0f))
                   : t < 1.6f ? 0.6f * Mathf.Sin(Mathf.PI * Tramo(t, 1.0f, 1.6f))
                   : S(Tramo(t, 1.7f, 2.6f));
        negro.color = new Color(0f, 0f, 0f, 1f - abre);

        // --- bandas: se retiran al final
        float banda = 0.12f * (1f - S(Tramo(t, Duracion - 2.4f, Duracion - 0.6f)));
        bandaArriba.anchorMin = new Vector2(0f, 1f - banda);
        bandaAbajo.anchorMax = new Vector2(1f, banda);

        // --- antena: parpadea y se queda
        bool luz = t > 3.4f && (t > 4.6f || Mathf.Repeat(t * 7f, 1f) > 0.55f);
        Despierta(luz);

        // --- Nilo
        // Se incorpora como una máquina: hasta medio cuerpo, un temblor de esfuerzo, y arriba.
        float tumbado;
        if (t < 5.6f) tumbado = 88f;
        else if (t < 7.0f) tumbado = Mathf.Lerp(88f, 50f, S(Tramo(t, 5.6f, 7.0f)));
        else if (t < 7.9f) tumbado = Mathf.Lerp(50f, 46f, Tramo(t, 7.0f, 7.9f)) + Mathf.Sin(Mathf.PI * Tramo(t, 7.0f, 7.9f)) * Mathf.Sin(t * 47f) * 1.6f;
        else tumbado = Mathf.Lerp(46f, 0f, S(Tramo(t, 7.9f, 9.2f)));
        float resbalon = Mathf.Sin(Mathf.PI * Tramo(t, 8.8f, 9.8f)) * 0.18f;
        // Mientras está tumbado, el animador queda quieto (si no, su respiración lo hacía rebotar).
        if (animador != null) animador.speed = t < 8.6f ? 0f : 1f;
        float mira = t < 9.6f ? 0f : Mathf.Sin(Mathf.PI * 2f * Tramo(t, 9.6f, 11.6f)) * 35f;
        Postura(mira, tumbado, resbalon);

        // --- cámara (con un poco de cámara en mano)
        Vector3 mano = new Vector3(Mathf.PerlinNoise(t * 0.6f, 1f) - 0.5f, Mathf.PerlinNoise(2f, t * 0.6f) - 0.5f, 0f) * 0.05f;
        Vector3 cabezaTumbado = pie - adelante * 1.35f + Vector3.up * 0.6f;
        Vector3 cabeza = cuerpo.position + cuerpo.up * 1.45f;
        if (t < 5.6f)
        {
            // P1 · Picado lateral sobre Nilo tumbado; la cámara desciende despacio.
            float s = S(Tramo(t, 0f, 5.6f));
            Vector3 centroCuerpo = pie - adelante * 0.75f + Vector3.up * 0.4f;
            Vector3 desde = centroCuerpo + derecha * 2.6f + Vector3.up * 3.0f + adelante * 0.6f;
            Vector3 hasta = centroCuerpo + derecha * 2.0f + Vector3.up * 1.9f + adelante * 0.4f;
            Colocar(Vector3.Lerp(desde, hasta, s) + mano, centroCuerpo, 40f);
        }
        else if (t < 9.6f)
        {
            // P2 · Plano medio lateral, cámara fija sobre trípode: Nilo se incorpora en el centro del
            // encuadre. La mirada sigue una altura suave, no el cuerpo, para que el temblor sea de
            // Nilo y no de la cámara.
            float s = S(Tramo(t, 5.6f, 9.4f));
            Vector3 cam = pie + derecha * 3.4f - adelante * 1.2f + Vector3.up * 1.1f;
            Vector3 objetivo = pie - adelante * Mathf.Lerp(0.6f, 0f, s) + Vector3.up * Mathf.Lerp(0.5f, 1.1f, s);
            Colocar(cam, objetivo, 42f);
        }
        else if (t < T_Dialogo)
        {
            float s = S(Tramo(t, 9.6f, T_Dialogo));
            Vector3 orbita = Quaternion.AngleAxis(Mathf.Lerp(0f, -55f, s), Vector3.up) * (derecha * 2.6f + adelante * 1.6f);
            Colocar(pie + orbita + Vector3.up * Mathf.Lerp(0.9f, 1.4f, s) + mano, cabeza, 38f);
        }
        else if (t < T_Dialogo || camaraJuego == null)
        {
            Vector3 orbita = Quaternion.AngleAxis(-55f, Vector3.up) * (derecha * 2.6f + adelante * 1.6f);
            Colocar(pie + orbita + Vector3.up * 1.4f + mano, cabeza, 38f);
        }
        else
        {
            // Sube hasta la vista del juego sin corte. La cámara del juego es ortográfica: se termina
            // muy lejos, en su mismo eje, con el campo de visión que abarca lo mismo que su encuadre;
            // a esa distancia la perspectiva es casi plana y al pasar a ella no se nota el cambio.
            float s = S(Tramo(t, T_Dialogo, Duracion));
            Vector3 orbita = Quaternion.AngleAxis(-55f, Vector3.up) * (derecha * 2.6f + adelante * 1.6f);
            Vector3 desde = pie + orbita + Vector3.up * 1.4f;
            Transform tj = camaraJuego.transform;
            float d = Vector3.Dot(pie - tj.position, tj.forward);
            Vector3 centro = tj.position + tj.forward * d;
            const float Lejos = 320f;
            Vector3 hasta = centro - tj.forward * Lejos;
            float campo = camaraJuego.orthographic
                ? 2f * Mathf.Atan(camaraJuego.orthographicSize / Lejos) * Mathf.Rad2Deg
                : camaraJuego.fieldOfView;
            // El campo se cierra en escala logarítmica para que el alejamiento se sienta uniforme.
            Vector3 pos = Vector3.Lerp(desde, hasta, s);
            cine.transform.position = pos;
            Vector3 objetivo = Vector3.Lerp(cabeza, centro, S(Tramo(t, T_Dialogo, T_Dialogo + 1.6f)));
            cine.transform.rotation = Quaternion.Slerp(Quaternion.LookRotation(objetivo - pos, Vector3.up), tj.rotation, s * s);
            cine.fieldOfView = Mathf.Exp(Mathf.Lerp(Mathf.Log(38f), Mathf.Log(campo), s));
        }
    }

    void Colocar(Vector3 pos, Vector3 mira, float campo)
    {
        cine.transform.position = pos;
        cine.transform.rotation = Quaternion.LookRotation(mira - pos, Vector3.up);
        cine.fieldOfView = campo;
    }

    /// <summary>Nilo rígido, girando entero sobre los talones; 'tumbado' en grados (88 = boca arriba).</summary>
    void Postura(float mira, float tumbado, float resbalon)
    {
        Vector3 adelante = giroInicial * Vector3.forward;
        Vector3 derecha = giroInicial * Vector3.right;
        cuerpo.rotation = Quaternion.AngleAxis(-tumbado, derecha) * Quaternion.AngleAxis(mira, Vector3.up) * giroInicial;
        // El pivote está en los pies: al tumbarse, el cuerpo se elevaría poco y se hundiría en las
        // tablas. Se sube lo que mide medio cuerpo de grueso para que quede apoyado encima.
        float apoyo = Mathf.Sin(tumbado * Mathf.Deg2Rad) * 0.42f;
        cuerpo.position = pie + adelante * resbalon + Vector3.up * apoyo;
    }

    void Despierta(bool si)
    {
        foreach (Renderer r in antena) if (r != null) r.enabled = si;
    }

    void Terminar()
    {
        terminado = true;
        if (dialogo != null) dialogo.Cerrar();
        if (animador != null) animador.speed = 1f;
        if (sombraContacto != null) sombraContacto.SetActive(true);
        cuerpo.rotation = giroInicial;
        Despierta(true);
        CharacterController cc = nilo.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = true;
        nilo.enabled = true;
        nilo.Colocar(pie);
        nilo.PuedeMoverse = true;
        if (camaraJuego != null) camaraJuego.enabled = true;
        foreach (Canvas c in interfaz) if (c != null) c.enabled = true;
        Destroy(cine.gameObject);
        alTerminar?.Invoke();
        Destroy(gameObject);
    }
}
