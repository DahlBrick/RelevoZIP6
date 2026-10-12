using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Diálogo al estilo de los RPG de retratos: el busto del que habla abajo a la izquierda, Nilo
/// escuchando abajo a la derecha, y el texto en burbujas oscuras, una por frase, que se escriben
/// letra a letra. Es modal: mientras está abierto el juego se detiene (UIManager.BloqueaJuego) y
/// se cierra con la tecla de interactuar, Enter o un clic.
///
/// Los retratos no son imágenes: son cámaras que miran al personaje de verdad y lo dibujan en una
/// textura con fondo transparente, así que respiran, parpadean y se mueven igual que en el nivel.
/// Para aislarlos, el personaje pasa a una capa propia mientras dura el diálogo.
/// </summary>
public sealed class DialogoUI : MonoBehaviour
{
    // Una capa por retrato: si Nilo está al lado de Maia, cada cámara ve solo a su personaje.
    const int CapaRetrato = 30;
    const int CapaRetratoNilo = 31;
    const float LetrasPorSegundo = 48f;
    const float AnchoMaximoBurbuja = 760f;

    /// <summary>
    /// Un mensaje del diálogo. Además del texto puede llevar una tarjeta ilustrada (Maia explicando
    /// una mecánica), opciones para elegir con ↑↓ y E, y una acción al cerrarse.
    /// </summary>
    public struct Mensaje
    {
        public string titulo;
        public string texto;
        public Color acento;
        public IlustracionTutorial.Tipo ilustracion;
        public string[] opciones;
        public System.Action<int> alElegir;
        public System.Action alCerrar;
    }

    Mensaje actual;
    IlustracionTutorial ilustracion;
    readonly List<Opcion> opciones = new List<Opcion>();
    int seleccion;

    readonly Queue<Mensaje> cola = new Queue<Mensaje>();

    RectTransform raiz;
    CanvasGroup grupo;
    Image degradado;
    RawImage retratoIzq;
    RawImage retratoDer;
    Text nombre;
    Image subrayado;
    RectTransform pila;
    GameObject aviso;
    Text textoAviso;
    TeclaAnimada teclaAviso;

    readonly List<Burbuja> burbujas = new List<Burbuja>();
    Retrato camaraIzq;
    Retrato camaraDer;

    bool hablaNilo;
    int burbujaActual;
    float letras;
    float relojEntrada;
    int frameApertura = -1;
    int frameCierre = -1;

    bool abierto;
    public bool Abierto { get { return abierto; } }

    /// <summary>
    /// Verdadero mientras hay diálogo y también el fotograma en que se cierra: la misma pulsación
    /// que lo cierra no debe llegar al jugador (volvería a hablar con Maia).
    /// </summary>
    public bool Bloquea { get { return Abierto || Time.frameCount == frameCierre; } }

    /// <summary>Se avisa al cerrar el último mensaje, para que UIManager devuelva el control.</summary>
    public System.Action AlCerrar;

    // ------------------------------------------------------------------ construcción

    public static DialogoUI Crear(Transform lienzo)
    {
        GameObject go = new GameObject("Dialogo");
        go.transform.SetParent(lienzo, false);
        DialogoUI d = go.AddComponent<DialogoUI>();
        d.Construir();
        return d;
    }

    void Construir()
    {
        raiz = gameObject.AddComponent<RectTransform>();
        raiz.anchorMin = Vector2.zero;
        raiz.anchorMax = Vector2.one;
        raiz.offsetMin = raiz.offsetMax = Vector2.zero;
        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        // Sombra que sube desde el borde inferior: asienta los bustos y da contraste al texto.
        degradado = Imagen(raiz, "Degradado", Formas.Degradado(), new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 420f), new Color(0f, 0.01f, 0.015f, 1f));

