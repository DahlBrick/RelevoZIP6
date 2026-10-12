#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// Herramienta de la bitácora (solo existe en el editor; no entra en las builds): pulsa una tecla de forma simulada y saca una
/// serie de capturas de la vista de juego —con la interfaz incluida— a intervalos fijos, para
/// documentar animaciones cortas como la tecla que se hunde.
///
/// Uso desde la CLI: SecuenciaCapturas.Iniciar("docs/.../tecla", Key.E, 0.3f, 8, 0.05f)
/// </summary>
public sealed class SecuenciaCapturas : MonoBehaviour
{
    string prefijo;
    Key tecla;
    float retardo;
    int cuantas;
    float intervalo;

    public static string Iniciar(string prefijoRelativo, Key teclaAPulsar, float retardoPulsacion, int capturas, float cadaSegundos)
    {
        GameObject go = new GameObject("Secuencia de capturas");
        SecuenciaCapturas s = go.AddComponent<SecuenciaCapturas>();
        s.prefijo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", prefijoRelativo));
        s.tecla = teclaAPulsar;
        s.retardo = retardoPulsacion;
        s.cuantas = capturas;
        s.intervalo = cadaSegundos;
        Directory.CreateDirectory(Path.GetDirectoryName(s.prefijo));
        return s.prefijo;
    }

    void Start()
    {
        StartCoroutine(Ejecutar());
    }

    IEnumerator Ejecutar()
    {
        float inicio = Time.unscaledTime;
        bool pulsada = false;
        bool soltada = false;

        for (int i = 0; i < cuantas; i++)
        {
            float objetivo = inicio + i * intervalo;
            while (Time.unscaledTime < objetivo)
            {
                Pulsar(inicio, ref pulsada, ref soltada);
                yield return null;
            }

            Pulsar(inicio, ref pulsada, ref soltada);
            ScreenCapture.CaptureScreenshot(prefijo + "_" + i.ToString("00") + ".png");
            yield return null;
        }

        Destroy(gameObject);
    }

    void Pulsar(float inicio, ref bool pulsada, ref bool soltada)
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        if (!pulsada && Time.unscaledTime >= inicio + retardo)
        {
            InputSystem.QueueStateEvent(teclado, new KeyboardState(tecla));
            pulsada = true;
        }
        else if (pulsada && !soltada && Time.unscaledTime >= inicio + retardo + 0.12f)
        {
            InputSystem.QueueStateEvent(teclado, new KeyboardState());
            soltada = true;
        }
    }
}
#endif
