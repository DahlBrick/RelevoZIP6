using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Lleva a Unity los bloques de Liria que exporta Blender (Assets/Meshes/Liria/*.fbx).
///
/// Cada FBX trae dos mallas: "_Geo", la geometría unificada del bloque, y "_Limites", las paredes
/// invisibles del borde de lo pisable. Aquí se ajusta su importación, se le asigna un material de
/// Unity por categoría (LIRIA_Madera, LIRIA_Chapa...) y se guarda un prefab con sus colisiones.
/// Los materiales usan RELEVO/Comic con un color plano por categoría (la paleta de abajo); la
/// textura de detalle se hace al final. El contorno de tinta lo pone Liria_Renderer, un renderer
/// propio para no tocar el aspecto de los niveles actuales.
/// </summary>
public static class PrepararLiria
{
    const string CarpetaMallas = "Assets/Meshes/Liria";
    const string CarpetaMateriales = "Assets/Materials/Liria";
    const string CarpetaPrefabs = "Assets/Prefabs/Liria";
    const string EscenaPrueba = "Assets/Scenes/Liria_Prueba.unity";

    const string RendererBase = "Assets/Settings/PC_Renderer.asset";
    const string RendererLiria = "Assets/Settings/Liria_Renderer.asset";
    const string PipelinePC = "Assets/Settings/PC_RPAsset.asset";

    // Paleta de tarde de la hoja de referencia: madera cálida, chapa verdosa gastada, óxido en los
    // tejados y piedra gris. Las sombras las tiñe el shader de violeta frío.
    static readonly (string nombre, Color color)[] Categorias =
    {
        ("LIRIA_Madera", new Color(0.60f, 0.47f, 0.34f)),
        ("LIRIA_Chapa", new Color(0.48f, 0.58f, 0.58f)),
        ("LIRIA_Tejado", new Color(0.52f, 0.26f, 0.20f)),
        ("LIRIA_Tela", new Color(0.88f, 0.80f, 0.62f)),
        ("LIRIA_Cristal", new Color(0.25f, 0.35f, 0.40f)),
        ("LIRIA_Roca", new Color(0.50f, 0.52f, 0.55f)),
        ("LIRIA_Follaje", new Color(0.55f, 0.58f, 0.24f)),
        ("LIRIA_Montana", new Color(0.44f, 0.52f, 0.48f)),
        ("LIRIA_Varios", new Color(0.66f, 0.60f, 0.52f)),
    };

    static readonly Color Sol = new Color(1.0f, 0.86f, 0.68f);

    /// <summary>Color del horizonte del cielo: la niebla usa el mismo para fundir mar, montañas y cielo.</summary>
    public static readonly Color Horizonte = new Color(0.80f, 0.87f, 0.89f);

    [MenuItem("RELEVO/Liria/Importar bloques")]
    public static string ImportarBloques()
    {
        Directory.CreateDirectory(CarpetaMateriales);
        Directory.CreateDirectory(CarpetaPrefabs);
        AssetDatabase.Refresh();

        foreach (var (nombre, color) in Categorias) Material(nombre, color);

        string informe = "";
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { CarpetaMallas }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            informe += Importar(ruta) + "\n";
        }
        AssetDatabase.SaveAssets();
        return informe;
    }

    static Material Material(string nombre, Color color)
    {
        string ruta = CarpetaMateriales + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        Shader comic = Shader.Find("RELEVO/Comic");
        if (m == null)
        {
            m = new Material(comic);
            AssetDatabase.CreateAsset(m, ruta);
        }
        if (m.shader != comic) m.shader = comic;
        m.SetColor("_BaseColor", color);
        // Chapa ondulada en los tejados de óxido y en las paredes de chapa de las casas.
        bool ondulado = nombre == "LIRIA_Tejado" || nombre == "LIRIA_Chapa";
        m.SetFloat("_Ondulado", ondulado ? 1f : 0f);
        if (ondulado) m.EnableKeyword("_ONDULADO"); else m.DisableKeyword("_ONDULADO");
        // Montañas: poco contraste y casi sin trama, para que se lean como fondo lejano.
        if (nombre == "LIRIA_Montana")
        {
            m.SetColor("_ColorSombra", new Color(0.66f, 0.70f, 0.82f));
            m.SetFloat("_Trama", 0.04f);
            m.SetFloat("_Ambiente", 0.45f);
            m.SetFloat("_Medio", 0.7f);
        }
        if (nombre == "LIRIA_Chapa")
        {
            m.SetFloat("_RelieveOnda", 0.45f);
            m.SetFloat("_TintaOnda", 0.22f);
            m.SetFloat("_Oxido", 0.35f);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static string Importar(string ruta)
    {
        ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(ruta);
        imp.globalScale = 1f;
        imp.useFileScale = true;
        imp.bakeAxisConversion = false;
        imp.importAnimation = false;
        imp.animationType = ModelImporterAnimationType.None;
        imp.importCameras = false;
        imp.importLights = false;
        imp.addCollider = false;
        imp.isReadable = false;
        imp.importNormals = ModelImporterNormals.Import;
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
        foreach (var (nombre, _) in Categorias)
        {
            var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), nombre);
            imp.AddRemap(id, AssetDatabase.LoadAssetAtPath<Material>(CarpetaMateriales + "/" + nombre + ".mat"));
        }
        imp.SaveAndReimport();

        GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        GameObject raiz = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
        int tris = 0;
        // El fondo (montañas e islas lejanas) no se pisa: sin colisiones.
        bool fondo = modelo.name.StartsWith("Fondo");
        foreach (MeshFilter f in raiz.GetComponentsInChildren<MeshFilter>())
        {
            if (fondo)
            {
                tris += f.sharedMesh.triangles.Length / 3;
                f.gameObject.isStatic = true;
                continue;
            }
            MeshCollider c = f.gameObject.AddComponent<MeshCollider>();
            c.sharedMesh = f.sharedMesh;
            if (f.name.EndsWith("_Limites"))
            {
                f.GetComponent<MeshRenderer>().enabled = false;
                f.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            else
            {
                tris += f.sharedMesh.triangles.Length / 3;
                f.gameObject.isStatic = true;
            }
        }
        string destino = CarpetaPrefabs + "/" + modelo.name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(raiz, destino);
        Bounds b = raiz.GetComponentInChildren<MeshRenderer>().bounds;
        Object.DestroyImmediate(raiz);
        return modelo.name + ": " + tris + " triángulos, caja " + b.min.ToString("F1") + " a " + b.max.ToString("F1");
    }

    /// <summary>
    /// Liria_Renderer: copia de PC_Renderer con el contorno de tinta como pase de pantalla
    /// completa. Se añade a la lista de renderers del pipeline de PC y devuelve su índice, que es
    /// el que tiene que usar la cámara.
    /// </summary>
    [MenuItem("RELEVO/Liria/Preparar renderer de tinta")]
    public static int PrepararRenderer()
    {
        UniversalRendererData datos = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererLiria);
        if (datos == null)
        {
            AssetDatabase.CopyAsset(RendererBase, RendererLiria);
            datos = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererLiria);
        }

        string rutaTinta = CarpetaMateriales + "/LIRIA_ContornoTinta.mat";
        Material tinta = AssetDatabase.LoadAssetAtPath<Material>(rutaTinta);
        if (tinta == null)
        {
            tinta = new Material(Shader.Find("RELEVO/ContornoTinta"));
            AssetDatabase.CreateAsset(tinta, rutaTinta);
        }
        tinta.SetColor("_ColorTinta", new Color(0.24f, 0.15f, 0.13f));
        tinta.SetFloat("_Grosor", 1.3f);
        tinta.SetFloat("_UmbralProfundidad", 0.3f);
        tinta.SetFloat("_UmbralNormal", 0.35f);
        tinta.SetFloat("_Opacidad", 0.7f);
        EditorUtility.SetDirty(tinta);

        FullScreenPassRendererFeature contorno = null;
        foreach (ScriptableRendererFeature f in datos.rendererFeatures)
            if (f is FullScreenPassRendererFeature fs) contorno = fs;
        if (contorno == null)
        {
            contorno = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            contorno.name = "ContornoTinta";
            AssetDatabase.AddObjectToAsset(contorno, datos);
            datos.rendererFeatures.Add(contorno);
        }
        contorno.passMaterial = tinta;
        // Antes de los transparentes: el agua se dibuja encima y tapa las líneas de lo sumergido.
        contorno.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingTransparents;
        contorno.fetchColorBuffer = true;
        contorno.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
        contorno.SetActive(true);

        // El mapa de identificadores de las features lo mantiene el inspector; aquí se rehace.
        SerializedObject so = new SerializedObject(datos);
        SerializedProperty mapa = so.FindProperty("m_RendererFeatureMap");
        mapa.arraySize = datos.rendererFeatures.Count;
        for (int i = 0; i < datos.rendererFeatures.Count; i++)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(datos.rendererFeatures[i], out string _, out long id);
            mapa.GetArrayElementAtIndex(i).longValue = id;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(contorno);
        EditorUtility.SetDirty(datos);

        UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePC);
        SerializedObject sp = new SerializedObject(pipeline);
        SerializedProperty lista = sp.FindProperty("m_RendererDataList");
        int indice = -1;
        for (int i = 0; i < lista.arraySize; i++)
            if (lista.GetArrayElementAtIndex(i).objectReferenceValue == datos) indice = i;
        if (indice < 0)
        {
            indice = lista.arraySize;
            lista.arraySize++;
            lista.GetArrayElementAtIndex(indice).objectReferenceValue = datos;
            sp.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssets();
        return indice;
    }

    /// <summary>Agua de Liria: el shader RELEVO/Agua (espuma de orilla y vetas) con tonos de la hoja.</summary>
    static Material MaterialAgua()
    {
        string ruta = CarpetaMateriales + "/LIRIA_Agua.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(Shader.Find("RELEVO/Agua"));
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.SetColor("_ColorSomero", new Color(0.36f, 0.66f, 0.68f));
        m.SetColor("_ColorProfundo", new Color(0.12f, 0.30f, 0.42f));
        m.SetColor("_ColorEspuma", new Color(0.96f, 0.95f, 0.88f));
        m.SetFloat("_Profundidad", 1.6f);
        m.SetFloat("_AnchoEspuma", 0.45f);
        m.SetFloat("_EscalaOndas", 0.45f);
        m.SetFloat("_Opacidad", 0.88f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static VolumeProfile PerfilLiria()
    {
        const string ruta = "Assets/Settings/PerfilLiria.asset";
        VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
        if (perfil == null)
        {
            perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(perfil, ruta);
        }
        T Ajuste<T>() where T : VolumeComponent
        {
            if (!perfil.TryGet(out T c))
            {
                c = perfil.Add<T>(true);
                c.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(c, perfil);
            }
            return c;
        }
        Ajuste<Tonemapping>().mode.Override(TonemappingMode.None);
        ColorAdjustments color = Ajuste<ColorAdjustments>();
        color.postExposure.Override(0f);
        color.contrast.Override(6f);
        color.saturation.Override(10f);
        color.colorFilter.Override(Color.white);
        Bloom bloom = Ajuste<Bloom>();
        bloom.intensity.Override(0.3f);
        bloom.threshold.Override(1.1f);
        Vignette vineta = Ajuste<Vignette>();
        vineta.intensity.Override(0.16f);
        vineta.smoothness.Override(0.5f);
        EditorUtility.SetDirty(perfil);
        AssetDatabase.SaveAssets();
        return perfil;
    }

    /// <summary>Cielo de cómic (RELEVO/CieloComic) con los tonos de la hoja de Liria.</summary>
    static Material MaterialCielo()
    {
        string ruta = CarpetaMateriales + "/LIRIA_Cielo.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(Shader.Find("RELEVO/CieloComic"));
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.SetColor("_ColorHorizonte", Horizonte);
        m.SetColor("_ColorMedio", new Color(0.58f, 0.77f, 0.90f));
        m.SetColor("_ColorCenit", new Color(0.33f, 0.58f, 0.85f));
        m.SetColor("_ColorNube", new Color(1.0f, 0.99f, 0.95f));
        m.SetColor("_ColorNubeSombra", new Color(0.74f, 0.76f, 0.90f));
        m.SetFloat("_Escala", 2.0f);
        m.SetFloat("_Cobertura", 0.4f);
        m.SetFloat("_GrosorTinta", 0.007f);
        EditorUtility.SetDirty(m);
        return m;
    }

    [MenuItem("RELEVO/Liria/Crear escena de prueba")]
    public static string CrearEscenaPrueba()
    {
        ImportarBloques();
        int renderer = PrepararRenderer();

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ActorFactory.ConfigurarEntorno();
        // Sol de tarde: bajo, cálido y lateral (entra por la izquierda y algo por detrás de la cámara):
        // las fachadas quedan en medio tono, los laterales contrastan y las sombras cruzan el muelle.
        Light sol = ActorFactory.CrearSol(null);
        sol.color = Sol;
        sol.intensity = 1.25f;
        sol.transform.rotation = Quaternion.Euler(38f, 65f, 0f);
        sol.shadows = LightShadows.Hard;

        // Todo lo que sale de liria_master.blend cuelga de "Liria". En Blender el mar queda hacia -Y,
        // que en Unity es +Z, lejos de la cámara: girado, el muelle queda delante y las casas detrás.
        // El giro es sobre el origen, que es donde arranca la pasarela, así que el inicio de Nilo
        // no cambia.
        Transform liria = new GameObject("Liria").transform;
        liria.rotation = Quaternion.Euler(0f, 180f, 0f);
        GameObject b01 = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPrefabs + "/B01_Muelle.prefab"), liria);
        PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPrefabs + "/Fondo_Liria.prefab"), liria);

        Material cielo = MaterialCielo();
        RenderSettings.skybox = cielo;

        // Post-proceso propio de Liria, por encima del de los niveles (prioridad 1): sin mapeo
        // tonal, que comprimía los blancos y dejaba nubes y cielo grises, y con algo más de color.
        Volume volumen = new GameObject("Volumen Liria").AddComponent<Volume>();
        volumen.isGlobal = true;
        volumen.priority = 5f;
        volumen.sharedProfile = PerfilLiria();

        GameObject agua = GameObject.CreatePrimitive(PrimitiveType.Plane);
        agua.name = "Agua_provisional";
        Object.DestroyImmediate(agua.GetComponent<Collider>());
        agua.transform.position = new Vector3(0f, -1.5f, 0f);
        agua.transform.localScale = Vector3.one * 140f;   // 1,4 km: llega al horizonte en las cinemáticas
        agua.GetComponent<MeshRenderer>().sharedMaterial = MaterialAgua();

        // Cámara menos cenital que la de los niveles (55°): a 40° se leen las fachadas y los pilotes.
        CameraRig rig = new GameObject("Camara").AddComponent<CameraRig>();
        SerializedObject sr = new SerializedObject(rig);
        sr.FindProperty("inclinacion").floatValue = 40f;
        sr.FindProperty("holguraLateral").floatValue = 0f;
        sr.ApplyModifiedPropertiesWithoutUndo();

        PruebaLiria prueba = new GameObject("PruebaLiria").AddComponent<PruebaLiria>();
        SerializedObject so = new SerializedObject(prueba);
        so.FindProperty("acciones").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Input/Relevo.inputactions");
        so.FindProperty("bloque").objectReferenceValue = b01.transform;
        // Arranque de la pasarela, junto a las casas.
        so.FindProperty("inicioNilo").vector3Value = new Vector3(0f, 0f, 0f);
        so.FindProperty("indiceRenderer").intValue = renderer;
        so.FindProperty("cielo").objectReferenceValue = cielo;
        so.FindProperty("colorHorizonte").colorValue = Horizonte;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Plano de cinemática: desde el mar, a ras de agua, con el pueblo delante y las montañas
        // detrás. Queda apagado; se enciende para grabar o para revisar el fondo.
        Camera cine = new GameObject("Plano_Frontal").AddComponent<Camera>();
        cine.transform.position = new Vector3(14f, 5.5f, -46f);
        cine.transform.LookAt(new Vector3(-2f, 8f, 30f));
        cine.fieldOfView = 42f;
        cine.nearClipPlane = 0.3f;
        cine.farClipPlane = 2500f;
        cine.clearFlags = CameraClearFlags.Skybox;
        var datosCine = cine.GetUniversalAdditionalCameraData();
        datosCine.SetRenderer(renderer);
        datosCine.renderPostProcessing = true;
        cine.enabled = false;

        EditorSceneManager.SaveScene(escena, EscenaPrueba);
        return "Escena guardada en " + EscenaPrueba;
    }
}
