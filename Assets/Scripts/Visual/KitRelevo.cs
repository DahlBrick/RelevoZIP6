using UnityEngine;

/// <summary>
/// Catálogo del arte de RELEVO: modelos de los packs CC0 (Assets/ThirdParty) y materiales propios.
///
/// DioramaBuilder y ActorFactory lo consultan para vestir la maqueta. Si el asset no existe, los
/// niveles se construyen como antes, con primitivas: el kit solo cambia el aspecto, nunca las
/// colisiones ni la lógica. Vive en Resources para que la construcción en tiempo de ejecución
/// (escena sin hornear) también lo encuentre.
///
/// El asset lo crea y lo rellena el menú "Relevo/Preparar kit visual" (Assets/Editor/PrepararKitVisual.cs).
/// </summary>
[CreateAssetMenu(menuName = "RELEVO/Kit visual", fileName = "KitRelevo")]
public sealed class KitRelevo : ScriptableObject
{
    const string Ruta = "KitRelevo";

    static KitRelevo cache;
    static bool buscado;

    /// <summary>El kit cargado, o null si el proyecto no tiene uno.</summary>
    public static KitRelevo Activo
    {
        get
        {
            if (!buscado || cache == null)
            {
                cache = Resources.Load<KitRelevo>(Ruta);
                buscado = true;
            }
            return cache;
        }
    }

    [Header("Agua")]
    public Material agua;
    public Material aguaMar;

    [Header("Suelo")]
    public GameObject[] losasDePiedra;
    public GameObject tablones;
    public Material piedraJunta;
    public Material piedraMuro;
    public Material maderaMuelle;

    [Header("Vegetación")]
    public GameObject[] arboles;
    public GameObject[] arbustos;
    public GameObject[] flores;
    public GameObject[] hierbas;
    public GameObject[] rocas;
    public GameObject[] guijarros;

    [Header("Puerto")]
    public GameObject barril;
    public GameObject caja;
    public GameObject[] botes;
    public GameObject boya;
    public GameObject muelle;
    public GameObject poste;

    [Header("Efectos")]
    public Material particula;

    [Header("Nilo")]
    public GameObject niloModelo;
    public RuntimeAnimatorController niloAnimador;
    [Tooltip("Escala del modelo para que mida lo mismo que el CharacterController (1,8).")]
    public float niloEscala = 0.4f;

    /// <summary>Tamaño de un modelo a escala 1, sumando las mallas de todas sus piezas.</summary>
    public static Vector3 Tamano(GameObject modelo)
    {
        if (modelo == null) return Vector3.one;

        bool primero = true;
        Bounds caja = new Bounds();
        foreach (MeshFilter filtro in modelo.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filtro.sharedMesh == null) continue;
            Bounds local = filtro.sharedMesh.bounds;
            Vector3 escala = filtro.transform.lossyScale;
            Bounds medida = new Bounds(Vector3.Scale(local.center, escala), Vector3.Scale(local.size, escala));
            if (primero) { caja = medida; primero = false; }
            else caja.Encapsulate(medida);
        }

        return primero ? Vector3.one : caja.size;
    }

    /// <summary>Elige un elemento de forma determinista a partir de una semilla (sin Random).</summary>
    public static GameObject Elegir(GameObject[] lista, float semilla01)
    {
        if (lista == null || lista.Length == 0) return null;
        int i = Mathf.Clamp(Mathf.FloorToInt(semilla01 * lista.Length), 0, lista.Length - 1);
        return lista[i];
    }

    /// <summary>
    /// Instancia un modelo como decoración: sin colliders, para que nunca cambie por dónde
    /// puede caminar Nilo. En el editor conserva el enlace al modelo (horneado limpio).
    /// </summary>
    public static GameObject Colocar(GameObject modelo, Transform padre, Vector3 posicion, float giroY, float escala, bool sombra = true)
    {
        if (modelo == null) return null;

        GameObject go;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(modelo, padre);
        }
        else
#endif
        {
            go = Object.Instantiate(modelo, padre);
        }

        go.name = modelo.name;
        go.transform.localPosition = posicion;
        go.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);
        go.transform.localScale = Vector3.one * escala;

        foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
        {
            if (Application.isPlaying) Object.Destroy(c);
            else Object.DestroyImmediate(c);
        }

        if (!sombra)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        return go;
    }
}
