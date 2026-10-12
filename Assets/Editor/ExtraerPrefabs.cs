using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Extrae prefabs desde la escena horneada, sin tocar la escena ni el runtime.
///
/// Esta herramienta NO cambia la fuente de verdad: ActorFactory sigue construyendo los actores
/// y LevelBootstrap.Enlazar() sigue enlazando el contexto de ejecución. Los prefabs son una
/// instantánea reutilizable (plantillas para las escenas 2 y 3 y para autoría en el editor).
///
/// Se usa PrefabUtility.SaveAsPrefabAsset y nunca la variante *AndConnect: guardar y conectar
/// sustituiría los objetos de la escena por instancias de prefab, y en una escena horneada eso
/// convertiría cada actor en una instancia que el siguiente horneado borraría y reconstruiría.
/// Guardar sin conectar deja la escena exactamente como está y solo añade assets.
///
/// Límite conocido y deliberado: un asset prefab no puede referenciar objetos que estén fuera de
/// su propia jerarquía. EnergySocket.Puente apunta a 'Actores/Puente B' y EnergySocket.cable a
/// 'Actores/Cable', que son hermanos, así que en esos prefabs quedan a null. Lo mismo ocurre con
/// LeverInteractable.Destino y con la mano de la batería. La herramienta informa de todas esas
/// referencias para cada prefab, de modo que se sepa exactamente qué hay que cablear en escena.
///
/// Los prefabs son una foto del horneado: si se vuelve a hornear el nivel, hay que volver a
/// extraerlos para que no queden desfasados.
/// </summary>
public static class ExtraerPrefabs
{
    const string CarpetaActores = "Assets/Prefabs/Actores";
    const string CarpetaModulos = "Assets/Prefabs/Modulos";
    const string NombreTemporal = "__ExtraccionPrefabs";

    /// <summary>Nombres del plan de Fase 4 donde no coinciden con el nombre del objeto de escena.</summary>
    static readonly Dictionary<string, string> nombreDePrefab = new Dictionary<string, string>
    {
        { "Base A",     "BaseEnergia" },
        { "Puente B",   "PuenteTemporal" },
        { "Pasarela P", "PasarelaPermanente" },
        { "Palanca L",  "Palanca" },
    };

    [MenuItem("Relevo/Extraer prefabs de la escena horneada")]
    public static void Extraer() { Extraer(true); }

