using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Construye los actores del nivel con primitivas. Fase de vertical slice: lo importante
/// es que la lectura sea clara (quién es quién, qué está energizado) y que las medidas
/// encajen con la rejilla del mapa. Los prefabs definitivos llegan en la fase de estética.
/// Ningún actor lleva collider salvo su bloqueo explícito, para no atrapar a Nilo.
/// </summary>
public static class ActorFactory
{
    // ------------------------------------------------------------------ entorno

    /// <summary>
    /// Ambiente y niebla del diorama. El relleno lo hace el ambiente Trilight y no una segunda
    /// luz direccional a propósito: el asset URP del proyecto permite cuatro luces adicionales por
    /// objeto (Assets/Settings/PC_RPAsset.asset) y el nivel ya usa seis —la luz de la base y el
    /// farol cálido de cada casilla de vegetación—, así que otra luz desplazaría a las cálidas,
    /// que son las que dan vida a los rincones. El ambiente no consume ese cupo.
    /// </summary>
    public static void ConfigurarEntorno()
    {
        // El relleno sube respecto a la fase anterior: con el mar y los muelles vecinos ya
        // construidos, las caras sur (las únicas que la cámara ve de frente) quedaban demasiado
        // oscuras y el muelle se leía como un recorte plano sobre el agua.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.56f, 0.63f, 0.72f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.46f, 0.48f);
        RenderSettings.ambientGroundColor = new Color(0.26f, 0.22f, 0.18f);

        // La niebla va al color de 'Neblina' y no al de 'Fondo': 'Fondo' es el tono de la
        // maqueta y del zócalo, y con la densidad actual teñía de gris oscuro los muelles
        // vecinos. 'Neblina', más claro, deja que la distancia se lea como bruma marina.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.011f;
        RenderSettings.fogColor = RelevoPalette.ColorDe(ClaveMaterial.Neblina);

        // Con el kit visual, luz de tarde (GDD: la aventura transcurre durante una tarde): cielo
        // más claro y un rebote cálido desde el agua y la piedra, que es lo que hace que el
        // follaje y la piedra crema se vean bañados por el sol y no grises.
        if (KitRelevo.Activo != null)
        {
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.74f, 0.84f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.58f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.31f, 0.26f);
            // Niebla lineal y no exponencial: la cámara ortográfica está a unos 45 m del muelle, y
            // la exponencial lavaba un tercio del color de todo el nivel. La lineal empieza
            // detrás de la maqueta y solo funde el mar lejano con el cielo.
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 58f;
            RenderSettings.fogEndDistance = 120f;
            RenderSettings.fogColor = new Color(0.55f, 0.68f, 0.72f);
        }
    }

    public static Light CrearSol(Transform padre)
    {
        GameObject go = new GameObject("Sol");
        go.transform.SetParent(padre, false);
        go.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

        Light luz = go.AddComponent<Light>();
        luz.type = LightType.Directional;
        // Sol algo más cálido y más alto, con la sombra suavizada: las sombras duras de la fase
        // anterior dibujaban un borde negro sobre el agua que rompía la lectura de diorama.
        luz.color = new Color(1f, 0.93f, 0.82f);
        luz.intensity = 1.45f;
        luz.shadows = LightShadows.Soft;
        luz.shadowStrength = 0.7f;

        // Sol de media tarde: más bajo y más dorado, con sombras largas que dan volumen a los
        // árboles y a los muelles vistos desde la cámara inclinada.
        if (KitRelevo.Activo != null)
        {
            go.transform.rotation = Quaternion.Euler(38f, -38f, 0f);
            luz.color = new Color(1f, 0.87f, 0.7f);
            luz.intensity = 1.65f;
            luz.shadowStrength = 0.62f;
        }

        return luz;
    }

    // ------------------------------------------------------------------ Nilo

    public static PlayerController CrearNilo(Transform padre, Vector3 posicion, float giroY)
    {
        KitRelevo kit = KitRelevo.Activo;
        if (kit != null && kit.niloModelo != null) return CrearNiloModelo(padre, posicion, giroY, kit);

        GameObject raiz = new GameObject("Nilo");
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = posicion;
        raiz.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);

        CharacterController controlador = raiz.AddComponent<CharacterController>();
        controlador.height = 1.8f;
        controlador.radius = 0.34f;
        controlador.center = new Vector3(0f, 0.9f, 0f);
        controlador.skinWidth = 0.03f;
        controlador.stepOffset = 0.3f;
        controlador.slopeLimit = 45f;
        controlador.minMoveDistance = 0f;

        PlayerController jugador = raiz.AddComponent<PlayerController>();
        raiz.AddComponent<PlayerInteractor>();

        Transform cuerpo = DioramaBuilder.CrearAncla("Cuerpo", raiz.transform, Vector3.zero);

        // Contenedor visual: toda la geometría que se ve cuelga de aquí, con un 18 % más de escala,
        // para que Nilo pese lo mismo que una casilla del diorama y su silueta se lea desde la
        // altura de la cámara. Es un ancla VACÍA —no añade collider, ni renderer, ni luz— y deja
        // fuera 'ManoBateria', que tiene que seguir siendo hija directa de 'Cuerpo' porque
        // EngancharBateriaALaMano la resuelve por la ruta "Cuerpo/ManoBateria" y su posición marca
        // dónde se apoya la batería. El CharacterController no se toca: sigue en 1,8 de alto.
        Transform figura = DioramaBuilder.CrearAncla("Figura", cuerpo, Vector3.zero);
        figura.localScale = Vector3.one * 1.18f;

        // Sombra de contacto: la carcasa de Nilo (#EDE6DA) y la piedra crema del muelle (#E8DEC6)
        // son casi el mismo tono, así que sin una mancha bajo los pies la silueta se deshace contra
        // el suelo. Va fuera de 'Figura' para quedarse pegada al suelo y no proyecta sombra —esa ya
        // la pone el sol.
        GameObject huella = DioramaBuilder.CrearEsfera("Sombra de contacto", cuerpo,
            new Vector3(0f, 0.02f, 0f), 1f, RelevoPalette.MaterialDe(ClaveMaterial.Maqueta));
        huella.transform.localScale = new Vector3(0.88f, 0.03f, 0.88f);
        MeshRenderer renderHuella = huella.GetComponent<MeshRenderer>();
        if (renderHuella != null) renderHuella.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Material carcasa = RelevoPalette.MaterialDe(ClaveMaterial.NiloCarcasa);
        Material extremidades = RelevoPalette.MaterialDe(ClaveMaterial.NiloExtremidades);

        DioramaBuilder.CrearCilindro("Torso", figura, new Vector3(0f, 0.95f, 0f), 0.33f, 0.9f, carcasa);
        DioramaBuilder.CrearCaja("Pecho", figura, new Vector3(0f, 1.12f, 0.17f), new Vector3(0.46f, 0.34f, 0.22f), extremidades, false);

        DioramaBuilder.CrearEsfera("Cabeza", figura, new Vector3(0f, 1.68f, 0f), 0.62f, carcasa);
        DioramaBuilder.CrearCaja("Pantalla", figura, new Vector3(0f, 1.7f, 0.26f), new Vector3(0.32f, 0.18f, 0.06f), RelevoPalette.MaterialDe(ClaveMaterial.NiloPantalla), false);
        GameObject ojoIzquierdo = DioramaBuilder.CrearEsfera("Ojo izquierdo", figura, new Vector3(-0.09f, 1.7f, 0.31f), 0.1f, RelevoPalette.MaterialDe(ClaveMaterial.NiloOjos));
        GameObject ojoDerecho = DioramaBuilder.CrearEsfera("Ojo derecho", figura, new Vector3(0.09f, 1.7f, 0.31f), 0.1f, RelevoPalette.MaterialDe(ClaveMaterial.NiloOjos));

        DioramaBuilder.CrearCilindro("Bufanda", figura, new Vector3(0f, 1.34f, 0f), 0.34f, 0.16f, RelevoPalette.MaterialDe(ClaveMaterial.NiloBufanda));

        // Caderas y hombros: anclas VACÍAS en el eje de giro. Sin ellas, animar una pierna la
        // haría girar sobre su propio centro en vez de oscilar desde la articulación; con ellas
        // el vaivén sale de la cadera y del hombro como en un muñeco articulado. Las medidas
        // locales reproducen la pose de reposo al milímetro, así que parado la silueta es
        // exactamente la del Bloque 4B, y no añaden ni renderer ni collider.
        DioramaBuilder.CrearAncla("Cadera izquierda", figura, new Vector3(-0.16f, 0.52f, 0f));
        DioramaBuilder.CrearAncla("Cadera derecha", figura, new Vector3(0.16f, 0.52f, 0f));
        DioramaBuilder.CrearAncla("Hombro pivote izquierdo", figura, new Vector3(-0.4f, 1.28f, 0f));
        DioramaBuilder.CrearAncla("Hombro pivote derecho", figura, new Vector3(0.4f, 1.28f, 0f));

        CrearPiernaDeNilo(figura, -1f, extremidades);
        CrearPiernaDeNilo(figura, 1f, extremidades);
        CrearBrazoDeNilo(figura, -1f, extremidades);
        CrearBrazoDeNilo(figura, 1f, extremidades);

        // Detalle de acabado: solo geometría nueva, sin collider y sin tocar las piezas que ya
        // llevan nombre con significado (Cuerpo, ManoBateria). Es lo que hace que Nilo lea como
        // un robot con manos y antena en vez de como un cilindro con ojos.
        Material pantalla = RelevoPalette.MaterialDe(ClaveMaterial.NiloPantalla);
        Material cobre = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);

        DioramaBuilder.CrearEsfera("Hombro izquierdo", figura, new Vector3(-0.3f, 1.28f, 0f), 0.3f, carcasa);
        DioramaBuilder.CrearEsfera("Hombro derecho", figura, new Vector3(0.3f, 1.28f, 0f), 0.3f, carcasa);

        // Las manos ya no se crean aquí: cuelgan del hombro pivote de su lado, en CrearBrazoDeNilo.

        DioramaBuilder.CrearCaja("Pelvis", figura, new Vector3(0f, 0.52f, 0f), new Vector3(0.5f, 0.14f, 0.44f), extremidades, false);
        DioramaBuilder.CrearCaja("Respaldo", figura, new Vector3(0f, 1f, -0.33f), new Vector3(0.34f, 0.56f, 0.06f), pantalla, false);

        // La antena cuelga de un ancla en su base (2,16 − 0,18) para poder mecerse desde la
        // unión con la cabeza y no sobre su propio centro.
        Transform antenaBase = DioramaBuilder.CrearAncla("Antena base", figura, new Vector3(0f, 1.98f, 0f));
        DioramaBuilder.CrearCilindro("Antena", antenaBase, new Vector3(0f, 0.18f, 0f), 0.022f, 0.36f, cobre);
        GameObject antenaPunta = DioramaBuilder.CrearEsfera("Antena punta", antenaBase, new Vector3(0f, 0.41f, 0f), 0.09f, RelevoPalette.MaterialDe(ClaveMaterial.NiloOjos));

        // Acento del Bloque 5: un aro emisivo sobre la antena. Da a la silueta un punto brillante
        // en la cabeza —lo que engancha la mirada desde el otro extremo del muelle— y es el latido
        // que dice 'Nilo está vivo' sin tocar la pantalla ni los ojos, que son planos y se ven
        // poco a esta distancia.
        GameObject aroAntena = DioramaBuilder.CrearAro("Aro de antena", antenaBase,
            new Vector3(0f, 0.47f, 0f), 0.16f, RelevoPalette.MaterialDe(ClaveMaterial.NiloOjos));

        GameObject bufandaCola = DioramaBuilder.CrearCaja("Bufanda cola", figura,
            new Vector3(0.24f, 1.18f, -0.3f), new Vector3(0.14f, 0.44f, 0.09f),
            RelevoPalette.MaterialDe(ClaveMaterial.NiloBufanda), false);
        bufandaCola.transform.localRotation = Quaternion.Euler(14f, 0f, -18f);

        // Punto de apoyo de la batería cuando Nilo la lleva. Hijo directo de 'Cuerpo' a propósito: la
        // mano visual crece con 'Figura', pero este anclaje marca una posición funcional y no se
        // mueve.
        DioramaBuilder.CrearAncla("ManoBateria", cuerpo, new Vector3(-0.52f, 0.98f, 0.26f));

        // --- marcha e idle: solo describe el movimiento, el PlayerController sigue mandando.
        raiz.AddComponent<AnimadorNilo>();

        CrearAcento(cuerpo.gameObject, new List<Renderer>
        {
            RendererDe(ojoIzquierdo),
            RendererDe(ojoDerecho),
            RendererDe(antenaPunta),
            RendererDe(aroAntena)
        }, 2.6f, 0f, 0.5f, 1.7f, 0f);

        return jugador;
    }

    /// <summary>
    /// Nilo con el modelo del kit visual: el robot animado de Quaternius, pintado con la paleta de
    /// Nilo y con sus señas propias (bufanda naranja, antena con luz cian) colgadas de los huesos.
    ///
    /// Lo funcional es idéntico a la versión de primitivas: mismo CharacterController, mismos
    /// componentes de juego y el ancla 'ManoBateria'. La batería ahora va a la espalda, colgada del
    /// hueso del torso, para que el balanceo de los brazos al andar no la atraviese.
    /// </summary>
    static PlayerController CrearNiloModelo(Transform padre, Vector3 posicion, float giroY, KitRelevo kit)
    {
        GameObject raiz = new GameObject("Nilo");
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = posicion;
        raiz.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);

        CharacterController controlador = raiz.AddComponent<CharacterController>();
        controlador.height = 1.8f;
        controlador.radius = 0.34f;
        controlador.center = new Vector3(0f, 0.9f, 0f);
        controlador.skinWidth = 0.03f;
        controlador.stepOffset = 0.3f;
        controlador.slopeLimit = 45f;
        controlador.minMoveDistance = 0f;

        PlayerController jugador = raiz.AddComponent<PlayerController>();
        raiz.AddComponent<PlayerInteractor>();

        Transform cuerpo = DioramaBuilder.CrearAncla("Cuerpo", raiz.transform, Vector3.zero);
        Transform figura = DioramaBuilder.CrearAncla("Figura", cuerpo, Vector3.zero);

        GameObject modelo = KitRelevo.Colocar(kit.niloModelo, figura, Vector3.zero, 0f, kit.niloEscala);
        modelo.name = "Modelo";

        Animator animador = modelo.GetComponent<Animator>();
        if (animador == null) animador = modelo.AddComponent<Animator>();
        animador.runtimeAnimatorController = kit.niloAnimador;
        animador.applyRootMotion = false;
        animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Sombra de contacto, igual que en la versión de primitivas: separa la carcasa clara del
        // adoquinado crema.
        GameObject huella = DioramaBuilder.CrearEsfera("Sombra de contacto", cuerpo,
            new Vector3(0f, 0.02f, 0f), 1f, RelevoPalette.MaterialDe(ClaveMaterial.Maqueta));
        huella.transform.localScale = new Vector3(0.9f, 0.03f, 0.9f);
        MeshRenderer renderHuella = huella.GetComponent<MeshRenderer>();
        if (renderHuella != null) renderHuella.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Transform cuello = BuscarHueso(modelo.transform, "Neck");
        Transform cabeza = BuscarHueso(modelo.transform, "Head");
        Transform torso = BuscarHueso(modelo.transform, "Torso");

        // --- bufanda: aro al cuello y cola que ondea (la anima AnimadorNiloModelo).
        Material bufanda = RelevoPalette.MaterialDe(ClaveMaterial.NiloBufanda);
        Transform grupoBufanda = DioramaBuilder.CrearAncla("Bufanda", figura, new Vector3(0f, 1.07f, 0f));
        DioramaBuilder.CrearCilindro("Aro bufanda", grupoBufanda, Vector3.zero, 0.27f, 0.13f, bufanda);
        DioramaBuilder.CrearEsfera("Nudo bufanda", grupoBufanda, new Vector3(0.12f, -0.02f, -0.24f), 0.15f, bufanda);
        Transform cola = DioramaBuilder.CrearAncla("Bufanda cola", grupoBufanda, new Vector3(0.12f, -0.02f, -0.26f));
        DioramaBuilder.CrearCaja("Tira", cola, new Vector3(0f, -0.2f, -0.02f), new Vector3(0.13f, 0.4f, 0.05f), bufanda, false);
        if (cuello != null) grupoBufanda.SetParent(cuello, true);

        // --- antena con punta luminosa sobre la cabeza.
        Transform antena = DioramaBuilder.CrearAncla("Antena base", figura, new Vector3(0.12f, 1.8f, -0.06f));
        DioramaBuilder.CrearCilindro("Antena", antena, new Vector3(0f, 0.15f, 0f), 0.02f, 0.3f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
        GameObject antenaPunta = DioramaBuilder.CrearEsfera("Antena punta", antena, new Vector3(0f, 0.33f, 0f), 0.09f, RelevoPalette.MaterialDe(ClaveMaterial.NiloOjos));
        GameObject aroAntena = DioramaBuilder.CrearAro("Aro de antena", antena, new Vector3(0f, 0.38f, 0f), 0.14f, RelevoPalette.MaterialDe(ClaveMaterial.NiloOjos));
        if (cabeza != null) antena.SetParent(cabeza, true);

        // --- la batería va a la espalda, sobre el torso y algo reducida.
        Transform mano = DioramaBuilder.CrearAncla("ManoBateria", cuerpo, new Vector3(0f, 0.86f, -0.4f));
        mano.localScale = Vector3.one * 0.75f;
        if (torso != null) mano.SetParent(torso, true);

        AnimadorNiloModelo animacion = raiz.AddComponent<AnimadorNiloModelo>();
        animacion.Configurar(animador, cola, antena);

        CrearAcento(antena.gameObject, new List<Renderer>
        {
            RendererDe(antenaPunta),
            RendererDe(aroAntena)
        }, 2.6f, 0f, 0.5f, 1.8f, 0f);

        return jugador;
    }

    /// <summary>Busca un hueso por nombre en todo el esqueleto (el primero que aparezca).</summary>
    static Transform BuscarHueso(Transform raiz, string nombre)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == nombre) return t;
        }
        return null;
    }

    // ------------------------------------------------------------------ Maia

    public static MaiaTerminal CrearMaia(Transform padre, Vector3 posicion, LevelDefinition definicion)
    {
        GameObject raiz = new GameObject("Maia");
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = posicion;

        // Bloqueo: no se puede atravesar el terminal, pero sí hablar con él desde al lado.
        DioramaBuilder.CrearVolumen("Bloqueo", raiz.transform, new Vector3(0f, 0.6f, 0f), new Vector3(1.1f, 3.2f, 1.1f));

        Transform cuerpo = DioramaBuilder.CrearAncla("Cuerpo", raiz.transform, Vector3.zero);

        DioramaBuilder.CrearCilindro("Pedestal", cuerpo, new Vector3(0f, 0.22f, 0f), 0.6f, 0.44f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
        DioramaBuilder.CrearCaja("Torre", cuerpo, new Vector3(0f, 0.98f, 0f), new Vector3(0.78f, 0.9f, 0.5f), RelevoPalette.MaterialDe(ClaveMaterial.MaiaCarcasa), false);
        DioramaBuilder.CrearCaja("Pantalla", cuerpo, new Vector3(0f, 1.05f, 0.26f), new Vector3(0.58f, 0.4f, 0.06f), RelevoPalette.MaterialDe(ClaveMaterial.MaiaPantalla), false);
        GameObject ojoIzquierdo = DioramaBuilder.CrearEsfera("Ojo izquierdo", cuerpo, new Vector3(-0.14f, 1.05f, 0.3f), 0.11f, RelevoPalette.MaterialDe(ClaveMaterial.MaiaOjos));
        GameObject ojoDerecho = DioramaBuilder.CrearEsfera("Ojo derecho", cuerpo, new Vector3(0.14f, 1.05f, 0.3f), 0.11f, RelevoPalette.MaterialDe(ClaveMaterial.MaiaOjos));

        DioramaBuilder.CrearEsfera("Vegetacion A", cuerpo, new Vector3(0.46f, 1.2f, -0.2f), 0.5f, RelevoPalette.MaterialDe(ClaveMaterial.Vegetacion));
        DioramaBuilder.CrearEsfera("Vegetacion B", cuerpo, new Vector3(-0.5f, 0.9f, -0.26f), 0.38f, RelevoPalette.MaterialDe(ClaveMaterial.Vegetacion));

        // Detalle de acabado: un basamento que la asienta en el muelle, el cuello que hoy deja un
        // hueco entre el pedestal y la torre, una cornisa que remata la cabeza y una antena con
        // punta luminosa, que es lo que la emparenta visualmente con Nilo.
        Material maiaCarcasa = RelevoPalette.MaterialDe(ClaveMaterial.MaiaCarcasa);
        Material cobreMaia = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);

        DioramaBuilder.CrearCilindro("Basamento", cuerpo, new Vector3(0f, 0.06f, 0f), 0.74f, 0.12f, RelevoPalette.MaterialDe(ClaveMaterial.Maqueta));
        DioramaBuilder.CrearCaja("Cuello", cuerpo, new Vector3(0f, 0.485f, 0f), new Vector3(0.5f, 0.12f, 0.34f), cobreMaia, false);
        DioramaBuilder.CrearCaja("Cornisa", cuerpo, new Vector3(0f, 1.48f, 0f), new Vector3(0.9f, 0.1f, 0.62f), maiaCarcasa, false);

        DioramaBuilder.CrearCaja("Banda izquierda", cuerpo, new Vector3(-0.3f, 0.98f, 0.26f), new Vector3(0.07f, 0.86f, 0.07f), cobreMaia, false);
        DioramaBuilder.CrearCaja("Banda derecha", cuerpo, new Vector3(0.3f, 0.98f, 0.26f), new Vector3(0.07f, 0.86f, 0.07f), cobreMaia, false);

        // La antena cuelga de un ancla en su base (1,78 − 0,25) para mecerse desde el hombro de
        // la cornisa, no sobre su propio centro.
        Transform antenaBase = DioramaBuilder.CrearAncla("Antena base", cuerpo, new Vector3(0.3f, 1.53f, -0.12f));
        DioramaBuilder.CrearCilindro("Antena", antenaBase, new Vector3(0f, 0.25f, 0f), 0.025f, 0.5f, cobreMaia);
        GameObject antenaPunta = DioramaBuilder.CrearEsfera("Antena punta", antenaBase, new Vector3(0f, 0.53f, 0f), 0.1f, RelevoPalette.MaterialDe(ClaveMaterial.MaiaOjos));
        GameObject aroAntena = DioramaBuilder.CrearAro("Aro de antena", antenaBase,
            new Vector3(0f, 0.62f, 0f), 0.17f, RelevoPalette.MaterialDe(ClaveMaterial.MaiaOjos));

        // --- Bloque 5: brazos, halo y latido.
        //
        // Brazos con pinza, cortos y abiertos hacia la pantalla: es lo que le da silueta propia
        // frente a Nilo, que es redondo. Cuelgan de un ancla por hombro para que el gesto salga
        // de la articulación, y no llevan collider ni mueven el bloqueo del terminal.
        CrearBrazoDeMaia(cuerpo, -1f, cobreMaia, maiaCarcasa);
        CrearBrazoDeMaia(cuerpo, 1f, cobreMaia, maiaCarcasa);

        // Halo giratorio sobre el basamento: marca a Maia como el punto de habla del muelle
        // desde cualquier distancia. El aro va acompañado de aspas porque un anillo solo es
        // radialmente simétrico y giraría sin que se notara.
        Transform halo = DioramaBuilder.CrearAncla("Halo", cuerpo, new Vector3(0f, 0.2f, 0f));
        Material turquesa = RelevoPalette.MaterialDe(ClaveMaterial.Turquesa);
        GameObject aroHalo = DioramaBuilder.CrearAro("Aro", halo, Vector3.zero, 0.7f, turquesa);

        List<Renderer> acentosHalo = new List<Renderer> { RendererDe(aroHalo) };
        Sumar(acentosHalo, CrearAspas(halo, 3, 0.3f, 0.68f, 0.055f, turquesa, 0f));
        CrearAcento(halo.gameObject, acentosHalo, 3.4f, 0.2f, 0.45f, 1.6f, 20f);

        // El latido de la cara va aparte y con otro periodo, para que no vaya en cadencia con el
        // halo: Maia es una terminal con varios subsistemas, no un adorno que parpadea a coro.
        CrearAcento(cuerpo.gameObject, new List<Renderer>
        {
            RendererDe(ojoIzquierdo),
            RendererDe(ojoDerecho),
            RendererDe(antenaPunta),
            RendererDe(aroAntena)
        }, 2.2f, 0.6f, 0.5f, 1.5f, 0f);

        if (KitRelevo.Activo != null) ActorVestido.Maia(cuerpo, KitRelevo.Activo);

        raiz.AddComponent<AnimadorMaia>();

        MaiaTerminal maia = raiz.AddComponent<MaiaTerminal>();
        string pista = definicion != null ? definicion.pistaMaia : "";
        string cierre = definicion != null ? definicion.textoCierre : "";
        string intro = definicion != null ? definicion.textoIntro : "";
        maia.Configurar(intro, pista, cierre, cuerpo);

        return maia;
    }

    // ------------------------------------------------------------------ Batería

    public static BatteryController CrearBateria(Transform padre, Vector3 posicion, Transform raizActores)
    {
        GameObject raiz = new GameObject("Bateria");
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = posicion;

        Transform cuerpo = DioramaBuilder.CrearAncla("Cuerpo", raiz.transform, Vector3.zero);

        DioramaBuilder.CrearCaja("Celda", cuerpo, new Vector3(0f, 0.34f, 0f), new Vector3(0.42f, 0.68f, 0.42f), RelevoPalette.MaterialDe(ClaveMaterial.Ambar), false);
        DioramaBuilder.CrearCaja("Tapa", cuerpo, new Vector3(0f, 0.72f, 0f), new Vector3(0.46f, 0.08f, 0.46f), RelevoPalette.MaterialDe(ClaveMaterial.Cobre), false);
        DioramaBuilder.CrearCilindro("Terminal izquierdo", cuerpo, new Vector3(-0.12f, 0.82f, 0f), 0.05f, 0.14f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
        DioramaBuilder.CrearCilindro("Terminal derecho", cuerpo, new Vector3(0.12f, 0.82f, 0f), 0.05f, 0.14f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));

        // Detalle de acabado: la banda emisiva es la que deja leer de un vistazo que la celda
        // está cargada, y las guías de cobre la convierten en un objeto con jaula y no en un cubo.
        Material cobreBateria = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);
        DioramaBuilder.CrearCaja("Base", cuerpo, new Vector3(0f, 0.02f, 0f), new Vector3(0.46f, 0.04f, 0.46f), RelevoPalette.MaterialDe(ClaveMaterial.Maqueta), false);
        DioramaBuilder.CrearCaja("Banda", cuerpo, new Vector3(0f, 0.56f, 0f), new Vector3(0.46f, 0.07f, 0.46f), RelevoPalette.MaterialDe(ClaveMaterial.AmbarSuave), false);
        DioramaBuilder.CrearCaja("Guia izquierda", cuerpo, new Vector3(-0.23f, 0.3f, 0f), new Vector3(0.05f, 0.52f, 0.46f), cobreBateria, false);
        DioramaBuilder.CrearCaja("Guia derecha", cuerpo, new Vector3(0.23f, 0.3f, 0f), new Vector3(0.05f, 0.52f, 0.46f), cobreBateria, false);

        Transform anclaAviso = DioramaBuilder.CrearAncla("Ancla", cuerpo, new Vector3(0f, 0.72f, 0f));

        // Acento del Bloque 5: un aro de carga encajado en la tapa. Se inscribe en la caja de la
        // tapa (0,46 de lado) en vez de rodearla, que es lo que lo hace leer como una marca
        // luminosa del propio objeto y no como un aro flotando a su alrededor. Las aspas existen
        // para que su giro se note: un anillo liso es radialmente simétrico.
        Transform carga = DioramaBuilder.CrearAncla("Carga", cuerpo, new Vector3(0f, 0.765f, 0f));
        Material ambarBateria = RelevoPalette.MaterialDe(ClaveMaterial.Ambar);
        GameObject aroCarga = DioramaBuilder.CrearAro("Aro de carga", carga, Vector3.zero, 0.22f, ambarBateria);

        List<Renderer> acentosCarga = new List<Renderer> { RendererDe(aroCarga) };
        Sumar(acentosCarga, CrearAspas(carga, 3, 0.07f, 0.2f, 0.045f, ambarBateria, 60f));
        CrearAcento(carga.gameObject, acentosCarga, 1.5f, 0f, 0.4f, 1.9f, 40f);

        if (KitRelevo.Activo != null) ActorVestido.Bateria(cuerpo, KitRelevo.Activo);

        BatteryController bateria = raiz.AddComponent<BatteryController>();
        bateria.Inicializar(null, raizActores, anclaAviso);
        bateria.ColocarEnSuelo(posicion);

        return bateria;
    }

    /// <summary>Engancha la batería a la mano de Nilo (se llama cuando el jugador ya existe).</summary>
    public static void EngancharBateriaALaMano(BatteryController bateria, PlayerController jugador)
    {
        if (bateria == null || jugador == null) return;

        // Con el modelo del kit, 'ManoBateria' cuelga de un hueso y no de 'Cuerpo': se busca por
        // nombre en todo Nilo si la ruta directa no existe.
        Transform mano = jugador.transform.Find("Cuerpo/ManoBateria");
        if (mano == null) mano = BuscarHueso(jugador.transform, "ManoBateria");
        if (mano == null) mano = jugador.transform;

        bateria.Inicializar(mano, bateria.transform.parent, bateria.Ancla);
    }

    // ------------------------------------------------------------------ Base de energía

    public static EnergySocket CrearBaseEnergia(Transform padre, Vector3 posicion, string identificador,
        string etiqueta, TemporaryBridge puente, Vector3 haciaElPuente)
    {
        GameObject raiz = new GameObject("Base " + identificador);
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = posicion;

        Transform cuerpo = DioramaBuilder.CrearAncla("Cuerpo", raiz.transform, Vector3.zero);

        // La base es la pieza que el jugador lee de un vistazo, así que tiene silueta propia:
        // un prisma hexagonal tallado en dos escalones, frente a los cilindros y cajas del resto
        // del diorama. El escalón superior es más estrecho que el anillo a propósito, para que el
        // anillo de estado se lea como una franja encendida entre los dos niveles.
        Material cobreBase = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);
        DioramaBuilder.CrearPrismaHexagonal("Zocalo", cuerpo, new Vector3(0f, 0.17f, 0f), 0.78f, 0.34f, cobreBase);
        DioramaBuilder.CrearCilindro("Nucleo", cuerpo, new Vector3(0f, 0.46f, 0f), 0.24f, 0.26f, RelevoPalette.MaterialDe(ClaveMaterial.AmbarSuave));
        DioramaBuilder.CrearPrismaHexagonal("Zocalo alto", cuerpo, new Vector3(0f, 0.475f, 0f), 0.46f, 0.09f, cobreBase);

        GameObject anillo = DioramaBuilder.CrearCilindro("Anillo", cuerpo, new Vector3(0f, 0.38f, 0f), 0.6f, 0.1f, cobreBase);
        Transform anclaje = DioramaBuilder.CrearAncla("Anclaje", cuerpo, new Vector3(0f, 0.52f, 0f));
        Transform ancla = DioramaBuilder.CrearAncla("Ancla", cuerpo, new Vector3(0f, 0.4f, 0f));

        DioramaBuilder.CrearCilindro("Punta", cuerpo, new Vector3(0f, 0.605f, 0f), 0.09f, 0.05f, RelevoPalette.MaterialDe(ClaveMaterial.Ambar));

        // Pernos en los seis vértices del zócalo: es lo que convierte el prisma en pieza tallada.
        for (int i = 0; i < 6; i++)
        {
            float angulo = i * Mathf.PI / 3f;
            DioramaBuilder.CrearCilindro("Perno " + (i + 1), cuerpo,
                new Vector3(Mathf.Cos(angulo) * 0.62f, 0.36f, Mathf.Sin(angulo) * 0.62f),
                0.045f, 0.05f, cobreBase);
        }

        GameObject goLuz = new GameObject("Luz base");
        goLuz.transform.SetParent(cuerpo, false);
        goLuz.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        Light luz = goLuz.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = RelevoPalette.ColorDe(ClaveMaterial.Ambar);
        luz.range = 8f;
        luz.intensity = 0f;
        luz.shadows = LightShadows.None;

        // Cable visible hasta su puente: su material es el que informa del estado.
        GameObject cable = CableHasta(padre, posicion, haciaElPuente);

        // Acento del Bloque 5: un halo giratorio sobre el núcleo, por encima del escalón alto y
        // alrededor de la punta. La base es la pieza que más miradas recibe, así que lleva el
        // acento más brillante del nivel; va en geometría NUEVA y no en 'Anillo' ni en 'Cable',
        // que son los renderers que EnergySocket repinta con el material compartido al energizar.
        Transform halo = DioramaBuilder.CrearAncla("Halo", cuerpo, new Vector3(0f, 0.6f, 0f));
        Material ambarBase = RelevoPalette.MaterialDe(ClaveMaterial.Ambar);
        GameObject aroHalo = DioramaBuilder.CrearAro("Aro", halo, Vector3.zero, 0.54f, ambarBase);

        List<Renderer> acentosBase = new List<Renderer> { RendererDe(aroHalo) };
        Sumar(acentosBase, CrearAspas(halo, 3, 0.2f, 0.52f, 0.06f, ambarBase, 0f));
        CrearAcento(halo.gameObject, acentosBase, 2f, 0f, 0.5f, 2.2f, 26f);

        if (KitRelevo.Activo != null) ActorVestido.Base(cuerpo, KitRelevo.Activo);

        EnergySocket baseEnergia = raiz.AddComponent<EnergySocket>();
        baseEnergia.Configurar(identificador, etiqueta, puente, ancla, anclaje,
            anillo.GetComponent<Renderer>(), cable != null ? cable.GetComponent<Renderer>() : null, luz);

        return baseEnergia;
    }

    static GameObject CableHasta(Transform padre, Vector3 desde, Vector3 hasta)
    {
        Vector3 a = new Vector3(desde.x, 0.06f, desde.z);
        Vector3 b = new Vector3(hasta.x, 0.06f, hasta.z);

        Vector3 direccion = b - a;
        float longitud = direccion.magnitude;
        if (longitud < 0.3f) return null;

        direccion /= longitud;
        if (longitud < 2.4f)
        {
            a += direccion * 0.8f;
            b -= direccion * 0.7f;
        }
        else
        {
            a += direccion * 0.8f;
            b -= direccion * 0.9f;
        }

        return DioramaBuilder.CrearCajaEntre("Cable", padre, a, b, 0.08f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
    }

    // ------------------------------------------------------------------ Puente eléctrico

    public static TemporaryBridge CrearPuente(Transform padre, LevelLayout layout, char marcador, string identificador)
    {
        GameObject raiz = new GameObject("Puente " + identificador);
        raiz.transform.SetParent(padre, false);

        TemporaryBridge puente = raiz.AddComponent<TemporaryBridge>();
        puente.Identificador = identificador;

        Material material = RelevoPalette.MaterialDe(ClaveMaterial.Ambar);
        float t = layout.TamanoTile;
        int indice = 0;

        for (int fila = 0; fila < layout.FilasCuenta; fila++)
        {
            int columna = 0;
            while (columna < layout.Columnas)
            {
                if (layout.CharEn(columna, fila) != marcador)
                {
                    columna++;
                    continue;
                }

                int fin = columna;
                while (fin + 1 < layout.Columnas && layout.CharEn(fin + 1, fila) == marcador) fin++;

                for (int c = columna; c <= fin; c++)
                {
                    Plataforma(puente, raiz.transform, layout.CentroTile(c, fila), t, material, indice * 0.05f);
                    indice++;
                }

                columna = fin + 1;
            }
        }

        return puente;
    }

    // ------------------------------------------------------------------ Pasarela manual

    public static PermanentWalkway CrearPasarela(Transform padre, LevelLayout layout, char marcador, string identificador)
    {
        GameObject raiz = new GameObject("Pasarela " + identificador);
        raiz.transform.SetParent(padre, false);

        PermanentWalkway pasarela = raiz.AddComponent<PermanentWalkway>();
        pasarela.Identificador = identificador;

        Material material = RelevoPalette.MaterialDe(ClaveMaterial.Turquesa);
        float t = layout.TamanoTile;
        int indice = 0;

        for (int fila = 0; fila < layout.FilasCuenta; fila++)
        {
            int columna = 0;
            while (columna < layout.Columnas)
            {
                if (layout.CharEn(columna, fila) != marcador)
                {
                    columna++;
                    continue;
                }

                int fin = columna;
                while (fin + 1 < layout.Columnas && layout.CharEn(fin + 1, fila) == marcador) fin++;

                for (int c = columna; c <= fin; c++)
                {
                    Plataforma(pasarela, raiz.transform, layout.CentroTile(c, fila), t, material, indice * 0.06f);
                    indice++;
                }

                columna = fin + 1;
            }
        }

        return pasarela;
    }

    /// <summary>
    /// Crea la plataforma y su bloqueo para un tramo. El bloqueo es el que garantiza que
    /// un tramo retraído no sea un hueco caminable (Nilo no tiene gravedad).
    ///
    /// La plataforma y el bloqueo se piden a DioramaBuilder con las mismas medidas de siempre:
    /// una casilla por 0,32 de canto para la plataforma (cara superior en la altura caminable) y
    /// una casilla por 3,2 de alto para el bloqueo. Lo único que cambia es el aspecto de la
    /// plataforma, que ahora es una malla de tablones con la estructura de energía asomando por
    /// las juntas y por los cabezales.
    /// </summary>
    static GameObject CrearPlataformaDeTramo(Transform raiz, Vector3 centro, float t, Material material)
    {
        return DioramaBuilder.CrearPlataforma("Plataforma", raiz,
            new Vector3(centro.x, -0.16f, centro.z),
            new Vector3(t * 0.96f, 0.32f, t * 0.96f), material);
    }

    static GameObject CrearBloqueoDeTramo(Transform raiz, Vector3 centro, float t)
    {
        return DioramaBuilder.CrearVolumen("Bloqueo", raiz,
            new Vector3(centro.x, 0.6f, centro.z),
            new Vector3(t, 3.2f, t));
    }

    static void Plataforma(TemporaryBridge puente, Transform raiz, Vector3 centro, float t, Material material, float retardo)
    {
        GameObject plataforma = CrearPlataformaDeTramo(raiz, centro, t, material);
        GameObject bloqueo = CrearBloqueoDeTramo(raiz, centro, t);

        puente.RegistrarTramo(plataforma.transform, plataforma.GetComponent<BoxCollider>(),
            bloqueo.GetComponent<BoxCollider>(), retardo);
    }

    static void Plataforma(PermanentWalkway pasarela, Transform raiz, Vector3 centro, float t, Material material, float retardo)
    {
        GameObject plataforma = CrearPlataformaDeTramo(raiz, centro, t, material);
        GameObject bloqueo = CrearBloqueoDeTramo(raiz, centro, t);

        pasarela.RegistrarTramo(plataforma.transform, plataforma.GetComponent<BoxCollider>(),
            bloqueo.GetComponent<BoxCollider>(), retardo);
    }

    // ------------------------------------------------------------------ Palanca

    public static LeverInteractable CrearPalanca(Transform padre, Vector3 posicion, string identificador, PermanentWalkway destino)
    {
        GameObject raiz = new GameObject("Palanca " + identificador);
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = posicion;

        // El mecanismo se aparta del centro de la casilla para dejar libre el paso.
        Transform grupo = DioramaBuilder.CrearAncla("Grupo", raiz.transform, new Vector3(-0.6f, 0f, 0.58f));

        Material cobrePalanca = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);
        DioramaBuilder.CrearCaja("Placa", grupo, new Vector3(0f, 0.03f, 0f), new Vector3(0.62f, 0.06f, 0.62f), RelevoPalette.MaterialDe(ClaveMaterial.Maqueta), false);
        DioramaBuilder.CrearCaja("Zocalo", grupo, new Vector3(0f, 0.22f, 0f), new Vector3(0.42f, 0.44f, 0.42f), cobrePalanca, false);

        // Horquilla que guía la barra: da lectura mecánica al conjunto y ancla la mirada en el
        // punto donde el jugador tiene que pulsar.
        DioramaBuilder.CrearCaja("Guarda izquierda", grupo, new Vector3(-0.18f, 0.56f, 0f), new Vector3(0.06f, 0.26f, 0.3f), cobrePalanca, false);
        DioramaBuilder.CrearCaja("Guarda derecha", grupo, new Vector3(0.18f, 0.56f, 0f), new Vector3(0.06f, 0.26f, 0.3f), cobrePalanca, false);

        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0) ? -0.25f : 0.25f;
            float z = (i < 2) ? -0.25f : 0.25f;
            DioramaBuilder.CrearCilindro("Perno " + (i + 1), grupo, new Vector3(x, 0.07f, z), 0.035f, 0.05f, cobrePalanca);
        }

        Transform pivote = DioramaBuilder.CrearAncla("Pivote", grupo, new Vector3(0f, 0.44f, 0f));
        DioramaBuilder.CrearCaja("Barra", pivote, new Vector3(0f, 0.42f, 0f), new Vector3(0.09f, 0.84f, 0.09f), RelevoPalette.MaterialDe(ClaveMaterial.Coral), false);
        GameObject indicador = DioramaBuilder.CrearEsfera("Indicador", pivote, new Vector3(0f, 0.9f, 0f), 0.2f, RelevoPalette.MaterialDe(ClaveMaterial.Coral));

        Transform ancla = DioramaBuilder.CrearAncla("Ancla", raiz.transform, new Vector3(0f, 0.5f, 0f));

        // Acento del Bloque 5: un aro sobre la placa, en el coral del propio mecanismo. Se elige
        // coral y no turquesa a propósito: el turquesa del indicador está reservado para 'ya
        // accionada', y un aro de ese color apagaría la única señal de estado de la palanca.
        Transform aro = DioramaBuilder.CrearAncla("Aro", grupo, new Vector3(0f, 0.1f, 0f));
        Material coralAro = RelevoPalette.MaterialDe(ClaveMaterial.Coral);
        GameObject aroPlaca = DioramaBuilder.CrearAro("Aro de placa", aro, Vector3.zero, 0.3f, coralAro);

        List<Renderer> acentosPalanca = new List<Renderer> { RendererDe(aroPlaca) };
        Sumar(acentosPalanca, CrearAspas(aro, 2, 0.2f, 0.32f, 0.045f, coralAro, 90f));
        CrearAcento(aro.gameObject, acentosPalanca, 1.8f, 0.6f, 0.4f, 1.8f, 18f);

        if (KitRelevo.Activo != null) ActorVestido.Palanca(grupo, KitRelevo.Activo);

        LeverInteractable palanca = raiz.AddComponent<LeverInteractable>();
        palanca.Configurar(identificador, destino, pivote, ancla, indicador.GetComponent<Renderer>());

        return palanca;
    }

    // ------------------------------------------------------------------ Salida

    public static ExitInteractable CrearSalida(Transform padre, Vector3 posicion, string etiqueta)
    {
        GameObject raiz = new GameObject("Salida");
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = posicion;

        Transform cuerpo = DioramaBuilder.CrearAncla("Cuerpo", raiz.transform, Vector3.zero);

        Material cobre = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);
        DioramaBuilder.CrearCaja("Suelo", cuerpo, new Vector3(0f, 0.04f, 0f), new Vector3(1.9f, 0.08f, 1.9f), RelevoPalette.MaterialDe(ClaveMaterial.AmbarSuave), false);
        DioramaBuilder.CrearCaja("Poste izquierdo", cuerpo, new Vector3(-0.78f, 0.85f, 0f), new Vector3(0.2f, 1.7f, 0.2f), cobre, false);
        DioramaBuilder.CrearCaja("Poste derecho", cuerpo, new Vector3(0.78f, 0.85f, 0f), new Vector3(0.2f, 1.7f, 0.2f), cobre, false);
        DioramaBuilder.CrearCaja("Dintel", cuerpo, new Vector3(0f, 1.78f, 0f), new Vector3(1.9f, 0.22f, 0.24f), cobre, false);
        DioramaBuilder.CrearCaja("Umbral", cuerpo, new Vector3(0f, 1.1f, 0f), new Vector3(1.35f, 0.9f, 0.05f), RelevoPalette.MaterialDe(ClaveMaterial.Turquesa), false);

        // Remate y franja luminosa sobre el dintel: el pórtico es el destino del nivel, así que
        // se marca con una línea de luz que se lee desde el otro extremo del muelle.
        DioramaBuilder.CrearCaja("Remate", cuerpo, new Vector3(0f, 1.93f, 0f), new Vector3(2.06f, 0.09f, 0.34f), cobre, false);
        DioramaBuilder.CrearCaja("Tira de luz", cuerpo, new Vector3(0f, 1.84f, 0.14f), new Vector3(1.5f, 0.05f, 0.07f), RelevoPalette.MaterialDe(ClaveMaterial.Ambar), false);
        DioramaBuilder.CrearCaja("Marco izquierdo", cuerpo, new Vector3(-0.7f, 1.1f, 0f), new Vector3(0.07f, 0.9f, 0.09f), cobre, false);
        DioramaBuilder.CrearCaja("Marco derecho", cuerpo, new Vector3(0.7f, 1.1f, 0f), new Vector3(0.07f, 0.9f, 0.09f), cobre, false);
        DioramaBuilder.CrearCaja("Base poste izquierdo", cuerpo, new Vector3(-0.78f, 0.11f, 0f), new Vector3(0.32f, 0.1f, 0.32f), cobre, false);
        DioramaBuilder.CrearCaja("Base poste derecho", cuerpo, new Vector3(0.78f, 0.11f, 0f), new Vector3(0.32f, 0.1f, 0.32f), cobre, false);

        Transform ancla = DioramaBuilder.CrearAncla("Ancla", cuerpo, new Vector3(0f, 1f, 0f));

        // Acento del Bloque 5: un halo giratorio sobre la losa de entrada. El nivel se completa
        // aquí, y este aro es lo que hace que el pórtico se lea como destino desde el otro
        // extremo del muelle aunque el umbral turquesa quede de espaldas a la cámara.
        Transform halo = DioramaBuilder.CrearAncla("Halo", cuerpo, new Vector3(0f, 0.1f, 0f));
        Material ambarSalida = RelevoPalette.MaterialDe(ClaveMaterial.Ambar);
        GameObject aroHalo = DioramaBuilder.CrearAro("Aro", halo, Vector3.zero, 0.74f, ambarSalida);

        List<Renderer> acentosSalida = new List<Renderer> { RendererDe(aroHalo) };
        Sumar(acentosSalida, CrearAspas(halo, 2, 0.2f, 0.7f, 0.07f, ambarSalida, 90f));
        CrearAcento(halo.gameObject, acentosSalida, 2.8f, 0f, 0.55f, 2f, 14f);

        if (KitRelevo.Activo != null) ActorVestido.Salida(cuerpo, KitRelevo.Activo);

        ExitInteractable salida = raiz.AddComponent<ExitInteractable>();
        salida.Configurar(etiqueta, ancla);

        return salida;
    }

    // ------------------------------------------------------------------ Recuerdo

    public static MemoryInteractable CrearRecuerdo(Transform padre, Vector3 posicion, LevelDefinition definicion)
    {
        GameObject raiz = new GameObject("Recuerdo");
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = posicion;

        // El orbe flota y se mece (lo anima MemoryInteractable), así que el detalle se apoya en
        // el pedestal y no debajo del orbe: un soporte fijo se desencajaría con el vaivén.
        DioramaBuilder.CrearCilindro("Base", raiz.transform, new Vector3(0f, 0.025f, 0f), 0.3f, 0.05f, RelevoPalette.MaterialDe(ClaveMaterial.Maqueta));
        DioramaBuilder.CrearCilindro("Pedestal", raiz.transform, new Vector3(0f, 0.16f, 0f), 0.24f, 0.32f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
        DioramaBuilder.CrearCilindro("Aro", raiz.transform, new Vector3(0f, 0.345f, 0f), 0.34f, 0.03f, RelevoPalette.MaterialDe(ClaveMaterial.Turquesa));
        GameObject orbe = DioramaBuilder.CrearEsfera("Orbe", raiz.transform, new Vector3(0f, 0.8f, 0f), 0.36f, RelevoPalette.MaterialDe(ClaveMaterial.AmbarSuave));
        Transform ancla = DioramaBuilder.CrearAncla("Ancla", raiz.transform, new Vector3(0f, 0.8f, 0f));

        // Acento del Bloque 5: un halo fijo bajo el orbe y el latido del propio orbe. El acento va
        // en el orbe y no en un anillo aparte porque MemoryInteractable lo desactiva al recogerlo:
        // así el latido desaparece con él y no queda un aro girando alrededor de un pedestal vacío.
        // Ese script no le asigna nunca el material (solo lo mece y lo rota), así que la copia de
        // instancia sobrevive.
        DioramaBuilder.CrearAro("Halo", raiz.transform, new Vector3(0f, 0.4f, 0f), 0.44f, RelevoPalette.MaterialDe(ClaveMaterial.Turquesa));

        CrearAcento(orbe, new List<Renderer> { RendererDe(orbe) }, 3f, 0f, 0.6f, 1.8f, 0f);

        if (KitRelevo.Activo != null) ActorVestido.Recuerdo(raiz.transform, KitRelevo.Activo);

        MemoryInteractable recuerdo = raiz.AddComponent<MemoryInteractable>();
        string id = definicion != null ? definicion.recuerdoIdentificador : "nivel1_recuerdo";
        string titulo = definicion != null ? definicion.recuerdoTitulo : "Recuerdo de Liria";
        string texto = definicion != null ? definicion.recuerdoTexto : "";
        recuerdo.Configurar(id, titulo, texto, orbe.transform, ancla);

        return recuerdo;
    }

    // ------------------------------------------------------------------ Faro (salida del Nivel 3)

    /// <summary>
    /// Faro de Liria: la salida del último nivel. No es un umbral que se cruza sino una torre a la
    /// que se entrega la batería, así que su ancla va en una cuna de cobre a ras de suelo —donde la
    /// batería queda depositada al completar el nivel— y la torre se desplaza 0,42 hacia el norte
    /// de la casilla: el eje +Z es "arriba" en pantalla, de modo que la torre queda de espaldas a
    /// la cámara y deja la parte de la casilla que el jugador pisa libre para la cuna.
    ///
    /// Se construye con las mismas piezas que el resto del diorama y sin collider, sin luz y sin
    /// tocar ningún transform con nombre: el faro no bloquea el paso ni gasta cupo de luces.
    /// El aro del haz es su único acento emisivo, y existe para que el faro se lea como destino
    /// desde cualquier punto del canal.
    /// </summary>
    public static ExitInteractable CrearFaro(Transform padre, Vector3 posicion)
    {
        GameObject raiz = new GameObject("Faro");
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = posicion;

        Transform cuerpo = DioramaBuilder.CrearAncla("Cuerpo", raiz.transform, Vector3.zero);

        Material piedra = RelevoPalette.MaterialDe(ClaveMaterial.PiedraCrema);
        Material cobre = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);
        Material ambar = RelevoPalette.MaterialDe(ClaveMaterial.Ambar);

        const float torre = 0.42f;

        DioramaBuilder.CrearPrismaHexagonal("Zocalo", cuerpo, new Vector3(0f, 0.22f, torre), 1.35f, 0.44f, cobre);
        DioramaBuilder.CrearCilindro("Torre baja", cuerpo, new Vector3(0f, 1f, torre), 0.62f, 1.12f, piedra);
        DioramaBuilder.CrearCilindro("Anillo de torre", cuerpo, new Vector3(0f, 1.56f, torre), 0.73f, 0.14f, cobre);
        DioramaBuilder.CrearCilindro("Torre alta", cuerpo, new Vector3(0f, 2.18f, torre), 0.5f, 1.16f, piedra);
        DioramaBuilder.CrearPrismaHexagonal("Balcon", cuerpo, new Vector3(0f, 2.79f, torre), 0.72f, 0.16f, cobre);
        DioramaBuilder.CrearCilindro("Sala de luz", cuerpo, new Vector3(0f, 3.25f, torre), 0.42f, 0.78f, ambar);
        DioramaBuilder.CrearEsfera("Cupula", cuerpo, new Vector3(0f, 3.68f, torre), 0.86f, cobre);
        DioramaBuilder.CrearCilindro("Aguja", cuerpo, new Vector3(0f, 4.22f, torre), 0.05f, 0.62f, cobre);
        DioramaBuilder.CrearEsfera("Punta", cuerpo, new Vector3(0f, 4.58f, torre), 0.18f, ambar);

        // El aro va por fuera de la sala de luz, sobre el balcón, y las dos aspas existen para que
        // el giro se note: un anillo liso es radialmente simétrico y giraría sin que se viera.
        Transform halo = DioramaBuilder.CrearAncla("Halo", cuerpo, new Vector3(0f, 3.25f, torre));
        GameObject aroHalo = DioramaBuilder.CrearAro("Aro", halo, Vector3.zero, 0.88f, ambar);

        List<Renderer> acentosFaro = new List<Renderer> { RendererDe(aroHalo) };
        Sumar(acentosFaro, CrearAspas(halo, 2, 0.42f, 0.86f, 0.07f, ambar, 0f));
        CrearAcento(halo.gameObject, acentosFaro, 4.6f, 0f, 0.5f, 2.4f, 12f);

        // Cuna: el collar donde se entrega la batería. A ras de suelo y por delante de la torre,
        // en la mitad sur de la casilla. El plato se hunde del todo para que la batería entregada
        // quede asentada dentro del aro y no flotando sobre él.
        Transform cuna = DioramaBuilder.CrearAncla("Cuna", cuerpo, new Vector3(0f, 0f, -0.95f));
        DioramaBuilder.CrearCilindro("Plato de cuna", cuna, new Vector3(0f, 0.04f, 0f), 0.6f, 0.08f, cobre);
        DioramaBuilder.CrearAro("Aro de cuna", cuna, new Vector3(0f, 0.1f, 0f), 0.56f, cobre);

        ParticleSystem chispas = RelevoEfectos.CrearChispas(cuna, new Vector3(0f, 0.3f, 0f),
            RelevoPalette.ColorDe(ClaveMaterial.Ambar), 0.4f, 10);

        ExitInteractable salida = raiz.AddComponent<ExitInteractable>();
        salida.Configurar("el faro", cuna, true, chispas);

        return salida;
    }

    // ------------------------------------------------------------------ articulaciones y acentos

    /// <summary>
    /// Pierna de Nilo colgada de su cadera. Reproduce la pose de reposo anterior midiendo desde
    /// la cadera, que está a 0,52: muslo a 0,52 − 0,26 y pie a 0,52 − 0,47.
    ///
    /// 'lado' vale -1 para la izquierda y 1 para la derecha. Ojo con el género de los nombres, que
    /// es el de la escena del Bloque 4B: la cadera y la pierna son 'izquierda'/'derecha', y el pie
    /// es 'izquierdo'/'derecho'. Un desajuste aquí deja la cadera vacía —y a Nilo sin piernas— sin
    /// que el compilador diga nada.
    /// </summary>
    static void CrearPiernaDeNilo(Transform figura, float lado, Material material)
    {
        bool izquierda = lado < 0f;
        Transform cadera = figura.Find(izquierda ? "Cadera izquierda" : "Cadera derecha");
        if (cadera == null) return;

        string ladoM = izquierda ? "izquierdo" : "derecho";
        string ladoF = izquierda ? "izquierda" : "derecha";

        DioramaBuilder.CrearCilindro("Pierna " + ladoF, cadera, new Vector3(0f, -0.26f, 0f), 0.09f, 0.52f, material);
        DioramaBuilder.CrearCaja("Pie " + ladoM, cadera, new Vector3(0f, -0.47f, 0.07f), new Vector3(0.19f, 0.1f, 0.3f), material, false);
    }

    /// <summary>
    /// Brazo de Nilo colgado de su hombro pivote (1,28): brazo a 1,28 − 0,26 y mano a 1,28 − 0,62.
    /// El brazo es 'izquierdo' y la mano es 'izquierda', como en la escena del Bloque 4B.
    /// </summary>
    static void CrearBrazoDeNilo(Transform figura, float lado, Material material)
    {
        bool izquierda = lado < 0f;
        Transform hombro = figura.Find(izquierda ? "Hombro pivote izquierdo" : "Hombro pivote derecho");
        if (hombro == null) return;

        string ladoM = izquierda ? "izquierdo" : "derecho";
        string ladoF = izquierda ? "izquierda" : "derecha";

        DioramaBuilder.CrearCilindro("Brazo " + ladoM, hombro, new Vector3(0f, -0.26f, 0f), 0.07f, 0.62f, material);
        DioramaBuilder.CrearEsfera("Mano " + ladoF, hombro, new Vector3(0f, -0.62f, 0f), 0.17f, material);
    }

    /// <summary>
    /// Brazo de Maia: cuelga de un ancla en el hombro y se abre hacia la pantalla. La pinza va en
    /// el extremo del tramo, de modo que rotar el ancla mueve el brazo entero y no solo la mano.
    /// 'lado' vale -1 para el izquierdo y 1 para el derecho.
    /// </summary>
    static void CrearBrazoDeMaia(Transform cuerpo, float lado, Material cobre, Material carcasa)
    {
        string ladoM = lado < 0f ? "izquierdo" : "derecho";
        string ladoF = lado < 0f ? "izquierda" : "derecha";
        Transform hombro = DioramaBuilder.CrearAncla("Hombro " + ladoM, cuerpo,
            new Vector3(lado * 0.44f, 1.28f, 0f));

        Vector3 codo = new Vector3(0f, -0.34f, 0.06f);
        Vector3 puno = new Vector3(-lado * 0.1f, -0.6f, 0.36f);

        DioramaBuilder.CrearCajaEntre("Brazo " + ladoM, hombro, codo, puno, 0.1f, cobre);
        DioramaBuilder.CrearEsfera("Pinza " + ladoF, hombro, puno, 0.17f, carcasa);
    }

    /// <summary>
    /// Aspas planas repartidas alrededor de un ancla. Sirven para que el giro de un aro se note:
    /// un anillo liso es radialmente simétrico y giraría sin que se viera. Devuelve sus renderers
    /// para poder pasárselos al acento.
    /// </summary>
    static Renderer[] CrearAspas(Transform padre, int cuantas, float radioInterior, float radioExterior,
        float grosor, Material material, float desfaseGrados)
    {
        if (cuantas <= 0) return new Renderer[0];

        Renderer[] renderers = new Renderer[cuantas];

        for (int i = 0; i < cuantas; i++)
        {
            float angulo = (desfaseGrados + i * 360f / cuantas) * Mathf.Deg2Rad;
            Vector3 direccion = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo));

            GameObject aspa = DioramaBuilder.CrearCajaEntre("Aspa " + (i + 1), padre,
                direccion * radioInterior, direccion * radioExterior, grosor, material);

            renderers[i] = RendererDe(aspa);
        }

        return renderers;
    }

    static Renderer RendererDe(GameObject go)
    {
        return go != null ? go.GetComponent<Renderer>() : null;
    }

    static void Sumar(List<Renderer> lista, Renderer[] anadidos)
    {
        if (lista == null || anadidos == null) return;
        for (int i = 0; i < anadidos.Length; i++) lista.Add(anadidos[i]);
    }

    /// <summary>
    /// Cuelga un acento emisivo de un GameObject y le pasa los renderers que debe hacer latir.
    /// El acento crea sus copias de material en Awake, nunca durante el horneado: la escena
    /// guardada sigue apuntando a los materiales compartidos de la paleta y el latido es un
    /// efecto de partida.
    /// </summary>
    static AcentoEmisivo CrearAcento(GameObject huesped, List<Renderer> acentos,
        float periodo, float fase, float minimo, float maximo, float giro)
    {
        if (huesped == null) return null;

        List<Renderer> limpios = new List<Renderer>();
        for (int i = 0; i < acentos.Count; i++)
        {
            if (acentos[i] != null) limpios.Add(acentos[i]);
        }

        AcentoEmisivo acento = huesped.AddComponent<AcentoEmisivo>();
        acento.Configurar(limpios.ToArray(), periodo, fase, minimo, maximo, giro);
        return acento;
    }

    // ------------------------------------------------------------------ utilidades de mapa

    /// <summary>Centro de la primera casilla del marcador (origen del cable de una base).</summary>
    public static Vector3 CentroDePrimerTramo(LevelLayout layout, char marcador)
    {
        for (int fila = 0; fila < layout.FilasCuenta; fila++)
        {
            for (int columna = 0; columna < layout.Columnas; columna++)
            {
                if (layout.CharEn(columna, fila) == marcador) return layout.CentroTile(columna, fila);
            }
        }

        return Vector3.zero;
    }

    /// <summary>Posición de un marcador, con valor por defecto si el mapa no lo incluye.</summary>
    public static Vector3 PosicionMarcador(LevelLayout layout, char marcador, Vector3 porDefecto)
    {
        return layout.PrimerMarcador(marcador, porDefecto);
    }
}
