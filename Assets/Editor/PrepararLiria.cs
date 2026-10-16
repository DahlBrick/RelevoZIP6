using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Lleva a Unity los bloques de Liria que exporta Blender (Assets/Meshes/Liria/*.fbx).
///
/// Cada FBX trae dos mallas: "_Geo", la geometría unificada del bloque, y "_Limites", las paredes
/// invisibles del borde de lo pisable. Aquí se ajusta su importación, se le asigna un material de
/// Unity por categoría (LIRIA_Madera, LIRIA_Chapa...) y se guarda un prefab con sus colisiones.
/// Los materiales son colores planos provisionales: la textura definitiva se hace después.
/// </summary>
public static class PrepararLiria
{
    const string CarpetaMallas = "Assets/Meshes/Liria";
    const string CarpetaMateriales = "Assets/Materials/Liria";
    const string CarpetaPrefabs = "Assets/Prefabs/Liria";
    const string EscenaPrueba = "Assets/Scenes/Liria_Prueba.unity";

    static readonly (string nombre, Color color)[] Categorias =
    {
        ("LIRIA_Madera", new Color(0.62f, 0.48f, 0.34f)),
        ("LIRIA_Chapa", new Color(0.55f, 0.57f, 0.6f)),
        ("LIRIA_Tejado", new Color(0.5f, 0.36f, 0.3f)),
        ("LIRIA_Tela", new Color(0.75f, 0.68f, 0.55f)),
        ("LIRIA_Cristal", new Color(0.4f, 0.55f, 0.6f)),
        ("LIRIA_Varios", new Color(0.6f, 0.6f, 0.55f)),
    };

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
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", 0.15f);
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
        foreach (MeshFilter f in raiz.GetComponentsInChildren<MeshFilter>())
        {
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

    [MenuItem("RELEVO/Liria/Crear escena de prueba")]
    public static string CrearEscenaPrueba()
    {
        ImportarBloques();

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ActorFactory.ConfigurarEntorno();
        ActorFactory.CrearSol(null);

        GameObject b01 = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPrefabs + "/B01_Muelle.prefab"));

        GameObject agua = GameObject.CreatePrimitive(PrimitiveType.Plane);
        agua.name = "Agua_provisional";
        Object.DestroyImmediate(agua.GetComponent<Collider>());
        agua.transform.position = new Vector3(0f, -1.5f, 0f);
        agua.transform.localScale = Vector3.one * 8f;
        Material matAgua = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RELEVO_Agua.mat");
        if (matAgua != null) agua.GetComponent<MeshRenderer>().sharedMaterial = matAgua;

        new GameObject("Camara").AddComponent<CameraRig>();

        PruebaLiria prueba = new GameObject("PruebaLiria").AddComponent<PruebaLiria>();
        SerializedObject so = new SerializedObject(prueba);
        so.FindProperty("acciones").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Input/Relevo.inputactions");
        so.FindProperty("bloque").objectReferenceValue = b01.transform;
        // Arranque de la pasarela, junto a las casas.
        so.FindProperty("inicioNilo").vector3Value = new Vector3(0f, 0f, 0f);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(escena, EscenaPrueba);
        return "Escena guardada en " + EscenaPrueba;
    }
}
