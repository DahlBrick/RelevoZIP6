using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fábrica de las piezas del Patio de Bombas: cajas empujables, placas de presión con su cable,
/// compuertas de barrotes y el puente manual que despliega una placa.
///
/// Igual que ActorFactory, construye geometría que se hornea con la escena y deja el estado de
/// ejecución en manos de Vincular().
/// </summary>
public static class ActorPatio
{
    // ------------------------------------------------------------------ caja

    public static CajaEmpujable CrearCaja(Transform padre, LevelLayout layout, Vector2Int casilla)
    {
        float t = layout.TamanoTile;
        GameObject raiz = new GameObject("Caja " + casilla.x + "," + casilla.y);
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = layout.CentroTile(casilla.x, casilla.y);

        // Colisión: casi una casilla, para que Nilo choque de frente con ella y la empuje.
        BoxCollider bc = raiz.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.75f, 0f);
        bc.size = new Vector3(t * 0.8f, 1.5f, t * 0.8f);

        Transform visual = DioramaBuilder.CrearAncla("Visual", raiz.transform, Vector3.zero);
        KitRelevo kit = KitRelevo.Activo;
        GameObject modelo = kit != null ? kit.caja : null;
        if (modelo != null)
        {
            Vector3 tam = KitRelevo.Tamano(modelo);
            float escala = t * 0.78f / Mathf.Max(0.01f, Mathf.Max(tam.x, tam.z));
            KitRelevo.ColocarCentrado(modelo, visual, "Modelo", Vector3.zero, 0f, Vector3.one * escala);
            // El modelo de Kenney es un cajón abierto: se tapa con tablones, que de cerca se nota.
            float alto = tam.y * escala;
            Material madera = RelevoPalette.MaterialDe(ClaveMaterial.MaderaOscura);
            for (int i = 0; i < 3; i++)
                DioramaBuilder.CrearCaja("Tapa", visual, new Vector3(0f, alto - 0.06f, (i - 1) * t * 0.24f),
                    new Vector3(t * 0.7f, 0.08f, t * 0.22f), madera, false);
        }
        else
        {
            DioramaBuilder.CrearCaja("Cuerpo", visual, new Vector3(0f, 0.7f, 0f), new Vector3(t * 0.78f, 1.4f, t * 0.78f),
                RelevoPalette.MaterialDe(ClaveMaterial.MaderaOscura), false);
        }

