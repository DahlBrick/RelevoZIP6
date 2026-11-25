using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pantalla de título (escena "Titulo", primera de Build Settings).
/// Detrás, en 3D (FondoTitulo): el muelle del Puerto 1, Nilo y la radio de Maia, y la Liria real
/// al otro lado del agua, con un barrio encendido por puerto restaurado. Delante, el menú.
/// Flujo: Nueva partida → Cinematica_Liria → Nivel 1 … Nivel 13 → Créditos → Título.
/// </summary>
public class TituloRelevo : MonoBehaviour
{
    public const string Escena = "Titulo";
    public const string EscenaApertura = "Cinematica_Liria";

    const string TextoBorrar = "¿Borrar el progreso? Confirmar";

    [SerializeField] InputActionAsset acciones;

    RelevoInput entrada;
    readonly List<string> opciones = new List<string>();
    readonly List<Fila> filas = new List<Fila>();
    Transform raizMenu;
    Image radio, velo;
    Text lema, aviso;
    CartaDePuertos carta;
    int seleccion, cierreCarta = -1;
    bool flanco, confirmandoNueva, saliendo;
    float t;
    AudioSource musica, clic;
    AudioClip sonidoMover, sonidoElegir;

    sealed class Fila
    {
        public RectTransform caja;
        public Image fondo, marca;
        public Text texto, detalle;
        public float brillo;
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        Time.timeScale = 1f;
        GameEvents.Limpiar();
        RelevoGame.Limpiar();
        acciones = KitRelevo.Acciones(acciones);
        entrada = acciones != null ? new RelevoInput(acciones) : null;

        // La cámara de la escena se apaga: el fondo 3D trae la suya.
        foreach (Camera c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
        new GameObject("Fondo del titulo").AddComponent<FondoTitulo>();

        musica = gameObject.AddComponent<AudioSource>();
        musica.clip = Resources.Load<AudioClip>("MareasDeLiria");
        musica.loop = true;
        musica.volume = 0f;
        musica.spatialBlend = 0f;
        if (musica.clip != null) musica.Play();
        clic = gameObject.AddComponent<AudioSource>();
        clic.spatialBlend = 0f;
        sonidoMover = SonidosApertura.Pitido(660f);
        sonidoElegir = SonidosApertura.Rele();

        Construir();
        Rehacer();
    }

    // ------------------------------------------------------------------ interfaz

    static readonly Vector2 ArribaIzq = new Vector2(0f, 1f);
    static readonly Vector2 AbajoIzq = new Vector2(0f, 0f);
    static readonly Vector2 AbajoDer = new Vector2(1f, 0f);

