using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Jefe final (nivel 13, la Sala del Relevo): el Eco del Apagón, la grabación de la ruta que hizo
/// Nilo la noche en que apagó Liria convertida en un Nilo gigante de tinta. Quiere apagarlo.
///
/// Ataques: persigue a Nilo y le golpea al tocarlo; embiste en línea recta (avisa con una línea en el
/// suelo); lanza pulsos que dañan alrededor y repliegan los puentes eléctricos dos segundos (un
/// condensador con carga aguanta). Por donde pasa deja sombra que gasta la reserva el doble.
///
/// Cómo se le vence: tres torres repartidas por la sala, cada una tras un puzzle. Con la batería
/// puesta, una torre se carga en dos segundos; si el Eco entra en su radio, la descarga le quita una
/// vida y lo aturde. Tres descargas y el Eco se deshace: el nivel termina al derrotarlo.
///
/// El reloj es la señal de Maia: baja sola y con cada golpe. Si se apaga, todo vuelve a empezar.
/// Se monta en ejecución, no se hornea.
/// </summary>
[DefaultExecutionOrder(500)]
public sealed partial class JefeEcoApagon : MonoBehaviour
{
    public static JefeEcoApagon Activo { get; private set; }

    enum Estado { Persigue, Prepara, Embiste, Aturdido, Vencido }

    const int VidaTotal = 3;
    /// <summary>La señal de Maia aguanta más si en el Puerto 6 se le dio la energía a su antena.</summary>
    static float SenalTotal
    {
        get { string e = SaveManager.DecisionEnergia; return e == "maia" ? 270f : e == "ciudad" ? 225f : 240f; }
    }
    const float Golpe = 16f;
    const float RadioContacto = 2f;
    const float RadioPulso = 6.5f;
    const float RadioTorre = 5f;
    const float CargaTorre = 2f;
    const float AvisoPulso = 1.5f;
    const float DuracionPulso = 2f;
    const float VidaSombra = 4f;
    const float Invulnerable = 1.6f;

    static readonly float[] Velocidad = { 2.5f, 3.1f, 3.7f };
    static readonly float[] EsperaEmbestida = { 6.5f, 5f, 3.8f };
    static readonly float[] AvisoEmbestida = { 1f, 0.85f, 0.7f };
    static readonly float[] IntervaloPulso = { 11f, 8.5f, 6.5f };
    const float VelocidadEmbestida = 13f;
    const float DuracionEmbestida = 0.9f;

    // Torres: casilla, nombre y dónde está (para el objetivo).
    static readonly Vector2Int[] CasillaTorre = { new Vector2Int(3, 2), new Vector2Int(19, 2), new Vector2Int(19, 12) };
    static readonly string[] NombreTorre = { "Torre de Cobre", "Torre de Sal", "Núcleo" };
    static readonly string[] LugarTorre = { "al oeste, tras la compuerta", "en el islote del norte", "en el islote del sur" };

    sealed class Torre
    {
        public EnergySocket baseTorre;
        public Renderer lampara;
        public Light luz;
        public LineRenderer rayo;
        public float carga, relojRayo;
        public bool usada;
    }

    LevelLayout layout;
    Transform eco, aro, figura;
    LineRenderer linea;
    Light brasa;
    Estado estado = Estado.Persigue;
    int vida = VidaTotal;
    float senal;
    float reloj, relojEmbestida, relojPulso, relojSombra, relojFrase, invulnerable, relojOnda, relojDerrota;
    Vector3 direccion, empuje;
    float relojEmpuje, pausa;
    bool avisandoPulso, perdido;

    readonly List<Torre> torres = new List<Torre>();
    readonly List<TemporaryBridge> caidos = new List<TemporaryBridge>();
    float restaurar;
    readonly Dictionary<Vector2Int, float> sombra = new Dictionary<Vector2Int, float>();
    readonly List<Transform> manchas = new List<Transform>();
    int siguienteMancha;

