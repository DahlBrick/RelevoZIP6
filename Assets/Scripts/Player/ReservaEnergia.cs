using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reserva de energía de Nilo (Patio de Bombas en adelante).
///
///  - Cerca de la batería (o llevándola) Nilo se recarga. La zona segura se ve como un aro ámbar
///    discontinuo alrededor de la batería.
///  - Fuera de la zona, cada casilla recorrida gasta una unidad de reserva, y empujar una caja
///    gasta media más. Quedarse quieto no gasta: es un puzle de planificación, no de prisa.
///  - Con la reserva baja, la antena pasa de turquesa a ámbar y a coral, y parpadea.
///  - Si la reserva llega a cero lejos de la batería, Nilo se apaga: la animación se detiene
///    poco a poco, la antena se pone roja y se apaga, se le apagan los ojos y agacha la cabeza.
///    Después Maia lo explica y el nivel se reinicia.
///
/// Se añade en ejecución (LevelBootstrap) solo si la definición del nivel tiene reserva.
/// </summary>
public sealed class ReservaEnergia : MonoBehaviour
{
    public const float RadioZona = 3f;          // 1,5 casillas alrededor de la batería
    const float RecargaPorSegundo = 8f;          // casillas por segundo

    public float Maximo { get; private set; }
    public float Actual { get; private set; }
    public bool EnZona { get; private set; }
    public bool Apagado { get; private set; }
    public float Fraccion { get { return Maximo > 0f ? Mathf.Clamp01(Actual / Maximo) : 1f; } }

    PlayerController jugador;
    Animator animador;
    Transform cabeza, torso, figura;
    Renderer[] antena;
    Material materialAntena;
    readonly List<Renderer> ojos = new List<Renderer>();
    Material materialOjos;
    Transform aro;
    Light alarma;
    ParticleSystem humo;

    Vector3 posicionAnterior;
    float tamanoCasilla = 2f;
    float relojAlerta;
    bool avisoBajo;

    // Apagado
    float caida;                     // 0 → 1: cabeza y torso caen
    Quaternion baseCabeza, baseTorso;
    Vector3 ejeCabeza, ejeTorso;
    Vector3 baseFigura;

    static readonly Color Turquesa = new Color(0.25f, 0.9f, 0.85f);
    static readonly Color AmbarC = new Color(1f, 0.68f, 0.2f);
    static readonly Color CoralC = new Color(1f, 0.38f, 0.3f);
    static readonly Color Rojo = new Color(1f, 0.08f, 0.05f);

    public void Configurar(float maximoCasillas, float tamanoCasilla)
    {
        Maximo = maximoCasillas;
        Actual = maximoCasillas;
        this.tamanoCasilla = tamanoCasilla;
        RelevoGame.Reserva = this;
    }

    void Start()
    {
        jugador = GetComponent<PlayerController>();
        animador = GetComponentInChildren<Animator>();
        posicionAnterior = transform.position;

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Head" && cabeza == null) cabeza = t;
            else if (t.name == "Torso" && torso == null) torso = t;
            else if (t.name == "Figura") figura = t;
        }