    void Construir()
    {
        var raiz = new GameObject("Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        raiz.transform.SetParent(transform, false);
        var canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var escala = raiz.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 1f;
        Transform r = raiz.transform;

        // Sombra a la izquierda para leer el menú sobre la escena: franjas que se desvanecen.
        const int pasos = 28;
        for (int i = 0; i < pasos; i++)
        {
            float k = i / (float)(pasos - 1);
            var f = RelevoUI.Franja(r, "Sombra", AbajoIzq, AbajoIzq, new Vector2(i * 30f, 0f), new Vector2(31f, 0f), new Color(0.01f, 0.025f, 0.04f, 0.9f * (1f - k * k)));
            f.rectTransform.anchorMax = new Vector2(0f, 1f);
            f.rectTransform.sizeDelta = new Vector2(31f, 0f);
        }

        // Título.
        var titulo = RelevoUI.Texto(r, "Titulo", ArribaIzq, ArribaIzq, new Vector2(118, -96), new Vector2(900, 190), 170, TextAnchor.UpperLeft, RelevoUI.Ambar);
        titulo.font = RelevoUI.FuenteTitulo();
        titulo.text = "RELEVO";
        var sombra = titulo.gameObject.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.6f);
        sombra.effectDistance = new Vector2(5f, -5f);
        var sub = RelevoUI.Texto(r, "Subtitulo", ArribaIzq, ArribaIzq, new Vector2(124, -290), new Vector2(700, 40), 32, TextAnchor.UpperLeft, RelevoUI.Tinta);
        sub.text = "Una noche de turno en Liria";
        RelevoUI.Franja(r, "Linea", ArribaIzq, ArribaIzq, new Vector2(124, -348), new Vector2(460, 3), RelevoUI.Cobre);

        raizMenu = new GameObject("Opciones", typeof(RectTransform)).transform;
        raizMenu.SetParent(r, false);
        var rm = (RectTransform)raizMenu;
        rm.anchorMin = rm.anchorMax = rm.pivot = ArribaIzq;
        rm.anchoredPosition = new Vector2(100, -400);

        // Turno de noche: una raya por puerto restaurado, como las de la baranda del muelle.
        int hechos = Completados();
        var etiqueta = RelevoUI.Texto(r, "Turno", AbajoIzq, AbajoIzq, new Vector2(124, 196), new Vector2(600, 30), 22, TextAnchor.LowerLeft, RelevoUI.ConAlfa(RelevoUI.Tinta, 0.7f));
        etiqueta.text = "PUERTOS RESTAURADOS  " + hechos + " / " + LevelLayouts.Cantidad;
        for (int i = 0; i < LevelLayouts.Cantidad; i++)
        {
            bool hecho = i < hechos;
            RelevoUI.Franja(r, "Raya", AbajoIzq, AbajoIzq, new Vector2(126 + i * 20 + (i / 5) * 8, 150), new Vector2(7, 32),
                hecho ? RelevoUI.Ambar : RelevoUI.ConAlfa(RelevoUI.Tinta, 0.18f));
        }

        // La radio de Maia: una luz verde que late y su saludo.
        radio = RelevoUI.Franja(r, "Radio", AbajoIzq, AbajoIzq, new Vector2(126, 86), new Vector2(16, 16), RelevoUI.Verde);
        lema = RelevoUI.Texto(r, "Lema", AbajoIzq, AbajoIzq, new Vector2(156, 76), new Vector2(900, 36), 26, TextAnchor.MiddleLeft, RelevoUI.Verde);

        aviso = RelevoUI.Texto(r, "Aviso", AbajoDer, AbajoDer, new Vector2(-70, 60), new Vector2(900, 30), 22, TextAnchor.LowerRight, RelevoUI.ConAlfa(RelevoUI.Tinta, 0.65f));
        aviso.text = "Flechas o stick: elegir   ·   Enter o Espacio: confirmar   ·   Ratón: clic";

        velo = RelevoUI.Franja(r, "Velo", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Color.black);
        velo.rectTransform.anchorMin = Vector2.zero;
        velo.rectTransform.anchorMax = Vector2.one;
        velo.rectTransform.offsetMin = velo.rectTransform.offsetMax = Vector2.zero;
        velo.raycastTarget = false;

        // Sin EventSystem el ratón no llega a los botones.
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }

    /// <summary>Crea las filas del menú según haya o no partida guardada.</summary>
    void Rehacer()
    {
        foreach (Fila f in filas) Destroy(f.caja.gameObject);
        filas.Clear();
        opciones.Clear();

        if (SaveManager.HayProgreso) { opciones.Add("Continuar"); opciones.Add("Elegir puerto"); }
        opciones.Add(confirmandoNueva ? TextoBorrar : "Nueva partida");
        opciones.Add("Créditos");
        opciones.Add("Salir");

        for (int i = 0; i < opciones.Count; i++)
        {
            var fila = new Fila();
            fila.fondo = RelevoUI.Franja(raizMenu, "Opcion", ArribaIzq, ArribaIzq, new Vector2(0, -i * 84), new Vector2(560, 70), Color.clear);
            fila.fondo.raycastTarget = true;
            fila.caja = fila.fondo.rectTransform;
            fila.marca = RelevoUI.Franja(fila.caja, "Marca", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18, 0), new Vector2(6, 44), RelevoUI.Ambar);
            fila.texto = RelevoUI.Texto(fila.caja, "Texto", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(44, 6), new Vector2(520, 50), 40, TextAnchor.MiddleLeft, RelevoUI.Tinta);
            fila.texto.text = opciones[i];
            fila.detalle = RelevoUI.Texto(fila.caja, "Detalle", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(46, -24), new Vector2(520, 24), 19, TextAnchor.MiddleLeft, RelevoUI.ConAlfa(RelevoUI.Tinta, 0.6f));
            fila.detalle.text = Detalle(opciones[i]);

            int indice = i;
            var boton = fila.fondo.gameObject.AddComponent<Button>();
            boton.transition = Selectable.Transition.None;
            boton.onClick.AddListener(() => { seleccion = indice; Elegir(); });
            var hover = fila.fondo.gameObject.AddComponent<EventTrigger>();
            var e = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            e.callback.AddListener(_ => Seleccionar(indice));
            hover.triggers.Add(e);
            filas.Add(fila);
        }
        velo.transform.SetAsLastSibling();
        seleccion = Mathf.Clamp(seleccion, 0, opciones.Count - 1);
    }

