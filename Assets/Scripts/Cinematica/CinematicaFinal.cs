using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cinemática final · El relevo (plan_cinematicas.md, §3). Se juega al vencer al Eco en el Puerto 13
/// y termina en los créditos.
///
/// Tronco común: el Eco se deshace en la luz, silencio, Nilo solo en la sala del núcleo con la
/// batería en las manos y la radio al fondo. Después, según Historia.FinalActual:
///  A · El relevo: la batería se queda en el núcleo, Liria se enciende barrio a barrio, la última
///      emisión de Maia, Nilo se apaga, y en el muelle espera una batería para el siguiente turno.
///  B · Encender Liria: la luz vuelve a medias, la radio se apaga, Nilo sale a reparar.
///  C · Fue ella: Liria se enciende y Nilo arranca la antena de la radio. Silencio.
///  D · Estática: ruido, el muelle de la apertura otra vez y trece rayas en la baranda.
/// </summary>
public sealed class CinematicaFinal : CinematicaBase
{
    public const string Escena = "Cinematica_Final";

    const float P_Eco = 0f, P_Solo = 6f, P_Bateria = 12f, V = 18f;
    Historia.Final final;

    protected override float Fin
    {
        get
        {
            switch (final)
            {
                case Historia.Final.ElRelevo: return V + 50f;
                case Historia.Final.EncenderLiria: return V + 46f;
                case Historia.Final.FueElla: return V + 44f;
                default: return V + 38f;
            }
        }
    }

    // Sala del núcleo
    PlayerController nilo, niloCiudad;
    Transform bateria, radioSala, antenaSala, eco, caja;
    Material brilloBateria, nucleo, pilotoSala, ojosEco;
    readonly List<Transform> trozos = new List<Transform>();
    readonly List<Vector3> origenTrozo = new List<Vector3>(), velocidadTrozo = new List<Vector3>(), escalaTrozo = new List<Vector3>();
    readonly List<Renderer> antenaNilo = new List<Renderer>();
    static readonly Vector3 Hueco = new Vector3(0f, 2.15f, 3.2f);
    static readonly Vector3 InicioNilo = new Vector3(0f, 0f, -3f);
    static readonly Vector3 AnteNucleo = new Vector3(0f, 0f, 1.7f);

    // Liria y el muelle
    static readonly Vector3 Liria = new Vector3(0f, 0f, 400f);
    static readonly Vector3 PosMuelle = new Vector3(-400f, 0f, 0f);
    List<Material> ventanas;
    List<Vector3> barrios;
    Material pilotoMuelle;
    Transform bateriaMuelle;
    bool niloEnNucleo;

    protected override string Destino()
    {
        return Application.CanStreamedLevelBeLoaded(Creditos.Escena) ? Creditos.Escena : TituloRelevo.Escena;
    }

    protected override void Construir()
    {
        final = Historia.FinalActual;
        name = "Final " + final;

        // --- La sala del núcleo
        Transform s = Raiz("Sala del nucleo", Vector3.zero);
        Caja("Suelo", s, new Vector3(0f, -0.1f, 0f), new Vector3(30f, 0.2f, 30f), M(ClaveMaterial.PiedraBorde));
        Cilindro("Plataforma", s, new Vector3(0f, 0.05f, 1f), 5f, 0.1f, M(ClaveMaterial.Cobre));
        Caja("Pared", s, new Vector3(0f, 4f, 7f), new Vector3(30f, 8f, 0.5f), M(ClaveMaterial.Maqueta));
        for (int i = 0; i < 5; i++)
            Caja("Pilar", s, new Vector3(-10f + i * 5f, 4f, 6.6f), new Vector3(0.8f, 8f, 0.8f), M(ClaveMaterial.PiedraBorde));
        Cilindro("Nucleo", s, new Vector3(0f, 1.1f, 3.2f), 1.1f, 2.2f, M(ClaveMaterial.PiedraBorde));
        Cilindro("Aro", s, new Vector3(0f, 2.22f, 3.2f), 1.2f, 0.12f, M(ClaveMaterial.Cobre));
        nucleo = Brillo(new Color(0.25f, 0.2f, 0.12f), Color.black);
        Cilindro("Hueco", s, new Vector3(0f, 2.26f, 3.2f), 0.32f, 0.04f, nucleo);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            DioramaBuilder.CrearCajaEntre("Conducto", s, new Vector3(Mathf.Sin(a) * 1.2f, 0.1f, 3.2f + Mathf.Cos(a) * 1.2f),
                new Vector3(Mathf.Sin(a) * 9f, 0.1f, 3.2f + Mathf.Cos(a) * 9f), 0.18f, nucleo);
        }
        radioSala = Radio(s, new Vector3(4.2f, 0f, 2.5f), 30f, out pilotoSala, out antenaSala);
        ConstruirEco(s);

