using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Atlas de Liria. Reúne las fichas de recuerdos, los mecanismos del tramo y la carta
/// de navegación. UIManager conserva la pausa y gestiona el cambio de puerto.
/// </summary>
public class MenuExploracionUI : MonoBehaviour
{
    /// <summary>Lo engancha UIManager para abrir la escena del cuaderno sin duplicar la lógica.</summary>
    public System.Action<int> AlAbrirCuaderno;

    /// <summary>Lo engancha UIManager para cerrar el overlay y devolver el tiempo.</summary>
    public System.Action AlCerrar;
    public System.Action AlAbrirCarta;

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

    const int Niveles = LevelLayouts.Cantidad;
    static readonly Vector2[] HitosCanal = {
        new Vector2(-205,77), new Vector2(-102,64), new Vector2(0,83), new Vector2(102,70), new Vector2(205,76),
        new Vector2(205,-20), new Vector2(102,-35), new Vector2(0,-22), new Vector2(-102,-38), new Vector2(-205,-22)
    };
    static readonly string[] Muelles = { "muelle", "patio", "faro", "aduana", "dársena", "esclusa", "lonja", "archivo", "astillero", "guardias" };

    static readonly string[] Nombres = { "RECUERDOS", "TALLER", "MAPA Y ESTADO" };
    static readonly string[] Pies = { "el cuaderno de Nilo", "mecanismos del tramo", "carta de navegación de Liria" };

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

    enum Seccion { Cuaderno, Taller, Estado }

    static readonly string[] Mecanismos = { "Batería", "Puentes eléctricos", "Palancas", "Cajas y placas", "Condensadores", "Red de conductores" };
    static readonly string[] Notas =
    {
        "Una base ámbar recibe la batería. Puedes recuperarla al regresar. Cerca de ella, Nilo repone la reserva que gasta al alejarse.",
        "El puente necesita corriente en su orilla. Si retiras la fuente, busca un paso de regreso o comprueba si queda carga en el condensador.",
        "Una palanca despliega una pasarela fija. Puedes retirar la batería y regresar con ella por el nuevo paso.",
        "Para empujar, ponte detrás de la caja y camina recto contra ella. Déjala sobre el rombo para que la placa siga activa cuando te alejes.",
        "Espera a que se llenen las barras antes de retirar la batería. La carga mantiene el puente unos segundos mientras cruzas con ella.",
        "Sigue el cobre desde la fuente. Gira los discos junto a Nilo hasta que los contactos se encuentren y la corriente llegue al borne del puente."
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
    PostalDeLiria detPostal;
    Text detTitulo;
    Text detOrigen;
    Text detFrase;
    Text detNarrativa;
    Text detLugar;

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

    readonly FilaInv[] categorias = new FilaInv[Mecanismos.Length];
    Text invTitulo;
    Text invNota;
    Text invCuenta;
    PostalDeLiria postalTaller;
    int mecanismoSel;

    // --- ESTADO
    readonly Text[] estadoValor = new Text[8];
    readonly Image[] estadoFilo = new Image[8];
    Image hitoActual, botonCarta;
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
        cabTitulo.text = "ATLAS DEL CANAL";

        cabPie = RelevoUI.Texto(hoja, "Cabecera pie", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -60f), new Vector2(AnchoDoble, 26f), 18, TextAnchor.MiddleCenter, TintaSuave);

