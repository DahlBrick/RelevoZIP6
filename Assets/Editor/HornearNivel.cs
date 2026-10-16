using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Hornea el nivel de la escena activa: construye con LevelBootstrap la maqueta, la cámara
/// ortográfica, los actores y sus colliders como objetos persistentes y guarda la escena, de
/// modo que el nivel sea visible y editable fuera de Play Mode.
///
/// Dos detalles que no son obvios y que esta herramienta resuelve:
///  - Los materiales de la paleta y el perfil de post-proceso se crean en memoria con
///    'new Material()' y 'CreateInstance()', así que no son assets: guardarlos en una escena
///    los perdería y el nivel quedaría invisible. Aquí se convierten en assets reales
///    (Assets/Materials y Assets/Settings) antes de guardar.
///  - Los objetos se marcan con ContenidoHorneado, de forma que hornear dos veces limpie
///    únicamente lo que generó esta herramienta y no duplique nada.
///
/// Los estados dinámicos (batería, puentes, palancas, animaciones y partículas) siguen bajo
/// sus sistemas actuales: no se congelan aquí. LevelBootstrap.Enlazar() los restablece al
/// empezar el nivel llamando a los Vincular() de cada actor.
/// </summary>
public static class HornearNivel
{
    const string CarpetaMateriales = "Assets/Materials";
    const string CarpetaMallas = "Assets/Meshes";
    const string CarpetaAjustes = "Assets/Settings";

    static readonly Dictionary<int, Object> persistidos = new Dictionary<int, Object>();
    static int assetsConvertidos;

