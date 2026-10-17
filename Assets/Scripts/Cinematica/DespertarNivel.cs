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
    const float Duracion = 14f;

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

    static float S(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
    static float Tramo(float t, float a, float b) { return Mathf.Clamp01((t - a) / (b - a)); }

    void Update()
    {
        if (terminado) return;
        var teclado = Keyboard.current;
        if (teclado != null && teclado.escapeKey.isPressed) pulsadoEsc += Time.deltaTime; else pulsadoEsc = 0f;
        T += Time.deltaTime;
        if (T >= Duracion || pulsadoEsc > 0.6f) { Terminar(); return; }
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
        float levanta = S(Tramo(t, 5.6f, 9.2f));
        float temblor = Mathf.Sin(Mathf.PI * Tramo(t, 6.8f, 8.0f)) * Mathf.Sin(t * 47f) * 2.2f;
        float tumbado = Mathf.Lerp(88f, 0f, levanta);
        if (levanta > 0.35f && levanta < 0.6f) tumbado = Mathf.Lerp(88f * 0.65f, 88f * 0.55f, Tramo(t, 6.8f, 7.6f)) + temblor;
        float resbalon = Mathf.Sin(Mathf.PI * Tramo(t, 8.6f, 9.6f)) * 0.18f;
        float mira = t < 9.6f ? 0f : Mathf.Sin(Mathf.PI * 2f * Tramo(t, 9.6f, 11.6f)) * 35f;
        Postura(mira, tumbado, resbalon);

        // --- cámara (con un poco de cámara en mano)
        Vector3 mano = new Vector3(Mathf.PerlinNoise(t * 0.6f, 1f) - 0.5f, Mathf.PerlinNoise(2f, t * 0.6f) - 0.5f, 0f) * 0.05f;
        Vector3 cabezaTumbado = pie - adelante * 1.35f + Vector3.up * 0.25f;
        Vector3 cabeza = cuerpo.position + cuerpo.up * 1.45f;
        if (t < 5.6f)
        {
            float s = S(Tramo(t, 0f, 5.6f));
            // Desde arriba, sobre la cara: Nilo mira al cielo y la cámara baja despacio hacia él.
            Vector3 desde = cabezaTumbado + Vector3.up * 2.2f + derecha * 0.7f + adelante * 0.9f;
            Vector3 hasta = cabezaTumbado + Vector3.up * 1.35f + derecha * 0.45f + adelante * 0.55f;
            Colocar(Vector3.Lerp(desde, hasta, s) + mano, cabezaTumbado, 38f);
        }
        else if (t < 9.6f)
        {
            float s = S(Tramo(t, 5.6f, 9.6f));
            // Delante y algo a un lado, a la altura de la rodilla: se le ve la cara al incorporarse.
            Vector3 rodilla = pie - adelante * 2.8f + derecha * 1.3f + Vector3.up * 0.55f;
            Colocar(rodilla + mano, Vector3.Lerp(cabezaTumbado, cabeza, s) - Vector3.up * 0.2f, Mathf.Lerp(42f, 36f, s));
        }
        else if (t < Duracion - 2.6f)
        {
            float s = S(Tramo(t, 9.6f, Duracion - 2.6f));
            Vector3 orbita = Quaternion.AngleAxis(Mathf.Lerp(0f, -55f, s), Vector3.up) * (derecha * 2.6f + adelante * 1.6f);
            Colocar(pie + orbita + Vector3.up * Mathf.Lerp(0.9f, 1.4f, s) + mano, cabeza, 38f);
        }
        else if (camaraJuego != null)
        {
            // Sube hasta la vista del juego.
            float s = S(Tramo(t, Duracion - 2.6f, Duracion - 0.3f));
            Vector3 orbita = Quaternion.AngleAxis(-55f, Vector3.up) * (derecha * 2.6f + adelante * 1.6f);
            Vector3 desde = pie + orbita + Vector3.up * 1.4f;
            // Termina donde está la cámara del juego, con un campo de visión que abarca lo mismo
            // que su encuadre ortográfico: al cortar a ella no hay salto.
            Vector3 hasta = camaraJuego.transform.position;
            float distancia = Mathf.Max(1f, Vector3.Dot(pie - hasta, camaraJuego.transform.forward));
            float campo = camaraJuego.orthographic
                ? 2f * Mathf.Atan(camaraJuego.orthographicSize / distancia) * Mathf.Rad2Deg
                : camaraJuego.fieldOfView;
            cine.transform.position = Vector3.Lerp(desde, hasta, s);
            cine.transform.rotation = Quaternion.Slerp(Quaternion.LookRotation(cabeza - desde), camaraJuego.transform.rotation, s);
            cine.fieldOfView = Mathf.Lerp(38f, campo, s);
            negro.color = new Color(0f, 0f, 0f, S(Tramo(t, Duracion - 0.7f, Duracion)));
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
        cuerpo.position = pie + adelante * resbalon;
    }

    void Despierta(bool si)
    {
        foreach (Renderer r in antena) if (r != null) r.enabled = si;
    }

    void Terminar()
    {
        terminado = true;
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
