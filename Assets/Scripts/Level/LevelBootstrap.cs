using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Punto de entrada del nivel.
///
/// La escena puede estar horneada (maqueta, cámara y actores ya existen como objetos
/// guardados, visibles y editables en el editor) o vacía. Si está horneada, este componente
/// solo enlaza el contexto de ejecución y restablece el estado dinámico; si está vacía,
/// construye el nivel entero en tiempo de ejecución.
///
/// El estado que no sobrevive al horneado (batería en el suelo, puentes retraídos, palancas
/// sin accionar) se restablece siempre y de forma explícita al empezar el nivel.
/// </summary>
public class LevelBootstrap : MonoBehaviour
{
    [Header("Nivel")]
    [SerializeField] int indiceNivel = 1;
    [SerializeField] LevelDefinition definicion;

    [Header("Entrada")]
    [Tooltip("Assets/Input/Relevo.inputactions")]
    [SerializeField] InputActionAsset acciones;

    [Header("Escena")]
    [Tooltip("Lo marca la herramienta de horneado: el nivel ya está guardado en la escena.")]
    [SerializeField] bool escenaHorneada;

    void Awake()
    {
        Time.timeScale = 1f;
        GameEvents.Limpiar();
        RelevoGame.Limpiar();

        LevelLayout layout = LevelLayouts.Obtener(indiceNivel);
        if (layout == null)
        {
            Debug.LogError("RELEVO: no hay mapa definido para el nivel " + indiceNivel + ".");
            return;
        }

        LevelDefinition def = DefinicionActiva();
        RelevoGame.Layout = layout;

        ActorFactory.ConfigurarEntorno();

        if (!escenaHorneada) Construir(layout, def);

        Enlazar(layout, def);
    }

    LevelDefinition DefinicionActiva()
    {
        LevelDefinition def = definicion != null ? definicion : DefinicionesPorDefecto.Obtener(indiceNivel);
        RelevoGame.Definicion = def;
        return def;
    }

    // ------------------------------------------------------------------ construcción

    /// <summary>
    /// Construye maqueta, cámara y actores dentro de la escena actual, sin nada de
    /// ejecución (ni entrada, ni HUD, ni mensajes). La usa la herramienta de horneado.
    /// </summary>
    public void ConstruirParaEditor()
    {
        LevelLayout layout = LevelLayouts.Obtener(indiceNivel);
        if (layout == null)
        {
            Debug.LogError("RELEVO: no hay mapa definido para el nivel " + indiceNivel + ".");
            return;
        }

        CameraRig camara = ConstruirMaqueta(layout);
        ConstruirActores(layout, DefinicionActiva(), camara);

        escenaHorneada = true;
    }

    void Construir(LevelLayout layout, LevelDefinition def)
    {
        CameraRig camara = ConstruirMaqueta(layout);
        ConstruirActores(layout, def, camara);
    }

    /// <summary>Maqueta, colliders y cámara ortográfica: lo que queda guardado en la escena.</summary>
    CameraRig ConstruirMaqueta(LevelLayout layout)
    {
        GameObject raizNivel = new GameObject("Nivel");
        raizNivel.AddComponent<ContenidoHorneado>();

        // Animación ambiental (marea, ondulaciones, brillo del agua): vive en la raíz del nivel
        // —no en un actor— porque solo afecta al aspecto y así desaparece con el resto del
        // contenido horneado al reconstruir la escena.
        raizNivel.AddComponent<AmbienteRelevo>();

        Bounds limites = DioramaBuilder.Construir(layout, raizNivel.transform);

        GameObject raizCamara = new GameObject("Camara");
        raizCamara.AddComponent<ContenidoHorneado>();
        CameraRig camara = raizCamara.AddComponent<CameraRig>();
        camara.Configurar(limites);

        return camara;
    }

