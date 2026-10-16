using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Liria de noche, encendida: la única vez que el jugador la ve viva. Es el decorado de la primera
/// parte de la cinemática de apertura.
///
/// Un canal recto (eje z, hacia el norte) entre dos malecones de piedra, con casas a ambos lados
/// mirando al agua, faroles en el borde, dos puentes, barcas con velas, la rueda de una bomba que
/// gira, ventanas encendidas y, al fondo, la antena de la radio con su luz roja.
///
/// Cada fuente de luz (farol, ventana, vela) es una <see cref="Luz"/> con un orden de 0 (la más
/// lejana) a 1 (la más cercana a la cámara). La cinemática las corta en cadena, de lejos a cerca,
/// al ritmo de la canción: así se cuenta el apagón y, sin decirlo, lo que hizo Nilo.
///
/// Las casas son de verdad: se levantan con CasaModular (Medieval Village MegaKit de Quaternius),
/// con zócalo de piedra, entramado de madera, contraventanas, balcones y tejado de teja, en tres
/// hileras por orilla, en los colores planos de la paleta (low-poly, como el resto del juego). Los
/// malecones están adoquinados y con barandilla de hierro. No se ve a nadie: los habitantes quedan
/// a la imaginación, en las ventanas encendidas.
/// </summary>
public sealed partial class LiriaNocturna
{
    public const float AnchoCanal = 8f;          // de malecón a malecón
    const float Medio = AnchoCanal * 0.5f;
    const float FachadaOeste = -5.6f;            // x de la fachada de las casas del oeste
    const float ZInicio = -34f, ZFin = 96f;

    static readonly Color Calido = new Color(1f, 0.72f, 0.38f);
    static readonly Color Vela = new Color(1f, 0.62f, 0.28f);

    /// <summary>Una fuente de luz de la ciudad que se puede cortar.</summary>
    public sealed class Luz
    {
        public Light luz;
        public float intensidad;
        public readonly List<Material> materiales = new List<Material>();
        public Color emision;
        public readonly List<GameObject> sombras = new List<GameObject>();
        public float orden;            // 0 lejos … 1 cerca
        public float cortadaEn = -1f;  // momento del corte; <0 = encendida
        public float semilla;
        public Vector3 posicion;
    }

    public Transform Raiz { get; private set; }
    public readonly List<Luz> Luces = new List<Luz>();

    // Puntos de interés para la cámara.
    public Vector3 FarolDetalle { get; private set; }
    public Luz UltimoFarol { get; private set; }
    public Transform Rueda { get; private set; }
    public Transform Barca { get; private set; }
    public Vector3 Balcon { get; private set; }
    public Vector3 PuntoAntena { get; private set; }

    readonly System.Func<string, GameObject> modelo;
    readonly KitRelevo kit;
    Transform agua;
    float alturaAgua0;
    Light luzAntena;
    Material materialAntena;
    Material materialLuna;
    ParticleSystem chispas;
    readonly List<Transform> flotan = new List<Transform>();
    readonly Dictionary<Color, Material> lisos = new Dictionary<Color, Material>();

    LiriaNocturna(System.Func<string, GameObject> modelo, KitRelevo kit)
    {
        this.modelo = modelo;
        this.kit = kit;
    }

    public static LiriaNocturna Construir(Transform padre, System.Func<string, GameObject> modelo, KitRelevo kit)
    {
        LiriaNocturna l = new LiriaNocturna(modelo, kit);
        l.Levantar(padre);
        return l;
    }

    // ------------------------------------------------------------------ construcción

    void Levantar(Transform padre)
    {
        Raiz = new GameObject("Liria nocturna").transform;
        Raiz.SetParent(padre, false);

        Malecones();
        Agua();
        Casas();
        Faroles();
        Puentes();
        Barcas();
        RuedaDeBomba();
        Antena();
        Cielo();
        Chispas();

        // Orden de corte: de lo más lejano (norte) a lo más cercano a la cámara (sur).
        foreach (Luz l in Luces) l.orden = Mathf.InverseLerp(ZFin, ZInicio, l.posicion.z);
        Luces.Sort((a, b) => a.orden.CompareTo(b.orden));
    }

