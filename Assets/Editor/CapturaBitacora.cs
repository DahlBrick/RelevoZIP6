using System.IO;
using UnityEngine;

/// <summary>
/// Capturas para la bitácora de la mejora gráfica (docs/bitacora/img). Renderiza una cámara a
/// una textura y la guarda como PNG en una ruta relativa a la carpeta del proyecto, de modo que
/// la imagen no cae dentro de Assets y no se importa como textura.
///
/// Se invoca desde la CLI de Unity: CapturaBitacora.Capturar("docs/bitacora/img/x.png", 1600, 900).
/// Sin cámara explícita usa la que tenga la etiqueta MainCamera o, si no, la primera activa.
/// </summary>
public static class CapturaBitacora
{
    public static string Capturar(string rutaRelativa, int ancho, int alto, Camera camara = null)
    {
        if (camara == null) camara = Camera.main;
        if (camara == null)
        {
            Camera[] todas = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (int i = 0; i < todas.Length && camara == null; i++)
            {
                if (todas[i].isActiveAndEnabled) camara = todas[i];
            }
        }

        if (camara == null) return "sin camara";

        RenderTexture objetivo = RenderTexture.GetTemporary(ancho, alto, 24, RenderTextureFormat.ARGB32);
        objetivo.antiAliasing = 4;

        RenderTexture previoCamara = camara.targetTexture;
        RenderTexture previoActivo = RenderTexture.active;

        camara.targetTexture = objetivo;
        camara.Render();

        RenderTexture.active = objetivo;
        Texture2D imagen = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
        imagen.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
        imagen.Apply();

        camara.targetTexture = previoCamara;
        RenderTexture.active = previoActivo;
        RenderTexture.ReleaseTemporary(objetivo);

        string ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", rutaRelativa));
        Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        File.WriteAllBytes(ruta, imagen.EncodeToPNG());
        Object.DestroyImmediate(imagen);

        return ruta;
    }
}
