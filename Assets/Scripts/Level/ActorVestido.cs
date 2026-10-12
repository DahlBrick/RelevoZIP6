using UnityEngine;

/// <summary>
/// Viste las piezas del puzle (batería, base, palanca, salida, recuerdo y Maia) con modelos del
/// kit visual. Igual que DioramaVestido, trabaja ENCIMA de lo que construye ActorFactory:
///  - Las anclas, los pivotes y los renderers que los scripts de juego repintan para mostrar el
///    estado (anillo de la base, indicador de la palanca, umbral de la salida, orbe del recuerdo)
///    se conservan tal cual.
///  - Las primitivas que solo eran forma se ocultan (se apaga su MeshRenderer, el objeto sigue
///    existiendo, así que cualquier búsqueda por nombre sigue funcionando).
///  - Ningún modelo añadido lleva collider.
/// </summary>
public static class ActorVestido
{
    // ------------------------------------------------------------------ batería

    /// <summary>
    /// La celda es un cilindro de máquina (Space Kit) con la carcasa en cobre y la franja de carga
    /// en ámbar luminoso. El aro de carga se sube a la tapa del modelo.
    /// </summary>
    public static void Bateria(Transform cuerpo, KitRelevo kit)
    {
        if (kit.bateria == null) return;

        Ocultar(cuerpo, "Celda", "Tapa", "Terminal izquierdo", "Terminal derecho", "Base", "Banda",
            "Guia izquierda", "Guia derecha");

        const float alto = 0.74f;
        GameObject modelo = KitRelevo.ColocarCentrado(kit.bateria, cuerpo, "Modelo bateria",
            Vector3.zero, 0f, Vector3.one * (alto / Alto(kit.bateria)));
        // Cuerpo ámbar (la energía, en la paleta del GDD) con la franja de carga luminosa.
        TenirMaquina(modelo, kit, kit.ambarMate != null ? kit.ambarMate : kit.ambarBrillante, kit.ambarBrillante, kit.oscuro);

        Mover(cuerpo, "Carga", new Vector3(0f, alto + 0.03f, 0f));
        Mover(cuerpo, "Ancla", new Vector3(0f, alto, 0f));
    }

    // ------------------------------------------------------------------ base de energía

    /// <summary>
    /// La base conserva su prisma hexagonal (es el conector del GDD) pero pasa a piedra tallada
    /// con pernos de cobre, para que se lea como pieza de la ciudad y no como un bloque marrón.
    /// </summary>
    public static void Base(Transform cuerpo, KitRelevo kit)
    {
        Material piedra = kit.piedraMuro;
        Pintar(cuerpo, "Zocalo", piedra);
        Pintar(cuerpo, "Zocalo alto", piedra);

        for (int i = 1; i <= 6; i++) Pintar(cuerpo, "Perno " + i, kit.cobre);
    }

    // ------------------------------------------------------------------ palanca

    /// <summary>La barra y su indicador siguen igual; la caja del mecanismo es una máquina de Kenney.</summary>
    public static void Palanca(Transform grupo, KitRelevo kit)
    {
        if (kit.maquina == null) return;

        Ocultar(grupo, "Placa", "Zocalo", "Guarda izquierda", "Guarda derecha", "Perno 1", "Perno 2", "Perno 3", "Perno 4");

        GameObject caja = KitRelevo.ColocarCentrado(kit.maquina, grupo, "Caja palanca",
            Vector3.zero, 90f, Vector3.one * (0.46f / Alto(kit.maquina)));
        TenirMaquina(caja, kit, kit.cobre, kit.ambarMate, kit.oscuro);
    }

    // ------------------------------------------------------------------ salida

