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
    /// <summary>
    /// Compone varias imágenes en una sola hoja (filas de 'columnas' celdas) sobre un fondo liso.
    /// Sirve para revisar de un vistazo vistas previas de modelos o sprites de UI.
    /// </summary>
    public static string HojaDeContactos(string[] rutas, string salida, Color fondo, int celda = 256, int columnas = 5)
    {
        int filas = Mathf.CeilToInt(rutas.Length / (float)columnas);
        Texture2D hoja = new Texture2D(celda * columnas, celda * filas, TextureFormat.RGBA32, false);
        Color[] relleno = new Color[hoja.width * hoja.height];
        for (int i = 0; i < relleno.Length; i++) relleno[i] = fondo;
        hoja.SetPixels(relleno);

        for (int i = 0; i < rutas.Length; i++)
        {
            if (!File.Exists(rutas[i])) continue;
            Texture2D t = new Texture2D(2, 2);
            t.LoadImage(File.ReadAllBytes(rutas[i]));

            int ox = (i % columnas) * celda;
            int oy = (filas - 1 - i / columnas) * celda;
            for (int y = 0; y < celda; y++)
            {
                for (int x = 0; x < celda; x++)
                {
                    Color c = t.GetPixelBilinear((x + 0.5f) / celda, (y + 0.5f) / celda);
                    hoja.SetPixel(ox + x, oy + y, Color.Lerp(hoja.GetPixel(ox + x, oy + y), c, c.a));
                }
            }
            Object.DestroyImmediate(t);
        }

        hoja.Apply();
        string ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", salida));
        File.WriteAllBytes(ruta, hoja.EncodeToPNG());
        Object.DestroyImmediate(hoja);
        return ruta;
    }

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