    static string Detalle(string opcion)
    {
        switch (opcion)
        {
            case "Continuar": return "Puerto " + SaveManager.PuertoParaContinuar() + " · Recuerdos " + Historia.RecuerdosEncontrados + " de " + Historia.RecuerdosTotales;
            case "Elegir puerto": return "Vuelve a cualquier puerto ya abierto";
            case "Nueva partida": return SaveManager.HayProgreso ? "Empieza el turno desde el principio" : "Empieza el turno";
            case TextoBorrar: return "Se perderá la partida guardada · Esc para volver";
            default: return "";
        }
    }

    static int Completados()
    {
        int n = 0;
        for (int i = 1; i <= LevelLayouts.Cantidad; i++) if (SaveManager.NivelCompletado(i)) n++;
        return n;
    }

    void Seleccionar(int i)
    {
        if (i == seleccion || saliendo) return;
        seleccion = i;
        if (sonidoMover != null) clic.PlayOneShot(sonidoMover, 0.12f);
        ConfirmarSiCambia();
    }

    // ------------------------------------------------------------------ bucle

    void Update()
    {
        t += Time.unscaledDeltaTime;
        float dt = Time.unscaledDeltaTime;

        if (!saliendo) velo.color = new Color(0, 0, 0, Mathf.Clamp01(1f - t / 1.2f));
        if (musica != null && !saliendo) musica.volume = Mathf.Min(0.5f, t * 0.3f);

        float latido = 0.5f + 0.5f * Mathf.Sin(t * 2.4f);
        radio.color = RelevoUI.ConAlfa(RelevoUI.Verde, 0.35f + 0.65f * latido);
        const string frase = "«Buenas noches a quienes siguen de turno»";
        int letras = Mathf.Clamp((int)((t - 1.4f) * 22f), 0, frase.Length);
        lema.text = frase.Substring(0, letras);

        for (int i = 0; i < filas.Count; i++)
        {
            Fila f = filas[i];
            bool sel = i == seleccion;
            f.brillo = Mathf.MoveTowards(f.brillo, sel ? 1f : 0f, dt * 7f);
            float b = f.brillo;
            // Entrada escalonada de las filas al abrir el título.
            float llegada = Mathf.Clamp01((t - 0.6f - i * 0.08f) * 3f);
            f.caja.anchoredPosition = new Vector2(-40f * (1f - llegada) + 14f * b, -i * 84);
            f.fondo.color = new Color(RelevoUI.FondoSeleccion.r, RelevoUI.FondoSeleccion.g, RelevoUI.FondoSeleccion.b, 0.55f * b * llegada);
            f.marca.color = RelevoUI.ConAlfa(RelevoUI.Ambar, b * llegada);
            bool peligro = opciones[i] == TextoBorrar;
            Color tinta = Color.Lerp(RelevoUI.ConAlfa(RelevoUI.Tinta, 0.8f), peligro ? RelevoUI.Coral : RelevoUI.Ambar, b);
            f.texto.color = RelevoUI.ConAlfa(tinta, tinta.a * llegada);
            f.detalle.color = RelevoUI.ConAlfa(RelevoUI.Tinta, (0.25f + 0.45f * b) * llegada);
        }

        if (saliendo || carta != null || Time.frameCount == cierreCarta || entrada == null) return;

        Vector2 m = entrada.LeerMovimiento();
        if (Mathf.Abs(m.y) < 0.5f) flanco = false;
        else if (!flanco)
        {
            flanco = true;
            Seleccionar((seleccion + (m.y > 0 ? -1 : 1) + opciones.Count) % opciones.Count);
        }

        Keyboard k = Keyboard.current;
        bool enter = k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame);
        if (enter || entrada.Pulsado(entrada.Confirmar) || entrada.Pulsado(entrada.Interactuar)) Elegir();
        else if (entrada.Pulsado(entrada.Cancelar) && confirmandoNueva) { confirmandoNueva = false; Rehacer(); }
    }

    void ConfirmarSiCambia()
    {
        if (confirmandoNueva && opciones[seleccion] != TextoBorrar) { confirmandoNueva = false; Rehacer(); }
    }

    void Elegir()
    {
        if (saliendo || carta != null) return;
        if (sonidoElegir != null) clic.PlayOneShot(sonidoElegir, 0.5f);
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
            case TextoBorrar:
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
