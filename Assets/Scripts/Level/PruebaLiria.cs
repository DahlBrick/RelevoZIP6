using UnityEngine;
using UnityEngine.InputSystem;

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
        if (camara != null) camara.Configurar(Limites());

        PlayerController nilo = FindFirstObjectByType<PlayerController>();
        if (nilo == null) nilo = ActorFactory.CrearNilo(null, inicioNilo, 180f);
        nilo.Inicializar(entrada, camara != null ? camara.Camara : null);
        nilo.Colocar(inicioNilo);
        RelevoGame.Jugador = nilo;
    }

    Bounds Limites()
    {
        Bounds b = new Bounds(transform.position, Vector3.one);
        if (bloque == null) return b;
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
