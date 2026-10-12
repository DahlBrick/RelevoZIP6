using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Construye la maqueta a partir del mapa de texto: fusiona casillas contiguas del mismo
/// tipo en una sola caja (una corrida = un GameObject), de modo que un nivel de 13x10 no
/// genera cientos de objetos.
///
/// Importante para este juego: Nilo se mueve con la altura fija (sin gravedad), así que
/// un hueco sin colisión sería caminable. Por eso el agua y los vacíos llevan un volumen
/// sólido invisible más alto que la superficie caminable, y la maqueta se cierra con un
/// perímetro: es imposible salirse del nivel o caminar por encima del canal.
/// Las casillas de puente y pasarela solo reciben el agua visual: su bloqueo lo gestionan
/// TemporaryBridge y PermanentWalkway, que lo liberan únicamente al asentarse la plataforma.
/// </summary>
public static class DioramaBuilder
{
    public static Bounds Construir(LevelLayout layout, Transform raiz)
    {
        Transform suelo = Hijo("Suelo", raiz);
        Transform agua = Hijo("Agua", raiz);
        Transform bloqueos = Hijo("Bloqueos", raiz);
        Transform detalles = Hijo("Detalles", raiz);
        Transform fondo = Hijo("Fondo", raiz);

        Bounds limites = layout.Limites();
        float t = layout.TamanoTile;

        // Con kit visual, las cajas siguen siendo las mismas (son el suelo y las colisiones) pero
        // cambian de material, y encima se colocan los modelos (DioramaVestido).
        KitRelevo kit = KitRelevo.Activo;

        // Zócalo: refuerza la lectura de maqueta, tapa los laterales y esconde los
        // tramos retraídos de puentes y pasarelas.
        CrearCaja("Zocalo", raiz,
            new Vector3(limites.center.x, -1.7f, limites.center.z),
            new Vector3(limites.size.x + 3f, 1.6f, limites.size.z + 3f),
            kit != null && kit.piedraJunta != null ? kit.piedraJunta : RelevoPalette.MaterialDe(ClaveMaterial.Maqueta), false);

        for (int fila = 0; fila < layout.FilasCuenta; fila++)
        {
            int columna = 0;
            while (columna < layout.Columnas)
            {
                char caracter = layout.CharEn(columna, fila);
                TipoTile tipo = LevelLayout.Mapear(caracter);

                int fin = columna;
                while (fin + 1 < layout.Columnas && LevelLayout.Mapear(layout.CharEn(fin + 1, fila)) == tipo) fin++;

                int casillas = fin - columna + 1;
                float centroColumna = columna + (casillas - 1) * 0.5f;
                Vector3 centro = layout.CentroTile(centroColumna, fila);
                float ancho = casillas * t;

                if (LevelLayout.EsTerrenoTransitable(tipo))
                {
                    CrearCaja("Terreno", suelo,
                        new Vector3(centro.x, -0.5f, centro.z),
                        new Vector3(ancho, 1f, t),
                        MaterialTerreno(tipo, kit), true);

                    if (tipo == TipoTile.Vegetacion) AnadirVegetacion(detalles, centro, kit == null);
                }
                else if (tipo == TipoTile.Agua)
                {
                    CrearLosaAgua(agua, centro, ancho, t, kit);
                    CrearVolumen("Bloqueo agua", bloqueos,
                        new Vector3(centro.x, 0.6f, centro.z),
                        new Vector3(ancho, 3.2f, t));
                }
                else if (tipo == TipoTile.Vacio)
                {
                    CrearVolumen("Bloqueo vacio", bloqueos,
                        new Vector3(centro.x, 0.6f, centro.z),
                        new Vector3(ancho, 3.2f, t));
                }
                else
                {
                    // Puente temporal (B/C) o pasarela permanente (P/Q): canal visible
                    // bajo la plataforma, sin bloqueo permanente.
                    CrearLosaAgua(agua, centro, ancho, t, kit);
                }

                columna = fin + 1;
            }
        }

        AnadirPerimetro(limites, bloqueos);
        AnadirNorays(detalles, layout);
        AnadirDetallesDeTerreno(detalles, layout, kit == null);
        AnadirFondo(fondo, limites, kit);

        if (kit != null) DioramaVestido.Vestir(layout, raiz, detalles, fondo, limites, kit);

        return limites;
    }

    /// <summary>Superficie de agua visible, hundida 0,45 respecto al suelo caminable.</summary>
    static void CrearLosaAgua(Transform padreAgua, Vector3 centro, float ancho, float fondo, KitRelevo kit)
    {
        GameObject losa = CrearCaja("Agua", padreAgua,
            new Vector3(centro.x, -0.75f, centro.z),
            new Vector3(ancho, 0.6f, fondo),
            kit != null && kit.agua != null ? kit.agua : RelevoPalette.MaterialDe(ClaveMaterial.Agua), false);

        // El agua del kit es transparente y dibuja la sombra que recibe: no debe proyectar otra.
        if (kit != null) SinSombra(losa);
    }

    static Material MaterialTerreno(TipoTile tipo, KitRelevo kit)
    {
        if (kit != null)
        {
            if (tipo == TipoTile.Madera && kit.maderaMuelle != null) return kit.maderaMuelle;
            if (tipo != TipoTile.Cobre && kit.piedraMuro != null) return kit.piedraMuro;
        }

        switch (tipo)
        {
            case TipoTile.Madera: return RelevoPalette.MaterialDe(ClaveMaterial.MaderaOscura);
            case TipoTile.Cobre: return RelevoPalette.MaterialDe(ClaveMaterial.Cobre);
            default: return RelevoPalette.MaterialDe(ClaveMaterial.PiedraCrema);
        }
    }