    void Malecones()
    {
        Material piedra = Liso(new Color(0.42f, 0.4f, 0.42f));
        Material borde = Liso(new Color(0.58f, 0.55f, 0.52f));
        float largo = ZFin - ZInicio + 20f, centroZ = (ZFin + ZInicio) * 0.5f;
        for (int lado = -1; lado <= 1; lado += 2)
        {
            Caja("Malecon", new Vector3(lado * (Medio + 30f), -0.8f, centroZ), new Vector3(60f, 1.6f, largo), piedra);
            Caja("Bordillo", new Vector3(lado * (Medio + 0.25f), 0.05f, centroZ), new Vector3(0.5f, 0.12f, largo), borde);
            Caja("Muro del canal", new Vector3(lado * (Medio + 0.05f), -1.2f, centroZ), new Vector3(0.12f, 2.4f, largo), Liso(new Color(0.3f, 0.3f, 0.33f)));

            // Adoquinado del paseo, entre el canal y la primera hilera de casas.
            for (float z = ZInicio - 4f; z < ZFin + 4f; z += 2f)
                for (int k = 0; k < 3; k++)
                    Pieza(Raiz, "Floor_UnevenBrick", new Vector3(lado * (Medio + 1f + 2f * k), 0.01f, z + 1f), 0f, Vector3.one, Color.white);

            // Barandilla de hierro en el borde, con huecos donde bajan las escaleras al agua.
            for (float z = ZInicio; z < ZFin; z += 2f)
            {
                if (Mathf.Repeat(z, 22f) < 2f) continue;
                Pieza(Raiz, "Prop_MetalFence_Ornament", new Vector3(lado * (Medio + 0.35f), 0.1f, z + 1f), lado * -90f, Vector3.one, new Color(0.35f, 0.35f, 0.38f));
            }
        }
        // Tierra lejos, al otro lado de las casas, para que la niebla tenga algo que comerse.
        Caja("Fondo norte", new Vector3(0f, -0.6f, ZFin + 30f), new Vector3(140f, 1.2f, 40f), piedra);
    }

    void Agua()
    {
        Material m = kit != null && kit.agua != null ? new Material(kit.agua) : Liso(new Color(0.05f, 0.1f, 0.18f));
        if (m.HasProperty("_ColorSomero"))
        {
            m.SetColor("_ColorSomero", new Color(0.09f, 0.16f, 0.24f));
            m.SetColor("_ColorProfundo", new Color(0.015f, 0.035f, 0.07f));
            m.SetColor("_ColorEspuma", new Color(0.35f, 0.42f, 0.5f));
            m.SetFloat("_Destellos", 0.9f);
            m.SetFloat("_Opacidad", 0.94f);
            m.SetFloat("_EscalaOndas", 0.55f);
        }
        float largo = ZFin - ZInicio + 20f;
        GameObject g = Caja("Agua", new Vector3(0f, -0.62f, (ZFin + ZInicio) * 0.5f), new Vector3(AnchoCanal, 0.3f, largo), m);
        g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        agua = g.transform;
        alturaAgua0 = agua.position.y;
    }

    const float Paseo = 4.5f;   // de la barandilla a la fachada de la primera hilera

