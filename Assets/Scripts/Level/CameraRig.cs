using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Cámara ortográfica fija con inclinación de maqueta. No rota con el ratón: se coloca
/// sola y ajusta su tamaño para encuadrar los límites del nivel. El movimiento de Nilo
/// se calcula en XZ a partir de los ejes de esta cámara, así W y D coinciden con la pantalla.
/// </summary>
public sealed class CameraRig : MonoBehaviour
{
    [SerializeField] float inclinacion = 55f;
    [SerializeField] float margen = 1.22f;

    /// <summary>
    /// Holgura lateral, en unidades de mundo, que se suma a la mitad del ancho de la maqueta antes
    /// de encuadrar. El fondo ambiental —los fanales y las boyas de los muelles vecinos— vive fuera
    /// de los límites jugables: el fanal más lejano queda a 16,76 del centro y el borde del jugable
    /// a 13, así que con solo 'margen' el encuadre se quedaba en 16,76 y los fanales salían cortados
    /// justo por su borde. Con 1,4 la mitad visible pasa a 17,6 y sobran unos 0,8 de aire.
    /// Como el término vertical ya iba por delante, la holgura hace que mande el horizontal: la
    /// maqueta se ve un 4,6 % más pequeña y aparecen las puntas de los muelles vecinos, de modo que
    /// los fanales dejan de leerse como luces flotando en el vacío.
    /// </summary>
    [SerializeField] float holguraLateral = 1.4f;
    [SerializeField] float distanciaReserva = 45f;
    [SerializeField] bool crearBrillo = true;

    Camera camara;
    Bounds limites;
    float aspectoAnterior = -1f;
    bool configurada;

    public Camera Camara { get { return camara; } }

    public void Configurar(Bounds limitesNivel)
    {
        limites = limitesNivel;

        // Si la escena ya está horneada la cámara existe: se adopta en vez de duplicarla.
        Transform existente = transform.Find("Camara principal");
        camara = existente != null ? existente.GetComponent<Camera>() : null;

        if (camara == null) camara = CrearCamara();
        else AfinarCamara(camara);

        configurada = true;
        Ajustar();
    }

    Camera CrearCamara()
    {
        GameObject go = new GameObject("Camara principal");
        go.transform.SetParent(transform, false);

        Camera nueva = go.AddComponent<Camera>();
        AfinarCamara(nueva);
        return nueva;
    }

    /// <summary>Ajustes de la cámara ortográfica y de su post-proceso (idempotente).</summary>
    void AfinarCamara(Camera c)
    {
        GameObject go = c.gameObject;
        if (go.tag != "MainCamera") go.tag = "MainCamera";

        c.orthographic = true;
        c.nearClipPlane = 0.3f;
        c.farClipPlane = 240f;
        // El fondo de la cámara va al color de la niebla, no al de 'Fondo': si difiriesen, el
        // mar se fundiría con la niebla pero el cielo del encuadre cortaría en seco.
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = RelevoPalette.ColorDe(ClaveMaterial.Neblina);
        c.allowHDR = true;

        // Sin el volumen global el brillo de la energía no se apreciaría.
        UniversalAdditionalCameraData datos = go.GetComponent<UniversalAdditionalCameraData>();
        if (datos == null) datos = go.AddComponent<UniversalAdditionalCameraData>();
        datos.renderPostProcessing = crearBrillo;

        if (go.GetComponent<AudioListener>() == null) go.AddComponent<AudioListener>();

        if (crearBrillo) AsegurarPerfil(go.transform);
    }

    /// <summary>
    /// Deja la cámara con un volumen global y un perfil activo. Es idempotente: si la escena ya
    /// está horneada se reutiliza su volumen y su perfil de Assets/Settings, que es el que la
    /// herramienta de horneado escribe con sus ajustes. Solo se construye un perfil nuevo cuando
    /// no hay ninguno, para que la escena no se quede sin post-proceso al jugar.
    /// </summary>
    void AsegurarPerfil(Transform padre)
    {
        Volume volumen = padre.GetComponentInChildren<Volume>(true);

        if (volumen == null)
        {
            GameObject go = new GameObject("Volumen global");
            go.transform.SetParent(padre, false);
            volumen = go.AddComponent<Volume>();
        }

        volumen.isGlobal = true;
        volumen.priority = 1f;

        if (volumen.sharedProfile == null) volumen.sharedProfile = CrearPerfil();
    }

    /// <summary>
    /// Ajustes de imagen del diorama. El brillo es el que hace visible que una base está
    /// energizada; el mapeo tonal suave y el ajuste cálido unifican la paleta, que sin ellos
    /// queda más apagada de lo que el color de los materiales promete; y el viñeteado cierra los
    /// bordes para que la mirada se quede en el muelle.
    /// </summary>
    VolumeProfile CrearPerfil()
    {
        VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        perfil.name = "PerfilRelevo";

        Bloom bloom = perfil.Add<Bloom>(true);
        bloom.intensity.Override(0.55f);
        bloom.threshold.Override(1.05f);
        bloom.scatter.Override(0.6f);

        // Neutral en vez de ACES: conserva el tono pastel de la paleta en lugar de apagarlo.
        Tonemapping tono = perfil.Add<Tonemapping>(true);
        tono.mode.Override(TonemappingMode.Neutral);

        ColorAdjustments color = perfil.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.25f);
        color.contrast.Override(10f);
        color.saturation.Override(6f);
        color.colorFilter.Override(new Color(1f, 0.97f, 0.92f));

        Vignette vineta = perfil.Add<Vignette>(true);
        vineta.color.Override(new Color(0.05f, 0.08f, 0.10f));
        vineta.intensity.Override(0.24f);
        vineta.smoothness.Override(0.45f);

        return perfil;
    }

    /// <summary>Encaje automático: proyecta las ocho esquinas sobre los ejes de la cámara.</summary>
    public void Ajustar()
    {
        if (camara == null) return;

        Quaternion rotacion = Quaternion.Euler(inclinacion, 0f, 0f);
        Vector3 adelante = rotacion * Vector3.forward;
        Vector3 derecha = rotacion * Vector3.right;
        Vector3 arriba = rotacion * Vector3.up;

        camara.transform.position = limites.center - adelante * distanciaReserva;
        camara.transform.rotation = rotacion;

        Vector3 ext = limites.extents;
        float maxX = 0f;
        float maxY = 0f;

        for (int i = 0; i < 8; i++)
        {
            Vector3 esquina = new Vector3(
                (i & 1) == 0 ? -ext.x : ext.x,
                (i & 2) == 0 ? -ext.y : ext.y,
                (i & 4) == 0 ? -ext.z : ext.z);

            maxX = Mathf.Max(maxX, Mathf.Abs(Vector3.Dot(esquina, derecha)));
            maxY = Mathf.Max(maxY, Mathf.Abs(Vector3.Dot(esquina, arriba)));
        }

        float aspecto = Mathf.Max(0.2f, camara.aspect);
        float tamanoVertical = maxY * margen;
        float tamanoHorizontal = ((maxX + holguraLateral) * margen) / aspecto;
        camara.orthographicSize = Mathf.Max(tamanoVertical, tamanoHorizontal);
        aspectoAnterior = aspecto;
    }

    void LateUpdate()
    {
        if (!configurada || camara == null) return;
        if (Mathf.Abs(camara.aspect - aspectoAnterior) > 0.002f) Ajustar();
    }
}
