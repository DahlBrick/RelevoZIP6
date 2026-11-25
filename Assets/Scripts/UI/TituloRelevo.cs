using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pantalla de título (escena "Titulo", primera de Build Settings).
/// Liria de noche: una fila de edificios con ventanas que se encienden según los puertos
/// restaurados, la luz de la radio de Maia y el menú. Sin personas: solo la ciudad.
/// Flujo: Nueva partida → Cinematica_Liria → Nivel 1 … Nivel 13 → Créditos → Título.
/// </summary>
public class TituloRelevo : MonoBehaviour
{
    public const string Escena = "Titulo";
    public const string EscenaApertura = "Cinematica_Liria";

    [SerializeField] InputActionAsset acciones;

    RelevoInput entrada;
    readonly List<string> opciones = new List<string>();
    readonly List<Text> filas = new List<Text>();
    readonly List<Image> marcos = new List<Image>();
    readonly List<Image> ventanas = new List<Image>();
    readonly List<float> encendidoVentana = new List<float>();
    Image radio, velo;
    Text lema, aviso;
    CartaDePuertos carta;
    int seleccion, cierreCarta = -1;
    bool flanco, confirmandoNueva, saliendo;
    float t;
    AudioSource musica;

    void Start()
    {
        Application.targetFrameRate = 60;
        Time.timeScale = 1f;
        GameEvents.Limpiar();
        RelevoGame.Limpiar();
        entrada = acciones != null ? new RelevoInput(acciones) : null;

        if (FindFirstObjectByType<Camera>() == null)
        {
            var cam = new GameObject("Camara").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.04f, 0.07f);
        }

        musica = gameObject.AddComponent<AudioSource>();
        musica.clip = Resources.Load<AudioClip>("MareasDeLiria");
        musica.loop = true;
        musica.volume = 0f;
        musica.spatialBlend = 0f;
        if (musica.clip != null) musica.Play();

