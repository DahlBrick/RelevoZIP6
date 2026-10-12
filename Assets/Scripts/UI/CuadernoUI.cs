using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cuaderno de viaje: la pantalla que recoge los recuerdos encontrados.
///
/// No pertenece a ningún nivel y no lleva LevelBootstrap, así que se construye entera por código
/// —igual que UIManager— y no depende de que exista un mapa: lee el progreso directamente de
/// SaveManager y vuelve a la escena desde la que se abrió, que LevelManager dejó apuntada en
/// EscenaOrigen antes de cargar esta.
///
/// Sigue la identidad de Liria: papel azul verdoso oscuro, filetes de cobre, un conector hexagonal
/// por tramo (cobre, turquesa y ámbar, los materiales de cada nivel) y la tarjeta con el foco en
/// ámbar. Cada tarjeta lleva su miniatura procedural, su chip de estado y su recuerdo; el pie
/// señala cuál es el siguiente destino.
///
/// La lógica de desbloqueo y de guardado NO cambia: SaveManager solo se consulta.
///
/// Se navega con el asset de entrada (mover elige tarjeta; confirmar, interactuar, cancelar,
/// cuaderno o pausa cierran), sin EventSystem, para que el cuaderno use exactamente los mismos
/// controles que el resto del juego.
/// </summary>
public class CuadernoUI : MonoBehaviour
{
    [Tooltip("Assets/Input/Relevo.inputactions")]
    [SerializeField] InputActionAsset acciones;

    /// <summary>Niveles con recuerdo propio. Coincide con LevelLayouts.Obtener().</summary>
    const int Niveles = 3;

    const float AnchoTarjeta = 372f;
    const float AltoTarjeta = 500f;
    const float Separacion = 24f;
    const float AlturaHoja = 840f;

    /// <summary>Tarjeta de un tramo: fondo, marco, sello, textos y marca del recuerdo.</summary>
    class Tarjeta
    {
        public GameObject raiz;
        public Image fondo;
        public Image[] filetes;
        public Image indicador;
        public Text sello;
        public Image chipFondo;
        public Text chipTexto;
        public Text titulo;
        public Text recuerdoTitulo;
        public Text cuerpo;
        public Image marcaRecuerdo;
        public Text marcaRecuerdoTexto;
    }

    RelevoInput entrada;

    GameObject hoja;
    Tarjeta[] tarjetas;
    Text textoRecuento;
    Image franjaSiguiente;
    Image selloSiguiente;
    Text textoSiguiente;

    int seleccion;
    bool flanco;

    void Awake()
    {
        // Se llega aquí desde la pausa, que congela el tiempo: sin devolverlo, esta pantalla no
        // respondería a la entrada.
        Time.timeScale = 1f;

        entrada = new RelevoInput(acciones);
        Construir();
        Refrescar();
        RelevoUIAnim.Entrada(hoja, 0.26f, -26f, true);
    }

    void Update()
    {
        if (entrada == null) return;

        // Se sale con la misma tecla con la que se abre (N), con el interactuar del nivel (E) y con
        // cualquiera de los botones de confirmar o cancelar.
        if (entrada.Pulsado(entrada.Confirmar)
            || entrada.Pulsado(entrada.Interactuar)
            || entrada.Pulsado(entrada.Cancelar)
            || entrada.Pulsado(entrada.Cuaderno)
            || entrada.Pulsado(entrada.Pausa))
        {
            Volver();
            return;
        }

        Navegar();
    }

    /// <summary>Las flechas mueven el foco entre las tarjetas, en ciclo y sin salir de la pantalla.</summary>
    void Navegar()
    {
        Vector2 mover = entrada.LeerMovimiento();

        // Horizontal si domina, y en vertical se sube con "arriba": el eje Y del input crece hacia
        // arriba, así que se invierte para que ↑ vaya a la tarjeta anterior.
        float eje = Mathf.Abs(mover.x) >= Mathf.Abs(mover.y) ? mover.x : -mover.y;

        if (Mathf.Abs(eje) < 0.5f)
        {
            flanco = false;
            return;
        }

        if (flanco) return;
        flanco = true;

        seleccion += eje > 0f ? 1 : -1;
        if (seleccion < 0) seleccion = Niveles - 1;
        if (seleccion >= Niveles) seleccion = 0;

        Refrescar();
    }