    /// <summary>
    /// Tres hileras de casas por orilla, todas con la fachada hacia el canal. La primera, a pie del
    /// paseo; las otras dos asoman por detrás, más altas o más bajas, y desde el aire hacen ciudad.
    /// </summary>
    void Casas()
    {
        System.Random azar = new System.Random(21);
        float[] fachadas = { Medio + Paseo, Medio + Paseo + 14f, Medio + Paseo + 28f };
        for (int lado = -1; lado <= 1; lado += 2)
            for (int fila = 0; fila < fachadas.Length; fila++)
            {
                float z = ZInicio + (float)azar.NextDouble() * 3f + (lado > 0 ? 1.5f : 0f);
                while (z < ZFin - 4f)
                {
                    CasaModular.Plano plano = CasaModular.Azar(azar);
                    if (fila == 2 && azar.Next(3) == 0) plano.pisos = 2;
                    float medio = z + plano.ancho * 0.5f;
                    // Calles hacia los puentes y el hueco de la bomba.
                    bool hueco = Mathf.Abs(medio - 14f) < 4f || Mathf.Abs(medio - 46f) < 4f || (fila == 0 && lado < 0 && Mathf.Abs(medio - 20f) < 4.5f);
                    if (hueco) { z += 2f; continue; }

                    // La casa del balcón: primera hilera, orilla este, a la vista del canal.
                    bool balcon = fila == 0 && lado > 0 && Balcon == Vector3.zero && medio > 22f && plano.pisos > 1;
                    plano.balcon = balcon;

                    GameObject casa = CasaModular.Construir(Raiz, modelo, plano);
                    float x = lado * (fachadas[fila] + plano.fondo * 0.5f);
                    casa.transform.position = new Vector3(x, 0f, medio);
                    casa.transform.rotation = Quaternion.Euler(0f, lado < 0 ? 90f : -90f, 0f);
                    Iluminar(casa, fila, azar);

                    if (balcon)
                    {
                        Transform b = casa.transform.Find("Balcony_Simple_Straight");
                        if (b != null) Balcon = b.position + Vector3.up * 1.2f;
                    }

                    z += plano.ancho + (azar.Next(4) == 0 ? 2f : 0.02f);
                }
            }
    }

    /// <summary>Las ventanas de una casa: casi todas encendidas, alguna a oscuras.</summary>
    void Iluminar(GameObject casa, int fila, System.Random azar)
    {
        VentanasCasa v = casa.GetComponent<VentanasCasa>();
        if (v == null || v.Cristal == null || azar.Next(7) == 0) return;
        float brillo = 0.8f + (float)azar.NextDouble() * 0.5f;
        Luz l = new Luz { emision = new Color(1f, 0.7f, 0.36f) * (2.4f * brillo), posicion = casa.transform.position, semilla = (float)azar.NextDouble() * 10f };
        l.materiales.Add(v.Cristal);
        // Una luz de verdad solo en algunas casas de delante: el resto brilla por la emisión.
        if (fila == 0 && azar.Next(2) == 0)
        {
            Vector3 frente = casa.transform.position + casa.transform.forward * (casa.transform.localScale.z + 3f);
            l.luz = PuntoDeLuz(casa.transform, new Vector3(frente.x, 3.5f, frente.z), Calido, 2.2f, 7f);
            l.intensidad = l.luz.intensity;
        }
        Luces.Add(l);
    }

    void Faroles()
    {
        float paso = 7.5f;
        int i = 0;
        for (float z = ZInicio + 4f; z < ZFin - 4f; z += paso, i++)
        {
            for (int lado = -1; lado <= 1; lado += 2)
            {
                if ((i + (lado > 0 ? 1 : 0)) % 2 != 0 && z > 40f) continue;  // al fondo, al tresbolillo
                Vector3 pie = new Vector3(lado * (Medio + 0.7f), 0f, z + (lado > 0 ? paso * 0.5f : 0f));
                Luz l = Farol(pie);
                if (lado < 0 && Mathf.Abs(pie.z - 8f) < paso * 0.5f) FarolDetalle = l.posicion;
                if (lado > 0 && UltimoFarol == null && pie.z > -24f) UltimoFarol = l;
            }
        }
        if (UltimoFarol == null && Luces.Count > 0) UltimoFarol = Luces[0];
    }

    Luz Farol(Vector3 pie)
    {
        Transform t = new GameObject("Farol").transform;
        t.SetParent(Raiz, false);
        t.position = pie;
        GameObject poste = Pieza(t, "lantern", pie, 0f, Vector3.one * 2.1f, new Color(0.35f, 0.33f, 0.32f));
        float alto = 3.1f;
        if (poste == null)
        {
            Caja("Poste", pie + Vector3.up * 1.5f, new Vector3(0.14f, 3f, 0.14f), Liso(new Color(0.15f, 0.15f, 0.16f))).transform.SetParent(t, true);
        }
        Material llama = Emisivo(Calido, 2.2f);
        GameObject bombilla = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(bombilla.GetComponent<Collider>());
        bombilla.name = "Llama";
        bombilla.transform.SetParent(t, false);
        bombilla.transform.position = pie + Vector3.up * (alto - 0.35f);
        bombilla.transform.localScale = new Vector3(0.17f, 0.24f, 0.17f);
        bombilla.GetComponent<Renderer>().sharedMaterial = llama;
        bombilla.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Luz l = new Luz { emision = Calido * 2.2f, posicion = bombilla.transform.position, semilla = pie.z * 0.37f };
        l.materiales.Add(llama);
        l.luz = PuntoDeLuz(t, bombilla.transform.position, Calido, 5.5f, 10f);
        l.intensidad = l.luz.intensity;
        Luces.Add(l);
        return l;
    }

