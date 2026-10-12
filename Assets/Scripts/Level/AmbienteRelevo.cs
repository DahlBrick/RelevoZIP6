using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animación ambiental del nivel: marea y ondulaciones en el agua, con un brillo que se mece por
/// su superficie. Es puramente cosmético —no toca colliders, ni estado de juego, ni las reglas de
/// victoria— y vive fuera de los actores a propósito: el nivel se hornea sin ninguna animación en
/// marcha y solo se mueve al entrar en Play Mode, de modo que la escena guardada sigue siendo
/// estática y editable.
///
/// Dos decisiones que conviene no deshacer:
///  - Los materiales animados son COPIAS de ejecución. 'Agua' lo comparten el canal, los tramos de
///    puente y pasarela, el mar y los materiales de la paleta; escribir sobre el material de la
///    paleta teñiría todos esos objetos a la vez y el próximo horneado capturaría el color ya
///    deformado.
///  - Las superficies de agua se reconocen por el NOMBRE de su material y no por su identidad: tras
///    hornear, cada renderer apunta al asset 'Assets/Materials/RELEVO_Agua.mat', mientras que
///    RelevoPalette.MaterialDe() devuelve en ejecución un material nuevo en memoria. Comparar por
///    identidad no encontraría ni una sola superficie.
/// </summary>
[DisallowMultipleComponent]
public sealed class AmbienteRelevo : MonoBehaviour
{
    [Header("Marea")]
    [SerializeField] float velocidadMarea = 0.42f;
    [SerializeField] float amplitudMarea = 0.035f;

    [Header("Ondulaciones")]
    [SerializeField] int maximoOndas = 12;
    [SerializeField] float duracionOnda = 3.4f;

    const string NombreMaterialAgua = "RELEVO_Agua";

    struct Onda
    {
        public Transform transform;
        public Material material;
        public float desfase;
    }

    readonly List<Transform> superficies = new List<Transform>();
    readonly List<float> alturas = new List<float>();
    readonly List<Onda> ondas = new List<Onda>();

    Material aguaAnimada;
    Color aguaClara;
    Color aguaProfunda;
    Color espumaClara;
    float tiempo;

    void Start()
    {
        RecogerAgua();
        CrearOndas();
    }

    void Update()
    {
        tiempo = Time.time;

        AnimarMaterial();
        AnimarMarea();
        AnimarOndas();
    }

    // ------------------------------------------------------------------ agua

