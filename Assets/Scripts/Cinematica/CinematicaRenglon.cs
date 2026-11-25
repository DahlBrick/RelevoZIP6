using UnityEngine;

/// <summary>
/// Cinemática de la revelación · El renglón del operador (plan_cinematicas.md, §4). Tras el Puerto 10.
///
/// El libro de guardia al trasluz con el renglón que faltaba; la cara de Nilo, que reconoce su
/// propio código; la radio de Maia, que por primera vez se apaga sola. Carga el Puerto 11.
/// </summary>
public sealed class CinematicaRenglon : CinematicaBase
{
    public const string Escena = "Cinematica_Renglon";

    const float P_Libro = 0f, P_Nilo = 11f, P_Radio = 19f, FinTotal = 30f;
    protected override float Fin { get { return FinTotal; } }

    PlayerController nilo;
    Transform libro, radio;
    Material renglon, piloto, lampara;

    static readonly Vector3 Mesa = new Vector3(0f, 0f, 0f);

    protected override string Destino()
    {
        string n = LevelManager.NombreEscenaNivel(11);
        return Application.CanStreamedLevelBeLoaded(n) ? n : LevelManager.NombreEscenaNivel(1);
    }

    protected override void Construir()
    {
        Transform r = Raiz("Casa de las Dos Guardias", Vector3.zero);
        Caja("Suelo", r, new Vector3(0f, -0.1f, 0f), new Vector3(14f, 0.2f, 14f), M(ClaveMaterial.MaderaOscura));
        Caja("Pared", r, new Vector3(0f, 2.5f, 3.2f), new Vector3(14f, 5f, 0.3f), M(ClaveMaterial.PiedraCrema));
        Caja("Ventana", r, new Vector3(-2.5f, 2.8f, 3.03f), new Vector3(1.8f, 1.4f, 0.05f), Brillo(new Color(0.1f, 0.14f, 0.25f), new Color(0.05f, 0.08f, 0.2f)));
        Caja("Mesa", r, new Vector3(0f, 0.9f, 1.2f), new Vector3(3.2f, 0.12f, 1.6f), M(ClaveMaterial.MaderaOscura));
        for (int i = 0; i < 4; i++)
            Caja("Pata", r, new Vector3(i % 2 == 0 ? -1.45f : 1.45f, 0.42f, i < 2 ? 0.55f : 1.85f), new Vector3(0.1f, 0.85f, 0.1f), M(ClaveMaterial.MaderaOscura));
        lampara = Brillo(new Color(1f, 0.85f, 0.6f), new Color(2.2f, 1.7f, 1f));
        Cilindro("Pie de lampara", r, new Vector3(-1.1f, 1.3f, 1.6f), 0.04f, 0.7f, M(ClaveMaterial.Cobre));
        Esfera("Lampara", r, new Vector3(-1.1f, 1.7f, 1.6f), 0.3f, lampara);

        libro = new GameObject("Libro de guardia").transform;
        libro.SetParent(r, false);
        libro.localPosition = new Vector3(0.1f, 0.97f, 1.1f);
        var p1 = Caja("Pagina izquierda", libro, new Vector3(-0.32f, 0.02f, 0f), new Vector3(0.6f, 0.03f, 0.85f), M(ClaveMaterial.PiedraCrema));
        p1.transform.localRotation = Quaternion.Euler(0f, 0f, 5f);
        var p2 = Caja("Pagina derecha", libro, new Vector3(0.32f, 0.02f, 0f), new Vector3(0.6f, 0.03f, 0.85f), M(ClaveMaterial.PiedraCrema));
        p2.transform.localRotation = Quaternion.Euler(0f, 0f, -5f);
        renglon = Brillo(new Color(0.6f, 0.15f, 0.12f), Color.black);
        for (int i = 0; i < 7; i++)
            Caja("Renglon", libro, new Vector3(0.32f, 0.04f, 0.3f - i * 0.1f), new Vector3(0.45f, 0.004f, 0.015f), i == 4 ? renglon : M(ClaveMaterial.MaderaOscura));
        Cilindro("Mancha del carrete", libro, new Vector3(-0.3f, 0.04f, -0.15f), 0.12f, 0.004f, M(ClaveMaterial.Maqueta));

        radio = Radio(r, new Vector3(1.1f, 0.96f, 1.5f), 10f, out piloto, out _);
        radio.localScale = Vector3.one * 0.7f;

        nilo = CrearNilo(transform, new Vector3(0.2f, 0f, -0.6f), 0f);
    }