    void Puentes()
    {
        Material piedra = Liso(new Color(0.5f, 0.47f, 0.45f));
        foreach (float z in new[] { 14f, 46f })
        {
            Caja("Puente", new Vector3(0f, 0.25f, z), new Vector3(AnchoCanal + 2f, 0.5f, 3f), piedra);
            Caja("Arco", new Vector3(0f, -0.25f, z), new Vector3(AnchoCanal - 2.4f, 0.5f, 3f), Liso(new Color(0.2f, 0.2f, 0.22f)));
            for (int lado = -1; lado <= 1; lado += 2)
                Caja("Pretil", new Vector3(0f, 0.85f, z + lado * 1.35f), new Vector3(AnchoCanal + 2f, 0.7f, 0.3f), piedra);
            Luz a = Farol(new Vector3(-AnchoCanal * 0.5f - 0.2f, 0.5f, z - 1.35f));
            Luz b = Farol(new Vector3(AnchoCanal * 0.5f + 0.2f, 0.5f, z + 1.35f));
            a.semilla += 3f; b.semilla += 5f;
        }
    }

    void Barcas()
    {
        Vector3[] sitios = { new Vector3(-2.2f, -0.55f, 27f), new Vector3(2.4f, -0.55f, 4f), new Vector3(-2.3f, -0.55f, 62f), new Vector3(2.2f, -0.55f, 78f) };
        string[] modelos = { "boat-row-small", "boat-row-large" };
        for (int i = 0; i < sitios.Length; i++)
        {
            Transform b = new GameObject("Barca").transform;
            b.SetParent(Raiz, false);
            b.position = sitios[i];
            b.rotation = Quaternion.Euler(0f, 4f + i * 9f, 0f);
            GameObject casco = Pieza(b, modelos[i % 2], sitios[i], 4f + i * 9f, Vector3.one * 1.6f, new Color(0.85f, 0.7f, 0.55f));
            if (casco == null) Caja("Casco", sitios[i] + Vector3.up * 0.2f, new Vector3(1.2f, 0.4f, 3f), Liso(new Color(0.3f, 0.22f, 0.15f))).transform.SetParent(b, true);
            flotan.Add(b);

            // Una vela encendida en la proa.
            Vector3 vela = sitios[i] + b.forward * 0.9f + Vector3.up * 0.55f;
            Material m = Emisivo(Vela, 5f);
            GameObject llama = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(llama.GetComponent<Collider>());
            llama.transform.SetParent(b, false);
            llama.transform.position = vela;
            llama.transform.localScale = new Vector3(0.08f, 0.13f, 0.08f);
            llama.GetComponent<Renderer>().sharedMaterial = m;
            Caja("Cirio", vela + Vector3.down * 0.13f, new Vector3(0.07f, 0.16f, 0.07f), Liso(new Color(0.9f, 0.86f, 0.75f))).transform.SetParent(b, true);
            Luz l = new Luz { emision = Vela * 5f, posicion = vela, semilla = i * 2.3f };
            l.materiales.Add(m);
            l.luz = PuntoDeLuz(b, vela + Vector3.up * 0.15f, Vela, 3.2f, 5.5f);
            l.intensidad = l.luz.intensity;

            // En la primera barca, alguien sentado de espaldas, mirando el agua.
            if (i == 0)
            {
                Barca = b;
            }
            Luces.Add(l);
        }

    }