    Transform ConstruirActores(LevelLayout layout, LevelDefinition def, CameraRig camara)
    {
        GameObject raizActores = new GameObject("Actores");
        raizActores.AddComponent<ContenidoHorneado>();
        ActorFactory.CrearSol(raizActores.transform);
        CrearActores(layout, def, raizActores.transform, camara);
        return raizActores.transform;
    }

    /// <summary>
    /// Crea todos los actores del nivel bajo la raíz indicada. La entrada y el HUD no se
    /// enlazan aquí, sino después, en Enlazar().
    /// </summary>
    public void CrearActores(LevelLayout layout, LevelDefinition def, Transform raizActores, CameraRig camara)
    {
        Vector3 inicioNilo = ActorFactory.PosicionMarcador(layout, 'N', new Vector3(2f, 0f, -2f));
        PlayerController nilo = ActorFactory.CrearNilo(raizActores, inicioNilo, 45f);
        PlayerInteractor interactor = nilo.GetComponent<PlayerInteractor>();

        nilo.Inicializar(null, camara != null ? camara.Camara : null);
        interactor.Inicializar(nilo);

        if (layout.TieneCasilla('Y'))
        {
            Vector3 posBateria = ActorFactory.PosicionMarcador(layout, 'Y', inicioNilo + Vector3.right * 2f);
            BatteryController bateria = ActorFactory.CrearBateria(raizActores, posBateria, raizActores);
            ActorFactory.EngancharBateriaALaMano(bateria, nilo);
        }

        TemporaryBridge puente = null;
        if (layout.TieneCasilla('B'))
        {
            puente = ActorFactory.CrearPuente(raizActores, layout, 'B', "B");
        }

        if (layout.TieneCasilla('A'))
        {
            Vector3 posBase = ActorFactory.PosicionMarcador(layout, 'A', inicioNilo + Vector3.right * 4f);
            Vector3 hacia = puente != null
                ? ActorFactory.CentroDePrimerTramo(layout, 'B')
                : posBase + Vector3.right * 2f;

            ActorFactory.CrearBaseEnergia(raizActores, posBase, "A", "base A", puente, hacia);
        }

        // Segunda base y segundo puente: es el mismo actor otra vez, no una variante. El Nivel 1
        // no tiene casillas 'C' ni 'D', así que todo este bloque queda inerte allí.
        TemporaryBridge puenteC = null;
        if (layout.TieneCasilla('C'))
        {
            puenteC = ActorFactory.CrearPuente(raizActores, layout, 'C', "C");
        }

        if (layout.TieneCasilla('D'))
        {
            Vector3 posBase = ActorFactory.PosicionMarcador(layout, 'D', inicioNilo + Vector3.right * 8f);
            Vector3 hacia = puenteC != null
                ? ActorFactory.CentroDePrimerTramo(layout, 'C')
                : posBase + Vector3.right * 2f;

            ActorFactory.CrearBaseEnergia(raizActores, posBase, "D", "base D", puenteC, hacia);
        }

        PermanentWalkway pasarela = null;
        if (layout.TieneCasilla('P'))
        {
            pasarela = ActorFactory.CrearPasarela(raizActores, layout, 'P', "P");
        }

        if (layout.TieneCasilla('L'))
        {
            Vector3 posPalanca = ActorFactory.PosicionMarcador(layout, 'L', inicioNilo);
            ActorFactory.CrearPalanca(raizActores, posPalanca, "L", pasarela);
        }

        // Segunda pasarela y segunda palanca, con el mismo criterio.
        PermanentWalkway pasarelaQ = null;
        if (layout.TieneCasilla('Q'))
        {
            pasarelaQ = ActorFactory.CrearPasarela(raizActores, layout, 'Q', "Q");
        }

        if (layout.TieneCasilla('K'))
        {
            Vector3 posPalanca = ActorFactory.PosicionMarcador(layout, 'K', inicioNilo);
            ActorFactory.CrearPalanca(raizActores, posPalanca, "K", pasarelaQ);
        }

        if (layout.TieneCasilla('E'))
        {
            Vector3 posSalida = ActorFactory.PosicionMarcador(layout, 'E', inicioNilo);

            // La definición decide si la salida es un pórtico o el faro. El Nivel 1 y el 2 no
            // marcan salidaEsFaro, así que siguen usando exactamente el pórtico de siempre.
            if (def != null && def.salidaEsFaro) ActorFactory.CrearFaro(raizActores, posSalida);
            else ActorFactory.CrearSalida(raizActores, posSalida, "la salida");
        }

        if (layout.TieneCasilla('X'))
        {
            Vector3 posRecuerdo = ActorFactory.PosicionMarcador(layout, 'X', inicioNilo);
            ActorFactory.CrearRecuerdo(raizActores, posRecuerdo, def);
        }

        if (layout.TieneCasilla('M'))
        {
            Vector3 posMaia = ActorFactory.PosicionMarcador(layout, 'M', inicioNilo);
            ActorFactory.CrearMaia(raizActores, posMaia, def);
        }

        CrearPiezasDelPatio(layout, raizActores);
    }

