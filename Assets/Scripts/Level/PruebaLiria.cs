using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Arranque mínimo de la escena Liria_Prueba: cámara de maqueta encuadrando el bloque y Nilo
/// manejable, sin niveles, HUD ni tutorial. Sirve para recorrer los bloques que salen de
/// Blender (Arte/Blender/scripts/construir_liria.py) y comprobar escala y límites.
/// </summary>
public class PruebaLiria : MonoBehaviour
{
    [SerializeField] InputActionAsset acciones;
    [SerializeField] Transform bloque;
    [SerializeField] Vector3 inicioNilo = new Vector3(0f, 0f, 0f);
    [Tooltip("Índice de Liria_Renderer en el pipeline (contorno de tinta). -1 usa el de por defecto.")]
    [SerializeField] int indiceRenderer = -1;

    void Awake()
    {
        Time.timeScale = 1f;
        GameEvents.Limpiar();
        RelevoGame.Limpiar();
        ActorFactory.ConfigurarEntorno();

        RelevoInput entrada = new RelevoInput(acciones);
        RelevoGame.Input = entrada;
        entrada.Activar();

        CameraRig camara = FindFirstObjectByType<CameraRig>();
        if (camara != null)
        {
            camara.Configurar(Limites());
            if (indiceRenderer >= 0) camara.Camara.GetUniversalAdditionalCameraData().SetRenderer(indiceRenderer);
        }

        PlayerController nilo = FindFirstObjectByType<PlayerController>();
        if (nilo == null) nilo = ActorFactory.CrearNilo(null, inicioNilo, 180f);
        nilo.Inicializar(entrada, camara != null ? camara.Camara : null);
        nilo.Colocar(inicioNilo);
        RelevoGame.Jugador = nilo;
    }

    /// <summary>
    /// Encuadre: lo pisable (la malla de límites) con unos metros de aire, no todo el bloque; las
    /// rocas lejanas quedan como fondo y la cámara se acerca a donde juega Nilo.
    /// </summary>
    Bounds Limites()
    {
        Bounds b = new Bounds(transform.position, Vector3.one);
        if (bloque == null) return b;
        foreach (MeshCollider c in bloque.GetComponentsInChildren<MeshCollider>())
        {
            if (!c.name.EndsWith("_Limites")) continue;
            b = c.bounds;
            b.Expand(new Vector3(6f, 0f, 6f));
            return b;
        }
        bool primero = true;
        foreach (Renderer r in bloque.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            if (primero) { b = r.bounds; primero = false; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }
}