    /// <summary>
    /// Pórtico de piedra: dos pilares de Fantasy Town, un dintel de madera, estandartes verdes y
    /// dos faroles. El umbral turquesa y el halo, que son los que dicen "aquí se sale", se quedan.
    /// </summary>
    public static void Salida(Transform cuerpo, KitRelevo kit)
    {
        if (kit.pilarPiedra == null) return;

        Ocultar(cuerpo, "Poste izquierdo", "Poste derecho", "Dintel", "Remate", "Marco izquierdo",
            "Marco derecho", "Base poste izquierdo", "Base poste derecho");

        for (int lado = -1; lado <= 1; lado += 2)
        {
            GameObject pilar = KitRelevo.ColocarCentrado(kit.pilarPiedra, cuerpo, "Pilar",
                new Vector3(lado * 0.8f, 0f, 0f), 0f, new Vector3(1.7f, 1.95f, 1.7f));
            KitRelevo.Tenir(pilar, n => kit.piedraPueblo);

            GameObject estandarte = KitRelevo.ColocarCentrado(kit.estandarte, cuerpo, "Estandarte",
                new Vector3(lado * 0.8f, 0.82f, -0.17f), 90f, Vector3.one * 1.15f);
            KitRelevo.Tenir(estandarte, n => kit.piedraPueblo);

            GameObject farol = KitRelevo.ColocarCentrado(kit.farol, cuerpo, "Farol",
                new Vector3(lado * 1.15f, 0f, -0.55f), 0f, Vector3.one * 0.75f);
            KitRelevo.Tenir(farol, n => kit.piedraPueblo);
        }

        // Dintel: un pilar de madera tumbado de lado a lado sobre los de piedra.
        if (kit.pilarMadera != null)
        {
            GameObject dintel = KitRelevo.ColocarCentrado(kit.pilarMadera, cuerpo, "Dintel madera",
                Vector3.zero, 0f, new Vector3(1.9f, 1.95f, 1.9f));
            dintel.transform.localPosition = new Vector3(0.98f, 1.96f, 0f);
            dintel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            KitRelevo.Tenir(dintel, n => kit.piedraPueblo);
        }

        Mover(cuerpo, "Tira de luz", new Vector3(0f, 1.84f, -0.16f));
    }

    // ------------------------------------------------------------------ recuerdo

    /// <summary>El orbe flota ahora sobre un pilar corto de piedra.</summary>
    public static void Recuerdo(Transform raiz, KitRelevo kit)
    {
        if (kit.pilarPiedra == null) return;

        Ocultar(raiz, "Base", "Pedestal");

        GameObject pilar = KitRelevo.ColocarCentrado(kit.pilarPiedra, raiz, "Pedestal piedra",
            Vector3.zero, 0f, new Vector3(2.7f, 0.34f, 2.7f));
        KitRelevo.Tenir(pilar, n => kit.piedraPueblo);
    }

    // ------------------------------------------------------------------ Maia

