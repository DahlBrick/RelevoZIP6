using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Jefe final (nivel 13, la Sala del Relevo): el Eco del Apagón. Es la grabación de la ruta que hizo
/// Nilo la noche en que apagó Liria, convertida en un Nilo gigante de tinta que la repite en bucle.
///
/// - Pulso del apagón: cada pocos segundos repliega los puentes eléctricos durante dos segundos. Un
///   condensador con carga aguanta el pulso (pierde un tercio). Quien esté sobre un puente cae.
/// - Sombra: por donde pasa el Eco la reserva de Nilo se gasta el doble durante unos segundos.
/// - Señal de Maia: es el reloj. Baja sola, baja de golpe si el pulso alcanza a Nilo y si se
///   apaga hay que empezar de cero.
/// Cada torre encendida (palanca L, palanca K) acelera al Eco y acorta el intervalo entre pulsos.
/// Se vence llevando la batería al núcleo (la salida E). Se monta en ejecución, no se hornea.
/// </summary>
public sealed class JefeEcoApagon : MonoBehaviour
{
    public static JefeEcoApagon Activo { get; private set; }

    const float SenalTotal = 225f;
    const float CastigoPulso = 15f;
    const float AvisoPulso = 1.5f;
    const float DuracionPulso = 2f;
    const float VidaSombra = 4f;
    static readonly float[] Velocidad = { 2.2f, 3f, 3.8f };
    static readonly float[] Intervalo = { 12f, 9f, 6f };

    // Ruta de aquella noche, en casillas del mapa del nivel 13. El Eco flota también sobre el agua.
    static readonly Vector2Int[] Ruta =
    {
        new Vector2Int(3, 7), new Vector2Int(3, 2), new Vector2Int(9, 3), new Vector2Int(11, 7),
        new Vector2Int(17, 7), new Vector2Int(18, 10), new Vector2Int(11, 10), new Vector2Int(5, 7)
    };

    LevelLayout layout;
    Transform eco, aro;
    Material materialAro;
    int destino = 1;
    float senal = SenalTotal;
    float relojPulso, relojSombra, relojFrase;
    bool avisando, perdido, vencido;
    int faseAnterior = -1;

    readonly List<TemporaryBridge> caidos = new List<TemporaryBridge>();
    float restaurar;
    readonly Dictionary<Vector2Int, float> sombra = new Dictionary<Vector2Int, float>();
    readonly List<Transform> manchas = new List<Transform>();
    int siguienteMancha;
    readonly Renderer[] torres = new Renderer[3];
    readonly Light[] lucesTorre = new Light[3];

    Image barra;
    Text frase;
    string fraseActual = "";

    public static void Montar(LevelLayout mapa, Transform raiz)
    {
        var go = new GameObject("Eco del Apagón");
        go.transform.SetParent(raiz, false);
        go.AddComponent<JefeEcoApagon>().Construir(mapa);
    }

    void Construir(LevelLayout mapa)
    {
        Activo = this;
        layout = mapa;
        relojPulso = Intervalo[0];
        CrearEco();
        CrearSombra();
        CrearTorres();
        CrearPanel();
        ReservaEnergia.MultiplicadorGasto = GastoEnSombra;
        Decir("Nilo… esa figura eres tú. Es tu ruta de aquella noche. Enciende las torres antes de que me apague.");
    }

    void OnDestroy()
    {
        if (Activo == this) Activo = null;
        if (ReservaEnergia.MultiplicadorGasto == GastoEnSombra) ReservaEnergia.MultiplicadorGasto = null;
    }

    // ------------------------------------------------------------------ estado

    public int Fase
    {
        get
        {
            var nivel = RelevoGame.Nivel;
            if (nivel == null) return 0;
            int f = 0;
            foreach (var p in nivel.Palancas)
                if (p != null && p.Accionada && (p.Identificador == "L" || p.Identificador == "K")) f++;
            return Mathf.Min(f, 2);
        }
    }