        Construir();
        Rehacer();
    }

    // ------------------------------------------------------------------ interfaz

    void Construir()
    {
        var raiz = new GameObject("Lienzo", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        raiz.transform.SetParent(transform, false);
        var canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var escala = raiz.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1200, 760);
        escala.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        Transform r = raiz.transform;

        // Cielo y agua.
        Estirar(RelevoUI.Franja(r, "Cielo", Centro, Centro, Vector2.zero, Vector2.zero, new Color(0.03f, 0.06f, 0.11f)).rectTransform, 0f, 1f);
        Estirar(RelevoUI.Franja(r, "Agua", Centro, Centro, Vector2.zero, Vector2.zero, new Color(0.02f, 0.05f, 0.08f)).rectTransform, 0f, 0.15f);

        // Liria: siluetas de edificios con ventanas. Se encienden según los puertos restaurados.
        var rnd = new System.Random(7);
        int completados = 0;
        for (int i = 1; i <= LevelLayouts.Cantidad; i++) if (SaveManager.NivelCompletado(i)) completados++;
        float encendidas = completados / (float)LevelLayouts.Cantidad;
        float x = -620f;
        while (x < 620f)
        {
            float ancho = 46 + rnd.Next(0, 64);
            float alto = 44 + rnd.Next(0, 92);
            var edificio = RelevoUI.Franja(r, "Edificio", Centro, new Vector2(0.5f, 0f), new Vector2(x + ancho / 2f, -266f), new Vector2(ancho - 6, alto), new Color(0.05f, 0.08f, 0.12f));
            for (float vy = 18; vy < alto - 14; vy += 22)
                for (float vx = -ancho / 2f + 14; vx < ancho / 2f - 14; vx += 18)
                {
                    var v = RelevoUI.Franja(edificio.transform, "Ventana", new Vector2(0.5f, 0f), Centro, new Vector2(vx + 4, vy), new Vector2(7, 9), Color.black);
                    ventanas.Add(v);
                    // Las ventanas encendidas aparecen poco a poco al abrir el título.
                    encendidoVentana.Add(rnd.NextDouble() < encendidas ? 0.6f + (float)rnd.NextDouble() * 2.5f : -1f);
                }
            x += ancho;
        }
        RelevoUI.Franja(r, "Muelle", Centro, Centro, new Vector2(0, -268), new Vector2(1300, 4), RelevoUI.CobreTenue);

        // Título.
        var titulo = RelevoUI.Texto(r, "Titulo", Centro, Centro, new Vector2(-250, 262), new Vector2(560, 120), 96, TextAnchor.MiddleLeft, RelevoUI.Ambar);
        titulo.text = "RELEVO";
        titulo.fontStyle = FontStyle.Bold;
        var sub = RelevoUI.Texto(r, "Subtitulo", Centro, Centro, new Vector2(-250, 192), new Vector2(560, 30), 18, TextAnchor.MiddleLeft, RelevoUI.Tinta);
        sub.text = "Una noche de turno en Liria";

        // La radio de Maia: una luz verde que late.
        radio = RelevoUI.Franja(r, "Radio", Centro, Centro, new Vector2(-516, 148), new Vector2(12, 12), RelevoUI.Verde);
        lema = RelevoUI.Texto(r, "Lema", Centro, Centro, new Vector2(-240, 148), new Vector2(520, 26), 15, TextAnchor.MiddleLeft, RelevoUI.Verde);

        aviso = RelevoUI.Texto(r, "Aviso", Centro, Centro, new Vector2(0, -350), new Vector2(1100, 24), 13, TextAnchor.MiddleCenter, RelevoUI.ConAlfa(RelevoUI.Tinta, 0.6f));

        velo = RelevoUI.Franja(r, "Velo", Centro, Centro, Vector2.zero, Vector2.zero, Color.black);
        Estirar(velo.rectTransform, 0f, 1f);
        velo.raycastTarget = false;
    }

    static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);

    static void Estirar(RectTransform rt, float abajo, float arriba)
    {
        rt.anchorMin = new Vector2(0f, abajo);
        rt.anchorMax = new Vector2(1f, arriba);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    /// <summary>Crea las filas del menú según haya o no partida guardada.</summary>
    void Rehacer()
    {
        foreach (Text f in filas) Destroy(f.transform.parent.gameObject);
        filas.Clear();
        marcos.Clear();
        opciones.Clear();

        if (SaveManager.HayProgreso) { opciones.Add("Continuar"); opciones.Add("Elegir puerto"); }
        opciones.Add(confirmandoNueva ? "¿Borrar el progreso? Confirmar" : "Nueva partida");
        opciones.Add("Créditos");
        opciones.Add("Salir");

        Transform r = velo.transform.parent;
        for (int i = 0; i < opciones.Count; i++)
        {
            var marco = RelevoUI.Franja(r, "Opcion", Centro, Centro, new Vector2(-370, 84 - i * 44), new Vector2(330, 38), RelevoUI.Fondo);
            var texto = RelevoUI.Texto(marco.transform, "Texto", Centro, Centro, new Vector2(10, 0), new Vector2(300, 34), 18, TextAnchor.MiddleLeft, RelevoUI.Tinta);
            texto.text = opciones[i];
            int indice = i;
            var boton = marco.gameObject.AddComponent<Button>();
            boton.transition = Selectable.Transition.None;
            boton.onClick.AddListener(() => { seleccion = indice; Elegir(); });
            var hover = marco.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var e = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            e.callback.AddListener(_ => { seleccion = indice; });
            hover.triggers.Add(e);
            marcos.Add(marco);
            filas.Add(texto);
        }
        velo.transform.SetAsLastSibling();
        seleccion = Mathf.Clamp(seleccion, 0, opciones.Count - 1);

        int recuerdos = Historia.RecuerdosEncontrados;
        aviso.text = SaveManager.HayProgreso
            ? "Puertos restaurados: " + Completados() + " de " + LevelLayouts.Cantidad + "   ·   Recuerdos: " + recuerdos + " de " + Historia.RecuerdosTotales
            : "Flechas o stick para elegir · Espacio o Enter para confirmar";
    }

    static int Completados()
    {
        int n = 0;
        for (int i = 1; i <= LevelLayouts.Cantidad; i++) if (SaveManager.NivelCompletado(i)) n++;
        return n;
    }

    // ------------------------------------------------------------------ bucle

    void Update()
    {
        t += Time.unscaledDeltaTime;

        // Entrada del negro y de la música.
        if (!saliendo) velo.color = new Color(0, 0, 0, Mathf.Clamp01(1f - t / 1.4f));
        if (musica != null && !saliendo) musica.volume = Mathf.Min(0.55f, t * 0.3f);

        for (int i = 0; i < ventanas.Count; i++)
        {
            float on = encendidoVentana[i];
            float a = on < 0f ? 0f : Mathf.Clamp01((t - on) * 3f);
            float parpadeo = 0.85f + 0.15f * Mathf.Sin(t * 1.3f + i);
            ventanas[i].color = Color.Lerp(new Color(0.03f, 0.05f, 0.08f), RelevoUI.Ambar * parpadeo, a);
        }

        float latido = 0.5f + 0.5f * Mathf.Sin(t * 2.4f);
        radio.color = RelevoUI.ConAlfa(RelevoUI.Verde, 0.35f + 0.65f * latido);
        string frase = "«Buenas noches a quienes siguen de turno»";
        int letras = Mathf.Clamp((int)((t - 1.2f) * 22f), 0, frase.Length);
        lema.text = frase.Substring(0, letras);

        for (int i = 0; i < filas.Count; i++)
        {
            bool sel = i == seleccion;
            marcos[i].color = sel ? RelevoUI.FondoSeleccion : RelevoUI.ConAlfa(RelevoUI.Fondo, 0.55f);
            filas[i].color = sel ? RelevoUI.Ambar : RelevoUI.Tinta;
        }

        if (saliendo || carta != null || Time.frameCount == cierreCarta || entrada == null) return;

        Vector2 m = entrada.LeerMovimiento();
        if (Mathf.Abs(m.y) < 0.5f) flanco = false;
        else if (!flanco)
        {
            flanco = true;
            seleccion = (seleccion + (m.y > 0 ? -1 : 1) + opciones.Count) % opciones.Count;
            confirmarSiCambia();
        }

        if (entrada.Pulsado(entrada.Confirmar) || entrada.Pulsado(entrada.Interactuar)) Elegir();
        else if (entrada.Pulsado(entrada.Cancelar) && confirmandoNueva) { confirmandoNueva = false; Rehacer(); }
    }

    void confirmarSiCambia()
    {
        if (confirmandoNueva && opciones[seleccion] != "¿Borrar el progreso? Confirmar") { confirmandoNueva = false; Rehacer(); }
    }

    void Elegir()
    {
        if (saliendo || carta != null) return;
        switch (opciones[seleccion])
        {
            case "Continuar":
                Ir(LevelManager.NombreEscenaNivel(SaveManager.PuertoParaContinuar()));
                break;
            case "Elegir puerto":
                carta = CartaDePuertos.Abrir(entrada, 0, false, p => Ir(LevelManager.NombreEscenaNivel(p)), CerrarCarta, () => Ir(EscenaApertura));
                break;
            case "Nueva partida":
                if (SaveManager.HayProgreso) { confirmandoNueva = true; Rehacer(); }
                else Ir(EscenaApertura);
                break;
            case "¿Borrar el progreso? Confirmar":
                SaveManager.ReiniciarProgreso();
                Ir(EscenaApertura);
                break;
            case "Créditos":
                Ir(Creditos.Escena);
                break;
            case "Salir":
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }

    void CerrarCarta()
    {
        cierreCarta = Time.frameCount;
        if (carta == null) return;
        Destroy(carta.gameObject);
        carta = null;
    }

    void Ir(string escena)
    {
        if (saliendo) return;
        saliendo = true;
        if (carta != null) CerrarCarta();
        StartCoroutine(Fundido(escena));
    }

    System.Collections.IEnumerator Fundido(string escena)
    {
        float a = 0f;
        float vol = musica != null ? musica.volume : 0f;
        while (a < 1f)
        {
            a += Time.unscaledDeltaTime / 0.7f;
            velo.color = new Color(0, 0, 0, a);
            if (musica != null) musica.volume = vol * (1f - a);
            yield return null;
        }
        if (entrada != null) entrada.Desactivar();
        SceneManager.LoadScene(escena);
    }
}