    protected override void Evaluar(float previo, float t, float dt)
    {
        Base();
        // En el plano de la radio Nilo quedaría entre la cámara y la mesa.
        nilo.gameObject.SetActive(t < P_Radio);
        Luna(1.1f, new Color(1f, 0.88f, 0.72f), new Vector3(55f, -20f, 0f));
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f);
        RenderSettings.fog = false;
        musica.volume = 0f;
        estatica.volume = t < P_Radio + 5f ? 0.06f : 0f;
        piloto.SetColor("_EmissionColor", RelevoUI.Verde * 1.4f);
        renglon.SetColor("_EmissionColor", new Color(1.6f, 0.25f, 0.2f) * Tramo(t, 3f, 5f));

        if (t < P_Nilo)
        {
            // El libro al trasluz: la cámara baja despacio hasta el renglón.
            float u = S(Tramo(t, P_Libro, P_Nilo));
            Vector3 c = libro.position + new Vector3(0.3f, 0f, -0.1f);
            Colocar(c + new Vector3(-0.3f, 1.4f - u * 0.6f, -0.9f + u * 0.3f), c, 36f, 0.4f);
            Fundido(1f - Tramo(t, 0f, 1.5f));
            Rotulo("Casa de las Dos Guardias", Ventana(t, 1f, 5f));
            Cita(Escribir("00:17. Carga de las bombas desviada a la emisora.", Tramo(t, 4.5f, 7.5f)), Ventana(t, 4.5f, P_Nilo - 0.3f, 0.5f), RelevoUI.Coral);
            Pie("Operador de guardia: NILO", Ventana(t, 7.8f, P_Nilo - 0.3f, 0.6f));
            if (Cruza(previo, t, 7.8f)) Sonar(clipPitido, 0.25f);
        }
        else if (t < P_Radio)
        {
            // Nilo reconoce el código: un destello turquesa en cada pulso.
            Vector3 cabeza = nilo.transform.position + new Vector3(0f, 1.6f, 0f);
            float u = Tramo(t, P_Nilo, P_Radio);
            Colocar(cabeza + new Vector3(0.5f, 0.1f, 3.2f - u * 0.6f), cabeza, 36f, 0.4f);
            Luna(0.9f, new Color(1f, 0.88f, 0.72f), new Vector3(30f, 180f, 0f));
            float pulso = Mathf.Max(0f, Mathf.Sin((t - P_Nilo) * 5f)) * Ventana(t, P_Nilo + 1.5f, P_Nilo + 5f, 0.3f);
            destello.color = new Color(RelevoUI.Turquesa.r, RelevoUI.Turquesa.g, RelevoUI.Turquesa.b, 0.12f * pulso);
            Rotulo("CÓDIGO DEL OPERADOR · COINCIDE", Ventana(t, P_Nilo + 2f, P_Radio - 0.3f));
            rotulo.color = new Color(RelevoUI.Turquesa.r, RelevoUI.Turquesa.g, RelevoUI.Turquesa.b, rotulo.color.a);
            Pie("Nilo lo reconoce: es el suyo.", Ventana(t, P_Nilo + 3.5f, P_Radio - 0.3f));
            if (Cruza(previo, t, P_Nilo + 1.5f)) Sonar(clipArranque, 0.4f);
        }
        else
        {
            // La radio se apaga sola.
            Vector3 c = radio.position + new Vector3(0f, 0.3f, 0f);
            float u = Tramo(t, P_Radio, FinTotal);
            Colocar(c + new Vector3(-1.2f - u * 0.6f, 0.7f, -1.8f - u * 0.8f), c, 34f, 0.3f);
            float apaga = t < P_Radio + 4f ? 1f : t < P_Radio + 4.6f ? (Mathf.PerlinNoise(t * 20f, 0f) > 0.5f ? 1f : 0f) : 0f;
            piloto.SetColor("_EmissionColor", RelevoUI.Verde * 1.4f * apaga);
            if (Cruza(previo, t, P_Radio + 4.6f)) Sonar(clipRele, 0.9f);
            Pie("Maia no dice nada. Por primera vez, su radio se apaga sola.", Ventana(t, P_Radio + 6f, FinTotal - 1f));
            Fundido(Tramo(t, FinTotal - 1.2f, FinTotal));
        }
    }
}