    /// <summary>
    /// Maia (GDD: terminal fijo con carcasa verde y rostro de hoja): una jardinera de piedra con
    /// helechos, un cuerpo de máquina con antena y una pantalla encima, todo en el verde de Maia.
    /// Los ojos, la antena, los brazos y el halo son los de siempre (los anima AnimadorMaia) y solo
    /// se recolocan para que la cara mire a la cámara.
    /// </summary>
    public static void Maia(Transform cuerpo, KitRelevo kit)
    {
        if (kit.antenaMaquina == null || kit.pantalla == null) return;

        Ocultar(cuerpo, "Pedestal", "Torre", "Pantalla", "Basamento", "Cuello", "Cornisa",
            "Banda izquierda", "Banda derecha", "Vegetacion A", "Vegetacion B");

        if (kit.jardinera != null)
        {
            GameObject jardinera = KitRelevo.ColocarCentrado(kit.jardinera, cuerpo, "Jardinera",
                Vector3.zero, 0f, new Vector3(0.62f, 0.9f, 0.62f));
            KitRelevo.Tenir(jardinera, n => n.StartsWith("Water") ? kit.verdeMaia : kit.piedraPueblo);
        }

        GameObject maquina = KitRelevo.ColocarCentrado(kit.antenaMaquina, cuerpo, "Cuerpo Maia",
            new Vector3(0f, 0.2f, 0.05f), 180f, Vector3.one * (0.66f / Alto(kit.antenaMaquina)));
        TenirMaquina(maquina, kit, kit.verdeMaia, kit.cobre, kit.oscuro);

        GameObject cara = KitRelevo.ColocarCentrado(kit.pantalla, cuerpo, "Cara Maia",
            new Vector3(0f, 0.78f, 0.02f), 180f, Vector3.one * (0.98f / Alto(kit.pantalla)));
        KitRelevo.Tenir(cara, n => n == "dark" ? kit.pantallaMaia : (n == "metalDark" ? kit.cobre : kit.verdeMaia));

        // Ojos sobre la pantalla, hacia la cámara (que mira desde el sur, -Z).
        Mover(cuerpo, "Ojo izquierdo", new Vector3(-0.13f, 1.33f, -0.14f));
        Mover(cuerpo, "Ojo derecho", new Vector3(0.13f, 1.33f, -0.14f));
        Mover(cuerpo, "Antena base", new Vector3(0.28f, 1.74f, 0.08f));
        Mover(cuerpo, "Hombro izquierdo", new Vector3(-0.5f, 0.8f, 0f));
        Mover(cuerpo, "Hombro derecho", new Vector3(0.5f, 0.8f, 0f));

        // Helechos en la jardinera: cuelgan de 'Vegetacion A/B', que AnimadorMaia hace respirar.
        Helecho(cuerpo, "Vegetacion A", new Vector3(0.36f, 0.16f, -0.3f), kit, 0);
        Helecho(cuerpo, "Vegetacion B", new Vector3(-0.38f, 0.16f, -0.26f), kit, 1);
    }

    static void Helecho(Transform cuerpo, string nombre, Vector3 posicion, KitRelevo kit, int indice)
    {
        Transform ancla = cuerpo.Find(nombre);
        if (ancla == null || kit.arbustos == null || kit.arbustos.Length == 0) return;

        ancla.localPosition = posicion;
        GameObject planta = kit.arbustos[(kit.arbustos.Length - 1 - indice) % kit.arbustos.Length];

        // El ancla es una esfera escalada: el helecho compensa esa escala para medir lo pedido.
        float escalaAncla = Mathf.Max(0.01f, ancla.localScale.x);
        KitRelevo.Colocar(planta, ancla, Vector3.zero, indice * 140f, 0.2f / escalaAncla);
    }

    // ------------------------------------------------------------------ utilidades

    /// <summary>
    /// Tiñe un modelo del Space Kit. En estos modelos el cuerpo principal es el material
    /// 'metalRed' y las franjas y remates son 'metal'; 'metalDark' son tapas y 'dark' cables.
    /// </summary>
    static void TenirMaquina(GameObject go, KitRelevo kit, Material cuerpo, Material franja, Material oscuro)
    {
        KitRelevo.Tenir(go, n =>
        {
            switch (n)
            {
                case "metalRed": return cuerpo;
                case "metal": return franja;
                case "metalDark": return kit.cobre;
                default: return oscuro;
            }
        });
    }

    static float Alto(GameObject modelo)
    {
        return Mathf.Max(0.01f, KitRelevo.Limites(modelo).size.y);
    }

    static void Ocultar(Transform raiz, params string[] nombres)
    {
        foreach (string nombre in nombres)
        {
            Transform t = Buscar(raiz, nombre);
            if (t == null) continue;
            MeshRenderer render = t.GetComponent<MeshRenderer>();
            if (render != null) render.enabled = false;
        }
    }

    static void Pintar(Transform raiz, string nombre, Material material)
    {
        Transform t = Buscar(raiz, nombre);
        if (t == null || material == null) return;
        MeshRenderer render = t.GetComponent<MeshRenderer>();
        if (render != null) render.sharedMaterial = material;
    }

    static void Mover(Transform raiz, string nombre, Vector3 posicion)
    {
        Transform t = Buscar(raiz, nombre);
        if (t != null) t.localPosition = posicion;
    }

    static Transform Buscar(Transform raiz, string nombre)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == nombre) return t;
        }
        return null;
    }
}
