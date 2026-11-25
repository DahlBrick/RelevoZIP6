using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cinemática de decisión 1 · La antena (docs/narrativa/plan_cinematicas.md, §1). Tras el Puerto 6.
///
/// La esclusa se abre y la corriente vuelve por los cables de la orilla hasta un cruce. Un ramal va
/// a la torre de la emisora, con la antena casi apagada; el otro, al barrio a oscuras. La radio de
/// Maia pide la energía para sí. Nilo mira a un lado y al otro. Elección. Resolución: se enciende
/// el barrio y la antena se apaga, o se enciende la antena y el barrio sigue a oscuras.
/// Guarda SaveManager.Decidir(1, "ciudad" | "maia") y carga el Puerto 7.
/// </summary>
public sealed class CinematicaAntena : CinematicaBase
{
    public const string Escena = "Cinematica_Antena";

    const float P_Esclusa = 0f, P_Cables = 8f, P_Radio = 18f, P_Linea = 20f, P_Mirar = 22f, P_Eleccion = 30f,
        P_Resolucion = 30.2f, FinTotal = 44f;
    protected override float Fin { get { return FinTotal; } }

    PlayerController nilo;
    Transform hojaA, hojaB, aguaAlta, radio;
    Material piloto, matAntena, matTorre;
    readonly List<Material> cableComun = new List<Material>(), cableTorre = new List<Material>(), cableBarrio = new List<Material>();
    List<Material> ventanas;
    List<Vector3> centrosBarrio;
    bool lineaDicha, respuestaDicha;

    static readonly Vector3 Cruce = new Vector3(0f, 0.06f, 14f);
    static readonly Vector3 Torre = new Vector3(-20f, 0f, 34f);
    static readonly Vector3 PuntoBarrio = new Vector3(14f, 0.06f, 30f);
    static readonly Vector3 PosNilo = new Vector3(1.6f, 0f, 5f);

    protected override string Destino()
    {
        string n = LevelManager.NombreEscenaNivel(7);
        return Application.CanStreamedLevelBeLoaded(n) ? n : LevelManager.NombreEscenaNivel(1);
    }

    protected override void ElegirOpcion(int opcion)
    {
        base.ElegirOpcion(opcion);
        SaveManager.Decidir(1, Historia.ValorDecision(1, opcion));
        if (T < P_Resolucion) T = P_Resolucion;
    }

    protected override void AlTerminar() { if (eleccion < 0) ElegirOpcion(0); }

    protected override void Saltar()
    {
        if (eleccion >= 0) { Terminar(); return; }
        dialogo.Cerrar();
        lineaDicha = true;
        T = P_Eleccion - 0.1f;
    }

    protected override bool Retener()
    {
        // La línea de Maia retiene la escena hasta que se cierra; la elección, hasta que se responde.
        return esperandoEleccion || (T >= P_Linea + 1.5f && T < P_Mirar && dialogo.Abierto);
    }

    // ------------------------------------------------------------------ decorado