    public float Senal01 { get { return senal / SenalTotal; } }
    bool PulsoEnCurso { get { return caidos.Count > 0 || avisando; } }

    void Update()
    {
        if (vencido) return;
        if (RelevoGame.Nivel != null && RelevoGame.Nivel.Completado) { Vencer(); return; }
        bool bloqueado = RelevoGame.UI != null && RelevoGame.UI.BloqueaJuego;
        if (bloqueado || perdido || CaidaAlAgua.Activa) { PintarPanel(); return; }

        float dt = Time.deltaTime;
        int fase = Fase;
        if (fase != faseAnterior) CambioDeFase(fase);

        Mover(fase, dt);
        Pulsos(fase, dt);
        Sombras(dt);

        PlayerController nilo = RelevoGame.Jugador;
        float gasto = 1f;
        if (nilo != null && Plano(nilo.transform.position - eco.position).magnitude < 2.4f) gasto += 5f;   // tocar al Eco
        senal = Mathf.Max(0f, senal - gasto * dt);
        if (senal <= 0f) Perder();

        relojFrase -= dt;
        if (relojFrase <= 0f && senal < SenalTotal * 0.3f && !fraseActual.StartsWith("No pares"))
            Decir("No pares, Nilo. Apagarse también es…");
        PintarPanel();
    }

    void CambioDeFase(int fase)
    {
        if (faseAnterior >= 0)
        {
            AudioDirector.Pedir(Tono.Faro);
            relojPulso = Mathf.Min(relojPulso, Intervalo[fase]);
            if (fase == 1) Decir("La Torre de Cobre vuelve a respirar. Te oigo… un poco más lejos.");
            if (fase == 2) Decir("La Torre de Sal está encendida. Lleva tu corazón al núcleo, Nilo.");
        }
        for (int i = 0; i < torres.Length; i++) Encender(i, i < fase);
        faseAnterior = fase;
    }

    // ------------------------------------------------------------------ Eco

