using System.Collections.Generic;
using UnityEngine;

/// <summary>Claves de la paleta de RELEVO.</summary>
public enum ClaveMaterial
{
    PiedraCrema,
    PiedraBorde,
    MaderaOscura,
    Cobre,
    Agua,
    Vegetacion,
    Ambar,
    AmbarSuave,
    Turquesa,
    Coral,
    NiloCarcasa,
    NiloExtremidades,
    NiloPantalla,
    NiloOjos,
    NiloBufanda,
    MaiaCarcasa,
    MaiaPantalla,
    MaiaOjos,
    Maqueta,
    Fondo,
    Neblina
}

/// <summary>
/// Paleta única del proyecto. Los materiales se crean una vez en tiempo de
/// ejecución con el shader URP/Lit activo, de modo que la geometría modular,
/// los actores y los efectos comparten exactamente los mismos colores.
/// </summary>
public static class RelevoPalette
{
    static readonly Dictionary<ClaveMaterial, Material> materiales = new Dictionary<ClaveMaterial, Material>();

    // nombre hex, metallic, smoothness, intensidad emisiva
    static readonly Dictionary<ClaveMaterial, Color> colores = new Dictionary<ClaveMaterial, Color>
    {
        { ClaveMaterial.PiedraCrema, Hex("E8DEC6") },
        { ClaveMaterial.PiedraBorde, Hex("CFC2A6") },
        { ClaveMaterial.MaderaOscura, Hex("4A3527") },
        { ClaveMaterial.Cobre, Hex("6E5233") },
        { ClaveMaterial.Agua, Hex("1E4A48") },
        { ClaveMaterial.Vegetacion, Hex("5C7A4A") },
        { ClaveMaterial.Ambar, Hex("FFB347") },
        { ClaveMaterial.AmbarSuave, Hex("F6C878") },
        { ClaveMaterial.Turquesa, Hex("2FB6A8") },
        { ClaveMaterial.Coral, Hex("E0715C") },
        { ClaveMaterial.NiloCarcasa, Hex("EDE6DA") },
        { ClaveMaterial.NiloExtremidades, Hex("2A3B5C") },
        { ClaveMaterial.NiloPantalla, Hex("14212B") },
        { ClaveMaterial.NiloOjos, Hex("35E0E6") },
        { ClaveMaterial.NiloBufanda, Hex("E8792B") },
        { ClaveMaterial.MaiaCarcasa, Hex("3E5C46") },
        { ClaveMaterial.MaiaPantalla, Hex("1B2620") },
        { ClaveMaterial.MaiaOjos, Hex("9FE8B0") },
        { ClaveMaterial.Maqueta, Hex("33261C") },
        { ClaveMaterial.Fondo, Hex("233642") },
        // No es un material: es el color de la niebla y del fondo de la camara. Va mas claro que
        // 'Fondo' a proposito, para que la distancia se vaya hacia una bruma y no hacia un muro
        // oscuro: con el mar y los muelles vecinos delante, lo que se ve al fondo es agua.
        { ClaveMaterial.Neblina, Hex("455761") }
    };

    static readonly Dictionary<ClaveMaterial, float> emisivos = new Dictionary<ClaveMaterial, float>
    {
        { ClaveMaterial.Ambar, 2.4f },
        { ClaveMaterial.AmbarSuave, 1.1f },
        { ClaveMaterial.Turquesa, 0.7f },
        { ClaveMaterial.NiloOjos, 2.2f },
        { ClaveMaterial.MaiaOjos, 1.5f }
    };

    static readonly Dictionary<ClaveMaterial, float> metalicos = new Dictionary<ClaveMaterial, float>
    {
        { ClaveMaterial.Cobre, 0.55f },
        { ClaveMaterial.Turquesa, 0.35f },
        { ClaveMaterial.MaiaCarcasa, 0.4f },
        { ClaveMaterial.NiloCarcasa, 0.1f }
    };

    static readonly Dictionary<ClaveMaterial, float> brillos = new Dictionary<ClaveMaterial, float>
    {
        { ClaveMaterial.Agua, 0.9f },
        { ClaveMaterial.Ambar, 0.6f },
        { ClaveMaterial.Turquesa, 0.5f },
        { ClaveMaterial.Cobre, 0.45f },
        { ClaveMaterial.NiloCarcasa, 0.35f }
    };

    static Color Hex(string hex)
    {
        Color color;
        ColorUtility.TryParseHtmlString("#" + hex, out color);
        return color;
    }

    public static Color ColorDe(ClaveMaterial clave)
    {
        Color color;
        return colores.TryGetValue(clave, out color) ? color : UnityEngine.Color.magenta;
    }

    public static Material MaterialDe(ClaveMaterial clave)
    {
        Material material;
        if (materiales.TryGetValue(clave, out material) && material != null) return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;

        material = new Material(shader);
        material.name = "RELEVO_" + clave;
        material.SetColor("_BaseColor", ColorDe(clave));
        material.SetColor("_Color", ColorDe(clave));

        float metalico;
        if (metalicos.TryGetValue(clave, out metalico)) material.SetFloat("_Metallic", metalico);

        float brillo;
        if (brillos.TryGetValue(clave, out brillo)) material.SetFloat("_Smoothness", brillo);

        float emision;
        if (emisivos.TryGetValue(clave, out emision))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", ColorDe(clave) * emision);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        materiales[clave] = material;
        return material;
    }
}
