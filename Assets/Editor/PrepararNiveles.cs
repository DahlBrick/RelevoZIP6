using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Prepara las escenas de la fase final: crea Nivel2_PatioDeBombas y Nivel3_FaroDeLiria con su
/// LevelBootstrap configurado, las hornea con la misma herramienta que el Muelle, monta la escena
/// del Cuaderno y deja las cuatro en Build Settings.
///
/// El reparto en dos pasos no es casual: LevelLayouts es la única fuente de verdad del diseño
/// espacial, así que añadir un nivel es añadir su mapa y su índice aquí, no editar una escena a
/// mano. Y el horneado actúa siempre sobre la escena ACTIVA, de modo que cada nivel necesita su
/// propia pasada.
///
/// La herramienta es idempotente: reescribir una escena ya creada la regenera entera (HornearNivel
/// borra antes todo lo marcado con ContenidoHorneado) y no duplica entradas en Build Settings.
/// </summary>
public static class PrepararNiveles
{
    const string CarpetaEscenas = "Assets/Scenes/Juego";
    const string RutaEntrada = "Assets/Input/Relevo.inputactions";

    const string Escena2 = "Nivel2_PatioDeBombas";
    const string Escena3 = "Nivel3_FaroDeLiria";
    const string EscenaCuaderno = "Cuaderno";