    protected override void Construir()
    {
        Transform r = Raiz("Esclusa", Vector3.zero);
        // Canal este-oeste con dos muelles, las dos hojas en el centro.
        Caja("Agua baja", r, new Vector3(0f, -1.5f, 0f), new Vector3(200f, 0.2f, 200f), M(ClaveMaterial.Agua));
        aguaAlta = Caja("Agua alta", r, new Vector3(-7f, -0.35f, 0f), new Vector3(14f, 0.2f, 5.6f), M(ClaveMaterial.Agua)).transform;
        Caja("Muelle norte", r, new Vector3(0f, -0.6f, 9f), new Vector3(30f, 1.2f, 12f), M(ClaveMaterial.PiedraBorde));
        Caja("Muelle sur", r, new Vector3(0f, -0.6f, -9f), new Vector3(30f, 1.2f, 12f), M(ClaveMaterial.PiedraBorde));
        Caja("Losa norte", r, new Vector3(0f, 0.02f, 9f), new Vector3(30f, 0.04f, 12f), M(ClaveMaterial.PiedraCrema));
        hojaA = Hoja(r, new Vector3(0f, 0f, 3f), 1f);
        hojaB = Hoja(r, new Vector3(0f, 0f, -3f), -1f);
        for (int i = 0; i < 2; i++)
            Cilindro("Llave", r, new Vector3(-1.2f + i * 2.4f, 0.4f, 3.8f), 0.3f, 0.8f, M(ClaveMaterial.Cobre));

        // Los cables: de la esclusa al cruce, y del cruce a la torre y al barrio.
        Cable(r, new Vector3(0f, 0.06f, 3.6f), Cruce, 10, cableComun);
        Cable(r, Cruce, Torre + new Vector3(0f, 0.06f, -1.2f), 16, cableTorre);
        Cable(r, Cruce, PuntoBarrio, 14, cableBarrio);
        Caja("Caja de cruce", r, Cruce + new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.8f, 0.9f), M(ClaveMaterial.Maqueta));
        Caja("Terreno", r, new Vector3(0f, -0.6f, 30f), new Vector3(70f, 1.2f, 30f), M(ClaveMaterial.PiedraBorde));