        retratoDer = CrearRetrato(raiz, "Retrato Nilo", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-10f, -40f), 470f);
        retratoIzq = CrearRetrato(raiz, "Retrato", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, -40f), 520f);

        GameObject p = new GameObject("Burbujas");
        p.transform.SetParent(raiz, false);
        pila = p.AddComponent<RectTransform>();
        pila.anchorMin = pila.anchorMax = pila.pivot = Vector2.zero;

        nombre = RelevoUI.Texto(pila, "Nombre", new Vector2(0f, 1f), new Vector2(0f, 0f),
            new Vector2(26f, 12f), new Vector2(500f, 40f), 30, TextAnchor.LowerLeft, RelevoUI.Verde);
        nombre.font = RelevoUI.FuenteTitulo();
        Outline o = nombre.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.6f);
        o.effectDistance = new Vector2(2f, -2f);
        subrayado = Imagen(pila, "Subrayado", null, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f),
            new Vector2(26f, 8f), new Vector2(90f, 3f), RelevoUI.Verde);

        aviso = new GameObject("Continuar");
        aviso.transform.SetParent(pila, false);
        RectTransform ra = aviso.AddComponent<RectTransform>();
        ra.anchorMin = ra.anchorMax = new Vector2(0f, 0f);
        ra.pivot = new Vector2(0f, 1f);
        ra.sizeDelta = new Vector2(260f, 46f);
        teclaAviso = TeclaAnimada.Crear(aviso.transform, "Tecla", RelevoUI.SpriteTecla("e"),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 4f), 44f, RelevoUI.AmbarSuave, true);
        teclaAviso.Reposo(1.2f);
        textoAviso = RelevoUI.Texto(aviso.transform, "Texto", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(54f, 0f), new Vector2(200f, 40f), 20, TextAnchor.MiddleLeft, RelevoUI.ConAlfa(RelevoUI.Tinta, 0.9f));

        ilustracion = IlustracionTutorial.Crear(raiz);

        camaraIzq = new Retrato("Camara retrato", retratoIzq, 25f);
        camaraDer = new Retrato("Camara retrato Nilo", retratoDer, -25f);

        gameObject.SetActive(false);
    }

    RawImage CrearRetrato(Transform padre, string n, Vector2 ancla, Vector2 pivote, Vector2 pos, float lado)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(padre, false);
        RawImage img = go.AddComponent<RawImage>();
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(lado, lado);
        return img;
    }

    static Image Imagen(Transform padre, string n, Sprite sprite, Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote,
        Vector2 pos, Vector2 tam, Color color)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(padre, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return img;
    }

    // ------------------------------------------------------------------ API

    public void Encolar(string titulo, string texto, Color acento)
    {
        Encolar(new Mensaje { titulo = titulo, texto = texto, acento = acento });
    }

    public void Encolar(Mensaje mensaje)
    {
        cola.Enqueue(mensaje);
        if (!Abierto) Siguiente();
    }

    /// <summary>Verdadero cuando el mensaje en pantalla ya terminó de escribirse y ofrece opciones.</summary>
    public bool EligiendoOpcion { get { return Abierto && opciones.Count > 0 && burbujaActual >= burbujas.Count; } }

    public void MoverSeleccion(int paso)
    {
        if (!EligiendoOpcion) return;
        seleccion = (seleccion + paso + opciones.Count) % opciones.Count;
        PintarOpciones();
        AudioDirector.Pedir(Tono.Mensaje);
    }

    /// <summary>Ejecuta la acción de cierre del mensaje actual una sola vez.</summary>
    void SoltarMensaje()
    {
        System.Action alCerrar = actual.alCerrar;
        actual.alCerrar = null;
        if (alCerrar != null) alCerrar();
    }

    /// <summary>Tecla de avanzar: completa la frase en curso, pasa a la siguiente o cierra.</summary>
    public void Avanzar()
    {
        if (!Abierto || Time.frameCount == frameApertura) return;
        if (teclaAviso != null) teclaAviso.Presionar();

        if (burbujaActual < burbujas.Count)
        {
            // Se completa todo lo que queda del mensaje de una vez.
            for (int i = burbujaActual; i < burbujas.Count; i++) burbujas[i].Mostrar(int.MaxValue);
            burbujaActual = burbujas.Count;
            return;
        }

        if (opciones.Count > 0)
        {
            // Elegir: la acción puede encolar más mensajes (por ejemplo, repetir la explicación).
            System.Action<int> alElegir = actual.alElegir;
            int elegida = seleccion;
            actual.alElegir = null;
            if (alElegir != null) alElegir(elegida);
        }

        SoltarMensaje();
        if (cola.Count > 0) Siguiente();
        else Cerrar();
    }

    public void Cerrar()
    {
        cola.Clear();
        if (!Abierto) return;
        SoltarMensaje();
        ilustracion.Ocultar();
        camaraIzq.Apagar();
        camaraDer.Apagar();
        frameCierre = Time.frameCount;
        abierto = false;
        RelevoUIAnim.Salida(gameObject, 0.18f, null);
        if (AlCerrar != null) AlCerrar();
    }

    // ------------------------------------------------------------------ mensajes

    void Siguiente()
    {
        Mensaje m = cola.Dequeue();
        actual = m;
        bool estabaAbierto = Abierto;

        string t = m.titulo != null ? m.titulo.ToLowerInvariant() : "";
        bool hablaMaia = t.Contains("maia");
        hablaNilo = t.Contains("aviso") || t.Contains("salida");

        // Retratos: Maia a la izquierda cuando habla; Nilo siempre a la derecha, atenuado si escucha.
        MaiaTerminal maia = hablaMaia ? FindFirstObjectByType<MaiaTerminal>() : null;
        bool conIzq = maia != null && camaraIzq.Encender(maia.transform, CapaRetrato, true);
        retratoIzq.gameObject.SetActive(conIzq);

        bool conDer = RelevoGame.Jugador != null && camaraDer.Encender(RelevoGame.Jugador.transform, CapaRetratoNilo, false);
        retratoDer.gameObject.SetActive(conDer);
        retratoDer.color = hablaNilo ? Color.white : new Color(0.55f, 0.58f, 0.6f, 1f);

        // Burbujas: una por frase.
        foreach (Burbuja b in burbujas) Destroy(b.raiz.gameObject);
        burbujas.Clear();

        List<string> frases = Frases(m.texto);
        float izquierda = conIzq ? 470f : (hablaNilo ? 0f : 170f);
        float y = 0f;
        float anchoMax = 0f;
        for (int i = 0; i < frases.Count; i++)
        {
            Burbuja b = new Burbuja(pila, frases[i], hablaNilo, m.acento);
            b.raiz.anchoredPosition = new Vector2(0f, -y);
            y += b.raiz.sizeDelta.y + 10f;
            anchoMax = Mathf.Max(anchoMax, b.raiz.sizeDelta.x);
            burbujas.Add(b);
        }

        // La pila se apoya abajo; si Nilo habla, se alinea a su izquierda.
        pila.anchorMin = pila.anchorMax = new Vector2(hablaNilo ? 1f : 0f, 0f);
        pila.pivot = new Vector2(hablaNilo ? 1f : 0f, 0f);
        pila.sizeDelta = new Vector2(anchoMax, y);
        // Con opciones, la pila sube para dejar sitio a las filas debajo.
        int numOpciones = m.opciones != null ? m.opciones.Length : 0;
        float alzado = numOpciones * 54f;
        pila.anchoredPosition = new Vector2(hablaNilo ? -430f : izquierda, 70f + alzado);
        foreach (Burbuja b in burbujas)
        {
            RectTransform r = b.raiz;
            r.anchorMin = r.anchorMax = new Vector2(hablaNilo ? 1f : 0f, 1f);
            r.pivot = new Vector2(hablaNilo ? 1f : 0f, 1f);
        }

        nombre.text = m.titulo;
        nombre.color = m.acento;
        nombre.alignment = hablaNilo ? TextAnchor.LowerRight : TextAnchor.LowerLeft;
        Reanclar(nombre.rectTransform, hablaNilo, new Vector2(26f, 12f));
        subrayado.color = m.acento;
        subrayado.rectTransform.sizeDelta = new Vector2(Mathf.Clamp(nombre.preferredWidth, 40f, 400f), 3f);
        Reanclar(subrayado.rectTransform, hablaNilo, new Vector2(26f, 8f));

        RectTransform ra = (RectTransform)aviso.transform;
        ra.anchorMin = ra.anchorMax = new Vector2(hablaNilo ? 1f : 0f, 0f);
        ra.pivot = new Vector2(hablaNilo ? 1f : 0f, 1f);
        ra.anchoredPosition = new Vector2(hablaNilo ? -10f : 30f, -10f);
        textoAviso.text = cola.Count > 0 ? "Continuar" : "Cerrar";
        teclaAviso.Tenir(m.acento);
        aviso.SetActive(false);

        // Opciones: filas debajo de las burbujas; aparecen cuando el texto termina.
        foreach (Opcion o in opciones) Destroy(o.raiz.gameObject);
        opciones.Clear();
        seleccion = 0;
        for (int i = 0; i < numOpciones; i++)
        {
            Opcion o = new Opcion(pila, m.opciones[i], m.acento);
            o.raiz.anchoredPosition = new Vector2(24f, -16f - i * 54f);
            o.raiz.gameObject.SetActive(false);
            opciones.Add(o);
        }
        PintarOpciones();

        // Tarjeta ilustrada encima de las burbujas. Se coloca antes de mostrarla: la animación de
        // entrada toma como reposo la posición que tenga en ese momento.
        RectTransform ri = (RectTransform)ilustracion.transform;
        ri.anchorMin = ri.anchorMax = ri.pivot = Vector2.zero;
        float xTarjeta = hablaNilo ? 1920f * 0.5f - 270f : Mathf.Max(izquierda, 470f);
        ri.anchoredPosition = new Vector2(xTarjeta, 70f + alzado + y + 70f);
        ilustracion.Mostrar(m.ilustracion, m.acento);

        burbujaActual = 0;
        letras = 0f;
        relojEntrada = 0f;
        frameApertura = Time.frameCount;

        abierto = true;
        if (!estabaAbierto)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            RelevoUIAnim.Entrada(gameObject, 0.22f, 0f, true);
        }
    }

    static void Reanclar(RectTransform rt, bool derecha, Vector2 pos)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(derecha ? 1f : 0f, 1f);
        rt.pivot = new Vector2(derecha ? 1f : 0f, 0f);
        rt.anchoredPosition = new Vector2(derecha ? -pos.x : pos.x, pos.y);
    }

    /// <summary>Corta el texto en frases (tras . ! ? …) y junta las muy cortas con la anterior.</summary>
    static List<string> Frases(string texto)
    {
        List<string> frases = new List<string>();
        if (string.IsNullOrEmpty(texto)) { frases.Add(""); return frases; }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < texto.Length; i++)
        {
            char c = texto[i];
            sb.Append(c);
            bool fin = (c == '.' || c == '!' || c == '?' || c == '…') && (i + 1 >= texto.Length || texto[i + 1] == ' ' || texto[i + 1] == '\n');
            if (fin || c == '\n')
            {
                string f = sb.ToString().Trim();
                if (f.Length > 0)
                {
                    if (frases.Count > 0 && f.Length < 18) frases[frases.Count - 1] += " " + f;
                    else frases.Add(f);
                }
                sb.Length = 0;
            }
        }
        string resto = sb.ToString().Trim();
        if (resto.Length > 0) frases.Add(resto);
        if (frases.Count == 0) frases.Add(texto);

        // Más de cuatro burbujas tapan el nivel: lo que sobre va en la última.
        while (frases.Count > 4)
        {
            frases[frases.Count - 2] += " " + frases[frases.Count - 1];
            frases.RemoveAt(frases.Count - 1);
        }
        return frases;
    }

    // ------------------------------------------------------------------ animación

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        relojEntrada += dt;

        // Bustos: entran deslizándose y "respiran" mientras se escribe su frase.
        bool escribiendo = burbujaActual < burbujas.Count;
        float entrada = Suave(relojEntrada / 0.35f);
        float respira = escribiendo ? Mathf.Sin(Time.unscaledTime * 9f) * 0.008f : 0f;
        if (retratoIzq.gameObject.activeSelf)
        {
            retratoIzq.rectTransform.anchoredPosition = new Vector2(10f - 140f * (1f - entrada), -40f);
            retratoIzq.rectTransform.localScale = Vector3.one * (1f + (hablaNilo ? 0f : respira));
        }
        if (retratoDer.gameObject.activeSelf)
        {
            retratoDer.rectTransform.anchoredPosition = new Vector2(-10f + 140f * (1f - entrada), -40f);
            retratoDer.rectTransform.localScale = Vector3.one * (1f + (hablaNilo ? respira : 0f));
        }

        camaraIzq.Seguir();
        camaraDer.Seguir();

        // Máquina de escribir, burbuja a burbuja.
        if (escribiendo && relojEntrada > 0.15f)
        {
            Burbuja b = burbujas[burbujaActual];
            letras += dt * LetrasPorSegundo;
            if (b.Mostrar((int)letras))
            {
                burbujaActual++;
                letras = -6f; // pausa breve entre frases
            }
        }

        foreach (Burbuja b in burbujas) b.Animar(dt);

        bool listo = burbujaActual >= burbujas.Count;
        // La cola puede crecer con el mensaje ya en pantalla (Maia encola tras la intro).
        string etiqueta = cola.Count > 0 ? "Continuar" : "Cerrar";
        if (textoAviso.text != etiqueta) textoAviso.text = etiqueta;
        if (listo && opciones.Count == 0 && !aviso.activeSelf) RelevoUIAnim.Entrada(aviso, 0.16f, 8f, true);
        if (listo)
            for (int i = 0; i < opciones.Count; i++)
                if (!opciones[i].raiz.gameObject.activeSelf)
                {
                    opciones[i].raiz.gameObject.SetActive(true);
                    RelevoUIAnim.Entrada(opciones[i].raiz.gameObject, 0.16f + i * 0.05f, 10f, true);
                }
        for (int i = 0; i < opciones.Count; i++) opciones[i].Animar(i == seleccion, dt);
    }

    void PintarOpciones()
    {
        for (int i = 0; i < opciones.Count; i++) opciones[i].Seleccionar(i == seleccion);
    }

    // ------------------------------------------------------------------ opción

    sealed class Opcion
    {
        public readonly RectTransform raiz;
        readonly Image relleno, contorno, marca;
        readonly Text texto;
        readonly Color acento;
        float brillo;

        public Opcion(Transform padre, string etiqueta, Color acento)
        {
            this.acento = acento;
            GameObject go = new GameObject("Opcion");
            go.transform.SetParent(padre, false);
            raiz = go.AddComponent<RectTransform>();
            raiz.anchorMin = raiz.anchorMax = new Vector2(0f, 0f);
            raiz.pivot = new Vector2(0f, 1f);
            raiz.sizeDelta = new Vector2(420f, 46f);

            relleno = Imagen(raiz, "Relleno", Formas.BurbujaRelleno(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, new Color(0.03f, 0.075f, 0.085f, 0.9f));
            relleno.type = Image.Type.Sliced;
            contorno = Imagen(raiz, "Contorno", Formas.BurbujaContorno(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, acento);
            contorno.type = Image.Type.Sliced;
            marca = Imagen(raiz, "Marca", null, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(36f, 0f), new Vector2(10f, 10f), acento);
            marca.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            texto = RelevoUI.Texto(raiz, "Texto", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(54f, 0f),
                new Vector2(350f, 40f), 21, TextAnchor.MiddleLeft, new Color(0.96f, 0.93f, 0.86f, 1f));
            texto.font = RelevoUI.Fuente();
            texto.text = etiqueta;
        }

        public void Seleccionar(bool si)
        {
            contorno.color = si ? acento : RelevoUI.ConAlfa(acento, 0.3f);
            marca.enabled = si;
            texto.color = si ? new Color(1f, 0.98f, 0.92f, 1f) : new Color(0.75f, 0.75f, 0.72f, 0.85f);
            relleno.color = si ? new Color(acento.r * 0.22f, acento.g * 0.22f, acento.b * 0.22f, 0.94f) : new Color(0.03f, 0.075f, 0.085f, 0.85f);
        }

        public void Animar(bool si, float dt)
        {
            brillo = Mathf.MoveTowards(brillo, si ? 1f : 0f, dt * 8f);
            raiz.localScale = Vector3.one * (1f + 0.035f * brillo);
            if (si) marca.rectTransform.anchoredPosition = new Vector2(36f + Mathf.Sin(Time.unscaledTime * 6f) * 3f, 0f);
        }
    }

    static float Suave(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - (1f - x) * (1f - x) * (1f - x);
    }

    void OnDestroy()
    {
        if (camaraIzq != null) camaraIzq.Liberar();
        if (camaraDer != null) camaraDer.Liberar();
    }

    // ------------------------------------------------------------------ burbuja

    sealed class Burbuja
    {
        public readonly RectTransform raiz;
        readonly Text texto;
        readonly string completo;
        float reloj = -1f;
        int mostradas;

        public Burbuja(Transform padre, string frase, bool haciaDerecha, Color acento)
        {
            completo = frase;
            GameObject go = new GameObject("Burbuja");
            go.transform.SetParent(padre, false);
            raiz = go.AddComponent<RectTransform>();

            // Relleno y contorno son dos sprites de 9-slice; si habla Nilo se reflejan para que el
            // pico apunte a la derecha. El texto va en un hijo aparte y nunca se refleja.
            Image relleno = Imagen(raiz, "Relleno", Formas.BurbujaRelleno(), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.03f, 0.075f, 0.085f, 0.88f));
            relleno.type = Image.Type.Sliced;
            Image contorno = Imagen(raiz, "Contorno", Formas.BurbujaContorno(), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, RelevoUI.ConAlfa(RelevoUI.Aclarar(acento, 0.35f), 0.75f));
            contorno.type = Image.Type.Sliced;
            if (haciaDerecha)
            {
                relleno.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                contorno.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }

            float margenPico = 24f, margen = 20f;
            texto = RelevoUI.Texto(raiz, "Texto", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, 24,
                TextAnchor.MiddleLeft, new Color(0.96f, 0.93f, 0.86f, 1f));
            texto.font = RelevoUI.Fuente();
            texto.supportRichText = true;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.text = frase;
            Shadow sombra = texto.gameObject.AddComponent<Shadow>();
            sombra.effectColor = new Color(0f, 0f, 0f, 0.7f);
            sombra.effectDistance = new Vector2(1.5f, -1.5f);

            float anchoTexto = Mathf.Min(texto.preferredWidth + 2f, AnchoMaximoBurbuja - margenPico - margen);
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            RectTransform rt = texto.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(anchoTexto, 10f);
            float alto = Mathf.Max(30f, texto.preferredHeight);
            rt.sizeDelta = new Vector2(anchoTexto, alto);
            rt.anchoredPosition = new Vector2(haciaDerecha ? margen : margenPico, 0f);

            raiz.sizeDelta = new Vector2(anchoTexto + margenPico + margen, alto + 26f);
            texto.text = "";
            go.SetActive(false);
        }

        /// <summary>Muestra las primeras 'n' letras; devuelve true cuando ya está completa.</summary>
        public bool Mostrar(int n)
        {
            if (!raiz.gameObject.activeSelf)
            {
                raiz.gameObject.SetActive(true);
                reloj = 0f;
            }
            n = Mathf.Clamp(n, 0, completo.Length);
            if (n != mostradas || texto.text.Length == 0)
            {
                mostradas = n;
                // El resto va transparente: así el texto no salta de línea mientras se escribe.
                texto.text = n >= completo.Length ? completo
                    : completo.Substring(0, n) + "<color=#00000000>" + completo.Substring(n) + "</color>";
            }
            return n >= completo.Length;
        }

        public void Animar(float dt)
        {
            if (reloj < 0f) return;
            reloj += dt;
            float t = Mathf.Clamp01(reloj / 0.22f);
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.06f;
            raiz.localScale = new Vector3(Mathf.Lerp(0.85f, 1f, t) * s, Mathf.Lerp(0.6f, 1f, t) * s, 1f);
            if (t >= 1f) { raiz.localScale = Vector3.one; reloj = -1f; }
        }
    }

    // ------------------------------------------------------------------ retrato en vivo

    /// <summary>
    /// Cámara que encuadra el busto de un personaje desde delante, girada unos grados para que mire
    /// hacia las burbujas, y lo dibuja con fondo transparente.
    /// </summary>
    sealed class Retrato
    {
        readonly RawImage destino;
        readonly float giro;
        readonly Camera camara;
        RenderTexture textura;
        Transform objetivo;
        Transform ojo, otroOjo;   // si existe, la cara es hacia donde están los ojos (Maia mira al sur, no a su +Z)
        readonly List<KeyValuePair<GameObject, int>> capas = new List<KeyValuePair<GameObject, int>>();

        public Retrato(string nombre, RawImage destino, float giro)
        {
            this.destino = destino;
            this.giro = giro;
            GameObject go = new GameObject(nombre);
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.DontSave;
            camara = go.AddComponent<Camera>();
            camara.enabled = false;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camara.fieldOfView = 26f;
            camara.nearClipPlane = 0.05f;
            camara.farClipPlane = 30f;
            camara.allowHDR = false;
            camara.allowMSAA = false;
            var datos = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            // Se dibuja antes que la cámara principal y sin pedir texturas de profundidad ni de
            // color: así no deja estado global (sombras, profundidad) que la principal herede.
            camara.depth = -50f;
            datos.renderPostProcessing = false;
            datos.renderShadows = true;
            datos.requiresDepthOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;
            datos.requiresColorOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;
            datos.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;
        }

        public bool Encender(Transform personaje, int capa, bool porLosOjos)
        {
            Apagar();
            if (personaje == null) return false;
            objetivo = personaje;
            ojo = otroOjo = null;
            if (porLosOjos)
                foreach (Transform t in personaje.GetComponentsInChildren<Transform>())
                {
                    if (t.name == "Ojo izquierdo") ojo = t;
                    else if (t.name == "Ojo derecho") otroOjo = t;
                }
            if (otroOjo == null) ojo = null;

            if (textura == null)
            {
                textura = new RenderTexture(640, 640, 24, RenderTextureFormat.ARGB32);
                textura.antiAliasing = 4;
                textura.name = camara.name;
            }
            camara.cullingMask = 1 << capa;
            camara.targetTexture = textura;
            destino.texture = textura;

            foreach (Transform t in personaje.GetComponentsInChildren<Transform>(true))
            {
                capas.Add(new KeyValuePair<GameObject, int>(t.gameObject, t.gameObject.layer));
                t.gameObject.layer = capa;
            }

            // La cámara principal tiene que seguir viendo la capa del retrato.
            Camera principal = Camera.main;
            if (principal != null) principal.cullingMask |= 1 << capa;

            camara.enabled = true;
            Seguir();
            return true;
        }

        public void Seguir()
        {
            if (objetivo == null || !camara.enabled) return;

            Bounds b = new Bounds(objetivo.position, Vector3.zero);
            bool hay = false;
            foreach (Renderer r in objetivo.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r is ParticleSystemRenderer) continue;
                if (hay) b.Encapsulate(r.bounds); else { b = r.bounds; hay = true; }
            }

            float alto = Mathf.Max(0.3f, b.max.y - objetivo.position.y);
            Vector3 cara = new Vector3(objetivo.position.x, objetivo.position.y + alto * 0.7f, objetivo.position.z);
            Vector3 haciaDelante = objetivo.forward;
            if (ojo != null)
            {
                Vector3 ojos = (ojo.position + otroOjo.position) * 0.5f;
                // Desde el pie del personaje: el centro de los límites se desplaza con las plantas.
                haciaDelante = ojos - new Vector3(objetivo.position.x, ojos.y, objetivo.position.z);
                cara = new Vector3(objetivo.position.x, ojos.y - 0.1f, objetivo.position.z);
            }
            Vector3 frente = Quaternion.Euler(0f, giro, 0f) * Plano(haciaDelante);
            float distancia = (alto * 0.46f) / Mathf.Tan(camara.fieldOfView * 0.5f * Mathf.Deg2Rad);
            camara.transform.position = cara + frente * distancia + Vector3.up * alto * 0.08f;
            camara.transform.LookAt(cara);
        }

        static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }

        public void Apagar()
        {
            camara.enabled = false;
            foreach (var par in capas) if (par.Key != null) par.Key.layer = par.Value;
            capas.Clear();
            objetivo = null;
        }

        public void Liberar()
        {
            Apagar();
            if (textura != null) { textura.Release(); Object.Destroy(textura); }
            if (camara != null) Object.Destroy(camara.gameObject);
        }
    }

    // ------------------------------------------------------------------ formas

    static class Formas
    {
        static Sprite relleno, contorno, degradado;
        const int An = 72, Al = 64;

        /// <summary>
        /// Burbuja con esquinas achaflanadas y un pico a la izquierda, cerca de la esquina superior
        /// (zona de 9-slice que no se estira, para que el pico no se deforme).
        /// </summary>
        static bool Dentro(float x, float y)
        {
            const float izq = 12f, chaflan = 9f;
            // Caja achaflanada.
            if (x >= izq && x <= An && y >= 0f && y <= Al)
            {
                float dx = x - izq, dy = Al - y;      // esquina superior izquierda
                float ex = x - izq, ey = y;           // esquina inferior izquierda
                float fx = An - x, fy = Al - y;       // superior derecha
                float gx = An - x, gy = y;            // inferior derecha
                if (dx + dy < chaflan || ex + ey < chaflan || fx + fy < chaflan || gx + gy < chaflan) return false;
                return true;
            }
            // Pico: triángulo con la punta en x=0 a 22 px del borde superior.
            float punta = Al - 22f;
            if (x >= 0f && x < izq + 1f)
            {
                float medio = (x / izq) * 9f;
                return Mathf.Abs(y - punta) <= medio;
            }
            return false;
        }

        static float Cobertura(int px, int py, float erosion)
        {
            // Supermuestreo 4x4 para bordes suaves.
            int dentro = 0;
            for (int sy = 0; sy < 4; sy++)
                for (int sx = 0; sx < 4; sx++)
                {
                    float x = px + (sx + 0.5f) / 4f, y = py + (sy + 0.5f) / 4f;
                    bool d = Dentro(x, y);
                    if (d && erosion > 0f)
                    {
                        for (int k = 0; k < 8 && d; k++)
                        {
                            float a = k * Mathf.PI * 0.25f;
                            if (!Dentro(x + Mathf.Cos(a) * erosion, y + Mathf.Sin(a) * erosion)) d = false;
                        }
                    }
                    if (d) dentro++;
                }
            return dentro / 16f;
        }

        static Sprite Hacer(System.Func<int, int, float> alfa)
        {
            Texture2D t = new Texture2D(An, Al, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.DontSave;
            t.wrapMode = TextureWrapMode.Clamp;
            Color32[] px = new Color32[An * Al];
            for (int y = 0; y < Al; y++)
                for (int x = 0; x < An; x++)
                    px[y * An + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alfa(x, y)) * 255f));
            t.SetPixels32(px);
            t.Apply();
            // Bordes: izquierda 26 (pico y chaflán), arriba 30 (pico), derecha y abajo 14.
            Sprite s = Sprite.Create(t, new Rect(0, 0, An, Al), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(26f, 14f, 14f, 32f));
            s.hideFlags = HideFlags.DontSave;
            return s;
        }

        public static Sprite BurbujaRelleno()
        {
            if (relleno == null) relleno = Hacer((x, y) => Cobertura(x, y, 0f));
            return relleno;
        }

        public static Sprite BurbujaContorno()
        {
            if (contorno == null) contorno = Hacer((x, y) => Cobertura(x, y, 0f) - Cobertura(x, y, 2f));
            return contorno;
        }

        public static Sprite Degradado()
        {
            if (degradado != null) return degradado;
            Texture2D t = new Texture2D(4, 64, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.DontSave;
            t.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 64; y++)
            {
                float a = 1f - y / 63f;
                a = Mathf.Pow(a, 1.6f) * 0.9f;
                for (int x = 0; x < 4; x++) t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            t.Apply();
            degradado = Sprite.Create(t, new Rect(0, 0, 4, 64), new Vector2(0.5f, 0.5f), 100f);
            degradado.hideFlags = HideFlags.DontSave;
            return degradado;
        }
    }
}
