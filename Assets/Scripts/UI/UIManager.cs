using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD y menús de RELEVO, construidos por código con UGUI y sin assets externos.
///
/// La presentación sigue la identidad de Liria: paneles azul verdoso muy oscuros, filetes de cobre,
/// ámbar para la energía y la selección, turquesa para los mecanismos manuales y coral solo para
/// advertencias. El escenario se deja despejado: todo lo informativo vive en las esquinas y en el
/// borde inferior, y el diálogo aparece por debajo del centro de la pantalla.
///
/// La lógica no cambia en absoluto: se siguen leyendo los mismos eventos de GameEvents y se sigue
/// llamando a LevelManager para pausar, pedir pista, reiniciar, ir al cuaderno y completar. Los
/// menús se navegan con el propio asset de entrada (mover + confirmar + cancelar), así funcionan
/// igual con teclado y con mando y no dependen de un EventSystem.
/// </summary>
public class UIManager : MonoBehaviour
{
    static readonly string[] OpcionesPausa = { "Reanudar", "Pedir pista a Maia", "Reiniciar nivel", "Cuaderno de viaje", "Salir al menú" };
    static readonly string[] OpcionesFinal = { "Siguiente nivel", "Reiniciar nivel", "Cuaderno de viaje" };
    static readonly string[] OpcionesConfirmar = { "Sí, reiniciar", "No, continuar" };

    /// <summary>Una opción de menú: fondo, marco, indicador hexagonal y texto.</summary>
    class Fila
    {
        public GameObject raiz;
        public Image fondo;
        public Image indicador;
        public Text texto;
        public Image[] filetes;
    }

    LevelManager nivel;
    RelevoInput entrada;
    LevelDefinition definicion;

    // --- HUD
    GameObject grupoHUD;
    Text tituloNivel;
    Text textoObjetivo;
    Image hexagonoBateria;
    Text textoBateria;
    Text textoRecuerdos;

    // --- aviso contextual
    GameObject panelPrompt;
    Image franjaPrompt;
    Image capsulaPrompt;
    Text letraPrompt;
    Text textoPrompt;
    bool promptVisible;

    // --- diálogo
    GameObject panelDialogo;
    Image franjaDialogo;
    Image anilloDialogo;
    Image iconoHoja;
    Image iconoHexagono;
    Image iconoCapsula;
    Text textoMensajeTitulo;
    Text textoMensajeCuerpo;
    float tiempoMensaje;
    bool mostrandoMensaje;

    // --- pausa
    GameObject panelPausa;
    RectTransform tarjetaPausa;
    Text textoPausa;
    Text subtituloPausa;
    GameObject grupoOpciones;
    GameObject grupoConfirmar;
    Text textoConfirmar;
    Fila[] filasPausa;
    Fila[] filasConfirmar;

    // --- final
    GameObject panelFinal;
    RectTransform tarjetaFinal;
    Text textoFinal;
    Text textoFinalMensaje;
    Text textoFinalRecuerdos;
    Fila[] filasFinal;
    Image bandaAmbar;
    Image bandaTurquesa;

    bool enPausa;
    bool completado;
    bool confirmandoReinicio;
    int seleccionPausa;
    int seleccionFinal;
    int seleccionConfirmar;
    bool flanco;

    public bool BloqueaJuego { get { return enPausa || completado; } }

    void Awake()
    {
        ConstruirHUD();
    }

    void OnEnable()
    {
        GameEvents.BateriaCambiada += AlCambiarBateria;
        GameEvents.ObjetivoCambiado += AlCambiarObjetivo;
        GameEvents.PromptCambiado += AlCambiarPrompt;
        GameEvents.RecuerdosCambiados += AlCambiarRecuerdos;
        GameEvents.MensajeMostrado += AlMostrarMensaje;
        GameEvents.NivelCompletado += AlCompletarNivel;
    }

    void OnDisable()
    {
        GameEvents.BateriaCambiada -= AlCambiarBateria;
        GameEvents.ObjetivoCambiado -= AlCambiarObjetivo;
        GameEvents.PromptCambiado -= AlCambiarPrompt;
        GameEvents.RecuerdosCambiados -= AlCambiarRecuerdos;
        GameEvents.MensajeMostrado -= AlMostrarMensaje;
        GameEvents.NivelCompletado -= AlCompletarNivel;
    }

    public void Inicializar(LevelManager nivel, RelevoInput entrada, LevelDefinition definicion)
    {
        this.nivel = nivel;
        this.entrada = entrada;
        this.definicion = definicion;

        if (tituloNivel != null) tituloNivel.text = NombreDelNivel().ToUpperInvariant();

        AplicarBloqueo();
    }

    string NombreDelNivel()
    {
        if (definicion != null && !string.IsNullOrEmpty(definicion.nombre)) return definicion.nombre;
        if (nivel != null && !string.IsNullOrEmpty(nivel.NombreNivel)) return nivel.NombreNivel;
        return "Liria";
    }

    bool EsFaro()
    {
        if (definicion != null) return definicion.salidaEsFaro;
        return nivel != null && nivel.IndiceNivel == 3;
    }

    // ------------------------------------------------------------------ entrada