    void Volver()
    {
        if (entrada != null) entrada.Desactivar();
        Time.timeScale = 1f;

        // Sin origen no hay a dónde volver: pasa al abrir Cuaderno.unity directamente en el editor.
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
        canvas.sortingOrder = 10;

        CanvasScaler escalador = lienzo.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        lienzo.AddComponent<GraphicRaycaster>();

        Transform raiz = lienzo.transform;

        // El cuaderno es una pantalla propia, no un panel sobre el nivel: la hoja cubre el encuadre
        // entero y tapa el diorama que quedó detrás.
        RelevoUI.Panel(raiz, "Fondo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(4000f, 4000f), RelevoUI.ConAlfa(RelevoUI.FondoHondo, 0.97f));

        hoja = RelevoUI.Panel(raiz, "Hoja", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1200f, AlturaHoja), RelevoUI.Fondo);

        Transform papel = hoja.transform;
        RelevoUI.Marco(papel, RelevoUI.Cobre, 2f);
        RelevoUI.Esquinas(papel, 26f, 2f, RelevoUI.Cobre);

        Cabecera(papel);
        Tarjetas(papel);
        SiguienteDestino(papel);
        Leyenda(papel);
    }

    void Cabecera(Transform papel)
    {
        RelevoUI.Franja(papel, "Franja superior", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(1200f, 4f), RelevoUI.ConAlfa(RelevoUI.Ambar, 0.85f));

        Text titulo = RelevoUI.Texto(papel, "Titulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -24f), new Vector2(1120f, 56f), 42, TextAnchor.UpperCenter, RelevoUI.Ambar);
        titulo.text = "CUADERNO DE VIAJE";
        titulo.fontStyle = FontStyle.Bold;

        textoRecuento = RelevoUI.Texto(papel, "Recuento", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -84f), new Vector2(1120f, 26f), 19, TextAnchor.UpperCenter,
            RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.92f));

