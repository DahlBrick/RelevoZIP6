using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// La Liria nueva (la de Liria_Prueba, construida en Blender con construir_liria.py) de noche, para
/// la parte A de la apertura. Ofrece lo mismo que la Liria antigua a la cinemática: luces que se
/// cortan de lejos a cerca, el farol del detalle y el último farol, la rueda de una bomba, la barca
/// con una vela, el balcón, la antena roja de la radio, el agua que sube y las chispas.
///
/// Las ventanas de cada bloque comparten un material de cristal propio (copia de LIRIA_Cristal):
/// cada bloque es una tanda del apagón, y los faroles caen uno a uno.
/// Coordenadas: los bloques cuelgan de una raíz girada 180°, como en Liria_Prueba; en el mundo, x es
/// la x de Blender y z la y de Blender.
/// </summary>
public sealed partial class LiriaNocturna
{
    static readonly Color Calida = new Color(1f, 0.68f, 0.32f);

    bool aguaAncha;

    public static LiriaNocturna ConstruirNueva(Transform padre, System.Func<string, GameObject> modelo, KitRelevo kit, IList<GameObject> bloques)
    {
        LiriaNocturna l = new LiriaNocturna(modelo, kit);
        l.LevantarNueva(padre, bloques);
        return l;
    }

    void LevantarNueva(Transform padre, IList<GameObject> bloques)
    {
        Raiz = new GameObject("Liria nocturna (nueva)").transform;
        Raiz.SetParent(padre, false);
        aguaAncha = true;

        Transform liria = new GameObject("Liria").transform;
        liria.SetParent(Raiz, false);
        liria.rotation = Quaternion.Euler(0f, 180f, 0f);
        int semilla = 0;
        foreach (GameObject prefab in bloques)
        {
            if (prefab == null || prefab.name.Contains("Limites")) continue;
            GameObject g = Object.Instantiate(prefab, liria);
            VentanasDelBloque(g, semilla++);
        }

        // Mar: un plano grande a la altura del agua de Blender (-1,5 m).
        Material mar = kit != null && kit.agua != null ? new Material(kit.agua) : Liso(new Color(0.04f, 0.08f, 0.14f));
        GameObject plano = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Object.Destroy(plano.GetComponent<Collider>());
        plano.name = "Agua";
        plano.transform.SetParent(Raiz, false);
        plano.transform.position = new Vector3(0f, -1.5f, 0f);
        plano.transform.localScale = Vector3.one * 140f;
        plano.GetComponent<Renderer>().sharedMaterial = mar;
        agua = plano.transform;
        alturaAgua0 = -1.5f;

        // Los faroles salen de Liria_Faroles (un hijo "Farol_..." por farol, con el origen en su base):
        // si se mueve uno en la escena o en Blender, su luz se enciende y se apaga donde está.
        Luz detalle = null;
        foreach (Transform t in liria.GetComponentsInChildren<Transform>())
        {
            if (!t.name.StartsWith("Farol_") || t.name.EndsWith("_Malla")) continue;
            // La llama va donde está la malla del farol (su parte alta), no donde está el vacío:
            // así sigue al farol aunque se haya movido a mano la malla o el grupo entero.
            Renderer malla = t.GetComponentInChildren<Renderer>();
            if (malla == null) continue;
            Bounds b = malla.bounds;
            Vector3 llama = new Vector3(b.center.x, b.max.y - (t.name.StartsWith("Farol_Piedra") ? 0.35f : 0.3f), b.center.z);
            Luz l = FarolNuevo(llama);
            if (detalle == null || Cerca(llama, new Vector3(0.7f, 0f, -4f), detalle.posicion)) detalle = l;
            if (UltimoFarol == null || Cerca(llama, new Vector3(-2.6f, 0f, -14f), UltimoFarol.posicion)) UltimoFarol = l;
        }
        if (detalle != null) FarolDetalle = detalle.posicion;

        RuedaEnElCanal(new Vector3(24.7f, -0.9f, 6f));
        BarcaConVela(new Vector3(29.2f, -1.45f, 9f));
        Balcon = new Vector3(-1.5f, 4.6f, 38.4f);
        AntenaEn(new Vector3(6f, -1.5f, 60f));
        Cielo();
        Chispas();

        // Orden de corte: de lo más lejano (barrio de piedra, +z) a lo más cercano a la cámara del apagón, que está en el mar (-z).
        foreach (Luz l in Luces) l.orden = Mathf.InverseLerp(50f, -30f, l.posicion.z);
        Luces.Sort((a, b) => a.orden.CompareTo(b.orden));
    }

