using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cuaderno de viaje: la libreta física de Nilo, abierta ENCIMA del nivel.
///
/// Presentación: el mismo pliego de dos páginas que la pestaña CUADERNO del menú de exploración
/// —papel envejecido, lomo cosido, pliegue central, manchas de agua, recortes pegados con celo,
/// sellos y anotaciones—, con pestañas físicas en el canto derecho, una por recuerdo.
///
/// Contenido: SOLO la colección de recuerdos. Aquí no hay inventario, ni skills, ni equipamiento,
/// ni mapa jugable: cada página es un recuerdo y nada más. Los recuerdos se leen del guardado
/// (SaveManager y LevelDefinition); lo único que este panel escribe es la marca de «leído» al abrir
/// una página encontrada, que es justo su trabajo.
///
/// Dos vistas sobre el mismo pliego:
///   · colección → el índice de entradas a la izquierda y la ficha de la elegida a la derecha;
///   · recuerdo abierto → ilustración, frase, texto, origen y sello a doble página.
/// Las flechas pasan páginas (con giro de hoja y sonido de papel); Enter abre el recuerdo y Esc o
/// Tab vuelven a la colección y, desde ella, cierran y devuelven al nivel.
///
/// Se abre en aditivo: el nivel sigue cargado debajo, congelado, y al cerrar se descarga solo esta
/// escena, de modo que Nilo, la batería, los puentes y el progreso quedan exactamente como estaban.
/// </summary>
public class CuadernoUI : MonoBehaviour
{
    [Tooltip("Assets/Input/Relevo.inputactions")]
    [SerializeField] InputActionAsset acciones;

    /// <summary>Niveles con recuerdo propio. Coincide con LevelLayouts.Obtener().</summary>
    const int Niveles = 3;

    // ------------------------------------------------------------------ medidas del pliego

    const float AnchoHoja = 1420f;
    const float AltoHoja = 800f;
    const float AnchoPagina = 560f;
    const float ColIzq = -310f;
    const float ColDer = 310f;
    // Bloque de papel: deja ver el cuero de la tapa por los cuatro cantos del libro.
    const float AnchoPapel = 1216f;
    const float AltoPapel = 740f;
    static readonly Vector2 CentroPapel = new Vector2(0f, 8f);

    // Paleta del cuaderno: papel cálido, tinta marrón, sello rojo. Nada de azul tecnológico.
    static readonly Color Papel = new Color(0.898f, 0.843f, 0.714f, 1f);
    static readonly Color PapelClaro = new Color(0.945f, 0.904f, 0.796f, 1f);
    static readonly Color PapelSombra = new Color(0.827f, 0.757f, 0.616f, 1f);
    static readonly Color PapelHondo = new Color(0.690f, 0.612f, 0.478f, 1f);
    static readonly Color Tinta = new Color(0.153f, 0.122f, 0.094f, 1f);
    static readonly Color TintaSuave = new Color(0.361f, 0.306f, 0.235f, 1f);
    static readonly Color Sello = new Color(0.612f, 0.196f, 0.157f, 1f);
    static readonly Color Gris = new Color(0.478f, 0.463f, 0.427f, 1f);
    static readonly Color Esmalte = new Color(0.129f, 0.263f, 0.271f, 1f);

    // Cuero de la tapa: marrón rojizo envejecido, con su luz alta y su hondura.
    static readonly Color Cuero = new Color(0.376f, 0.176f, 0.110f, 1f);
    static readonly Color CueroLuz = new Color(0.514f, 0.263f, 0.161f, 1f);
    static readonly Color CueroHondo = new Color(0.235f, 0.098f, 0.055f, 1f);
    static readonly Color CueroBorde = new Color(0.157f, 0.063f, 0.035f, 1f);
    static readonly Color Madera = new Color(0.827f, 0.639f, 0.353f, 1f);
    static readonly Color Grafito = new Color(0.180f, 0.184f, 0.196f, 1f);

    /// <summary>Entrada del índice: el recorte de un recuerdo en la página izquierda.</summary>
    class Entrada
    {
        public GameObject raiz;
        public Image fondo;
        public Image[] filetes;
        public Image anillo;
        public Text numero;
        public Image chip;
        public Text chipTexto;
        public Text titulo;
        public Text lugar;
        public Text pista;
        public Image cinta;
        public GameObject roto;      // señales de página incompleta (solo en recuerdos bloqueados)
    }

    RelevoInput entrada;

    RectTransform hoja;
    RectTransform paginas;
    GameObject vistaColeccion;
    GameObject vistaRecuerdo;

    Text cabPie;
    Text indicadorHoja;
    Text pieTexto;
    Text recuento;

    readonly Entrada[] entradas = new Entrada[Niveles];
    readonly Image[] pestanas = new Image[Niveles];
    readonly Text[] pestanasTexto = new Text[Niveles];
    readonly Image[] pestanasFilo = new Image[Niveles];

    // --- ficha de la derecha, en la vista de colección
    Image colIlustracion;
    Image colAnillo;
    Text colNumero;
    Text colCaption;
    Image colSello;
    Text colSelloTexto;
    List<Image> colSelloBordes;
    Text colTitulo;
    Text colSubtitulo;
    Text colNota;
    Text colTexto;
    Image colCta;
    Text colCtaTexto;

    // --- recuerdo abierto
    Image detIlustracion;
    Image detAnillo;
    Text detNumero;
    Text detCaption;
    Text detTitulo;
    Text detOrigen;
    Image detChip;
    Text detChipTexto;
    Text detPista;
    Text detLugar;
    Text detReferencia;
    Image detSello;
    Text detSelloTexto;
    List<Image> detSelloBordes;
    Text detFrase;
    Text detNarrativa;
    Image detCta;
    Text detCtaTexto;

    // señales de página incompleta en el recuerdo abierto
    GameObject detFotoVelada;
    GameObject detCosido;
    GameObject detFragmentos;
    GameObject detArrancado;
    Text detAviso;

    // --- hoja que gira y sonido de papel
    Image hojaGiro;
    Image hojaPliegue;
    Image hojaBrillo;
    Image sombraGiro;
    AudioSource fuente;
    AudioClip clipHoja;
    Coroutine girando;

    int seleccion;
    bool abierto;
    bool flanco;

    /// <summary>
    /// Hay un cuaderno delante del nivel. UIManager lo consulta para dejar de leer la entrada y
    /// para saber que el nivel sigue bloqueado aunque su propio menú esté cerrado.
    /// </summary>
    public static bool Abierto { get; private set; }

    void Awake()
    {
        Abierto = true;

        // El cuaderno se abre ENCIMA del nivel, que se queda cargado y congelado: no se devuelve el
        // tiempo (hacerlo reanudaría el nivel que hay detrás). Toda esta interfaz anima con tiempo
        // sin escalar, así que responde igual con timeScale = 0.
        Time.timeScale = 0f;

        entrada = new RelevoInput(acciones);
        Construir();
        Refrescar();
        RelevoUIAnim.Entrada(hoja.gameObject, 0.26f, -26f, true);
        SonarHoja();
    }

    void OnDestroy()
    {
        Abierto = false;

        // NO se desactiva el asset de entrada: RelevoInput.Activar/Desactivar actúan sobre los mapas
        // del ASSET COMPARTIDO con el nivel, así que desactivarlos al cerrar el cuaderno dejaba a
        // UIManager sin entrada para siempre (ni Tab ni Esc volvían a responder). Al destruirse este
        // objeto su Update deja de correr solo: no hay nada que apagar.
    }

    // ------------------------------------------------------------------ entrada

    void Update()
    {
        if (entrada == null) return;

        // Esc, Tab y pausa: cierran desde la colección y vuelven a ella desde un recuerdo abierto.
        bool atras = entrada.Pulsado(entrada.Cancelar)
            || entrada.Pulsado(entrada.Pausa)
            || entrada.Pulsado(entrada.Cuaderno);

        bool confirmar = entrada.Pulsado(entrada.Confirmar) || entrada.Pulsado(entrada.Interactuar);

        if (abierto)
        {
            if (atras || confirmar)
            {
                abierto = false;
                Refrescar();
                SonarHoja();
                return;
            }

            int paso = LeerPaso();
            if (paso != 0) CambiarRecuerdo(paso);
            return;
        }

        if (atras)
        {
            Volver();
            return;
        }

        if (confirmar)
        {
            AbrirRecuerdo();
            return;
        }

        int mover = LeerPaso();
        if (mover != 0) CambiarRecuerdo(mover);
    }