    /// <summary>
    /// Reúne las superficies de agua del nivel —canal, tramos de puente y pasarela, y mar— y las
    /// pasa a un único material de ejecución animado, que es lo que permite tratarlas como una
    /// sola lámina aunque sean cajas distintas.
    /// </summary>
    void RecogerAgua()
    {
        Material baseAgua = RelevoPalette.MaterialDe(ClaveMaterial.Agua);
        if (baseAgua == null) return;

        aguaAnimada = new Material(baseAgua);
        aguaAnimada.name = "RELEVO_AguaAnimada";

        aguaClara = RelevoPalette.ColorDe(ClaveMaterial.Agua);
        aguaProfunda = Color.Lerp(aguaClara, RelevoPalette.ColorDe(ClaveMaterial.Turquesa), 0.22f);

        MeshRenderer[] renders = GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renders.Length; i++)
        {
            MeshRenderer render = renders[i];
            Material material = render.sharedMaterial;

            if (material == null || material.name != NombreMaterialAgua) continue;

            render.sharedMaterial = aguaAnimada;
            superficies.Add(render.transform);
            alturas.Add(render.transform.localPosition.y);
        }
    }

    /// <summary>
    /// El brillo que sube y baja es lo que se lee como luz meciéndose en el agua; el color se
    /// desplaza muy poco, solo lo justo para que la lámina no parezca congelada.
    /// </summary>
    void AnimarMaterial()
    {
        if (aguaAnimada == null) return;

        aguaAnimada.SetFloat("_Smoothness", 0.74f + Mathf.Sin(tiempo * velocidadMarea) * 0.18f);

        float mezcla = (Mathf.Sin(tiempo * velocidadMarea * 0.43f) + 1f) * 0.5f;
        Color color = Color.Lerp(aguaClara, aguaProfunda, mezcla);
        aguaAnimada.SetColor("_BaseColor", color);
        aguaAnimada.SetColor("_Color", color);
    }

    /// <summary>
    /// Marea: cada losa sube y baja poco más de tres centímetros. La fase sale de su POSICIÓN y no
    /// de su índice, porque el canal está partido en veinticinco cajas contiguas: con fases
    /// distintas cada una se movería por su cuenta y el agua se desencajaría en escalones.
    /// </summary>
    void AnimarMarea()
    {
        for (int i = 0; i < superficies.Count; i++)
        {
            Transform superficie = superficies[i];
            if (superficie == null) continue;

            Vector3 posicion = superficie.localPosition;
            float fase = (posicion.x + posicion.z) * 0.25f;
            posicion.y = alturas[i] + Mathf.Sin(tiempo * velocidadMarea + fase) * amplitudMarea;
            superficie.localPosition = posicion;
        }
    }

    // ------------------------------------------------------------------ ondulaciones

    /// <summary>
    /// Reparte las ondulaciones por el agua. Cada una es un anillo fino y plano que se abre desde su
    /// centro y se disuelve en el color del agua: sin transparencia no se puede desvanecer, pero un
    /// anillo casi blanco al nacer y del color del canal al final se lee igual desde la cámara y no
    /// arriesga la superficie del canal —un material transparente mal configurado estropearía toda
    /// el agua del nivel de una vez.
    ///
    /// El anillo vive en el plano XZ a radio 0,5, así que la escala del transform es el diámetro
    /// pedido y nada más. Se apoya 6 cm sobre la superficie en reposo para que la marea (±3,5 cm)
    /// no lo atraviese en la cresta.
    /// </summary>
    void CrearOndas()
    {
        if (superficies.Count == 0) return;
        if (RelevoPalette.MaterialDe(ClaveMaterial.Agua) == null) return;

        espumaClara = Color.Lerp(aguaClara, Color.white, 0.82f);

        Transform contenedor = DioramaBuilder.CrearAncla("Ondulaciones", transform, Vector3.zero);
        List<Vector3> puntos = new List<Vector3>();

        for (int i = 0; i < superficies.Count; i++)
        {
            MeshRenderer render = superficies[i].GetComponent<MeshRenderer>();
            if (render == null) continue;

            Bounds caja = render.bounds;

            // El mar es una sola losa de 300 unidades: sus ondas caerían fuera del encuadre, así
            // que se colocan aparte, en las franjas de agua que deja el zócalo a los lados.
            if (caja.size.x >= 200f) continue;

            // Losas de una o dos casillas: no hay sitio para que la onda se abra sin salirse.
            if (caja.size.x < 3.6f && caja.size.z < 3.6f) continue;

            puntos.Add(new Vector3(caja.center.x, caja.max.y + 0.06f, caja.center.z));
        }

        AnadirOndasDelMar(puntos);

        if (puntos.Count == 0) return;

        // Se toma una de cada 'salto' para que las ondas queden repartidas por todo el canal en
        // lugar de amontonarse en las primeras losas del recorrido del mapa.
        int salto = Mathf.Max(1, Mathf.CeilToInt(puntos.Count / (float)Mathf.Max(1, maximoOndas)));
        for (int i = 0; i < puntos.Count; i += salto) CrearOnda(contenedor, puntos[i]);
    }

    /// <summary>Ondas del mar, en las dos franjas laterales que sí entran en el encuadre.</summary>
    void AnadirOndasDelMar(List<Vector3> puntos)
    {
        MeshRenderer renderMar = null;
        for (int i = 0; i < superficies.Count; i++)
        {
            MeshRenderer render = superficies[i].GetComponent<MeshRenderer>();
            if (render == null) continue;
            if (render.bounds.size.x < 200f) continue;

            renderMar = render;
            break;
        }

        Transform zocalo = transform.Find("Zocalo");
        MeshRenderer renderZocalo = zocalo != null ? zocalo.GetComponent<MeshRenderer>() : null;

        if (renderMar == null || renderZocalo == null) return;

        Bounds maqueta = renderZocalo.bounds;
        float altura = renderMar.bounds.max.y + 0.06f;

        for (int lado = -1; lado <= 1; lado += 2)
        {
            float x = maqueta.center.x + lado * (maqueta.size.x * 0.5f + 0.95f);
            puntos.Add(new Vector3(x, altura, maqueta.center.z - maqueta.size.z * 0.22f));
            puntos.Add(new Vector3(x, altura, maqueta.center.z + maqueta.size.z * 0.17f));
        }
    }

    void CrearOnda(Transform contenedor, Vector3 punto)
    {
        GameObject go = new GameObject("Onda");
        go.transform.SetParent(contenedor, false);
        go.transform.localPosition = punto;
        go.transform.localScale = new Vector3(0.44f, 1f, 0.44f);

        MeshFilter filtro = go.AddComponent<MeshFilter>();
        filtro.sharedMesh = DioramaBuilder.MallaAnilloEspuma();

        // Material de instancia por onda: el color tiene que seguir al del agua para que la onda se
        // disuelva, y el material de la paleta lo comparten medio nivel. Al crearse en Start() no
        // entra nunca en el horneado.
        Material material = new Material(RelevoPalette.MaterialDe(ClaveMaterial.Agua));
        material.name = "RELEVO_AguaEspuma";
        material.SetFloat("_Smoothness", 0.12f);
        material.SetFloat("_Metallic", 0f);

        MeshRenderer render = go.AddComponent<MeshRenderer>();
        render.sharedMaterial = material;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;

        Onda onda = new Onda();
        onda.transform = go.transform;
        onda.material = material;
        onda.desfase = punto.x * 0.7f + punto.z * 1.3f;
        ondas.Add(onda);
    }

    void AnimarOndas()
    {
        // La onda se disuelve hacia el color VIVO del agua y no hacia el de la paleta: el canal ya
        // no es del color base cuando la onda termina de abrirse, y quedarse en el color de partida
        // dejaría un cerco visible al final de cada ciclo.
        Color vivo = aguaAnimada != null ? aguaAnimada.GetColor("_BaseColor") : aguaClara;

        for (int i = 0; i < ondas.Count; i++)
        {
            Onda onda = ondas[i];
            if (onda.transform == null) continue;

            float avance = Mathf.Repeat((tiempo + onda.desfase) / duracionOnda, 1f);

            // El anillo mide 1 de diámetro en la malla: la escala es el diámetro pedido en X y Z.
            float radio = Mathf.Lerp(0.24f, 1.7f, avance);
            onda.transform.localScale = new Vector3(radio * 2f, 1f, radio * 2f);

            if (onda.material == null) continue;

            Color color = Color.Lerp(espumaClara, vivo, avance);
            onda.material.SetColor("_BaseColor", color);
            onda.material.SetColor("_Color", color);
        }
    }
}
