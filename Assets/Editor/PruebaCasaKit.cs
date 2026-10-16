using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Banco de pruebas del kit Medieval Village: arma una casa con CasaModular en una escena aparte,
/// la ilumina y la renderiza desde varios ángulos a Temp/capturas. Sirve para comprobar que las
/// piezas encajan antes de levantar Liria con ellas.
/// </summary>
public static class PruebaCasaKit
{
    public static string Renderizar()
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        Scene previa = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(escena);

        Light sol = new GameObject("Sol").AddComponent<Light>();
        sol.type = LightType.Directional;
        sol.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
        sol.intensity = 1.3f;
        RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);

        System.Func<string, GameObject> modelo = n =>
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Quaternius_Village/" + n + ".fbx");
        GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.transform.localScale = Vector3.one * 4f;

        var a = CasaModular.Construir(null, modelo, new CasaModular.Plano { ancho = 6, fondo = 6, pisos = 3, semilla = 3 });
        a.transform.position = new Vector3(-4f, 0f, 0f);
        var b = CasaModular.Construir(null, modelo, new CasaModular.Plano { ancho = 4, fondo = 8, pisos = 2, semilla = 7 });
        b.transform.position = new Vector3(4f, 0f, 0f);

        Camera cam = new GameObject("Cam").AddComponent<Camera>();
        cam.fieldOfView = 45f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.6f, 0.7f, 0.8f);
        RenderTexture rt = new RenderTexture(1600, 900, 24);
        cam.targetTexture = rt;
        Texture2D tx = new Texture2D(1600, 900);
        Vector3[] puntos = { new Vector3(-10f, 6f, 18f), new Vector3(12f, 8f, -16f), new Vector3(0f, 22f, 8f) };
        for (int i = 0; i < puntos.Length; i++)
        {
            cam.transform.position = puntos[i];
            cam.transform.LookAt(new Vector3(0f, 4f, 0f));
            cam.Render();
            RenderTexture.active = rt;
            tx.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tx.Apply();
            System.IO.File.WriteAllBytes("Temp/capturas/casa_" + i + ".png", tx.EncodeToPNG());
        }
        RenderTexture.active = null;
        cam.targetTexture = null;
        SceneManager.SetActiveScene(previa);
        EditorSceneManager.CloseScene(escena, true);
        return "ok";
    }
}