    /// <summary>Devuelve -1, 0 o 1 por flanco: una pulsación = una página, sin repetición.</summary>
    int LeerPaso()
    {
        Vector2 mover = entrada.LeerMovimiento();

        // El eje Y del input crece hacia arriba: se invierte para que ↑ vaya al recuerdo anterior.
        float eje = Mathf.Abs(mover.x) >= Mathf.Abs(mover.y) ? mover.x : -mover.y;

        if (Mathf.Abs(eje) < 0.5f)
        {
            flanco = false;
            return 0;
        }

        if (flanco) return 0;
        flanco = true;

        return eje > 0f ? 1 : -1;
    }

    void CambiarRecuerdo(int paso)
    {
        seleccion += paso;
        if (seleccion < 0) seleccion = Niveles - 1;
        if (seleccion >= Niveles) seleccion = 0;

        Refrescar();
        PasarHoja();
        SonarHoja();
    }

    void AbrirRecuerdo()
    {
        abierto = true;

        // La página que el jugador tiene delante es la que se da por leída. Es lo ÚNICO que este
        // panel escribe en el guardado, igual que antes de esta fase.
        LevelDefinition definicion = DefinicionesPorDefecto.Obtener(seleccion + 1);
        if (definicion != null
            && SaveManager.TieneRecuerdo(definicion.recuerdoIdentificador)
            && !SaveManager.RecuerdoLeido(definicion.recuerdoIdentificador))
        {
            SaveManager.MarcarRecuerdoLeido(definicion.recuerdoIdentificador);
            SaveManager.Guardar();
        }

        Refrescar();
        PasarHoja();
        SonarHoja();
    }

    void Volver()
    {
        // Sin Desactivar(): los mapas de entrada son los mismos que usa el nivel (ver OnDestroy).
        Abierto = false;

        // Cierre aditivo: se descarga solo esta escena y el nivel de detrás sigue tal cual estaba,
        // sin recargarse. Nada de GameEvents.Limpiar(): el nivel no se reconstruye.
        if (SceneManager.sceneCount > 1)
        {
            SceneManager.UnloadSceneAsync(gameObject.scene);
            return;
        }

        // Modo edición: el cuaderno se jugó solo, sin nivel detrás. Se vuelve por carga normal.
        Time.timeScale = 1f;

        string destino = string.IsNullOrEmpty(LevelManager.EscenaOrigen)
            ? LevelManager.NombreEscenaNivel(1)
            : LevelManager.EscenaOrigen;

        if (Application.CanStreamedLevelBeLoaded(destino)) SceneManager.LoadScene(destino);
    }

    // ------------------------------------------------------------------ construcción

    void Construir()
    {
        GameObject lienzo = new GameObject("Lienzo");
        lienzo.transform.SetParent(transform, false);

        Canvas canvas = lienzo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Por encima del lienzo del HUD (UIManager usa 10): con el mismo orden el HUD del nivel
        // gana el desempate y se dibuja sobre el velo del cuaderno.
        canvas.sortingOrder = 30;

        CanvasScaler escalador = lienzo.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        lienzo.AddComponent<GraphicRaycaster>();

        Transform raiz = lienzo.transform;

        // El cuaderno es una pantalla propia: la libreta tapa el diorama que quedó detrás.
        RelevoUI.Panel(raiz, "Fondo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(4000f, 4000f), RelevoUI.ConAlfa(RelevoUI.FondoHondo, 0.97f));

        // Sombra del libro sobre la mesa: dos capas, una ancha y difusa y otra pegada al canto.
        RelevoUI.Franja(raiz, "Sombra libro", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(16f, -24f), new Vector2(AnchoHoja + 58f, AltoHoja + 48f), new Color(0f, 0f, 0f, 0.30f));
        RelevoUI.Franja(raiz, "Sombra libro 2", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(4f, -10f), new Vector2(AnchoHoja + 14f, AltoHoja + 10f), new Color(0f, 0f, 0f, 0.32f));

        // La tapa: cuero redondeado. Todo lo demás —papel, canto, pestañas, cinta, lápiz— va encima.
        hoja = RelevoUI.Panel(raiz, "Libro", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(AnchoHoja, AltoHoja), Cuero).GetComponent<RectTransform>();

        Papeleria();

        Text cabTitulo = RelevoUI.Texto(hoja, "Cabecera titulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(700f, 40f), 32, TextAnchor.MiddleCenter, Tinta);
        cabTitulo.font = RelevoUI.FuenteTitulo();
        cabTitulo.text = "CUADERNO DE VIAJE";

        cabPie = RelevoUI.Texto(hoja, "Cabecera pie", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -72f), new Vector2(700f, 24f), 15, TextAnchor.MiddleCenter, TintaSuave);

        indicadorHoja = RelevoUI.Texto(hoja, "Indicador hoja", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-110f, -26f), new Vector2(200f, 24f), 14, TextAnchor.MiddleRight,
            RelevoUI.ConAlfa(TintaSuave, 0.85f));

        paginas = RelevoUI.Franja(hoja, "Paginas", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -60f), new Vector2(1180f, 520f), new Color(0f, 0f, 0f, 0f)).rectTransform;

        vistaColeccion = NuevaVista("Vista coleccion");
        vistaRecuerdo = NuevaVista("Vista recuerdo");

        ConstruirColeccion(vistaColeccion.transform);
        ConstruirRecuerdo(vistaRecuerdo.transform);

        Pestanas();
        Pie();