        pagina = RelevoUI.Franja(hoja, "Paginas", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -60f), new Vector2(1180f, 520f), new Color(0f, 0f, 0f, 0f)).rectTransform;

        paginas[(int)Seccion.Cuaderno] = PaginaCuaderno();
        paginas[(int)Seccion.Taller] = PaginaTaller();
        paginas[(int)Seccion.Estado] = PaginaEstado();

        Pestanas();
        Pie();

        panel.SetActive(false);
    }

    public void Abrir()
    {
        if (panel == null) return;

        seccion = 0;
        entradaSel = Mathf.Clamp(RelevoGame.Nivel != null ? RelevoGame.Nivel.IndiceNivel - 1 : 0, 0, Niveles - 1);
        mecanismoSel = 0;
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
        if (ManejarRaton()) return;

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

            if (seccion == (int)Seccion.Cuaderno && AlAbrirCuaderno != null) AlAbrirCuaderno(entradaSel + 1);
            if (seccion == (int)Seccion.Estado && AlAbrirCarta != null) AlAbrirCarta();
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

        if (seccion == (int)Seccion.Taller)
        {
            if (horizontal) return;

            if (eje > 0f)
            {
                if (mecanismoSel == 0) zonaPestanas = true;
                else mecanismoSel--;
            }
            else if (mecanismoSel < Mecanismos.Length - 1) mecanismoSel++;

            Refrescar();
            return;
        }

    }

    bool ManejarRaton()
    {
        var raton = Mouse.current;
        if (raton == null) return false;
        var posicion = raton.position.ReadValue();
        bool pulsado = raton.leftButton.wasPressedThisFrame;
        if (pulsado)
        {
            for (int i = 0; i < pestanas.Length; i++)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(pestanas[i].rectTransform, posicion)) continue;
                zonaPestanas = false;
                if (i != seccion) CambiarSeccion(i - seccion);
                else Refrescar();
                return true;
            }
        }
        float rueda = raton.scroll.y.ReadValue();
        if (seccion == (int)Seccion.Cuaderno)
        {
            if (Mathf.Abs(rueda) > 0.01f)
            {
                entradaSel = Mathf.Clamp(entradaSel + (rueda < 0 ? 1 : -1), 0, Niveles - 1);
                Refrescar();
                return true;
            }
            if (!pulsado) return false;
            if (RectTransformUtility.RectangleContainsScreenPoint(detCta.rectTransform, posicion))
            {
                AlAbrirCuaderno?.Invoke(entradaSel + 1);
                return true;
            }
            for (int i = 0; i < entradas.Length; i++)
                if (entradas[i].raiz.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(entradas[i].fondo.rectTransform, posicion))
                {
                    entradaSel = i;
                    Refrescar();
                    return true;
                }
        }
        else if (pulsado && seccion == (int)Seccion.Taller)
        {
            for (int i = 0; i < categorias.Length; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(categorias[i].fondo.rectTransform, posicion))
                {
                    mecanismoSel = i;
                    Refrescar();
                    return true;
                }
        }
        else if (pulsado && seccion == (int)Seccion.Estado &&
                 RectTransformUtility.RectangleContainsScreenPoint(botonCarta.rectTransform, posicion))
        {
            AlAbrirCarta?.Invoke();
            return true;
        }
        return false;
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
            case Seccion.Taller: return RelevoUI.Cobre;
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
        cabeceraIzq.text = "PUERTOS DEL CANAL";

        recuento = RelevoUI.Texto(p.transform, "Recuento", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, 232f), new Vector2(AnchoPagina, 24f), 14, TextAnchor.MiddleRight, Sello);

        for (int i = 0; i < Niveles; i++)
        {
            entradas[i] = CrearEntrada(p.transform, i, 140f - (i % 3) * 150f);
        }

        // ---------------- página derecha: la entrada abierta
        detIlustracion = RelevoUI.Franja(p.transform, "Ilustracion", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 175f), new Vector2(AnchoPagina, 150f), RelevoUI.ConAlfa(PapelSombra, 0.75f));
        detPostal = PostalDeLiria.Crear(detIlustracion);

        detTitulo = RelevoUI.Texto(p.transform, "Detalle titulo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 62f), new Vector2(AnchoPagina, 34f), 24, TextAnchor.MiddleLeft, Tinta);
        detTitulo.font = RelevoUI.FuenteTitulo();
        detTitulo.fontStyle = FontStyle.Normal;

        detOrigen = RelevoUI.Texto(p.transform, "Detalle origen", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 34f), new Vector2(AnchoPagina, 22f), 14, TextAnchor.MiddleLeft, Sello);

        RelevoUI.Franja(p.transform, "Filete detalle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 18f), new Vector2(AnchoPagina, 1f), RelevoUI.ConAlfa(PapelHondo, 0.8f));

        detFrase = RelevoUI.Texto(p.transform, "Detalle frase", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -12f), new Vector2(AnchoPagina, 56f), 17, TextAnchor.MiddleLeft, Tinta);

        detNarrativa = RelevoUI.Texto(p.transform, "Detalle narrativa", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -96f), new Vector2(AnchoPagina, 104f), 17, TextAnchor.UpperLeft, TintaSuave);

        detLugar = RelevoUI.Texto(p.transform, "Detalle lugar", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer + 12f, -162f), new Vector2(AnchoPagina - 12f, 22f), 14, TextAnchor.MiddleLeft, TintaSuave);

        RelevoUI.Hexagono(p.transform, "Marca lugar", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer - (AnchoPagina * 0.5f) + 4f, -162f), new Vector2(14f, 14f), Sello, true);

        detCta = RelevoUI.Capsula(p.transform, "Cta", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -226f), new Vector2(AnchoPagina, 42f), RelevoUI.ConAlfa(Sello, 0.9f));

        detCtaTexto = RelevoUI.Texto(p.transform, "Cta texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -226f), new Vector2(AnchoPagina, 42f), 17, TextAnchor.MiddleCenter, PapelClaro);
        detCtaTexto.fontStyle = FontStyle.Bold;



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
        e.numero.text = (indice + 1).ToString("00");

        e.chip = RelevoUI.Capsula(e.raiz.transform, "Chip", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(128f, -12f), new Vector2(168f, 24f), RelevoUI.Ambar);

        e.chipTexto = RelevoUI.Texto(e.raiz.transform, "Chip texto", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(128f, -12f), new Vector2(168f, 24f), 13, TextAnchor.MiddleCenter, Esmalte);
        e.chipTexto.fontStyle = FontStyle.Bold;

        e.titulo = RelevoUI.Texto(e.raiz.transform, "Titulo", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(128f, -42f), new Vector2(AnchoPagina - 150f, 28f), 21, TextAnchor.UpperLeft, Tinta);
        e.titulo.font = RelevoUI.Fuente();
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
            LevelDefinition definicion = DefinicionesPorDefecto.FichaDelCuaderno(nivel);

            bool tiene = definicion != null && SaveManager.TieneRecuerdo(definicion.recuerdoIdentificador);
            bool leido = tiene && SaveManager.RecuerdoLeido(definicion.recuerdoIdentificador);

            if (tiene) encontrados++;
            if (leido) leidos++;

            Color acento = ColorDeNivel(nivel);
            Entrada e = entradas[i];
            e.raiz.SetActive(i / 3 == entradaSel / 3);
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

            e.titulo.text = definicion.tituloIntro;
            e.titulo.color = tiene ? Tinta : RelevoUI.ConAlfa(TintaSuave, 0.85f);
            e.lugar.text = tiene ? definicion.recuerdoTitulo : "Recuerdo por encontrar";

            e.pista.text = leido
                ? "abierto en el cuaderno"
                : (tiene ? "encontrado · ábrelo para leerlo" : "la página sigue en blanco");

            e.pista.color = leido ? RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.95f)
                : (tiene ? RelevoUI.ConAlfa(RelevoUI.Ambar, 0.95f) : RelevoUI.ConAlfa(TintaSuave, 0.8f));
            if (elegida)
            {
                e.fondo.color = new Color32(30, 69, 69, 255);
                e.titulo.color = PapelClaro;
                e.lugar.color = new Color32(238, 220, 180, 255);
                e.pista.color = new Color32(200, 221, 205, 255);
                e.anillo.color = e.numero.color = new Color32(239, 188, 109, 255);
            }

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
        detPostal.Mostrar(d);

        detTitulo.text = tiene ? d.recuerdoTitulo : "Recuerdo bloqueado";
        detTitulo.color = tiene ? Tinta : RelevoUI.ConAlfa(TintaSuave, 0.9f);

        detOrigen.text = "Nivel de origen · " + d.nombre;
        detOrigen.color = tiene ? Sello : Sello;

        detFrase.text = tiene
            ? d.recuerdoTexto
            : "«—»";
        detFrase.color = tiene ? Tinta : RelevoUI.ConAlfa(TintaSuave, 0.7f);

        detNarrativa.text = tiene
            ? d.textoIntro
            : "Falta el recuerdo de este puerto. Hay un destello en " + d.tituloIntro
              + ".";
        detNarrativa.text = HuellaDelCanal.NotaDeViaje(d.indice, detNarrativa.text);
        detNarrativa.color = tiene ? TintaSuave : TintaSuave;

        detLugar.text = "Lugar de origen · " + d.tituloIntro;

        detCta.color = tiene ? RelevoUI.ConAlfa(Sello, 0.9f) : RelevoUI.ConAlfa(PapelHondo, 0.7f);
        detCtaTexto.color = tiene ? PapelClaro : RelevoUI.ConAlfa(PapelClaro, 0.75f);
        detCtaTexto.text = tiene
            ? "Enter · abrir el cuaderno de viaje"
            : "Enter · abrir el cuaderno de viaje (entrada en blanco)";
    }

    // ------------------------------------------------------------------ página INVENTARIO

    GameObject PaginaTaller()
    {
        GameObject p = NuevaPagina("Pagina inventario");

        Text cabecera = RelevoUI.Texto(p.transform, "Cabecera", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColIzq, 232f), new Vector2(AnchoPagina, 24f), 14, TextAnchor.MiddleLeft, TintaSuave);
        cabecera.text = "MECANISMOS DEL TRAMO";

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
                new Vector2(72f, 12f), new Vector2(420f, 30f), 21, TextAnchor.MiddleLeft, Tinta);
            fila.nombre.font = RelevoUI.Fuente();
            fila.nombre.text = Mecanismos[i];

            fila.cuenta = RelevoUI.Texto(fila.fondo.transform, "Cuenta", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(72f, -15f), new Vector2(420f, 25f), 17, TextAnchor.MiddleLeft,
                RelevoUI.ConAlfa(TintaSuave, 0.9f));


            categorias[i] = fila;
        }

        // ---------------- página derecha: el cajón abierto
        invTitulo = RelevoUI.Texto(p.transform, "Inv titulo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 210f), new Vector2(AnchoPagina, 34f), 24, TextAnchor.MiddleLeft, Tinta);
        invTitulo.font = RelevoUI.FuenteTitulo();
        invTitulo.fontStyle = FontStyle.Normal;

        invCuenta = RelevoUI.Texto(p.transform, "Inv cuenta", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, 180f), new Vector2(AnchoPagina, 22f), 14, TextAnchor.MiddleLeft, Sello);
        var soporte = RelevoUI.Franja(p.transform, "Vista del tramo", Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(ColDer, 60f), new Vector2(AnchoPagina, 170f), Esmalte);
        postalTaller = PostalDeLiria.Crear(soporte);

        invNota = RelevoUI.Texto(p.transform, "Inv nota", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -96f), new Vector2(AnchoPagina, 120f), 21, TextAnchor.UpperLeft, TintaSuave);

        RelevoUI.Franja(p.transform, "Inv filete", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(ColDer, -172f), new Vector2(AnchoPagina, 1f), RelevoUI.ConAlfa(PapelHondo, 0.8f));

        return p;
    }

    void RefrescarTaller()
    {
        var nivel = RelevoGame.Nivel;
        if (nivel == null) return;
        var bateria = RelevoGame.Bateria;
        var reservas = RelevoGame.Reserva;
        int puentes = nivel.Puentes.FindAll(x => x != null && x.Desplegado).Count;
        int palancas = nivel.Palancas.FindAll(x => x != null && x.Accionada).Count;
        int placas = PlacaPresion.Todas.FindAll(x => x != null && x.Presionada).Count;
        int torres = 0, carga = 0;
        foreach (var toma in nivel.Bases)
        {
            if (toma == null || toma.Condensador == null) continue;
            torres++;
            carga = Mathf.Max(carga, Mathf.RoundToInt(100f * toma.Condensador.Carga));
        }
        int discos = RelevoGame.Layout != null && RelevoGame.Layout.TieneCasilla('t')
            ? Object.FindObjectsByType<PruebaCircuitoPieza>(FindObjectsSortMode.None).Length : 0;
        string[] lecturas =
        {
            bateria == null ? "Sin batería" : (bateria.EnManos ? "Nilo la lleva" : (bateria.BaseActual != null ? "En la base " + bateria.BaseActual.Identificador : "En el suelo")),
            puentes + " de " + nivel.Puentes.Count + " desplegados",
            palancas + " de " + nivel.Palancas.Count + " accionadas",
            CajaEmpujable.Todas.Count + " cajas · " + placas + " de " + PlacaPresion.Todas.Count + " placas activas",
            torres == 0 ? "Ausentes en este puerto" : (torres == 1 ? "Carga actual " : "Mayor carga del tramo ") + carga + " %",
            discos == 0 ? "Sin discos en este tramo" : discos + " discos giratorios"
        };
        postalTaller.Mostrar(nivel.Definicion);
        for (int i = 0; i < categorias.Length; i++)
        {
            bool elegida = i == mecanismoSel;
            categorias[i].fondo.color = elegida ? PapelClaro : RelevoUI.ConAlfa(PapelSombra, 0.85f);
            for (int f = 0; f < categorias[i].filetes.Length; f++)
                categorias[i].filetes[f].color = elegida ? Sello : PapelHondo;

            categorias[i].icono.color = elegida ? Sello : RelevoUI.Cobre;
            categorias[i].cuenta.text = lecturas[i];
            if (elegida) categorias[i].fondo.color = Esmalte;
            categorias[i].nombre.color = elegida ? PapelClaro : Tinta;
            categorias[i].cuenta.color = elegida ? PapelClaro : TintaSuave;
            categorias[i].icono.color = elegida ? RelevoUI.Ambar : RelevoUI.Cobre;
        }

        invTitulo.text = Mecanismos[mecanismoSel];
        invCuenta.text = lecturas[mecanismoSel];
        invNota.font = RelevoUI.Fuente();
        invNota.text = Notas[mecanismoSel];
        if (mecanismoSel == 0 && (reservas == null || reservas.Maximo <= 0))
            invNota.text = "Recoge la batería y colócala en una base ámbar para alimentar el puente. Vuelve por ella cuando el camino de regreso esté abierto.";
        if (mecanismoSel == 2 && nivel.IndiceNivel == 7)
            invNota.text = nivel.RamalLonja == DestinoCorriente.Ninguno
                ? "Solo una palanca recibirá corriente. El mercado queda al norte y la emisora al sur."
                : HuellaDelCanal.Anotacion(nivel.RamalLonja);
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

        for (int i = 1; i < HitosCanal.Length; i++)
            Enlace(mapa.transform, "Ruta " + i, HitosCanal[i - 1], HitosCanal[i], 3f, RelevoUI.ConAlfa(TintaSuave, 0.6f));
        Enlace(mapa.transform, "Canal", new Vector2(-230f, -20f), new Vector2(230f, -8f), 2f, RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.35f));

        Mancha(mapa.transform, "Mancha mapa", new Vector2(150f, -70f), new Vector2(170f, 90f),
            RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.16f), 10f);

        for (int i = 0; i < HitosCanal.Length; i++)
        {
            float x = HitosCanal[i].x;
            float y = HitosCanal[i].y;

            RelevoUI.Hexagono(mapa.transform, "Hito " + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, y), new Vector2(26f, 26f), RelevoUI.ConAlfa(PapelHondo, 0.9f), true);

            RelevoUI.Texto(mapa.transform, "Hito texto " + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, y - 26f), new Vector2(120f, 20f), 12, TextAnchor.MiddleCenter, TintaSuave).text =
                Muelles[i];
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
                     "Transcripción de la radio del faro. La fecha quedó bajo una mancha de sal.";

        botonCarta = RelevoUI.Capsula(p.transform, "Elegir puerto", Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(ColIzq, -232f), new Vector2(AnchoPagina, 44f), Esmalte);
        RelevoUI.Texto(botonCarta.transform, "Etiqueta", Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.zero, new Vector2(AnchoPagina, 42f), 20, TextAnchor.MiddleCenter, PapelClaro).text = "Elegir puerto";

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
        int indice = nivel != null ? Mathf.Clamp(nivel.IndiceNivel, 1, Niveles) : 1;
        Vector2 hito = new Vector2(ColIzq, 92f) + HitosCanal[indice - 1];
        if (hitoActual != null) hitoActual.rectTransform.anchoredPosition = hito;
        if (hitoActualTexto != null) hitoActualTexto.rectTransform.anchoredPosition = hito + new Vector2(0f, 23f);

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
            LevelDefinition d = DefinicionesPorDefecto.FichaDelCuaderno(i);
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
        if (nivel != null && nivel.IndiceNivel == 7 && nivel.RamalLonja != DestinoCorriente.Ninguno && !completado)
            estadoValor[7].text = "Corriente hacia " + (nivel.RamalLonja == DestinoCorriente.Mercado ? "el mercado" : "la emisora");
        estadoFilo[7].color = completado ? RelevoUI.Verde : RelevoUI.Coral;
    }

    // ------------------------------------------------------------------ refresco global

    void Refrescar()
    {
        if (panel == null) return;

        cabTitulo.text = "ATLAS DEL CANAL";
        cabPie.text = Pies[seccion];
        pieTexto.text = zonaPestanas ? "← →  Cambiar de pestaña      ·      ↓  Entrar      ·      Esc / Tab  Volver"
            : (seccion == (int)Seccion.Estado ? "Enter  Elegir puerto      ·      ↑  Pestañas      ·      Esc / Tab  Volver"
            : "↑ ↓  Elegir      ·      ↑ desde el inicio  Pestañas      ·      Esc / Tab  Volver" + (seccion == (int)Seccion.Cuaderno ? "      ·      Enter  Abrir recuerdo      ·      Rueda  Pasar página" : ""));

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
            case Seccion.Taller: RefrescarTaller(); break;
            case Seccion.Estado: RefrescarEstado(); break;
        }
    }

    static float PestanaX(int indice)
    {
        return -PasoPestana * (Nombres.Length - 1) * 0.5f + indice * PasoPestana;
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
