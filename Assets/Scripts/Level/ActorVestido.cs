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
    /// Maia es una radio (decisión narrativa: nunca tuvo vida; Nilo la confundió con alguien). Se
    /// ocultan todas las piezas del terminal antiguo —ojos, brazos, antena, carcasa— y en su sitio
    /// queda una radio portátil sobre una peana de piedra, con el dial encendido. Solo se conserva
    /// el halo del suelo, que marca dónde se habla con ella.
    ///
    /// Los "ojos" siguen existiendo como anclas invisibles a la altura del dial: el retrato del
    /// diálogo los usa para encuadrar.
    /// </summary>
    public static void Maia(Transform cuerpo, KitRelevo kit)
    {
        if (kit.radio == null) return;

        Transform halo = cuerpo.Find("Halo");
        foreach (Renderer r in cuerpo.GetComponentsInChildren<Renderer>(true))
            if (halo == null || !r.transform.IsChildOf(halo)) r.enabled = false;

        float alturaPeana = 0.8f;
        if (kit.pilarPiedra != null)
        {
            GameObject peana = KitRelevo.ColocarCentrado(kit.pilarPiedra, cuerpo, "Peana",
                Vector3.zero, 0f, new Vector3(2.6f, 1f, 2.6f) * (alturaPeana / Alto(kit.pilarPiedra)));
            KitRelevo.Tenir(peana, n => kit.piedraPueblo);
        }

        // Ancha y legible desde la cámara del nivel: unos 0,8 m de frente. El frente del modelo
        // mira a +Z y la cámara del juego mira desde el sur, así que se gira media vuelta.
        Bounds b = KitRelevo.Limites(kit.radio);
        float escala = 0.8f / Mathf.Max(0.01f, b.size.x);
        GameObject radio = KitRelevo.ColocarCentrado(kit.radio, cuerpo, "Radio", new Vector3(0f, alturaPeana, 0f), 180f, Vector3.one * escala);

        // El dial, encendido: una luz cálida delante del frente.
        GameObject luz = new GameObject("Luz del dial");
        luz.transform.SetParent(cuerpo, false);
        luz.transform.localPosition = new Vector3(0f, alturaPeana + 0.3f, -0.45f);
        Light l = luz.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.65f, 0.3f);
        l.range = 1.6f;
        l.intensity = 1.2f;
        l.shadows = LightShadows.None;

        float alturaDial = alturaPeana + b.size.y * escala * 0.25f;
        Mover(cuerpo, "Ojo izquierdo", new Vector3(-0.15f, alturaDial, -0.1f));
        Mover(cuerpo, "Ojo derecho", new Vector3(0.15f, alturaDial, -0.1f));
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