        // Flejes de cobre: la caja es pieza del puzle, no atrezo.
        Material cobre = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);
        DioramaBuilder.CrearCaja("Fleje", visual, new Vector3(0f, 0.35f, 0f), new Vector3(t * 0.8f, 0.1f, t * 0.8f), cobre, false);

        CajaEmpujable caja = raiz.AddComponent<CajaEmpujable>();
        caja.Configurar(casilla, visual);
        return caja;
    }

    // ------------------------------------------------------------------ compuerta

    public static Compuerta CrearCompuerta(Transform padre, LevelLayout layout, Vector2Int casilla)
    {
        float t = layout.TamanoTile;
        GameObject raiz = new GameObject("Compuerta " + casilla.x + "," + casilla.y);
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = layout.CentroTile(casilla.x, casilla.y);

        // Si a los lados hay agua, el paso es norte-sur y la compuerta se tiende de este a oeste.
        bool pasoNorteSur = !Pisable(layout, casilla.x - 1, casilla.y) && !Pisable(layout, casilla.x + 1, casilla.y);
        raiz.transform.localRotation = Quaternion.Euler(0f, pasoNorteSur ? 0f : 90f, 0f);

        Material cobre = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);
        Material turquesa = RelevoPalette.MaterialDe(ClaveMaterial.Turquesa);
        float medio = t * 0.5f - 0.12f;

        // Postes fijos a los lados.
        for (int lado = -1; lado <= 1; lado += 2)
        {
            DioramaBuilder.CrearCaja("Poste", raiz.transform, new Vector3(lado * medio, 0.75f, 0f), new Vector3(0.22f, 1.5f, 0.32f), cobre, false);
            DioramaBuilder.CrearCaja("Remate", raiz.transform, new Vector3(lado * medio, 1.55f, 0f), new Vector3(0.3f, 0.12f, 0.4f), turquesa, false);
        }

        // Barrotes que se hunden; quedan escondidos bajo el suelo al abrirse.
        Transform barrotes = DioramaBuilder.CrearAncla("Barrotes", raiz.transform, Vector3.zero);
        int n = 6;
        for (int i = 0; i < n; i++)
        {
            float x = Mathf.Lerp(-medio + 0.25f, medio - 0.25f, i / (float)(n - 1));
            DioramaBuilder.CrearCaja("Barrote", barrotes, new Vector3(x, 0.65f, 0f), new Vector3(0.09f, 1.3f, 0.09f), turquesa, false);
        }
        DioramaBuilder.CrearCaja("Travesaño", barrotes, new Vector3(0f, 1.22f, 0f), new Vector3(medio * 2f - 0.2f, 0.1f, 0.12f), cobre, false);
        DioramaBuilder.CrearCaja("Travesaño", barrotes, new Vector3(0f, 0.35f, 0f), new Vector3(medio * 2f - 0.2f, 0.1f, 0.12f), cobre, false);

        // Bloqueo: la casilla entera, para que no se pueda rodear por los bordes.
        GameObject bloqueo = new GameObject("Bloqueo");
        bloqueo.transform.SetParent(raiz.transform, false);
        BoxCollider bc = bloqueo.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 1f, 0f);
        bc.size = new Vector3(t, 2f, 0.7f);

        Compuerta c = raiz.AddComponent<Compuerta>();
        c.Configurar(casilla, barrotes, bc);
        return c;
    }

    static bool Pisable(LevelLayout layout, int c, int f)
    {
        return LevelLayout.EsTerrenoTransitable(LevelLayout.Mapear(layout.CharEn(c, f)));
    }

    // ------------------------------------------------------------------ placa

    /// <summary>
    /// Placa de presión con un cable en el suelo hasta lo que acciona ('destino', el centro de la
    /// casilla más cercana de la compuerta o del puente). El cable va en L: primero en X, luego en Z.
    /// </summary>
    public static PlacaPresion CrearPlaca(Transform padre, LevelLayout layout, Vector2Int casilla,
        Compuerta compuerta, TemporaryBridge puente, Vector3 destino)
    {
        float t = layout.TamanoTile;
        Vector3 centro = layout.CentroTile(casilla.x, casilla.y);
        GameObject raiz = new GameObject("Placa " + casilla.x + "," + casilla.y);
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = centro;

        KitRelevo kit = KitRelevo.Activo;
        Material marco = kit != null && kit.piedraMuro != null ? kit.piedraMuro : RelevoPalette.MaterialDe(ClaveMaterial.PiedraBorde);
        Material cobre = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);

        DioramaBuilder.CrearCaja("Marco", raiz.transform, new Vector3(0f, 0.04f, 0f), new Vector3(t * 0.82f, 0.08f, t * 0.82f), marco, false);
        Transform tapa = DioramaBuilder.CrearAncla("Tapa", raiz.transform, Vector3.zero);
        var encendidos = new List<Renderer>();
        encendidos.Add(DioramaBuilder.CrearCaja("Losa", tapa, new Vector3(0f, 0.12f, 0f), new Vector3(t * 0.62f, 0.08f, t * 0.62f), cobre, false).GetComponent<Renderer>());
        // Rombo central: la placa se reconoce de un vistazo y se enciende en turquesa al pisarla.
        GameObject rombo = DioramaBuilder.CrearCaja("Rombo", tapa, new Vector3(0f, 0.18f, 0f), new Vector3(t * 0.3f, 0.05f, t * 0.3f),
            RelevoPalette.MaterialDe(ClaveMaterial.Turquesa), false);
        rombo.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

        // Cable: tramos cortos y separados, a ras de suelo.
        Vector3 d = destino - centro;
        AnadirCable(raiz.transform, Vector3.zero, new Vector3(d.x, 0f, 0f), cobre, encendidos);
        AnadirCable(raiz.transform, new Vector3(d.x, 0f, 0f), new Vector3(d.x, 0f, d.z), cobre, encendidos);

        PlacaPresion placa = raiz.AddComponent<PlacaPresion>();
        placa.Configurar(casilla, tapa, encendidos.ToArray(), compuerta, puente);
        return placa;
    }

    static void AnadirCable(Transform padre, Vector3 desde, Vector3 hasta, Material material, List<Renderer> encendidos)
    {
        float largo = (hasta - desde).magnitude;
        if (largo < 0.5f) return;
        Vector3 dir = (hasta - desde) / largo;
        const float trazo = 0.34f, hueco = 0.2f;
        for (float s = 0.75f; s + trazo < largo - 0.4f; s += trazo + hueco)
        {
            Vector3 p = desde + dir * (s + trazo * 0.5f);
            GameObject g = DioramaBuilder.CrearCaja("Cable", padre, new Vector3(p.x, 0.03f, p.z),
                Mathf.Abs(dir.x) > 0.5f ? new Vector3(trazo, 0.04f, 0.1f) : new Vector3(0.1f, 0.04f, trazo), material, false);
            Renderer r = g.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            encendidos.Add(r);
        }
    }

    /// <summary>Centro de la casilla con 'marcador' más cercana a 'desde'.</summary>
    public static Vector3 CasillaMasCercana(LevelLayout layout, char marcador, Vector3 desde)
    {
        Vector3 mejor = desde;
        float mejorD = float.MaxValue;
        foreach (Vector3 c in layout.CentrosDeCasillas(marcador))
        {
            float d = (c - desde).sqrMagnitude;
            if (d < mejorD) { mejorD = d; mejor = c; }
        }
        return mejor;
    }

    /// <summary>
    /// El puente de una placa es un mecanismo manual: se pinta en turquesa, no en ámbar, para no
    /// confundirlo con los puentes de la batería.
    /// </summary>
    public static void PintarPuenteManual(TemporaryBridge puente)
    {
        if (puente == null) return;
        Material turquesa = RelevoPalette.MaterialDe(ClaveMaterial.Turquesa);
        Material ambar = RelevoPalette.MaterialDe(ClaveMaterial.Ambar);
        foreach (Renderer r in puente.GetComponentsInChildren<Renderer>(true))
        {
            Material[] ms = r.sharedMaterials;
            bool cambio = false;
            for (int i = 0; i < ms.Length; i++)
                if (ms[i] == ambar) { ms[i] = turquesa; cambio = true; }
            if (cambio) r.sharedMaterials = ms;
        }
    }
}
