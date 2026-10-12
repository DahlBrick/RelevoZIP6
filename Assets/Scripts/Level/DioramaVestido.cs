using UnityEngine;

/// <summary>
/// Viste la maqueta con los modelos del kit visual (KitRelevo). Trabaja ENCIMA de lo que construye
/// DioramaBuilder: las cajas de terreno, los volúmenes de bloqueo y el perímetro siguen siendo los
/// mismos, así que por dónde camina Nilo no cambia ni un milímetro. Todo lo que se añade aquí es
/// decoración sin collider.
///
/// Igual que el resto del constructor, la variación sale de un ruido determinista por casilla y no
/// de Random: hornear dos veces el mismo mapa produce exactamente la misma escena.
/// </summary>
public static class DioramaVestido
{
    // Altura de la cara superior caminable y de la lámina de agua del canal.
    const float Suelo = 0f;
    const float AguaCanal = -0.45f;

    public static void Vestir(LevelLayout layout, Transform raiz, Transform detalles, Transform fondo, Bounds limites, KitRelevo kit)
    {
        Transform suelo = Hijo("Vestido suelo", detalles);
        Transform plantas = Hijo("Vestido plantas", detalles);
        Transform orillas = Hijo("Vestido orillas", detalles);
        Transform puerto = Hijo("Vestido puerto", detalles);

        for (int fila = 0; fila < layout.FilasCuenta; fila++)
        {
            for (int columna = 0; columna < layout.Columnas; columna++)
            {
                TipoTile tipo = LevelLayout.Mapear(layout.CharEn(columna, fila));
                Vector3 centro = layout.CentroTile(columna, fila);

                switch (tipo)
                {
                    case TipoTile.Piedra:
                    case TipoTile.Cobre:
                        Adoquines(suelo, kit, layout, columna, fila, centro);
                        break;
                    case TipoTile.Madera:
                        Tablones(suelo, kit, layout, columna, fila, centro);
                        break;
                    case TipoTile.Vegetacion:
                        Adoquines(suelo, kit, layout, columna, fila, centro);
                        Jardinera(plantas, kit, columna, fila, centro);
                        break;
                }

                if (LevelLayout.EsTerrenoTransitable(tipo))
                {
                    Orillas(orillas, kit, layout, columna, fila, centro);
                    Atrezo(puerto, kit, layout, columna, fila, centro);
                }
            }
        }

        Fondo(fondo, kit, limites);
        Particulas(fondo, kit, layout, limites);
    }

    // ------------------------------------------------------------------ partículas ambientales

    /// <summary>
    /// Polen que flota despacio sobre todo el canal y luciérnagas ámbar junto a los faroles de las
    /// jardineras. Son pocas y lentas a propósito: dan vida al aire sin competir con el brillo de la
    /// batería y de las bases, que es lo que el jugador tiene que leer.
    /// </summary>
    static void Particulas(Transform padre, KitRelevo kit, LevelLayout layout, Bounds limites)
    {
        if (kit.particula == null) return;

        ParticleSystem polen = Sistema("Polen", padre, kit.particula,
            new Vector3(limites.center.x, 1.4f, limites.center.z), new Color(1f, 0.97f, 0.86f, 0.75f));
        var forma = polen.shape;
        forma.shapeType = ParticleSystemShapeType.Box;
        forma.scale = new Vector3(limites.size.x, 2f, limites.size.z);
        var principal = polen.main;
        principal.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
        principal.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
        principal.maxParticles = 70;
        var emision = polen.emission;
        emision.rateOverTime = 6f;

        foreach (Vector2Int casilla in layout.Casillas('v'))
        {
            Vector3 farol = layout.CentroTile(casilla.x, casilla.y) + new Vector3(0.55f, 1.2f, -0.55f);
            ParticleSystem luciernagas = Sistema("Luciernagas", padre, kit.particula, farol, new Color(1f, 0.78f, 0.38f, 1f));
            var formaL = luciernagas.shape;
            formaL.shapeType = ParticleSystemShapeType.Sphere;
            formaL.radius = 0.9f;
            var principalL = luciernagas.main;
            principalL.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            principalL.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
            principalL.maxParticles = 10;
            var emisionL = luciernagas.emission;
            emisionL.rateOverTime = 2.2f;
        }
    }