        nilo = CrearNilo(transform, InicioNilo, 0f);
        bateria = Bateria(nilo.transform, new Vector3(0f, 1.05f, 0.55f), out brilloBateria);

        // --- Liria, para los planos aéreos
        ventanas = Ciudad(Raiz("Liria", Liria), 12, 4, out barrios);
        if (final == Historia.Final.EncenderLiria)
        {
            niloCiudad = CrearNilo(transform, Liria + barrios[1] + new Vector3(11f, 0.2f, 0f), 90f);
            caja = Caja("Caja de herramientas", niloCiudad.transform, new Vector3(0.45f, 0.5f, 0.1f), new Vector3(0.5f, 0.3f, 0.25f), M(ClaveMaterial.Coral)).transform;
        }

        // --- El muelle del Puerto 1
        Transform antenaMuelle;
        Muelle(PosMuelle, final == Historia.Final.Estatica ? 13 : 0, out pilotoMuelle, out antenaMuelle);
        if (final == Historia.Final.ElRelevo)
        {
            Material b;
            bateriaMuelle = Bateria(transform, Vector3.zero, out b);
            bateriaMuelle.position = PosMuelle + new Vector3(-0.8f, 0.22f, 4.7f);
        }
    }

    void ConstruirEco(Transform padre)
    {
        eco = new GameObject("Eco").transform;
        eco.SetParent(padre, false);
        eco.localPosition = new Vector3(0f, 0f, 1f);
        Material cuerpo = M(ClaveMaterial.Fondo);
        ojosEco = Brillo(RelevoUI.Coral, RelevoUI.Coral * 2f);
        Trozo(Caja("Torso", eco, new Vector3(0f, 2.6f, 0f), new Vector3(1.8f, 2f, 1.1f), cuerpo));
        Trozo(Esfera("Cabeza", eco, new Vector3(0f, 4.2f, 0f), 1.4f, cuerpo));
        Trozo(Esfera("Ojo", eco, new Vector3(-0.3f, 4.3f, -0.6f), 0.3f, ojosEco));
        Trozo(Esfera("Ojo", eco, new Vector3(0.3f, 4.3f, -0.6f), 0.3f, ojosEco));
        Trozo(Caja("Brazo", eco, new Vector3(-1.3f, 2.4f, 0f), new Vector3(0.5f, 2f, 0.5f), cuerpo));
        Trozo(Caja("Brazo", eco, new Vector3(1.3f, 2.4f, 0f), new Vector3(0.5f, 2f, 0.5f), cuerpo));
        Trozo(Caja("Pierna", eco, new Vector3(-0.5f, 0.8f, 0f), new Vector3(0.6f, 1.6f, 0.6f), cuerpo));
        Trozo(Caja("Pierna", eco, new Vector3(0.5f, 0.8f, 0f), new Vector3(0.6f, 1.6f, 0.6f), cuerpo));
        // Chispas: la luz en la que se deshace.
        Material chispa = Brillo(RelevoUI.Ambar, RelevoUI.Ambar * 3f);
        var rnd = new System.Random(13);
        for (int i = 0; i < 40; i++)
        {
            Vector3 p = new Vector3((float)rnd.NextDouble() * 2f - 1f, 0.5f + (float)rnd.NextDouble() * 4f, (float)rnd.NextDouble() - 0.5f);
            Trozo(Caja("Chispa", eco, p, Vector3.one * 0.16f, chispa));
        }
        var r2 = new System.Random(5);
        foreach (Transform t in trozos)
        {
            origenTrozo.Add(t.localPosition);
            escalaTrozo.Add(t.localScale);
            Vector3 dir = new Vector3(t.localPosition.x + ((float)r2.NextDouble() - 0.5f), 0.6f + (float)r2.NextDouble(), ((float)r2.NextDouble() - 0.5f) * 2f);
            velocidadTrozo.Add(dir.normalized * (2f + (float)r2.NextDouble() * 4f));
        }
    }

    void Trozo(GameObject g) { trozos.Add(g.transform); }

    protected override void DespuesDelEstilo()
    {
        foreach (Renderer r in nilo.GetComponentsInChildren<Renderer>(true))
            if (r.name == "Antena punta" || r.name == "Aro de antena") antenaNilo.Add(r);
    }

    // ------------------------------------------------------------------ guion

    protected override void Evaluar(float previo, float t, float dt)
    {
        Base();
        Luna(0.9f, new Color(0.7f, 0.74f, 0.95f), new Vector3(50f, -25f, 0f));
        agua.volume = 0f;
        estatica.volume = 0f;
        musica.volume = 0f;
        foreach (Material v in ventanas) EncenderBarrio(v, 0f);
        pilotoSala.SetColor("_EmissionColor", RelevoUI.Verde * 1.2f);
        pilotoMuelle.SetColor("_EmissionColor", RelevoUI.Verde * 1.2f);
        nucleo.SetColor("_EmissionColor", Color.black);
        foreach (Renderer r in antenaNilo) r.enabled = true;
        DeshacerEco(t);

        if (t < V) Tronco(previo, t);
        else
        {
            switch (final)
            {
                case Historia.Final.ElRelevo: ElRelevo(previo, t - V); break;
                case Historia.Final.EncenderLiria: EncenderLiria(previo - V, t - V); break;
                case Historia.Final.FueElla: FueElla(previo - V, t - V); break;
                default: Estatica(previo - V, t - V); break;
            }
        }
    }

    void DeshacerEco(float t)
    {
        float u = Tramo(t, 0.4f, 5.5f);
        for (int i = 0; i < trozos.Count; i++)
        {
            trozos[i].localPosition = origenTrozo[i] + velocidadTrozo[i] * u * 2.2f + Vector3.up * u * u * 2f;
            trozos[i].localScale = escalaTrozo[i] * (1f - S(u));
            trozos[i].localRotation = Quaternion.Euler(u * 200f * (i % 3), u * 150f * (i % 2), 0f);
        }
        eco.gameObject.SetActive(u < 1f);
        ojosEco.SetColor("_EmissionColor", RelevoUI.Coral * 2f * (1f - u));
    }

    void Tronco(float previo, float t)
    {
        Poner(nilo, InicioNilo, 0f);
        brilloBateria.SetColor("_EmissionColor", RelevoUI.Ambar * 1.6f);
        if (t < P_Solo)
        {
            // Plano general: el Eco estalla en luz; un fogonazo que se desvanece.
            Colocar(new Vector3(-7f, 4f, -9f), new Vector3(0f, 2.5f, 1f), 42f, 0.6f);
            destello.color = new Color(1f, 0.9f, 0.7f, 1f - Tramo(t, 0f, 2.5f));
            if (Cruza(previo, t, 0.01f)) { Sonar(clipGolpe, 1f); Sacudir(0.8f); }
        }
        else if (t < P_Bateria)
        {
            // Silencio: Nilo solo en la sala, la cámara se acerca despacio.
            float u = Tramo(t, P_Solo, P_Bateria);
            Colocar(new Vector3(-6f + u * 2f, 3f - u * 0.6f, -10f + u * 2f), InicioNilo + new Vector3(0f, 1.2f, 2f), 38f, 0.5f);
            Pie("Silencio. En la sala del núcleo solo queda Nilo.", Ventana(t, P_Solo + 1f, P_Bateria - 0.2f));
        }
        else
        {
            // Primer plano de frente: la batería en sus manos (la radio ya se vio al fondo en el plano anterior).
            Vector3 c = bateria.position;
            float u = Tramo(t, P_Bateria, V);
            Colocar(c + new Vector3(1.7f - u * 0.3f, 0.6f, 2.1f - u * 0.3f), c + new Vector3(0f, 0.15f, 0f), 34f, 0.4f);
            Pie("La batería todavía late en sus manos.", Ventana(t, P_Bateria + 0.8f, V - 0.2f));
        }
    }

    /// <summary>Nilo va al núcleo y deja la batería en el hueco (de 0 a 6 s desde 'a').</summary>
    void DejarBateria(float previo, float t, float a)
    {
        float anda = S(Tramo(t, a, a + 3.5f));
        Poner(nilo, Vector3.Lerp(InicioNilo, AnteNucleo, anda), 0f);
        float mete = S(Tramo(t, a + 3.8f, a + 5f));
        if (mete <= 0f)
        {
            bateria.SetParent(nilo.transform, false);
            bateria.localPosition = new Vector3(0f, 1.05f, 0.55f);
        }
        else
        {
            Vector3 mano = nilo.transform.TransformPoint(new Vector3(0f, 1.05f, 0.55f));
            bateria.SetParent(transform, true);
            bateria.position = Vector3.Lerp(mano, Hueco, mete);
        }
        float luz = Tramo(t, a + 5f, a + 6f);
        nucleo.SetColor("_EmissionColor", RelevoUI.Ambar * 2.4f * luz);
        if (Cruza(previo, t, a + 5f)) { Sonar(clipArranque, 0.9f); Sacudir(0.3f); }
        Vector3 c = new Vector3(0f, 1.6f, 2f);
        Colocar(new Vector3(4.5f, 3f, -1.5f), c, 40f, 0.5f);
    }

    /// <summary>Plano aéreo de Liria; 'cuantos' barrios se encienden en orden desde 'a'.</summary>
    void Aereo(float t, float a, float duracion, int cuantos, bool alternos)
    {
        float u = Tramo(t, a, a + duracion);
        Vector3 centro = Liria + new Vector3(0f, 0f, 22f);
        Colocar(centro + new Vector3(-60f + u * 30f, 55f - u * 10f, -70f + u * 10f), centro, 42f, 0.3f);
        Luna(0.55f, new Color(0.5f, 0.58f, 0.9f), new Vector3(50f, -25f, 0f));
        int n = 0;
        for (int i = 0; i < ventanas.Count; i++)
        {
            if (alternos && i % 2 == 1) continue;
            if (n >= cuantos) break;
            EncenderBarrio(ventanas[i], Tramo(t, a + 1f + n * 0.9f, a + 1.6f + n * 0.9f));
            n++;
        }
    }

    void Titulo(float t, float a, float b)
    {
        // Fondo negro bajo el texto (el destello está debajo de los rótulos; el fundido, encima).
        destello.color = new Color(0f, 0f, 0f, Tramo(t, a - 1f, a));
        Cita(Historia.TituloFinal(final), Ventana(t, a, b), RelevoUI.Ambar);
        Fundido(Tramo(t, b - 0.4f, b));
    }

    // A · El relevo
    void ElRelevo(float previo, float u)
    {
        float p = previo - V;
        if (u < 6f) { DejarBateria(p, u, 0f); return; }
        nucleo.SetColor("_EmissionColor", RelevoUI.Ambar * 2.4f);
        bateria.SetParent(transform, true);
        bateria.position = Hueco;
        Poner(nilo, AnteNucleo, 0f);
        musica.volume = 0.4f * Tramo(u, 6f, 8f);

        if (u < 22f)
        {
            Aereo(u, 6f, 16f, 12, false);
            Cita("«Apagarse también es parte de la canción»", Ventana(u, 13f, 20f), RelevoUI.Verde);
            Fundido(1f - Tramo(u, 6f, 6.8f));
        }
        else if (u < 32f)
        {
            // Nilo se apaga despacio: se sienta, se inclina, la antena se apaga.
            foreach (Material v in ventanas) EncenderBarrio(v, 1f);
            float baja = S(Tramo(u, 23f, 29f));
            Poner(nilo, AnteNucleo + new Vector3(0f, -0.25f * baja, 0f), 0f, 20f * baja);
            foreach (Renderer r in antenaNilo) r.enabled = u < 28.5f || (u < 29.2f && Mathf.PerlinNoise(u * 15f, 0f) > 0.5f);
            Vector3 c = AnteNucleo + new Vector3(0f, 1.4f, 0f);
            Colocar(c + new Vector3(-3.5f, 0.6f, -3.4f + (u - 22f) * 0.1f), c, 36f, 0.3f);
            Luna(0.9f * (1f - 0.4f * baja), new Color(1f, 0.85f, 0.6f), new Vector3(40f, 30f, 0f));
            Pie("Esta vez, el relevo lo lleva él.", Ventana(u, 25f, 31.5f));
            Fundido(1f - Tramo(u, 22f, 22.8f));
        }
        else if (u < 42f)
        {
            // El muelle del principio: una batería espera junto a la baranda.
            foreach (Renderer r in antenaNilo) r.enabled = false;
            Luna(1.2f, new Color(1f, 0.86f, 0.7f), new Vector3(20f, 60f, 0f));
            Vector3 c = bateriaMuelle.position;
            Colocar(c + new Vector3(1.6f - (u - 32f) * 0.08f, 0.9f, -2.4f), c + new Vector3(0f, 0.4f, 0.4f), 36f, 0.4f);
            Pie("Junto a la baranda, una batería espera al siguiente turno.", Ventana(u, 33.5f, 41.5f));
            Fundido(1f - Tramo(u, 32f, 32.8f));
            agua.volume = 0.3f;
        }
        else Titulo(u, 43f, 50f);
    }

    // B · Encender Liria
    void EncenderLiria(float p, float u)
    {
        if (u < 6f) { DejarBateria(p, u, 0f); return; }
        nucleo.SetColor("_EmissionColor", RelevoUI.Ambar * 1.2f);
        bateria.SetParent(transform, true);
        bateria.position = Hueco;
        Poner(nilo, AnteNucleo, 0f);

        if (u < 17f)
        {
            Aereo(u, 6f, 11f, 6, true);
            Cita("«Apagarse también es parte de la canción»", Ventana(u, 10f, 16f), new Color(RelevoUI.Verde.r, RelevoUI.Verde.g, RelevoUI.Verde.b));
            estatica.volume = 0.08f;
            Fundido(1f - Tramo(u, 6f, 6.8f));
        }
        else if (u < 24f)
        {
            // La radio se apaga.
            Vector3 c = radioSala.position + new Vector3(0f, 0.5f, 0f);
            Colocar(c + new Vector3(-1.6f, 0.6f, -2f), c, 34f, 0.3f);
            pilotoSala.SetColor("_EmissionColor", u < 20f ? RelevoUI.Verde * 1.2f : Color.black);
            if (Cruza(p, u, 20f)) Sonar(clipRele, 0.9f);
            Pie("Después, solo silencio.", Ventana(u, 20.5f, 23.8f));
        }
        else if (u < 38f)
        {
            // Nilo pequeño, con su caja de herramientas, cruzando los puentes encendidos.
            for (int i = 0; i < ventanas.Count; i += 2) EncenderBarrio(ventanas[i], 1f);
            float k = Tramo(u, 24f, 38f);
            Vector3 desde = Liria + barrios[1] + new Vector3(6f, 0.2f, 0f), hasta = Liria + barrios[2] + new Vector3(-6f, 0.2f, 0f);
            // Solo el tramo del puente: en los extremos quedaría dentro de los edificios.
            Poner(niloCiudad, Vector3.Lerp(desde, hasta, Mathf.Lerp(0.35f, 0.8f, k)), 90f);
            Vector3 c = niloCiudad.transform.position;
            // Cámara alta: a media altura los edificios tapaban a Nilo.
            Colocar(c + new Vector3(-3f, 16f, -10f), c + new Vector3(1.5f, 0f, 0f), 34f, 0.3f);
            Luna(0.7f, new Color(0.6f, 0.66f, 0.95f), new Vector3(45f, -30f, 0f));
            Pie("Nilo se queda de turno, reparando lo que rompió. Nadie viene a darle las gracias. Nadie viene a culparlo.", Ventana(u, 26f, 37.5f));
            Fundido(1f - Tramo(u, 24f, 24.8f));
            musica.volume = 0.22f;
        }
        else Titulo(u, 39f, 46f);
    }

    // C · Fue ella
    void FueElla(float p, float u)
    {
        if (u < 6f) { DejarBateria(p, u, 0f); return; }
        nucleo.SetColor("_EmissionColor", RelevoUI.Ambar * 2.4f);
        bateria.SetParent(transform, true);
        bateria.position = Hueco;

        if (u < 15f)
        {
            Poner(nilo, AnteNucleo, 0f);
            Aereo(u, 6f, 9f, 12, false);
            Fundido(1f - Tramo(u, 6f, 6.8f));
            musica.volume = 0.3f;
        }
        else if (u < 28f)
        {
            // Nilo va hasta la radio y arranca la antena.
            float anda = S(Tramo(u, 15.5f, 19.5f));
            Vector3 frente = radioSala.position + new Vector3(-1.1f, 0f, -0.6f);
            Vector3 dir = radioSala.position - frente; dir.y = 0f;
            float giro = Mathf.LerpAngle(0f, Quaternion.LookRotation(dir).eulerAngles.y, Tramo(u, 15f, 16f));
            Poner(nilo, Vector3.Lerp(AnteNucleo, frente, anda), giro);
            float arranca = S(Tramo(u, 21f, 21.6f));
            antenaSala.localPosition = new Vector3(0.3f, 0.7f, 0f) + new Vector3(0.6f, -0.4f, -0.5f) * arranca;
            antenaSala.localRotation = Quaternion.Euler(0f, 0f, -18f - 70f * arranca);
            pilotoSala.SetColor("_EmissionColor", u < 21.4f ? RelevoUI.Verde * 1.2f : Color.black);
            if (Cruza(p, u, 21.2f)) { Sonar(clipGolpe, 0.9f); Sacudir(0.4f); }
            Vector3 c = radioSala.position + new Vector3(-0.4f, 0.8f, 0f);
            Colocar(c + new Vector3(-3.6f, 1f, -3.2f), c, 38f, 0.4f);
            Pie("Fue la radio, se dice. Fue ella quien se lo pidió.", Ventana(u, 23f, 27.8f));
            color.saturation.Override(-30f * Tramo(u, 21f, 24f));
        }
        else if (u < 36f)
        {
            // El muelle: la radio, con el piloto en negro.
            pilotoMuelle.SetColor("_EmissionColor", Color.black);
            Luna(1f, new Color(0.7f, 0.75f, 0.9f), new Vector3(30f, 70f, 0f));
            Vector3 c = PosMuelle + new Vector3(2.4f, 0.5f, 3.6f);
            Colocar(c + new Vector3(-2f + (u - 28f) * 0.05f, 0.7f, -2.6f), c, 34f, 0.3f);
            color.saturation.Override(-35f);
            Pie("La radio no vuelve a sonar.", Ventana(u, 29.5f, 35.5f));
            Fundido(1f - Tramo(u, 28f, 28.8f));
            agua.volume = 0.25f;
        }
        else Titulo(u, 37f, 44f);
    }

    // D · Estática
    void Estatica(float p, float u)
    {
        if (u < 6f)
        {
            // Nilo se vuelve hacia la radio; la estática lo llena todo.
            Vector3 dir = radioSala.position - InicioNilo; dir.y = 0f;
            Poner(nilo, InicioNilo, Mathf.LerpAngle(0f, Quaternion.LookRotation(dir).eulerAngles.y, S(Tramo(u, 0f, 2f))));
            Vector3 c = InicioNilo + new Vector3(0f, 1.4f, 0f);
            Colocar(c + new Vector3(-2.2f, 0.4f, 3.2f), c, 38f, 0.6f);
            estatica.volume = 0.6f * Tramo(u, 1f, 5f);
            float ruido = Mathf.PerlinNoise(u * 30f, 0.3f);
            destello.color = new Color(1f, 1f, 1f, Tramo(u, 2f, 5.5f) * (0.4f + 0.6f * ruido));
            Fundido(Tramo(u, 5.4f, 6f));
            return;
        }
        if (u < 18f)
        {
            // El muelle de la apertura. La radio da las buenas noches.
            Luna(0.9f, new Color(0.55f, 0.62f, 0.95f), new Vector3(35f, 50f, 0f));
            Vector3 c = PosMuelle + new Vector3(2.4f, 0.5f, 3.6f);
            Colocar(c + new Vector3(-2.4f, 0.9f, -3.2f), c, 36f, 0.4f);
            estatica.volume = 0.15f;
            agua.volume = 0.3f;
            Cita(Escribir("«Buenas noches a quienes siguen de turno»", Tramo(u, 9f, 12.5f)), Ventana(u, 9f, 17.5f), RelevoUI.Verde);
            Fundido(1f - Tramo(u, 6f, 7.5f));
            return;
        }
        if (u < 28f)
        {
            // La cámara baja hasta la baranda: trece rayas.
            float k = S(Tramo(u, 18f, 24f));
            Vector3 rayas = PosMuelle + new Vector3(-0.2f, 1f, 5.2f);
            Vector3 desde = PosMuelle + new Vector3(2f, 1.8f, 1.6f), hasta = rayas + new Vector3(0.2f, 0.15f, -1.1f);
            Colocar(Vector3.Lerp(desde, hasta, k), Vector3.Lerp(PosMuelle + new Vector3(2.4f, 0.5f, 3.6f), rayas, k), 36f, 0.3f);
            Luna(0.9f, new Color(0.55f, 0.62f, 0.95f), new Vector3(35f, 20f, 0f));
            agua.volume = 0.3f;
            Pie("Trece rayas en la baranda. No es la primera vez.", Ventana(u, 23f, 27.8f));
            return;
        }
        Titulo(u, 29f, 38f);
    }
}
