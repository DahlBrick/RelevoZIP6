using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Casas de Liria levantadas con las piezas modulares del Medieval Village MegaKit de Quaternius
/// (Assets/ThirdParty/Quaternius_Village, CC0).
///
/// Medidas del kit: la rejilla es de 2 m, cada piso mide 3,12 m y la cara exterior de cada pieza
/// mira a +z local. Una casa tiene 'ancho' metros de fachada (x) y 'fondo' metros (z), ambos pares,
/// y su fachada principal mira a +z: es la que se pone frente al canal. El tejado de teja tiene la
/// cumbrera perpendicular a la fachada, así que cada casa enseña su frontón al agua, como en un
/// puerto antiguo.
///
/// Todos los cristales de una casa comparten un material propio (<see cref="VentanasCasa"/>), así
/// que se encienden y se apagan juntos: es lo que usa el apagón de la cinemática.
/// </summary>
public static class CasaModular
{
    public const float Piso = 3.12f;
    const float Grosor = 0.095f;

    public struct Plano
    {
        public int ancho, fondo, pisos, semilla;
        public bool balcon;
    }

    static readonly int[] Anchos = { 4, 6, 8 };

    /// <summary>Un plano de casa al azar con medidas que el kit sabe cubrir con un tejado.</summary>
    public static Plano Azar(System.Random azar)
    {
        int ancho = Anchos[azar.Next(Anchos.Length)];
        int fondo = ancho == 8 ? 8 + 2 * azar.Next(2) : 6 + 2 * azar.Next(2);
        return new Plano { ancho = ancho, fondo = fondo, pisos = 2 + azar.Next(2), semilla = azar.Next() };
    }

    public static GameObject Construir(Transform padre, System.Func<string, GameObject> modelo, Plano p)
    {
        System.Random azar = new System.Random(p.semilla);
        GameObject raiz = new GameObject("Casa " + p.ancho + "x" + p.fondo);
        if (padre != null) raiz.transform.SetParent(padre, false);
        VentanasCasa ventanas = raiz.AddComponent<VentanasCasa>();

        float w = p.ancho * 0.5f, d = p.fondo * 0.5f;
        bool madera = azar.Next(3) == 0;               // planta alta con entramado de madera
        int celdaPuerta = azar.Next(p.ancho / 2);

        for (int piso = 0; piso < p.pisos; piso++)
        {
            float y = piso * Piso;
            // Fachada al canal.
            for (int i = 0; i < p.ancho / 2; i++)
            {
                Vector3 pos = new Vector3(-w + 1f + 2f * i, y, d - Grosor);
                if (piso == 0 && i == celdaPuerta)
                {
                    Pieza(raiz.transform, modelo, "Wall_UnevenBrick_Door_Round", pos, 0f);
                    Pieza(raiz.transform, modelo, "Door_1_Round", pos, 0f);
                }
                else if (piso == 0)
                {
                    Pieza(raiz.transform, modelo, "Wall_UnevenBrick_Window_Wide_Round", pos, 0f);
                    Pieza(raiz.transform, modelo, "Window_Wide_Round1", pos, 0f);
                }
                else
                {
                    string muro = madera && azar.Next(2) == 0 ? "Wall_Plaster_Window_Wide_Flat2" : "Wall_Plaster_Window_Wide_Flat";
                    Pieza(raiz.transform, modelo, muro, pos, 0f);
                    Pieza(raiz.transform, modelo, "Window_Wide_Flat1", pos, 0f);
                    if (azar.Next(3) == 0) Pieza(raiz.transform, modelo, "WindowShutters_Wide_Flat_Open", pos, 0f);
                }
            }
            // Trasera.
            for (int i = 0; i < p.ancho / 2; i++)
            {
                Vector3 pos = new Vector3(-w + 1f + 2f * i, y, -d + Grosor);
                bool ventana = piso > 0 && azar.Next(2) == 0;
                Pieza(raiz.transform, modelo, piso == 0 ? "Wall_UnevenBrick_Straight" : (ventana ? "Wall_Plaster_Window_Thin_Round" : "Wall_Plaster_Straight"), pos, 180f);
                if (ventana) Pieza(raiz.transform, modelo, "Window_Thin_Round1", pos, 180f);
            }
            // Laterales.
            for (int j = 0; j < p.fondo / 2; j++)
            {
                float z = -d + 1f + 2f * j;
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    Vector3 pos = new Vector3(lado * (w - Grosor), y, z);
                    bool ventana = piso > 0 && azar.Next(3) == 0;
                    string muro = piso == 0 ? "Wall_UnevenBrick_Straight" : (madera ? "Wall_Plaster_WoodGrid" : (ventana ? "Wall_Plaster_Window_Thin_Round" : "Wall_Plaster_Straight"));
                    Pieza(raiz.transform, modelo, muro, pos, lado * 90f);
                    if (ventana && !madera) Pieza(raiz.transform, modelo, "Window_Thin_Round1", pos, lado * 90f);
                }
            }
            // Esquinas.
            for (int ex = -1; ex <= 1; ex += 2)
                for (int ez = -1; ez <= 1; ez += 2)
                    Pieza(raiz.transform, modelo, piso == 0 ? "Corner_Exterior_Brick" : "Corner_Exterior_Wood",
                        new Vector3(ex * w, y, ez * d), Mathf.Atan2(ex, ez) * Mathf.Rad2Deg - 45f);
        }