    static ParticleSystem Sistema(string nombre, Transform padre, Material material, Vector3 posicion, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;

        ParticleSystem sistema = go.AddComponent<ParticleSystem>();
        var principal = sistema.main;
        principal.loop = true;
        principal.playOnAwake = true;
        principal.prewarm = true;
        principal.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
        principal.startColor = color;
        principal.gravityModifier = -0.005f;
        principal.simulationSpace = ParticleSystemSimulationSpace.World;

        // Deriva suave e irregular: es lo que distingue polen de chispas.
        var ruido = sistema.noise;
        ruido.enabled = true;
        ruido.strength = 0.25f;
        ruido.frequency = 0.35f;
        ruido.scrollSpeed = 0.15f;

        var colores = sistema.colorOverLifetime;
        colores.enabled = true;
        Gradient gradiente = new Gradient();
        gradiente.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        colores.color = new ParticleSystem.MinMaxGradient(gradiente);

        ParticleSystemRenderer render = go.GetComponent<ParticleSystemRenderer>();
        render.sharedMaterial = material;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;
        return sistema;
    }

    // ------------------------------------------------------------------ suelo

    /// <summary>
    /// Adoquinado de piedra crema sobre la caja de terreno. La caja queda debajo como mortero (se
    /// ve por las juntas) y como pared del muelle hacia el agua.
    /// </summary>
    static void Adoquines(Transform padre, KitRelevo kit, LevelLayout layout, int columna, int fila, Vector3 centro)
    {
        GameObject losa = KitRelevo.Elegir(kit.losasDePiedra, Ruido(columna, fila, 11));
        if (losa == null) return;

        float giro = Ruido(columna, fila, 12) > 0.5f ? 180f : 0f;
        float t = layout.TamanoTile;
        KitRelevo.Colocar(losa, padre, new Vector3(centro.x, Suelo - 0.085f, centro.z), giro, t * 0.485f);
    }

    /// <summary>Entablado del muelle de madera: un juego de tablones de Kenney por casilla.</summary>
    static void Tablones(Transform padre, KitRelevo kit, LevelLayout layout, int columna, int fila, Vector3 centro)
    {
        if (kit.tablones == null) return;

        // La tarima se escala a la casilla exacta a partir de su tamaño real, y su cara superior
        // queda a ras del suelo caminable.
        float t = layout.TamanoTile;
        Vector3 tamano = KitRelevo.Tamano(kit.tablones);
        float alto = 0.5f;
        GameObject go = KitRelevo.Colocar(kit.tablones, padre, centro, Ruido(columna, fila, 13) > 0.5f ? 180f : 0f, 1f);
        go.transform.localScale = new Vector3(t / Mathf.Max(0.01f, tamano.x), alto, t / Mathf.Max(0.01f, tamano.z));
        go.transform.localPosition = new Vector3(centro.x, Suelo + 0.02f - tamano.y * alto, centro.z);
    }

    // ------------------------------------------------------------------ vegetación

    /// <summary>
    /// Jardinera: un árbol, una mata y flores sobre las casillas 'v'. El farol cálido que ya
    /// construye DioramaBuilder se conserva, así que aquí solo va la vegetación.
    /// </summary>
    static void Jardinera(Transform padre, KitRelevo kit, int columna, int fila, Vector3 centro)
    {
        GameObject arbol = KitRelevo.Elegir(kit.arboles, Ruido(columna, fila, 21));
        KitRelevo.Colocar(arbol, padre, centro + new Vector3(-0.15f, Suelo, 0.2f),
            Ruido(columna, fila, 22) * 360f, 0.3f + Ruido(columna, fila, 23) * 0.06f);

        GameObject mata = KitRelevo.Elegir(kit.arbustos, Ruido(columna, fila, 24));
        KitRelevo.Colocar(mata, padre, centro + new Vector3(0.55f, Suelo, -0.45f),
            Ruido(columna, fila, 25) * 360f, 0.42f);

        GameObject flor = KitRelevo.Elegir(kit.flores, Ruido(columna, fila, 26));
        KitRelevo.Colocar(flor, padre, centro + new Vector3(-0.6f, Suelo, -0.55f),
            Ruido(columna, fila, 27) * 360f, 0.34f);

        GameObject hierba = KitRelevo.Elegir(kit.hierbas, Ruido(columna, fila, 28));
        KitRelevo.Colocar(hierba, padre, centro + new Vector3(0.62f, Suelo, 0.55f),
            Ruido(columna, fila, 29) * 360f, 0.4f);
    }