    /// <summary>Vegetación y farol cálido sobre las casillas marcadas con 'v'.</summary>
    static void AnadirVegetacion(Transform padre, Vector3 centro, bool conCopa)
    {
        // Con kit visual el árbol lo pone DioramaVestido; aquí queda solo el farol.
        if (conCopa)
        {
            CrearCilindro("Tronco", padre, centro + new Vector3(0f, 0.3f, 0f), 0.1f, 0.6f, RelevoPalette.MaterialDe(ClaveMaterial.MaderaOscura));
            CrearEsfera("Copa A", padre, centro + new Vector3(0.08f, 0.85f, 0.05f), 0.7f, RelevoPalette.MaterialDe(ClaveMaterial.Vegetacion));
            CrearEsfera("Copa B", padre, centro + new Vector3(-0.24f, 0.68f, -0.18f), 0.48f, RelevoPalette.MaterialDe(ClaveMaterial.Vegetacion));
        }

        if (!FarolDelKit(padre, centro + new Vector3(0.55f, 0f, -0.55f)))
        {
            CrearCilindro("Poste farol", padre, centro + new Vector3(0.55f, 0.75f, -0.55f), 0.05f, 1.5f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
            CrearCaja("Luz farol", padre, centro + new Vector3(0.55f, 1.55f, -0.55f), new Vector3(0.22f, 0.22f, 0.22f), RelevoPalette.MaterialDe(ClaveMaterial.Ambar), false);
        }

        GameObject luz = new GameObject("Luz calida");
        luz.transform.SetParent(padre, false);
        luz.transform.localPosition = centro + new Vector3(0.55f, 1.6f, -0.55f);
        Light componente = luz.AddComponent<Light>();
        componente.type = LightType.Point;
        componente.color = new Color(1f, 0.78f, 0.5f);
        componente.range = 9f;
        componente.intensity = 1.4f;
        componente.shadows = LightShadows.None;

        if (!conCopa) luz.AddComponent<LuzTitilante>();
    }

    /// <summary>Norays de cobre en los extremos de los muelles, sin coste de colisión.</summary>
    static void AnadirNorays(Transform padre, LevelLayout layout)
    {
        var norays = layout.CentrosDeCasillas('Y');
        for (int i = 0; i < norays.Count; i++)
        {
            CrearCilindro("Noray", padre, norays[i] + new Vector3(-0.62f, 0.2f, 0.62f), 0.1f, 0.4f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
        }
    }

    // ------------------------------------------------------------------ detalle del terreno

    /// <summary>
    /// Acabado del terreno: labio de piedra en los cantos, juntas del entablado, bitas de amarre,
    /// macetas con sus matas y faroles sobre el muelle de madera. Se calcula sobre el mapa completo
    /// y no dentro del bucle de casillas, porque los cantos y las juntas abarcan varias filas a la
    /// vez. Nada de esto lleva collider.
    /// </summary>
    static void AnadirDetallesDeTerreno(Transform padre, LevelLayout layout, bool primitivo)
    {
        AnadirBordes(padre, layout);
        AnadirFarolesDeMadera(padre, layout);

        // Juntas, losas, manchas, travesaños y macetas dibujaban con cajas lo que con el kit visual
        // ya traen los adoquines, los tablones y las plantas: solo se construyen sin kit.
        if (!primitivo) return;

        AnadirJuntasDeMadera(padre, layout);
        AnadirLozasDePiedra(padre, layout);
        AnadirManchasDePiedra(padre, layout);
        AnadirTravesanosDeMadera(padre, layout);
        AnadirMacetas(padre, layout);
    }

    /// <summary>
    /// Lozas del pavimento: una junta en los cantos interiores de la piedra. Cada plataforma de
    /// piedra mide seis por seis unidades y sin esto se lee como una plancha lisa; las corridas
    /// seguidas se funden en una sola caja, así que las dos plataformas del nivel cuestan catorce
    /// cajas y no una por casilla. Van en 'PiedraBorde', un tono por debajo de 'PiedraCrema': se
    /// leen como junta sin dibujar una raya negra sobre una acera clara.
    /// </summary>
    static void AnadirLozasDePiedra(Transform padre, LevelLayout layout)
    {
        float t = layout.TamanoTile;
        Material junta = RelevoPalette.MaterialDe(ClaveMaterial.PiedraBorde);

        // Juntas verticales (a lo largo de Z), en los cantos entre columnas.
        for (int canto = 1; canto < layout.Columnas; canto++)
        {
            int inicio = -1;

            for (int fila = 0; fila <= layout.FilasCuenta; fila++)
            {
                bool piedra = fila < layout.FilasCuenta
                    && EsPiedra(layout, canto - 1, fila) && EsPiedra(layout, canto, fila);

                if (piedra)
                {
                    if (inicio < 0) inicio = fila;
                    continue;
                }

                if (inicio < 0) continue;

                float zUna = layout.CentroTile(0, inicio).z + t * 0.5f;
                float zOtra = layout.CentroTile(0, fila - 1).z - t * 0.5f;

                CrearCaja("Losa de piedra", padre,
                    new Vector3(canto * t, 0.02f, (zUna + zOtra) * 0.5f),
                    new Vector3(0.09f, 0.04f, Mathf.Abs(zUna - zOtra)),
                    junta, false);

                inicio = -1;
            }
        }

        // Juntas horizontales (a lo largo de X), en los cantos entre filas.
        for (int canto = 1; canto < layout.FilasCuenta; canto++)
        {
            int inicio = -1;

            for (int columna = 0; columna <= layout.Columnas; columna++)
            {
                bool piedra = columna < layout.Columnas
                    && EsPiedra(layout, columna, canto - 1) && EsPiedra(layout, columna, canto);

                if (piedra)
                {
                    if (inicio < 0) inicio = columna;
                    continue;
                }

                if (inicio < 0) continue;

                int casillas = columna - inicio;
                Vector3 centro = layout.CentroTile(inicio + (casillas - 1) * 0.5f, 0);

                CrearCaja("Losa de piedra", padre,
                    new Vector3(centro.x, 0.02f, -canto * t),
                    new Vector3(casillas * t, 0.04f, 0.09f),
                    junta, false);

                inicio = -1;
            }
        }
    }

    /// <summary>
    /// Desgaste de la piedra: manchas planas del tono del borde donde el ruido determinista lo
    /// pide, con el tamaño acotado para que nunca asomen por el canto de la casilla y se vean
    /// flotando sobre el agua. Quitan a las plataformas el aspecto de plancha recién pintada sin
    /// introducir colliders ni objetos que estorben desde el aire.
    /// </summary>
    static void AnadirManchasDePiedra(Transform padre, LevelLayout layout)
    {
        float t = layout.TamanoTile;
        Material mancha = RelevoPalette.MaterialDe(ClaveMaterial.PiedraBorde);

        for (int fila = 0; fila < layout.FilasCuenta; fila++)
        {
            for (int columna = 0; columna < layout.Columnas; columna++)
            {
                if (!EsPiedra(layout, columna, fila)) continue;
                if (Ruido(columna, fila, 5) <= 0.72f) continue;

                float ancho = t * (0.3f + Ruido(columna, fila, 6) * 0.24f);
                float fondo = t * (0.14f + Ruido(columna, fila, 7) * 0.2f);
                float desvioX = (Ruido(columna, fila, 8) - 0.5f) * t * 0.24f;
                float desvioZ = (Ruido(columna, fila, 9) - 0.5f) * t * 0.24f;

                Vector3 centro = layout.CentroTile(columna, fila);

                CrearCaja("Mancha de piedra", padre,
                    new Vector3(centro.x + desvioX, 0.011f, centro.z + desvioZ),
                    new Vector3(ancho, 0.022f, fondo),
                    mancha, false);
            }
        }
    }

    /// <summary>
    /// Travesaños del muelle de madera: la junta transversal que le falta al entablado. Hoy las
    /// únicas juntas de madera van en Z, así que el muelle se lee como una losa oscura lisa;
    /// estas van en X y cierran la cuadrícula.
    /// </summary>
    static void AnadirTravesanosDeMadera(Transform padre, LevelLayout layout)
    {
        float t = layout.TamanoTile;
        Material junta = RelevoPalette.MaterialDe(ClaveMaterial.Maqueta);

        for (int canto = 1; canto < layout.FilasCuenta; canto++)
        {
            int inicio = -1;

            for (int columna = 0; columna <= layout.Columnas; columna++)
            {
                bool madera = columna < layout.Columnas
                    && EsMadera(layout, columna, canto - 1) && EsMadera(layout, columna, canto);

                if (madera)
                {
                    if (inicio < 0) inicio = columna;
                    continue;
                }

                if (inicio < 0) continue;

                int casillas = columna - inicio;
                Vector3 centro = layout.CentroTile(inicio + (casillas - 1) * 0.5f, 0);

                CrearCaja("Travesano", padre,
                    new Vector3(centro.x, 0.015f, -canto * t),
                    new Vector3(casillas * t, 0.03f, 0.11f),
                    junta, false);

                inicio = -1;
            }
        }
    }

    /// <summary>
    /// Labio de piedra en los cantos que dan al agua o al vacío. La cámara mira desde el sur y sin
    /// giro sobre el eje vertical, así que los cantos de levante y poniente quedarían exactamente de
    /// canto: solo se construyen el norte y el sur, que son los dos que se ven.
    /// </summary>
    static void AnadirBordes(Transform padre, LevelLayout layout)
    {
        int bitas = 0;
        AnadirBordesDeCanto(padre, layout, -1, ref bitas);
        AnadirBordesDeCanto(padre, layout, 1, ref bitas);
    }

    static void AnadirBordesDeCanto(Transform padre, LevelLayout layout, int paso, ref int bitas)
    {
        float t = layout.TamanoTile;
        Material piedra = RelevoPalette.MaterialDe(ClaveMaterial.PiedraBorde);

        for (int fila = 0; fila < layout.FilasCuenta; fila++)
        {
            int columna = 0;
            while (columna < layout.Columnas)
            {
                if (!EsTransitable(layout, columna, fila) || !CantoExpuesto(layout, columna, fila, paso))
                {
                    columna++;
                    continue;
                }

                int fin = columna;
                while (fin + 1 < layout.Columnas
                    && EsTransitable(layout, fin + 1, fila)
                    && CantoExpuesto(layout, fin + 1, fila, paso)) fin++;

                int casillas = fin - columna + 1;
                float centroColumna = columna + (casillas - 1) * 0.5f;
                Vector3 centro = layout.CentroTile(centroColumna, fila);

                CrearCaja("Borde", padre,
                    new Vector3(centro.x, 0.07f, centro.z + paso * (t * 0.5f - 0.12f)),
                    new Vector3(casillas * t, 0.14f, 0.24f),
                    piedra, false);

                // Bitas en el canto que da al canal: es el único que un jugador ve de cerca al
                // bordear el agua, y es lo que convierte el canto en atraque de cobre.
                if (paso > 0)
                {
                    int cuantas = casillas >= 3 ? 2 : 1;
                    for (int b = 0; b < cuantas && bitas < 8; b++)
                    {
                        float avance = (b + 1f) / (cuantas + 1f);
                        Bita(padre, new Vector3(centro.x + (avance - 0.5f) * casillas * t,
                            centro.z + paso * (t * 0.5f - 0.3f)));
                        bitas++;
                    }
                }

                columna = fin + 1;
            }
        }
    }

    static void Bita(Transform padre, Vector3 centro)
    {
        Material cobre = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);
        CrearCilindro("Bita", padre, new Vector3(centro.x, 0.19f, centro.z), 0.12f, 0.38f, cobre);
        CrearEsfera("Bita cabeza", padre, new Vector3(centro.x, 0.42f, centro.z), 0.3f, cobre);
    }

    /// <summary>
    /// Juntas del entablado: una línea oscura en cada canto de columna que tenga madera a alguno de
    /// sus lados. El bloque de madera ocupa varias filas, así que se recorren los cantos de columna
    /// y se unen en vertical las filas seguidas que dan madera.
    /// </summary>
    static void AnadirJuntasDeMadera(Transform padre, LevelLayout layout)
    {
        float t = layout.TamanoTile;
        Material junta = RelevoPalette.MaterialDe(ClaveMaterial.Maqueta);

        for (int canto = 0; canto <= layout.Columnas; canto++)
        {
            int inicio = -1;

            for (int fila = 0; fila <= layout.FilasCuenta; fila++)
            {
                bool madera = fila < layout.FilasCuenta
                    && (EsMadera(layout, canto - 1, fila) || EsMadera(layout, canto, fila));

                if (madera)
                {
                    if (inicio < 0) inicio = fila;
                    continue;
                }

                if (inicio < 0) continue;

                float zArriba = layout.CentroTile(0, inicio).z + t * 0.5f;
                float zAbajo = layout.CentroTile(0, fila - 1).z - t * 0.5f;

                CrearCaja("Junta de madera", padre,
                    new Vector3(canto * t, 0.015f, (zArriba + zAbajo) * 0.5f),
                    new Vector3(0.11f, 0.03f, Mathf.Abs(zArriba - zAbajo)),
                    junta, false);

                inicio = -1;
            }
        }
    }

    /// <summary>
    /// Maceta de cobre con su mata y un matorral bajo junto a cada casilla de vegetación: es lo que
    /// hace que esas casillas lean como jardineras cuidadas y no como un árbol suelto.
    /// </summary>
    static void AnadirMacetas(Transform padre, LevelLayout layout)
    {
        var casillas = layout.Casillas('v');
        Material cobre = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);
        Material hoja = RelevoPalette.MaterialDe(ClaveMaterial.Vegetacion);

        for (int i = 0; i < casillas.Count; i++)
        {
            Vector3 centro = layout.CentroTile(casillas[i].x, casillas[i].y);
            float desvio = Ruido(casillas[i].x, casillas[i].y, 1) * 0.26f - 0.13f;
            Vector3 rincon = centro + new Vector3(-0.6f + desvio, 0f, 0.58f);

            CrearCilindro("Maceta", padre, rincon + new Vector3(0f, 0.14f, 0f), 0.17f, 0.28f, cobre);
            CrearEsfera("Mata", padre, rincon + new Vector3(0f, 0.46f, 0f), 0.36f, hoja);

            CrearEsfera("Matorral", padre, centro + new Vector3(0.62f, 0.2f, 0.6f), 0.4f, hoja);

            if (Ruido(casillas[i].x, casillas[i].y, 2) > 0.45f)
            {
                CrearEsfera("Matorral", padre, centro + new Vector3(-0.62f, 0.16f, -0.58f), 0.32f, hoja);
            }
        }
    }