    /// <summary>
    /// Cajas, placas de presión, compuertas y el puente de placa (Patio de Bombas). La placa 'o'
    /// acciona la compuerta 'g' y la placa 'u' despliega el puente 'R'. En niveles sin esas
    /// casillas no se crea nada.
    /// </summary>
    static void CrearPiezasDelPatio(LevelLayout layout, Transform raiz)
    {
        Compuerta compuerta = null;
        foreach (Vector2Int c in layout.Casillas('g')) compuerta = ActorPatio.CrearCompuerta(raiz, layout, c);

        TemporaryBridge puenteR = null;
        if (layout.TieneCasilla('R'))
        {
            puenteR = ActorFactory.CrearPuente(raiz, layout, 'R', "R");
            ActorPatio.PintarPuenteManual(puenteR);
        }

        foreach (Vector2Int c in layout.Casillas('o'))
        {
            Vector3 centro = layout.CentroTile(c.x, c.y);
            ActorPatio.CrearPlaca(raiz, layout, c, compuerta, null, ActorPatio.CasillaMasCercana(layout, 'g', centro));
        }

        foreach (Vector2Int c in layout.Casillas('u'))
        {
            // El cable llega a la casilla de tierra junto al puente, no al agua.
            Vector3 centro = layout.CentroTile(c.x, c.y);
            Vector3 destino = ActorPatio.CasillaMasCercana(layout, 'R', centro);
            Vector2Int cd = layout.Casilla(destino);
            Vector3 orilla = layout.CentroTile(cd.x + (c.x > cd.x ? 1 : -1), cd.y);
            ActorPatio.CrearPlaca(raiz, layout, c, null, puenteR, orilla);
        }

        foreach (Vector2Int c in layout.Casillas('k')) ActorPatio.CrearCaja(raiz, layout, c);
    }

    // ------------------------------------------------------------------ enlace

