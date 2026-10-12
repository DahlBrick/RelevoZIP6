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

    [Header("Piezas del puzle")]
    public GameObject bateria;          // Space Kit: machine_barrel
    public GameObject maquina;          // Space Kit: machine_generator (caja de la palanca)
    public GameObject antenaMaquina;    // Space Kit: machine_wireless (cuerpo de Maia)
    public GameObject pantalla;         // Space Kit: desk_computerScreen (cara de Maia)
    public GameObject pilarPiedra;      // Fantasy Town: pillar-stone
    public GameObject pilarMadera;      // Fantasy Town: pillar-wood
    public GameObject farol;            // Fantasy Town: lantern
    public GameObject estandarte;       // Fantasy Town: banner-green
    public GameObject jardinera;        // Fantasy Town: fountain-round (sin agua, con helechos)

    [Header("Materiales de las piezas")]
    public Material cobre;
    public Material ambarBrillante;
    public Material ambarMate;
    public Material verdeMaia;
    public Material pantallaMaia;
    public Material oscuro;
    public Material piedraPueblo;       // texturas de Fantasy Town teñidas a la piedra crema

    [Header("Efectos")]
    public Material particula;

    [Header("Nilo")]
    public GameObject niloModelo;
    public RuntimeAnimatorController niloAnimador;
    [Tooltip("Escala del modelo para que mida lo mismo que el CharacterController (1,8).")]
    public float niloEscala = 0.4f;

    /// <summary>
    /// Coloca un modelo con su base centrada en 'posicion': los modelos del Space Kit traen el
    /// pivote desplazado de la pieza, así que se envuelven en un objeto vacío y se compensa el
    /// desfase. Devuelve el envoltorio.
    /// </summary>
    public static GameObject ColocarCentrado(GameObject modelo, Transform padre, string nombre, Vector3 posicion, float giroY, Vector3 escala)
    {
        if (modelo == null) return null;

        GameObject envoltorio = new GameObject(nombre);
        envoltorio.transform.SetParent(padre, false);
        envoltorio.transform.localPosition = posicion;
        envoltorio.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);
        envoltorio.transform.localScale = escala;

        GameObject go = Colocar(modelo, envoltorio.transform, Vector3.zero, 0f, 1f);
        Bounds caja = Limites(modelo);
        go.transform.localPosition = new Vector3(-caja.center.x, -caja.min.y, -caja.center.z);
        return envoltorio;
    }

    /// <summary>Caja del modelo a escala 1 en su propio espacio.</summary>
    public static Bounds Limites(GameObject modelo)
    {
        bool primero = true;
        Bounds caja = new Bounds();
        if (modelo == null) return caja;

        foreach (MeshFilter filtro in modelo.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filtro.sharedMesh == null) continue;
            Matrix4x4 m = modelo.transform.worldToLocalMatrix * filtro.transform.localToWorldMatrix;
            Bounds local = filtro.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 esquina = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = m.MultiplyPoint3x4(esquina);
                if (primero) { caja = new Bounds(p, Vector3.zero); primero = false; }
                else caja.Encapsulate(p);
            }
        }
        return caja;
    }

    /// <summary>
    /// Cambia los materiales de un modelo por nombre de material original. Los modelos de Kenney
    /// usan nombres como 'metal', 'metalRed' o 'dark'; aquí se traducen a la paleta de RELEVO.
    /// </summary>
    public static void Tenir(GameObject go, System.Func<string, Material> traductor)
    {
        if (go == null) return;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materiales = r.sharedMaterials;
            for (int i = 0; i < materiales.Length; i++)
            {
                string nombre = materiales[i] != null ? materiales[i].name : "";
                Material nuevo = traductor(nombre);
                if (nuevo != null) materiales[i] = nuevo;
            }
            r.sharedMaterials = materiales;
        }
    }

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