    [MenuItem("Relevo/Hornear nivel de la escena activa")]
    public static void Hornear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Aviso("Sal de Play Mode antes de hornear: la escena debe construirse y guardarse "
                + "desde el editor, no desde una partida en curso.");
            return;
        }

        Scene escena = SceneManager.GetActiveScene();
        if (!escena.IsValid() || !escena.isLoaded)
        {
            Debug.LogError("RELEVO hornear: no hay ninguna escena válida abierta.");
            return;
        }

        LevelBootstrap bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
        if (bootstrap == null)
        {
            Aviso("La escena '" + escena.name + "' no contiene ningún LevelBootstrap.\n\n"
                + "Abre 'Assets/Scenes/Juego/Nivel1_MuelleDeCobre.unity' (o la escena del nivel que "
                + "quieras hornear) y vuelve a ejecutar la herramienta.");
            return;
        }

        persistidos.Clear();
        assetsConvertidos = 0;

        int borrados = LimpiarContenidoHorneado(escena);

        bootstrap.ConstruirParaEditor();

        List<GameObject> raices = RaicesHorneadas(escena);
        int materiales = PersistirMateriales(raices);
        int mallas = PersistirMallas(raices);
        int perfiles = PersistirPerfilesDeVolumen(raices);
        int renders = ContarComponentes<Renderer>(raices);
        int colliders = ContarComponentes<Collider>(raices);

        MarcarEscenaHorneada(bootstrap);

        EditorSceneManager.MarkSceneDirty(escena);
        bool guardada = EditorSceneManager.SaveScene(escena);
        AssetDatabase.SaveAssets();

        Debug.Log("RELEVO hornear: '" + escena.name + "' " + (guardada ? "guardada" : "NO guardada")
            + " — raíces " + raices.Count + ", borrados antes " + borrados
            + ", renderers " + renders + ", colliders " + colliders
            + ", assets creados o refrescados " + assetsConvertidos
            + ", slots de material apuntando a asset " + materiales
            + ", mallas generadas convertidas en asset " + mallas
            + ", perfiles de post-proceso " + perfiles + ".");

        if (!guardada)
        {
            Debug.LogError("RELEVO hornear: la escena no se pudo guardar. Revisa que el archivo "
                + "no esté bloqueado por OneDrive y vuelve a intentarlo.");
        }
    }

    /// <summary>
    /// Borra solo las raíces marcadas con ContenidoHorneado que pertenecen a esta escena.
    /// Al destruir la raíz desaparecen también sus hijos, así que no queda nada del horneado
    /// anterior y nunca se toca contenido ajeno.
    /// </summary>
    static int LimpiarContenidoHorneado(Scene escena)
    {
        ContenidoHorneado[] marcados = Object.FindObjectsByType<ContenidoHorneado>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        HashSet<GameObject> raices = new HashSet<GameObject>();
        for (int i = 0; i < marcados.Length; i++)
        {
            ContenidoHorneado marcado = marcados[i];
            if (marcado == null) continue;
            if (marcado.gameObject.scene != escena) continue;
            raices.Add(marcado.transform.root.gameObject);
        }

        int borrados = 0;
        foreach (GameObject raiz in raices)
        {
            if (raiz == null) continue;
            borrados += raiz.GetComponentsInChildren<Transform>(true).Length;
            Object.DestroyImmediate(raiz);
        }

        return borrados;
    }

    static List<GameObject> RaicesHorneadas(Scene escena)
    {
        ContenidoHorneado[] marcados = Object.FindObjectsByType<ContenidoHorneado>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        HashSet<GameObject> unicas = new HashSet<GameObject>();
        for (int i = 0; i < marcados.Length; i++)
        {
            ContenidoHorneado marcado = marcados[i];
            if (marcado == null) continue;
            if (marcado.gameObject.scene != escena) continue;
            unicas.Add(marcado.transform.root.gameObject);
        }

        return new List<GameObject>(unicas);
    }

    static int ContarComponentes<T>(List<GameObject> raices) where T : Component
    {
        int total = 0;
        for (int i = 0; i < raices.Count; i++)
        {
            if (raices[i] == null) continue;
            total += raices[i].GetComponentsInChildren<T>(true).Length;
        }
        return total;
    }

    /// <summary>
    /// Sustituye cada material de los renderers horneados por un asset real. Sin esto la
    /// escena guardada apuntaría a materiales que solo existen en memoria y se vería sin
    /// color fuera de Play Mode.
    /// </summary>
    static int PersistirMateriales(List<GameObject> raices)
    {
        int cambios = 0;

        for (int i = 0; i < raices.Count; i++)
        {
            if (raices[i] == null) continue;

            Renderer[] renders = raices[i].GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renders.Length; r++)
            {
                Material[] materiales = renders[r].sharedMaterials;
                if (materiales.Length == 0) continue;

                bool tocado = false;
                for (int m = 0; m < materiales.Length; m++)
                {
                    Material original = materiales[m];
                    if (original == null) continue;

                    Material asset = PersistirAsset(original, CarpetaMateriales, ".mat") as Material;
                    if (asset == null) continue;

                    cambios++;

                    if (asset != original)
                    {
                        materiales[m] = asset;
                        tocado = true;
                    }
                }

                if (tocado) renders[r].sharedMaterials = materiales;
            }
        }

        return cambios;
    }

    /// <summary>
    /// Convierte en asset las mallas generadas por código. Las primitivas del motor (Cube,
    /// Sphere, Cylinder) son recursos internos: persistirlas crearía assets basura y ensuciaría
    /// la escena, así que solo se tocan las que llevan el prefijo 'RELEVO_'. Sin esto, la malla
    /// del prisma hexagonal de la base viviría solo en memoria y la escena guardada la perdería.
    /// </summary>
    static int PersistirMallas(List<GameObject> raices)
    {
        int cambios = 0;

        for (int i = 0; i < raices.Count; i++)
        {
            if (raices[i] == null) continue;

            MeshFilter[] filtros = raices[i].GetComponentsInChildren<MeshFilter>(true);
            for (int f = 0; f < filtros.Length; f++)
            {
                Mesh malla = filtros[f].sharedMesh;
                if (malla == null) continue;
                if (!malla.name.StartsWith("RELEVO_")) continue;

                Mesh asset = PersistirAsset(malla, CarpetaMallas, ".asset") as Mesh;
                if (asset == null) continue;

                cambios++;

                if (asset != malla) filtros[f].sharedMesh = asset;

                // Se registra el recuento de vértices para poder comprobar que el refresco de un
                // asset ya existente copió de verdad la geometría y no la dejó vacía.
                Debug.Log("RELEVO hornear: malla '" + asset.name + "' en "
                    + AssetDatabase.GetAssetPath(asset) + " — vértices " + asset.vertexCount + ".");
            }
        }

        return cambios;
    }

    static int PersistirPerfilesDeVolumen(List<GameObject> raices)
    {
        int cambios = 0;

        for (int i = 0; i < raices.Count; i++)
        {
            if (raices[i] == null) continue;

            Volume[] volumenes = raices[i].GetComponentsInChildren<Volume>(true);
            for (int v = 0; v < volumenes.Length; v++)
            {
                Volume volumen = volumenes[v];
                if (volumen == null) continue;

                VolumeProfile perfil = volumen.sharedProfile;

                // CameraRig asigna el perfil con 'volumen.profile = perfil', y ese setter escribe
                // el perfil interno (no serializado), así que el compartido llega vacío: hay que
                // tomarlo del interno para poder guardarlo con la escena.
                if (perfil == null) perfil = volumen.profile;

                if (perfil == null) continue;

                VolumeProfile asset = PersistirPerfilVolumen(perfil);
                if (asset == null) continue;

                volumen.sharedProfile = asset;  // es el campo que se guarda con la escena
                volumen.profile = asset;        // y queda activo ya en la vista de escena

                if (asset != perfil) cambios++;
            }
        }

        return cambios;
    }

    /// <summary>
    /// Escribe el perfil de post-proceso como un asset completo. Los componentes de un volumen
    /// (Bloom, Tonemapping, ColorAdjustments, Vignette) son ScriptableObjects independientes: si
    /// solo se guarda el perfil, cada entrada de su lista apunta a un objeto que no pertenece a
    /// ningún asset y el archivo acaba con 'components: - {fileID: 0}'. La escena horneada se ve
    /// entonces sin post-proceso aunque el volumen siga ahí. Por eso cada componente se añade
    /// también al archivo como sub-asset, y al rehornear se borran los anteriores para no acumular
    /// copias dentro del mismo perfil.
    /// </summary>
    static VolumeProfile PersistirPerfilVolumen(VolumeProfile original)
    {
        if (original == null) return null;

        Object guardado;
        if (persistidos.TryGetValue(original.GetInstanceID(), out guardado)) return guardado as VolumeProfile;

        if (AssetDatabase.Contains(original))
        {
            persistidos[original.GetInstanceID()] = original;
            return original;
        }

        AsegurarCarpeta(CarpetaAjustes);
        string ruta = CarpetaAjustes + "/" + original.name + ".asset";

        VolumeProfile asset = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);

        if (asset == null)
        {
            AssetDatabase.CreateAsset(original, ruta);
            asset = original;
        }
        else
        {
            Object[] contenidos = AssetDatabase.LoadAllAssetsAtPath(ruta);
            for (int i = 0; i < contenidos.Length; i++)
            {
                if (contenidos[i] is VolumeComponent) Object.DestroyImmediate(contenidos[i], true);
            }

            EditorUtility.CopySerialized(original, asset);
        }

        for (int i = 0; i < asset.components.Count; i++)
        {
            VolumeComponent componente = asset.components[i];
            if (componente == null) continue;

            componente.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(componente, asset);
        }

        EditorUtility.SetDirty(asset);
        persistidos[original.GetInstanceID()] = asset;
        assetsConvertidos++;

        Debug.Log("RELEVO hornear: perfil de post-proceso '" + asset.name + "' en " + ruta
            + " con " + asset.components.Count + " componentes.");

        return asset;
    }

    /// <summary>
    /// Convierte un objeto creado en memoria en un asset. Si ya existe un asset en esa ruta
    /// se conserva su GUID y solo se refresca su contenido, para no romper referencias.
    /// </summary>
    static Object PersistirAsset(Object original, string carpeta, string extension)
    {
        if (original == null) return null;
        if (AssetDatabase.Contains(original)) return original;

        Object yaPersistido;
        if (persistidos.TryGetValue(original.GetInstanceID(), out yaPersistido)) return yaPersistido;

        AsegurarCarpeta(carpeta);
        string ruta = carpeta + "/" + original.name + extension;

        Object enDisco = AssetDatabase.LoadAssetAtPath<Object>(ruta);
        if (enDisco != null)
        {
            EditorUtility.CopySerialized(original, enDisco);
            EditorUtility.SetDirty(enDisco);
            persistidos[original.GetInstanceID()] = enDisco;
            assetsConvertidos++;
            return enDisco;
        }

        AssetDatabase.CreateAsset(original, ruta);
        persistidos[original.GetInstanceID()] = original;
        assetsConvertidos++;
        return original;
    }

    static void MarcarEscenaHorneada(LevelBootstrap bootstrap)
    {
        SerializedObject serializado = new SerializedObject(bootstrap);
        SerializedProperty propiedad = serializado.FindProperty("escenaHorneada");

        if (propiedad == null)
        {
            Debug.LogError("RELEVO hornear: no se encontró el campo 'escenaHorneada' en "
                + "LevelBootstrap; la escena se construyó pero volverá a construirse en tiempo "
                + "de ejecución.");
            return;
        }

        propiedad.boolValue = true;
        serializado.ApplyModifiedProperties();
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

    static void Aviso(string mensaje)
    {
        EditorUtility.DisplayDialog("Relevo — Hornear nivel", mensaje, "Entendido");
    }
}
