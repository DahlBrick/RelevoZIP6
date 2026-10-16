using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Pasa al estilo cómic de Liria (RELEVO/Comic + contorno de tinta de Liria_Renderer) todo lo que
/// cuelga de una raíz construida en tiempo de ejecución, como la apertura.
///
/// - Los materiales opacos de URP/Lit cambian de shader a RELEVO/Comic. Los nombres de propiedad
///   coinciden (_BaseColor, _BaseMap, _EmissionColor), así que se conservan color, textura y
///   emisión, y el código que anima la emisión (ventanas, faroles, antena, dial de la radio) sigue
///   funcionando sobre el mismo material.
/// - Un material creado en ejecución (id negativo) se cambia en el sitio. Un material que es un
///   asset (id positivo, p. ej. los de Nilo o del kit) se clona antes: así nunca se modifica en
///   disco y los niveles siguen como estaban.
/// - Se dejan como están los transparentes, las partículas, el agua y el follaje recortado.
/// </summary>
public static class EstiloComic
{
    static readonly Color Sombra = new Color(0.42f, 0.38f, 0.58f);

    public static int Aplicar(Transform raiz, Color? colorSombra = null)
    {
        Shader comic = Shader.Find("RELEVO/Comic");
        if (comic == null || raiz == null) return 0;
        var clones = new Dictionary<Material, Material>();
        int cambiados = 0;

        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
            Material[] ms = r.sharedMaterials;
            bool tocado = false;
            for (int i = 0; i < ms.Length; i++)
            {
                Material m = ms[i];
                if (!Convertible(m)) continue;
                if (m.GetInstanceID() > 0)
                {
                    if (!clones.TryGetValue(m, out Material copia))
                    {
                        copia = new Material(m) { name = m.name + " (cómic)" };
                        Convertir(copia, comic, colorSombra);
                        clones[m] = copia;
                    }
                    ms[i] = copia;
                    tocado = true;
                }
                else
                {
                    Convertir(m, comic, colorSombra);
                }
                cambiados++;
            }
            if (tocado) r.sharedMaterials = ms;
        }
        return cambiados;
    }

    static bool Convertible(Material m)
    {
        if (m == null || m.shader == null) return false;
        string n = m.shader.name;
        if (n == "RELEVO/Comic") return false;
        if (!n.StartsWith("Universal Render Pipeline/Lit") && !n.StartsWith("Universal Render Pipeline/Simple Lit")) return false;
        if (m.renderQueue >= (int)RenderQueue.AlphaTest) return false;   // transparentes y recortados
        return true;
    }

    static void Convertir(Material m, Shader comic, Color? colorSombra)
    {
        bool emite = m.IsKeywordEnabled("_EMISSION");
        Color emision = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
        m.shader = comic;
        m.SetColor("_EmissionColor", emite ? emision : Color.black);
        m.SetColor("_ColorSombra", colorSombra ?? Sombra);
    }

    /// <summary>Pone la cámara en el renderer con el contorno de tinta (Liria_Renderer), si existe.</summary>
    public static void RendererTinta(Camera camara)
    {
        if (camara == null || !(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)) return;
        var lista = urp.rendererDataList;
        for (int i = 0; i < lista.Length; i++)
        {
            if (lista[i] != null && lista[i].name == "Liria_Renderer")
            {
                camara.GetUniversalAdditionalCameraData().SetRenderer(i);
                return;
            }
        }
    }
}
