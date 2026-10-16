using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Crea la escena de la cinemática de apertura (Assets/Scenes/Juego/Apertura.unity) y la pone la primera
/// en Build Settings, para que sea lo primero que se ve al abrir el juego.
///
/// La escena solo guarda un objeto con el componente Apertura y sus referencias (entrada, modelos
/// de los kits y el cielo nocturno): todo lo demás se construye al darle Play, igual que las
/// maquetas. La herramienta es idempotente: volver a ejecutarla regenera la escena.
/// </summary>
public static class PrepararApertura
{
    const string RutaEscena = "Assets/Scenes/Juego/Apertura.unity";
    const string RutaEntrada = "Assets/Input/Relevo.inputactions";

    static readonly string[] Modelos =
    {
        "Kenney_FantasyTown/lantern", "Kenney_FantasyTown/wheel",
        "Kenney_Watercraft/boat-row-small", "Kenney_Watercraft/boat-row-large",
        "Kenney_Space/machine_generator", "Kenney_Space/machine_barrelLarge", "Kenney_Space/machine_barrel",
    };

    [MenuItem("Relevo/Preparar escena de apertura")]
    public static void Preparar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("RELEVO apertura: sal de Play Mode antes de preparar la escena.");
            return;
        }

        Scene origen = SceneManager.GetActiveScene();
        if (origen.IsValid() && origen.isDirty)
        {
            Debug.LogWarning("RELEVO apertura: la escena '" + origen.name + "' tiene cambios sin guardar. Guárdala y vuelve a ejecutar la herramienta.");
            return;
        }
        string rutaOrigen = origen.IsValid() ? origen.path : "";

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject go = new GameObject("Apertura");
        Apertura apertura = go.AddComponent<Apertura>();

        SerializedObject so = new SerializedObject(apertura);
        so.FindProperty("acciones").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(RutaEntrada);

        SerializedProperty lista = so.FindProperty("modelos");
        lista.ClearArray();
        var rutas = new List<string>();
        foreach (string m in Modelos) rutas.Add("Assets/ThirdParty/" + m + ".fbx");
        // Las casas (Medieval Village) y los habitantes (personas posadas) entran enteros.
        foreach (string carpeta in new[] { "Assets/ThirdParty/Quaternius_Village", "Assets/ThirdParty/Quaternius_Posed" })
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { carpeta }))
                rutas.Add(AssetDatabase.GUIDToAssetPath(guid));
        foreach (string ruta in rutas)
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (modelo == null) { Debug.LogWarning("RELEVO apertura: falta el modelo " + ruta + "."); continue; }
            lista.InsertArrayElementAtIndex(lista.arraySize);
            lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = modelo;
        }

        so.FindProperty("cielo").objectReferenceValue = CieloNocturno();
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(escena, RutaEscena);
        PonerPrimeraEnElBuild(RutaEscena);

        if (!string.IsNullOrEmpty(rutaOrigen)) EditorSceneManager.OpenScene(rutaOrigen, OpenSceneMode.Single);
        Debug.Log("RELEVO apertura: escena '" + RutaEscena + "' lista y primera en Build Settings.");
    }

    /// <summary>
    /// Cielo de noche con el skybox procedural de Unity: sin sol, azul muy oscuro arriba y un
    /// resplandor apagado en el horizonte. Es un asset para que el shader entre en el build.
    /// </summary>
    static Material CieloNocturno()
    {
        const string ruta = "Assets/Materials/CieloNocturno.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.SetFloat("_SunDisk", 0f);
        m.EnableKeyword("_SUNDISK_NONE");
        m.DisableKeyword("_SUNDISK_SIMPLE");
        m.DisableKeyword("_SUNDISK_HIGH_QUALITY");
        m.SetFloat("_SunSize", 0f);
        m.SetFloat("_AtmosphereThickness", 0.55f);
        m.SetColor("_SkyTint", new Color(0.18f, 0.24f, 0.45f));
        m.SetColor("_GroundColor", new Color(0.03f, 0.04f, 0.06f));
        m.SetFloat("_Exposure", 0.12f);
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    static void PonerPrimeraEnElBuild(string ruta)
    {
        var escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        escenas.RemoveAll(e => e.path == ruta);
        escenas.Insert(0, new EditorBuildSettingsScene(ruta, true));
        EditorBuildSettings.scenes = escenas.ToArray();
    }
}
