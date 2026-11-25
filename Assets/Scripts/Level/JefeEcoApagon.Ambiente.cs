using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Puesta en escena del jefe final: la sala de noche (luna y ambiente apagado), los faroles del muro
/// que se encienden uno a uno al vencer, la animación del Eco (paso, respiración, agazapado antes de
/// embestir, aturdido), el destello al recibir un rayo, las chispas al deshacerse y el temblor de
/// cámara con cada golpe.
/// </summary>
public sealed partial class JefeEcoApagon
{
    struct LuzGuardada { public Light luz; public float intensidad; public Color color; }

    readonly List<LuzGuardada> soles = new List<LuzGuardada>();
    Color ambiente0, cielo0, ecuador0, suelo0;
    float intensidadAmbiente0;
    float relojAmanecer = -1f;
    ColorAdjustments ajuste;
    Vignette vineta;

    sealed class Farol { public Renderer lampara; public Light luz; public float orden; public bool encendido; }
    readonly List<Farol> faroles = new List<Farol>();

    readonly List<Renderer> piezasFigura = new List<Renderer>();
    readonly List<Material[]> materialesFigura = new List<Material[]>();
    float relojDestello;
    Vector3 posAnterior;
    float fasePaso;

    sealed class Chispa { public Transform t; public Vector3 v; public float vida; }
    readonly List<Chispa> chispas = new List<Chispa>();

    float temblor;
    Vector3 temblorAplicado;
    bool camaraSuscrita;

    // ------------------------------------------------------------------ noche y amanecer

    void PrepararNoche()
    {
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            soles.Add(new LuzGuardada { luz = l, intensidad = l.intensity, color = l.color });
            l.intensity *= 0.2f;
            l.color = new Color(0.55f, 0.66f, 1f);
        }
        ambiente0 = RenderSettings.ambientLight;
        cielo0 = RenderSettings.ambientSkyColor;
        ecuador0 = RenderSettings.ambientEquatorColor;
        suelo0 = RenderSettings.ambientGroundColor;
        intensidadAmbiente0 = RenderSettings.ambientIntensity;
        RenderSettings.ambientLight = Noche(ambiente0);
        RenderSettings.ambientSkyColor = Noche(cielo0);
        RenderSettings.ambientEquatorColor = Noche(ecuador0);
        RenderSettings.ambientGroundColor = Noche(suelo0);
        RenderSettings.ambientIntensity = intensidadAmbiente0 * 0.55f;