    void Update()
    {
        if (mostrandoMensaje && panelDialogo != null && panelDialogo.activeSelf)
        {
            tiempoMensaje -= Time.unscaledDeltaTime;
            if (tiempoMensaje <= 0f)
            {
                mostrandoMensaje = false;
                RelevoUIAnim.Salida(panelDialogo, 0.24f, null);
            }
        }

        RefrescarFondoPrompt();

        if (entrada == null) return;

        if (completado)
        {
            Navegar(OpcionesFinal, ref seleccionFinal, ElegirFinal, RefrescarFinal);
            return;
        }

        if (confirmandoReinicio)
        {
            if (entrada.Pulsado(entrada.Cancelar) || entrada.Pulsado(entrada.Pausa))
            {
                CancelarConfirmacion();
                return;
            }

            Navegar(OpcionesConfirmar, ref seleccionConfirmar, ElegirConfirmar, RefrescarConfirmacion);
            return;
        }

        if (enPausa)
        {
            if (entrada.Pulsado(entrada.Cancelar) || entrada.Pulsado(entrada.Pausa)) { CerrarPausa(); return; }

            Navegar(OpcionesPausa, ref seleccionPausa, ElegirPausa, RefrescarPausa);
            return;
        }

        if (entrada.Pulsado(entrada.Pausa)) AbrirPausa();
        else if (entrada.Pulsado(entrada.Reiniciar)) PedirConfirmacion();
        else if (entrada.Pulsado(entrada.Cuaderno) && nivel != null) nivel.IrAlCuaderno();
    }

    /// <summary>Navegación por flanco: el movimiento no repite opción mientras se mantiene.</summary>
    void Navegar(string[] opciones, ref int indice, System.Action<int> alElegir, System.Action refrescar)
    {
        if (entrada.Pulsado(entrada.Confirmar))
        {
            alElegir(indice);
            return;
        }

        Vector2 mover = entrada.LeerMovimiento();
        float eje = Mathf.Abs(mover.y) >= Mathf.Abs(mover.x) ? mover.y : mover.x;

        if (Mathf.Abs(eje) < 0.5f)
        {
            flanco = false;
            return;
        }

        if (flanco) return;
        flanco = true;

        indice += eje > 0f ? -1 : 1;
        if (indice < 0) indice = opciones.Length - 1;
        if (indice >= opciones.Length) indice = 0;

        refrescar();
    }

    /// <summary>
    /// El aviso contextual solo tiene sentido cuando hay algo que hacer: se enciende y se apaga
    /// con el texto, con una entrada y una salida suaves. Se resuelve aquí y no en el evento
    /// porque los menús de pausa y de cierre vacían el aviso por su cuenta.
    /// </summary>
    void RefrescarFondoPrompt()
    {
        if (panelPrompt == null || textoPrompt == null) return;

        bool visible = !string.IsNullOrEmpty(textoPrompt.text) && !BloqueaJuego;
        if (visible == promptVisible) return;

        promptVisible = visible;
        if (visible) RelevoUIAnim.Entrada(panelPrompt, 0.16f, -12f, true);
        else RelevoUIAnim.Salida(panelPrompt, 0.14f, null);
    }

    // ------------------------------------------------------------------ pausa y cierre

    void AbrirPausa()
    {
        if (enPausa || completado) return;

        enPausa = true;
        confirmandoReinicio = false;
        seleccionPausa = 0;
        Time.timeScale = 0f;

        if (textoPrompt != null) textoPrompt.text = "";
        promptVisible = false;
        if (panelPrompt != null) RelevoUIAnim.Salida(panelPrompt, 0.12f, null);

        RefrescarPausa();
        MostrarMenu(panelPausa, tarjetaPausa);
        AplicarBloqueo();
    }

    void CerrarPausa()
    {
        enPausa = false;
        confirmandoReinicio = false;
        Time.timeScale = 1f;

        OcultarMenu(panelPausa);
        AplicarBloqueo();
    }

    void PedirConfirmacion()
    {
        enPausa = true;
        confirmandoReinicio = true;
        seleccionConfirmar = 1;
        Time.timeScale = 0f;

        if (textoPrompt != null) textoPrompt.text = "";
        promptVisible = false;
        if (panelPrompt != null) RelevoUIAnim.Salida(panelPrompt, 0.12f, null);

        RefrescarConfirmacion();
        MostrarMenu(panelPausa, tarjetaPausa);
        AplicarBloqueo();
    }

    /// <summary>
    /// Esc durante la confirmación: vuelve al menú de pausa en lugar de cerrarlo.
    /// Vale igual si la confirmación se abrió con R durante el juego o desde el menú.
    /// </summary>
    void CancelarConfirmacion()
    {
        confirmandoReinicio = false;
        RefrescarPausa();
    }

    void ElegirPausa(int indice)
    {
        switch (indice)
        {
            case 0:
                CerrarPausa();
                break;

            case 1:
                if (nivel != null) nivel.MostrarPista();
                CerrarPausa();
                break;

            case 2:
                confirmandoReinicio = true;
                seleccionConfirmar = 1;
                RefrescarConfirmacion();
                break;

            case 3:
                CerrarPausa();
                if (nivel != null) nivel.IrAlCuaderno();
                break;

            case 4:
                // Todavía no hay escena de menú principal en el proyecto (Build Settings arranca
                // en el nivel). Se avisa con el mismo mensaje de "siguiente fase" que usa el
                // propio juego y la pausa sigue abierta, sin inventar una mecánica nueva.
                GameEvents.RaiseMensajeMostrado("Menú principal",
                    "El menú principal todavía no existe como escena. Tu progreso ya está guardado.");
                break;
        }
    }

