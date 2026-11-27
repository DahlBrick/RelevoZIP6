#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor.Media;
using UnityEngine;

/// <summary>Herramienta temporal: graba a MP4 (sin interfaz) la cámara activa de mayor profundidad.</summary>
public sealed class GrabadorCamara : MonoBehaviour
{
    public static string Estado = "";
    string ruta; float segundos;

    public static void Grabar(string ruta, float segundos)
    {
        var g = new GameObject("Grabador").AddComponent<GrabadorCamara>();
        DontDestroyOnLoad(g.gameObject);
        g.ruta = ruta; g.segundos = segundos;
    }

    /// <summary>Deja pedida la grabación para la próxima vez que se pulse Play.</summary>
    public static string Pedir(string ruta, float segundos)
    {
        UnityEditor.SessionState.SetString("GrabadorCamaraRuta", ruta);
        UnityEditor.SessionState.SetFloat("GrabadorCamaraSeg", segundos);
        return ruta;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AlArrancar()
    {
        string r = UnityEditor.SessionState.GetString("GrabadorCamaraRuta", "");
        if (string.IsNullOrEmpty(r)) return;
        UnityEditor.SessionState.EraseString("GrabadorCamaraRuta");
        Grabar(r, UnityEditor.SessionState.GetFloat("GrabadorCamaraSeg", 20f));
    }

    /// <summary>Foto de la cámara activa a PNG (para revisar planos sin grabar vídeo).</summary>
    public static string Foto(string ruta, int ancho = 960, int alto = 540)
    {
        Camera c = Activa();
        if (c == null) return "sin camara";
        var rt = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
        var img = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
        c.targetTexture = rt; c.Render(); c.targetTexture = null;
        RenderTexture prev = RenderTexture.active; RenderTexture.active = rt;
        img.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0, false); img.Apply(false);
        RenderTexture.active = prev; rt.Release();
        Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        File.WriteAllBytes(ruta, img.EncodeToPNG());
        return ruta;
    }

    static Camera Activa()
    {
        Camera mejor = null;
        foreach (Camera c in Camera.allCameras)
        {
            if (!c.enabled || c.targetTexture != null) continue;
            if ((c.cullingMask & ~(1 << 5)) == 0) continue;   // solo UI
            if (mejor == null || c.depth > mejor.depth) mejor = c;
        }
        return mejor;
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        if (File.Exists(ruta)) File.Delete(ruta);
        var enc = new MediaEncoder(ruta, new VideoTrackAttributes
        {
            frameRate = new MediaRational(30), width = 1920, height = 1080,
            includeAlpha = false, bitRateMode = UnityEditor.VideoBitrateMode.High
        });
        var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
        var img = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
        Time.captureFramerate = 30;
        int total = Mathf.RoundToInt(segundos * 30f);
        for (int i = 0; i < total; i++)
        {
            yield return new WaitForEndOfFrame();
            Camera c = Activa();
            if (c != null)
            {
                c.targetTexture = rt; c.Render(); c.targetTexture = null;
                RenderTexture prev = RenderTexture.active; RenderTexture.active = rt;
                img.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0, false); img.Apply(false);
                RenderTexture.active = prev;
            }
            enc.AddFrame(img);
            Estado = "grabando " + i + "/" + total + " " + (c != null ? c.name : "-");
        }
        enc.Dispose();
        Time.captureFramerate = 0;
        rt.Release();
        Estado = "grabado " + ruta;
        Destroy(gameObject);
    }
}
#endif