    void Mover(int fase, float dt)
    {
        Vector3 meta = Centro(Ruta[destino]);
        Vector3 d = Plano(meta - eco.position);
        float paso = Velocidad[fase] * dt;
        if (d.magnitude <= paso) { eco.position = meta; destino = (destino + 1) % Ruta.Length; }
        else eco.position += d.normalized * paso;
        if (d.sqrMagnitude > 0.01f)
            eco.rotation = Quaternion.Slerp(eco.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-4f * dt));
        float latido = avisando ? 1f + 0.5f * Mathf.PingPong(Time.time * 6f, 1f) : 1f;
        aro.localScale = Vector3.one * latido;
    }

    void Pulsos(int fase, float dt)
    {
        if (caidos.Count > 0)
        {
            restaurar -= dt;
            if (restaurar <= 0f) Restaurar();
            return;
        }
        relojPulso -= dt;
        if (!avisando && relojPulso <= AvisoPulso)
        {
            avisando = true;
            AudioDirector.Pedir(Tono.Alerta);
        }
        if (relojPulso > 0f) return;

        avisando = false;
        relojPulso = Intervalo[fase];
        AudioDirector.Pedir(Tono.Apagado);

        foreach (TemporaryBridge p in FindObjectsByType<TemporaryBridge>(FindObjectsSortMode.None))
        {
            if (!p.Desplegado) continue;
            EnergySocket b = BaseDe(p);
            Condensador c = b != null ? b.Condensador : null;
            if (c != null && c.Carga >= 0.3f) { c.Drenar(0.3f); continue; }
            caidos.Add(p);
            p.Activar(false);
        }
        if (caidos.Count > 0) restaurar = DuracionPulso;

        PlayerController nilo = RelevoGame.Jugador;
        if (nilo != null && Plano(nilo.transform.position - eco.position).magnitude < 5f)
        {
            senal = Mathf.Max(0f, senal - CastigoPulso);
            Decir("¡Ah…! El pulso me atraviesa. Aléjate de él cuando brille.");
        }
    }

    void Restaurar()
    {
        foreach (TemporaryBridge p in caidos)
        {
            if (p == null) continue;
            EnergySocket b = BaseDe(p);
            bool sostener;
            if (b != null) sostener = b.TieneBateria || (b.Condensador != null && b.Condensador.Sosteniendo);
            else sostener = SostenidoPorPlaca(p);
            if (sostener) p.Activar(true);
        }
        caidos.Clear();
    }

    static EnergySocket BaseDe(TemporaryBridge p)
    {
        foreach (EnergySocket b in FindObjectsByType<EnergySocket>(FindObjectsSortMode.None))
            if (b.Puente == p) return b;
        return null;
    }

    static bool SostenidoPorPlaca(TemporaryBridge p)
    {
        foreach (PlacaPresion placa in PlacaPresion.Todas)
            if (placa != null && placa.Puente == p && placa.PorCaja) return true;
        return false;
    }

    // ------------------------------------------------------------------ sombra

    void Sombras(float dt)
    {
        relojSombra -= dt;
        if (relojSombra <= 0f)
        {
            relojSombra = 0.3f;
            Vector2Int c = layout.Casilla(eco.position);
            sombra[c] = Time.time + VidaSombra;
            Transform m = manchas[siguienteMancha];
            siguienteMancha = (siguienteMancha + 1) % manchas.Count;
            Vector3 pos = layout.CentroTile(c.x, c.y);
            m.position = new Vector3(pos.x, 0.06f, pos.z);
            m.gameObject.SetActive(true);
            m.localScale = Vector3.one;
        }
        for (int i = 0; i < manchas.Count; i++)
        {
            Transform m = manchas[i];
            if (!m.gameObject.activeSelf) continue;
            float s = m.localScale.x - dt / VidaSombra;
            if (s <= 0.05f) m.gameObject.SetActive(false);
            else m.localScale = new Vector3(s, 1f, s);
        }
    }

    float GastoEnSombra(Vector3 pos)
    {
        float hasta;
        return sombra.TryGetValue(layout.Casilla(pos), out hasta) && hasta > Time.time ? 2f : 1f;
    }

    // ------------------------------------------------------------------ fin

    void Perder()
    {
        if (perdido) return;
        perdido = true;
        AudioDirector.Pedir(Tono.Apagado);
        DialogoUI dialogo = RelevoGame.UI != null ? RelevoGame.UI.Dialogo : null;
        System.Action reiniciar = delegate { if (RelevoGame.Nivel != null) RelevoGame.Nivel.ReiniciarNivel(); };
        if (dialogo == null) { reiniciar(); return; }
        dialogo.Encolar(new DialogoUI.Mensaje
        {
            titulo = "…",
            texto = "La radio solo deja ruido. El Eco sigue su ruta, como aquella noche. Todo vuelve a empezar.",
            acento = RelevoUI.Cobre,
            ilustracion = IlustracionTutorial.Tipo.Apagado,
            alCerrar = reiniciar
        });
    }

    void Vencer()
    {
        vencido = true;
        for (int i = 0; i < torres.Length; i++) Encender(i, true);
        foreach (TemporaryBridge p in caidos) if (p != null && SostenidoPorPlaca(p)) p.Activar(true);
        caidos.Clear();
        eco.gameObject.SetActive(false);
        foreach (Transform m in manchas) m.gameObject.SetActive(false);
        ReservaEnergia.MultiplicadorGasto = null;
        Decir("");
        if (barra != null) barra.transform.parent.parent.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ objetivo

    public string Objetivo
    {
        get
        {
            if (PulsoEnCurso) return "¡Pulso del apagón! Sal de los puentes.";
            BatteryController bat = RelevoGame.Bateria;
            if (bat == null) return "";
            string b = bat.BaseActual != null ? bat.BaseActual.Identificador : "";
            switch (Fase)
            {
                case 0:
                    if (!CompuertaAbierta()) return "Empuja la caja a la placa: abre el paso a la Torre de Cobre.";
                    if (b != "A") return bat.EnManos ? "Coloca la batería en la base A y carga su condensador." : "Recupera la batería.";
                    if (bat.BaseActual.Condensador != null && bat.BaseActual.Condensador.Carga < 0.6f) return "Carga el condensador: aguantará el pulso.";
                    return "Cruza a la Torre de Cobre y baja su palanca.";
                case 1:
                    if (b == "A") return "Recupera la batería y cruza la pasarela al centro.";
                    if (b != "D") return bat.EnManos ? "Coloca la batería en la base D." : "Recupera la batería.";
                    return "Cruza entre dos pulsos y baja la palanca de la Torre de Sal. Evita la sombra.";
                default:
                    if (b != "") return "Recupera la batería de la base " + b + ".";
                    if (!PuenteDeNucleo()) return "Empuja el cajón al rombo: tenderá el puente al núcleo.";
                    return "Lleva la batería al núcleo entre dos pulsos.";
            }
        }
    }

    static bool CompuertaAbierta()
    {
        foreach (PlacaPresion p in PlacaPresion.Todas)
            if (p != null && p.Compuerta != null && !p.PorCaja) return false;
        return true;
    }

    static bool PuenteDeNucleo()
    {
        foreach (PlacaPresion p in PlacaPresion.Todas)
            if (p != null && p.Puente != null && p.PorCaja) return true;
        return false;
    }

    // ------------------------------------------------------------------ construcción

    void CrearEco()
    {
        eco = DioramaBuilder.CrearAncla("Eco gigante", transform, Centro(Ruta[0]));
        var kit = KitRelevo.Activo;
        Material tinta = RelevoPalette.MaterialDe(ClaveMaterial.Fondo);
        Material brasa = RelevoPalette.MaterialDe(ClaveMaterial.Coral);
        if (kit != null && kit.niloModelo != null)
        {
            var modelo = KitRelevo.Colocar(kit.niloModelo, eco, Vector3.zero, 0, kit.niloEscala * 3.2f, false);
            foreach (var r in modelo.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++) mats[m] = m % 3 == 2 ? brasa : tinta;
                r.sharedMaterials = mats;
            }
            foreach (var c in modelo.GetComponentsInChildren<Collider>(true)) Destroy(c);
        }
        else
        {
            var cuerpo = DioramaBuilder.CrearCilindro("Cuerpo de tinta", eco, Vector3.up * 2.4f, 1.3f, 4.8f, tinta);
            foreach (var c in cuerpo.GetComponentsInChildren<Collider>(true)) Destroy(c);
        }
        materialAro = brasa;
        var anillo = DioramaBuilder.CrearAro("Pulso", eco, Vector3.up * 0.15f, 2.4f, brasa);
        aro = anillo.transform;
        var luz = new GameObject("Brasa").AddComponent<Light>();
        luz.transform.SetParent(eco, false);
        luz.transform.localPosition = Vector3.up * 3f;
        luz.type = LightType.Point;
        luz.color = RelevoPalette.ColorDe(ClaveMaterial.Coral);
        luz.range = 9f;
        luz.intensity = 2.2f;
    }

    void CrearSombra()
    {
        Material tinta = RelevoPalette.MaterialDe(ClaveMaterial.Fondo);
        for (int i = 0; i < 18; i++)
        {
            var m = DioramaBuilder.CrearCilindro("Sombra del Eco", transform, Vector3.zero, 0.95f, 0.04f, tinta);
            foreach (var c in m.GetComponentsInChildren<Collider>(true)) Destroy(c);
            m.SetActive(false);
            manchas.Add(m.transform);
        }
    }

    void CrearTorres()
    {
        // Torre de Cobre en el islote de L, Torre de Sal junto a K y la del núcleo junto a E.
        Torre(0, layout.PrimerMarcador('L', Vector3.zero) + new Vector3(-TamanoTile(), 0f, 0f), "Torre de Cobre");
        Torre(1, layout.PrimerMarcador('K', Vector3.zero) + new Vector3(-TamanoTile() * 2f, 0f, 0f), "Torre de Sal");
        Torre(2, layout.PrimerMarcador('E', Vector3.zero) + new Vector3(0f, 0f, TamanoTile()), "Núcleo de Liria");
    }

    float TamanoTile() { return layout.TamanoTile; }

    void Torre(int i, Vector3 pos, string nombre)
    {
        var ancla = DioramaBuilder.CrearAncla(nombre, transform, pos);
        var poste = DioramaBuilder.CrearCilindro("Poste", ancla, Vector3.up * 1.6f, 0.22f, 3.2f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
        var lampara = DioramaBuilder.CrearEsfera("Lámpara", ancla, Vector3.up * 3.45f, 0.75f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
        foreach (var c in ancla.GetComponentsInChildren<Collider>(true)) Destroy(c);
        torres[i] = lampara.GetComponent<Renderer>();
        var luz = new GameObject("Luz de la torre").AddComponent<Light>();
        luz.transform.SetParent(ancla, false);
        luz.transform.localPosition = Vector3.up * 3.5f;
        luz.type = LightType.Point;
        luz.color = RelevoPalette.ColorDe(ClaveMaterial.Ambar);
        luz.range = 8f;
        luz.intensity = 0f;
        lucesTorre[i] = luz;
    }

    void Encender(int i, bool on)
    {
        if (torres[i] != null) torres[i].sharedMaterial = RelevoPalette.MaterialDe(on ? ClaveMaterial.Ambar : ClaveMaterial.Cobre);
        if (lucesTorre[i] != null) lucesTorre[i].intensity = on ? 3f : 0f;
    }

    void CrearPanel()
    {
        var go = new GameObject("Señal de Maia", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        var escala = go.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1440, 900);
        escala.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var marco = RelevoUI.Franja(go.transform, "Marco", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(520, 74), RelevoUI.Fondo);
        RelevoUI.Texto(marco.transform, "Rótulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -4), new Vector2(500, 22), 15, TextAnchor.MiddleCenter, RelevoUI.Turquesa).text = "SEÑAL DE MAIA";
        var fondo = RelevoUI.Franja(marco.transform, "Fondo de la barra", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(480, 12), RelevoUI.FondoHondo);
        barra = RelevoUI.Franja(fondo.transform, "Barra", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(480, 12), RelevoUI.Turquesa);
        frase = RelevoUI.Texto(marco.transform, "Voz", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -46), new Vector2(510, 24), 15, TextAnchor.MiddleCenter, RelevoUI.AmbarSuave);
    }

    void PintarPanel()
    {
        if (barra == null) return;
        float f = Senal01;
        barra.rectTransform.sizeDelta = new Vector2(480f * f, 12f);
        Color c = f > 0.5f ? RelevoUI.Turquesa : (f > 0.25f ? RelevoUI.Ambar : RelevoUI.Cobre);
        if (f < 0.3f && Mathf.Repeat(Time.time * 3f, 1f) < 0.3f) c.a = 0.35f;   // la señal parpadea
        barra.color = c;
        if (avisando) frase.text = "¡El Eco va a lanzar un pulso!";
        else if (frase.text != fraseActual) frase.text = fraseActual;
    }

    void Decir(string texto)
    {
        fraseActual = texto;
        relojFrase = 12f;
        if (frase != null) frase.text = texto;
    }

    Vector3 Centro(Vector2Int c) { return layout.CentroTile(c.x, c.y); }
    static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }
}
