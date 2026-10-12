using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Acento luminoso reutilizable: hace latir la emisión de unos cuantos renderers y, si se le
/// pide, gira su propio transform para que ese latido tenga dirección.
///
/// El motivo de que exista este componente es el horneado. Los materiales de la paleta son
/// ASSETS compartidos: escribir en ellos teñiría de paso el cobre de los pernos y el ámbar de
/// las boyas, y además ensuciaría el asset en disco en cada partida. Por eso las copias se crean
/// en Awake —nunca durante el horneado— y solo viven en tiempo de ejecución: la escena guardada
/// sigue apuntando al material compartido y el latido es un efecto de partida que no sobrevive
/// al archivo, igual que la marea o el vaivén del recuerdo.
///
/// Las copias se agrupan por material de origen, así que los cuatro acentos de Nilo comparten
/// una sola copia de 'NiloOjos' en lugar de cuatro.
///
/// Solo debe apuntarse a geometría propia de este Bloque 5. Los renderers que otro script
/// reasigna (el anillo y el cable de EnergySocket, el indicador de LeverInteractable, el orbe de
/// MemoryInteractable) no sirven: su 'PintarEnergia'/'Interactuar' les devuelve el material
/// compartido y el latido se perdería en el primer cambio de estado.
/// </summary>
public class AcentoEmisivo : MonoBehaviour
{
    [Header("Acentos")]
    [Tooltip("Renderers cuya emisión late. Tienen que ser geometría propia de este componente.")]
    [SerializeField] Renderer[] acentos;

    [Header("Latido")]
    [SerializeField] float periodo = 2.2f;
    [SerializeField] float fase = 0f;
    [SerializeField] float brilloMinimo = 0.45f;
    [SerializeField] float brilloMaximo = 1.7f;

    [Header("Giro")]
    [SerializeField] float giroGradosPorSegundo = 0f;
    [SerializeField] Vector3 ejeGiro = Vector3.up;

    Material[] materiales;
    Color[] coloresBase;
    float tiempo;

    /// <summary>
    /// Deja el acento configurado. Lo llaman las fábricas de actores justo después de crear la
    /// geometría del acento, de modo que las referencias quedan serializadas con la escena.
    /// </summary>
    public void Configurar(Renderer[] objetivo, float periodoPulso, float desfase,
        float minimo, float maximo, float giro)
    {
        acentos = objetivo;
        periodo = periodoPulso;
        fase = desfase;
        brilloMinimo = minimo;
        brilloMaximo = maximo;
        giroGradosPorSegundo = giro;
    }

    void Awake()
    {
        PrepararCopias();
    }

    void PrepararCopias()
    {
        if (acentos == null || acentos.Length == 0) return;

        Dictionary<int, Material> copias = new Dictionary<int, Material>();
        List<Material> lista = new List<Material>();
        List<Color> bases = new List<Color>();

        for (int i = 0; i < acentos.Length; i++)
        {
            Renderer render = acentos[i];
            if (render == null) continue;

            Material origen = render.sharedMaterial;
            if (origen == null) continue;

            Material copia;
            if (!copias.TryGetValue(origen.GetInstanceID(), out copia))
            {
                copia = new Material(origen);
                copia.name = origen.name + " acento";
                copia.EnableKeyword("_EMISSION");
                copia.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

                // Los acentos hechos con materiales sin emisión propia —el coral de la palanca—
                // arrancarían con emisión negra y el latido no se vería: se les da la suya a
                // partir del color base.
                if (copia.HasProperty("_EmissionColor")
                    && copia.GetColor("_EmissionColor").maxColorComponent <= 0.001f)
                {
                    Color baseColor = copia.HasProperty("_BaseColor") ? copia.GetColor("_BaseColor") : Color.white;
                    copia.SetColor("_EmissionColor", baseColor * 1.35f);
                }

                copias[origen.GetInstanceID()] = copia;
                lista.Add(copia);
                bases.Add(copia.HasProperty("_EmissionColor") ? copia.GetColor("_EmissionColor") : Color.white);
            }

            render.sharedMaterial = copia;
        }

        materiales = lista.ToArray();
        coloresBase = bases.ToArray();
    }

    void Update()
    {
        if (materiales == null || materiales.Length == 0) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        tiempo += dt;

        float onda = Mathf.Sin((tiempo / Mathf.Max(0.05f, periodo) + fase) * Mathf.PI * 2f);
        float brillo = Mathf.Lerp(brilloMinimo, brilloMaximo, onda * 0.5f + 0.5f);

        for (int i = 0; i < materiales.Length; i++)
        {
            if (materiales[i] == null) continue;
            materiales[i].SetColor("_EmissionColor", coloresBase[i] * brillo);
        }

        if (!Mathf.Approximately(giroGradosPorSegundo, 0f))
        {
            transform.Rotate(ejeGiro, giroGradosPorSegundo * dt, Space.Self);
        }
    }
}