    void ElegirConfirmar(int indice)
    {
        if (indice == 0)
        {
            confirmandoReinicio = false;
            Time.timeScale = 1f;
            if (nivel != null) nivel.ReiniciarNivel();
            return;
        }

        CerrarPausa();
    }

    void ElegirFinal(int indice)
    {
        if (nivel == null) return;

        switch (indice)
        {
            case 0: nivel.IrASiguienteNivel(); break;
            case 1: nivel.ReiniciarNivel(); break;
            case 2: nivel.IrAlCuaderno(); break;
        }
    }

    void AplicarBloqueo()
    {
        if (RelevoGame.Jugador != null) RelevoGame.Jugador.PuedeMoverse = !BloqueaJuego;

        // Con un menú delante, el HUD se atenúa: la jerarquía se lee de un vistazo.
        if (grupoHUD != null) RelevoUIAnim.FijarAlfa(grupoHUD, BloqueaJuego ? 0.18f : 1f);
    }

    // ------------------------------------------------------------------ refresco de menús

    void RefrescarPausa()
    {
        if (textoPausa != null) textoPausa.text = "PAUSA";
        if (subtituloPausa != null) subtituloPausa.text = NombreDelNivel() + "  ·  cuaderno de mantenimiento";

        AplicarModo(false);
        RefrescarFilas(filasPausa, OpcionesPausa, seleccionPausa);
    }

    void RefrescarConfirmacion()
    {
        if (textoPausa != null) textoPausa.text = "REINICIAR NIVEL";
        if (subtituloPausa != null) subtituloPausa.text = "Muelle de obra: se pierde el avance no guardado.";
        if (textoConfirmar != null) textoConfirmar.text = "Se perderá el progreso no guardado de este nivel.";

        AplicarModo(true);
        RefrescarFilas(filasConfirmar, OpcionesConfirmar, seleccionConfirmar);
    }

    void AplicarModo(bool confirmando)
    {
        if (grupoOpciones != null) grupoOpciones.SetActive(!confirmando);
        if (grupoConfirmar != null) grupoConfirmar.SetActive(confirmando);
    }

    void RefrescarFinal()
    {
        if (textoFinal == null) return;

        bool faro = EsFaro();
        string titulo = faro ? "EL FARO VUELVE A BRILLAR" : "NIVEL RESTAURADO";
        textoFinal.text = titulo;
        textoFinal.color = RelevoUI.Ambar;

        if (textoFinalMensaje != null)
        {
            string cierre = definicion != null ? definicion.textoCierre : "";
            textoFinalMensaje.text = string.IsNullOrEmpty(cierre)
                ? "Liria vuelve a tener energía en este tramo del canal."
                : cierre;
        }

        if (textoFinalRecuerdos != null)
        {
            int indice = nivel != null ? nivel.IndiceNivel : 1;
            int encontrados = SaveManager.RecuerdosDelNivel(indice);
            textoFinalRecuerdos.text = encontrados > 0
                ? "Recuerdos encontrados en este tramo: " + encontrados + " de 1."
                : "Sin recuerdo en este tramo: el orbe ámbar sigue esperando.";
        }

        RefrescarFilas(filasFinal, OpcionesFinal, seleccionFinal);
    }

    void RefrescarFilas(Fila[] filas, string[] opciones, int seleccion)
    {
        if (filas == null) return;

        for (int i = 0; i < filas.Length; i++)
        {
            string etiqueta = i < opciones.Length ? opciones[i] : "";
            RefrescarFila(filas[i], etiqueta, i == seleccion);
        }
    }

    /// <summary>
    /// La opción con el foco se marca con tres señales a la vez: fondo más claro, marco ámbar e
    /// indicador hexagonal. Las demás quedan en cobre apagado, que es la jerarquía del cuaderno.
    /// </summary>
    void RefrescarFila(Fila fila, string etiqueta, bool seleccionada)
    {
        if (fila == null) return;

        if (fila.texto != null)
        {
            fila.texto.text = etiqueta;
            fila.texto.color = seleccionada ? RelevoUI.AmbarSuave : RelevoUI.ConAlfa(RelevoUI.Tinta, 0.82f);
        }

        if (fila.fondo != null)
        {
            fila.fondo.color = seleccionada
                ? RelevoUI.FondoSeleccion
                : RelevoUI.ConAlfa(RelevoUI.FondoHondo, 0.88f);
        }

        Color borde = seleccionada ? RelevoUI.Ambar : RelevoUI.CobreTenue;
        if (fila.filetes != null)
        {
            for (int i = 0; i < fila.filetes.Length; i++) fila.filetes[i].color = borde;
        }

        if (fila.indicador != null) fila.indicador.gameObject.SetActive(seleccionada);
    }

    // ------------------------------------------------------------------ eventos

    void AlCambiarBateria(EstadoBateria estado, string etiquetaBase)
    {
        if (textoBateria == null) return;

        string valor;
        switch (estado)
        {
            case EstadoBateria.EnManos: valor = "EN MANOS DE NILO"; break;
            case EstadoBateria.EnBase:
                valor = string.IsNullOrEmpty(etiquetaBase) ? "ACOPLADA" : "EN " + etiquetaBase.ToUpperInvariant();
                break;
            default: valor = "EN EL SUELO"; break;
        }

        textoBateria.text = valor;

        // El hexágono se enciende cuando la energía está en juego (manos o base) y queda apagado
        // mientras la batería espera en el suelo.
        if (hexagonoBateria != null)
        {
            hexagonoBateria.color = estado == EstadoBateria.EnSuelo
                ? RelevoUI.ConAlfa(RelevoUI.Ambar, 0.38f)
                : RelevoUI.Ambar;
        }
    }

