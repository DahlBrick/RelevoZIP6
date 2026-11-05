using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Estado y reglas del nivel en curso: qué hay construido, qué se ha activado y cómo se
/// completa. El objetivo del HUD se calcula a partir del estado real de la batería y de
/// las palancas, así que nunca indica algo imposible.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public int IndiceNivel = 1;
    public string NombreNivel = "Nivel 1";
    public LevelDefinition Definicion;

    public readonly List<EnergySocket> Bases = new List<EnergySocket>();
    public readonly List<TemporaryBridge> Puentes = new List<TemporaryBridge>();
    public readonly List<PermanentWalkway> Pasarelas = new List<PermanentWalkway>();
    public readonly List<LeverInteractable> Palancas = new List<LeverInteractable>();

    public ExitInteractable Salida;
    public MemoryInteractable Recuerdo;
    public MaiaTerminal Maia;

    public bool Completado { get; private set; }
    public int RecuerdosEncontrados { get; private set; }
    public int RecuerdosTotales { get; private set; }

    public bool QuedanPalancas
    {
        get
        {
            for (int i = 0; i < Palancas.Count; i++)
            {
                if (Palancas[i] != null && !Palancas[i].Accionada) return true;
            }
            return false;
        }
    }

    public bool TodasLasPalancasAccionadas
    {
        get { return Palancas.Count > 0 && !QuedanPalancas; }
    }

    void Awake()
    {
        RelevoGame.Nivel = this;

        GameEvents.BateriaCambiada += AlCambiarBateria;
        GameEvents.BaseEnergiaCambiada += AlCambiarBase;
        GameEvents.PuenteCambiado += AlCambiarPuente;
        GameEvents.PasarelaAbierta += AlCambiarPasarela;
    }

    void OnDestroy()
    {
        GameEvents.BateriaCambiada -= AlCambiarBateria;
        GameEvents.BaseEnergiaCambiada -= AlCambiarBase;
        GameEvents.PuenteCambiado -= AlCambiarPuente;
        GameEvents.PasarelaAbierta -= AlCambiarPasarela;

        if (RelevoGame.Nivel == this) RelevoGame.Nivel = null;
    }

    public void Inicializar(int indice, LevelDefinition definicion)
    {
        IndiceNivel = indice;
        Definicion = definicion;

        NombreNivel = definicion != null && !string.IsNullOrEmpty(definicion.nombre)
            ? definicion.nombre
            : "Nivel " + indice;

        RecuerdosTotales = Recuerdo != null ? 1 : 0;
        RecuerdosEncontrados = Mathf.Clamp(SaveManager.RecuerdosDelNivel(indice), 0, RecuerdosTotales);

        // Estado inicial publicado al HUD.
        BatteryController bateria = RelevoGame.Bateria;
        if (bateria != null)
        {
            GameEvents.RaiseBateriaCambiada(bateria.Estado, bateria.BaseActual != null ? bateria.BaseActual.Etiqueta : "");
        }

        GameEvents.RaiseRecuerdosCambiados(RecuerdosEncontrados, RecuerdosTotales);
        ultimoObjetivo = null;
        ActualizarObjetivo();
    }

    // El objetivo también depende de cosas que no publican eventos (dónde está Nilo, la carga de un
    // condensador), así que se revisa dos veces por segundo; solo se publica si cambia.
    string ultimoObjetivo;
    float relojObjetivo;

    void Update()
    {
        relojObjetivo -= Time.deltaTime;
        if (relojObjetivo > 0f) return;
        relojObjetivo = 0.5f;
        ActualizarObjetivo();
    }

    void Publicar(string objetivo)
    {
        if (objetivo == ultimoObjetivo) return;
        ultimoObjetivo = objetivo;
        GameEvents.RaiseObjetivoCambiado(objetivo);
    }

    /// <summary>
    /// Reengancha las listas de actores al cargar una escena ya horneada. Son readonly y
    /// Unity no las serializa, así que se reconstruyen una sola vez al empezar el nivel
    /// (nunca dentro de Update), antes de Inicializar().
    /// </summary>
    public void Vincular(ExitInteractable salida, MemoryInteractable recuerdo, MaiaTerminal maia)
    {
        Salida = salida;
        Recuerdo = recuerdo;
        Maia = maia;

        Bases.Clear();
        Puentes.Clear();
        Pasarelas.Clear();
        Palancas.Clear();

        Bases.AddRange(Object.FindObjectsByType<EnergySocket>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        Puentes.AddRange(Object.FindObjectsByType<TemporaryBridge>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        Pasarelas.AddRange(Object.FindObjectsByType<PermanentWalkway>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        Palancas.AddRange(Object.FindObjectsByType<LeverInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None));
    }

    void AlCambiarBateria(EstadoBateria estado, string etiquetaBase) { ActualizarObjetivo(); }
    void AlCambiarBase(EnergySocket baseEnergia, bool activa) { ActualizarObjetivo(); }
    void AlCambiarPuente(TemporaryBridge puente, bool desplegado) { ActualizarObjetivo(); }
    void AlCambiarPasarela(PermanentWalkway pasarela) { ActualizarObjetivo(); }

    public void ActualizarObjetivo()
    {
        if (Completado)
        {
            Publicar("Nivel completado.");
            return;
        }

        BatteryController bateria = RelevoGame.Bateria;
        string objetivo;

        if (bateria != null && bateria.Estado != EstadoBateria.EnSuelo)
        {
            string propio = null;
            if (IndiceNivel == 1 && TienePalanca("L") && TienePalanca("K")) propio = ObjetivoMuelle(bateria);
            else if (IndiceNivel == 2 && Definicion != null && Definicion.tutorialPatio) propio = ObjetivoPatio(bateria);
            else if (IndiceNivel == 3 && Definicion != null && Definicion.tutorialCondensadores) propio = ObjetivoFaro(bateria);
            if (propio != null)
            {
                Publicar(propio);
                return;
            }
        }
        else if (bateria != null && Definicion != null && Definicion.tutorialPatio && !CompuertaAbierta())
        {
            Publicar("Empuja la caja sobre la placa: abre la compuerta del norte.");
            return;
        }

        if (bateria == null)
        {
            objetivo = "Explora el muelle.";
        }
        else if (bateria.Estado == EstadoBateria.EnSuelo)
        {
            objetivo = Bases.Count > 0
                ? "Recoge la batería y colócala en una base de energía."
                : "Recoge la batería.";
        }
        else if (bateria.EnManos)
        {
            // La salida se nombra con su propia etiqueta —'la salida' en el Muelle y el Patio de
            // Bombas, 'el faro' en Liria— en vez de darla por supuesta. Salida puede ser null en el
            // primer ActualizarObjetivo, que BatteryController.Vincular() dispara antes de que
            // LevelBootstrap.Enlazar() llegue a LevelManager.Vincular().
            objetivo = QuedanPalancas && Bases.Count > 0
                ? "Coloca la batería en una base para desplegar el paso eléctrico."
                : "Lleva la batería hasta " + (Salida != null ? Salida.Etiqueta : "la salida") + ".";
        }
        else
        {
            objetivo = QuedanPalancas
                ? "Cruza el puente y acciona la palanca: abre el regreso."
                : "Retira la batería de la base y vuelve por la pasarela manual.";
        }

        Publicar(objetivo);
    }

    bool TienePalanca(string id) { return Palanca(id) != null; }

    LeverInteractable Palanca(string id)
    {
        for (int i = 0; i < Palancas.Count; i++)
            if (Palancas[i] != null && Palancas[i].Identificador == id) return Palancas[i];
        return null;
    }

    bool Accionada(string id)
    {
        LeverInteractable p = Palanca(id);
        return p != null && p.Accionada;
    }

    static bool CompuertaAbierta()
    {
        for (int i = 0; i < PlacaPresion.Todas.Count; i++)
        {
            PlacaPresion p = PlacaPresion.Todas[i];
            if (p != null && p.Compuerta != null && !p.PorCaja) return false;
        }
        return true;
    }

    static bool PuentePlacaSostenido()
    {
        for (int i = 0; i < PlacaPresion.Todas.Count; i++)
        {
            PlacaPresion p = PlacaPresion.Todas[i];
            if (p != null && p.Puente != null && p.PorCaja) return true;
        }
        return false;
    }

    string EtiquetaSalida { get { return Salida != null ? Salida.Etiqueta : "la salida"; } }

    /// <summary>Muelle de Cobre: el ciclo base-puente-palanca dos veces, de oeste a este.</summary>
    string ObjetivoMuelle(BatteryController bateria)
    {
        string b = bateria.BaseActual != null ? bateria.BaseActual.Identificador : "";
        bool l = Accionada("L"), k = Accionada("K");

        if (!l)
        {
            if (b == "A") return "Cruza el puente y acciona la palanca: abrirá el camino de vuelta.";
            if (bateria.EnManos) return "Coloca la batería en la base A para desplegar el puente.";
            return "Retira la batería y vuelve a la base A.";
        }
        if (!k)
        {
            if (b == "A") return "Retira la batería de la base A y llévala por la pasarela a la isla.";
            if (bateria.EnManos) return "Coloca la batería en la base D para cruzar al este.";
            return "Cruza al este y acciona la segunda palanca.";
        }
        if (b != "") return "Retira la batería de la base " + b + " y vuelve por la pasarela.";
        return "Lleva la batería hasta " + EtiquetaSalida + ".";
    }

    /// <summary>
    /// Objetivo del Patio de Bombas, paso a paso según el estado real: compuerta, palanca, caja
    /// del muelle este y dónde está la batería.
    /// </summary>
    string ObjetivoPatio(BatteryController bateria)
    {
        string b = bateria.BaseActual != null ? bateria.BaseActual.Identificador : "";

        if (!Accionada("L"))
        {
            if (!CompuertaAbierta() && b == "") return "Empuja la caja sobre la placa: abre la compuerta del norte.";
            if (b == "A") return "Cruza, acciona la palanca y vuelve antes de agotar la reserva.";
            if (bateria.EnManos) return "Coloca la batería en la base A para desplegar el puente.";
            return "Recupera la batería y llévala a la base A.";
        }

        if (!PuentePlacaSostenido())
        {
            if (b == "A") return "Retira la batería de la base A y llévala por la pasarela al patio central.";
            if (bateria.EnManos) return "Coloca la batería en la base D para cruzar al muelle este.";
            return "Empuja la caja del muelle este hasta su placa. Si la reserva baja, vuelve a recargar junto a D.";
        }

        if (b != "") return "Retira la batería de la base y cruza por el puente de la placa.";
        return "Lleva la batería hasta " + EtiquetaSalida + ".";
    }

    /// <summary>Faro de Liria: condensadores, la caja del pasillo y la carrera final.</summary>
    string ObjetivoFaro(BatteryController bateria)
    {
        EnergySocket baseActual = bateria.BaseActual;
        string b = baseActual != null ? baseActual.Identificador : "";
        Condensador cond = baseActual != null ? baseActual.Condensador : null;
        int columna = RelevoGame.Jugador != null && RelevoGame.Layout != null
            ? RelevoGame.Layout.Casilla(RelevoGame.Jugador.transform.position).x : 0;

        if (bateria.EnManos && columna < 12)
        {
            for (int i = 0; i < Bases.Count; i++)
            {
                Condensador c = Bases[i] != null ? Bases[i].Condensador : null;
                if (c != null && c.Sosteniendo) return "¡Corre! El puente aguanta mientras se vacía el condensador.";
            }
        }

        if (columna >= 12) return bateria.EnManos ? "Entrega la batería en " + EtiquetaSalida + "." : "Vuelve a por la batería.";

        if (columna <= 3 && b != "D" && !Accionada("K"))
        {
            if (b == "A")
            {
                if (cond != null && cond.Carga < 0.99f) return "Espera a que el condensador se llene: mira sus barras.";
                return "Retira la batería y cruza el puente antes de que el condensador se vacíe.";
            }
            if (bateria.EnManos) return "Coloca la batería en la base A: cargará su condensador.";
        }

        if (!CompuertaAbierta() && !Accionada("K"))
            return "Empuja la caja hasta la placa del norte: abre la compuerta del pasillo.";

        if (!Accionada("K"))
        {
            if (b == "D") return "Lejos de la batería la reserva no llega a la palanca. Llévala contigo por el pasillo.";
            return "Baja por el pasillo con la batería y acciona la palanca.";
        }

        if (b == "D")
        {
            if (cond != null && cond.Carga < 0.99f) return "Espera a que el condensador de D se llene.";
            return "Retira la batería y corre por la pasarela y el puente hasta el faro.";
        }
        if (bateria.EnManos) return "Coloca la batería en la base D para cargar su condensador.";
        return "Retira la batería de la base y llévala a la base D.";
    }

    public void RegistrarRecuerdo(MemoryInteractable recuerdo)
    {
        if (recuerdo == null) return;

        if (!SaveManager.TieneRecuerdo(recuerdo.Identificador))
        {
            SaveManager.RegistrarRecuerdo(recuerdo.Identificador);
            SaveManager.Guardar();
        }

        RecuerdosEncontrados = Mathf.Clamp(RecuerdosEncontrados + 1, 0, RecuerdosTotales);
        GameEvents.RaiseRecuerdosCambiados(RecuerdosEncontrados, RecuerdosTotales);

        string texto = string.IsNullOrEmpty(recuerdo.Texto)
            ? "Guardado en tu cuaderno de viaje."
            : recuerdo.Texto + "\n\nGuardado en tu cuaderno de viaje.";

        GameEvents.RaiseMensajeMostrado(recuerdo.Titulo, texto);
    }

    public void CompletarNivel()
    {
        if (Completado) return;

        BatteryController bateria = RelevoGame.Bateria;
        if (bateria == null || !bateria.EnManos)
        {
            AudioDirector.Pedir(Tono.Bloqueado);
            GameEvents.RaiseMensajeMostrado("Salida", "Necesitas llevar la batería para salir.");
            return;
        }

        Completado = true;
        SaveManager.MarcarNivelCompletado(IndiceNivel);

        AudioDirector.Pedir(Tono.Salida);
        AudioDirector.Pedir(Tono.Faro);

        GameEvents.RaiseNivelCompletado(NombreNivel);

        string cierre = Definicion != null ? Definicion.textoCierre : "";
        GameEvents.RaiseMensajeMostrado("Nivel completado",
            string.IsNullOrEmpty(cierre) ? NombreNivel : cierre);

        ActualizarObjetivo();
    }

    public void MostrarPista()
    {
        string pista = Definicion != null ? Definicion.pistaMaia : "";
        if (string.IsNullOrEmpty(pista) && Maia != null) pista = Maia.TextoPista;
        if (string.IsNullOrEmpty(pista)) pista = "La batería abre el paso eléctrico; la palanca abre el regreso.";

        AudioDirector.Pedir(Tono.Mensaje);
        GameEvents.RaiseMensajeMostrado("Maia", pista);
    }

    /// <summary>
    /// Lo marca ReiniciarNivel justo antes de recargar la escena: el nivel que arranca sabe que es
    /// un reintento (R o Nilo apagado) y no repite la introducción ni el tutorial.
    /// </summary>
    public static bool ReinicioPendiente;

    public void ReiniciarNivel()
    {
        ReinicioPendiente = true;
        Time.timeScale = 1f;
        GameEvents.Limpiar();
        CargarEscena(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Nivel desde el que se abrió el cuaderno. Se conserva aunque ahora la escena se cargue de
    /// forma aditiva: sigue sirviendo para saber desde dónde se abrió y para el modo edición.
    /// </summary>
    public static string EscenaOrigen;

    /// <summary>
    /// Abre el cuaderno ENCIMA del nivel, sin descargarlo ni recargarlo. El nivel se queda cargado
    /// y con el tiempo detenido, así que Nilo, la batería, los puentes, las compuertas y los
    /// contadores siguen exactamente donde estaban: no hace falta guardar ni restaurar nada.
    /// </summary>
    public void IrAlCuaderno()
    {
        if (!Application.CanStreamedLevelBeLoaded(NombreEscenaCuaderno))
        {
            GameEvents.RaiseMensajeMostrado("Cuaderno de viaje",
                "El cuaderno llega en la siguiente fase del prototipo. Tu progreso ya se guarda en disco.");
            return;
        }

        // Ya está abierto: abrir dos veces duplicaría la escena y su lienzo.
        if (SceneManager.GetSceneByName(NombreEscenaCuaderno).isLoaded) return;

        EscenaOrigen = SceneManager.GetActiveScene().name;

        // El nivel no se descarga, así que GameEvents y RelevoGame se conservan intactos.
        Time.timeScale = 0f;
        SceneManager.LoadScene(NombreEscenaCuaderno, LoadSceneMode.Additive);
    }

    public void IrASiguienteNivel()
    {
        int siguiente = IndiceNivel + 1;

        if (LevelLayouts.Obtener(siguiente) == null)
        {
            GameEvents.RaiseMensajeMostrado("Continuará",
                "El Nivel " + siguiente + " llega en la siguiente fase del prototipo. Este nivel queda guardado como completado.");
            return;
        }

        Time.timeScale = 1f;
        GameEvents.Limpiar();
        CargarEscena(NombreEscenaNivel(siguiente));
    }

    static void CargarEscena(string nombre)
    {
        if (Application.CanStreamedLevelBeLoaded(nombre))
        {
            SceneManager.LoadScene(nombre);
            return;
        }

        GameEvents.RaiseMensajeMostrado("Aviso",
            "La escena '" + nombre + "' todavía no está en Build Settings.");
    }

    public const string NombreEscenaCuaderno = "cuadernoRelevo";

    public static string NombreEscenaNivel(int indice)
    {
        switch (indice)
        {
            case 1: return "Nivel1_MuelleDeCobre";
            case 2: return "Nivel2_PatioDeBombas";
            case 3: return "Nivel3_FaroDeLiria";
            default: return "Nivel1_MuelleDeCobre";
        }
    }
}