    Image barraVida, barraSenal, destello;
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
        senal = SenalTotal;
        CrearEco();
        CrearSombra();
        for (int i = 0; i < CasillaTorre.Length; i++) CrearTorre(i);
        CrearPanel();
        PrepararNoche();
        CrearFaroles();
        RecogerFigura();
        ReservaEnergia.MultiplicadorGasto = GastoEnSombra;
        relojEmbestida = EsperaEmbestida[0];
        relojPulso = IntervaloPulso[0];
        Decir("Nilo… esa figura eres tú, la noche del apagón. Viene a por ti. Atráelo a las torres.");
    }

    void Start()
    {
        AudioDirector.Musica("JefeEco", 1f);
    }

    void OnDestroy()
    {
        if (Activo == this) Activo = null;
        SoltarCamara();
        if (ReservaEnergia.MultiplicadorGasto == GastoEnSombra) ReservaEnergia.MultiplicadorGasto = null;
    }

    public int Fase { get { return Mathf.Clamp(VidaTotal - vida, 0, 2); } }
    public int Vida { get { return vida; } }
    public bool Derrotado { get { return estado == Estado.Vencido; } }
    public float Senal01 { get { return senal / SenalTotal; } }

    // ------------------------------------------------------------------ bucle

    void Update()
    {
        float dt = Time.deltaTime;
        if (estado == Estado.Vencido) { Desvanecer(dt); Amanecer(dt); AnimarChispas(dt); return; }

        bool bloqueado = RelevoGame.UI != null && RelevoGame.UI.BloqueaJuego;
        if (bloqueado || perdido || CaidaAlAgua.Activa) { PintarPanel(); return; }

        PlayerController nilo = RelevoGame.Jugador;
        if (nilo == null) return;

        invulnerable -= dt;
        Empujar(nilo, dt);
        Comportamiento(nilo, dt);
        Animar(dt);
        Pulsos(nilo, dt);
        Torres(dt);
        Sombras(dt);

        if (estado != Estado.Aturdido && invulnerable <= 0f
            && Plano(nilo.transform.position - eco.position).magnitude < RadioContacto)
            Danar(nilo, "¡Te ha alcanzado! No dejes que te toque.");

        senal = Mathf.Max(0f, senal - dt);
        if (senal <= 0f) Perder();

        relojFrase -= dt;
        if (relojFrase <= 0f && senal < SenalTotal * 0.3f) Decir("No pares, Nilo. Apagarse también es…");
        PintarPanel();
    }

    void Comportamiento(PlayerController nilo, float dt)
    {
        Vector3 haciaNilo = Plano(nilo.transform.position - eco.position);
        int f = Fase;
        switch (estado)
        {
            case Estado.Persigue:
                if (pausa > 0f) { pausa -= dt; break; }
                if (haciaNilo.magnitude > 0.2f)
                {
                    eco.position += haciaNilo.normalized * Mathf.Min(haciaNilo.magnitude, Velocidad[f] * dt);
                    Mirar(haciaNilo, dt);
                }
                relojEmbestida -= dt;
                if (relojEmbestida <= 0f && haciaNilo.magnitude > 3f && haciaNilo.magnitude < 16f && !avisandoPulso)
                {
                    estado = Estado.Prepara;
                    reloj = AvisoEmbestida[f];
                    AudioDirector.Pedir(Tono.Alerta);
                }
                break;

            case Estado.Prepara:
                direccion = haciaNilo.sqrMagnitude > 0.01f ? haciaNilo.normalized : eco.forward;
                Mirar(direccion, dt * 3f);
                linea.enabled = true;
                Vector3 a = eco.position + Vector3.up * 0.12f;
                linea.SetPosition(0, a);
                linea.SetPosition(1, a + direccion * VelocidadEmbestida * DuracionEmbestida);
                reloj -= dt;
                if (reloj <= 0f)
                {
                    estado = Estado.Embiste;
                    reloj = DuracionEmbestida;
                    AudioDirector.Pedir(Tono.Empujar);
                }
                break;

            case Estado.Embiste:
                eco.position += direccion * VelocidadEmbestida * dt;
                reloj -= dt;
                if (reloj <= 0f)
                {
                    linea.enabled = false;
                    estado = Estado.Persigue;
                    relojEmbestida = EsperaEmbestida[f];
                }
                break;

            case Estado.Aturdido:
                reloj -= dt;
                figura.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 30f) * 6f, 0f);
                if (reloj <= 0f)
                {
                    figura.localRotation = Quaternion.identity;
                    estado = Estado.Persigue;
                    relojEmbestida = EsperaEmbestida[f];
                }
                break;
        }
        LimitarAlMapa();
        float latido = avisandoPulso ? 1f + 0.6f * Mathf.PingPong(Time.time * 6f, 1f) : 1f;
        if (relojOnda > 0f)
        {
            relojOnda -= dt;
            latido = Mathf.Lerp(RadioPulso / 2.4f, 1f, relojOnda / 0.35f);
        }
        aro.localScale = Vector3.one * latido;
        brasa.intensity = estado == Estado.Aturdido ? 0.5f : (estado == Estado.Prepara ? 4f : 2.2f);
    }

    void Mirar(Vector3 d, float dt)
    {
        if (d.sqrMagnitude < 0.001f) return;
        eco.rotation = Quaternion.Slerp(eco.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-5f * dt));
    }

    void LimitarAlMapa()
    {
        Vector3 min = layout.CentroTile(0, 0), max = layout.CentroTile(layout.Columnas - 1, layout.FilasCuenta - 1);
        Vector3 p = eco.position;
        p.x = Mathf.Clamp(p.x, Mathf.Min(min.x, max.x), Mathf.Max(min.x, max.x));
        p.z = Mathf.Clamp(p.z, Mathf.Min(min.z, max.z), Mathf.Max(min.z, max.z));
        eco.position = p;
    }

    // ------------------------------------------------------------------ daño a Nilo

    void Danar(PlayerController nilo, string grito)
    {
        if (invulnerable > 0f) return;
        invulnerable = Invulnerable;
        senal = Mathf.Max(0f, senal - Golpe);
        AudioDirector.Pedir(Tono.Apagado);
        Vector3 d = Plano(nilo.transform.position - eco.position);
        empuje = (d.sqrMagnitude > 0.01f ? d.normalized : -eco.forward) * 7f;
        relojEmpuje = 0.25f;
        destello.color = new Color(0.9f, 0.25f, 0.2f, 0.45f);
        Sacudir(0.45f);
        Decir(grito);
        if (estado == Estado.Embiste) { reloj = 0f; }
        pausa = 1f;   // tras golpear, el Eco se queda un instante mirando: da tiempo a huir
    }

    void Empujar(PlayerController nilo, float dt)
    {
        if (destello != null && destello.color.a > 0f) destello.color = new Color(0.9f, 0.25f, 0.2f, Mathf.Max(0f, destello.color.a - dt * 1.2f));
        if (relojEmpuje <= 0f) return;
        relojEmpuje -= dt;
        CharacterController cc = nilo.GetComponent<CharacterController>();
        if (cc != null && cc.enabled) cc.Move(empuje * (dt / 0.25f));
    }

    // ------------------------------------------------------------------ pulso

    void Pulsos(PlayerController nilo, float dt)
    {
        if (caidos.Count > 0)
        {
            restaurar -= dt;
            if (restaurar <= 0f) Restaurar();
        }
        if (estado == Estado.Aturdido) return;

        relojPulso -= dt;
        if (!avisandoPulso && relojPulso <= AvisoPulso)
        {
            avisandoPulso = true;
            AudioDirector.Pedir(Tono.Alerta);
        }
        if (relojPulso > 0f) return;

        avisandoPulso = false;
        relojPulso = IntervaloPulso[Fase];
        relojOnda = 0.35f;
        Sacudir(0.3f);
        AudioDirector.Pedir(Tono.PuenteRetraido);

        foreach (TemporaryBridge p in FindObjectsByType<TemporaryBridge>(FindObjectsSortMode.None))
        {
            if (!p.Desplegado || caidos.Contains(p)) continue;
            EnergySocket b = BaseDe(p);
            Condensador c = b != null ? b.Condensador : null;
            if (c != null && c.Carga >= 0.3f) { c.Drenar(0.3f); continue; }
            caidos.Add(p);
            p.Activar(false);
        }
        if (caidos.Count > 0) restaurar = DuracionPulso;

        if (Plano(nilo.transform.position - eco.position).magnitude < RadioPulso)
            Danar(nilo, "¡El pulso te ha dado! Aléjate cuando brille.");
    }

    void Restaurar()
    {
        foreach (TemporaryBridge p in caidos)
        {
            if (p == null) continue;
            EnergySocket b = BaseDe(p);
            bool sostener = b != null ? b.TieneBateria || (b.Condensador != null && b.Condensador.Sosteniendo) : SostenidoPorPlaca(p);
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

    // ------------------------------------------------------------------ torres

    void Torres(float dt)
    {
        for (int i = 0; i < torres.Count; i++)
        {
            Torre t = torres[i];
            if (t.relojRayo > 0f)
            {
                t.relojRayo -= dt;
                t.rayo.enabled = t.relojRayo > 0f;
                if (t.rayo.enabled) Rayo(t);
            }
            if (t.usada) continue;
            bool conBateria = t.baseTorre != null && t.baseTorre.TieneBateria;
            float antes = t.carga;
            t.carga = conBateria ? Mathf.Min(1f, t.carga + dt / CargaTorre) : 0f;
            if (antes < 1f && t.carga >= 1f) AudioDirector.Pedir(Tono.Insertar);

            bool lista = t.carga >= 1f;
            Material m = RelevoPalette.MaterialDe(lista ? ClaveMaterial.Turquesa
                : (conBateria && Mathf.Repeat(Time.time * 4f, 1f) < 0.5f ? ClaveMaterial.Ambar : ClaveMaterial.Cobre));
            if (t.lampara.sharedMaterial != m) t.lampara.sharedMaterial = m;
            t.luz.intensity = lista ? 3.2f : t.carga * 1.5f;

            if (lista && estado != Estado.Aturdido
                && Plano(eco.position - t.baseTorre.transform.position).magnitude < RadioTorre)
                Descargar(i);
        }
    }

    void Descargar(int i)
    {
        Torre t = torres[i];
        t.usada = true;
        t.relojRayo = 0.6f;
        t.rayo.enabled = true;
        Rayo(t);
        t.lampara.sharedMaterial = RelevoPalette.MaterialDe(ClaveMaterial.Ambar);
        t.luz.intensity = 3f;
        AudioDirector.Pedir(Tono.Faro);
        AudioDirector.Pedir(Tono.PuenteActivo);

        vida--;
        linea.enabled = false;
        Sacudir(0.75f);
        Destellar();
        if (vida <= 0) { Vencer(); return; }

        estado = Estado.Aturdido;
        reloj = 3.5f;
        relojPulso = Mathf.Max(relojPulso, AvisoPulso + 3.5f);
        avisandoPulso = false;
        AudioDirector.Musica("JefeEco", 1f + 0.07f * Fase);
        Decir(vida == 2 ? "¡La Torre le ha dado! Recupera la batería: quedan dos." : "¡Otra vez! Una más, Nilo. Se ha vuelto más rápido.");
    }

    void Rayo(Torre t)
    {
        Vector3 a = t.baseTorre.transform.position + Vector3.up * 3.4f;
        Vector3 b = eco.position + Vector3.up * 2.5f;
        for (int k = 0; k < t.rayo.positionCount; k++)
        {
            float f = k / (float)(t.rayo.positionCount - 1);
            Vector3 p = Vector3.Lerp(a, b, f);
            if (k > 0 && k < t.rayo.positionCount - 1) p += Random.insideUnitSphere * 0.5f;
            t.rayo.SetPosition(k, p);
        }
    }

    // ------------------------------------------------------------------ sombra

    void Sombras(float dt)
    {
        relojSombra -= dt;
        if (relojSombra <= 0f && estado != Estado.Aturdido)
        {
            relojSombra = 0.3f;
            Vector2Int c = layout.Casilla(eco.position);
            sombra[c] = Time.time + VidaSombra;
            Transform m = manchas[siguienteMancha];
            siguienteMancha = (siguienteMancha + 1) % manchas.Count;
            Vector3 pos = layout.CentroTile(c.x, c.y);
            m.position = new Vector3(pos.x, 0.06f, pos.z);
            m.localScale = Vector3.one;
            m.gameObject.SetActive(true);
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

    // ------------------------------------------------------------------ final

    void Perder()
    {
        if (perdido) return;
        perdido = true;
        linea.enabled = false;
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
        estado = Estado.Vencido;
        relojDerrota = 2.2f;
        linea.enabled = false;
        foreach (TemporaryBridge p in caidos) if (p != null) { EnergySocket b = BaseDe(p); if ((b != null && b.TieneBateria) || SostenidoPorPlaca(p)) p.Activar(true); }
        caidos.Clear();
        foreach (Transform m in manchas) m.gameObject.SetActive(false);
        ReservaEnergia.MultiplicadorGasto = null;
        AudioDirector.Pedir(Tono.Faro);
        AudioDirector.Musica("MareasDeLiria", 1f);
        EstallarChispas();
        Sacudir(1f);
        Decir("Se deshace en la luz…");
    }

    bool cierrePedido;

    void Desvanecer(float dt)
    {
        relojDerrota -= dt;
        if (relojDerrota > 0f)
        {
            float f = Mathf.Clamp01(relojDerrota / 2.2f);
            eco.localScale = Vector3.one * f;
            eco.position += Vector3.up * dt * 1.5f;
            brasa.intensity = 6f * f;
            PintarPanel();
            return;
        }
        if (eco.gameObject.activeSelf)
        {
            eco.gameObject.SetActive(false);
            if (barraVida != null) barraVida.canvas.gameObject.SetActive(false);
        }
        // Se deja ver cómo se enciende Liria antes del cierre.
        if (!cierrePedido && relojDerrota <= -4.5f)
        {
            cierrePedido = true;
            if (RelevoGame.Nivel != null) RelevoGame.Nivel.CompletarPorJefe();
        }
    }

    // ------------------------------------------------------------------ objetivo

    public string Objetivo
    {
        get
        {
            string cabeza = "Derrota al Eco del Apagón (" + (VidaTotal - vida) + "/" + VidaTotal + "). ";
            if (estado == Estado.Vencido) return "El Eco del Apagón se deshace.";
            if (estado == Estado.Prepara) return cabeza + "¡Va a embestir! Sal de la línea.";
            if (avisandoPulso) return cabeza + "¡Pulso! Aléjate de él y sal de los puentes.";
            if (estado == Estado.Aturdido) return cabeza + "Está aturdido: recupera la batería y corre a otra torre.";

            BatteryController bat = RelevoGame.Bateria;
            if (bat == null) return cabeza;
            for (int i = 0; i < torres.Count; i++)
            {
                if (torres[i].baseTorre == null || bat.BaseActual != torres[i].baseTorre) continue;
                if (torres[i].usada) return cabeza + "Esta torre ya descargó: recupera la batería.";
                if (torres[i].carga < 1f) return cabeza + "La torre se carga… aguanta cerca.";
                return cabeza + "¡Torre lista! Atrae al Eco hasta ella.";
            }
            if (bat.BaseActual != null) return cabeza + "Recupera la batería de la " + bat.BaseActual.Etiqueta + " cuando puedas.";
            if (!bat.EnManos) return cabeza + "Recoge la batería.";
            for (int i = 0; i < torres.Count; i++)
                if (!torres[i].usada) return cabeza + "Lleva la batería a una torre apagada: " + NombreTorre[i] + ", " + LugarTorre[i] + ".";
            return cabeza;
        }
    }

    // ------------------------------------------------------------------ construcción

    void CrearEco()
    {
        Vector3 inicio = layout.CentroTile(12, 3);
        eco = DioramaBuilder.CrearAncla("Eco gigante", transform, inicio);
        figura = DioramaBuilder.CrearAncla("Figura", eco, Vector3.zero);
        var kit = KitRelevo.Activo;
        Material tinta = RelevoPalette.MaterialDe(ClaveMaterial.Fondo);
        Material ascua = RelevoPalette.MaterialDe(ClaveMaterial.Coral);
        if (kit != null && kit.niloModelo != null)
        {
            var modelo = KitRelevo.Colocar(kit.niloModelo, figura, Vector3.zero, 0, kit.niloEscala * 3.2f, false);
            foreach (var r in modelo.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++) mats[m] = m % 3 == 2 ? ascua : tinta;
                r.sharedMaterials = mats;
            }
            foreach (var c in modelo.GetComponentsInChildren<Collider>(true)) Destroy(c);
        }
        else
        {
            var cuerpo = DioramaBuilder.CrearCilindro("Cuerpo de tinta", figura, Vector3.up * 2.4f, 1.3f, 4.8f, tinta);
            foreach (var c in cuerpo.GetComponentsInChildren<Collider>(true)) Destroy(c);
        }
        aro = DioramaBuilder.CrearAro("Pulso", eco, Vector3.up * 0.15f, 2.4f, ascua).transform;

        brasa = new GameObject("Ascua").AddComponent<Light>();
        brasa.transform.SetParent(eco, false);
        brasa.transform.localPosition = Vector3.up * 3f;
        brasa.type = LightType.Point;
        brasa.color = RelevoPalette.ColorDe(ClaveMaterial.Coral);
        brasa.range = 10f;
        brasa.intensity = 2.2f;

        linea = new GameObject("Aviso de embestida").AddComponent<LineRenderer>();
        linea.transform.SetParent(transform, false);
        linea.positionCount = 2;
        linea.widthMultiplier = 1.6f;
        linea.sharedMaterial = ascua;
        linea.enabled = false;
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

    void CrearTorre(int i)
    {
        Vector3 pos = layout.CentroTile(CasillaTorre[i].x, CasillaTorre[i].y);
        var t = new Torre();
        t.baseTorre = ActorFactory.CrearBaseEnergia(transform, pos, "T" + (i + 1), NombreTorre[i].ToLowerInvariant().Replace("núcleo", "el núcleo"), null, pos + Vector3.forward);
        var ancla = DioramaBuilder.CrearAncla("Mástil de la " + NombreTorre[i], transform, pos + new Vector3(0.9f, 0f, 0.9f));
        DioramaBuilder.CrearCilindro("Poste", ancla, Vector3.up * 1.7f, 0.24f, 3.4f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
        var lampara = DioramaBuilder.CrearEsfera("Lámpara", ancla, Vector3.up * 3.6f, 0.85f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
        foreach (var c in ancla.GetComponentsInChildren<Collider>(true)) Destroy(c);
        t.lampara = lampara.GetComponent<Renderer>();
        t.luz = new GameObject("Luz").AddComponent<Light>();
        t.luz.transform.SetParent(ancla, false);
        t.luz.transform.localPosition = Vector3.up * 3.6f;
        t.luz.type = LightType.Point;
        t.luz.color = RelevoPalette.ColorDe(ClaveMaterial.Ambar);
        t.luz.range = 9f;
        t.luz.intensity = 0f;
        t.rayo = new GameObject("Rayo").AddComponent<LineRenderer>();
        t.rayo.transform.SetParent(ancla, false);
        t.rayo.positionCount = 8;
        t.rayo.widthMultiplier = 0.35f;
        t.rayo.sharedMaterial = RelevoPalette.MaterialDe(ClaveMaterial.Turquesa);
        t.rayo.enabled = false;
        torres.Add(t);
    }

    void CrearPanel()
    {
        var go = new GameObject("Combate", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        var escala = go.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1440, 900);
        escala.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        destello = RelevoUI.Franja(go.transform, "Golpe", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(4000, 4000), new Color(0.9f, 0.25f, 0.2f, 0f));
        destello.raycastTarget = false;

        var marco = RelevoUI.Franja(go.transform, "Marco", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(560, 104), RelevoUI.Fondo);
        RelevoUI.Texto(marco.transform, "Jefe", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -4), new Vector2(540, 22), 16, TextAnchor.MiddleCenter, RelevoUI.Cobre).text = "ECO DEL APAGÓN";
        var fondoVida = RelevoUI.Franja(marco.transform, "Fondo vida", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -29), new Vector2(520, 16), RelevoUI.FondoHondo);
        barraVida = RelevoUI.Franja(fondoVida.transform, "Vida", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(520, 16), RelevoPalette.ColorDe(ClaveMaterial.Coral));
        for (int i = 1; i < VidaTotal; i++)
            RelevoUI.Franja(fondoVida.transform, "Corte", new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520f * i / VidaTotal, 0), new Vector2(4, 16), RelevoUI.Fondo);
        RelevoUI.Texto(marco.transform, "Maia", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20, -50), new Vector2(130, 18), 13, TextAnchor.MiddleLeft, RelevoUI.Turquesa).text = "SEÑAL DE MAIA";
        var fondoSenal = RelevoUI.Franja(marco.transform, "Fondo señal", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150, -54), new Vector2(390, 9), RelevoUI.FondoHondo);
        barraSenal = RelevoUI.Franja(fondoSenal.transform, "Señal", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(390, 9), RelevoUI.Turquesa);
        frase = RelevoUI.Texto(marco.transform, "Voz", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -78), new Vector2(545, 22), 14, TextAnchor.MiddleCenter, RelevoUI.AmbarSuave);
    }

    void PintarPanel()
    {
        if (barraVida == null) return;
        barraVida.rectTransform.sizeDelta = new Vector2(520f * Mathf.Max(0, vida) / VidaTotal, 16f);
        float f = Senal01;
        barraSenal.rectTransform.sizeDelta = new Vector2(390f * f, 9f);
        Color c = f > 0.5f ? RelevoUI.Turquesa : (f > 0.25f ? RelevoUI.Ambar : RelevoUI.Cobre);
        if (f < 0.3f && Mathf.Repeat(Time.time * 3f, 1f) < 0.3f) c.a = 0.35f;
        barraSenal.color = c;
        string texto = estado == Estado.Prepara ? "¡Va a embestir!" : (avisandoPulso ? "¡Pulso del apagón!" : fraseActual);
        if (frase.text != texto) frase.text = texto;
    }

    void Decir(string texto)
    {
        // La voz se rompe a medida que se pierde su señal: al final solo quedan fragmentos.
        float s = Senal01;
        texto = Historia.InterferenciaMaia(texto, s > 0.6f ? 8 : s > 0.3f ? 11 : 13);
        fraseActual = texto;
        relojFrase = 10f;
        if (frase != null) frase.text = texto;
    }

    static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }
}