        // Tejado de teja con su frontón delante y detrás.
        float alto = p.pisos * Piso;
        Pieza(raiz.transform, modelo, "Roof_RoundTiles_" + p.ancho + "x" + p.fondo, new Vector3(0f, alto, 0f), 0f);
        Pieza(raiz.transform, modelo, "Roof_Front_Brick" + p.ancho, new Vector3(0f, alto, d - Grosor), 0f);
        Pieza(raiz.transform, modelo, "Roof_Front_Brick" + p.ancho, new Vector3(0f, alto, -d + Grosor), 180f);
        if (azar.Next(2) == 0)
            Pieza(raiz.transform, modelo, azar.Next(2) == 0 ? "Prop_Chimney" : "Prop_Chimney2",
                new Vector3((azar.Next(2) == 0 ? -1f : 1f) * (w * 0.5f), alto + 0.6f, -d * 0.4f), 0f);

        // Balcón en el primer piso, a veces.
        if (p.pisos > 1 && (p.balcon || azar.Next(3) == 0))
            Pieza(raiz.transform, modelo, "Balcony_Simple_Straight", new Vector3(-w + 1f + 2f * (azar.Next(p.ancho / 2)), Piso, d), 0f);

        ventanas.Recoger();
        return raiz;
    }

    static GameObject Pieza(Transform padre, System.Func<string, GameObject> modelo, string nombre, Vector3 pos, float giro)
    {
        GameObject m = modelo != null ? modelo(nombre) : null;
        if (m == null) return null;
        GameObject g = Object.Instantiate(m, padre);
        g.name = nombre;
        foreach (Collider c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        // Las piezas del kit traen su propio giro de importación (ejes de Blender): se conserva.
        g.transform.localPosition = pos;
        g.transform.localRotation = Quaternion.Euler(0f, giro, 0f) * m.transform.localRotation;
        return g;
    }
}

/// <summary>
/// Los cristales de una casa: un material propio compartido por todas sus ventanas, para
/// encenderlas, apagarlas o hacerlas titilar a la vez.
/// </summary>
public sealed class VentanasCasa : MonoBehaviour
{
    public Material Cristal { get; private set; }
    public readonly List<Renderer> Renderers = new List<Renderer>();
    static readonly Color Calida = new Color(1f, 0.7f, 0.36f);

    public void Recoger()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            Material[] ms = r.sharedMaterials;
            bool cambia = false;
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i] == null || !(ms[i].name.Contains("WindowGlass") || ms[i].name.Contains("Cristal"))) continue;
                if (Cristal == null)
                {
                    Cristal = new Material(ms[i]);
                    Cristal.EnableKeyword("_EMISSION");
                    Cristal.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
                ms[i] = Cristal;
                cambia = true;
            }
            if (cambia) { r.sharedMaterials = ms; Renderers.Add(r); }
        }
        Encender(0f);
    }

    /// <summary>0 apagadas (cristal oscuro), 1 encendidas (luz cálida de dentro).</summary>
    public void Encender(float k)
    {
        if (Cristal == null) return;
        Cristal.SetColor("_EmissionColor", Calida * (2.4f * k));
    }
}
