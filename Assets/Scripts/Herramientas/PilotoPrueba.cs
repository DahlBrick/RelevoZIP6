#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// Herramienta de pruebas (solo editor): mantiene pulsadas unas teclas simuladas durante un
/// tiempo, aunque el editor no tenga el foco. La usan las pruebas automáticas del Patio de Bombas.
/// </summary>
public sealed class PilotoPrueba : MonoBehaviour
{
    public static string Estado = "";
    Key[] teclas;
    float segundos;

    public static string Pulsar(float segundos, params Key[] teclas)
    {
        GameObject go = new GameObject("Piloto de prueba");
        PilotoPrueba p = go.AddComponent<PilotoPrueba>();
        p.teclas = teclas;
        p.segundos = segundos;
        Estado = "pulsando";
        return "ok";
    }

    IEnumerator Start()
    {
        var ajustes = InputSystem.settings;
        var previoEditor = ajustes.editorInputBehaviorInPlayMode;
        var previoFondo = ajustes.backgroundBehavior;
        ajustes.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        ajustes.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        if (Keyboard.current != null && !Keyboard.current.enabled) InputSystem.EnableDevice(Keyboard.current);
        yield return null;
        yield return null;

        float fin = Time.time + segundos;
        while (Time.time < fin)
        {
            if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(teclas));
            yield return null;
        }
        if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        yield return null;
        yield return null;

        ajustes.editorInputBehaviorInPlayMode = previoEditor;
        ajustes.backgroundBehavior = previoFondo;
        Estado = "listo";
        Destroy(gameObject);
    }
}
#endif