        // Sombra que proyecta la hoja al girar: por encima del pliego pero por debajo de la hoja.
        sombraGiro = RelevoUI.Franja(hoja, "Sombra del giro", new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, -60f), new Vector2(592f, 520f), RelevoUI.ConAlfa(Tinta, 0f));

        // La hoja que gira, creada la última para quedar por encima de todo el pliego. El pivote va
        // en el lomo, así que gira como una página de verdad; lleva dentro su pliegue y su brillo
        // para que el giro tenga volumen y no sea una cortinilla plana.
        hojaGiro = RelevoUI.Franja(hoja, "Hoja que gira", new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, -60f), new Vector2(592f, 520f), PapelClaro);

        hojaPliegue = RelevoUI.Franja(hojaGiro.transform, "Pliegue hoja", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(48f, 520f), RelevoUI.ConAlfa(PapelHondo, 0f));

        hojaBrillo = RelevoUI.Franja(hojaGiro.transform, "Brillo hoja", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-6f, 0f), new Vector2(30f, 520f), RelevoUI.ConAlfa(PapelClaro, 0f));

        hojaGiro.gameObject.SetActive(false);

        PrepararAudio();
    }

    /// <summary>
    /// El libro físico: tapa de cuero con su canto, su costura y su desgaste; dentro, el bloque de
    /// papel con las hojas apiladas asomando por el borde, el canto rasgado y la canaleta del lomo
    /// abierto. Es papelería: aquí no hay ni un dato del recuerdo.
    /// </summary>
    void Papeleria()
    {
        // ---------------- la tapa de cuero
        RelevoUI.Marco(hoja, CueroBorde, 4f);
        MarcoInset(hoja, 10f, RelevoUI.ConAlfa(CueroLuz, 0.50f), 2f);
        MarcoInset(hoja, 18f, RelevoUI.ConAlfa(CueroHondo, 0.80f), 2f);

        Mancha(hoja, "Cuero gastado A", new Vector2(-650f, 236f), new Vector2(86f, 260f), RelevoUI.ConAlfa(CueroHondo, 0.55f), 4f);
        Mancha(hoja, "Cuero gastado B", new Vector2(652f, -150f), new Vector2(78f, 290f), RelevoUI.ConAlfa(CueroHondo, 0.50f), -6f);
        Mancha(hoja, "Cuero gastado C", new Vector2(-250f, 390f), new Vector2(420f, 16f), RelevoUI.ConAlfa(CueroLuz, 0.45f), 0f);
        Mancha(hoja, "Cuero gastado D", new Vector2(210f, -388f), new Vector2(380f, 14f), RelevoUI.ConAlfa(CueroHondo, 0.55f), 0f);

        CosturaCuero();

        // ---------------- el canto del libro: las hojas apiladas asoman bajo el papel
        for (int i = 3; i >= 1; i--)
        {
            float crece = i * 7f;

            RelevoUI.Franja(hoja, "Canto " + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                CentroPapel + new Vector2(0f, -1f),
                new Vector2(AnchoPapel + crece, AltoPapel + crece * 0.55f),
                Color.Lerp(PapelSombra, PapelHondo, i / 4.5f));
        }

        // ---------------- la hoja abierta
        RelevoUI.Franja(hoja, "Papel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            CentroPapel, new Vector2(AnchoPapel, AltoPapel), Papel);

        // Canto rasgado: el papel cortado a mano, con sus dientes irregulares por los lados y abajo.
        float papelAbajo = CentroPapel.y - AltoPapel * 0.5f;
        float papelArriba = CentroPapel.y + AltoPapel * 0.5f;

        Dientes(hoja, new Vector2(-AnchoPapel * 0.5f, papelAbajo), new Vector2(-AnchoPapel * 0.5f, papelArriba),
            14f, RelevoUI.ConAlfa(PapelClaro, 0.95f), 26);
        Dientes(hoja, new Vector2(AnchoPapel * 0.5f, papelAbajo), new Vector2(AnchoPapel * 0.5f, papelArriba),
            14f, RelevoUI.ConAlfa(PapelClaro, 0.95f), 26);
        Dientes(hoja, new Vector2(-AnchoPapel * 0.5f, papelAbajo), new Vector2(AnchoPapel * 0.5f, papelAbajo),
            13f, RelevoUI.ConAlfa(PapelClaro, 0.90f), 34);

        // Sombra interior: el papel se oscurece hacia el canto, como en un libro abierto de verdad.
        for (int i = 3; i >= 1; i--)
        {
            float ancho = i * 22f;
            float alfa = 0.05f + (3 - i) * 0.03f;

            RelevoUI.Franja(hoja, "Sombra canto izq " + i, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(-AnchoPapel * 0.5f, CentroPapel.y), new Vector2(ancho, AltoPapel),
                RelevoUI.ConAlfa(PapelHondo, alfa));

            RelevoUI.Franja(hoja, "Sombra canto der " + i, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(AnchoPapel * 0.5f, CentroPapel.y), new Vector2(ancho, AltoPapel),
                RelevoUI.ConAlfa(PapelHondo, alfa));
        }

        // Canaleta: el pliegue central del lomo abierto, con la luz del papel a su izquierda.
        for (int i = 4; i >= 1; i--)
        {
            RelevoUI.Franja(hoja, "Canaleta " + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                CentroPapel, new Vector2(10f + i * 9f, AltoPapel - 6f),
                RelevoUI.ConAlfa(PapelHondo, 0.10f + (4 - i) * 0.07f));
        }

        RelevoUI.Franja(hoja, "Canaleta luz", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            CentroPapel + new Vector2(-16f, 0f), new Vector2(9f, AltoPapel - 24f),
            RelevoUI.ConAlfa(PapelClaro, 0.55f));

        // Manchas de agua y de tinta sobre el papel.
        Mancha(hoja, "Mancha A", new Vector2(-470f, 300f), new Vector2(250f, 116f), RelevoUI.ConAlfa(PapelHondo, 0.22f), 12f);
        Mancha(hoja, "Mancha B", new Vector2(500f, -300f), new Vector2(226f, 106f), RelevoUI.ConAlfa(PapelHondo, 0.18f), -9f);
        Mancha(hoja, "Mancha C", new Vector2(430f, 320f), new Vector2(168f, 78f), RelevoUI.ConAlfa(PapelHondo, 0.14f), 20f);
        Mancha(hoja, "Mancha D", new Vector2(-556f, -300f), new Vector2(176f, 68f), RelevoUI.ConAlfa(PapelHondo, 0.16f), -16f);

        // Sello de mantenimiento, a la izquierda del título.
        Image sello = RelevoUI.Franja(hoja, "Sello radio", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-474f, 340f), new Vector2(184f, 38f), new Color(0f, 0f, 0f, 0f));
        sello.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -6f);
        RelevoUI.Marco(sello.transform, RelevoUI.ConAlfa(Sello, 0.55f), 2f);

        Text selloTexto = RelevoUI.Texto(sello.transform, "Sello texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(184f, 38f), 13, TextAnchor.MiddleCenter, RelevoUI.ConAlfa(Sello, 0.72f));
        selloTexto.text = "RECEPTOR DE NILO";

        // Papel doblado que asoma por el canto inferior: algo que Nilo dejó dentro del cuaderno.
        Image recorte = RelevoUI.Franja(hoja, "Recorte", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(416f, -396f), new Vector2(212f, 66f), RelevoUI.ConAlfa(PapelClaro, 0.95f));
        recorte.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -3f);
        RelevoUI.Marco(recorte.transform, RelevoUI.ConAlfa(PapelHondo, 0.85f), 1f);

        Text nota = RelevoUI.Texto(recorte.transform, "Texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(190f, 52f), 12, TextAnchor.MiddleCenter, TintaSuave);
        nota.text = "«Si la luz se apaga,\nesperen.» — la barquera";

        // Celo sobre el canto: el papel está pegado a la tapa.
        Image celo = RelevoUI.Franja(hoja, "Celo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(452f, -362f), new Vector2(132f, 22f), RelevoUI.ConAlfa(PapelClaro, 0.28f));
        celo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -9f);

        // Cinta de marcapáginas: roja, colgando del canto inferior, con su punta más estrecha.
        Image cinta = RelevoUI.Franja(hoja, "Marcapaginas", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f),
            new Vector2(-248f, -326f), new Vector2(30f, 168f), Sello);
        cinta.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 1.4f);

        RelevoUI.Franja(cinta.transform, "Punta", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(18f, 34f), RelevoUI.ConAlfa(Sello, 0.92f));

        // Lápiz apoyado en la mesa, por delante del canto inferior del libro.
        Linea(hoja, "Lapiz", new Vector2(-560f, -368f), new Vector2(60f, -404f), 15f, Madera);
        Linea(hoja, "Lapiz goma", new Vector2(-560f, -368f), new Vector2(-600f, -365f), 15f,
            RelevoUI.ConAlfa(Sello, 0.85f));
        Linea(hoja, "Lapiz punta", new Vector2(60f, -404f), new Vector2(106f, -408f), 9f,
            RelevoUI.ConAlfa(Grafito, 0.92f));
    }

    /// <summary>Costura del cuero: puntadas cortas repartidas por el marco de la tapa.</summary>
    void CosturaCuero()
    {
        for (int i = 0; i < 18; i++)
        {
            float y = -330f + i * 38f;

            Puntada(new Vector2(-648f, y), 0f);
            Puntada(new Vector2(648f, y), 0f);
        }

        for (int i = 0; i < 15; i++)
        {
            float x = -560f + i * 80f;

            Puntada(new Vector2(x, 388f), 90f);
            Puntada(new Vector2(x, -386f), 90f);
        }
    }

    void Puntada(Vector2 posicion, float giro)
    {
        Image hilo = RelevoUI.Franja(hoja, "Puntada", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            posicion, new Vector2(11f, 3f), RelevoUI.ConAlfa(CueroLuz, 0.55f));
        hilo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, giro);
    }

    /// <summary>
    /// Pestañas físicas del canto izquierdo, como en un libro de verdad: una por recuerdo, en número
    /// romano, sobresaliendo del cuero. La de la página abierta se alza y se separa.
    /// </summary>
    void Pestanas()
    {
        for (int i = 0; i < Niveles; i++)
        {
            pestanas[i] = RelevoUI.Panel(hoja, "Pestana " + (i + 1), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(4f, 130f - i * 120f), new Vector2(88f, 74f), PapelClaro).GetComponent<Image>();
            pestanas[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 1.6f);

            pestanasTexto[i] = RelevoUI.Texto(hoja, "Pestana texto " + (i + 1), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(4f, 130f - i * 120f), new Vector2(88f, 74f), 22, TextAnchor.MiddleCenter, TintaSuave);
            pestanasTexto[i].font = RelevoUI.FuenteTitulo();
            pestanasTexto[i].text = Romano(i + 1);

            pestanasFilo[i] = RelevoUI.Franja(hoja, "Pestana filo " + (i + 1), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(-80f, 130f - i * 120f), new Vector2(5f, 52f), ColorDeNivel(i + 1));
        }
    }

    /// <summary>Número romano de la pestaña: como los índices de una libreta encuadernada a mano.</summary>
    static string Romano(int numero)
    {
        switch (numero)
        {
            case 1: return "I";
            case 2: return "II";
            case 3: return "III";
            default: return numero.ToString();
        }
    }

    void Pie()
    {
        RelevoUI.Franja(hoja, "Filete pie", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 78f), new Vector2(AnchoPapel - 120f, 2f), RelevoUI.ConAlfa(PapelHondo, 0.9f));

        pieTexto = RelevoUI.Texto(hoja, "Pie", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 50f), new Vector2(AnchoPapel - 140f, 24f), 14, TextAnchor.MiddleCenter,
            RelevoUI.ConAlfa(TintaSuave, 0.95f));
    }

    GameObject NuevaVista(string nombre)
    {
        GameObject v = new GameObject(nombre);
        v.transform.SetParent(paginas, false);

        RectTransform rt = v.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        v.SetActive(false);
        return v;
    }

    // ------------------------------------------------------------------ vista: colección

    void ConstruirColeccion(Transform p)
    {
        Text cabecera = RelevoUI.Texto(p, "Cabecera izq", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, 232f), new Vector2(AnchoPagina, 24f), 14, TextAnchor.MiddleLeft, TintaSuave);
        cabecera.text = "ENTRADAS · TRES RECUERDOS";

        recuento = RelevoUI.Texto(p, "Recuento", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, 232f), new Vector2(AnchoPagina, 24f), 14, TextAnchor.MiddleRight, Sello);

        for (int i = 0; i < Niveles; i++)
        {
            entradas[i] = CrearEntrada(p, i, 140f - i * 150f);
        }

        // ---------------- página derecha: la ficha de la entrada elegida
        colIlustracion = RelevoUI.Franja(p, "Ilustracion", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 175f), new Vector2(AnchoPagina, 150f), RelevoUI.ConAlfa(PapelSombra, 0.75f));
        RelevoUI.Marco(colIlustracion.transform, PapelHondo, 2f);
        RelevoUI.Esquinas(colIlustracion.transform, 16f, 2f, PapelHondo);

        Mancha(colIlustracion.transform, "Mancha ilus A", new Vector2(150f, -30f), new Vector2(180f, 90f),
            RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.18f), 14f);
        Mancha(colIlustracion.transform, "Mancha ilus B", new Vector2(-160f, 40f), new Vector2(120f, 60f),
            RelevoUI.ConAlfa(RelevoUI.Ambar, 0.12f), -8f);

        colAnillo = RelevoUI.Hexagono(colIlustracion.transform, "Anillo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-186f, 4f), new Vector2(104f, 104f), RelevoUI.Cobre, false);

        colNumero = RelevoUI.Texto(colIlustracion.transform, "Numero", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-186f, 4f), new Vector2(104f, 104f), 34, TextAnchor.MiddleCenter, RelevoUI.Cobre);
        colNumero.font = RelevoUI.FuenteTitulo();

        Linea(colIlustracion.transform, "Cable", new Vector2(-120f, 4f), new Vector2(236f, 4f), 2f,
            RelevoUI.ConAlfa(PapelHondo, 0.9f));

        Image clip = RelevoUI.Franja(colIlustracion.transform, "Clip", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(30f, -12f), new Vector2(96f, 16f), RelevoUI.ConAlfa(Tinta, 0.12f));
        clip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -12f);

        colCaption = RelevoUI.Texto(colIlustracion.transform, "Caption", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(14f, 8f), new Vector2(240f, 20f), 13, TextAnchor.LowerLeft, TintaSuave);

        colTitulo = RelevoUI.Texto(p, "Col titulo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 62f), new Vector2(AnchoPagina, 34f), 24, TextAnchor.MiddleLeft, Tinta);
        colTitulo.font = RelevoUI.FuenteTitulo();
        colTitulo.fontStyle = FontStyle.Bold;

        colSubtitulo = RelevoUI.Texto(p, "Col subtitulo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 34f), new Vector2(AnchoPagina, 22f), 14, TextAnchor.MiddleLeft, Sello);

        RelevoUI.Franja(p, "Filete ficha", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 18f), new Vector2(AnchoPagina, 1f), RelevoUI.ConAlfa(PapelHondo, 0.8f));

        colNota = RelevoUI.Texto(p, "Col nota", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -12f), new Vector2(AnchoPagina, 56f), 17, TextAnchor.MiddleLeft, Tinta);

        colTexto = RelevoUI.Texto(p, "Col texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -96f), new Vector2(AnchoPagina, 104f), 15, TextAnchor.UpperLeft, TintaSuave);

        colSello = RelevoUI.Franja(p, "Col sello", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer + 150f, 127f), new Vector2(230f, 54f), new Color(0f, 0f, 0f, 0f));
        colSello.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -7f);
        colSelloBordes = new List<Image>();
        RelevoUI.Marco(colSello.transform, RelevoUI.ConAlfa(Sello, 0.8f), 3f, colSelloBordes);

        colSelloTexto = RelevoUI.Texto(colSello.transform, "Col sello texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(230f, 54f), 20, TextAnchor.MiddleCenter, RelevoUI.ConAlfa(Sello, 0.9f));
        colSelloTexto.font = RelevoUI.FuenteTitulo();

        colCta = RelevoUI.Capsula(p, "Col cta", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -226f), new Vector2(AnchoPagina, 42f), RelevoUI.ConAlfa(Sello, 0.9f));

        colCtaTexto = RelevoUI.Texto(p, "Col cta texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -226f), new Vector2(AnchoPagina, 42f), 17, TextAnchor.MiddleCenter, PapelClaro);
        colCtaTexto.fontStyle = FontStyle.Bold;
    }

    Entrada CrearEntrada(Transform padre, int indice, float y)
    {
        Entrada e = new Entrada();

        e.fondo = RelevoUI.Franja(padre, "Entrada " + (indice + 1), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, y), new Vector2(AnchoPagina, 130f), RelevoUI.ConAlfa(PapelSombra, 0.9f));
        e.raiz = e.fondo.gameObject;

        List<Image> filetes = new List<Image>();
        RelevoUI.Marco(e.raiz.transform, PapelHondo, 2f, filetes);
        e.filetes = filetes.ToArray();

        e.anillo = RelevoUI.Hexagono(e.raiz.transform, "Anillo", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(16f, 0f), new Vector2(96f, 96f), RelevoUI.Cobre, false);

        e.numero = RelevoUI.Texto(e.raiz.transform, "Numero", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(16f, 0f), new Vector2(96f, 96f), 32, TextAnchor.MiddleCenter, RelevoUI.Cobre);
        e.numero.font = RelevoUI.FuenteTitulo();
        e.numero.text = (indice + 1).ToString();

        e.chip = RelevoUI.Capsula(e.raiz.transform, "Chip", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(128f, -12f), new Vector2(168f, 24f), RelevoUI.Ambar);

        e.chipTexto = RelevoUI.Texto(e.raiz.transform, "Chip texto", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(128f, -12f), new Vector2(168f, 24f), 13, TextAnchor.MiddleCenter, Esmalte);
        e.chipTexto.fontStyle = FontStyle.Bold;

        e.titulo = RelevoUI.Texto(e.raiz.transform, "Titulo", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(128f, -42f), new Vector2(AnchoPagina - 150f, 26f), 19, TextAnchor.UpperLeft, Tinta);
        e.titulo.font = RelevoUI.FuenteTitulo();
        e.titulo.fontStyle = FontStyle.Bold;

        e.lugar = RelevoUI.Texto(e.raiz.transform, "Lugar", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(128f, -70f), new Vector2(AnchoPagina - 150f, 22f), 14, TextAnchor.UpperLeft, TintaSuave);

        e.pista = RelevoUI.Texto(e.raiz.transform, "Pista", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(128f, -94f), new Vector2(AnchoPagina - 150f, 20f), 13, TextAnchor.UpperLeft,
            RelevoUI.ConAlfa(TintaSuave, 0.9f));

        // Marcapáginas: solo lo lleva la entrada abierta.
        e.cinta = RelevoUI.Franja(e.raiz.transform, "Marcapaginas", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(-6f, 6f), new Vector2(10f, 0f), Sello);

        // Página arrancada: el hueco que dejó la hoja y su canto desgarrado, con el hilo suelto. Solo
        // se enciende si falta el recuerdo, para que una entrada bloqueada no se lea como una tarjeta
        // vacía sino como una hoja que alguien arrancó del cuaderno.
        e.roto = new GameObject("Roto");
        e.roto.transform.SetParent(e.raiz.transform, false);
        RectTransform rt = e.roto.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        RelevoUI.Franja(e.roto.transform, "Hueco", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f),
            new Vector2(0f, -65f), new Vector2(556f, 34f), RelevoUI.ConAlfa(PapelHondo, 0.45f));

        Dientes(e.roto.transform, new Vector2(-260f, -32f), new Vector2(260f, -32f), 14f,
            RelevoUI.ConAlfa(PapelClaro, 0.95f), 22);

        Cosido(e.roto.transform, new Vector2(-150f, -46f), 34f, RelevoUI.ConAlfa(Sello, 0.6f));
        Cosido(e.roto.transform, new Vector2(180f, -46f), 28f, RelevoUI.ConAlfa(Sello, 0.5f));

        e.roto.SetActive(false);

        return e;
    }

    // ------------------------------------------------------------------ vista: recuerdo abierto

    void ConstruirRecuerdo(Transform p)
    {
        // ---- página izquierda: la ilustración ocupa la hoja
        detIlustracion = RelevoUI.Franja(p, "Ilustracion", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, 118f), new Vector2(AnchoPagina - 24f, 230f), RelevoUI.ConAlfa(PapelSombra, 0.8f));
        RelevoUI.Marco(detIlustracion.transform, PapelHondo, 2f);
        RelevoUI.Esquinas(detIlustracion.transform, 16f, 2f, PapelHondo);

        Mancha(detIlustracion.transform, "Mancha ilus A", new Vector2(150f, -60f), new Vector2(200f, 100f),
            RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.16f), 12f);
        Mancha(detIlustracion.transform, "Mancha ilus B", new Vector2(-170f, 60f), new Vector2(150f, 70f),
            RelevoUI.ConAlfa(RelevoUI.Ambar, 0.12f), -7f);

        detAnillo = RelevoUI.Hexagono(detIlustracion.transform, "Anillo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-176f, 10f), new Vector2(120f, 120f), RelevoUI.Cobre, false);

        detNumero = RelevoUI.Texto(detIlustracion.transform, "Numero", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-176f, 10f), new Vector2(120f, 120f), 40, TextAnchor.MiddleCenter, RelevoUI.Cobre);
        detNumero.font = RelevoUI.FuenteTitulo();

        Linea(detIlustracion.transform, "Cable", new Vector2(-104f, 10f), new Vector2(210f, 10f), 2f,
            RelevoUI.ConAlfa(PapelHondo, 0.9f));

        Image clip = RelevoUI.Franja(detIlustracion.transform, "Clip", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(28f, -14f), new Vector2(102f, 16f), RelevoUI.ConAlfa(Tinta, 0.13f));
        clip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -10f);

        detCaption = RelevoUI.Texto(detIlustracion.transform, "Caption", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(14f, 8f), new Vector2(300f, 20f), 13, TextAnchor.LowerLeft, TintaSuave);

        // Hueco de la hoja arrancada: un vacío oscuro rodeado por el canto desgarrado del papel.
        // Solo aparece en los recuerdos que todavía no están en el cuaderno.
        detFotoVelada = RelevoUI.Panel(detIlustracion.transform, "Hueco arrancado", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(AnchoPagina - 24f, 230f), RelevoUI.ConAlfa(Tinta, 0.60f));

        RelevoUI.Franja(detFotoVelada.transform, "Hondo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(AnchoPagina - 52f, 200f), RelevoUI.ConAlfa(Esmalte, 0.55f));

        float medioAncho = (AnchoPagina - 24f) * 0.5f;
        const float medioAlto = 115f;

        Dientes(detFotoVelada.transform, new Vector2(-medioAncho, -medioAlto), new Vector2(medioAncho, -medioAlto),
            15f, RelevoUI.ConAlfa(PapelClaro, 0.95f), 26);
        Dientes(detFotoVelada.transform, new Vector2(-medioAncho, medioAlto), new Vector2(medioAncho, medioAlto),
            14f, RelevoUI.ConAlfa(Papel, 0.90f), 26);
        Dientes(detFotoVelada.transform, new Vector2(-medioAncho, -medioAlto), new Vector2(-medioAncho, medioAlto),
            15f, RelevoUI.ConAlfa(PapelClaro, 0.90f), 12);
        Dientes(detFotoVelada.transform, new Vector2(medioAncho, -medioAlto), new Vector2(medioAncho, medioAlto),
            15f, RelevoUI.ConAlfa(PapelClaro, 0.90f), 12);

        Text velada = RelevoUI.Texto(detFotoVelada.transform, "Velada texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 12f), new Vector2(400f, 34f), 20, TextAnchor.MiddleCenter, RelevoUI.ConAlfa(PapelSombra, 0.88f));
        velada.font = RelevoUI.FuenteTitulo();
        velada.text = "HOJA ARRANCADA";

        Text veladaPie = RelevoUI.Texto(detFotoVelada.transform, "Velada pie", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -20f), new Vector2(420f, 24f), 13, TextAnchor.MiddleCenter, RelevoUI.ConAlfa(PapelSombra, 0.62f));
        veladaPie.text = "aquí estaba la fotografía del tramo";

        Mancha(detFotoVelada.transform, "Mancha velada", new Vector2(120f, -76f), new Vector2(220f, 78f),
            RelevoUI.ConAlfa(Tinta, 0.35f), 18f);

        // ---- página izquierda: identidad
        detTitulo = RelevoUI.Texto(p, "Det titulo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, -32f), new Vector2(AnchoPagina - 24f, 34f), 26, TextAnchor.MiddleLeft, Tinta);
        detTitulo.font = RelevoUI.FuenteTitulo();
        detTitulo.fontStyle = FontStyle.Bold;

        detOrigen = RelevoUI.Texto(p, "Det origen", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, -62f), new Vector2(AnchoPagina - 24f, 22f), 14, TextAnchor.MiddleLeft, Sello);

        RelevoUI.Franja(p, "Filete detalle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, -80f), new Vector2(AnchoPagina - 24f, 1f), RelevoUI.ConAlfa(PapelHondo, 0.8f));

        detChip = RelevoUI.Capsula(p, "Det chip", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, -112f), new Vector2(220f, 26f), RelevoUI.Ambar);

        detChipTexto = RelevoUI.Texto(p, "Det chip texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, -112f), new Vector2(220f, 26f), 13, TextAnchor.MiddleCenter, Esmalte);
        detChipTexto.fontStyle = FontStyle.Bold;

        detPista = RelevoUI.Texto(p, "Det pista", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, -148f), new Vector2(AnchoPagina - 24f, 22f), 14, TextAnchor.MiddleCenter,
            RelevoUI.ConAlfa(TintaSuave, 0.95f));

        detLugar = RelevoUI.Texto(p, "Det lugar", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, -186f), new Vector2(AnchoPagina - 24f, 22f), 14, TextAnchor.MiddleLeft, TintaSuave);

        detReferencia = RelevoUI.Texto(p, "Det referencia", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, -214f), new Vector2(AnchoPagina - 24f, 22f), 13, TextAnchor.MiddleLeft,
            RelevoUI.ConAlfa(TintaSuave, 0.8f));

        // ---- página derecha: el recuerdo
        detFrase = RelevoUI.Texto(p, "Det frase", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 168f), new Vector2(AnchoPagina, 80f), 20, TextAnchor.MiddleLeft, Tinta);

        RelevoUI.Franja(p, "Filete frase", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 116f), new Vector2(AnchoPagina, 1f), RelevoUI.ConAlfa(PapelHondo, 0.8f));

        detNarrativa = RelevoUI.Texto(p, "Det narrativa", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 30f), new Vector2(AnchoPagina, 160f), 16, TextAnchor.UpperLeft, TintaSuave);

        // Fragmentos ilegibles: renglones cortos y desiguales. Sustituyen al texto cuando la página
        // está incompleta, así que no hay hueco vacío ni caja gris.
        detFragmentos = new GameObject("Fragmentos");
        detFragmentos.transform.SetParent(p, false);
        RectTransform fr = detFragmentos.AddComponent<RectTransform>();
        fr.anchorMin = new Vector2(0.5f, 0.5f);
        fr.anchorMax = new Vector2(0.5f, 0.5f);
        fr.pivot = new Vector2(0.5f, 0.5f);
        fr.sizeDelta = Vector2.zero;
        fr.anchoredPosition = Vector2.zero;

        float[] anchos = { 470f, 390f, 520f, 300f, 440f, 210f };
        for (int i = 0; i < anchos.Length; i++)
        {
            RelevoUI.Franja(detFragmentos.transform, "Renglon " + i, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(ColDer - AnchoPagina * 0.5f, 104f - i * 30f), new Vector2(anchos[i], 7f),
                RelevoUI.ConAlfa(TintaSuave, 0.28f - i * 0.02f));
        }

        Mancha(detFragmentos.transform, "Mancha ilegible", new Vector2(ColDer + 60f, 10f), new Vector2(320f, 150f),
            RelevoUI.ConAlfa(PapelHondo, 0.45f), -8f);

        Text ilegible = RelevoUI.Texto(detFragmentos.transform, "Ilegible", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -92f), new Vector2(AnchoPagina, 26f), 15, TextAnchor.MiddleCenter,
            RelevoUI.ConAlfa(Sello, 0.75f));
        ilegible.text = "la tinta se corrió con el agua";

        detCosido = new GameObject("Cosido");
        detCosido.transform.SetParent(p, false);
        RectTransform cs = detCosido.AddComponent<RectTransform>();
        cs.anchorMin = new Vector2(0.5f, 0.5f);
        cs.anchorMax = new Vector2(0.5f, 0.5f);
        cs.pivot = new Vector2(0.5f, 0.5f);
        cs.sizeDelta = Vector2.zero;
        cs.anchoredPosition = Vector2.zero;

        Cosido(detCosido.transform, new Vector2(ColDer, 40f), 120f, RelevoUI.ConAlfa(Sello, 0.5f));

        // Restos de la página en el canto exterior: lo que quedo pegado al lomo al arrancarla.
        detArrancado = new GameObject("Arrancado");
        detArrancado.transform.SetParent(p, false);
        RectTransform ar = detArrancado.AddComponent<RectTransform>();
        ar.anchorMin = new Vector2(0.5f, 0.5f);
        ar.anchorMax = new Vector2(0.5f, 0.5f);
        ar.pivot = new Vector2(0.5f, 0.5f);
        ar.sizeDelta = Vector2.zero;
        ar.anchoredPosition = Vector2.zero;

        RelevoUI.Franja(detArrancado.transform, "Resto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer + AnchoPagina * 0.5f - 28f, -112f), new Vector2(48f, 196f),
            RelevoUI.ConAlfa(PapelSombra, 0.88f));

        Dientes(detArrancado.transform, new Vector2(ColDer + AnchoPagina * 0.5f - 10f, -206f),
            new Vector2(ColDer + AnchoPagina * 0.5f - 10f, -18f), 14f,
            RelevoUI.ConAlfa(PapelClaro, 0.9f), 14);

        Cosido(detArrancado.transform, new Vector2(ColDer + AnchoPagina * 0.5f - 46f, -150f), 42f,
            RelevoUI.ConAlfa(Sello, 0.55f));

        detSello = RelevoUI.Franja(p, "Det sello", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -140f), new Vector2(240f, 56f), new Color(0f, 0f, 0f, 0f));
        detSello.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -7f);
        detSelloBordes = new List<Image>();
        RelevoUI.Marco(detSello.transform, RelevoUI.ConAlfa(Sello, 0.8f), 3f, detSelloBordes);

        detSelloTexto = RelevoUI.Texto(detSello.transform, "Det sello texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(240f, 56f), 20, TextAnchor.MiddleCenter, RelevoUI.ConAlfa(Sello, 0.9f));
        detSelloTexto.font = RelevoUI.FuenteTitulo();

        detCta = RelevoUI.Capsula(p, "Det cta", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -218f), new Vector2(AnchoPagina, 42f), RelevoUI.ConAlfa(Sello, 0.9f));

        detCtaTexto = RelevoUI.Texto(p, "Det cta texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -218f), new Vector2(AnchoPagina, 42f), 17, TextAnchor.MiddleCenter, PapelClaro);
        detCtaTexto.fontStyle = FontStyle.Bold;

        detAviso = RelevoUI.Texto(p, "Det aviso", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -218f), new Vector2(AnchoPagina, 42f), 16, TextAnchor.MiddleCenter,
            RelevoUI.ConAlfa(PapelClaro, 0.9f));
    }

    // ------------------------------------------------------------------ estado

    void Refrescar()
    {
        int encontrados = 0;
        int leidos = 0;

        for (int i = 0; i < Niveles; i++)
        {
            int nivel = i + 1;
            LevelDefinition definicion = DefinicionesPorDefecto.Obtener(nivel);

            bool tiene = definicion != null && SaveManager.TieneRecuerdo(definicion.recuerdoIdentificador);
            bool leido = tiene && SaveManager.RecuerdoLeido(definicion.recuerdoIdentificador);

            if (tiene) encontrados++;
            if (leido) leidos++;

            Color acento = ColorDeNivel(nivel);
            Entrada e = entradas[i];
            bool elegida = i == seleccion;

            string chip;
            Color colorChip;

            if (leido)
            {
                chip = "LEÍDO";
                colorChip = RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.85f);
            }
            else if (tiene)
            {
                chip = "DESCUBIERTO";
                colorChip = RelevoUI.ConAlfa(RelevoUI.Ambar, 0.92f);
            }
            else
            {
                chip = "BLOQUEADO";
                colorChip = RelevoUI.ConAlfa(PapelHondo, 0.95f);
            }

            // Una entrada bloqueada se ve desvaída: el papel está incompleto, no solo sin leer.
            e.fondo.color = tiene
                ? (elegida ? PapelClaro : RelevoUI.ConAlfa(PapelSombra, 0.9f))
                : (elegida ? RelevoUI.ConAlfa(PapelClaro, 0.72f) : RelevoUI.ConAlfa(PapelHondo, 0.42f));

            for (int f = 0; f < e.filetes.Length; f++) e.filetes[f].color = elegida ? Sello : PapelHondo;

            e.anillo.color = tiene ? acento : RelevoUI.ConAlfa(Gris, 0.7f);
            e.numero.color = tiene ? acento : RelevoUI.ConAlfa(Gris, 0.85f);

            e.chip.color = colorChip;
            e.chipTexto.text = chip;
            e.chipTexto.color = tiene ? Esmalte : Tinta;

            e.cinta.rectTransform.sizeDelta = new Vector2(10f, elegida ? 92f : 0f);
            e.roto.SetActive(!tiene);

            if (definicion == null) continue;

            e.titulo.text = tiene ? definicion.recuerdoTitulo : "— hoja arrancada —";
            e.titulo.color = tiene ? Tinta : RelevoUI.ConAlfa(TintaSuave, 0.9f);
            e.lugar.text = "Origen · " + definicion.tituloIntro;

            e.pista.text = leido
                ? "abierto en el cuaderno"
                : (tiene ? "encontrado · ábrelo para leerlo" : "falta la página de este tramo");

            e.pista.color = leido ? RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.95f)
                : (tiene ? RelevoUI.ConAlfa(RelevoUI.Ambar, 0.95f) : RelevoUI.ConAlfa(Sello, 0.75f));
        }

        recuento.text = "encontrados " + encontrados + " de " + Niveles + "  ·  leídos " + leidos;

        RefrescarPestanas();
        RefrescarVistas();
    }

    void RefrescarPestanas()
    {
        for (int i = 0; i < Niveles; i++)
        {
            bool elegida = i == seleccion;
            Color acento = ColorDeNivel(i + 1);
            float y = 130f - i * 120f;

            pestanas[i].color = elegida ? PapelClaro : RelevoUI.ConAlfa(PapelSombra, 0.95f);
            pestanas[i].rectTransform.anchoredPosition = new Vector2(elegida ? 14f : 4f, y);
            pestanas[i].rectTransform.sizeDelta = new Vector2(elegida ? 104f : 88f, elegida ? 82f : 74f);

            pestanasTexto[i].color = elegida ? Tinta : RelevoUI.ConAlfa(TintaSuave, 0.85f);
            pestanasTexto[i].fontStyle = elegida ? FontStyle.Bold : FontStyle.Normal;
            pestanasTexto[i].rectTransform.anchoredPosition = pestanas[i].rectTransform.anchoredPosition;
            pestanasTexto[i].rectTransform.sizeDelta = pestanas[i].rectTransform.sizeDelta;

            // El filo de color va en el canto exterior de la pestaña, siguiendo su ancho.
            pestanasFilo[i].color = elegida ? acento : RelevoUI.ConAlfa(PapelHondo, 0.9f);
            pestanasFilo[i].rectTransform.anchoredPosition = new Vector2(
                pestanas[i].rectTransform.anchoredPosition.x - pestanas[i].rectTransform.sizeDelta.x + 5f, y);
        }
    }

    void RefrescarVistas()
    {
        vistaColeccion.SetActive(!abierto);
        vistaRecuerdo.SetActive(abierto);

        LevelDefinition d = DefinicionesPorDefecto.Obtener(seleccion + 1);
        bool tiene = d != null && SaveManager.TieneRecuerdo(d.recuerdoIdentificador);
        bool leido = tiene && SaveManager.RecuerdoLeido(d.recuerdoIdentificador);

        Color acento = ColorDeNivel(seleccion + 1);
        int pagina = seleccion + 1;

        indicadorHoja.text = "HOJA " + pagina + " / " + Niveles;
        cabPie.text = abierto ? "el recuerdo, tal y como Nilo lo apuntó" : "recuerdos, sellos y anotaciones";

        pieTexto.text = abierto
            ? "← →  otro recuerdo      ·      Esc / Tab  volver a la colección"
            : "← →  pasar página      ·      ↑ ↓  elegir      ·      Enter  abrir el recuerdo      ·      Esc / Tab  cerrar";

        if (d == null) return;

        // ---------------- ficha de la derecha (colección)
        colIlustracion.color = RelevoUI.ConAlfa(tiene ? RelevoUI.Aclarar(PapelSombra, 0.12f) : PapelSombra, 0.75f);
        colAnillo.color = tiene ? acento : RelevoUI.ConAlfa(Gris, 0.75f);
        colNumero.color = tiene ? acento : RelevoUI.ConAlfa(Gris, 0.9f);
        colNumero.text = pagina.ToString();
        colCaption.text = tiene
            ? "ILUSTRACIÓN · TRAMO " + pagina + " · " + d.tituloIntro.ToUpperInvariant()
            : "ILUSTRACIÓN PENDIENTE · TRAMO " + pagina;

        colTitulo.text = tiene ? d.recuerdoTitulo : "Recuerdo sin encontrar";
        colTitulo.color = tiene ? Tinta : RelevoUI.ConAlfa(TintaSuave, 0.9f);

        colSubtitulo.text = "Nivel de origen · " + d.nombre;
        colSubtitulo.color = tiene ? Sello : RelevoUI.ConAlfa(Sello, 0.5f);

        colNota.text = tiene ? d.recuerdoTexto : "«—»";
        colNota.color = tiene ? Tinta : RelevoUI.ConAlfa(TintaSuave, 0.7f);

        colTexto.text = tiene
            ? d.textoIntro
            : "Esta hoja está cosida al lomo. El recuerdo del " + d.tituloIntro
              + " todavía no ha llegado al cuaderno: Nilo tiene que encontrarlo en el tramo.";
        colTexto.color = tiene ? TintaSuave : RelevoUI.ConAlfa(TintaSuave, 0.75f);

        colSelloTexto.text = !tiene ? "SIN ENCONTRAR" : (leido ? "LEÍDO" : "DESCUBIERTO");
        colSelloTexto.color = !tiene ? RelevoUI.ConAlfa(Gris, 0.85f)
            : (leido ? RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.9f) : RelevoUI.ConAlfa(Sello, 0.85f));
        Tintar(colSelloBordes, RelevoUI.ConAlfa(colSelloTexto.color, 0.8f));

        colCta.color = tiene ? RelevoUI.ConAlfa(Sello, 0.9f) : RelevoUI.ConAlfa(PapelHondo, 0.7f);
        colCtaTexto.color = tiene ? PapelClaro : RelevoUI.ConAlfa(PapelClaro, 0.8f);
        colCtaTexto.text = tiene
            ? "Enter · abrir este recuerdo"
            : "Enter · abrir la hoja incompleta";

        // ---------------- doble página (recuerdo)
        detIlustracion.color = RelevoUI.ConAlfa(tiene ? RelevoUI.Aclarar(PapelSombra, 0.12f) : PapelSombra, 0.8f);
        detAnillo.color = tiene ? acento : RelevoUI.ConAlfa(Gris, 0.8f);
        detNumero.color = tiene ? acento : RelevoUI.ConAlfa(Gris, 0.9f);
        detNumero.text = pagina.ToString();
        detCaption.text = tiene
            ? "RECUERDO " + pagina + " · " + d.tituloIntro.ToUpperInvariant()
            : "PÁGINA ARRANCADA · TRAMO " + pagina;

        detTitulo.text = tiene ? d.recuerdoTitulo : "Hoja arrancada";
        detTitulo.color = tiene ? Tinta : RelevoUI.ConAlfa(TintaSuave, 0.95f);

        detOrigen.text = "Nivel de origen · " + d.nombre;
        detOrigen.color = tiene ? Sello : RelevoUI.ConAlfa(Sello, 0.5f);

        detChip.color = !tiene ? RelevoUI.ConAlfa(Gris, 0.7f)
            : (leido ? RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.85f) : RelevoUI.ConAlfa(acento, 0.85f));
        detChipTexto.text = !tiene ? "FALTA EL RECUERDO" : (leido ? "LEÍDO" : "DESCUBIERTO");
        detChipTexto.color = tiene ? Esmalte : Tinta;

        detPista.text = !tiene
            ? "vuelve al " + d.tituloIntro + " y encuéntralo"
            : (leido ? "abierto en el cuaderno" : "recién encontrado");
        detPista.color = !tiene ? RelevoUI.ConAlfa(Sello, 0.85f) : RelevoUI.ConAlfa(TintaSuave, 0.95f);

        detLugar.text = "Lugar · " + d.tituloIntro;
        detReferencia.text = "Recuerdo " + pagina + " de " + Niveles + " de la colección";

        detFrase.text = tiene ? d.recuerdoTexto : "— hoja arrancada —";
        detFrase.color = tiene ? Tinta : RelevoUI.ConAlfa(TintaSuave, 0.7f);

        detNarrativa.text = tiene ? d.textoIntro : string.Empty;
        detNarrativa.gameObject.SetActive(tiene);

        detFotoVelada.SetActive(!tiene);
        detFragmentos.SetActive(!tiene);
        detCosido.SetActive(!tiene);
        if (detArrancado != null) detArrancado.SetActive(!tiene);

        detSelloTexto.text = !tiene ? "INCOMPLETO" : (leido ? "LEÍDO" : "DESCUBIERTO");
        detSelloTexto.color = !tiene ? RelevoUI.ConAlfa(Gris, 0.9f)
            : (leido ? RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.9f) : RelevoUI.ConAlfa(Sello, 0.85f));
        Tintar(detSelloBordes, RelevoUI.ConAlfa(detSelloTexto.color, 0.8f));

        detCta.gameObject.SetActive(tiene);
        detCtaTexto.gameObject.SetActive(tiene);
        detAviso.gameObject.SetActive(!tiene);
        detAviso.text = "Esc · volver a la colección";
        detAviso.color = RelevoUI.ConAlfa(TintaSuave, 0.9f);
    }

    // ------------------------------------------------------------------ giro de hoja y sonido

    /// <summary>
    /// Giro de hoja: una página en blanco cruza el pliego desde el canto izquierdo del lomo. Usa
    /// tiempo NO escalado, así que funciona con Time.timeScale = 0 igual que el resto del cuaderno.
    /// </summary>
    void PasarHoja()
    {
        if (hojaGiro == null || !isActiveAndEnabled) return;

        if (girando != null) StopCoroutine(girando);
        girando = StartCoroutine(CorrutinaHoja());
    }

    /// <summary>
    /// Giro de hoja: el pliego de la derecha se levanta, cruza el lomo y se posa a la izquierda. La
    /// hoja no es plana —su pliegue se oscurece y su canto se enciende al inclinarse— y va dejando
    /// caer una sombra sobre la página que descubre. Todo con tiempo NO escalar, así que gira igual
    /// con Time.timeScale = 0.
    /// </summary>
    IEnumerator CorrutinaHoja()
    {
        hojaGiro.gameObject.SetActive(true);

        const float duracion = 0.44f;
        float t = 0f;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float e = Mathf.Clamp01(t / duracion);

            // Arranca con impulso y frena al posarse: dos tramos, como el peso de una hoja real.
            float avance = e < 0.7f
                ? Mathf.Pow(e / 0.7f, 0.68f) * 0.84f
                : 0.84f + (1f - Mathf.Pow(1f - (e - 0.7f) / 0.3f, 2.2f)) * 0.16f;

            float angulo = Mathf.LerpUnclamped(0f, -178f, avance);
            float arco = Mathf.Sin(e * Mathf.PI);

            // Un leve cabeceo lateral: la hoja cuelga de un lomo, no de un eje de máquina.
            hojaGiro.rectTransform.localRotation = Quaternion.Euler(0f, angulo, -1.8f * arco);

            // De frente es papel claro; de canto, sombra. El canto libre se enciende al inclinarse.
            float deCanto = 1f - Mathf.Abs(Mathf.Cos(angulo * Mathf.Deg2Rad));
            float alfa = e < 0.68f ? 1f : Mathf.Lerp(1f, 0f, (e - 0.68f) / 0.32f);

            hojaGiro.color = RelevoUI.ConAlfa(Color.Lerp(PapelClaro, PapelSombra, deCanto * 0.8f), alfa);

            if (hojaPliegue != null)
                hojaPliegue.color = RelevoUI.ConAlfa(PapelHondo, (0.08f + 0.42f * deCanto) * alfa);

            if (hojaBrillo != null)
                hojaBrillo.color = RelevoUI.ConAlfa(PapelClaro, (0.25f + 0.60f * deCanto) * alfa);

            // La sombra que la hoja deja caer sobre la página que va descubriendo.
            if (sombraGiro != null)
            {
                float peso = Mathf.Sin(Mathf.Clamp01(e / 0.78f) * Mathf.PI);
                sombraGiro.color = RelevoUI.ConAlfa(Tinta, 0.22f * peso);
                sombraGiro.rectTransform.sizeDelta =
                    new Vector2(Mathf.Lerp(592f, 150f, Mathf.Clamp01(avance)), 520f);
            }

            yield return null;
        }

        hojaGiro.rectTransform.localRotation = Quaternion.identity;
        hojaGiro.gameObject.SetActive(false);
        if (sombraGiro != null) sombraGiro.color = RelevoUI.ConAlfa(Tinta, 0f);

        girando = null;
    }

    void PrepararAudio()
    {
        fuente = gameObject.GetComponent<AudioSource>();
        if (fuente == null) fuente = gameObject.AddComponent<AudioSource>();

        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;
        fuente.volume = 1f;

        clipHoja = CrearClipHoja();
    }

    /// <summary>
    /// Sonido de pasar página, generado en memoria: ruido filtrado con envolvente de roce. No usa
    /// AudioDirector ni GameEvents —esos disparan gestos de Nilo detrás del menú—, así que el papel
    /// suena en la UI y en el nivel no pasa nada.
    /// </summary>
    static AudioClip CrearClipHoja()
    {
        const int frecuencia = 44100;
        const float duracion = 0.26f;

        int muestras = Mathf.RoundToInt(frecuencia * duracion);
        float[] datos = new float[muestras];
        float filtro = 0f;

        for (int i = 0; i < muestras; i++)
        {
            float t = i / (float)frecuencia;
            float e = Mathf.Clamp01(t / duracion);

            // Dos roces: el roce al levantar la hoja y el roce al soltarla.
            float envolvente = Mathf.Sin(Mathf.PI * Mathf.Pow(e, 0.7f)) * (1f - e * 0.45f);
            float segundo = 0.45f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((e - 0.35f) / 0.65f));

            float ruido = Random.value * 2f - 1f;
            filtro = Mathf.Lerp(filtro, ruido, 0.18f + 0.28f * e);

            datos[i] = filtro * (envolvente + segundo) * 0.5f;
        }

        AudioClip clip = AudioClip.Create("Hoja de cuaderno", muestras, 1, frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }

    void SonarHoja()
    {
        if (fuente == null || clipHoja == null) return;
        fuente.pitch = Random.Range(0.94f, 1.08f);
        fuente.PlayOneShot(clipHoja, 0.22f);
    }

    // ------------------------------------------------------------------ ayudantes

    /// <summary>Color de identidad de cada tramo: cobre, turquesa y ámbar, como sus materiales.</summary>
    static Color ColorDeNivel(int nivel)
    {
        switch (nivel)
        {
            case 2: return RelevoUI.Turquesa;
            case 3: return RelevoUI.Ambar;
            default: return RelevoUI.Cobre;
        }
    }

    /// <summary>Colorea un marco ya creado. Nunca crea marcos nuevos: eso acumularía hijos por refresco.</summary>
    static void Tintar(List<Image> bordes, Color color)
    {
        if (bordes == null) return;
        for (int i = 0; i < bordes.Count; i++) if (bordes[i] != null) bordes[i].color = color;
    }

    /// <summary>Mancha de agua: una cápsula translúcida, aplastada y girada.</summary>
    static Image Mancha(Transform padre, string nombre, Vector2 posicion, Vector2 tamano, Color color, float giro)
    {
        Image mancha = RelevoUI.Capsula(padre, nombre, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            posicion, tamano, color);
        mancha.rectTransform.localRotation = Quaternion.Euler(0f, 0f, giro);
        return mancha;
    }

    /// <summary>Tramo de hilo entre dos puntos, en coordenadas del centro del padre. Sin remaches.</summary>
    static Image Linea(Transform padre, string nombre, Vector2 desde, Vector2 hasta, float grosor, Color color)
    {
        GameObject grupo = new GameObject(nombre);
        grupo.transform.SetParent(padre, false);

        RectTransform rt = grupo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = (desde + hasta) * 0.5f;
        rt.sizeDelta = Vector2.zero;

        Vector2 delta = hasta - desde;
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        return RelevoUI.Franja(grupo.transform, "Tramo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(delta.magnitude, grosor), color);
    }

    /// <summary>
    /// Canto desgarrado: una fila de dientes de papel a lo largo de una línea, con el desvío
    /// irregular de una hoja arrancada a mano. Es lo que distingue una página que falta de una en
    /// blanco.
    /// </summary>
    static void Dientes(Transform padre, Vector2 desde, Vector2 hasta, float tamano, Color color, int cuantos)
    {
        Vector2 eje = hasta - desde;
        Vector2 normal = eje.sqrMagnitude > 0.0001f
            ? new Vector2(-eje.y, eje.x).normalized
            : Vector2.up;

        for (int i = 0; i < cuantos; i++)
        {
            float t = cuantos <= 1 ? 0f : i / (float)(cuantos - 1);
            float desvio = Mathf.Sin(i * 2.399f) * tamano * 0.5f;

            Image diente = RelevoUI.Capsula(padre, "Diente", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.Lerp(desde, hasta, t) + normal * desvio,
                new Vector2(tamano * (1.05f + 0.3f * Mathf.Sin(i * 1.7f)), tamano), color);
            diente.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
    }

    /// <summary>Marco interior desplazado hacia dentro: el rebaje del cuero.</summary>
    static void MarcoInset(Transform padre, float margen, Color color, float grosor)
    {
        GameObject go = new GameObject("Marco interior");
        go.transform.SetParent(padre, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);

        RelevoUI.Marco(go.transform, color, grosor);
    }

    /// <summary>Hilo cosido: una X de puntadas sobre la hoja incompleta.</summary>
    static void Cosido(Transform padre, Vector2 centro, float lado, Color color)
    {
        Linea(padre, "Hilo A", centro + new Vector2(-lado * 0.5f, lado * 0.5f),
            centro + new Vector2(lado * 0.5f, -lado * 0.5f), 2f, color);
        Linea(padre, "Hilo B", centro + new Vector2(-lado * 0.5f, -lado * 0.5f),
            centro + new Vector2(lado * 0.5f, lado * 0.5f), 2f, color);

        // Las puntadas: pequeños remates donde el hilo atraviesa el papel.
        for (int i = 0; i < 3; i++)
        {
            float t = 0.2f + i * 0.3f;
            Vector2 punto = Vector2.Lerp(centro + new Vector2(-lado * 0.5f, lado * 0.5f),
                centro + new Vector2(lado * 0.5f, -lado * 0.5f), t);

            RelevoUI.Franja(padre, "Puntada " + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                punto, new Vector2(10f, 3f), color);
        }
    }
}