    [MenuItem("Relevo/Preparar Niveles 2, 3 y Cuaderno")]
    public static void Preparar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("RELEVO preparar niveles: sal de Play Mode antes de preparar las escenas.");
            return;
        }

        Scene origen = SceneManager.GetActiveScene();
        if (origen.IsValid() && origen.isDirty)
        {
            // Se comprueba en vez de preguntar: abrir una escena nueva con cambios sin guardar
            // abriría un diálogo modal y esta herramienta tiene que poder lanzarse sin nadie
            // delante. No se guarda nada por iniciativa propia —el Muelle no debe tocarse—.
            Debug.LogWarning("RELEVO preparar niveles: la escena '" + origen.name
                + "' tiene cambios sin guardar. Guárdala (o ábrela de nuevo) y vuelve a ejecutar "
                + "la herramienta; la escena activa se va a reemplazar.");
            return;
        }

        if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(RutaEntrada)))
        {
            Debug.LogError("RELEVO preparar niveles: no existe " + RutaEntrada + ".");
            return;
        }

        string rutaOrigen = origen.IsValid() ? origen.path : "";

        AsegurarCarpeta(CarpetaEscenas);

        CrearNivel(2, Escena2);
        CrearNivel(3, Escena3);
        CrearCuaderno();

        AnadirAEscenasDelBuild(
            CarpetaEscenas + "/" + Escena2 + ".unity",
            CarpetaEscenas + "/" + Escena3 + ".unity",
            CarpetaEscenas + "/" + EscenaCuaderno + ".unity");

        // Se devuelve al usuario a la escena en la que estaba, para no dejarlo en el Cuaderno.
        if (!string.IsNullOrEmpty(rutaOrigen))
        {
            EditorSceneManager.OpenScene(rutaOrigen, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("RELEVO preparar niveles: '" + Escena2 + "', '" + Escena3 + "' y '" + EscenaCuaderno
            + "' listas y en Build Settings. Volver a la fase de jugar cada nivel para la QA.");
    }

    /// <summary>
    /// Crea la escena del nivel con su LevelBootstrap apuntando al índice pedido y la hornea.
    /// El índice es un campo privado serializado, así que se escribe con SerializedObject, igual
    /// que hace HornearNivel con 'escenaHorneada'.
    /// </summary>
    static void CrearNivel(int indice, string nombreEscena)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject go = new GameObject("LevelBootstrap");
        LevelBootstrap bootstrap = go.AddComponent<LevelBootstrap>();

        SerializedObject serializado = new SerializedObject(bootstrap);

        SerializedProperty indiceProp = serializado.FindProperty("indiceNivel");
        if (indiceProp != null) indiceProp.intValue = indice;
        else Debug.LogError("RELEVO preparar niveles: no se encontró 'indiceNivel' en LevelBootstrap.");

        serializado.ApplyModifiedPropertiesWithoutUndo();

        AsignarEntrada(bootstrap, "acciones");

        string ruta = CarpetaEscenas + "/" + nombreEscena + ".unity";
        if (!EditorSceneManager.SaveScene(escena, ruta))
        {
            Debug.LogError("RELEVO preparar niveles: no se pudo guardar " + ruta
                + ". Revisa que OneDrive no esté bloqueando el archivo.");
            return;
        }

        // Hornear actúa sobre la escena activa, que es la que se acaba de abrir y guardar.
        HornearNivel.Hornear();
    }

    /// <summary>
    /// Monta la pantalla del Cuaderno. No lleva LevelBootstrap a propósito: no hay nivel que
    /// construir, solo una UI que lee el progreso guardado.
    /// </summary>
    static void CrearCuaderno()
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Sin cámara ni AudioListener la escena avisa por consola al entrar en Play Mode, aunque
        // todo lo que se ve sea UI en modo Screen Space - Overlay.
        GameObject goCamara = new GameObject("Camara");
        Camera camara = goCamara.AddComponent<Camera>();
        camara.orthographic = true;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = RelevoPalette.ColorDe(ClaveMaterial.Neblina);
        goCamara.AddComponent<AudioListener>();
        goCamara.tag = "MainCamera";

        GameObject goUI = new GameObject("Cuaderno");
        CuadernoUI ui = goUI.AddComponent<CuadernoUI>();

        AsignarEntrada(ui, "acciones");

        string ruta = CarpetaEscenas + "/" + EscenaCuaderno + ".unity";
        if (!EditorSceneManager.SaveScene(escena, ruta))
        {
            Debug.LogError("RELEVO preparar niveles: no se pudo guardar " + ruta + ".");
        }
    }

    /// <summary>
    /// Escribe la referencia al asset de entrada. El asset se carga AQUÍ y no al principio de la
    /// herramienta a propósito: EditorSceneManager.NewScene descarga los assets que no referencia
    /// nadie, así que una referencia cargada antes de crear la escena llega ya destruida y la
    /// asignación se guarda como null sin que la API avise. Por eso además se relee para
    /// comprobarlo, en lugar de darla por buena.
    /// </summary>
    static void AsignarEntrada(Object componente, string campo)
    {
        InputActionAsset acciones = AssetDatabase.LoadAssetAtPath<InputActionAsset>(RutaEntrada);
        if (acciones == null)
        {
            Debug.LogError("RELEVO preparar niveles: no se pudo cargar " + RutaEntrada + ".");
            return;
        }

        SerializedObject serializado = new SerializedObject(componente);
        SerializedProperty propiedad = serializado.FindProperty(campo);

        if (propiedad == null)
        {
            Debug.LogError("RELEVO preparar niveles: no se encontró el campo '" + campo + "' en "
                + componente.GetType().Name + ".");
            return;
        }

        propiedad.objectReferenceValue = acciones;
        serializado.ApplyModifiedPropertiesWithoutUndo();

        // Instancia nueva: así se comprueba el objeto y no el SerializedObject que se acaba de tocar.
        SerializedObject comprobacion = new SerializedObject(componente);
        if (comprobacion.FindProperty(campo).objectReferenceValue == null)
        {
            Debug.LogError("RELEVO preparar niveles: la referencia a " + RutaEntrada
                + " no llegó a escribirse en " + componente.GetType().Name
                + "; esa escena se quedaría sin entrada.");
        }
    }

    /// <summary>
    /// Añade las escenas a Build Settings conservando las que ya estaban y sin duplicar entradas.
    /// Sin esto, SceneManager.LoadScene no encuentra la escena y el juego avisa de que falta.
    /// </summary>
    static void AnadirAEscenasDelBuild(params string[] rutas)
    {
        List<EditorBuildSettingsScene> escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int anadidas = 0;

        for (int i = 0; i < rutas.Length; i++)
        {
            bool ya = false;
            for (int e = 0; e < escenas.Count; e++)
            {
                if (escenas[e].path == rutas[i])
                {
                    ya = true;
                    break;
                }
            }

            if (ya) continue;

            escenas.Add(new EditorBuildSettingsScene(rutas[i], true));
            anadidas++;
        }

        if (anadidas == 0)
        {
            Debug.Log("RELEVO preparar niveles: las cuatro escenas ya estaban en Build Settings.");
            return;
        }

        EditorBuildSettings.scenes = escenas.ToArray();
        Debug.Log("RELEVO preparar niveles: " + anadidas + " escena(s) añadida(s) a Build Settings; "
            + escenas.Count + " en total.");
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
}
