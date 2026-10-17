using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menú de exploración (Tab): el cuaderno de viaje abierto ENCIMA del nivel, con pestañas físicas.
///
/// No crea Canvas propio, ni EventSystem, ni se hace persistente: lo construye UIManager DENTRO del
/// mismo lienzo del HUD y es UIManager quien le pasa la entrada, de modo que en toda la escena hay
/// un solo lector del asset de entrada y un solo Canvas de juego.
///
/// Identidad opuesta a la pausa: aquí todo es papel, tinta y memoria (cálido y artesanal), frente al
/// panel técnico de radio y energía de la pausa. Cada pestaña es un pliego de dos páginas: a la
/// izquierda el índice, a la derecha la entrada abierta. El cuaderno (CUADERNO) y ESTADO leen datos
/// reales; INVENTARIO, SKILLS y EQUIPAMIENTO quedan preparados con su estructura completa.
///
/// Nada de lo que hay aquí consulta o modifica el gameplay: SaveManager solo se lee (los recuerdos
/// ya leídos los marca la escena cuadernoRelevo, no este panel) y del nivel solo se leen estados
/// públicos (batería, reserva, puentes, compuertas, palancas).
/// </summary>
public class MenuExploracionUI : MonoBehaviour
{
    /// <summary>Lo engancha UIManager para abrir la escena del cuaderno sin duplicar la lógica.</summary>
    public System.Action AlAbrirCuaderno;

    /// <summary>Lo engancha UIManager para cerrar el overlay y devolver el tiempo.</summary>
    public System.Action AlCerrar;

    public bool Abierto { get; private set; }

    // ------------------------------------------------------------------ medidas del pliego

    const float AnchoHoja = 1420f;
    const float AltoHoja = 800f;

    const float AnchoPagina = 560f;
    const float ColIzq = -310f;
    const float ColDer = 310f;
    const float AnchoDoble = 1140f;

    const float AnchoPestana = 230f;
    const float PasoPestana = 238f;

    const int Niveles = 3;

    // Geometría del árbol de skills: la comparten el dibujo (PaginaSkills) y el refresco de la
    // selección (RefrescarSkills), que necesita saber dónde ha quedado el nodo elegido.
    const float SkX0 = -420f;
    const float SkPaso = 120f;
    const float SkYBus = 170f;
    const float SkYNivel1 = 60f;
    const float SkYNivel2 = -50f;
    const float SkPanelTope = -127f;   // borde superior del panel de descripción (centro -192, alto 130)

    static readonly string[] Nombres = { "CUADERNO", "INVENTARIO", "SKILLS", "EQUIPAMIENTO", "ESTADO" };

    static readonly string[] Pies =
    {
        "recuerdos, mapas y anotaciones",
        "lo que Nilo lleva encima",
        "lo que Nilo ha aprendido",
        "lo que Nilo puede equipar",
        "cómo está el tramo ahora mismo"
    };

    // Paleta del cuaderno: papel cálido, tinta marrón, sello rojo. Nada de azul tecnológico.
    static readonly Color Papel = new Color(0.898f, 0.843f, 0.714f, 1f);
    static readonly Color PapelClaro = new Color(0.945f, 0.904f, 0.796f, 1f);
    static readonly Color PapelSombra = new Color(0.827f, 0.757f, 0.616f, 1f);
    static readonly Color PapelHondo = new Color(0.690f, 0.612f, 0.478f, 1f);
    static readonly Color Tinta = new Color(0.153f, 0.122f, 0.094f, 1f);
    static readonly Color TintaSuave = new Color(0.361f, 0.306f, 0.235f, 1f);
    static readonly Color Sello = new Color(0.612f, 0.196f, 0.157f, 1f);
    static readonly Color Gris = new Color(0.478f, 0.463f, 0.427f, 1f);
    static readonly Color Dorado = new Color(0.788f, 0.647f, 0.322f, 1f);
    static readonly Color Esmalte = new Color(0.129f, 0.263f, 0.271f, 1f);

    enum Seccion { Cuaderno = 0, Inventario = 1, Skills = 2, Equipamiento = 3, Estado = 4 }

    // ------------------------------------------------------------------ contenido preparado

    static readonly string[] CategoriasInv =
    {
        "Herramientas", "Repuestos", "Cables y fusibles",
        "Objetos de memoria", "Carga de la batería", "Notas de campo"
    };

    static readonly string[] DetalleInv =
    {
        "Llaves, destornilladores y piezas de mano. Nilo todavía no lleva ninguna encima.",
        "Muelles, juntas y remaches del canal. El primer repuesto llegará con el siguiente tramo.",
        "Bobinas de cobre, fusibles y empalmes. Aquí se guardará lo que Nilo recoja del suelo.",
        "Los objetos que devuelven un recuerdo. Se ordenan aquí, no en el cuaderno.",
        "Celdas sueltas y cargas a medias. La batería buena no cuenta como objeto de inventario.",
        "Hojas arrancadas con medidas, nombres y dibujos de Maia."
    };

    static readonly string[] Equipo =
    {
        "Batería portátil", "Arnés de cable", "Botas de muelle",
        "Farol de mano", "Llave de paso", "Insignia de Liria"
    };

    static readonly string[] DetalleEquipo =
    {
        "La celda que Nilo puede llevar al hombro. Cambia quién mueve la energía del tramo.",
        "Correa con gancho para arrastrar cable sin soltar las manos.",
        "Suelas de ballesta: el primer salto sobre las compuertas abiertas.",
        "Luz corta de mantenimiento para los túneles sin faro.",
        "La llave cuadrada de las compuertas del Patio de Bombas.",
        "La insignia de quien vuelve a encender Liria. No se compra: se gana."
    };

    const int RamasNum = 8;
    const int NodosRama = 3;

    /// <summary>Una rama del árbol de habilidades: nombre, color, sus tres nodos y su estado.</summary>
    class Rama
    {
        public string nombre;
        public Color color;
        public string[] nodos;
        public string[] detalle;
        public int[] estado;   // 0 = bloqueado, 1 = disponible, 2 = desbloqueado
    }

    /// <summary>
    /// Andamiaje visual del árbol. Todavía NO hay sistema de habilidades: estos estados existen solo
    /// para que el árbol se lea completo (bloqueado, disponible y desbloqueado) y para calcular el
    /// porcentaje de cada rama. Cuando exista el sistema real, esta tabla se sustituye por datos y
    /// ni la navegación ni el dibujo tienen que cambiar.
    /// </summary>
    static readonly Rama[] Ramas =
    {
        new Rama
        {
            nombre = "Energía", color = RelevoUI.Ambar,
            nodos = new[] { "Empalme", "Circuito en serie", "Sobrecarga" },
            detalle = new[]
            {
                "Nilo aprende a cerrar un circuito con las manos y a no soltar el cable antes de tiempo.",
                "Dos fuentes encadenadas: la segunda solo entrega lo que la primera no se queda.",
                "Aguantar un pico de tensión sin que la reserva se vacíe de golpe."
            },
            estado = new[] { 2, 1, 0 }
        },
        new Rama
        {
            nombre = "Circuitos", color = RelevoUI.Turquesa,
            nodos = new[] { "Giro de tramos", "Lectura de puertos", "Tramo estable" },
            detalle = new[]
            {
                "Girar un tramo en pasos de noventa grados, sin forzarlo nunca.",
                "Leer a qué puerto cardinal apunta cada pieza antes de girarla.",
                "Dejar un tramo conectado sin que la corriente se pierda por el desvío muerto."
            },
            estado = new[] { 2, 2, 1 }
        },
        new Rama
        {
            nombre = "Agua", color = RelevoUI.ConAlfa(RelevoUI.Turquesa, 1f),
            nodos = new[] { "Paso inundado", "Compuertas", "Corriente a favor" },
            detalle = new[]
            {
                "Cruzar un tramo anegado sin resbalar con la batería en brazos.",
                "Abrir la compuerta correcta: la del norte vacía el patio.",
                "Usar la corriente del canal para llegar antes al siguiente muelle."
            },
            estado = new[] { 2, 1, 0 }
        },
        new Rama
        {
            nombre = "Exploración", color = RelevoUI.Cobre,
            nodos = new[] { "Paso firme", "Atajo del muelle", "Ruta del canal" },
            detalle = new[]
            {
                "Caminar por las pasarelas manuales sin perder el equilibrio.",
                "El desvío que ahorra la vuelta entera del muelle.",
                "Recordar de memoria el trazado completo del canal de Liria."
            },
            estado = new[] { 2, 0, 0 }
        },
        new Rama
        {
            nombre = "Memoria", color = Dorado,
            nodos = new[] { "Orbe recordado", "Voz de Liria", "Página completa" },
            detalle = new[]
            {
                "Recoger un recuerdo del suelo y guardarlo en el cuaderno.",
                "Reconocer la voz de quien escribió el recuerdo antes de leerlo entero.",
                "Cerrar una entrada del cuaderno con su ilustración y su lugar."
            },
            estado = new[] { 2, 1, 0 }
        },
        new Rama
        {
            nombre = "Radio", color = RelevoUI.Cobre,
            nodos = new[] { "Frecuencia de Maia", "Señal limpia", "Llamada a Liria" },
            detalle = new[]
            {
                "Encontrar la frecuencia en la que Maia sigue transmitiendo.",
                "Separar su voz de la interferencia del faro.",
                "Pedir ayuda a Liria desde cualquier punto del canal."
            },
            estado = new[] { 2, 0, 0 }
        },
        new Rama
        {
            nombre = "Cajas", color = RelevoUI.AmbarSuave,
            nodos = new[] { "Empuje firme", "Peso equilibrado", "Cadena de cajas" },
            detalle = new[]
            {
                "Empujar una caja hasta la placa sin que se desvíe por el borde.",
                "Calcular cuánto peso aguanta una placa antes de ceder.",
                "Mover dos cajas seguidas para mantener dos mecanismos abiertos."
            },
            estado = new[] { 2, 1, 0 }
        },
        new Rama
        {
            nombre = "Reserva", color = RelevoUI.Ambar,
            nodos = new[] { "Autonomía", "Reserva larga", "Recarga en marcha" },
            detalle = new[]
            {
                "Medir cuántas casillas aguanta Nilo lejos de la batería.",
                "Estirar la autonomía sin apagarse a medio patio.",
                "Recuperar carga mientras camina, dentro de la zona de la batería."
            },
            estado = new[] { 1, 0, 0 }
        }
    };

