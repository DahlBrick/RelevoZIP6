using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Elige el interactuable más cercano que Nilo mira y publica el aviso contextual.
/// Mantiene un registro estático de interactuables activos: nada de FindObjectOfType
/// ni consultas físicas por fotograma. La acción E es de pulsación única.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Detección")]
    [SerializeField] float alcance = 3.6f;
    [SerializeField] float sesgoFrontal = 0.8f;

    static readonly List<IInteractable> registrados = new List<IInteractable>();

    PlayerController jugador;
    IInteractable actual;
    Transform marco;
    readonly LineRenderer[] esquinas = new LineRenderer[4];
    Material colorMarco;

    void CrearMarco()
    {
        marco = new GameObject("Foco de interacción").transform;
        marco.SetParent(transform, false);
        for (int i = 0; i < esquinas.Length; i++)
        {
            var esquina = new GameObject("Esquina");
            esquina.transform.SetParent(marco, false);
            esquina.transform.localRotation = Quaternion.Euler(0f, 90f * i, 0f);
            var linea = esquina.AddComponent<LineRenderer>();
            linea.useWorldSpace = false;
            linea.widthMultiplier = 0.10f;
            linea.numCornerVertices = 2;
            linea.positionCount = 3;
            linea.SetPositions(new[] { new Vector3(0.42f, 0f, 0.85f), new Vector3(0.85f, 0f, 0.85f), new Vector3(0.85f, 0f, 0.42f) });
            linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            linea.receiveShadows = false;
            esquinas[i] = linea;
        }
    }

    void LateUpdate()
    {
        if (actual == null || (actual as Component) == null || (RelevoGame.UI != null && RelevoGame.UI.BloqueaJuego))
        {
            if (marco != null && marco.gameObject.activeSelf) marco.gameObject.SetActive(false);
            return;
        }
        if (marco == null) CrearMarco();
        if (!marco.gameObject.activeSelf) marco.gameObject.SetActive(true);
        Vector3 punto = actual.Ancla.position;
        punto.y = transform.position.y + 0.27f;
        marco.SetPositionAndRotation(punto, Quaternion.identity);
        Material tinta = RelevoPalette.MaterialDe(actual.Disponible ? ClaveMaterial.Ambar : ClaveMaterial.Coral);
        if (colorMarco != tinta)
        {
            colorMarco = tinta;
            foreach (var esquina in esquinas) esquina.sharedMaterial = tinta;
        }
    }

    string ultimoAviso;

    public IInteractable Actual { get { return actual; } }

    public static void Registrar(IInteractable interactuable)
    {
        // Fuera de Play mode el registro no tiene sentido y ensuciaría el estado del editor
        // al hornear la escena.
        if (!Application.isPlaying) return;
        if (interactuable == null) return;
        if (registrados.Contains(interactuable)) return;
        registrados.Add(interactuable);
    }

    public static void Desregistrar(IInteractable interactuable)
    {
        if (!Application.isPlaying) return;
        if (interactuable == null) return;
        registrados.Remove(interactuable);
    }

    void OnDisable()
    {
        actual = null;
        ultimoAviso = null;
        if (marco != null) marco.gameObject.SetActive(false);
    }

    public void Inicializar(PlayerController jugador)
    {
        this.jugador = jugador;
    }

    void Update()
    {
        if (jugador == null) return;

        // Con la pausa o el cierre de nivel abiertos no se interactúa ni se muestra aviso.
        if (RelevoGame.UI != null && RelevoGame.UI.BloqueaJuego)
        {
            if (ultimoAviso != null)
            {
                ultimoAviso = null;
                GameEvents.RaisePromptCambiado("", true);
            }
            return;
        }

        actual = BuscarMejor();

        string aviso = "";
        bool disponible = true;
        if (actual != null)
        {
            disponible = actual.Disponible;
            aviso = disponible ? actual.Verbo : actual.Motivo;
        }

        if (aviso != ultimoAviso)
        {
            ultimoAviso = aviso;
            GameEvents.RaisePromptCambiado(aviso, disponible);
        }

        if (jugador.Entrada != null && jugador.Entrada.Pulsado(jugador.Entrada.Interactuar))
        {
            Ejecutar();
        }
    }

    IInteractable BuscarMejor()
    {
        IInteractable mejor = null;
        float mejorPuntuacion = float.MaxValue;
        Vector3 posicionJugador = transform.position;
        Vector3 direccionJugador = jugador != null ? jugador.DireccionPlano : Vector3.forward;

        for (int i = registrados.Count - 1; i >= 0; i--)
        {
            IInteractable candidato = registrados[i];
            Component componente = candidato as Component;
            if (componente == null)
            {
                registrados.RemoveAt(i);
                continue;
            }

            Transform ancla = candidato.Ancla;
            Vector3 punto = ancla != null ? ancla.position : componente.transform.position;
            Vector3 desplazamiento = new Vector3(punto.x - posicionJugador.x, 0f, punto.z - posicionJugador.z);
            float distancia = desplazamiento.magnitude;
            if (distancia > alcance) continue;

            Vector3 direccion = distancia > 0.001f ? desplazamiento / distancia : direccionJugador;
            float frente = Vector3.Dot(direccionJugador, direccion);
            float puntuacion = distancia - sesgoFrontal * frente;

            if (puntuacion < mejorPuntuacion)
            {
                mejorPuntuacion = puntuacion;
                mejor = candidato;
            }
        }

        return mejor;
    }

    /// <summary>Se ejecuta con una sola pulsación de E.</summary>
    public void Ejecutar()
    {
        if (actual == null) return;

        if (!actual.Disponible)
        {
            AudioDirector.Pedir(Tono.Bloqueado);
            GameEvents.RaiseMensajeMostrado("Aviso", actual.Motivo);
            return;
        }

        actual.Interactuar(jugador);
    }
}