    /// <summary>Las ventanas de un bloque se encienden con un cristal propio: una tanda del apagón.</summary>
    void VentanasDelBloque(GameObject bloque, int semilla)
    {
        Luz luz = null;
        Material cristal = null;
        foreach (Renderer r in bloque.GetComponentsInChildren<Renderer>())
        {
            Material[] ms = r.sharedMaterials;
            bool cambia = false;
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i] == null || !ms[i].name.StartsWith("LIRIA_Cristal")) continue;
                if (cristal == null)
                {
                    cristal = new Material(ms[i]) { name = "Ventanas " + bloque.name };
                    luz = new Luz { emision = Calida * 2.4f, intensidad = 2.2f, semilla = semilla * 7.3f, posicion = r.bounds.center };
                    luz.materiales.Add(cristal);
                    luz.luz = PuntoDeLuz(Raiz, r.bounds.center + Vector3.up * 3f, Calida, luz.intensidad, 22f);
                    Luces.Add(luz);
                }
                ms[i] = cristal;
                cambia = true;
            }
            if (cambia) r.sharedMaterials = ms;
        }
    }

    /// <summary>¿Está 'a' más cerca que 'b' del punto de referencia (en planta)?</summary>
    static bool Cerca(Vector3 a, Vector3 referencia, Vector3 b)
    {
        Vector2 r = new Vector2(referencia.x, referencia.z);
        return Vector2.Distance(new Vector2(a.x, a.z), r) < Vector2.Distance(new Vector2(b.x, b.z), r);
    }

    Luz FarolNuevo(Vector3 llama)
    {
        Material m = Emisivo(Calida, 3f);
        GameObject bombilla = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(bombilla.GetComponent<Collider>());
        bombilla.name = "Llama";
        bombilla.transform.SetParent(Raiz, false);
        bombilla.transform.position = llama;
        bombilla.transform.localScale = Vector3.one * 0.2f;
        bombilla.GetComponent<Renderer>().sharedMaterial = m;
        bombilla.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Luz l = new Luz { emision = Calida * 3f, intensidad = 2.6f, semilla = llama.x * 1.7f + llama.z, posicion = llama };
        l.materiales.Add(m);
        l.luz = PuntoDeLuz(Raiz, llama, Calida, l.intensidad, 8f);
        Luces.Add(l);
        return l;
    }

    /// <summary>Rueda de paletas de una bomba, medio sumergida contra la orilla oeste del canal.</summary>
    void RuedaEnElCanal(Vector3 centro)
    {
        Material madera = Liso(new Color(0.42f, 0.29f, 0.19f));
        Rueda = new GameObject("Rueda de la bomba").transform;
        Rueda.SetParent(Raiz, false);
        Rueda.position = centro;
        GameObject eje = Caja("Eje", centro, new Vector3(1.2f, 0.25f, 0.25f), madera);
        eje.transform.SetParent(Rueda, true);
        for (int i = 0; i < 8; i++)
        {
            Quaternion q = Quaternion.Euler(i * 45f, 0f, 0f);
            GameObject radio = Caja("Radio", centro + q * Vector3.up * 0.8f, new Vector3(0.12f, 1.6f, 0.12f), madera);
            radio.transform.rotation = q;
            radio.transform.SetParent(Rueda, true);
            GameObject pala = Caja("Pala", centro + q * Vector3.up * 1.55f, new Vector3(0.9f, 0.08f, 0.55f), madera);
            pala.transform.rotation = q;
            pala.transform.SetParent(Rueda, true);
        }
    }

    /// <summary>Una barca amarrada en el canal con una vela encendida en la proa.</summary>
    void BarcaConVela(Vector3 pos)
    {
        Material madera = Liso(new Color(0.5f, 0.34f, 0.22f));
        Barca = new GameObject("Barca").transform;
        Barca.SetParent(Raiz, false);
        Barca.position = pos;
        Caja("Casco", pos, new Vector3(1.1f, 0.45f, 3.0f), madera).transform.SetParent(Barca, true);
        Caja("Banco", pos + new Vector3(0f, 0.25f, 0.3f), new Vector3(1.0f, 0.08f, 0.3f), madera).transform.SetParent(Barca, true);
        Caja("Vela", pos + new Vector3(0f, 0.38f, 1.1f), new Vector3(0.08f, 0.22f, 0.08f), Liso(new Color(0.9f, 0.86f, 0.75f))).transform.SetParent(Barca, true);
        Material llama = Emisivo(Calida, 4f);
        GameObject f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(f.GetComponent<Collider>());
        f.transform.SetParent(Barca, false);
        f.transform.position = pos + new Vector3(0f, 0.55f, 1.1f);
        f.transform.localScale = new Vector3(0.06f, 0.11f, 0.06f);
        f.GetComponent<Renderer>().sharedMaterial = llama;
        Luz l = new Luz { emision = Calida * 4f, intensidad = 1.4f, semilla = 3.3f, posicion = f.transform.position };
        l.materiales.Add(llama);
        l.luz = PuntoDeLuz(Barca, f.transform.position, Calida, l.intensidad, 4f);
        Luces.Add(l);
        flotan.Add(Barca);
    }
}