    // ------------------------------------------------------------------ orillas

    /// <summary>
    /// Rocas, guijarros y juncos en el agua junto a los cantos expuestos. Rompen la línea recta
    /// entre el muelle y el canal, y el shader de agua dibuja espuma alrededor de cada una.
    /// Van en el agua, fuera de la casilla caminable, y no llevan collider.
    /// </summary>
    static void Orillas(Transform padre, KitRelevo kit, LevelLayout layout, int columna, int fila, Vector3 centro)
    {
        float t = layout.TamanoTile;
        Vector2Int[] lados = { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };

        for (int i = 0; i < lados.Length; i++)
        {
            Vector2Int vecino = new Vector2Int(columna + lados[i].x, fila + lados[i].y);
            if (vecino.x < 0 || vecino.x >= layout.Columnas || vecino.y < 0 || vecino.y >= layout.FilasCuenta) continue;
            if (LevelLayout.Mapear(layout.CharEn(vecino.x, vecino.y)) != TipoTile.Agua) continue;

            int sal = 40 + i * 5;
            float azar = Ruido(columna, fila, sal);
            if (azar < 0.45f) continue;

            // Dirección del canto en el mundo: la fila crece hacia -Z.
            Vector3 hacia = new Vector3(lados[i].x, 0f, -lados[i].y);
            Vector3 lateral = new Vector3(hacia.z, 0f, -hacia.x);
            float deslizamiento = (Ruido(columna, fila, sal + 1) - 0.5f) * t * 0.7f;
            Vector3 punto = centro + hacia * (t * 0.5f + 0.18f) + lateral * deslizamiento;

            if (azar > 0.8f)
            {
                GameObject roca = KitRelevo.Elegir(kit.rocas, Ruido(columna, fila, sal + 2));
                KitRelevo.Colocar(roca, padre, new Vector3(punto.x, AguaCanal - 0.25f, punto.z),
                    Ruido(columna, fila, sal + 3) * 360f, 0.17f + Ruido(columna, fila, sal + 4) * 0.06f);
            }
            else
            {
                GameObject guijarro = KitRelevo.Elegir(kit.guijarros, Ruido(columna, fila, sal + 2));
                KitRelevo.Colocar(guijarro, padre, new Vector3(punto.x, AguaCanal - 0.08f, punto.z),
                    Ruido(columna, fila, sal + 3) * 360f, 0.5f);
            }
        }
    }

    // ------------------------------------------------------------------ atrezo del puerto

    /// <summary>
    /// Barriles y cajas en algunas esquinas de muelle que dan al agua. Solo en casillas sin
    /// marcador (ninguna pieza del puzle comparte casilla con el atrezo) y pegados al canto, para
    /// no tapar el recorrido.
    /// </summary>
    static void Atrezo(Transform padre, KitRelevo kit, LevelLayout layout, int columna, int fila, Vector3 centro)
    {
        char c = layout.CharEn(columna, fila);
        bool libre = c == '#' || c == 'm' || c == 'c';
        if (!libre) return;
        if (Ruido(columna, fila, 61) < 0.86f) return;

        int aguaAlrededor = 0;
        if (EsAgua(layout, columna, fila - 1)) aguaAlrededor++;
        if (EsAgua(layout, columna, fila + 1)) aguaAlrededor++;
        if (EsAgua(layout, columna - 1, fila)) aguaAlrededor++;
        if (EsAgua(layout, columna + 1, fila)) aguaAlrededor++;
        if (aguaAlrededor == 0) return;

        float sx = Ruido(columna, fila, 62) > 0.5f ? 1f : -1f;
        float sz = Ruido(columna, fila, 63) > 0.5f ? 1f : -1f;
        Vector3 esquina = centro + new Vector3(sx * 0.68f, Suelo, sz * 0.68f);

        GameObject modelo = Ruido(columna, fila, 64) > 0.5f ? kit.barril : kit.caja;
        KitRelevo.Colocar(modelo, padre, esquina, Ruido(columna, fila, 65) * 360f, 0.38f);
    }