    // ------------------------------------------------------------------ referencias

    GameObject panel;
    RectTransform hoja;
    RectTransform pagina;

    Text cabTitulo;
    Text cabPie;
    Text pieTexto;

    readonly Image[] pestanas = new Image[Nombres.Length];
    readonly Text[] pestanasTexto = new Text[Nombres.Length];
    readonly Image[] pestanasSub = new Image[Nombres.Length];
    readonly GameObject[] paginas = new GameObject[Nombres.Length];
    Image cursorPestana;

    // --- CUADERNO
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
    }

    readonly Entrada[] entradas = new Entrada[Niveles];
    Text recuento;
    Image detIlustracion;
    Image detAnillo;
    Text detNumero;
    Text detCaption;
    Text detTitulo;
    Text detOrigen;
    Text detFrase;
    Text detNarrativa;
    Text detLugar;
    Image detSelloFondo;
    Text detSelloTexto;
    List<Image> detSelloBordes;
    Image detCta;
    Text detCtaTexto;
    int entradaSel;

    // --- INVENTARIO
    class FilaInv
    {
        public Image fondo;
        public Image[] filetes;
        public Image icono;
        public Text nombre;
        public Text cuenta;
    }

    readonly FilaInv[] categorias = new FilaInv[CategoriasInv.Length];
    Text invTitulo;
    Text invNota;
    Text invCuenta;
    readonly Image[] huecos = new Image[3];
    readonly Text[] huecosTexto = new Text[3];
    int inventarioSel;

    // --- SKILLS
    readonly Image[,] nodoRelleno = new Image[RamasNum, NodosRama];
    readonly Image[,] nodoAnillo = new Image[RamasNum, NodosRama];
    readonly Image[,] nodoGlow = new Image[RamasNum, NodosRama];
    readonly Text[,] nodoTexto = new Text[RamasNum, NodosRama];

    // Selección del nodo activo. No basta con cambiar el texto del panel: el nodo elegido lleva
    // halo, borde claro, escala aumentada, marcador en rombo y un cable que baja hasta el panel de
    // descripción. Solo un nodo los tiene encendidos a la vez.
    readonly Image[,] nodoHalo = new Image[RamasNum, NodosRama];
    readonly Image[,] nodoMarco = new Image[RamasNum, NodosRama];
    readonly Image[,] nodoPunta = new Image[RamasNum, NodosRama];
    Image cableSeleccion;
    Image puntaPanel;
    List<Image> skPanelBordes;
    readonly Image[] bus = new Image[RamasNum];
    readonly Image[,] vertical = new Image[RamasNum, NodosRama - 1];
    readonly Text[] ramaEtiqueta = new Text[RamasNum];
    readonly Text[] ramaProgreso = new Text[RamasNum];

    Image raizRelleno;
    Image raizAnillo;
    Text raizTexto;
    Text raizEtiqueta;

    Text skTitulo;
    Image skChip;
    Text skChipTexto;
    Text skRama;
    Text skDesc;
    Text skRequisito;
    Text skCoste;
    Image skBarraFondo;
    Image skBarra;
    Image skFilo;
    Text skBarraTexto;

    int ramaSel;
    int nivelSel;

    // --- EQUIPAMIENTO
    class Slot
    {
        public Image fondo;
        public Image[] filetes;
        public Image anillo;
        public Text nombre;
        public Text estado;
    }

    readonly Slot[] equipo = new Slot[Equipo.Length];
    Text eqTitulo;
    Text eqEstado;
    Text eqDesc;
    Text eqRequisito;
    Image eqAnillo;
    int equipoSel;

    // --- ESTADO
    readonly Text[] estadoValor = new Text[8];
    readonly Image[] estadoFilo = new Image[8];
    Image hitoActual;
    Text hitoActualTexto;

    int seccion;
    bool zonaPestanas;
    bool flanco;
    Coroutine paginando;

    // ------------------------------------------------------------------ API

    /// <summary>Construye el overlay dentro del lienzo que ya existe. Lo llama UIManager una vez.</summary>
    public void Construir(Transform lienzo)
    {
        if (panel != null || lienzo == null) return;

        panel = RelevoUI.Panel(lienzo, "PanelExploracion", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(4000f, 4000f), RelevoUI.ConAlfa(RelevoUI.FondoHondo, 0.94f));

        // Sombra del pliego: la hoja se apoya sobre la mesa, no flota.
        RelevoUI.Franja(panel.transform, "Sombra hoja", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(10f, -14f), new Vector2(AnchoHoja, AltoHoja), new Color(0f, 0f, 0f, 0.35f));

        hoja = RelevoUI.Franja(panel.transform, "Hoja", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(AnchoHoja, AltoHoja), Papel).rectTransform;

        RelevoUI.Marco(hoja, PapelHondo, 3f);
        RelevoUI.Esquinas(hoja, 34f, 3f, PapelHondo);

        Papeleria();

        cabTitulo = RelevoUI.Texto(hoja, "Cabecera titulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -18f), new Vector2(AnchoDoble, 42f), 34, TextAnchor.MiddleCenter, Tinta);
        cabTitulo.font = RelevoUI.FuenteTitulo();
        cabTitulo.text = "CUADERNO DE VIAJE";

        cabPie = RelevoUI.Texto(hoja, "Cabecera pie", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -60f), new Vector2(AnchoDoble, 26f), 18, TextAnchor.MiddleCenter, TintaSuave);

        pagina = RelevoUI.Franja(hoja, "Paginas", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -60f), new Vector2(1180f, 520f), new Color(0f, 0f, 0f, 0f)).rectTransform;

        paginas[(int)Seccion.Cuaderno] = PaginaCuaderno();
        paginas[(int)Seccion.Inventario] = PaginaInventario();
        paginas[(int)Seccion.Skills] = PaginaSkills();
        paginas[(int)Seccion.Equipamiento] = PaginaEquipamiento();
        paginas[(int)Seccion.Estado] = PaginaEstado();

        Pestanas();
        Pie();

        panel.SetActive(false);
    }

    public void Abrir()
    {
        if (panel == null) return;

        seccion = 0;
        entradaSel = 0;
        inventarioSel = 0;
        ramaSel = 0;
        nivelSel = 0;
        equipoSel = 0;
        zonaPestanas = false;
        flanco = false;

        Abierto = true;
        Refrescar();
        RelevoUIAnim.Entrada(panel, 0.18f, 0f, true);
    }

    public void Cerrar()
    {
        Abierto = false;
        if (panel == null) return;
        RelevoUIAnim.Salida(panel, 0.16f, null);
    }

    /// <summary>
    /// Toda la navegación del overlay pasa por aquí: UIManager es el único que lee el asset de
    /// entrada y le cede el turno mientras este menú está abierto.
    ///
    /// Dos zonas de foco: el contenido de la página y la franja de pestañas. Se sube a las pestañas
    /// con ↑ desde la primera fila (o desde el primer nivel del árbol) y se vuelve al contenido con
    /// ↓ o Enter. Así ← → cambia de pestaña y, dentro de SKILLS, recorre las ramas del árbol.
    /// </summary>
    public void Manejar(RelevoInput entrada)
    {
        if (entrada == null || !Abierto) return;

        if (entrada.Pulsado(entrada.Cuaderno) || entrada.Pulsado(entrada.Pausa) || entrada.Pulsado(entrada.Cancelar))
        {
            if (AlCerrar != null) AlCerrar();
            return;
        }

        if (entrada.Pulsado(entrada.Confirmar))
        {
            if (zonaPestanas)
            {
                zonaPestanas = false;
                Refrescar();
                return;
            }

            if (seccion == (int)Seccion.Cuaderno && AlAbrirCuaderno != null) AlAbrirCuaderno();
            return;
        }

        Vector2 mover = entrada.LeerMovimiento();
        if (mover.sqrMagnitude < 0.25f)
        {
            flanco = false;
            return;
        }

        if (flanco) return;
        flanco = true;

        bool horizontal = Mathf.Abs(mover.x) >= Mathf.Abs(mover.y);
        float eje = horizontal ? mover.x : mover.y;

        if (zonaPestanas)
        {
            if (horizontal) CambiarSeccion(eje > 0f ? 1 : -1);
            else if (eje < 0f)
            {
                zonaPestanas = false;
                Refrescar();
            }
            return;
        }

        if (seccion == (int)Seccion.Skills)
        {
            if (horizontal)
            {
                ramaSel += eje > 0f ? 1 : -1;
                if (ramaSel < 0) ramaSel = RamasNum - 1;
                if (ramaSel >= RamasNum) ramaSel = 0;
            }
            else if (eje > 0f)
            {
                if (nivelSel == 0) zonaPestanas = true;
                else nivelSel--;
            }
            else if (nivelSel < NodosRama - 1)
            {
                nivelSel++;
            }

            Refrescar();
            return;
        }

        if (seccion == (int)Seccion.Estado)
        {
            if (eje > 0f) zonaPestanas = true;
            Refrescar();
            return;
        }

        if (seccion == (int)Seccion.Cuaderno)
        {
            if (horizontal) return;

            if (eje > 0f)
            {
                if (entradaSel == 0) zonaPestanas = true;
                else entradaSel--;
            }
            else if (entradaSel < Niveles - 1) entradaSel++;

            Refrescar();
            return;
        }

        if (seccion == (int)Seccion.Inventario)
        {
            if (horizontal) return;

            if (eje > 0f)
            {
                if (inventarioSel == 0) zonaPestanas = true;
                else inventarioSel--;
            }
            else if (inventarioSel < CategoriasInv.Length - 1) inventarioSel++;

            Refrescar();
            return;
        }

        // EQUIPAMIENTO: rejilla de 3 x 2.
        if (horizontal)
        {
            equipoSel += eje > 0f ? 1 : -1;
            if (equipoSel < 0) equipoSel = Equipo.Length - 1;
            if (equipoSel >= Equipo.Length) equipoSel = 0;
        }
        else if (eje > 0f)
        {
            if (equipoSel < 3) zonaPestanas = true;
            else equipoSel -= 3;
        }
        else if (equipoSel < 3)
        {
            equipoSel += 3;
        }

        Refrescar();
    }

    void CambiarSeccion(int paso)
    {
        seccion += paso;
        if (seccion < 0) seccion = Nombres.Length - 1;
        if (seccion >= Nombres.Length) seccion = 0;

        Refrescar();
        GirarPagina(paginas[seccion], paso);
    }

    // ------------------------------------------------------------------ papelería de fondo

    /// <summary>Manchas de agua, costuras, un sello de radio y recortes: la textura del cuaderno.</summary>
    void Papeleria()
    {
        // Lomo y costuras: la franja oscura de la izquierda, la que hace que se lea como un libro.
        RelevoUI.Franja(hoja, "Lomo", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(56f, AltoHoja), PapelHondo);

        for (int i = 0; i < 7; i++)
        {
            RelevoUI.Franja(hoja, "Costura " + i, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(20f, -300f + i * 100f), new Vector2(3f, 46f), RelevoUI.ConAlfa(PapelClaro, 0.45f));
        }

        // Pliegue central: el doblez del pliego.
        RelevoUI.Franja(hoja, "Pliegue", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -60f), new Vector2(2f, 520f), RelevoUI.ConAlfa(PapelHondo, 0.55f));

        // Manchas de agua repartidas: dos turquesas y una de café.
        Mancha(hoja, "Mancha A", new Vector2(-520f, 250f), new Vector2(190f, 120f), RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.10f), 18f);
        Mancha(hoja, "Mancha B", new Vector2(560f, -290f), new Vector2(240f, 130f), RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.08f), -12f);
        Mancha(hoja, "Mancha C", new Vector2(120f, 330f), new Vector2(150f, 90f), RelevoUI.ConAlfa(PapelHondo, 0.22f), 6f);
        Mancha(hoja, "Mancha D", new Vector2(-140f, -330f), new Vector2(210f, 70f), RelevoUI.ConAlfa(PapelHondo, 0.18f), -4f);

        // Sello de radio en la esquina: ondas concéntricas y un hexágono.
        RelevoUI.Hexagono(hoja, "Radio anillo", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-120f, -138f), new Vector2(120f, 120f), RelevoUI.ConAlfa(PapelHondo, 0.45f), false);
        RelevoUI.Hexagono(hoja, "Radio anillo 2", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-120f, -138f), new Vector2(72f, 72f), RelevoUI.ConAlfa(PapelHondo, 0.38f), false);
        RelevoUI.Hexagono(hoja, "Radio punto", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-120f, -138f), new Vector2(18f, 18f), RelevoUI.ConAlfa(Sello, 0.45f), true);

        // Recorte pegado con celo en la esquina inferior izquierda.
        Image recorte = RelevoUI.Franja(hoja, "Recorte", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(120f, 60f), new Vector2(230f, 74f), RelevoUI.ConAlfa(PapelClaro, 0.9f));
        RelevoUI.Marco(recorte.transform, RelevoUI.ConAlfa(PapelHondo, 0.7f), 1f);
        RelevoUI.Franja(recorte.transform, "Celo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, 6f), new Vector2(76f, 18f), RelevoUI.ConAlfa(RelevoUI.Tinta, 0.10f));

        Text nota = RelevoUI.Texto(recorte.transform, "Texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -4f), new Vector2(206f, 56f), 13, TextAnchor.MiddleCenter, TintaSuave);
        nota.text = "«Si la luz se apaga,\nesperen.»\n— cuaderno de la barquera";
    }

    // ------------------------------------------------------------------ pestañas y pie

    void Pestanas()
    {
        float inicio = -PasoPestana * 2f;

        for (int i = 0; i < Nombres.Length; i++)
        {
            float x = inicio + i * PasoPestana;

            pestanas[i] = RelevoUI.Capsula(hoja, "Pestana " + Nombres[i], new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                new Vector2(x, 8f), new Vector2(AnchoPestana, 64f), PapelHondo);

            pestanasTexto[i] = RelevoUI.Texto(hoja, "Pestana texto " + Nombres[i], new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                new Vector2(x, 8f), new Vector2(AnchoPestana, 64f), 17, TextAnchor.MiddleCenter, TintaSuave);
            pestanasTexto[i].font = RelevoUI.FuenteTitulo();
            pestanasTexto[i].text = Nombres[i];

            pestanasSub[i] = RelevoUI.Franja(hoja, "Pestana filo " + Nombres[i], new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                new Vector2(x, 12f), new Vector2(AnchoPestana - 26f, 3f), Acento(i));
        }

        cursorPestana = RelevoUI.Franja(hoja, "Cursor pestanas", new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
            new Vector2(0f, -6f), new Vector2(16f, 16f), Sello);
        cursorPestana.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        cursorPestana.gameObject.SetActive(false);
    }

    void Pie()
    {
        RelevoUI.Franja(hoja, "Filete pie", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 44f), new Vector2(AnchoHoja - 140f, 2f), RelevoUI.ConAlfa(PapelHondo, 0.9f));

        pieTexto = RelevoUI.Texto(hoja, "Pie", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 14f), new Vector2(AnchoHoja - 120f, 24f), 14, TextAnchor.MiddleCenter,
            RelevoUI.ConAlfa(TintaSuave, 0.95f));
        pieTexto.text = "↑↓  moverse      ·      ↑  subir a las pestañas      ·      ←→  pestaña o rama      ·      Enter  abrir      ·      Esc / Tab  volver";
    }

    /// <summary>Color de identidad de cada pestaña.</summary>
    static Color Acento(int indice)
    {
        switch ((Seccion)indice)
        {
            case Seccion.Inventario: return RelevoUI.Cobre;
            case Seccion.Skills: return RelevoUI.Turquesa;
            case Seccion.Equipamiento: return RelevoUI.Cobre;
            case Seccion.Estado: return RelevoUI.Verde;
            default: return RelevoUI.Ambar;
        }
    }

    // ------------------------------------------------------------------ página CUADERNO

    GameObject PaginaCuaderno()
    {
        GameObject p = NuevaPagina("Pagina cuaderno");

        Text cabeceraIzq = RelevoUI.Texto(p.transform, "Cabecera izq", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, 232f), new Vector2(AnchoPagina, 24f), 14, TextAnchor.MiddleLeft, TintaSuave);
        cabeceraIzq.text = "ENTRADAS · TRES TRAMOS";

        recuento = RelevoUI.Texto(p.transform, "Recuento", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, 232f), new Vector2(AnchoPagina, 24f), 14, TextAnchor.MiddleRight, Sello);

        for (int i = 0; i < Niveles; i++)
        {
            entradas[i] = CrearEntrada(p.transform, i, 140f - i * 150f);
        }

        // ---------------- página derecha: la entrada abierta
        detIlustracion = RelevoUI.Franja(p.transform, "Ilustracion", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 175f), new Vector2(AnchoPagina, 150f), RelevoUI.ConAlfa(PapelSombra, 0.75f));
        RelevoUI.Marco(detIlustracion.transform, PapelHondo, 2f);
        RelevoUI.Esquinas(detIlustracion.transform, 16f, 2f, PapelHondo);

        Mancha(detIlustracion.transform, "Mancha ilus A", new Vector2(150f, -30f), new Vector2(180f, 90f),
            RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.18f), 14f);
        Mancha(detIlustracion.transform, "Mancha ilus B", new Vector2(-160f, 40f), new Vector2(120f, 60f),
            RelevoUI.ConAlfa(RelevoUI.Ambar, 0.12f), -8f);

        detAnillo = RelevoUI.Hexagono(detIlustracion.transform, "Anillo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-186f, 4f), new Vector2(104f, 104f), RelevoUI.Cobre, false);

        detNumero = RelevoUI.Texto(detIlustracion.transform, "Numero", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-186f, 4f), new Vector2(104f, 104f), 34, TextAnchor.MiddleCenter, RelevoUI.Cobre);
        detNumero.font = RelevoUI.FuenteTitulo();

        Enlace(detIlustracion.transform, "Cable", new Vector2(-120f, 4f), new Vector2(236f, 4f), 2f, RelevoUI.ConAlfa(PapelHondo, 0.9f));

        Image clip = RelevoUI.Franja(detIlustracion.transform, "Clip", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(30f, -12f), new Vector2(96f, 16f), RelevoUI.ConAlfa(RelevoUI.Tinta, 0.12f));
        clip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -12f);

        detCaption = RelevoUI.Texto(detIlustracion.transform, "Caption", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(14f, 8f), new Vector2(240f, 20f), 13, TextAnchor.LowerLeft, TintaSuave);

        detTitulo = RelevoUI.Texto(p.transform, "Detalle titulo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 62f), new Vector2(AnchoPagina, 34f), 24, TextAnchor.MiddleLeft, Tinta);
        detTitulo.font = RelevoUI.FuenteTitulo();
        detTitulo.fontStyle = FontStyle.Bold;

        detOrigen = RelevoUI.Texto(p.transform, "Detalle origen", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 34f), new Vector2(AnchoPagina, 22f), 14, TextAnchor.MiddleLeft, Sello);

        RelevoUI.Franja(p.transform, "Filete detalle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 18f), new Vector2(AnchoPagina, 1f), RelevoUI.ConAlfa(PapelHondo, 0.8f));

        detFrase = RelevoUI.Texto(p.transform, "Detalle frase", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -12f), new Vector2(AnchoPagina, 56f), 17, TextAnchor.MiddleLeft, Tinta);

        detNarrativa = RelevoUI.Texto(p.transform, "Detalle narrativa", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -96f), new Vector2(AnchoPagina, 104f), 15, TextAnchor.UpperLeft, TintaSuave);

        detLugar = RelevoUI.Texto(p.transform, "Detalle lugar", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer + 12f, -162f), new Vector2(AnchoPagina - 12f, 22f), 14, TextAnchor.MiddleLeft, TintaSuave);

        RelevoUI.Hexagono(p.transform, "Marca lugar", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer - (AnchoPagina * 0.5f) + 4f, -162f), new Vector2(14f, 14f), Sello, true);

        detCta = RelevoUI.Capsula(p.transform, "Cta", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -226f), new Vector2(AnchoPagina, 42f), RelevoUI.ConAlfa(Sello, 0.9f));

        detCtaTexto = RelevoUI.Texto(p.transform, "Cta texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -226f), new Vector2(AnchoPagina, 42f), 17, TextAnchor.MiddleCenter, PapelClaro);
        detCtaTexto.fontStyle = FontStyle.Bold;

        detSelloFondo = RelevoUI.Franja(p.transform, "Sello fondo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer + 150f, 127f), new Vector2(230f, 54f), new Color(0f, 0f, 0f, 0f));
        detSelloFondo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -7f);
        detSelloBordes = new List<Image>();
        RelevoUI.Marco(detSelloFondo.transform, RelevoUI.ConAlfa(Sello, 0.8f), 3f, detSelloBordes);

        detSelloTexto = RelevoUI.Texto(detSelloFondo.transform, "Sello texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(230f, 54f), 20, TextAnchor.MiddleCenter, RelevoUI.ConAlfa(Sello, 0.9f));
        detSelloTexto.font = RelevoUI.FuenteTitulo();

        return p;
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

        // Miniatura: el conector hexagonal del tramo con su número de página.
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

        return e;
    }

    void RefrescarCuaderno()
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
            bool elegida = i == entradaSel;

            // Tres estados del recuerdo: bloqueado, descubierto y leído.
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

            e.fondo.color = elegida ? PapelClaro : RelevoUI.ConAlfa(PapelSombra, 0.9f);
            for (int f = 0; f < e.filetes.Length; f++) e.filetes[f].color = elegida ? Sello : PapelHondo;

            e.anillo.color = tiene ? acento : RelevoUI.ConAlfa(Gris, 0.7f);
            e.numero.color = tiene ? acento : RelevoUI.ConAlfa(Gris, 0.85f);

            e.chip.color = colorChip;
            e.chipTexto.text = chip;
            e.chipTexto.color = tiene ? Esmalte : Tinta;

            if (definicion == null) continue;

            e.titulo.text = tiene ? definicion.recuerdoTitulo : "Recuerdo bloqueado";
            e.titulo.color = tiene ? Tinta : RelevoUI.ConAlfa(TintaSuave, 0.85f);
            e.lugar.text = "Origen · " + definicion.tituloIntro;

            e.pista.text = leido
                ? "abierto en el cuaderno"
                : (tiene ? "encontrado · ábrelo para leerlo" : "la página sigue en blanco");

            e.pista.color = leido ? RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.95f)
                : (tiene ? RelevoUI.ConAlfa(RelevoUI.Ambar, 0.95f) : RelevoUI.ConAlfa(TintaSuave, 0.8f));

            e.cinta.rectTransform.sizeDelta = new Vector2(10f, elegida ? 92f : 0f);

            if (!elegida) continue;

            // ---- detalle de la página derecha
            DetallarRecuerdo(definicion, tiene, leido, nivel, acento);
        }

        recuento.text = "encontrados " + encontrados + " de " + Niveles + "  ·  leídos " + leidos;
    }

    /// <summary>Vuelca el recuerdo seleccionado en la página derecha, con sus seis campos.</summary>
    void DetallarRecuerdo(LevelDefinition d, bool tiene, bool leido, int nivel, Color acento)
    {
        detIlustracion.color = RelevoUI.ConAlfa(tiene ? RelevoUI.Aclarar(PapelSombra, 0.12f) : PapelSombra, 0.75f);
        detAnillo.color = tiene ? acento : RelevoUI.ConAlfa(Gris, 0.75f);
        detNumero.color = tiene ? acento : RelevoUI.ConAlfa(Gris, 0.9f);
        detNumero.text = nivel.ToString();
        detCaption.text = tiene
            ? "ILUSTRACIÓN · TRAMO " + nivel + " · " + d.tituloIntro.ToUpperInvariant()
            : "ILUSTRACIÓN PENDIENTE · TRAMO " + nivel;

        detTitulo.text = tiene ? d.recuerdoTitulo : "Recuerdo bloqueado";
        detTitulo.color = tiene ? Tinta : RelevoUI.ConAlfa(TintaSuave, 0.9f);

        detOrigen.text = "Nivel de origen · " + d.nombre;
        detOrigen.color = tiene ? Sello : RelevoUI.ConAlfa(Sello, 0.5f);

        detFrase.text = tiene
            ? d.recuerdoTexto
            : "«—»";
        detFrase.color = tiene ? Tinta : RelevoUI.ConAlfa(TintaSuave, 0.7f);

        detNarrativa.text = tiene
            ? d.textoIntro
            : "La página está en blanco. El orbe ámbar del " + d.tituloIntro
              + " sigue esperando a que Nilo lo encuentre y lo traiga hasta aquí.";
        detNarrativa.color = tiene ? TintaSuave : RelevoUI.ConAlfa(TintaSuave, 0.75f);

        detLugar.text = "Lugar de origen · " + d.tituloIntro;

        detSelloTexto.text = !tiene ? "BLOQUEADO" : (leido ? "LEÍDO" : "DESCUBIERTO");
        Color colorSello = !tiene ? RelevoUI.ConAlfa(Gris, 0.85f)
            : (leido ? RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.9f) : RelevoUI.ConAlfa(Sello, 0.85f));

        detSelloTexto.color = colorSello;

        // El marco se colorea, no se vuelve a crear: crear uno por refresco acumularía hijos.
        if (detSelloBordes != null)
        {
            Color colorMarco = RelevoUI.ConAlfa(colorSello, 0.8f);
            for (int b = 0; b < detSelloBordes.Count; b++) if (detSelloBordes[b] != null) detSelloBordes[b].color = colorMarco;
        }

        detCta.color = tiene ? RelevoUI.ConAlfa(Sello, 0.9f) : RelevoUI.ConAlfa(PapelHondo, 0.7f);
        detCtaTexto.color = tiene ? PapelClaro : RelevoUI.ConAlfa(PapelClaro, 0.75f);
        detCtaTexto.text = tiene
            ? "Enter · abrir el cuaderno de viaje"
            : "Enter · abrir el cuaderno de viaje (entrada en blanco)";
    }

    // ------------------------------------------------------------------ página INVENTARIO

    GameObject PaginaInventario()
    {
        GameObject p = NuevaPagina("Pagina inventario");

        Text cabecera = RelevoUI.Texto(p.transform, "Cabecera", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, 232f), new Vector2(AnchoPagina, 24f), 14, TextAnchor.MiddleLeft, TintaSuave);
        cabecera.text = "CATEGORÍAS · SEIS CAJONES";

        for (int i = 0; i < categorias.Length; i++)
        {
            float y = 170f - i * 72f;

            FilaInv fila = new FilaInv();
            fila.fondo = RelevoUI.Franja(p.transform, "Categoria " + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(ColIzq, y), new Vector2(AnchoPagina, 62f), RelevoUI.ConAlfa(PapelSombra, 0.85f));
            List<Image> filetes = new List<Image>();
            RelevoUI.Marco(fila.fondo.transform, PapelHondo, 1f, filetes);
            fila.filetes = filetes.ToArray();

            fila.icono = RelevoUI.Hexagono(fila.fondo.transform, "Icono", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(20f, 0f), new Vector2(38f, 38f), RelevoUI.Cobre, false);

            fila.nombre = RelevoUI.Texto(fila.fondo.transform, "Nombre", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(72f, 8f), new Vector2(300f, 26f), 18, TextAnchor.MiddleLeft, Tinta);
            fila.nombre.text = CategoriasInv[i];

            fila.cuenta = RelevoUI.Texto(fila.fondo.transform, "Cuenta", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(72f, -14f), new Vector2(360f, 20f), 13, TextAnchor.MiddleLeft,
                RelevoUI.ConAlfa(TintaSuave, 0.9f));
            fila.cuenta.text = "0 de 3 espacios · sin descubrir";

            categorias[i] = fila;
        }

        // ---------------- página derecha: el cajón abierto
        invTitulo = RelevoUI.Texto(p.transform, "Inv titulo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 210f), new Vector2(AnchoPagina, 34f), 24, TextAnchor.MiddleLeft, Tinta);
        invTitulo.font = RelevoUI.FuenteTitulo();
        invTitulo.fontStyle = FontStyle.Bold;

        invCuenta = RelevoUI.Texto(p.transform, "Inv cuenta", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 180f), new Vector2(AnchoPagina, 22f), 14, TextAnchor.MiddleLeft, Sello);
        invCuenta.text = "0 objetos · categoría preparada";

        for (int i = 0; i < 3; i++)
        {
            float x = ColDer - 170f + i * 170f;

            huecos[i] = RelevoUI.Franja(p.transform, "Hueco " + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, 40f), new Vector2(150f, 150f), RelevoUI.ConAlfa(PapelSombra, 0.55f));
            RelevoUI.Marco(huecos[i].transform, RelevoUI.ConAlfa(PapelHondo, 0.85f), 1f);
            RelevoUI.Esquinas(huecos[i].transform, 16f, 1f, RelevoUI.ConAlfa(PapelHondo, 0.85f));

            RelevoUI.Hexagono(huecos[i].transform, "Vacio", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 14f), new Vector2(66f, 66f), RelevoUI.ConAlfa(TintaSuave, 0.28f), false);

            huecosTexto[i] = RelevoUI.Texto(huecos[i].transform, "Etiqueta", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(140f, 34f), 13, TextAnchor.LowerCenter,
                RelevoUI.ConAlfa(TintaSuave, 0.85f));
            huecosTexto[i].text = "ESPACIO " + (i + 1) + "\nsin descubrir";
        }

        invNota = RelevoUI.Texto(p.transform, "Inv nota", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -96f), new Vector2(AnchoPagina, 120f), 16, TextAnchor.UpperLeft, TintaSuave);

        RelevoUI.Franja(p.transform, "Inv filete", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -172f), new Vector2(AnchoPagina, 1f), RelevoUI.ConAlfa(PapelHondo, 0.8f));

        RelevoUI.Texto(p.transform, "Inv pie", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -196f), new Vector2(AnchoPagina, 40f), 14, TextAnchor.UpperLeft,
            RelevoUI.ConAlfa(TintaSuave, 0.85f)).text =
            "Los huecos vacíos son intencionados: cada cajón espera a su primer objeto.\nNada de esta página cambia el juego.";

        return p;
    }

    void RefrescarInventario()
    {
        // Los tres huecos del cajón abierto se tiñen con el acento del tramo, para que se lean como
        // espacios reservados y no como un fallo de carga.
        for (int i = 0; i < huecos.Length; i++)
        {
            huecos[i].color = RelevoUI.ConAlfa(PapelSombra, 0.55f + inventarioSel * 0.04f);
            huecosTexto[i].color = RelevoUI.ConAlfa(TintaSuave, 0.75f + inventarioSel * 0.03f);
        }

        for (int i = 0; i < categorias.Length; i++)
        {
            bool elegida = i == inventarioSel;
            categorias[i].fondo.color = elegida ? PapelClaro : RelevoUI.ConAlfa(PapelSombra, 0.85f);
            for (int f = 0; f < categorias[i].filetes.Length; f++)
                categorias[i].filetes[f].color = elegida ? Sello : PapelHondo;

            categorias[i].icono.color = elegida ? Sello : RelevoUI.Cobre;
            categorias[i].nombre.color = elegida ? Tinta : RelevoUI.ConAlfa(Tinta, 0.85f);
        }

        invTitulo.text = CategoriasInv[inventarioSel];
        invNota.text = DetalleInv[inventarioSel];
    }

    // ------------------------------------------------------------------ página SKILLS

    GameObject PaginaSkills()
    {
        GameObject p = NuevaPagina("Pagina skills");

        const float yBus = SkYBus;
        const float yNivel1 = SkYNivel1;
        const float yNivel2 = SkYNivel2;
        const float paso = SkPaso;
        const float x0 = SkX0;

        // Raíz: Nilo, el nodo del que cuelga todo el árbol.
        raizRelleno = RelevoUI.Hexagono(p.transform, "Raiz", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-505f, yBus), new Vector2(62f, 62f), RelevoUI.ConAlfa(Esmalte, 0.92f), true);
        raizAnillo = RelevoUI.Hexagono(p.transform, "Raiz anillo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-505f, yBus), new Vector2(62f, 62f), RelevoUI.Ambar, false);
        raizTexto = RelevoUI.Texto(p.transform, "Raiz texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-505f, yBus), new Vector2(62f, 62f), 22, TextAnchor.MiddleCenter, RelevoUI.Ambar);
        raizTexto.font = RelevoUI.FuenteTitulo();
        raizTexto.text = "N";

        raizEtiqueta = RelevoUI.Texto(p.transform, "Raiz etiqueta", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-505f, 128f), new Vector2(120f, 20f), 13, TextAnchor.MiddleCenter, TintaSuave);
        raizEtiqueta.text = "NILO";

        // Conexión con el panel de descripción: se dibuja ANTES que los nodos para quedar detrás de
        // ellos, y se recoloca en cada refresco sobre la columna del nodo elegido.
        cableSeleccion = RelevoUI.Franja(p.transform, "Cable seleccion", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-380f, 20f), new Vector2(2f, 10f), RelevoUI.ConAlfa(RelevoUI.Ambar, 0.35f));

        puntaPanel = RelevoUI.Capsula(p.transform, "Punta panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-380f, SkPanelTope + 5f), new Vector2(12f, 12f), RelevoUI.Ambar);
        puntaPanel.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        for (int i = 0; i < RamasNum; i++)
        {
            Rama rama = Ramas[i];
            float x = x0 + i * paso;

            // Etiqueta de la rama, encima del bus.
            ramaEtiqueta[i] = RelevoUI.Texto(p.transform, "Rama " + rama.nombre, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, 226f), new Vector2(paso - 2f, 22f), 13, TextAnchor.MiddleCenter, TintaSuave);
            ramaEtiqueta[i].text = rama.nombre;

            // Bus: del nodo anterior (o de la raíz) a este.
            float desde = i == 0 ? -474f : x0 + (i - 1) * paso + 26f;
            bus[i] = Enlace(p.transform, "Bus " + i, new Vector2(desde, yBus), new Vector2(x - 26f, yBus), 2f,
                RelevoUI.ConAlfa(TintaSuave, 0.3f));

            for (int j = 0; j < NodosRama; j++)
            {
                float y = j == 0 ? yBus : (j == 1 ? yNivel1 : yNivel2);

                if (j > 0)
                {
                    vertical[i, j - 1] = Enlace(p.transform, "Vertical " + i + "_" + j,
                        new Vector2(x, (j == 1 ? yBus : yNivel1) - 26f),
                        new Vector2(x, y + 26f), 2f, RelevoUI.ConAlfa(TintaSuave, 0.3f));
                }

                nodoGlow[i, j] = RelevoUI.Hexagono(p.transform, "Glow " + i + "_" + j, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, y), new Vector2(78f, 78f), RelevoUI.ConAlfa(RelevoUI.Ambar, 0.35f), false);
                nodoGlow[i, j].gameObject.SetActive(false);

                nodoRelleno[i, j] = RelevoUI.Hexagono(p.transform, "Nodo " + i + "_" + j, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, y), new Vector2(52f, 52f), RelevoUI.ConAlfa(Gris, 0.5f), true);

                nodoAnillo[i, j] = RelevoUI.Hexagono(p.transform, "Nodo anillo " + i + "_" + j, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, y), new Vector2(52f, 52f), RelevoUI.ConAlfa(Gris, 0.8f), false);

                // Tres piezas de selección, apagadas hasta que el nodo recibe el foco.
                nodoHalo[i, j] = RelevoUI.Hexagono(p.transform, "Halo " + i + "_" + j, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, y), new Vector2(88f, 88f), RelevoUI.Ambar, false);
                nodoHalo[i, j].gameObject.SetActive(false);

                nodoMarco[i, j] = RelevoUI.Hexagono(p.transform, "Marco " + i + "_" + j, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, y), new Vector2(64f, 64f), PapelClaro, false);
                nodoMarco[i, j].gameObject.SetActive(false);

                nodoPunta[i, j] = RelevoUI.Capsula(p.transform, "Punta " + i + "_" + j, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, y + 38f), new Vector2(12f, 12f), RelevoUI.Ambar);
                nodoPunta[i, j].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                nodoPunta[i, j].gameObject.SetActive(false);

                nodoTexto[i, j] = RelevoUI.Texto(p.transform, "Nodo texto " + i + "_" + j, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, y), new Vector2(52f, 52f), 15, TextAnchor.MiddleCenter, RelevoUI.ConAlfa(Gris, 0.95f));
                nodoTexto[i, j].font = RelevoUI.FuenteTitulo();
                nodoTexto[i, j].text = Romanos(j);
            }

            ramaProgreso[i] = RelevoUI.Texto(p.transform, "Progreso " + rama.nombre, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, -96f), new Vector2(paso - 2f, 20f), 13, TextAnchor.MiddleCenter,
                RelevoUI.ConAlfa(TintaSuave, 0.9f));
        }

        // ---------------- panel de descripción del nodo
        Image panel = RelevoUI.Franja(p.transform, "Panel nodo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -192f), new Vector2(AnchoDoble, 130f), RelevoUI.ConAlfa(PapelSombra, 0.92f));
        skPanelBordes = new List<Image>();
        RelevoUI.Marco(panel.transform, PapelHondo, 2f, skPanelBordes);
        RelevoUI.Esquinas(panel.transform, 16f, 2f, PapelHondo);

        skFilo = RelevoUI.Franja(panel.transform, "Filo", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(5f, 130f), RelevoUI.Ambar);

        skTitulo = RelevoUI.Texto(panel.transform, "Titulo", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -14f), new Vector2(420f, 28f), 22, TextAnchor.UpperLeft, Tinta);
        skTitulo.font = RelevoUI.FuenteTitulo();
        skTitulo.fontStyle = FontStyle.Bold;

        skChip = RelevoUI.Capsula(panel.transform, "Chip", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -50f), new Vector2(150f, 24f), RelevoUI.Ambar);

        skChipTexto = RelevoUI.Texto(panel.transform, "Chip texto", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -50f), new Vector2(150f, 24f), 12, TextAnchor.MiddleCenter, Esmalte);
        skChipTexto.fontStyle = FontStyle.Bold;

        skRama = RelevoUI.Texto(panel.transform, "Rama texto", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(190f, -50f), new Vector2(260f, 24f), 14, TextAnchor.MiddleLeft, TintaSuave);

        skDesc = RelevoUI.Texto(panel.transform, "Descripcion", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(455f, -12f), new Vector2(330f, 100f), 15, TextAnchor.UpperLeft, TintaSuave);

        skRequisito = RelevoUI.Texto(panel.transform, "Requisito", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-24f, -12f), new Vector2(320f, 22f), 14, TextAnchor.UpperRight, TintaSuave);

        skCoste = RelevoUI.Texto(panel.transform, "Coste", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-24f, -38f), new Vector2(320f, 22f), 14, TextAnchor.UpperRight,
            RelevoUI.ConAlfa(TintaSuave, 0.85f));

        skBarraFondo = RelevoUI.Franja(panel.transform, "Barra fondo", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-24f, -70f), new Vector2(320f, 10f), RelevoUI.ConAlfa(PapelHondo, 0.85f));

        skBarra = RelevoUI.Franja(skBarraFondo.transform, "Barra", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(320f, 10f), RelevoUI.Ambar);

        skBarraTexto = RelevoUI.Texto(panel.transform, "Barra texto", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-24f, -88f), new Vector2(320f, 20f), 13, TextAnchor.UpperRight, TintaSuave);

        return p;
    }

    static string Romanos(int indice)
    {
        switch (indice)
        {
            case 1: return "II";
            case 2: return "III";
            default: return "I";
        }
    }

    void RefrescarSkills()
    {
        Rama seleccionada = Ramas[ramaSel];

        for (int i = 0; i < RamasNum; i++)
        {
            Rama rama = Ramas[i];
            int progreso = ProgresoDeRama(rama);

            ramaEtiqueta[i].color = rama.estado[0] == 2
                ? RelevoUI.ConAlfa(rama.color, 0.95f)
                : RelevoUI.ConAlfa(TintaSuave, 0.8f);

            ramaProgreso[i].text = progreso + "%";
            ramaProgreso[i].color = progreso > 0 ? RelevoUI.ConAlfa(rama.color, 0.95f) : RelevoUI.ConAlfa(Gris, 0.9f);

            // El bus se ilumina hasta la primera rama bloqueada y sigue el color de su rama.
            bus[i].color = ColorDeEnlace(rama, 0);

            for (int j = 0; j < NodosRama; j++)
            {
                int estado = rama.estado[j];
                Color relleno, anillo, texto;

                if (estado == 2)
                {
                    relleno = RelevoUI.ConAlfa(rama.color, 0.9f);
                    anillo = RelevoUI.Aclarar(rama.color, 0.35f);
                    texto = Esmalte;
                }
                else if (estado == 1)
                {
                    relleno = RelevoUI.ConAlfa(rama.color, 0.22f);
                    anillo = RelevoUI.ConAlfa(rama.color, 0.9f);
                    texto = RelevoUI.Aclarar(rama.color, 0.15f);
                }
                else
                {
                    relleno = RelevoUI.ConAlfa(Gris, 0.30f);
                    anillo = RelevoUI.ConAlfa(Gris, 0.75f);
                    texto = RelevoUI.ConAlfa(Gris, 0.95f);
                }

                nodoRelleno[i, j].color = relleno;
                nodoAnillo[i, j].color = anillo;
                nodoTexto[i, j].color = texto;

                bool elegido = i == ramaSel && j == nivelSel;
                Color filo = RelevoUI.Aclarar(rama.color, 0.5f);

                // Halo suave + borde claro + rombo + escala: cuatro señales que se suman a la
                // posición, para que el nodo elegido se distinga aunque haya vecinos cerca.
                nodoGlow[i, j].color = RelevoUI.ConAlfa(rama.color, elegido ? 0.55f : 0.30f);
                nodoGlow[i, j].gameObject.SetActive(elegido);

                nodoHalo[i, j].color = RelevoUI.ConAlfa(filo, 0.95f);
                nodoHalo[i, j].gameObject.SetActive(elegido);

                nodoMarco[i, j].color = RelevoUI.ConAlfa(PapelClaro, 0.95f);
                nodoMarco[i, j].gameObject.SetActive(elegido);

                nodoPunta[i, j].color = filo;
                nodoPunta[i, j].gameObject.SetActive(elegido);

                float escala = elegido ? 1.12f : 1f;
                nodoRelleno[i, j].rectTransform.localScale = new Vector3(escala, escala, 1f);
                nodoAnillo[i, j].rectTransform.localScale = new Vector3(escala, escala, 1f);
                nodoTexto[i, j].rectTransform.localScale = new Vector3(escala, escala, 1f);

                if (j > 0) vertical[i, j - 1].color = ColorDeEnlace(rama, j);
            }
        }

        // Cable del nodo elegido hasta el panel de descripción, con su punta en el borde del panel:
        // así se ve de qué nodo habla el texto de abajo.
        float ySel = nivelSel == 0 ? SkYBus : (nivelSel == 1 ? SkYNivel1 : SkYNivel2);
        float xSel = SkX0 + ramaSel * SkPaso + 40f;
        float largo = Mathf.Max(8f, ySel - SkPanelTope);

        if (cableSeleccion != null)
        {
            cableSeleccion.rectTransform.sizeDelta = new Vector2(2f, largo);
            cableSeleccion.rectTransform.anchoredPosition = new Vector2(xSel, SkPanelTope + largo * 0.5f);
            cableSeleccion.color = RelevoUI.ConAlfa(seleccionada.color, 0.4f);
        }

        if (puntaPanel != null)
        {
            puntaPanel.rectTransform.anchoredPosition = new Vector2(xSel, SkPanelTope + 5f);
            puntaPanel.color = seleccionada.color;
        }

        if (skPanelBordes != null)
        {
            Color borde = RelevoUI.ConAlfa(seleccionada.color, 0.9f);
            for (int b = 0; b < skPanelBordes.Count; b++) if (skPanelBordes[b] != null) skPanelBordes[b].color = borde;
        }

        // Raíz: siempre desbloqueada, se ilumina cuando el foco está en el primer nodo.
        bool enRaiz = ramaSel == 0 && nivelSel == 0;
        raizAnillo.color = enRaiz ? RelevoUI.Ambar : RelevoUI.ConAlfa(RelevoUI.Ambar, 0.35f);
        raizEtiqueta.color = enRaiz ? Tinta : RelevoUI.ConAlfa(TintaSuave, 0.8f);

        // ---- descripción del nodo elegido
        int estadoSel = seleccionada.estado[nivelSel];

        skTitulo.text = seleccionada.nodos[nivelSel];
        skDesc.text = seleccionada.detalle[nivelSel];
        skRama.text = "Rama " + seleccionada.nombre + " · " + ProgresoDeRama(seleccionada) + "%";

        skChipTexto.text = estadoSel == 2 ? "DESBLOQUEADO" : (estadoSel == 1 ? "DISPONIBLE" : "BLOQUEADO");
        skChip.color = estadoSel == 2 ? RelevoUI.ConAlfa(seleccionada.color, 0.95f)
            : (estadoSel == 1 ? RelevoUI.ConAlfa(seleccionada.color, 0.45f) : RelevoUI.ConAlfa(Gris, 0.7f));
        skChipTexto.color = estadoSel == 2 ? Esmalte : Tinta;

        skRequisito.text = nivelSel == 0
            ? "Requiere · raíz NILO"
            : "Requiere · " + seleccionada.nodos[nivelSel - 1];

        skCoste.text = "Coste · — (por definir)";

        float fraccion = ProgresoDeRama(seleccionada) / 100f;
        skBarra.rectTransform.sizeDelta = new Vector2(320f * fraccion, 10f);
        skBarra.color = seleccionada.color;
        skBarraTexto.text = "Progreso de la rama · " + ProgresoDeRama(seleccionada) + "%";

        if (skBarraFondo != null) skBarraFondo.color = RelevoUI.ConAlfa(PapelHondo, 0.85f);

        // El filo del panel y la etiqueta de la rama toman su color.
        if (skFilo != null) skFilo.color = seleccionada.color;
    }

    static int ProgresoDeRama(Rama rama)
    {
        int hechos = 0;
        for (int i = 0; i < rama.estado.Length; i++) if (rama.estado[i] == 2) hechos++;
        return Mathf.RoundToInt(hechos * 100f / rama.estado.Length);
    }

    static Color ColorDeEnlace(Rama rama, int nivel)
    {
        int estado = rama.estado[nivel];
        if (estado == 2) return RelevoUI.ConAlfa(rama.color, 0.9f);
        if (estado == 1) return RelevoUI.ConAlfa(rama.color, 0.45f);
        return RelevoUI.ConAlfa(TintaSuave, 0.25f);
    }

    // ------------------------------------------------------------------ página EQUIPAMIENTO

    GameObject PaginaEquipamiento()
    {
        GameObject p = NuevaPagina("Pagina equipamiento");

        Text cabecera = RelevoUI.Texto(p.transform, "Cabecera", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, 232f), new Vector2(AnchoPagina, 24f), 14, TextAnchor.MiddleLeft, TintaSuave);
        cabecera.text = "SEIS PIEZAS · NINGUNA EQUIPADA";

        for (int i = 0; i < equipo.Length; i++)
        {
            int columna = i % 3;
            int fila = i / 3;

            float x = ColIzq + (columna - 1) * 185f;
            float y = fila == 0 ? 96f : -104f;

            Slot slot = new Slot();
            slot.fondo = RelevoUI.Franja(p.transform, "Slot " + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, y), new Vector2(170f, 176f), RelevoUI.ConAlfa(PapelSombra, 0.9f));

            // Cada recorte está pegado un poco torcido, como en una libreta de campo.
            slot.fondo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? -1.6f : 1.6f));

            List<Image> filetes = new List<Image>();
            RelevoUI.Marco(slot.fondo.transform, PapelHondo, 1f, filetes);
            slot.filetes = filetes.ToArray();

            RelevoUI.Franja(slot.fondo.transform, "Celo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 4f), new Vector2(64f, 16f), RelevoUI.ConAlfa(RelevoUI.Tinta, 0.10f));

            slot.anillo = RelevoUI.Hexagono(slot.fondo.transform, "Anillo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -54f), new Vector2(66f, 66f), RelevoUI.ConAlfa(TintaSuave, 0.35f), false);

            slot.nombre = RelevoUI.Texto(slot.fondo.transform, "Nombre", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 42f), new Vector2(154f, 40f), 15, TextAnchor.LowerCenter, Tinta);
            slot.nombre.text = Equipo[i];

            slot.estado = RelevoUI.Texto(slot.fondo.transform, "Estado", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(154f, 20f), 12, TextAnchor.LowerCenter,
                RelevoUI.ConAlfa(TintaSuave, 0.85f));
            slot.estado.text = "sin equipar";

            equipo[i] = slot;
        }

        // ---------------- página derecha: la pieza elegida
        eqAnillo = RelevoUI.Hexagono(p.transform, "Eq anillo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 130f), new Vector2(110f, 110f), RelevoUI.ConAlfa(TintaSuave, 0.5f), false);

        Enlace(p.transform, "Eq cable", new Vector2(ColDer - 190f, 130f), new Vector2(ColDer + 190f, 130f), 2f,
            RelevoUI.ConAlfa(PapelHondo, 0.9f));

        eqTitulo = RelevoUI.Texto(p.transform, "Eq titulo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 40f), new Vector2(AnchoPagina, 32f), 23, TextAnchor.MiddleLeft, Tinta);
        eqTitulo.font = RelevoUI.FuenteTitulo();
        eqTitulo.fontStyle = FontStyle.Bold;

        eqEstado = RelevoUI.Texto(p.transform, "Eq estado", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 12f), new Vector2(AnchoPagina, 22f), 14, TextAnchor.MiddleLeft, Sello);
        eqEstado.text = "sin equipar · ranura preparada";

        RelevoUI.Franja(p.transform, "Eq filete", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -6f), new Vector2(AnchoPagina, 1f), RelevoUI.ConAlfa(PapelHondo, 0.8f));

        eqDesc = RelevoUI.Texto(p.transform, "Eq desc", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -70f), new Vector2(AnchoPagina, 100f), 16, TextAnchor.UpperLeft, TintaSuave);

        eqRequisito = RelevoUI.Texto(p.transform, "Eq requisito", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -150f), new Vector2(AnchoPagina, 22f), 14, TextAnchor.MiddleLeft,
            RelevoUI.ConAlfa(TintaSuave, 0.85f));
        eqRequisito.text = "Requisito · — (por definir)";

        RelevoUI.Texto(p.transform, "Eq pie", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -196f), new Vector2(AnchoPagina, 44f), 14, TextAnchor.UpperLeft,
            RelevoUI.ConAlfa(TintaSuave, 0.75f)).text =
            "Ninguna pieza está implementada todavía: la ranura existe, el objeto llegará después.\nNo hay bonificaciones activas.";

        return p;
    }

    void RefrescarEquipamiento()
    {
        for (int i = 0; i < equipo.Length; i++)
        {
            bool elegido = i == equipoSel;

            equipo[i].fondo.color = elegido ? PapelClaro : RelevoUI.ConAlfa(PapelSombra, 0.9f);
            for (int f = 0; f < equipo[i].filetes.Length; f++)
                equipo[i].filetes[f].color = elegido ? Sello : PapelHondo;

            equipo[i].anillo.color = elegido ? RelevoUI.ConAlfa(Sello, 0.9f) : RelevoUI.ConAlfa(TintaSuave, 0.35f);
            equipo[i].estado.color = elegido ? Sello : RelevoUI.ConAlfa(TintaSuave, 0.85f);
            equipo[i].fondo.rectTransform.localRotation = Quaternion.Euler(0f, 0f,
                elegido ? 0f : (i % 2 == 0 ? -1.6f : 1.6f));
        }

        eqTitulo.text = Equipo[equipoSel];
        eqDesc.text = DetalleEquipo[equipoSel];
        eqAnillo.color = RelevoUI.ConAlfa(Sello, 0.55f);
    }

    // ------------------------------------------------------------------ página ESTADO

    GameObject PaginaEstado()
    {
        GameObject p = NuevaPagina("Pagina estado");

        // ---------------- página izquierda: el mapa del canal con sus anotaciones
        Image mapa = RelevoUI.Franja(p.transform, "Mapa", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, 92f), new Vector2(AnchoPagina, 300f), RelevoUI.ConAlfa(PapelSombra, 0.75f));
        RelevoUI.Marco(mapa.transform, PapelHondo, 2f);
        RelevoUI.Esquinas(mapa.transform, 16f, 2f, PapelHondo);

        RelevoUI.Texto(mapa.transform, "Mapa titulo", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -10f), new Vector2(400f, 22f), 14, TextAnchor.UpperLeft, TintaSuave).text = "MAPA DEL CANAL";

        // Tres tramos dibujados a mano: muelle, patio y faro.
        Enlace(mapa.transform, "Ruta 1", new Vector2(-210f, 40f), new Vector2(-70f, 84f), 3f, RelevoUI.ConAlfa(TintaSuave, 0.6f));
        Enlace(mapa.transform, "Ruta 2", new Vector2(-70f, 84f), new Vector2(70f, 30f), 3f, RelevoUI.ConAlfa(TintaSuave, 0.6f));
        Enlace(mapa.transform, "Ruta 3", new Vector2(70f, 30f), new Vector2(212f, 76f), 3f, RelevoUI.ConAlfa(TintaSuave, 0.6f));
        Enlace(mapa.transform, "Canal", new Vector2(-230f, -20f), new Vector2(230f, -8f), 2f, RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.35f));

        Mancha(mapa.transform, "Mancha mapa", new Vector2(150f, -70f), new Vector2(170f, 90f),
            RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.16f), 10f);

        for (int i = 0; i < 3; i++)
        {
            float x = -210f + i * 140f;
            float y = i == 0 ? 40f : (i == 1 ? 84f : 30f);

            RelevoUI.Hexagono(mapa.transform, "Hito " + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, y), new Vector2(26f, 26f), RelevoUI.ConAlfa(PapelHondo, 0.9f), true);

            RelevoUI.Texto(mapa.transform, "Hito texto " + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, y - 26f), new Vector2(120f, 20f), 12, TextAnchor.MiddleCenter, TintaSuave).text =
                i == 0 ? "muelle" : (i == 1 ? "patio" : "faro");
        }

        // El hito del tramo en curso: un conector ámbar encima del mapa, sobre el tramo real.
        hitoActual = RelevoUI.Hexagono(p.transform, "Hito actual", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq - 210f, 158f), new Vector2(22f, 22f), RelevoUI.Ambar, true);
        hitoActualTexto = RelevoUI.Texto(p.transform, "Hito actual texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq - 210f, 134f), new Vector2(150f, 20f), 12, TextAnchor.MiddleCenter, Sello);
        hitoActualTexto.text = "Nilo aquí";

        Text notas = RelevoUI.Texto(p.transform, "Notas", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, -150f), new Vector2(AnchoPagina, 130f), 15, TextAnchor.UpperLeft, TintaSuave);
        notas.text = "«Turquesa es manual; ámbar, prestado.»\n" +
                     "«Un tramo en contra corta el camino entero.»\n\n" +
                     "Anotado por Maia en el margen de la página 3.";

        // ---------------- página derecha: el parte del tramo
        Text cabecera = RelevoUI.Texto(p.transform, "Cabecera", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 232f), new Vector2(AnchoPagina, 24f), 14, TextAnchor.MiddleLeft, TintaSuave);
        cabecera.text = "PARTE DEL TRAMO";

        string[] etiquetas =
        {
            "Nivel actual", "Batería", "Reserva de Nilo", "Puentes eléctricos",
            "Compuertas", "Recuerdos", "Objetivo en curso", "Estado del tramo"
        };

        for (int i = 0; i < etiquetas.Length; i++)
        {
            float y = 184f - i * 56f;

            Image fila = RelevoUI.Franja(p.transform, "Fila estado " + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(ColDer, y), new Vector2(AnchoPagina, 50f), RelevoUI.ConAlfa(PapelSombra, 0.7f));
            RelevoUI.Marco(fila.transform, RelevoUI.ConAlfa(PapelHondo, 0.85f), 1f);

            estadoFilo[i] = RelevoUI.Franja(fila.transform, "Filo", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(4f, 50f), RelevoUI.Cobre);

            RelevoUI.Texto(fila.transform, "Etiqueta", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(18f, 11f), new Vector2(230f, 20f), 14, TextAnchor.MiddleLeft, Tinta).text = etiquetas[i];

            estadoValor[i] = RelevoUI.Texto(fila.transform, "Valor", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(18f, -12f), new Vector2(524f, 22f), 15, TextAnchor.MiddleLeft, TintaSuave);
        }

        return p;
    }

    void RefrescarEstado()
    {
        LevelManager nivel = RelevoGame.Nivel;
        BatteryController bateria = RelevoGame.Bateria;
        ReservaEnergia reserva = RelevoGame.Reserva;

        estadoValor[0].text = nivel != null ? nivel.NombreNivel : "—";
        estadoFilo[0].color = RelevoUI.Cobre;

        // El hito del mapa se mueve al tramo en curso; nunca cambia de página, solo de sitio.
        int indice = nivel != null ? Mathf.Clamp(nivel.IndiceNivel, 1, 3) : 1;
        Vector2[] hitos = { new Vector2(-520f, 158f), new Vector2(-380f, 202f), new Vector2(-240f, 148f) };
        if (hitoActual != null) hitoActual.rectTransform.anchoredPosition = hitos[indice - 1];
        if (hitoActualTexto != null) hitoActualTexto.rectTransform.anchoredPosition = hitos[indice - 1] + new Vector2(0f, -24f);

        if (bateria == null)
        {
            estadoValor[1].text = "sin batería en este tramo";
        }
        else if (bateria.EnManos)
        {
            estadoValor[1].text = "en manos de Nilo";
        }
        else if (bateria.BaseActual != null)
        {
            estadoValor[1].text = "acoplada en " + bateria.BaseActual.Etiqueta;
        }
        else
        {
            estadoValor[1].text = "en el suelo";
        }
        estadoFilo[1].color = RelevoUI.Ambar;

        if (reserva == null)
        {
            estadoValor[2].text = "este tramo no usa reserva";
            estadoFilo[2].color = RelevoUI.ConAlfa(RelevoUI.Ambar, 0.5f);
        }
        else
        {
            estadoValor[2].text = Mathf.RoundToInt(reserva.Actual) + " de " + Mathf.RoundToInt(reserva.Maximo)
                + " casillas  ·  " + Mathf.RoundToInt(reserva.Fraccion * 100f) + "%"
                + (reserva.Apagado ? "  ·  APAGADO" : (reserva.EnZona ? "  ·  recargando" : ""));
            estadoFilo[2].color = reserva.Apagado ? RelevoUI.Coral : RelevoUI.Ambar;
        }

        int puentes = 0, desplegados = 0;
        if (nivel != null)
        {
            for (int i = 0; i < nivel.Puentes.Count; i++)
            {
                if (nivel.Puentes[i] == null) continue;
                puentes++;
                if (nivel.Puentes[i].Desplegado) desplegados++;
            }
        }
        estadoValor[3].text = puentes == 0
            ? "no hay puentes en este tramo"
            : desplegados + " de " + puentes + " desplegados";
        estadoFilo[3].color = RelevoUI.Turquesa;

        Compuerta[] compuertas = Object.FindObjectsByType<Compuerta>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int abiertas = 0;
        for (int i = 0; i < compuertas.Length; i++)
        {
            if (compuertas[i] != null && compuertas[i].Abierta) abiertas++;
        }
        estadoValor[4].text = compuertas.Length == 0
            ? "no hay compuertas en este tramo"
            : abiertas + " de " + compuertas.Length + " abiertas";
        estadoFilo[4].color = RelevoUI.Turquesa;

        int encontrados = 0;
        int leidos = 0;
        for (int i = 1; i <= Niveles; i++)
        {
            LevelDefinition d = DefinicionesPorDefecto.Obtener(i);
            if (d == null) continue;
            if (SaveManager.TieneRecuerdo(d.recuerdoIdentificador)) encontrados++;
            if (SaveManager.RecuerdoLeido(d.recuerdoIdentificador)) leidos++;
        }
        estadoValor[5].text = encontrados + " de " + Niveles + " encontrados  ·  " + leidos + " leídos en el cuaderno";
        estadoFilo[5].color = RelevoUI.Verde;

        estadoValor[6].text = RelevoGame.UI != null && !string.IsNullOrEmpty(RelevoGame.UI.ObjetivoActual)
            ? RelevoGame.UI.ObjetivoActual
            : "sin objetivo activo";
        estadoFilo[6].color = RelevoUI.Cobre;

        int palancas = 0, accionadas = 0;
        if (nivel != null)
        {
            for (int i = 0; i < nivel.Palancas.Count; i++)
            {
                if (nivel.Palancas[i] == null) continue;
                palancas++;
                if (nivel.Palancas[i].Accionada) accionadas++;
            }
        }

        bool completado = nivel != null && nivel.Completado;
        estadoValor[7].text = completado
            ? "restaurado · este tramo vuelve a tener energía"
            : (palancas == 0
                ? "en curso · sin palancas en este tramo"
                : "en curso · " + accionadas + " de " + palancas + " palancas accionadas");
        estadoFilo[7].color = completado ? RelevoUI.Verde : RelevoUI.Coral;
    }

    // ------------------------------------------------------------------ refresco global

    void Refrescar()
    {
        if (panel == null) return;

        cabTitulo.text = "CUADERNO DE VIAJE";
        cabPie.text = Pies[seccion];

        for (int i = 0; i < Nombres.Length; i++)
        {
            bool elegida = i == seccion;

            // La pestaña elegida es de papel claro, se levanta y lleva su filo de color; las demás
            // quedan más oscuras y más bajas, como separadas del pliego.
            pestanas[i].color = elegida ? PapelClaro : PapelHondo;
            pestanas[i].rectTransform.anchoredPosition = new Vector2(PestanaX(i), elegida ? 26f : 8f);
            pestanas[i].rectTransform.sizeDelta = new Vector2(AnchoPestana, elegida ? 84f : 64f);

            pestanasTexto[i].color = elegida ? Tinta : RelevoUI.ConAlfa(TintaSuave, 0.85f);
            pestanasTexto[i].fontStyle = elegida ? FontStyle.Bold : FontStyle.Normal;
            pestanasTexto[i].rectTransform.anchoredPosition = pestanas[i].rectTransform.anchoredPosition;
            pestanasTexto[i].rectTransform.sizeDelta = pestanas[i].rectTransform.sizeDelta;

            pestanasSub[i].color = elegida ? Acento(i) : RelevoUI.ConAlfa(PapelClaro, 0.35f);
            pestanasSub[i].rectTransform.anchoredPosition =
                new Vector2(PestanaX(i), elegida ? 30f : 12f);
            pestanasSub[i].rectTransform.sizeDelta = new Vector2(AnchoPestana - 26f, 3f);

            if (paginas[i] != null) paginas[i].SetActive(elegida);
        }

        cursorPestana.gameObject.SetActive(zonaPestanas);
        cursorPestana.rectTransform.anchoredPosition = new Vector2(PestanaX(seccion), -14f);

        switch ((Seccion)seccion)
        {
            case Seccion.Cuaderno: RefrescarCuaderno(); break;
            case Seccion.Inventario: RefrescarInventario(); break;
            case Seccion.Skills: RefrescarSkills(); break;
            case Seccion.Equipamiento: RefrescarEquipamiento(); break;
            case Seccion.Estado: RefrescarEstado(); break;
        }
    }

    static float PestanaX(int indice)
    {
        return -PasoPestana * 2f + indice * PasoPestana;
    }

    // ------------------------------------------------------------------ transición de página

    /// <summary>
    /// Giro de hoja: la página entra torcida unos grados y se endereza mientras aparece. Usa tiempo
    /// NO escalado, así que funciona con Time.timeScale = 0 igual que el resto del cuaderno.
    /// </summary>
    void GirarPagina(GameObject destino, int sentido)
    {
        if (destino == null || !isActiveAndEnabled) return;

        if (paginando != null) StopCoroutine(paginando);

        CanvasGroup grupo = destino.GetComponent<CanvasGroup>();
        if (grupo == null) grupo = destino.AddComponent<CanvasGroup>();

        RectTransform rt = destino.GetComponent<RectTransform>();
        paginando = StartCoroutine(CorrutinaPagina(grupo, rt, sentido >= 0 ? 1f : -1f));
    }

    IEnumerator CorrutinaPagina(CanvasGroup grupo, RectTransform rt, float sentido)
    {
        const float duracion = 0.22f;
        float t = 0f;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float e = Mathf.Clamp01(t / duracion);
            float suave = 1f - (1f - e) * (1f - e);

            grupo.alpha = Mathf.Lerp(0f, 1f, suave);
            if (rt != null)
            {
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(3.4f * sentido, 0f, suave));
                rt.localScale = Vector3.one * Mathf.Lerp(0.988f, 1f, suave);
            }

            yield return null;
        }

        grupo.alpha = 1f;
        if (rt != null)
        {
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }

        paginando = null;
    }

    // ------------------------------------------------------------------ ayudantes

    GameObject NuevaPagina(string nombre)
    {
        GameObject p = new GameObject(nombre);
        p.transform.SetParent(pagina, false);

        RectTransform rt = p.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        p.SetActive(false);
        return p;
    }

    /// <summary>Tramo de línea entre dos puntos, en coordenadas del centro del padre. Sin remaches.</summary>
    static Image Enlace(Transform padre, string nombre, Vector2 desde, Vector2 hasta, float grosor, Color color)
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

    /// <summary>Mancha de agua: una cápsula translúcida, aplastada y girada.</summary>
    static Image Mancha(Transform padre, string nombre, Vector2 posicion, Vector2 tamano, Color color, float giro)
    {
        Image mancha = RelevoUI.Capsula(padre, nombre, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            posicion, tamano, color);
        mancha.rectTransform.localRotation = Quaternion.Euler(0f, 0f, giro);
        return mancha;
    }

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
}