    /// <summary>La rueda de una bomba en el muro del canal, girando en el agua.</summary>
    void RuedaDeBomba()
    {
        Vector3 eje = new Vector3(-Medio + 0.55f, 0.4f, 20f);
        Transform soporte = new GameObject("Bomba").transform;
        soporte.SetParent(Raiz, false);
        soporte.position = eje;

        // La caseta de la bomba: una casa baja de piedra, de cara al canal.
        GameObject caseta = CasaModular.Construir(soporte, modelo, new CasaModular.Plano { ancho = 6, fondo = 6, pisos = 1, semilla = 4 });
        caseta.transform.position = new Vector3(-Medio - 1.2f - 3f, 0f, 20f);
        caseta.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        Iluminar(caseta, 0, new System.Random(1));

        // Un farolillo colgado sobre la rueda, para que se lea su giro a contraluz.
        Luz farolillo = new Luz { emision = Calido * 4f, posicion = new Vector3(-Medio + 0.2f, 2.6f, 21.6f), semilla = 7.7f };
        Material m = Emisivo(Calido, 4f);
        GameObject bombilla = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(bombilla.GetComponent<Collider>());
        bombilla.transform.SetParent(soporte, false);
        bombilla.transform.position = farolillo.posicion;
        bombilla.transform.localScale = new Vector3(0.22f, 0.28f, 0.22f);
        bombilla.GetComponent<Renderer>().sharedMaterial = m;
        farolillo.materiales.Add(m);
        farolillo.luz = PuntoDeLuz(soporte, farolillo.posicion + new Vector3(0.4f, -0.2f, 0f), Calido, 3.5f, 7f);
        farolillo.intensidad = farolillo.luz.intensity;
        Luces.Add(farolillo);

        Rueda = new GameObject("Rueda").transform;
        Rueda.SetParent(soporte, false);
        Rueda.position = eje;
        GameObject r = Pieza(Rueda, "wheel", eje, 90f, Vector3.one * 5.2f, new Color(0.85f, 0.68f, 0.5f));
        if (r != null)
        {
            // El modelo trae el eje desplazado: se centra sobre el pivote de la rueda.
            Bounds b = Limites(r);
            r.transform.position += eje - b.center;
        }
        else Caja("Paleta", eje, new Vector3(0.3f, 2.4f, 0.3f), Liso(new Color(0.4f, 0.3f, 0.2f))).transform.SetParent(Rueda, true);
    }

    void Antena() { AntenaEn(new Vector3(14f, 0f, ZFin + 18f)); }