    /// <summary>
    /// Enlaza el contexto de ejecución (entrada, gestor, HUD) con lo que ya hay en la
    /// escena, esté horneado o recién construido, y restablece el estado dinámico.
    /// Las búsquedas ocurren una sola vez al empezar, nunca dentro de Update.
    /// </summary>
    void Enlazar(LevelLayout layout, LevelDefinition def)
    {
        // El audio no tiene representación visual, así que nunca se hornea.
        if (Object.FindObjectsByType<AudioDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0)
        {
            new GameObject("Audio").AddComponent<AudioDirector>();
        }

        RelevoInput entrada = new RelevoInput(acciones);
        RelevoGame.Input = entrada;

        LevelManager nivel = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (nivel == null)
        {
            nivel = new GameObject("Gestor").AddComponent<LevelManager>();
        }
        RelevoGame.Nivel = nivel;

        // --- cámara y jugador
        CameraRig camara = Object.FindFirstObjectByType<CameraRig>(FindObjectsInactive.Include);
        if (camara != null) camara.Configurar(layout.Limites());

        PlayerController nilo = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        PlayerInteractor interactor = nilo != null ? nilo.GetComponent<PlayerInteractor>() : null;

        if (nilo != null)
        {
            nilo.Inicializar(entrada, camara != null ? camara.Camara : null);
            if (interactor != null) interactor.Inicializar(nilo);
        }

        RelevoGame.Jugador = nilo;
        RelevoGame.Interactor = interactor;

        // --- estado dinámico de los actores
        BatteryController bateria = Object.FindFirstObjectByType<BatteryController>(FindObjectsInactive.Include);
        if (bateria != null)
        {
            if (nilo != null) ActorFactory.EngancharBateriaALaMano(bateria, nilo);
            bateria.Vincular(null);
        }

        // Los tramos de estos dos actores se registran al construirlos, así que en una escena
        // horneada hay que devolverlos explícitamente a su estado retraído.
        TemporaryBridge[] puentes = Object.FindObjectsByType<TemporaryBridge>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < puentes.Length; i++) puentes[i].Vincular();

        PermanentWalkway[] pasarelas = Object.FindObjectsByType<PermanentWalkway>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < pasarelas.Length; i++) pasarelas[i].Vincular();

        EnergySocket[] bases = Object.FindObjectsByType<EnergySocket>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < bases.Length; i++) bases[i].Vincular();

        LeverInteractable[] palancas = Object.FindObjectsByType<LeverInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < palancas.Length; i++) palancas[i].Vincular();

        ExitInteractable salida = Object.FindFirstObjectByType<ExitInteractable>(FindObjectsInactive.Include);
        if (salida != null) salida.Vincular();

        MemoryInteractable recuerdo = Object.FindFirstObjectByType<MemoryInteractable>(FindObjectsInactive.Include);
        if (recuerdo != null) recuerdo.Vincular();

        // Piezas del Patio de Bombas: vuelven a su estado inicial también en escena horneada.
        foreach (CajaEmpujable caja in Object.FindObjectsByType<CajaEmpujable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            caja.Vincular(layout);
        foreach (Compuerta c in Object.FindObjectsByType<Compuerta>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            c.Vincular();
        foreach (PlacaPresion p in Object.FindObjectsByType<PlacaPresion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            p.Vincular();

        // Reserva de energía de Nilo: solo en niveles que la definen. Se añade en ejecución.
        if (nilo != null && def != null && def.reservaCasillas > 0)
        {
            ReservaEnergia reserva = nilo.GetComponent<ReservaEnergia>();
            if (reserva == null) reserva = nilo.gameObject.AddComponent<ReservaEnergia>();
            reserva.Configurar(def.reservaCasillas, layout.TamanoTile);
        }

        MaiaTerminal maia = Object.FindFirstObjectByType<MaiaTerminal>(FindObjectsInactive.Include);

        nivel.Vincular(salida, recuerdo, maia);

        // --- HUD
        UIManager ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        if (ui == null)
        {
            ui = new GameObject("UI").AddComponent<UIManager>();
        }
        RelevoGame.UI = ui;
        ui.Inicializar(nivel, entrada, def);

        nivel.Inicializar(indiceNivel, def);

        // La intro del nivel ya cuenta la presentación, así que Maia pasa directa a la pista.
        if (maia != null) maia.MarcarPresentacion();
        if (def != null) GameEvents.RaiseMensajeMostrado(def.tituloIntro, def.textoIntro);
        if (def != null && def.tutorialPatio) TutorialMaia.Presentacion();

        Debug.Log("RELEVO: " + nivel.NombreNivel
            + (escenaHorneada ? " (escena horneada)" : " (construido en ejecución)")
            + " — bases " + nivel.Bases.Count + ", puentes " + nivel.Puentes.Count
            + ", pasarelas " + nivel.Pasarelas.Count + ", palancas " + nivel.Palancas.Count + ".");
    }
}