    /// <summary>
    /// Faroles de cobre sobre el muelle de madera. No llevan componente de luz a propósito: el asset
    /// URP del proyecto permite cuatro luces adicionales por objeto y el nivel ya usa siete, así que
    /// el nivel de referencia ya está por encima. Brillan por emisión ámbar, que el post-proceso
    /// realza sin gastar cupo.
    /// </summary>
    static void AnadirFarolesDeMadera(Transform padre, LevelLayout layout)
    {
        AnadirFarol(padre, layout, 3, 7);
        AnadirFarol(padre, layout, 6, 9);
    }

    static void AnadirFarol(Transform padre, LevelLayout layout, int columna, int fila)
    {
        Vector3 centro = layout.CentroTile(columna, fila);

        if (FarolDelKit(padre, centro + new Vector3(0.62f, 0f, -0.62f))) return;

        CrearCilindro("Poste farol", padre, centro + new Vector3(0.62f, 0.85f, -0.62f),
            0.06f, 1.7f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));

        CrearCaja("Luz farol", padre, centro + new Vector3(0.62f, 1.8f, -0.62f),
            new Vector3(0.26f, 0.26f, 0.26f), RelevoPalette.MaterialDe(ClaveMaterial.Ambar), false);
    }

    /// <summary>
    /// Farol de Fantasy Town (Kenney) en lugar del poste y la caja ámbar. Devuelve false si el kit
    /// no lo trae, para que el llamador construya el farol de primitivas.
    /// </summary>
    static bool FarolDelKit(Transform padre, Vector3 base_)
    {
        KitRelevo kit = KitRelevo.Activo;
        if (kit == null || kit.farol == null) return false;

        GameObject farol = KitRelevo.ColocarCentrado(kit.farol, padre, "Farol", base_, 0f, Vector3.one);
        if (kit.piedraPueblo != null) KitRelevo.Tenir(farol, n => kit.piedraPueblo);
        return true;
    }

    static bool EsTransitable(LevelLayout layout, int columna, int fila)
    {
        return LevelLayout.EsTerrenoTransitable(LevelLayout.Mapear(layout.CharEn(columna, fila)));
    }

    static bool CantoExpuesto(LevelLayout layout, int columna, int fila, int paso)
    {
        return !EsTransitable(layout, columna, fila + paso);
    }

    static bool EsMadera(LevelLayout layout, int columna, int fila)
    {
        if (columna < 0 || columna >= layout.Columnas) return false;
        return LevelLayout.Mapear(layout.CharEn(columna, fila)) == TipoTile.Madera;
    }

    /// <summary>
    /// Piedra estricta, no 'terreno transitable': las casillas 'v' llevan vegetación y las de cobre
    /// son suelo de otra clase, así que no cuentan para las juntas del pavimento.
    /// </summary>
    static bool EsPiedra(LevelLayout layout, int columna, int fila)
    {
        if (columna < 0 || columna >= layout.Columnas) return false;
        if (fila < 0 || fila >= layout.FilasCuenta) return false;
        return LevelLayout.Mapear(layout.CharEn(columna, fila)) == TipoTile.Piedra;
    }

    /// <summary>
    /// Valor estable entre 0 y 1 a partir de unas coordenadas de casilla. Se usa en lugar de Random
    /// para que dos horneados del mismo mapa produzcan exactamente la misma escena y el archivo de
    /// la escena no cambie en cada pasada.
    /// </summary>
    static float Ruido(int columna, int fila, int sal)
    {
        int valor = (columna * 73856093) ^ (fila * 19349663) ^ (sal * 83492791);
        return ((valor & 0x7FFFFFFF) % 977) / 977f;
    }

    // ------------------------------------------------------------------ fondo ambiental

    /// <summary>
    /// Ambiente lejano: evita que el muelle quede rodeado de vacío. La cámara es ortográfica y
    /// mira inclinada 55°, así que fuera del zócalo solo se ve una franja estrecha a los lados; ahí
    /// es donde caen el mar, los dos muelles vecinos, sus pilotes y las boyas. Todo es geometría
    /// sin collider, fuera del recinto jugable y sin proyectar sombra, de modo que no puede alterar
    /// el paso de Nilo ni el cupo de cuatro luces adicionales por objeto del asset URP.
    /// </summary>
    static void AnadirFondo(Transform padre, Bounds limites, KitRelevo kit)
    {
        Vector3 centro = limites.center;

        // El mar se hunde 1,35 respecto al suelo caminable —tres veces el calado del canal
        // (0,45)—, así que el nivel lee como un muelle elevado y el zócalo asoma 0,45 sobre la
        // línea de agua, en vez de desaparecer bajo ella.
        GameObject mar = CrearCaja("Mar", padre,
            new Vector3(centro.x, -1.65f, centro.z),
            new Vector3(300f, 0.6f, 300f),
            kit != null && kit.aguaMar != null ? kit.aguaMar : RelevoPalette.MaterialDe(ClaveMaterial.Agua), false);
        SinSombra(mar);

        // El zócalo mide 3 unidades más que los límites en cada eje; desde su borde se deja una
        // franja de agua y luego el muelle vecino, para que se lea "otro muelle" y no una
        // prolongación del mismo.
        float bordeX = limites.size.x * 0.5f + 1.5f;
        float anchoMuelle = 6.3f;
        float separacion = 1.7f;
        float desplazamiento = bordeX + separacion + anchoMuelle * 0.5f;

        MuelleLejano(padre, centro.x - desplazamiento, centro.z, anchoMuelle, 1f, kit);
        MuelleLejano(padre, centro.x + desplazamiento, centro.z, anchoMuelle, -1f, kit);

        // Con kit, las boyas son las de Kenney y flotan (DioramaVestido).
        if (kit != null) return;

        float boya = bordeX + 0.9f;
        Boya(padre, new Vector3(centro.x - boya, -1.15f, centro.z - 2f));
        Boya(padre, new Vector3(centro.x - boya, -1.15f, centro.z + 9f));
        Boya(padre, new Vector3(centro.x + boya, -1.15f, centro.z - 8f));
        Boya(padre, new Vector3(centro.x + boya, -1.15f, centro.z + 5f));
    }

    /// <summary>
    /// Muelle vecino: plataforma de madera sobre pilotes de cobre y dos fanales en su borde
    /// interior, que es el único que el encuadre muestra. 'haciaElNivel' vale 1 para el muelle de
    /// poniente y -1 para el de levante.
    /// </summary>
    static void MuelleLejano(Transform padre, float x, float z, float ancho, float haciaElNivel, KitRelevo kit)
    {
        GameObject muelle = CrearCaja("Muelle vecino", padre,
            new Vector3(x, -0.95f, z),
            new Vector3(ancho, 0.5f, 30f),
            kit != null && kit.maderaMuelle != null ? kit.maderaMuelle : RelevoPalette.MaterialDe(ClaveMaterial.MaderaOscura), false);
        SinSombra(muelle);

        float medio = ancho * 0.5f - 0.25f;
        float interior = x + haciaElNivel * medio;
        float exterior = x - haciaElNivel * medio;

        for (int i = 0; i < 6; i++)
        {
            float zPilote = z - 12.5f + i * 5f;
            Pilote(padre, new Vector3(interior, -1.325f, zPilote));
            Pilote(padre, new Vector3(exterior, -1.325f, zPilote));
        }

        float xFanal = x + haciaElNivel * (ancho * 0.5f - 0.45f);
        Fanal(padre, new Vector3(xFanal, -0.72f, z - 6f));
        Fanal(padre, new Vector3(xFanal, -0.72f, z + 7f));
    }

    static void Pilote(Transform padre, Vector3 centro)
    {
        // Los pilotes nacen por debajo de la línea de agua (-1,35) para que se lean clavados.
        SinSombra(CrearCilindro("Pilote", padre, centro, 0.11f, 1.15f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre)));
    }

    /// <summary>Boya de balizamiento: una esfera ámbar flotando. Sin luz propia, para no gastar cupo.</summary>
    static void Boya(Transform padre, Vector3 centro)
    {
        SinSombra(CrearEsfera("Boya", padre, centro, 0.42f, RelevoPalette.MaterialDe(ClaveMaterial.Ambar)));
    }

    /// <summary>Fanal del muelle vecino: mismo recurso que el farol de la vegetación, poste y caja ámbar.</summary>
    static void Fanal(Transform padre, Vector3 basePoste)
    {
        SinSombra(CrearCilindro("Poste fanal", padre,
            new Vector3(basePoste.x, basePoste.y + 0.5f, basePoste.z), 0.05f, 1.1f,
            RelevoPalette.MaterialDe(ClaveMaterial.Cobre)));

        SinSombra(CrearCaja("Luz fanal", padre,
            new Vector3(basePoste.x, basePoste.y + 1.13f, basePoste.z),
            new Vector3(0.22f, 0.22f, 0.22f),
            RelevoPalette.MaterialDe(ClaveMaterial.Ambar), false));
    }

    /// <summary>
    /// El fondo está fuera del alcance de la luz direccional y a más de 20 unidades del muelle:
    /// proyectar sombra solo añadiría trabajo al mapa de sombras y riesgo de acné en una losa de
    /// 300 unidades. Recibir sombra sí se deja, para que el muelle se dibuje sobre el agua.
    /// </summary>
    static void SinSombra(GameObject go)
    {
        if (go == null) return;

        MeshRenderer render = go.GetComponent<MeshRenderer>();
        if (render == null) return;

        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>Muro invisible que cierra la maqueta: no se puede salir del nivel.</summary>
    static void AnadirPerimetro(Bounds limites, Transform padre)
    {
        float alto = 3.2f;
        float grosor = 1f;
        float mitadX = limites.size.x * 0.5f;
        float mitadZ = limites.size.z * 0.5f;

        CrearVolumen("Muro norte", padre,
            new Vector3(limites.center.x, 0.6f, limites.center.z + mitadZ + grosor * 0.5f),
            new Vector3(limites.size.x + grosor * 4f, alto, grosor));

        CrearVolumen("Muro sur", padre,
            new Vector3(limites.center.x, 0.6f, limites.center.z - mitadZ - grosor * 0.5f),
            new Vector3(limites.size.x + grosor * 4f, alto, grosor));

        CrearVolumen("Muro este", padre,
            new Vector3(limites.center.x + mitadX + grosor * 0.5f, 0.6f, limites.center.z),
            new Vector3(grosor, alto, limites.size.z + grosor * 4f));

        CrearVolumen("Muro oeste", padre,
            new Vector3(limites.center.x - mitadX - grosor * 0.5f, 0.6f, limites.center.z),
            new Vector3(grosor, alto, limites.size.z + grosor * 4f));
    }

    public static GameObject CrearCaja(string nombre, Transform padre, Vector3 centro, Vector3 tamano, Material material, bool conCollider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = centro;
        go.transform.localScale = tamano;

        MeshRenderer render = go.GetComponent<MeshRenderer>();
        if (render != null && material != null) render.sharedMaterial = material;

        if (!conCollider)
        {
            Destruir(go.GetComponent<Collider>());
        }

        return go;
    }

    public static GameObject CrearCilindro(string nombre, Transform padre, Vector3 centro, float radio, float altura, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = centro;
        // El cilindro primitivo mide 2 unidades de alto: media altura para obtener 'altura'.
        go.transform.localScale = new Vector3(radio * 2f, altura * 0.5f, radio * 2f);

        MeshRenderer render = go.GetComponent<MeshRenderer>();
        if (render != null && material != null) render.sharedMaterial = material;

        Destruir(go.GetComponent<Collider>());

        return go;
    }

    public static GameObject CrearEsfera(string nombre, Transform padre, Vector3 centro, float diametro, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = centro;
        go.transform.localScale = Vector3.one * diametro;

        MeshRenderer render = go.GetComponent<MeshRenderer>();
        if (render != null && material != null) render.sharedMaterial = material;

        Destruir(go.GetComponent<Collider>());

        return go;
    }

    static Mesh prismaHexagonal;

    /// <summary>
    /// Prisma hexagonal de caras planas, generado por código. Igual que las primitivas del motor,
    /// la malla se guarda a tamaño unidad y el tamaño se aplica con la escala del transform: así
    /// cualquier radio y altura reutilizan la misma malla en lugar de crear una por medida (y una
    /// malla por medida acabaría pisando el mismo asset al hornear, porque el nombre manda la ruta).
    ///
    /// El nombre empieza por 'RELEVO_' para que la herramienta de horneado sepa que es generada y
    /// tenga que convertirla en asset: una malla creada en memoria no sobrevive al guardado de una
    /// escena, igual que pasaba con los materiales.
    /// </summary>
    public static GameObject CrearPrismaHexagonal(string nombre, Transform padre, Vector3 centro,
        float radio, float altura, Material material)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = centro;
        // La malla unidad tiene radio 0,5 y altura 1, así que la escala reproduce la medida pedida.
        go.transform.localScale = new Vector3(radio * 2f, altura, radio * 2f);

        MeshFilter filtro = go.AddComponent<MeshFilter>();
        filtro.sharedMesh = MallaPrismaHexagonal();

        MeshRenderer render = go.AddComponent<MeshRenderer>();
        if (render != null && material != null) render.sharedMaterial = material;

        // Sin collider: ningún actor bloquea el paso salvo su bloqueo explícito.
        return go;
    }

    /// <summary>
    /// Mallado de un prisma de seis caras con sombreado plano: cada cara tiene sus propios
    /// vértices para que los cantos queden marcados, que es lo que da la lectura de tallado.
    /// Las normales de las caras laterales no tienen componente vertical, de modo que
    /// escalar la altura con el transform no las estropea.
    /// </summary>
    static Mesh MallaPrismaHexagonal()
    {
        if (prismaHexagonal != null) return prismaHexagonal;

        const int lados = 6;
        const float radio = 0.5f;
        const float altura = 1f;
        const float mitad = altura * 0.5f;
        float paso = Mathf.PI * 2f / lados;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normales = new List<Vector3>();
        List<int> triangulos = new List<int>();

        // Caras laterales. El orden de los vértices deja la normal mirando hacia fuera.
        for (int i = 0; i < lados; i++)
        {
            float a0 = i * paso;
            float a1 = (i + 1) * paso;

            Vector3 abajo0 = new Vector3(Mathf.Cos(a0) * radio, -mitad, Mathf.Sin(a0) * radio);
            Vector3 abajo1 = new Vector3(Mathf.Cos(a1) * radio, -mitad, Mathf.Sin(a1) * radio);
            Vector3 arriba0 = new Vector3(abajo0.x, mitad, abajo0.z);
            Vector3 arriba1 = new Vector3(abajo1.x, mitad, abajo1.z);

            Vector3 normal = new Vector3(abajo0.x + abajo1.x, 0f, abajo0.z + abajo1.z).normalized;

            int primero = vertices.Count;
            vertices.Add(abajo0);
            vertices.Add(arriba0);
            vertices.Add(arriba1);
            vertices.Add(abajo1);
            for (int n = 0; n < 4; n++) normales.Add(normal);

            triangulos.Add(primero); triangulos.Add(primero + 1); triangulos.Add(primero + 2);
            triangulos.Add(primero); triangulos.Add(primero + 2); triangulos.Add(primero + 3);
        }

        // Tapa superior.
        int centroArriba = vertices.Count;
        vertices.Add(new Vector3(0f, mitad, 0f));
        normales.Add(Vector3.up);
        for (int i = 0; i < lados; i++)
        {
            float a = i * paso;
            vertices.Add(new Vector3(Mathf.Cos(a) * radio, mitad, Mathf.Sin(a) * radio));
            normales.Add(Vector3.up);
        }
        for (int i = 0; i < lados; i++)
        {
            triangulos.Add(centroArriba);
            triangulos.Add(centroArriba + 1 + (i + 1) % lados);
            triangulos.Add(centroArriba + 1 + i);
        }

        // Tapa inferior, con el orden invertido para que mire hacia abajo.
        int centroAbajo = vertices.Count;
        vertices.Add(new Vector3(0f, -mitad, 0f));
        normales.Add(Vector3.down);
        for (int i = 0; i < lados; i++)
        {
            float a = i * paso;
            vertices.Add(new Vector3(Mathf.Cos(a) * radio, -mitad, Mathf.Sin(a) * radio));
            normales.Add(Vector3.down);
        }
        for (int i = 0; i < lados; i++)
        {
            triangulos.Add(centroAbajo);
            triangulos.Add(centroAbajo + 1 + i);
            triangulos.Add(centroAbajo + 1 + (i + 1) % lados);
        }

        Mesh malla = new Mesh();
        malla.name = "RELEVO_PrismaHexagonal";
        malla.SetVertices(vertices);
        malla.SetNormals(normales);
        malla.SetTriangles(triangulos, 0);
        malla.RecalculateBounds();

        prismaHexagonal = malla;
        return malla;
    }

    static Mesh plataformaDeTramo;

    /// <summary>
    /// Plataforma de un tramo de puente o pasarela: tablones de madera sobre una estructura que
    /// conserva el color de la energía. Va en una sola malla generada —y no en primitivas hijas—
    /// porque TemporaryBridge y PermanentWalkway escalan la plataforma en vertical mientras se
    /// despliega: cualquier pieza hija se aplastaría con ella, mientras que una sola malla se
    /// despliega entera. Además no añade ni un objeto ni un collider a la escena.
    ///
    /// Igual que el prisma hexagonal, la malla se guarda a tamaño unidad y la medida la pone la
    /// escala del transform, que es de 0,96 casillas por 0,32 de canto.
    /// </summary>
    public static GameObject CrearPlataforma(string nombre, Transform padre, Vector3 centro, Vector3 tamano, Material estructura)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = centro;
        go.transform.localScale = tamano;

        MeshFilter filtro = go.AddComponent<MeshFilter>();
        filtro.sharedMesh = MallaPlataforma();

        MeshRenderer render = go.AddComponent<MeshRenderer>();
        render.sharedMaterials = new Material[] { estructura, RelevoPalette.MaterialDe(ClaveMaterial.MaderaOscura) };

        // Misma caja que daba la primitiva anterior: una casilla por 0,32 de canto, con la cara
        // superior en la altura caminable (local y 0,16 sobre el centro, que está a -0,16).
        BoxCollider colision = go.AddComponent<BoxCollider>();
        colision.center = Vector3.zero;
        colision.size = Vector3.one;

        return go;
    }

    /// <summary>
    /// Mallado de la plataforma, en una caja unidad. El submesh 0 es la estructura (el canto
    /// inferior y los dos cabezales) y el 1 los seis tablones; la cara superior de los tablones
    /// queda exactamente a y = 0,5, que es donde estaba la cara superior de la losa anterior, así
    /// que la altura caminable no cambia. Entre tablón y tablón queda una junta de 0,032 en la que
    /// asoma la estructura, y ese es el único sitio donde se ve el color de la energía.
    /// </summary>
    static Mesh MallaPlataforma()
    {
        if (plataformaDeTramo != null) return plataformaDeTramo;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normales = new List<Vector3>();
        List<int> estructura = new List<int>();
        List<int> tablones = new List<int>();

        AnadirCaja(vertices, normales, estructura,
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.28f, 0.5f));
        AnadirCaja(vertices, normales, estructura,
            new Vector3(-0.5f, 0.28f, -0.5f), new Vector3(0.5f, 0.5f, -0.42f));
        AnadirCaja(vertices, normales, estructura,
            new Vector3(-0.5f, 0.28f, 0.42f), new Vector3(0.5f, 0.5f, 0.5f));

        // Los tablones atraviesan la plataforma en Z, que es perpendicular al sentido de la
        // marcha: las corridas de puente y pasarela avanzan en X, así que el jugador cruza
        // tablón a tablón.
        const int tablas = 6;
        const float junta = 0.032f;
        float ancho = (1f - junta * (tablas - 1)) / tablas;
        float x = -0.5f;

        for (int i = 0; i < tablas; i++)
        {
            AnadirCaja(vertices, normales, tablones,
                new Vector3(x, 0.28f, -0.42f), new Vector3(x + ancho, 0.5f, 0.42f));
            x += ancho + junta;
        }

        Mesh malla = new Mesh();
        malla.name = "RELEVO_PlataformaDeTramo";
        // Hay que declarar las dos submesh antes de rellenarlas: SetTriangles no crea la submesh 1
        // por sí solo, y sin esto los tablones se descartaban en silencio.
        malla.subMeshCount = 2;
        malla.SetVertices(vertices);
        malla.SetNormals(normales);
        malla.SetTriangles(estructura, 0);
        malla.SetTriangles(tablones, 1);
        malla.RecalculateBounds();

        plataformaDeTramo = malla;
        return malla;
    }

    static Mesh anilloEspuma;

    /// <summary>
    /// Anillo plano de espuma para las ondulaciones del agua. Antes la onda era un cilindro macizo,
    /// y un disco opaco de hasta tres unidades de diámetro sobre el canal se leía como un nenúfar
    /// gris y no como espuma; con un anillo el agua queda a la vista por el centro y lo único que se
    /// dibuja es el borde de la onda.
    ///
    /// Va en el plano XZ, con radio exterior 0,5 en la malla unidad y una banda del 22 % del radio,
    /// de modo que escalar el transform en X y Z da el radio pedido y en Y no hay que escalar nada.
    /// Lleva las dos caras: a 55° de inclinación la onda tiene que leerse mire por donde mire.
    /// </summary>
    public static Mesh MallaAnilloEspuma()
    {
        if (anilloEspuma != null) return anilloEspuma;

        const int lados = 40;
        const float radioExterior = 0.5f;
        const float radioInterior = radioExterior * 0.78f;
        float paso = Mathf.PI * 2f / lados;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normales = new List<Vector3>();
        List<int> triangulos = new List<int>();

        for (int i = 0; i < lados; i++)
        {
            float a0 = i * paso;
            float a1 = (i + 1) * paso;

            Vector3 fuera0 = new Vector3(Mathf.Cos(a0) * radioExterior, 0f, Mathf.Sin(a0) * radioExterior);
            Vector3 dentro0 = new Vector3(Mathf.Cos(a0) * radioInterior, 0f, Mathf.Sin(a0) * radioInterior);
            Vector3 fuera1 = new Vector3(Mathf.Cos(a1) * radioExterior, 0f, Mathf.Sin(a1) * radioExterior);
            Vector3 dentro1 = new Vector3(Mathf.Cos(a1) * radioInterior, 0f, Mathf.Sin(a1) * radioInterior);

            int haz = vertices.Count;
            vertices.Add(fuera0); vertices.Add(dentro0); vertices.Add(dentro1); vertices.Add(fuera1);
            for (int n = 0; n < 4; n++) normales.Add(Vector3.up);
            triangulos.Add(haz); triangulos.Add(haz + 1); triangulos.Add(haz + 2);
            triangulos.Add(haz); triangulos.Add(haz + 2); triangulos.Add(haz + 3);

            int reves = vertices.Count;
            vertices.Add(fuera0); vertices.Add(fuera1); vertices.Add(dentro1); vertices.Add(dentro0);
            for (int n = 0; n < 4; n++) normales.Add(Vector3.down);
            triangulos.Add(reves); triangulos.Add(reves + 1); triangulos.Add(reves + 2);
            triangulos.Add(reves); triangulos.Add(reves + 2); triangulos.Add(reves + 3);
        }

        Mesh malla = new Mesh();
        malla.name = "RELEVO_OndaEspuma";
        malla.SetVertices(vertices);
        malla.SetNormals(normales);
        malla.SetTriangles(triangulos, 0);
        malla.RecalculateBounds();

        anilloEspuma = malla;
        return malla;
    }

    /// <summary>
    /// Aro plano para los acentos emisivos de los actores: reutiliza la malla del anillo de espuma
    /// —un anillo horizontal, doble cara, con el radio exterior a 0,5 en la malla unidad— y le
    /// pone el material pedido. La cámara mira inclinada 55°, así que un aro horizontal se lee
    /// como un halo y es lo que da un punto brillante y legible a cada actor sin añadir collider
    /// ni un objeto más de los necesarios.
    ///
    /// El radio pedido es el radio EXTERIOR: la banda útil es el 22 % de él.
    /// </summary>
    public static GameObject CrearAro(string nombre, Transform padre, Vector3 centro, float radio, Material material)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = centro;
        go.transform.localScale = new Vector3(radio * 2f, 1f, radio * 2f);

        MeshFilter filtro = go.AddComponent<MeshFilter>();
        filtro.sharedMesh = MallaAnilloEspuma();

        MeshRenderer render = go.AddComponent<MeshRenderer>();
        if (render != null && material != null) render.sharedMaterial = material;

        // Un aro pegado al suelo proyectaría una sombra que lo delata como pieza suelta.
        if (render != null) render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return go;
    }

    /// <summary>
    /// Añade a la malla una caja de caras planas: cada cara lleva sus propios vértices para que
    /// los cantos queden marcados y no se promedien las normales.
    /// </summary>
    static void AnadirCaja(List<Vector3> vertices, List<Vector3> normales, List<int> triangulos, Vector3 min, Vector3 max)
    {
        float x0 = min.x, y0 = min.y, z0 = min.z;
        float x1 = max.x, y1 = max.y, z1 = max.z;

        AnadirCara(vertices, normales, triangulos, Vector3.right,
            new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x1, y0, z1));
        AnadirCara(vertices, normales, triangulos, Vector3.left,
            new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), new Vector3(x0, y0, z0));
        AnadirCara(vertices, normales, triangulos, Vector3.up,
            new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0));
        AnadirCara(vertices, normales, triangulos, Vector3.down,
            new Vector3(x0, y0, z1), new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y0, z1));
        AnadirCara(vertices, normales, triangulos, Vector3.forward,
            new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), new Vector3(x0, y0, z1));
        AnadirCara(vertices, normales, triangulos, Vector3.back,
            new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y0, z0));
    }

    /// <summary>
    /// Cuadrilátero con sus cuatro vértices en el orden en que (v1-v0)x(v2-v0) mira hacia fuera de
    /// la caja, que es lo que Unity necesita para darlo por cara frontal.
    /// </summary>
    static void AnadirCara(List<Vector3> vertices, List<Vector3> normales, List<int> triangulos, Vector3 normal,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int primero = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);

        for (int i = 0; i < 4; i++) normales.Add(normal);

        triangulos.Add(primero); triangulos.Add(primero + 1); triangulos.Add(primero + 2);
        triangulos.Add(primero); triangulos.Add(primero + 2); triangulos.Add(primero + 3);
    }

    /// <summary>Volumen invisible que bloquea el paso (agua, vacíos, perímetro).</summary>
    public static GameObject CrearVolumen(string nombre, Transform padre, Vector3 centro, Vector3 tamano)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = centro;
        go.transform.localScale = tamano;

        Destruir(go.GetComponent<MeshRenderer>());

        return go;
    }

    /// <summary>Caja orientada entre dos puntos (cables, barandillas, brazos mecánicos).</summary>
    public static GameObject CrearCajaEntre(string nombre, Transform padre, Vector3 desde, Vector3 hasta, float grosor, Material material)
    {
        Vector3 direccion = hasta - desde;
        float longitud = direccion.magnitude;
        GameObject go = CrearCaja(nombre, padre, Vector3.zero, new Vector3(grosor, grosor, Mathf.Max(0.01f, longitud)), material, false);
        go.transform.localPosition = desde + direccion * 0.5f;
        if (longitud > 0.001f) go.transform.localRotation = Quaternion.LookRotation(direccion);
        return go;
    }

    /// <summary>Objeto vacío auxiliar (anclas de aviso, pivotes, puntos de apoyo).</summary>
    public static Transform CrearAncla(string nombre, Transform padre, Vector3 posicionLocal)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicionLocal;
        return go.transform;
    }

    /// <summary>
    /// En el editor Destroy no es válido y dejaría colliders decorativos en la escena
    /// horneada, así que se elige el método correcto según el contexto.
    /// </summary>
    static void Destruir(Object objeto)
    {
        if (objeto == null) return;
        if (Application.isPlaying) Object.Destroy(objeto);
        else Object.DestroyImmediate(objeto);
    }

    static Transform Hijo(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        return go.transform;
    }
}