    void AntenaEn(Vector3 pie)
    {
        Material hierro = Liso(new Color(0.12f, 0.12f, 0.14f));
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f + 45f;
            Vector3 d = Quaternion.Euler(0f, a, 0f) * Vector3.forward * 1.6f;
            GameObject pata = Caja("Pata", pie + d * 0.5f + Vector3.up * 13f, new Vector3(0.25f, 26f, 0.25f), hierro);
            pata.transform.rotation = Quaternion.FromToRotation(Vector3.up, (Vector3.up * 26f - d).normalized);
        }
        for (int k = 1; k < 9; k++)
            Caja("Travesano", pie + Vector3.up * k * 3f, new Vector3(3.2f - k * 0.3f, 0.12f, 0.12f), hierro);
        PuntoAntena = pie + Vector3.up * 27f;
        materialAntena = Emisivo(new Color(1f, 0.12f, 0.08f), 6f);
        GameObject foco = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(foco.GetComponent<Collider>());
        foco.transform.SetParent(Raiz, false);
        foco.transform.position = PuntoAntena;
        foco.transform.localScale = Vector3.one * 0.6f;
        foco.GetComponent<Renderer>().sharedMaterial = materialAntena;
        luzAntena = PuntoDeLuz(Raiz, PuntoAntena, new Color(1f, 0.15f, 0.1f), 4f, 14f);
    }

    void Cielo()
    {
        // Luna: un disco emisivo lejano; el bloom le pone el halo.
        materialLuna = Emisivo(new Color(0.8f, 0.86f, 1f), 1.6f);
        GameObject luna = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(luna.GetComponent<Collider>());
        luna.name = "Luna";
        luna.transform.SetParent(Raiz, false);
        luna.transform.position = new Vector3(-32f, 30f, 175f);
        luna.transform.localScale = Vector3.one * 7f;
        luna.GetComponent<Renderer>().sharedMaterial = materialLuna;
        luna.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Estrellas: partículas quietas sobre una cúpula.
        GameObject go = new GameObject("Estrellas");
        go.transform.SetParent(Raiz, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = 600;
        main.startLifetime = 1000f;
        main.startSpeed = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.enabled = false;
        var sh = ps.shape; sh.enabled = false;
        ParticleSystemRenderer pr = go.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = kit != null ? kit.particula : null;
        System.Random azar = new System.Random(8);
        ParticleSystem.Particle[] estrellas = new ParticleSystem.Particle[420];
        for (int i = 0; i < estrellas.Length; i++)
        {
            float az = (float)azar.NextDouble() * Mathf.PI * 2f;
            float el = Mathf.Lerp(0.12f, 1.4f, Mathf.Pow((float)azar.NextDouble(), 0.7f));
            Vector3 d = new Vector3(Mathf.Cos(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Sin(az) * Mathf.Cos(el));
            estrellas[i].position = new Vector3(0f, 0f, 40f) + d * 190f;
            estrellas[i].startSize = Mathf.Lerp(1.2f, 3.2f, Mathf.Pow((float)azar.NextDouble(), 2f));
            float b = Mathf.Lerp(0.35f, 1f, (float)azar.NextDouble());
            estrellas[i].startColor = new Color(0.85f, 0.9f, 1f, b);
            estrellas[i].remainingLifetime = 1000f;
            estrellas[i].startLifetime = 1000f;
        }
        ps.SetParticles(estrellas, estrellas.Length);
        ps.Pause();
    }

    void Chispas()
    {
        GameObject go = new GameObject("Chispas");
        go.transform.SetParent(Raiz, false);
        chispas = go.AddComponent<ParticleSystem>();
        chispas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = chispas.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.5f, 0.15f));
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = chispas.emission; em.enabled = false;
        var sh = chispas.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.1f;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = kit != null ? kit.particula : null;
    }

    // ------------------------------------------------------------------ animación

    /// <summary>Lo que vive mientras la ciudad está encendida: llamas, rueda, barcas, antena.</summary>
    public void Animar(float t, float dt)
    {
        foreach (Luz l in Luces)
        {
            float k = Brillo(l, t);
            foreach (Material m in l.materiales) m.SetColor("_EmissionColor", l.emision * k);
            if (l.luz != null) l.luz.intensity = l.intensidad * k;
            bool visibles = k > 0.05f;
            foreach (GameObject s in l.sombras) if (s.activeSelf != visibles) s.SetActive(visibles);
        }

        if (Rueda != null) Rueda.Rotate(Vector3.right, -dt * 26f * VelocidadRueda, Space.World);
        foreach (Transform b in flotan)
        {
            Vector3 p = b.position;
            p.y = agua.position.y + 0.07f + Mathf.Sin(t * 1.1f + p.z) * 0.04f;
            b.position = p;
            b.rotation = Quaternion.Euler(Mathf.Sin(t * 0.9f + p.z) * 2.5f, b.eulerAngles.y, Mathf.Sin(t * 0.7f + p.z * 0.3f) * 3f);
        }

        // La antena de Maia parpadea lento, como un latido. Es lo único que no se apaga.
        float latido = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 2.2f)), 6f);
        if (materialAntena != null) materialAntena.SetColor("_EmissionColor", new Color(1f, 0.12f, 0.08f) * (0.6f + 7f * latido));
        if (luzAntena != null) luzAntena.intensity = 0.5f + 5f * latido;
    }

    /// <summary>1 encendida, 0 apagada; con titileo de llama y un chisporroteo al cortarse.</summary>
    static float Brillo(Luz l, float t)
    {
        float vivo = 0.92f + 0.08f * Mathf.PerlinNoise(t * 3f, l.semilla);
        if (l.cortadaEn < 0f) return vivo;
        float d = t - l.cortadaEn;
        if (d < 0f) return vivo;
        if (d > 0.42f) return 0f;
        // Parpadeo de fallo: dos o tres golpes de luz antes de morir.
        float golpe = Mathf.PerlinNoise(d * 40f, l.semilla) > 0.5f ? 1f : 0.08f;
        return vivo * golpe * (1f - d / 0.42f);
    }

    /// <summary>Cuánto gira la rueda: la cinemática la frena durante el apagón.</summary>
    public float VelocidadRueda = 1f;

    public void Cortar(Luz l, float ahora)
    {
        if (l != null && l.cortadaEn < 0f) l.cortadaEn = ahora;
    }

    public void Chisporrotear(Vector3 donde, int cuantas)
    {
        if (chispas == null) return;
        chispas.transform.position = donde;
        chispas.Emit(cuantas);
    }

    /// <summary>0 = nivel normal; 1 = el agua rebasa el malecón.</summary>
    public void SubirAgua(float k)
    {
        if (agua == null) return;
        Vector3 p = agua.position;
        p.y = Mathf.Lerp(alturaAgua0, 0.22f, k);
        agua.position = p;
        if (k > 0f && !aguaAncha)
        {
            // Al rebasar el borde, el agua se extiende sobre el malecón.
            float ancho = Mathf.Lerp(AnchoCanal, AnchoCanal + 9f, Mathf.Clamp01((k - 0.75f) / 0.25f));
            agua.localScale = new Vector3(ancho, agua.localScale.y, agua.localScale.z);
        }
    }

    // ------------------------------------------------------------------ utilidades

    GameObject Pieza(Transform padre, string nombre, Vector3 pos, float giroY, Vector3 escala, Color tinte)
    {
        GameObject m = modelo != null ? modelo(nombre) : null;
        if (m == null) return null;
        GameObject g = Object.Instantiate(m, padre);
        g.name = nombre;
        foreach (Collider c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        g.transform.position = pos;
        g.transform.rotation = Quaternion.Euler(0f, giroY, 0f);
        g.transform.localScale = escala;
        Tenir(g, tinte);
        return g;
    }

    readonly Dictionary<string, Material> tenidos = new Dictionary<string, Material>();

    void Tenir(GameObject g, Color tinte)
    {
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            Material[] ms = r.sharedMaterials;
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i] == null) continue;
                string clave = ms[i].GetInstanceID() + "|" + tinte;
                Material m;
                if (!tenidos.TryGetValue(clave, out m))
                {
                    m = new Material(ms[i]);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.GetColor("_BaseColor") * tinte);
                    tenidos[clave] = m;
                }
                ms[i] = m;
            }
            r.sharedMaterials = ms;
        }
    }

    static Bounds Limites(GameObject g)
    {
        Bounds b = new Bounds(g.transform.position, Vector3.zero);
        bool hay = false;
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            if (hay) b.Encapsulate(r.bounds); else { b = r.bounds; hay = true; }
        }
        return b;
    }

    GameObject Caja(string nombre, Vector3 centro, Vector3 tam, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(g.GetComponent<Collider>());
        g.name = nombre;
        g.transform.SetParent(Raiz, false);
        g.transform.position = centro;
        g.transform.localScale = tam;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    GameObject Quad(string nombre, Vector3 centro, Quaternion rot, Vector2 tam, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(g.GetComponent<Collider>());
        g.name = nombre;
        g.transform.SetParent(Raiz, false);
        g.transform.position = centro;
        g.transform.rotation = rot;
        g.transform.localScale = new Vector3(tam.x, tam.y, 1f);
        Renderer r = g.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g;
    }

    Light PuntoDeLuz(Transform padre, Vector3 pos, Color color, float intensidad, float alcance)
    {
        GameObject g = new GameObject("Luz");
        g.transform.SetParent(padre, false);
        g.transform.position = pos;
        Light l = g.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensidad;
        l.range = alcance;
        l.shadows = LightShadows.None;
        return l;
    }

    Material Liso(Color c)
    {
        Material m;
        if (lisos.TryGetValue(c, out m)) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.2f);
        lisos[c] = m;
        return m;
    }

    static Material Emisivo(Color c, float fuerza)
    {
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", Color.Lerp(c, Color.black, 0.6f));
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        m.SetColor("_EmissionColor", c * fuerza);
        return m;
    }
}