        // Cable con sus dos remaches: separa la cabecera de las tarjetas.
        RelevoUI.Cable(papel, "Cable", new Vector2(-540f, 298f), new Vector2(540f, 298f), 2f, RelevoUI.CobreTenue);
    }

    void Tarjetas(Transform papel)
    {
        tarjetas = new Tarjeta[Niveles];

        float paso = AnchoTarjeta + Separacion;

        for (int i = 0; i < Niveles; i++)
        {
            tarjetas[i] = CrearTarjeta(papel, i + 1, (i - 1) * paso);
        }
    }

    Tarjeta CrearTarjeta(Transform papel, int nivel, float x)
    {
        LevelDefinition definicion = DefinicionesPorDefecto.Obtener(nivel);
        Color acento = ColorDeNivel(nivel);

        Tarjeta tarjeta = new Tarjeta();

        tarjeta.raiz = RelevoUI.Panel(papel, "Tarjeta " + nivel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(x, -156f), new Vector2(AnchoTarjeta, AltoTarjeta), RelevoUI.FondoHondo);
        tarjeta.fondo = tarjeta.raiz.GetComponent<Image>();

        List<Image> filetes = new List<Image>();
        RelevoUI.Marco(tarjeta.raiz.transform, RelevoUI.CobreTenue, 2f, filetes);
        tarjeta.filetes = filetes.ToArray();
        RelevoUI.Esquinas(tarjeta.raiz.transform, 20f, 2f, RelevoUI.CobreTenue);

        Transform cara = tarjeta.raiz.transform;

        // --- miniatura: el conector del tramo con su sello y un tramo de cable
        GameObject miniatura = RelevoUI.Panel(cara, "Miniatura", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -14f), new Vector2(340f, 140f), RelevoUI.ConAlfa(RelevoUI.Fondo, 0.95f));
        RelevoUI.Marco(miniatura.transform, RelevoUI.CobreTenue, 1f);

        RelevoUI.Hexagono(miniatura.transform, "Anillo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-104f, 0f), new Vector2(84f, 84f), acento, false);

        tarjeta.sello = RelevoUI.Texto(miniatura.transform, "Sello", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-104f, 0f), new Vector2(84f, 84f), 34, TextAnchor.MiddleCenter, acento);
        tarjeta.sello.text = nivel.ToString();
        tarjeta.sello.fontStyle = FontStyle.Bold;

        RelevoUI.Cable(miniatura.transform, "Cable", new Vector2(-50f, 0f), new Vector2(136f, 0f), 2f, RelevoUI.CobreTenue);

        // --- chip de estado
        tarjeta.chipFondo = RelevoUI.Capsula(cara, "Chip", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -166f), new Vector2(190f, 30f), RelevoUI.Ambar);

        tarjeta.chipTexto = RelevoUI.Texto(cara, "Chip texto", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -166f), new Vector2(190f, 30f), 15, TextAnchor.MiddleCenter, RelevoUI.FondoHondo);
        tarjeta.chipTexto.fontStyle = FontStyle.Bold;

        // --- identidad del tramo
        tarjeta.titulo = RelevoUI.Texto(cara, "Titulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -202f), new Vector2(340f, 58f), 20, TextAnchor.UpperCenter, RelevoUI.Tinta);
        tarjeta.titulo.fontStyle = FontStyle.Bold;
        tarjeta.titulo.text = definicion.nombre;

        // --- recuerdo
        tarjeta.recuerdoTitulo = RelevoUI.Texto(cara, "Recuerdo titulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -266f), new Vector2(340f, 40f), 16, TextAnchor.UpperCenter, acento);

        tarjeta.cuerpo = RelevoUI.Texto(cara, "Cuerpo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -308f), new Vector2(340f, 128f), 15, TextAnchor.UpperLeft,
            RelevoUI.ConAlfa(RelevoUI.Tinta, 0.88f));

        RelevoUI.Franja(cara, "Filete pie", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -442f), new Vector2(340f, 1f), RelevoUI.CobreTenue);

        tarjeta.marcaRecuerdo = RelevoUI.Capsula(cara, "Marca recuerdo", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -454f), new Vector2(18f, 26f), RelevoUI.Ambar);

        tarjeta.marcaRecuerdoTexto = RelevoUI.Texto(cara, "Marca recuerdo texto", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(44f, -454f), new Vector2(312f, 26f), 15, TextAnchor.MiddleLeft,
            RelevoUI.ConAlfa(RelevoUI.Tinta, 0.8f));

        // Marca de foco: solo la lleva la tarjeta elegida.
        tarjeta.indicador = RelevoUI.Hexagono(cara, "Indicador", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-14f, -14f), new Vector2(20f, 20f), RelevoUI.Ambar, true);

        return tarjeta;
    }

    /// <summary>Banner de "siguiente destino": lee el progreso, nunca lo escribe.</summary>
    void SiguienteDestino(Transform papel)
    {
        GameObject banner = RelevoUI.Panel(papel, "Siguiente destino", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -672f), new Vector2(1164f, 62f), RelevoUI.FondoHondo);
        RelevoUI.Marco(banner.transform, RelevoUI.CobreTenue, 1f);

        franjaSiguiente = RelevoUI.Franja(banner.transform, "Filo", new Vector2(0f, 1f), new Vector2(0f, 1f),
            Vector2.zero, new Vector2(3f, 62f), RelevoUI.Ambar);

        selloSiguiente = RelevoUI.Hexagono(banner.transform, "Sello", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(26f, 0f), new Vector2(26f, 26f), RelevoUI.Ambar, true);

        textoSiguiente = RelevoUI.Texto(banner.transform, "Texto", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(56f, 0f), new Vector2(1080f, 40f), 20, TextAnchor.MiddleLeft, RelevoUI.AmbarSuave);
    }

    void Leyenda(Transform papel)
    {
        Color tecla = RelevoUI.ConAlfa(RelevoUI.Cobre, 0.9f);
        Color verbo = RelevoUI.ConAlfa(RelevoUI.Tinta, 0.6f);

        float y = -752f;
        float x = 20f;
        x = Chip(papel, x, y, "←→", "elegir", tecla, verbo, 16);
        x = Chip(papel, x, y, "Enter", "volver", tecla, verbo, 16);
        x = Chip(papel, x, y, "E", "volver", tecla, verbo, 16);
        Chip(papel, x, y, "Tab", "cerrar", tecla, verbo, 16);
    }

    // ------------------------------------------------------------------ estado

    void Refrescar()
    {
        int encontrados = 0;

        for (int i = 0; i < Niveles; i++)
        {
            int nivel = i + 1;
            LevelDefinition definicion = DefinicionesPorDefecto.Obtener(nivel);
            bool completado = SaveManager.NivelCompletado(nivel);
            bool desbloqueado = SaveManager.NivelDesbloqueado(nivel);
            bool tiene = SaveManager.TieneRecuerdo(definicion.recuerdoIdentificador);

            if (tiene) encontrados++;

            Color acento = ColorDeNivel(nivel);
            Tarjeta tarjeta = tarjetas[i];
            bool elegida = i == seleccion;

            // El foco se marca con tres señales: fondo más claro, marco ámbar e indicador hexagonal.
            tarjeta.fondo.color = elegida ? RelevoUI.FondoSeleccion : RelevoUI.FondoHondo;
            for (int f = 0; f < tarjeta.filetes.Length; f++)
            {
                tarjeta.filetes[f].color = elegida ? RelevoUI.Ambar : RelevoUI.CobreTenue;
            }
            tarjeta.indicador.gameObject.SetActive(elegida);
            tarjeta.titulo.color = elegida ? RelevoUI.AmbarSuave : RelevoUI.Tinta;

            // Chip de estado: ámbar si ya está restaurado, turquesa si se puede jugar y cobre
            // apagado si todavía está cerrado. El ancho se adapta al texto de la cápsula.
            string estado;
            Color colorEstado;

            if (completado)
            {
                estado = "COMPLETADO";
                colorEstado = RelevoUI.Ambar;
            }
            else if (desbloqueado)
            {
                estado = "DISPONIBLE";
                colorEstado = RelevoUI.Turquesa;
            }
            else
            {
                estado = "BLOQUEADO";
                colorEstado = RelevoUI.ConAlfa(RelevoUI.Cobre, 0.6f);
            }

            float anchoChip = 34f + estado.Length * 9.5f;
            tarjeta.chipTexto.text = estado;
            tarjeta.chipFondo.color = colorEstado;
            tarjeta.chipFondo.rectTransform.sizeDelta = new Vector2(anchoChip, 30f);
            tarjeta.chipTexto.rectTransform.sizeDelta = new Vector2(anchoChip, 30f);

            tarjeta.sello.color = desbloqueado ? acento : RelevoUI.ConAlfa(acento, 0.45f);

            tarjeta.recuerdoTitulo.text = tiene ? definicion.recuerdoTitulo : "Recuerdo sin leer";
            tarjeta.recuerdoTitulo.color = tiene
                ? RelevoUI.ConAlfa(acento, 0.95f)
                : RelevoUI.ConAlfa(RelevoUI.Cobre, 0.75f);

            tarjeta.cuerpo.text = tiene
                ? definicion.recuerdoTexto
                : "Todavía no has encontrado este recuerdo. El orbe ámbar del " + definicion.tituloIntro
                  + " sigue esperando.";

            tarjeta.marcaRecuerdo.color = tiene ? RelevoUI.Ambar : RelevoUI.ConAlfa(RelevoUI.Cobre, 0.4f);
            tarjeta.marcaRecuerdoTexto.text = tiene ? "RECUERDO 1 / 1" : "SIN RECUERDO";
        }

        textoRecuento.text = "Recuerdos encontrados: " + encontrados + " de " + Niveles + ".";

        RefrescarSiguiente();
    }

    void RefrescarSiguiente()
    {
        // El primer tramo sin completar es siempre el desbloqueado: el desbloqueo exige el anterior.
        int siguiente = 0;
        for (int nivel = 1; nivel <= Niveles; nivel++)
        {
            if (!SaveManager.NivelCompletado(nivel))
            {
                siguiente = nivel;
                break;
            }
        }

        if (siguiente == 0)
        {
            textoSiguiente.text = "TODOS LOS TRAMOS RESTAURADOS · el faro vuelve a brillar sobre el canal.";
            franjaSiguiente.color = RelevoUI.Verde;
            selloSiguiente.color = RelevoUI.Verde;
            return;
        }

        Color acento = ColorDeNivel(siguiente);
        textoSiguiente.text = "SIGUIENTE DESTINO · " + DefinicionesPorDefecto.Obtener(siguiente).nombre;
        franjaSiguiente.color = acento;
        selloSiguiente.color = acento;
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

    // ------------------------------------------------------------------ ayudantes de UGUI

    /// <summary>Tecla en cápsula más su verbo. Devuelve la X donde continúa la leyenda.</summary>
    static float Chip(Transform padre, float x, float y, string tecla, string verbo,
        Color colorTecla, Color colorVerbo, int tamano)
    {
        float alto = tamano * 2f;
        float anchoTecla = Mathf.Max(34f, 18f + tecla.Length * tamano * 0.62f);

        RelevoUI.Capsula(padre, "Tecla " + tecla, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(x, y), new Vector2(anchoTecla, alto), colorTecla);

        Text etiqueta = RelevoUI.Texto(padre, "Tecla " + tecla + " texto", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(x, y), new Vector2(anchoTecla, alto), tamano, TextAnchor.MiddleCenter, RelevoUI.FondoHondo);
        etiqueta.text = tecla;
        etiqueta.fontStyle = FontStyle.Bold;

        if (string.IsNullOrEmpty(verbo)) return x + anchoTecla + 16f;

        float anchoVerbo = 24f + verbo.Length * tamano * 0.52f;
        RelevoUI.Texto(padre, "Verbo " + verbo, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(x + anchoTecla + 10f, y), new Vector2(anchoVerbo, alto), tamano,
            TextAnchor.MiddleLeft, colorVerbo).text = verbo;

        return x + anchoTecla + 10f + anchoVerbo + 16f;
    }
}