        // El estilo cómic pinta en bandas y apenas nota la luz ambiente: la noche se hace en el
        // postproceso, con un volumen propio por encima del perfil del nivel.
        var go = new GameObject("Noche del Eco");
        go.transform.SetParent(transform, false);
        var volumen = go.AddComponent<Volume>();
        volumen.isGlobal = true;
        volumen.priority = 50f;
        var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        ajuste = perfil.Add<ColorAdjustments>(true);
        ajuste.postExposure.Override(-1.45f);
        ajuste.colorFilter.Override(new Color(0.6f, 0.7f, 1f));
        ajuste.saturation.Override(-22f);
        ajuste.contrast.Override(12f);
        vineta = perfil.Add<Vignette>(true);
        vineta.intensity.Override(0.42f);
        vineta.color.Override(new Color(0.02f, 0.03f, 0.08f));
        volumen.profile = perfil;
    }

    static Color Noche(Color c) { return new Color(c.r * 0.22f, c.g * 0.28f, c.b * 0.46f, c.a); }
    static Color Calido(Color c) { return new Color(Mathf.Min(1f, c.r * 1.3f), Mathf.Min(1f, c.g * 1.2f), c.b * 1.05f, c.a); }

    void Amanecer(float dt)
    {
        if (relojAmanecer < 0f) relojAmanecer = 0f;
        relojAmanecer += dt;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((relojAmanecer - 1.2f) / 4f));

        foreach (LuzGuardada s in soles)
        {
            if (s.luz == null) continue;
            s.luz.intensity = Mathf.Lerp(s.intensidad * 0.2f, s.intensidad * 1.35f, t);
            s.luz.color = Color.Lerp(new Color(0.55f, 0.66f, 1f), Calido(s.color), t);
        }
        RenderSettings.ambientLight = Color.Lerp(Noche(ambiente0), Calido(ambiente0), t);
        RenderSettings.ambientSkyColor = Color.Lerp(Noche(cielo0), Calido(cielo0), t);
        RenderSettings.ambientEquatorColor = Color.Lerp(Noche(ecuador0), Calido(ecuador0), t);
        RenderSettings.ambientGroundColor = Color.Lerp(Noche(suelo0), Calido(suelo0), t);
        RenderSettings.ambientIntensity = Mathf.Lerp(intensidadAmbiente0 * 0.55f, intensidadAmbiente0 * 1.25f, t);
        if (ajuste != null)
        {
            ajuste.postExposure.value = Mathf.Lerp(-1.45f, 0.35f, t);
            ajuste.colorFilter.value = Color.Lerp(new Color(0.6f, 0.7f, 1f), new Color(1f, 0.95f, 0.86f), t);
            ajuste.saturation.value = Mathf.Lerp(-22f, 14f, t);
            ajuste.contrast.value = Mathf.Lerp(12f, 4f, t);
            vineta.intensity.value = Mathf.Lerp(0.42f, 0.12f, t);
        }

        // Los faroles del muro se encienden desde el núcleo hacia fuera.
        float frente = (relojAmanecer - 0.8f) / 4.5f;
        foreach (Farol f in faroles)
        {
            if (f.encendido || f.orden > frente) continue;
            f.encendido = true;
            f.lampara.sharedMaterial = RelevoPalette.MaterialDe(ClaveMaterial.Ambar);
            if (f.luz != null) f.luz.intensity = 2.4f;
        }
        for (int i = 0; i < torres.Count; i++)
        {
            torres[i].lampara.sharedMaterial = RelevoPalette.MaterialDe(ClaveMaterial.Ambar);
            torres[i].luz.intensity = 3.4f;
        }
    }

    void CrearFaroles()
    {
        Vector3 a = layout.CentroTile(0, 0), b = layout.CentroTile(layout.Columnas - 1, layout.FilasCuenta - 1);
        float t = layout.TamanoTile;
        float x0 = Mathf.Min(a.x, b.x) - t * 1.1f, x1 = Mathf.Max(a.x, b.x) + t * 1.1f;
        float z0 = Mathf.Min(a.z, b.z) - t * 1.1f, z1 = Mathf.Max(a.z, b.z) + t * 1.1f;
        Vector3 nucleo = layout.CentroTile(CasillaTorre[2].x, CasillaTorre[2].y);
        var puntos = new List<Vector3>();
        for (float x = x0; x <= x1 + 0.01f; x += t * 2f) { puntos.Add(new Vector3(x, 0f, z0)); puntos.Add(new Vector3(x, 0f, z1)); }
        for (float z = z0 + t * 2f; z < z1 - 0.01f; z += t * 2f) { puntos.Add(new Vector3(x0, 0f, z)); puntos.Add(new Vector3(x1, 0f, z)); }

        float lejos = 1f;
        foreach (Vector3 p in puntos) lejos = Mathf.Max(lejos, Plano(p - nucleo).magnitude);
        Transform raiz = DioramaBuilder.CrearAncla("Faroles de Liria", transform, Vector3.zero);
        for (int i = 0; i < puntos.Count; i++)
        {
            var ancla = DioramaBuilder.CrearAncla("Farol", raiz, puntos[i]);
            DioramaBuilder.CrearCilindro("Pie", ancla, Vector3.up * 0.7f, 0.09f, 1.4f, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
            var lampara = DioramaBuilder.CrearEsfera("Vidrio", ancla, Vector3.up * 1.55f, 0.45f, RelevoPalette.MaterialDe(ClaveMaterial.Fondo));
            foreach (var c in ancla.GetComponentsInChildren<Collider>(true)) Destroy(c);
            var f = new Farol { lampara = lampara.GetComponent<Renderer>(), orden = Plano(puntos[i] - nucleo).magnitude / lejos };
            if (i % 4 == 0)
            {
                f.luz = new GameObject("Luz").AddComponent<Light>();
                f.luz.transform.SetParent(ancla, false);
                f.luz.transform.localPosition = Vector3.up * 1.6f;
                f.luz.type = LightType.Point;
                f.luz.color = RelevoPalette.ColorDe(ClaveMaterial.Ambar);
                f.luz.range = 7f;
                f.luz.intensity = 0f;
            }
            faroles.Add(f);
        }
    }

    // ------------------------------------------------------------------ animación del Eco

    void RecogerFigura()
    {
        foreach (Renderer r in figura.GetComponentsInChildren<Renderer>(true))
        {
            piezasFigura.Add(r);
            materialesFigura.Add(r.sharedMaterials);
        }
        posAnterior = eco.position;
    }

    void Animar(float dt)
    {
        Vector3 delta = Plano(eco.position - posAnterior);
        posAnterior = eco.position;
        float v = delta.magnitude / Mathf.Max(dt, 0.0001f);
        float marcha = Mathf.Clamp01(v / 2.5f);
        fasePaso += delta.magnitude * 1.1f;

        float bote = Mathf.Abs(Mathf.Sin(fasePaso)) * 0.35f * marcha;
        float balanceo = Mathf.Sin(fasePaso) * 6f * marcha;
        float inclinacion = 9f * marcha;
        Vector3 escala = Vector3.one;

        switch (estado)
        {
            case Estado.Prepara:
                float k = 1f - Mathf.Clamp01(reloj / AvisoEmbestida[Fase]);
                escala = new Vector3(1f + 0.16f * k, 1f - 0.2f * k, 1f + 0.16f * k);
                inclinacion = -10f * k;
                balanceo = Mathf.Sin(Time.time * 50f) * 2f * k;
                break;
            case Estado.Embiste:
                escala = new Vector3(0.9f, 1.06f, 1.14f);
                inclinacion = 26f;
                break;
            case Estado.Aturdido:
                balanceo = Mathf.Sin(Time.time * 28f) * 9f;
                inclinacion = -12f;
                escala = Vector3.one * (1f + 0.03f * Mathf.Sin(Time.time * 40f));
                break;
        }
        if (pausa > 0f) inclinacion = -14f;   // retrocede tras golpear

        figura.localPosition = new Vector3(0f, bote + 0.18f * Mathf.Sin(Time.time * 1.6f), 0f);
        figura.localRotation = Quaternion.Euler(inclinacion, 0f, balanceo);
        figura.localScale = Vector3.Lerp(figura.localScale, escala, 1f - Mathf.Exp(-14f * dt));

        if (relojDestello > 0f)
        {
            relojDestello -= dt;
            if (relojDestello <= 0f) PintarFigura(false);
        }
        temblor = Mathf.MoveTowards(temblor, 0f, dt * 2.2f);
    }

    void Destellar()
    {
        relojDestello = 0.3f;
        PintarFigura(true);
    }

    void PintarFigura(bool destellando)
    {
        Material luz = RelevoPalette.MaterialDe(ClaveMaterial.Turquesa);
        for (int i = 0; i < piezasFigura.Count; i++)
        {
            if (piezasFigura[i] == null) continue;
            if (!destellando) { piezasFigura[i].sharedMaterials = materialesFigura[i]; continue; }
            var mats = new Material[materialesFigura[i].Length];
            for (int m = 0; m < mats.Length; m++) mats[m] = luz;
            piezasFigura[i].sharedMaterials = mats;
        }
    }

    // ------------------------------------------------------------------ chispas al deshacerse

    void EstallarChispas()
    {
        Material ambar = RelevoPalette.MaterialDe(ClaveMaterial.Ambar);
        Material turquesa = RelevoPalette.MaterialDe(ClaveMaterial.Turquesa);
        for (int i = 0; i < 28; i++)
        {
            var e = DioramaBuilder.CrearEsfera("Chispa", transform, eco.position + Vector3.up * (1f + Random.value * 3f), 0.28f, i % 3 == 0 ? turquesa : ambar);
            foreach (var c in e.GetComponentsInChildren<Collider>(true)) Destroy(c);
            Vector3 v = Random.insideUnitSphere * 6f;
            v.y = Mathf.Abs(v.y) + 4f;
            chispas.Add(new Chispa { t = e.transform, v = v, vida = 1.4f + Random.value });
        }
    }

    void AnimarChispas(float dt)
    {
        for (int i = chispas.Count - 1; i >= 0; i--)
        {
            Chispa c = chispas[i];
            c.vida -= dt;
            if (c.vida <= 0f) { Destroy(c.t.gameObject); chispas.RemoveAt(i); continue; }
            c.v += Vector3.down * 7f * dt;
            c.t.position += c.v * dt;
            c.t.localScale = Vector3.one * Mathf.Clamp01(c.vida);
        }
        temblor = Mathf.MoveTowards(temblor, 0f, dt * 2.2f);
    }

    // ------------------------------------------------------------------ temblor de cámara

    void Sacudir(float fuerza)
    {
        temblor = Mathf.Max(temblor, fuerza);
        if (camaraSuscrita) return;
        RenderPipelineManager.beginCameraRendering += AntesDeDibujar;
        RenderPipelineManager.endCameraRendering += DespuesDeDibujar;
        camaraSuscrita = true;
    }

    void SoltarCamara()
    {
        if (!camaraSuscrita) return;
        RenderPipelineManager.beginCameraRendering -= AntesDeDibujar;
        RenderPipelineManager.endCameraRendering -= DespuesDeDibujar;
        camaraSuscrita = false;
    }

    void AntesDeDibujar(ScriptableRenderContext contexto, Camera camara)
    {
        if (camara != Camera.main || temblor <= 0.001f) return;
        temblorAplicado = Random.insideUnitSphere * temblor * 0.7f;
        camara.transform.position += temblorAplicado;
    }

    void DespuesDeDibujar(ScriptableRenderContext contexto, Camera camara)
    {
        if (camara != Camera.main || temblorAplicado == Vector3.zero) return;
        camara.transform.position -= temblorAplicado;
        temblorAplicado = Vector3.zero;
    }
}