        // La torre de la emisora.
        matTorre = Brillo(new Color(0.3f, 0.32f, 0.38f), Color.black);
        Transform t = Raiz("Torre de la emisora", Torre);
        Caja("Caseta", t, new Vector3(0f, 1.5f, 0f), new Vector3(4f, 3f, 3f), M(ClaveMaterial.MaiaCarcasa));
        Caja("Rotulo", t, new Vector3(0f, 2.4f, -1.52f), new Vector3(2.6f, 0.5f, 0.05f), matTorre);
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? -0.9f : 0.9f, z = i < 2 ? -0.9f : 0.9f;
            Caja("Pata", t, new Vector3(x * (1f - 0f), 9f, z), new Vector3(0.14f, 12f, 0.14f), M(ClaveMaterial.Cobre));
        }
        for (int i = 0; i < 6; i++) Caja("Travesano", t, new Vector3(0f, 4f + i * 2f, -0.9f), new Vector3(1.9f, 0.1f, 0.1f), M(ClaveMaterial.Cobre));
        Caja("Mastil", t, new Vector3(0f, 16.5f, 0f), new Vector3(0.12f, 3f, 0.12f), M(ClaveMaterial.Cobre));
        matAntena = Brillo(RelevoUI.Verde, RelevoUI.Verde * 0.3f);
        Esfera("Antena", t, new Vector3(0f, 18.2f, 0f), 0.7f, matAntena);

        // El barrio, a oscuras.
        ventanas = Ciudad(Raiz("Barrio", new Vector3(30f, 0f, 78f)), 2, 11, out centrosBarrio);

        radio = Radio(transform, new Vector3(3.4f, 0f, 4.4f), 20f, out piloto, out _);
        nilo = CrearNilo(transform, PosNilo, 0f);
    }

    Transform Hoja(Transform padre, Vector3 bisagra, float lado)
    {
        var h = new GameObject("Hoja").transform;
        h.SetParent(padre, false);
        h.localPosition = bisagra;
        Caja("Tablero", h, new Vector3(0f, -0.6f, -lado * 1.5f), new Vector3(0.4f, 2f, 3f), M(ClaveMaterial.MaderaOscura));
        Caja("Herraje", h, new Vector3(0f, 0.35f, -lado * 1.5f), new Vector3(0.45f, 0.12f, 3f), M(ClaveMaterial.Cobre));
        return h;
    }

    void Cable(Transform padre, Vector3 a, Vector3 b, int tramos, List<Material> lista)
    {
        for (int i = 0; i < tramos; i++)
        {
            var m = Brillo(new Color(0.25f, 0.2f, 0.15f), Color.black);
            lista.Add(m);
            Vector3 p0 = Vector3.Lerp(a, b, i / (float)tramos), p1 = Vector3.Lerp(a, b, (i + 0.85f) / tramos);
            DioramaBuilder.CrearCajaEntre("Cable", padre, p0, p1, 0.12f, m);
        }
    }

    static void Corriente(List<Material> cable, float k, float fuerza = 1.6f)
    {
        for (int i = 0; i < cable.Count; i++)
        {
            float on = Mathf.Clamp01(k * cable.Count - i);
            cable[i].SetColor("_EmissionColor", RelevoUI.Ambar * fuerza * on);
        }
    }

    // ------------------------------------------------------------------ guion

    protected override void Evaluar(float previo, float t, float dt)
    {
        Base();
        agua.volume = 0.35f * Tramo(t, 0f, 3f);
        musica.volume = t < P_Eleccion ? 0.18f * Tramo(t, 0f, 3f) : 0.1f;
        musica.pitch = 0.9f;

        // Esclusa abierta desde el principio de la escena hacia delante.
        float abre = S(Tramo(t, 1.5f, 6.5f));
        hojaA.localRotation = Quaternion.Euler(0f, -80f * abre, 0f);
        hojaB.localRotation = Quaternion.Euler(0f, 80f * abre, 0f);
        aguaAlta.localPosition = new Vector3(-7f, Mathf.Lerp(-0.35f, -1.45f, S(Tramo(t, 3f, 8f))), 0f);
        Corriente(cableComun, Tramo(t, P_Cables, P_Cables + 3.5f));
        float ramal = Tramo(t, P_Cables + 3.5f, P_Cables + 8f);
        // Antes de elegir, los dos ramales llegan a medias: solo alcanza para uno.
        Corriente(cableTorre, eleccion == 1 ? 1f : ramal * 0.55f, eleccion == 0 ? 1.6f * (1f - Tramo(t, P_Resolucion, P_Resolucion + 2f)) : 1.6f);
        Corriente(cableBarrio, eleccion == 0 ? 1f : ramal * 0.55f, eleccion == 1 ? 1.6f * (1f - Tramo(t, P_Resolucion, P_Resolucion + 2f)) : 1.6f);

        // La antena parpadea, casi apagada; el piloto de la radio igual.
        float parpadeo = Mathf.PerlinNoise(t * 3f, 0.5f) > 0.45f ? 1f : 0.2f;
        matAntena.SetColor("_EmissionColor", RelevoUI.Verde * 0.5f * parpadeo);
        piloto.SetColor("_EmissionColor", RelevoUI.Verde * 1.4f * parpadeo);
        foreach (Material v in ventanas) EncenderBarrio(v, 0f);
        Poner(nilo, PosNilo, 0f);

        if (t < P_Cables) Esclusa(t);
        else if (t < P_Radio) Cables(t);
        else if (t < P_Mirar) RadioPlano(t);
        else if (t < P_Resolucion || eleccion < 0) Mirar(t);
        else Resolucion(previo, t);

        if (Cruza(previo, t, 1.5f)) Sonar(clipGolpe, 0.6f);
        if (Cruza(previo, t, P_Cables)) Sonar(clipArranque, 0.5f);
    }

    void Esclusa(float t)
    {
        Fundido(1f - Tramo(t, 0f, 1.5f));
        float u = Tramo(t, 0f, P_Cables);
        Colocar(new Vector3(-10f + u * 4f, 9f - u * 2f, -10f), new Vector3(0f, -0.5f, 0f), 40f);
        Rotulo("Esclusa de las Dos Llaves", Ventana(t, 1.5f, 7.5f));
        Pie("Las dos hojas se abren. La corriente vuelve a la orilla.", Ventana(t, 2.2f, 7.5f));
    }

    void Cables(float t)
    {
        // Grúa que sube por detrás del cruce hasta ver los dos destinos.
        float u = S(Tramo(t, P_Cables, P_Radio));
        Colocar(new Vector3(2f, 3f + u * 12f, 2f - u * 6f), Vector3.Lerp(Cruce, new Vector3(0f, 6f, 30f), u), 46f, 0.6f);
        Pie("En la torre de la emisora, la antena de Maia parpadea. Al otro lado, el barrio espera a oscuras.", Ventana(t, P_Cables + 4f, P_Radio - 0.2f));
        Fundido(Tramo(t, P_Radio - 0.4f, P_Radio));
    }

    void RadioPlano(float t)
    {
        Vector3 c = radio.position + new Vector3(0f, 0.5f, 0f);
        Colocar(c + new Vector3(-1.4f, 0.6f, -2.2f), c, 34f, 0.4f);
        Fundido(1f - Tramo(t, P_Radio, P_Radio + 0.8f));
        estatica.volume = 0.12f;
        if (!lineaDicha && t >= P_Linea)
        {
            lineaDicha = true;
            Maia("Nilo… mira. Las dos luces a la vez no pueden ser. Si me la das a mí, seguiré contigo un poco más.");
        }
    }

    void Mirar(float t)
    {
        // Nilo mira a la torre, luego al barrio, y vuelve al centro: la cámara a su altura.
        float u = t - P_Mirar;
        float giro = u < 2.5f ? Mathf.Lerp(0f, -40f, S(u / 2.5f))
            : u < 5.5f ? Mathf.Lerp(-40f, 45f, S((u - 2.5f) / 3f))
            : Mathf.Lerp(45f, 0f, S((u - 5.5f) / 2.5f));
        Poner(nilo, PosNilo, giro);
        Vector3 cabeza = PosNilo + new Vector3(0f, 1.6f, 0f);
        Colocar(cabeza + new Vector3(1.2f, 0.9f, 6f), cabeza + new Vector3(0f, -0.2f, 0f), 40f, 0.5f);
        Luna(1.1f, new Color(0.6f, 0.68f, 0.95f), new Vector3(35f, 200f, 0f));
        Fundido(1f - Tramo(t, P_Mirar, P_Mirar + 0.6f));
        if (t >= P_Eleccion)
            OfrecerEleccion(Historia.TituloDecision(1), "La energía solo alcanza para una de las dos.", Historia.OpcionesDecision(1));
    }

    void Resolucion(float previo, float t)
    {
        float a = P_Resolucion;
        float u = Tramo(t, a, FinTotal);
        Colocar(new Vector3(-2f + u * 3f, 14f - u * 2f, -6f), new Vector3(-3f, 6f, 32f), 58f, 0.6f);

        if (eleccion == 0)
        {
            // El barrio se enciende ventana a ventana; la antena se apaga con un chasquido.
            for (int i = 0; i < ventanas.Count; i++) EncenderBarrio(ventanas[i], Tramo(t, a + 2f + i * 1.2f, a + 3.5f + i * 1.2f));
            float apagada = Tramo(t, a + 4f, a + 4.2f);
            matAntena.SetColor("_EmissionColor", RelevoUI.Verde * 0.5f * (1f - apagada));
            piloto.SetColor("_EmissionColor", Color.black);
            if (Cruza(previo, t, a + 4f)) Sonar(clipRele, 0.8f);
            Cita("«Está bien. Es lo correcto.»", Ventana(t, a + 6f, FinTotal - 1.5f), RelevoUI.Verde);
            musica.volume = 0.25f;
        }
        else
        {
            // La antena se enciende en verde; el barrio sigue a oscuras.
            float on = Tramo(t, a + 2f, a + 4f);
            matAntena.SetColor("_EmissionColor", RelevoUI.Verde * (0.5f + 3f * on));
            matTorre.SetColor("_EmissionColor", RelevoUI.Verde * 1.2f * on);
            piloto.SetColor("_EmissionColor", RelevoUI.Verde * 2f);
            if (Cruza(previo, t, a + 2f)) Sonar(clipArranque, 0.7f);
            Cita("«Gracias, Nilo. No me olvides.»", Ventana(t, a + 6f, FinTotal - 1.5f), RelevoUI.Verde);
            estatica.volume = 0.05f;
        }
        Fundido(Tramo(t, FinTotal - 1.2f, FinTotal));
    }
}