    // ------------------------------------------------------------------ fondo

    /// <summary>
    /// Vida alrededor de la maqueta: barcas amarradas que se mecen, boyas, gaviotas y un par de
    /// árboles en los muelles vecinos. Todo fuera del recinto jugable.
    /// </summary>
    static void Fondo(Transform padre, KitRelevo kit, Bounds limites)
    {
        Transform barcas = Hijo("Barcas", padre);
        Vector3 c = limites.center;
        float bordeX = limites.size.x * 0.5f + 1.5f;
        float mar = -1.35f;

        if (kit.botes != null && kit.botes.Length > 0)
        {
            Barca(barcas, kit.botes[0], new Vector3(c.x - bordeX - 0.9f, mar, c.z + 3.5f), 85f, 0.55f);
            Barca(barcas, kit.botes[1 % kit.botes.Length], new Vector3(c.x + bordeX + 0.9f, mar, c.z - 4f), -95f, 0.5f);
            Barca(barcas, kit.botes[2 % kit.botes.Length], new Vector3(c.x + bordeX + 1f, mar, c.z + 6.5f), -80f, 0.45f);
            Barca(barcas, kit.botes[3 % kit.botes.Length], new Vector3(c.x - bordeX - 1.2f, mar, c.z - 6f), 100f, 0.42f);
        }

        if (kit.boya != null)
        {
            float boya = bordeX + 0.9f;
            Boya(barcas, kit.boya, new Vector3(c.x - boya, mar, c.z - 2f));
            Boya(barcas, kit.boya, new Vector3(c.x + boya, mar, c.z + 2.5f));
        }

        GameObject gaviotas = new GameObject("Gaviotas");
        gaviotas.transform.SetParent(padre, false);
        gaviotas.transform.localPosition = new Vector3(c.x, 0f, c.z);
        gaviotas.AddComponent<Gaviotas>().Configurar(4, 6.5f, new Vector2(limites.size.x * 0.5f, limites.size.z * 0.45f));
    }

    static void Barca(Transform padre, GameObject modelo, Vector3 posicion, float giro, float escala)
    {
        GameObject go = KitRelevo.Colocar(modelo, padre, posicion, giro, escala);
        if (go == null) return;
        go.AddComponent<Flotador>().Configurar(0.05f, 3.4f, 2.5f, 4f, 3f);
    }

    static void Boya(Transform padre, GameObject modelo, Vector3 posicion)
    {
        GameObject go = KitRelevo.Colocar(modelo, padre, posicion, 0f, 0.45f, false);
        if (go == null) return;
        go.AddComponent<Flotador>().Configurar(0.07f, 2.6f, 6f, 7f, 0f);
    }

    // ------------------------------------------------------------------ utilidades

    static bool EsAgua(LevelLayout layout, int columna, int fila)
    {
        if (columna < 0 || columna >= layout.Columnas || fila < 0 || fila >= layout.FilasCuenta) return false;
        return LevelLayout.Mapear(layout.CharEn(columna, fila)) == TipoTile.Agua;
    }

    static float Ruido(int columna, int fila, int sal)
    {
        int valor = (columna * 73856093) ^ (fila * 19349663) ^ (sal * 83492791);
        return ((valor & 0x7FFFFFFF) % 977) / 977f;
    }

    static Transform Hijo(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        return go.transform;
    }
}
