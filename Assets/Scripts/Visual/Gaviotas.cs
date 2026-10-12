using UnityEngine;

/// <summary>
/// Gaviotas que planean en círculos sobre el canal. Cada una es una malla de dos alas generada
/// por código —no hace falta ningún asset— que bate y planea alternando. Vuelan muy por encima
/// del muelle y no proyectan sombra fuerte, así que nunca tapan una pieza del puzle.
/// </summary>
[DisallowMultipleComponent]
public sealed class Gaviotas : MonoBehaviour
{
    [SerializeField] int cantidad = 4;
    [SerializeField] float altura = 7f;
    [SerializeField] Vector2 radio = new Vector2(9f, 6f);
    [SerializeField] Color color = new Color(0.97f, 0.96f, 0.93f);

    sealed class Ave
    {
        public Transform cuerpo;
        public Transform alaIzquierda;
        public Transform alaDerecha;
        public float fase;
        public float velocidad;
        public float radioPropio;
        public float alturaPropia;
    }

    Ave[] aves;
    Mesh mallaAla;
    Material material;

    public void Configurar(int cuantas, float alturaVuelo, Vector2 radioVuelo)
    {
        cantidad = cuantas;
        altura = alturaVuelo;
        radio = radioVuelo;
    }

    void Start()
    {
        mallaAla = CrearMallaAla();

        Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        material.enableInstancing = true;

        aves = new Ave[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            Ave ave = new Ave();
            ave.cuerpo = new GameObject("Gaviota " + (i + 1)).transform;
            ave.cuerpo.SetParent(transform, false);
            ave.cuerpo.localScale = Vector3.one * (0.55f + (i % 3) * 0.08f);

            ave.alaIzquierda = CrearAla(ave.cuerpo, -1f);
            ave.alaDerecha = CrearAla(ave.cuerpo, 1f);

            ave.fase = i * Mathf.PI * 2f / cantidad + i * 0.37f;
            ave.velocidad = 0.16f + (i % 2) * 0.05f;
            ave.radioPropio = 0.75f + (i % 3) * 0.14f;
            ave.alturaPropia = altura + (i % 3) * 0.8f;
            aves[i] = ave;
        }
    }

    Transform CrearAla(Transform padre, float lado)
    {
        GameObject go = new GameObject(lado < 0f ? "Ala izquierda" : "Ala derecha");
        go.transform.SetParent(padre, false);
        go.transform.localScale = new Vector3(lado, 1f, 1f);
        go.AddComponent<MeshFilter>().sharedMesh = mallaAla;
        MeshRenderer render = go.AddComponent<MeshRenderer>();
        render.sharedMaterial = material;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.transform;
    }

    void Update()
    {
        if (aves == null) return;
        float t = Time.time;

        for (int i = 0; i < aves.Length; i++)
        {
            Ave ave = aves[i];
            float angulo = t * ave.velocidad + ave.fase;

            Vector3 posicion = new Vector3(
                Mathf.Cos(angulo) * radio.x * ave.radioPropio,
                ave.alturaPropia + Mathf.Sin(t * 0.7f + ave.fase) * 0.5f,
                Mathf.Sin(angulo) * radio.y * ave.radioPropio);

            Vector3 tangente = new Vector3(-Mathf.Sin(angulo) * radio.x, 0f, Mathf.Cos(angulo) * radio.y);
            ave.cuerpo.localPosition = posicion;
            ave.cuerpo.localRotation = Quaternion.LookRotation(tangente.normalized) * Quaternion.Euler(0f, 0f, -14f);

            // Bate unos segundos y planea otros: el planeo es lo que lee como gaviota.
            float ciclo = Mathf.Repeat(t * 0.25f + ave.fase, 1f);
            float batir = ciclo < 0.45f ? Mathf.Sin(t * 9f + ave.fase) * 32f : 6f + Mathf.Sin(t * 1.5f) * 4f;

            ave.alaIzquierda.localRotation = Quaternion.Euler(0f, 0f, batir);
            ave.alaDerecha.localRotation = Quaternion.Euler(0f, 0f, -batir);
        }
    }

    /// <summary>Ala en forma de flecha, con las dos caras, apuntando hacia +X desde el cuerpo.</summary>
    static Mesh CrearMallaAla()
    {
        // Las dos caras llevan vértices propios: si compartieran vértices, al recalcular las
        // normales se anularían entre sí y el ala saldría negra.
        Vector3 a = new Vector3(0f, 0f, 0.18f);
        Vector3 b = new Vector3(0f, 0f, -0.16f);
        Vector3 c = new Vector3(0.55f, 0.05f, -0.02f);
        Vector3 d = new Vector3(1.0f, -0.04f, -0.22f);

        Vector3[] v = { a, b, c, d, a, b, c, d };
        int[] tri = { 0, 2, 1, 1, 2, 3, 4, 5, 6, 5, 7, 6 };

        Mesh malla = new Mesh();
        malla.name = "RELEVO_AlaGaviota";
        malla.vertices = v;
        malla.triangles = tri;
        malla.RecalculateNormals();
        malla.RecalculateBounds();
        return malla;
    }
}