    /// <summary>
    /// interactivo = false evita los diálogos modales, que bloquearían el editor cuando la
    /// herramienta se invoca por script en lugar de desde el menú.
    /// </summary>
    public static void Extraer(bool interactivo)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Fallo("Sal de Play Mode antes de extraer prefabs: los prefabs deben salir de la "
                + "escena tal como está guardada, no de una partida en curso.", interactivo);
            return;
        }

        Scene escena = SceneManager.GetActiveScene();
        GameObject raizActores = RaizEnEscena(escena, "Actores");
        GameObject raizNivel = RaizEnEscena(escena, "Nivel");

        if (raizActores == null || raizNivel == null)
        {
            Fallo("La escena '" + escena.name + "' no tiene las raíces 'Actores' y 'Nivel'.\n\n"
                + "Ábrela y hornéala primero con 'Relevo/Hornear nivel de la escena activa'.", interactivo);
            return;
        }

        AsegurarCarpeta(CarpetaActores);
        AsegurarCarpeta(CarpetaModulos);

        List<string> actores = new List<string>();
        List<string> modulos = new List<string>();
        List<string> pendientes = new List<string>();

        ExtraerActores(raizActores, actores, pendientes);
        ExtraerModulos(raizNivel, modulos);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        StringBuilder informe = new StringBuilder();
        informe.Append("RELEVO extraer prefabs: '").Append(escena.name)
            .Append("' — actores ").Append(actores.Count)
            .Append(", módulos ").Append(modulos.Count).Append('.');

        if (actores.Count > 0) informe.Append("\n  Actores: ").Append(string.Join(", ", actores.ToArray()));
        if (modulos.Count > 0) informe.Append("\n  Módulos: ").Append(string.Join(", ", modulos.ToArray()));

        if (pendientes.Count > 0)
        {
            informe.Append("\n  Referencias que quedan fuera del prefab y hay que cablear en escena:");
            for (int i = 0; i < pendientes.Count; i++) informe.Append("\n    ").Append(pendientes[i]);
        }

        Debug.Log(informe.ToString());

        if (interactivo)
        {
            EditorUtility.DisplayDialog("Relevo — Extraer prefabs",
                "Actores: " + actores.Count + "\nMódulos: " + modulos.Count
                + "\n\nGuardados en Assets/Prefabs.", "Entendido");
        }
    }

    // ------------------------------------------------------------------ actores

    /// <summary>
    /// Guarda cada hijo directo de 'Actores' como su propio prefab. Cada actor ya es un objeto
    /// autocontenido salvo por sus enlaces a otros actores, que se informan aparte.
    /// </summary>
    static void ExtraerActores(GameObject raizActores, List<string> creados, List<string> pendientes)
    {
        for (int i = 0; i < raizActores.transform.childCount; i++)
        {
            Transform hijo = raizActores.transform.GetChild(i);
            if (hijo == null) continue;

            string nombre;
            if (!nombreDePrefab.TryGetValue(hijo.name, out nombre)) nombre = hijo.name;

            string ruta = CarpetaActores + "/" + nombre + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(hijo.gameObject, ruta);

            if (prefab == null)
            {
                Debug.LogError("RELEVO extraer prefabs: no se pudo guardar '" + hijo.name
                    + "' en " + ruta + ".");
                continue;
            }

            creados.Add(nombre);

            List<string> externas = ReferenciasFueraDe(hijo.gameObject);
            for (int e = 0; e < externas.Count; e++)
            {
                pendientes.Add(nombre + ": " + externas[e]);
            }
        }
    }

    /// <summary>
    /// Recorre las propiedades serializadas del objeto y devuelve las que apuntan a un objeto de
    /// escena que no forma parte de su jerarquía. Esas son las que el prefab no puede conservar.
    /// Los materiales, mallas y perfiles sí viajan con el prefab, así que no cuentan.
    /// </summary>
    static List<string> ReferenciasFueraDe(GameObject raiz)
    {
        HashSet<Object> dentro = new HashSet<Object>();

        Transform[] transforms = raiz.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] == null) continue;
            dentro.Add(transforms[i]);
            dentro.Add(transforms[i].gameObject);
        }

        Component[] componentes = raiz.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < componentes.Length; i++)
        {
            if (componentes[i] != null) dentro.Add(componentes[i]);
        }

        List<string> externas = new List<string>();

        for (int i = 0; i < componentes.Length; i++)
        {
            Component componente = componentes[i];
            if (componente == null || componente is Transform) continue;

            SerializedObject serializado = new SerializedObject(componente);
            SerializedProperty propiedad = serializado.GetIterator();

            bool entrar = true;
            while (propiedad.NextVisible(entrar))
            {
                entrar = false;
                if (propiedad.propertyType != SerializedPropertyType.ObjectReference) continue;

                Object destino = propiedad.objectReferenceValue;
                if (destino == null) continue;

                GameObject destinoGo = destino as GameObject;
                if (destinoGo == null)
                {
                    Component destinoComponente = destino as Component;
                    if (destinoComponente != null) destinoGo = destinoComponente.gameObject;
                }

                // No es un objeto de escena (material, malla, perfil de post-proceso).
                if (destinoGo == null) continue;

                // Es parte de la propia jerarquía: el prefab lo conserva.
                if (dentro.Contains(destinoGo)) continue;

                externas.Add(componente.GetType().Name + "." + propiedad.name + " → " + RutaDe(destinoGo));
            }
        }

        return externas;
    }

    // ------------------------------------------------------------------ módulos

    /// <summary>
    /// Extrae los módulos de la maqueta. Los actores son jerarquías autocontenidas, pero los
    /// módulos no: DioramaBuilder fusiona las casillas contiguas del mismo tipo en una sola caja,
    /// así que hay que buscar la corrida de exactamente una casilla para que el módulo encaje en
    /// la rejilla, y copiarla a un objeto temporal para poder centrar su pivote sin alterar la
    /// escena. El temporal se destruye al terminar.
    /// </summary>
    static void ExtraerModulos(GameObject raizNivel, List<string> creados)
    {
        float tamanoTile = LevelLayouts.TamanoTile;
        GameObject temporal = new GameObject(NombreTemporal);

        try
        {
            // 'Suelo', 'Agua' y 'Bloqueos' son los contenedores, no los módulos: dentro hay un
            // objeto por corrida fusionada, casi todos homónimos, y hay que elegir el de una casilla.
            ModuloDeUnaCasilla(raizNivel, temporal, "Suelo", "Terreno", tamanoTile, "Modulo_Terreno", creados);
            ModuloDeUnaCasilla(raizNivel, temporal, "Agua", "Agua", tamanoTile, "Modulo_Agua", creados);
            ModuloDeUnaCasilla(raizNivel, temporal, "Bloqueos", "Bloqueo agua", tamanoTile, "Modulo_Bloqueo", creados);
            ModuloVegetacion(raizNivel, temporal, creados);

            // El perímetro ('Muro norte' y compañía) se dimensiona al nivel entero, no por casillas,
            // así que no es un módulo reutilizable: el bloqueo por casilla es Modulo_Bloqueo.
        }
        finally
        {
            Object.DestroyImmediate(temporal);
        }
    }

    /// <summary>
    /// Copia de un contenedor de la maqueta el primer hijo del nombre indicado cuya escala sea
    /// la de una sola casilla. El nombre distingue el módulo de sus vecinos del mismo contenedor
    /// (por ejemplo 'Bloqueo agua' frente a los 'Muro *' del perímetro) y la escala descarta las
    /// corridas que DioramaBuilder fusionó.
    /// </summary>
    static void ModuloDeUnaCasilla(GameObject raizNivel, GameObject temporal, string rutaContenedor,
        string nombreHijo, float tamanoTile, string nombre, List<string> creados)
    {
        Transform contenedor = raizNivel.transform.Find(rutaContenedor);
        if (contenedor == null)
        {
            Debug.LogWarning("RELEVO extraer prefabs: no existe 'Nivel/" + rutaContenedor
                + "'; se omite " + nombre + ".");
            return;
        }

        GameObject origen = null;
        for (int i = 0; i < contenedor.childCount; i++)
        {
            Transform hijo = contenedor.GetChild(i);
            if (hijo == null) continue;
            if (hijo.name != nombreHijo) continue;

            Vector3 escala = hijo.localScale;
            if (Mathf.Abs(escala.x - tamanoTile) < 0.01f && Mathf.Abs(escala.z - tamanoTile) < 0.01f)
            {
                origen = hijo.gameObject;
                break;
            }
        }

        if (origen == null)
        {
            Debug.LogWarning("RELEVO extraer prefabs: no hay ninguna corrida de una sola casilla en "
                + "'Nivel/" + rutaContenedor + "/" + nombreHijo + "'; se omite " + nombre + ".");
            return;
        }

        GameObject clon = Object.Instantiate(origen, temporal.transform);
        clon.name = nombre;

        // El pivote queda en el centro de su casilla: se anula el desplazamiento horizontal y se
        // conserva la altura, que es la que sitúa el módulo respecto al suelo del nivel.
        clon.transform.localPosition = new Vector3(0f, origen.transform.localPosition.y, 0f);

        Guardar(clon, nombre, creados);
    }

    /// <summary>
    /// La vegetación de una casilla no es un solo objeto: son el tronco, las copas y el farol,
    /// repartidos entre hermanos distintos de 'Detalles'. Se agrupan por proximidad al centro de
    /// la casilla marcada con 'v'.
    /// </summary>
    static void ModuloVegetacion(GameObject raizNivel, GameObject temporal, List<string> creados)
    {
        Transform detalles = raizNivel.transform.Find("Detalles");
        if (detalles == null)
        {
            Debug.LogWarning("RELEVO extraer prefabs: no existe 'Nivel/Detalles'; se omite Modulo_Vegetacion.");
            return;
        }

        LevelLayout layout = LayoutDeLaEscena();
        if (layout == null)
        {
            Debug.LogWarning("RELEVO extraer prefabs: no se pudo resolver el mapa del nivel; "
                + "se omite Modulo_Vegetacion.");
            return;
        }

        List<Vector3> centros = layout.CentrosDeCasillas('v');
        if (centros.Count == 0)
        {
            Debug.LogWarning("RELEVO extraer prefabs: el mapa no tiene casillas de vegetación ('v'); "
                + "se omite Modulo_Vegetacion.");
            return;
        }

        Vector3 centro = centros[0];
        List<Transform> piezas = new List<Transform>();

        for (int i = 0; i < detalles.childCount; i++)
        {
            Transform hijo = detalles.GetChild(i);
            if (hijo == null) continue;

            float dx = hijo.localPosition.x - centro.x;
            float dz = hijo.localPosition.z - centro.z;
            // El detalle más alejado del farol está a 0,78 de su centro; se deja margen de sobra
            // sin llegar a alcanzar los norays de otra casilla.
            if (Mathf.Sqrt(dx * dx + dz * dz) > 1.2f) continue;

            piezas.Add(hijo);
        }

        if (piezas.Count == 0)
        {
            Debug.LogWarning("RELEVO extraer prefabs: no se encontraron piezas de vegetación; "
                + "se omite Modulo_Vegetacion.");
            return;
        }

        GameObject grupo = new GameObject("Modulo_Vegetacion");
        grupo.transform.SetParent(temporal.transform, false);

        for (int i = 0; i < piezas.Count; i++)
        {
            GameObject clon = Object.Instantiate(piezas[i].gameObject, grupo.transform);
            clon.name = piezas[i].name;
            clon.transform.localPosition = new Vector3(
                piezas[i].localPosition.x - centro.x,
                piezas[i].localPosition.y,
                piezas[i].localPosition.z - centro.z);
            clon.transform.localRotation = piezas[i].localRotation;
        }

        Guardar(grupo, "Modulo_Vegetacion", creados);
    }

    static void Guardar(GameObject clon, string nombre, List<string> creados)
    {
        string ruta = CarpetaModulos + "/" + nombre + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(clon, ruta);
        Object.DestroyImmediate(clon);

        if (prefab == null)
        {
            Debug.LogError("RELEVO extraer prefabs: no se pudo guardar " + ruta + ".");
            return;
        }

        creados.Add(nombre);
    }

    // ------------------------------------------------------------------ soporte

    /// <summary>Índice de nivel del LevelBootstrap de la escena, para resolver su mapa.</summary>
    static LevelLayout LayoutDeLaEscena()
    {
        LevelBootstrap bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
        if (bootstrap == null) return null;

        SerializedObject serializado = new SerializedObject(bootstrap);
        SerializedProperty propiedad = serializado.FindProperty("indiceNivel");
        if (propiedad == null) return null;

        return LevelLayouts.Obtener(propiedad.intValue);
    }

    static GameObject RaizEnEscena(Scene escena, string nombre)
    {
        if (!escena.IsValid()) return null;

        GameObject[] raices = escena.GetRootGameObjects();
        for (int i = 0; i < raices.Length; i++)
        {
            if (raices[i] != null && raices[i].name == nombre) return raices[i];
        }

        return null;
    }

    static string RutaDe(GameObject go)
    {
        StringBuilder ruta = new StringBuilder(go.name);
        Transform padre = go.transform.parent;

        while (padre != null)
        {
            ruta.Insert(0, padre.name + "/");
            padre = padre.parent;
        }

        return ruta.ToString();
    }

    static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;

        string[] partes = ruta.Split('/');
        string acumulada = partes[0];

        for (int i = 1; i < partes.Length; i++)
        {
            string siguiente = acumulada + "/" + partes[i];
            if (!AssetDatabase.IsValidFolder(siguiente)) AssetDatabase.CreateFolder(acumulada, partes[i]);
            acumulada = siguiente;
        }
    }

    static void Fallo(string mensaje, bool interactivo)
    {
        if (interactivo) EditorUtility.DisplayDialog("Relevo — Extraer prefabs", mensaje, "Entendido");
        else Debug.LogError("RELEVO extraer prefabs: " + mensaje);
    }
}