    void AlCambiarObjetivo(string objetivo)
    {
        // La etiqueta "OBJETIVO" ya está en el panel, así que el texto va sin prefijo.
        if (textoObjetivo != null) textoObjetivo.text = objetivo;
    }

    void AlCambiarRecuerdos(int encontrados, int total)
    {
        if (textoRecuerdos == null) return;

        textoRecuerdos.text = total > 0 ? encontrados + " / " + total : "sin recuerdo";
    }

    void AlCambiarPrompt(string texto, bool disponible)
    {
        if (textoPrompt == null) return;

        Color color = ColorDeAccion(texto, disponible);

        textoPrompt.text = texto;
        textoPrompt.color = color;
        if (capsulaPrompt != null) capsulaPrompt.color = color;
        if (franjaPrompt != null) franjaPrompt.color = color;
        if (letraPrompt != null) letraPrompt.color = RelevoUI.FondoHondo;
    }

    void AlMostrarMensaje(string titulo, string texto)
    {
        if (panelDialogo == null) return;

        Color acento = ColorDeEmisor(titulo);

        if (textoMensajeTitulo != null)
        {
            textoMensajeTitulo.text = titulo;
            textoMensajeTitulo.color = acento;
        }

        if (textoMensajeCuerpo != null) textoMensajeCuerpo.text = texto;

        if (franjaDialogo != null) franjaDialogo.color = RelevoUI.ConAlfa(acento, 0.9f);
        if (anilloDialogo != null) anilloDialogo.color = acento;

        IconoDeEmisor(titulo);

        tiempoMensaje = Mathf.Clamp(4.5f + (texto != null ? texto.Length : 0) * 0.03f, 5f, 13f);
        mostrandoMensaje = true;
        RelevoUIAnim.Entrada(panelDialogo, 0.24f, -22f, true);
    }

    void AlCompletarNivel(string nombre)
    {
        completado = true;
        enPausa = false;
        confirmandoReinicio = false;
        seleccionFinal = 0;

        OcultarMenu(panelPausa);
        RefrescarFinal();
        MostrarMenu(panelFinal, tarjetaFinal);
        AplicarBloqueo();

        StopCoroutine("DestelloFinal");
        StartCoroutine("DestelloFinal");
    }

    /// <summary>
    /// Transición breve de cierre: una luz ámbar y otra turquesa salen del centro de la tarjeta
    /// hacia sus bordes y se apagan. Son dos imágenes, no luces de escena.
    /// </summary>
    IEnumerator DestelloFinal()
    {
        if (bandaAmbar == null || bandaTurquesa == null) yield break;

        bandaAmbar.gameObject.SetActive(true);
        bandaTurquesa.gameObject.SetActive(true);

        float duracion = 0.75f;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float e = Mathf.Clamp01(t / duracion);
            float suave = 1f - (1f - e) * (1f - e);

            ColocarBanda(bandaAmbar, Mathf.Lerp(0f, 268f, suave), Mathf.Lerp(0.95f, 0f, suave));
            ColocarBanda(bandaTurquesa, Mathf.Lerp(0f, -268f, suave), Mathf.Lerp(0.8f, 0f, suave));
            yield return null;
        }