        PrepararAntena();
        PrepararOjos();
        CrearAro();
        CrearAlarma();
    }

    // ------------------------------------------------------------------ piezas visuales

    void PrepararAntena()
    {
        var lista = new List<Renderer>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            if (r.name == "Antena punta" || r.name == "Aro de antena") lista.Add(r);
        antena = lista.ToArray();
        if (antena.Length == 0) return;

        // El latido genérico de la antena (AcentoEmisivo) se sustituye por el de la reserva.
        foreach (AcentoEmisivo a in GetComponentsInChildren<AcentoEmisivo>(true))
            if (a.name == "Antena base") a.enabled = false;

        materialAntena = new Material(antena[0].sharedMaterial);
        materialAntena.EnableKeyword("_EMISSION");
        foreach (Renderer r in antena) r.sharedMaterial = materialAntena;
        PintarAntena(Turquesa, 2.2f);
    }

    /// <summary>
    /// Ojos luminosos sobre las lentes del modelo: el robot de Quaternius no trae ojos que se
    /// puedan apagar, así que se añade un disco emisivo delante de cada lente.
    ///
    /// Las lentes se buscan en la propia malla de la cabeza (la parte oscura): de sus vértices se
    /// toman los más adelantados de cada lado. Así los ojos encajan sea cual sea la pose del
    /// Animator en este momento, y como cuelgan del hueso de la cabeza la siguen después.
    /// </summary>
    void PrepararOjos()
    {
        if (cabeza == null) return;
        Material baseOjos = RelevoPalette.MaterialDe(ClaveMaterial.NiloOjos);
        materialOjos = new Material(baseOjos);
        materialOjos.EnableKeyword("_EMISSION");
        PintarOjos(Turquesa, 2.5f);

        Vector3[] centros;
        if (!BuscarLentes(out centros)) return;

        for (int i = 0; i < centros.Length; i++)
        {
            GameObject ojo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ojo.name = "Ojo luz";
            Destroy(ojo.GetComponent<Collider>());
            ojo.transform.SetParent(transform, false);
            ojo.transform.position = centros[i];
            ojo.transform.localRotation = Quaternion.identity;
            ojo.transform.localScale = new Vector3(OjoTamano, OjoTamano, OjoTamano * 0.35f);
            ojo.transform.SetParent(cabeza, true);
            Renderer r = ojo.GetComponent<Renderer>();
            r.sharedMaterial = materialOjos;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ojos.Add(r);
        }
    }

    const float OjoTamano = 0.11f;

    /// <summary>Centro del frente de cada lente, en coordenadas de mundo.</summary>
    bool BuscarLentes(out Vector3[] centros)
    {
        centros = null;
        // La malla de la cabeza cuelga del hueso 'Head' y se llama igual.
        MeshRenderer render = null;
        foreach (MeshRenderer m in cabeza.GetComponentsInChildren<MeshRenderer>())
            if (m.name == "Head") { render = m; break; }
        MeshFilter filtro = render != null ? render.GetComponent<MeshFilter>() : null;
        if (render == null || filtro == null || filtro.sharedMesh == null) return false;
        Transform pieza = render.transform;

        // Submalla oscura: la del material 'Oscuro' (lentes, bandas y cejas).
        int sub = -1;
        Material[] ms = render.sharedMaterials;
        for (int i = 0; i < ms.Length; i++)
            if (ms[i] != null && ms[i].name.Contains("Oscuro")) sub = i;
        Mesh malla = filtro.sharedMesh;
        if (sub < 0 || sub >= malla.subMeshCount || !malla.isReadable) return false;

        Vector3[] v = malla.vertices;
        int[] tri = malla.GetTriangles(sub);
        var locales = new List<Vector3>();
        float maxZ = float.MinValue;
        var vistos = new HashSet<int>();
        foreach (int k in tri)
        {
            if (!vistos.Add(k)) continue;
            Vector3 l = transform.InverseTransformPoint(pieza.TransformPoint(v[k]));
            locales.Add(l);
            if (l.z > maxZ) maxZ = l.z;
        }

        // Las lentes sobresalen más que nada: el 12 % más adelantado, separado por lados.
        Vector3 izq = Vector3.zero, der = Vector3.zero;
        int ni = 0, nd = 0;
        foreach (Vector3 l in locales)
        {
            if (l.z < maxZ - 0.06f) continue;
            if (l.x < 0f) { izq += l; ni++; } else { der += l; nd++; }
        }
        if (ni == 0 || nd == 0) return false;

        izq /= ni;
        der /= nd;
        izq.z = der.z = maxZ + 0.005f;
        centros = new[] { transform.TransformPoint(izq), transform.TransformPoint(der) };
        return true;
    }

    void CrearAro()
    {
        GameObject go = new GameObject("Zona de recarga");
        aro = go.transform;
        Material m = RelevoPalette.MaterialDe(ClaveMaterial.Ambar);
        const int trazos = 28;
        for (int i = 0; i < trazos; i++)
        {
            float a = i * Mathf.PI * 2f / trazos;
            GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            t.name = "Trazo";
            Destroy(t.GetComponent<Collider>());
            t.transform.SetParent(aro, false);
            t.transform.localPosition = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * RadioZona;
            t.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            t.transform.localScale = new Vector3(0.07f, 0.03f, RadioZona * Mathf.PI * 2f / trazos * 0.55f);
            Renderer r = t.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    /// <summary>
    /// Luz roja en la antena y humo gris: desde la cámara cenital, el apagado se lee por el
    /// destello y la columna de humo, no solo por la pose.
    /// </summary>
    void CrearAlarma()
    {
        Transform punta = antena != null && antena.Length > 0 ? antena[0].transform : transform;
        GameObject l = new GameObject("Luz alarma");
        l.transform.SetParent(punta, false);
        alarma = l.AddComponent<Light>();
        alarma.type = LightType.Point;
        alarma.color = Rojo;
        alarma.range = 3.5f;
        alarma.intensity = 0f;
        alarma.shadows = LightShadows.None;

        GameObject h = new GameObject("Humo apagado");
        h.transform.SetParent(transform, false);
        h.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        humo = h.AddComponent<ParticleSystem>();
        humo.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = humo.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startColor = new Color(0.55f, 0.56f, 0.58f, 0.7f);
        main.gravityModifier = -0.08f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = humo.emission;
        em.rateOverTime = 9f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
        var forma = humo.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.2f;
        var col = humo.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.75f, 0.15f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var tam = humo.sizeOverLifetime;
        tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));
        var r = humo.GetComponent<ParticleSystemRenderer>();
        KitRelevo kit = KitRelevo.Activo;
        if (kit != null && kit.particula != null) r.sharedMaterial = kit.particula;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void PintarAntena(Color c, float brillo)
    {
        if (materialAntena == null) return;
        materialAntena.SetColor("_BaseColor", c * 0.6f);
        materialAntena.SetColor("_EmissionColor", c * brillo);
    }

    void PintarOjos(Color c, float brillo)
    {
        if (materialOjos == null) return;
        materialOjos.SetColor("_BaseColor", brillo <= 0.01f ? new Color(0.05f, 0.06f, 0.08f) : c);
        materialOjos.SetColor("_EmissionColor", c * brillo);
    }

    // ------------------------------------------------------------------ reserva

    /// <summary>Cuánto ha caído la cabeza al apagarse (0 a 1). Lo usa la cámara de cine.</summary>
    public float Caida { get { return caida; } }

    /// <summary>Fija la reserva a mano (herramientas de cine y pruebas).</summary>
    public void FijarReserva(float casillas) { Actual = Mathf.Clamp(casillas, 0f, Maximo); }

    /// <summary>Gasto extra (empujar una caja). No gasta dentro de la zona de recarga.</summary>
    public void Gastar(float casillas)
    {
        if (Apagado || EnZona) return;
        Actual = Mathf.Max(0f, Actual - casillas);
    }

    void Update()
    {
        BatteryController bateria = RelevoGame.Bateria;
        Vector3 pos = transform.position;
        float recorrido = new Vector2(pos.x - posicionAnterior.x, pos.z - posicionAnterior.z).magnitude;
        posicionAnterior = pos;

        // Aro de la zona segura: sigue a la batería mientras no la lleva Nilo.
        if (aro != null)
        {
            bool verAro = bateria != null && !bateria.EnManos && !Apagado;
            if (aro.gameObject.activeSelf != verAro) aro.gameObject.SetActive(verAro);
            if (verAro)
            {
                Vector3 b = bateria.transform.position;
                aro.position = new Vector3(b.x, 0.06f, b.z);
                aro.rotation = Quaternion.Euler(0f, Time.time * 12f, 0f);
                float pulso = 1f + Mathf.Sin(Time.time * 3f) * 0.025f;
                aro.localScale = new Vector3(pulso, 1f, pulso);
            }
        }

        if (Apagado) return;

        if (bateria == null) { EnZona = true; return; }

        Vector3 delta = bateria.transform.position - pos;
        delta.y = 0f;
        EnZona = bateria.EnManos || delta.magnitude <= RadioZona;

        bool bloqueado = RelevoGame.UI != null && RelevoGame.UI.BloqueaJuego;
        if (EnZona)
        {
            Actual = Mathf.Min(Maximo, Actual + RecargaPorSegundo * Time.deltaTime);
        }
        else if (!bloqueado && recorrido > 0.0005f && recorrido < 2f)
        {
            Actual = Mathf.Max(0f, Actual - recorrido / tamanoCasilla);
        }

        ActualizarAntena();

        if (!EnZona && Actual <= 0f) StartCoroutine(Apagarse());
    }

    void ActualizarAntena()
    {
        float f = Fraccion;
        Color c = f > 0.5f ? Turquesa : (f > 0.25f ? Color.Lerp(CoralC, AmbarC, (f - 0.25f) / 0.25f) : CoralC);
        float brillo = 2.2f;

        if (!EnZona && f <= 0.3f)
        {
            // Parpadeo cada vez más rápido y un pitido corto.
            float ritmo = Mathf.Lerp(7f, 3f, f / 0.3f);
            brillo = Mathf.Sin(Time.time * ritmo * Mathf.PI) > 0f ? 3f : 0.6f;
            relojAlerta -= Time.deltaTime;
            if (relojAlerta <= 0f)
            {
                relojAlerta = Mathf.Lerp(0.5f, 1.3f, f / 0.3f);
                AudioDirector.Pedir(Tono.Alerta);
            }
            if (!avisoBajo) avisoBajo = true;
        }
        else
        {
            relojAlerta = 0f;
            if (EnZona) avisoBajo = false;
        }

        PintarAntena(c, brillo);
    }

    // ------------------------------------------------------------------ apagado

    IEnumerator Apagarse()
    {
        Apagado = true;
        Actual = 0f;
        AudioDirector.Pedir(Tono.Apagado);
        if (RelevoGame.UI != null) RelevoGame.UI.RefrescarBloqueo();

        if (cabeza != null)
        {
            baseCabeza = cabeza.localRotation;
            ejeCabeza = Quaternion.Inverse(cabeza.rotation) * transform.right;
        }
        if (torso != null)
        {
            baseTorso = torso.localRotation;
            ejeTorso = Quaternion.Inverse(torso.rotation) * transform.right;
        }
        if (figura != null) baseFigura = figura.localPosition;

        // 1. La antena parpadea en rojo y los ojos titilan: está fallando.
        if (humo != null) humo.Play();
        for (int i = 0; i < 3; i++)
        {
            PintarAntena(Rojo, 4f);
            PintarOjos(Turquesa, 1.2f);
            if (alarma != null) alarma.intensity = 6f;
            yield return new WaitForSeconds(0.12f);
            PintarAntena(Rojo, 0.3f);
            PintarOjos(Turquesa, 0.2f);
            if (alarma != null) alarma.intensity = 0.5f;
            yield return new WaitForSeconds(0.12f);
        }

        // 2. Todo se ralentiza hasta pararse, la cabeza cae y la luz se va.
        float t = 0f;
        const float duracion = 1.1f;
        float velocidadAnimador = animador != null ? animador.speed : 1f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float s = Mathf.Clamp01(t / duracion);
            float e = s * s * (3f - 2f * s);
            if (animador != null) animador.speed = Mathf.Lerp(velocidadAnimador, 0f, Mathf.Clamp01(s * 1.4f));
            caida = e;
            PintarAntena(Rojo, Mathf.Lerp(2.4f, 0.15f, e));
            if (alarma != null) alarma.intensity = Mathf.Lerp(4f, 0f, e);
            PintarOjos(Turquesa, Mathf.Lerp(1f, 0f, Mathf.Clamp01(s * 2f)));
            yield return null;
        }
        PintarOjos(Turquesa, 0f);
        PintarAntena(new Color(0.35f, 0.04f, 0.03f), 0.05f);

        yield return new WaitForSeconds(0.6f);

        // 3. Maia lo cuenta y el patio vuelve a empezar al cerrar el diálogo.
        DialogoUI dialogo = RelevoGame.UI != null ? RelevoGame.UI.Dialogo : null;
        if (dialogo != null)
        {
            dialogo.Encolar(new DialogoUI.Mensaje
            {
                titulo = "Maia",
                texto = "Nilo se apagó lejos de la batería. Sin reserva no puede volver solo. Reiniciemos el patio y planea mejor la ruta.",
                acento = RelevoUI.Verde,
                ilustracion = IlustracionTutorial.Tipo.Apagado,
                alCerrar = delegate { if (RelevoGame.Nivel != null) RelevoGame.Nivel.ReiniciarNivel(); }
            });
        }
        else if (RelevoGame.Nivel != null)
        {
            RelevoGame.Nivel.ReiniciarNivel();
        }
    }

    void LateUpdate()
    {
        if (caida <= 0f) return;

        // Encima de la pose del Animator (ya detenido): la cabeza cae hacia el pecho, el torso se
        // vence un poco y el cuerpo se asienta, como un robot que se queda sin energía.
        if (cabeza != null) cabeza.localRotation = baseCabeza * Quaternion.AngleAxis(26f * caida, ejeCabeza);
        if (torso != null) torso.localRotation = baseTorso * Quaternion.AngleAxis(7f * caida, ejeTorso);
        if (figura != null) figura.localPosition = baseFigura + Vector3.down * 0.06f * caida;
    }

    void OnDestroy()
    {
        if (aro != null) Destroy(aro.gameObject);
        if (RelevoGame.Reserva == this) RelevoGame.Reserva = null;
    }
}
