using UnityEngine;

/// <summary>
/// Efectos visuales creados por código. Se usan solo en la energía de las bases, la
/// palanca y la salida: centralizarlos aquí mantiene el estilo y el coste bajo control.
/// </summary>
public static class RelevoEfectos
{
    static Material materialParticula;

    static Material MaterialParticula(Color color)
    {
        // Con kit visual, la partícula suave y redonda del kit; el color lo da cada sistema con
        // startColor. Así ningún efecto le cambia el color a otro a través del material compartido.
        KitRelevo kit = KitRelevo.Activo;
        if (kit != null && kit.particula != null) return kit.particula;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return RelevoPalette.MaterialDe(ClaveMaterial.Ambar);

        if (materialParticula == null)
        {
            materialParticula = new Material(shader);
            materialParticula.name = "RELEVO_Particulas";
        }

        materialParticula.SetColor("_BaseColor", color);
        materialParticula.SetColor("_Color", color);
        return materialParticula;
    }

    /// <summary>Chispas ascendentes y suaves: señalan energía activa sin ensuciar el diorama.</summary>
    public static ParticleSystem CrearChispas(Transform padre, Vector3 posicionLocal, Color color, float radio, int cantidad)
    {
        if (padre == null) return null;

        GameObject go = new GameObject("Chispas");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicionLocal;

        ParticleSystem sistema = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule principal = sistema.main;
        principal.loop = true;
        principal.playOnAwake = true;
        principal.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        principal.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
        principal.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
        principal.startColor = color;
        principal.gravityModifier = -0.04f;
        principal.simulationSpace = ParticleSystemSimulationSpace.Local;
        principal.maxParticles = Mathf.Max(16, cantidad * 4);

        ParticleSystem.EmissionModule emision = sistema.emission;
        emision.rateOverTime = cantidad;

        ParticleSystem.ShapeModule forma = sistema.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = radio;

        ParticleSystem.ColorOverLifetimeModule colores = sistema.colorOverLifetime;
        colores.enabled = true;
        Gradient gradiente = new Gradient();
        gradiente.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.25f), new GradientAlphaKey(0f, 1f) });
        colores.color = new ParticleSystem.MinMaxGradient(gradiente);

        ParticleSystemRenderer render = sistema.GetComponent<ParticleSystemRenderer>();
        if (render != null)
        {
            render.sharedMaterial = MaterialParticula(color);
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
        }

        return sistema;
    }
}