        bandaAmbar.gameObject.SetActive(false);
        bandaTurquesa.gameObject.SetActive(false);
    }

    static void ColocarBanda(Image banda, float y, float alfa)
    {
        RectTransform rt = banda.rectTransform;
        rt.anchoredPosition = new Vector2(0f, y);
        banda.color = RelevoUI.ConAlfa(banda.color, alfa);
    }

    // ------------------------------------------------------------------ colores por acción

    /// <summary>
    /// Color del aviso según lo que va a pasar: ámbar para la batería y la energía, turquesa para
    /// pasarelas y mecanismos manuales, blanco para hablar y coral cuando no se puede hacer nada.
    /// </summary>
    static Color ColorDeAccion(string texto, bool disponible)
    {
        if (!disponible) return RelevoUI.Coral;

        string t = texto != null ? texto.ToLowerInvariant() : "";

        if (t.Contains("palanca") || t.Contains("pasarela") || t.Contains("mecanismo")) return RelevoUI.Turquesa;
        if (t.Contains("maia") || t.Contains("hablar")) return RelevoUI.Tinta;
        if (t.Contains("bater") || t.Contains("faro") || t.Contains("recuerdo") || t.Contains("salir")) return RelevoUI.Ambar;

        return RelevoUI.Tinta;
    }

    /// <summary>Color del identificador de quien habla, y por tanto de su filo y de su icono.</summary>
    static Color ColorDeEmisor(string titulo)
    {
        string t = titulo != null ? titulo.ToLowerInvariant() : "";

        if (t.Contains("maia")) return RelevoUI.Verde;
        if (t.Contains("mecanismo")) return RelevoUI.Turquesa;
        if (t.Contains("aviso") || t.Contains("salida") || t.Contains("continuar")) return RelevoUI.Coral;
        if (t.Contains("recuerdo") || t.Contains("completado") || t.Contains("menú") || t.Contains("menu")) return RelevoUI.Ambar;

        return RelevoUI.AmbarSuave;
    }

    /// <summary>
    /// Marca visual del emisor: hoja para Maia (la botánica de Liria), cápsula para los recuerdos
    /// del cuaderno y conector hexagonal para los avisos del sistema.
    /// </summary>
    void IconoDeEmisor(string titulo)
    {
        string t = titulo != null ? titulo.ToLowerInvariant() : "";

        bool hoja = t.Contains("maia");
        bool capsula = t.Contains("recuerdo");

        if (iconoHoja != null) iconoHoja.gameObject.SetActive(hoja);
        if (iconoCapsula != null) iconoCapsula.gameObject.SetActive(capsula);
        if (iconoHexagono != null) iconoHexagono.gameObject.SetActive(!hoja && !capsula);
    }

    // ------------------------------------------------------------------ animación de menús

    void MostrarMenu(GameObject raiz, RectTransform tarjeta)
    {
        if (raiz == null) return;

        RelevoUIAnim.Entrada(raiz, 0.18f, 0f, true);
        if (tarjeta != null) RelevoUIAnim.Entrada(tarjeta.gameObject, 0.24f, -24f, true);
    }

    void OcultarMenu(GameObject raiz)
    {
        if (raiz == null || !raiz.activeSelf) return;
        RelevoUIAnim.Salida(raiz, 0.16f, null);
    }

    // ------------------------------------------------------------------ construcción del HUD

    void ConstruirHUD()
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

        Transform raiz = lienzo.transform;

        // --- HUD: todo lo informativo vive en las esquinas para no tapar el puzle
        grupoHUD = new GameObject("HUD");
        grupoHUD.transform.SetParent(raiz, false);
        RectTransform rtHUD = grupoHUD.AddComponent<RectTransform>();
        rtHUD.anchorMin = Vector2.zero;
        rtHUD.anchorMax = Vector2.one;
        rtHUD.offsetMin = Vector2.zero;
        rtHUD.offsetMax = Vector2.zero;

        ConstruirObjetivo(grupoHUD.transform);
        ConstruirEstado(grupoHUD.transform);
        ConstruirControles(grupoHUD.transform);
        ConstruirPrompt(grupoHUD.transform);

        // --- diálogo: por encima de los menús, para que un mensaje nunca quede tapado
        ConstruirDialogo(raiz);
        ConstruirPausa(raiz);
        ConstruirFinal(raiz);
    }

    /// <summary>Arriba a la izquierda: título del nivel y objetivo actual, en una tarjeta compacta.</summary>
    void ConstruirObjetivo(Transform padre)
    {
        GameObject panel = RelevoUI.Panel(padre, "PanelObjetivo", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(22f, -22f), new Vector2(520f, 118f), RelevoUI.Fondo);

        RelevoUI.Marco(panel.transform, RelevoUI.Cobre, 2f);
        RelevoUI.Esquinas(panel.transform, 16f, 2f, RelevoUI.Cobre);

        tituloNivel = RelevoUI.Texto(panel.transform, "Titulo", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(18f, -12f), new Vector2(452f, 28f), 21, TextAnchor.UpperLeft, RelevoUI.AmbarSuave);
        tituloNivel.text = "LIRIA";
        tituloNivel.fontStyle = FontStyle.Bold;

        RelevoUI.Hexagono(panel.transform, "Conector", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-16f, -14f), new Vector2(24f, 24f), RelevoUI.Ambar, true);

        // Cable de cobre con sus dos remaches: el filete que separa el título del objetivo.
        RelevoUI.Cable(panel.transform, "Cable", new Vector2(-242f, 12f), new Vector2(242f, 12f), 2f, RelevoUI.CobreTenue);

        RelevoUI.Texto(panel.transform, "Etiqueta", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(18f, -46f), new Vector2(484f, 18f), 14, TextAnchor.UpperLeft,
            RelevoUI.ConAlfa(RelevoUI.Cobre, 0.95f)).text = "OBJETIVO";

        textoObjetivo = RelevoUI.Texto(panel.transform, "Objetivo", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(18f, -64f), new Vector2(484f, 46f), 18, TextAnchor.UpperLeft,
            RelevoUI.ConAlfa(RelevoUI.Tinta, 0.96f));
    }

    /// <summary>Abajo a la derecha: la batería con su conector hexagonal y el contador de recuerdos.</summary>
    void ConstruirEstado(Transform padre)
    {
        // --- batería
        GameObject bateria = RelevoUI.Panel(padre, "PanelBateria", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-22f, 22f), new Vector2(380f, 72f), RelevoUI.Fondo);
        RelevoUI.Marco(bateria.transform, RelevoUI.Cobre, 2f);

        hexagonoBateria = RelevoUI.Hexagono(bateria.transform, "Conector", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(18f, 21f), new Vector2(30f, 30f), RelevoUI.Ambar, true);

        RelevoUI.Texto(bateria.transform, "Etiqueta", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(60f, 46f), new Vector2(300f, 16f), 13, TextAnchor.LowerLeft,
            RelevoUI.ConAlfa(RelevoUI.Cobre, 0.95f)).text = "BATERÍA";

        textoBateria = RelevoUI.Texto(bateria.transform, "Estado", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(60f, 16f), new Vector2(300f, 26f), 19, TextAnchor.LowerLeft, RelevoUI.AmbarSuave);
        textoBateria.text = "EN EL SUELO";

        RelevoUI.Cable(bateria.transform, "Cable", new Vector2(-166f, -22f), new Vector2(166f, -22f), 2f, RelevoUI.CobreTenue);

        // --- recuerdos
        GameObject recuerdos = RelevoUI.Panel(padre, "PanelRecuerdos", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-22f, 104f), new Vector2(380f, 58f), RelevoUI.Fondo);
        RelevoUI.Marco(recuerdos.transform, RelevoUI.Cobre, 2f);

        RelevoUI.Capsula(recuerdos.transform, "Capsula", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(18f, 0f), new Vector2(20f, 30f), RelevoUI.Cobre);

        RelevoUI.Texto(recuerdos.transform, "Etiqueta", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(50f, 0f), new Vector2(200f, 24f), 14, TextAnchor.MiddleLeft,
            RelevoUI.ConAlfa(RelevoUI.Cobre, 0.95f)).text = "RECUERDOS";

        textoRecuerdos = RelevoUI.Texto(recuerdos.transform, "Cuenta", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-18f, 0f), new Vector2(140f, 24f), 19, TextAnchor.MiddleRight, RelevoUI.AmbarSuave);
        textoRecuerdos.text = "0 / 1";
    }

    /// <summary>Abajo a la izquierda: la leyenda de controles, con cada tecla en su cápsula.</summary>
    void ConstruirControles(Transform padre)
    {
        GameObject panel = RelevoUI.Panel(padre, "PanelControles", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(22f, 22f), new Vector2(780f, 46f), RelevoUI.ConAlfa(RelevoUI.Fondo, 0.62f));
        RelevoUI.Marco(panel.transform, RelevoUI.CobreTenue, 1f);

        Color tecla = RelevoUI.ConAlfa(RelevoUI.Cobre, 0.85f);
        Color verbo = RelevoUI.ConAlfa(RelevoUI.Tinta, 0.62f);

        float x = 14f;
        x = Chip(panel.transform, x, 8f, "WASD", "mover", tecla, verbo, 15);
        x = Chip(panel.transform, x, 8f, "E", "interactuar", tecla, verbo, 15);
        x = Chip(panel.transform, x, 8f, "Esc", "pausa", tecla, verbo, 15);
        x = Chip(panel.transform, x, 8f, "R", "reiniciar", tecla, verbo, 15);
        Chip(panel.transform, x, 8f, "Tab", "cuaderno", tecla, verbo, 15);
    }

    /// <summary>
    /// Abajo al centro: el aviso contextual. La tecla E va en su cápsula, separada del verbo por un
    /// filete de cobre, y todo el aviso cambia de color según la acción.
    /// </summary>
    void ConstruirPrompt(Transform padre)
    {
        panelPrompt = RelevoUI.Panel(padre, "PanelPrompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 120f), new Vector2(900f, 72f), RelevoUI.ConAlfa(RelevoUI.Fondo, 0.82f));

        RelevoUI.Marco(panelPrompt.transform, RelevoUI.Cobre, 2f);

        franjaPrompt = RelevoUI.Franja(panelPrompt.transform, "Franja", new Vector2(1f, 1f), new Vector2(0f, 1f),
            Vector2.zero, new Vector2(0f, 3f), RelevoUI.Ambar);

        capsulaPrompt = RelevoUI.Capsula(panelPrompt.transform, "Tecla E", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(18f, 0f), new Vector2(56f, 40f), RelevoUI.Ambar);

        letraPrompt = RelevoUI.Texto(panelPrompt.transform, "Letra", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(18f, 0f), new Vector2(56f, 40f), 26, TextAnchor.MiddleCenter, RelevoUI.FondoHondo);
        letraPrompt.text = "E";
        letraPrompt.fontStyle = FontStyle.Bold;

        RelevoUI.Franja(panelPrompt.transform, "Filete", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(86f, 0f), new Vector2(2f, 44f), RelevoUI.CobreTenue);

        textoPrompt = RelevoUI.Texto(panelPrompt.transform, "Prompt", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(38f, 0f), new Vector2(744f, 48f), 28, TextAnchor.MiddleCenter, RelevoUI.Ambar);

        panelPrompt.SetActive(false);
        promptVisible = false;
    }

    /// <summary>
    /// Caja de diálogo: identificador del emisor, su nombre y el texto con aire. Es hermana de los
    /// menús y va la última, para que ningún mensaje quede tapado.
    /// </summary>
    void ConstruirDialogo(Transform padre)
    {
        panelDialogo = RelevoUI.Panel(padre, "PanelDialogo", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 236f), new Vector2(1040f, 206f), RelevoUI.ConAlfa(RelevoUI.FondoHondo, 0.94f));

        RelevoUI.Marco(panelDialogo.transform, RelevoUI.Cobre, 2f);
        RelevoUI.Esquinas(panelDialogo.transform, 18f, 2f, RelevoUI.Cobre);

        // Filo lateral con el color del emisor.
        franjaDialogo = RelevoUI.Franja(panelDialogo.transform, "Filo", new Vector2(0f, 1f), new Vector2(0f, 1f),
            Vector2.zero, new Vector2(3f, 0f), RelevoUI.Ambar);
        RectTransform rtFilo = franjaDialogo.rectTransform;
        rtFilo.anchorMin = new Vector2(0f, 0f);
        rtFilo.anchorMax = new Vector2(0f, 1f);
        rtFilo.sizeDelta = new Vector2(3f, 0f);
        rtFilo.anchoredPosition = Vector2.zero;

        // Identificador del emisor: anillo hexagonal con el icono dentro.
        anilloDialogo = RelevoUI.Hexagono(panelDialogo.transform, "Anillo", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(24f, 0f), new Vector2(56f, 56f), RelevoUI.AmbarSuave, false);

        iconoHoja = RelevoUI.Hoja(panelDialogo.transform, "Icono hoja", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(38f, 0f), new Vector2(28f, 28f), RelevoUI.Verde);

        iconoHexagono = RelevoUI.Hexagono(panelDialogo.transform, "Icono hexagono", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(36f, 0f), new Vector2(32f, 32f), RelevoUI.AmbarSuave, true);

        iconoCapsula = RelevoUI.Capsula(panelDialogo.transform, "Icono capsula", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(39f, 0f), new Vector2(26f, 34f), RelevoUI.AmbarSuave);

        RelevoUI.Franja(panelDialogo.transform, "Filete", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(96f, -18f), new Vector2(2f, 170f), RelevoUI.CobreTenue);

        textoMensajeTitulo = RelevoUI.Texto(panelDialogo.transform, "Titulo", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(112f, -20f), new Vector2(900f, 30f), 23, TextAnchor.UpperLeft, RelevoUI.AmbarSuave);
        textoMensajeTitulo.fontStyle = FontStyle.Bold;

        textoMensajeCuerpo = RelevoUI.Texto(panelDialogo.transform, "Cuerpo", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(112f, -58f), new Vector2(906f, 132f), 20, TextAnchor.UpperLeft,
            RelevoUI.ConAlfa(RelevoUI.Tinta, 0.96f));

        panelDialogo.SetActive(false);
    }

    // ------------------------------------------------------------------ construcción de menús

    void ConstruirPausa(Transform padre)
    {
        panelPausa = RelevoUI.Panel(padre, "PanelPausa", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(4000f, 4000f), RelevoUI.Velo);

        tarjetaPausa = RelevoUI.Panel(panelPausa.transform, "TarjetaPausa", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(880f, 600f), RelevoUI.Fondo).GetComponent<RectTransform>();

        Transform tarjeta = tarjetaPausa.transform;
        RelevoUI.Marco(tarjeta, RelevoUI.Cobre, 2f);
        RelevoUI.Esquinas(tarjeta, 22f, 2f, RelevoUI.Cobre);
        RelevoUI.Cable(tarjeta, "Cable", new Vector2(-368f, 268f), new Vector2(368f, 268f), 2f, RelevoUI.CobreTenue);

        textoPausa = RelevoUI.Texto(tarjeta, "Titulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(800f, 54f), 40, TextAnchor.UpperCenter, RelevoUI.Ambar);
        textoPausa.text = "PAUSA";
        textoPausa.fontStyle = FontStyle.Bold;

        subtituloPausa = RelevoUI.Texto(tarjeta, "Subtitulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -86f), new Vector2(800f, 26f), 16, TextAnchor.UpperCenter,
            RelevoUI.ConAlfa(RelevoUI.Turquesa, 0.9f));

        RelevoUI.Cable(tarjeta, "Cable bajo titulo", new Vector2(-370f, 186f), new Vector2(370f, 186f), 2f, RelevoUI.CobreTenue);

        // --- opciones
        grupoOpciones = Grupo(tarjeta, "Opciones");
        filasPausa = CrearFilas(grupoOpciones.transform, OpcionesPausa.Length, 760f, 64f, -134f);

        // --- confirmación de reinicio, dentro de la misma tarjeta
        grupoConfirmar = Grupo(tarjeta, "Confirmar");
        textoConfirmar = RelevoUI.Texto(grupoConfirmar.transform, "Aviso", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -140f), new Vector2(800f, 28f), 20, TextAnchor.UpperCenter, RelevoUI.Coral);
        filasConfirmar = CrearFilas(grupoConfirmar.transform, OpcionesConfirmar.Length, 760f, 64f, -196f);

        Leyenda(tarjeta, -448f, 16, true);

        panelPausa.SetActive(false);
    }

    void ConstruirFinal(Transform padre)
    {
        panelFinal = RelevoUI.Panel(padre, "PanelFinal", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(4000f, 4000f), RelevoUI.Velo);

        tarjetaFinal = RelevoUI.Panel(panelFinal.transform, "TarjetaFinal", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900f, 620f), RelevoUI.Fondo).GetComponent<RectTransform>();

        Transform tarjeta = tarjetaFinal.transform;
        RelevoUI.Marco(tarjeta, RelevoUI.Cobre, 2f);
        RelevoUI.Esquinas(tarjeta, 22f, 2f, RelevoUI.Cobre);
        RelevoUI.Cable(tarjeta, "Cable", new Vector2(-378f, 278f), new Vector2(378f, 278f), 2f, RelevoUI.CobreTenue);

        textoFinal = RelevoUI.Texto(tarjeta, "Titulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -44f), new Vector2(820f, 60f), 38, TextAnchor.UpperCenter, RelevoUI.Ambar);
        textoFinal.fontStyle = FontStyle.Bold;

        textoFinalMensaje = RelevoUI.Texto(tarjeta, "Mensaje", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -114f), new Vector2(820f, 64f), 21, TextAnchor.UpperCenter,
            RelevoUI.ConAlfa(RelevoUI.Tinta, 0.96f));

        RelevoUI.Cable(tarjeta, "Cable bajo mensaje", new Vector2(-380f, 212f), new Vector2(380f, 212f), 2f, RelevoUI.CobreTenue);

        textoFinalRecuerdos = RelevoUI.Texto(tarjeta, "Recuerdos", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -208f), new Vector2(820f, 30f), 19, TextAnchor.UpperCenter,
            RelevoUI.ConAlfa(RelevoUI.AmbarSuave, 0.95f));

        filasFinal = CrearFilas(tarjeta, OpcionesFinal.Length, 760f, 64f, -262f);

        Leyenda(tarjeta, -496f, 16, false);

        // Bandas de la transición de cierre: salen del centro y se apagan.
        bandaAmbar = RelevoUI.Franja(tarjeta, "Banda ambar", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(880f, 5f), RelevoUI.ConAlfa(RelevoUI.Ambar, 0f));
        bandaTurquesa = RelevoUI.Franja(tarjeta, "Banda turquesa", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(880f, 3f), RelevoUI.ConAlfa(RelevoUI.Turquesa, 0f));
        bandaAmbar.gameObject.SetActive(false);
        bandaTurquesa.gameObject.SetActive(false);

        panelFinal.SetActive(false);
    }

    GameObject Grupo(Transform padre, string nombre)
    {
        GameObject grupo = new GameObject(nombre);
        grupo.transform.SetParent(padre, false);

        RectTransform rt = grupo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return grupo;
    }

    Fila[] CrearFilas(Transform padre, int cuantas, float ancho, float alto, float primeraY)
    {
        Fila[] filas = new Fila[cuantas];
        for (int i = 0; i < cuantas; i++)
        {
            float y = primeraY - i * (alto + 8f);
            filas[i] = CrearFila(padre, ancho, alto, y);
        }
        return filas;
    }

    Fila CrearFila(Transform padre, float ancho, float alto, float y)
    {
        Fila fila = new Fila();

        fila.raiz = RelevoUI.Panel(padre, "Fila", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), new Vector2(ancho, alto), RelevoUI.FondoHondo);
        fila.fondo = fila.raiz.GetComponent<Image>();

        List<Image> filetes = new List<Image>();
        RelevoUI.Marco(fila.raiz.transform, RelevoUI.CobreTenue, 2f, filetes);
        fila.filetes = filetes.ToArray();

        fila.indicador = RelevoUI.Hexagono(fila.raiz.transform, "Indicador", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(18f, 0f), new Vector2(22f, 22f), RelevoUI.Ambar, true);

        fila.texto = RelevoUI.Texto(fila.raiz.transform, "Texto", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(54f, 0f), new Vector2(ancho - 72f, alto - 6f), 22, TextAnchor.MiddleLeft, RelevoUI.Tinta);

        return fila;
    }

    /// <summary>Leyenda de controles de un menú, con cada tecla en su cápsula.</summary>
    void Leyenda(Transform tarjeta, float y, int tamano, bool conReiniciar)
    {
        Color tecla = RelevoUI.ConAlfa(RelevoUI.Cobre, 0.9f);
        Color verbo = RelevoUI.ConAlfa(RelevoUI.Tinta, 0.6f);

        float x = 42f;
        x = Chip(tarjeta, x, y, "↑↓", "elegir", tecla, verbo, tamano);
        x = Chip(tarjeta, x, y, "Enter", "confirmar", tecla, verbo, tamano);
        x = Chip(tarjeta, x, y, "Esc", conReiniciar ? "reanudar" : "cerrar", tecla, verbo, tamano);
        if (conReiniciar) Chip(tarjeta, x, y, "R", "reiniciar", tecla, verbo, tamano);
    }

    /// <summary>Tecla en cápsula más su verbo. Devuelve la X donde continúa la leyenda.</summary>
    float Chip(Transform padre, float x, float y, string tecla, string verbo, Color colorTecla, Color colorVerbo, int tamano)
    {
        float alto = tamano * 2f;
        float anchoTecla = Mathf.Max(34f, 18f + tecla.Length * tamano * 0.62f);

        RelevoUI.Capsula(padre, "Tecla " + tecla, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(x, y), new Vector2(anchoTecla, alto), colorTecla);

        Text etiqueta = RelevoUI.Texto(padre, "Tecla " + tecla + " texto", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(x, y), new Vector2(anchoTecla, alto), tamano, TextAnchor.MiddleCenter, RelevoUI.FondoHondo);
        etiqueta.text = tecla;
        etiqueta.fontStyle = FontStyle.Bold;

        if (string.IsNullOrEmpty(verbo)) return x + anchoTecla + 16f;

        float anchoVerbo = 24f + verbo.Length * tamano * 0.52f;
        RelevoUI.Texto(padre, "Verbo " + verbo, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(x + anchoTecla + 10f, y), new Vector2(anchoVerbo, alto), tamano, TextAnchor.MiddleLeft, colorVerbo)
            .text = verbo;

        return x + anchoTecla + 10f + anchoVerbo + 16f;
    }
}
