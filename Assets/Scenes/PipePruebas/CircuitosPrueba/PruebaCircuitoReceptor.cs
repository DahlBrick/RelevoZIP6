using UnityEngine;

/// <summary>
/// PROTOTIPO - Circuitos de energia. Adaptador de salida.
///
/// No reimplementa nada: llama a Compuerta.Abrir(bool) o a TemporaryBridge.Activar(bool).
/// Anade la unica proteccion que TemporaryBridge no trae de por si: no replegarse bajo Nilo.
/// Si se pierde la alimentacion con Nilo encima, el puente se queda desplegado hasta que quede
/// libre, y se repliega en cuanto deja de estarlo.
/// </summary>
public class PruebaCircuitoReceptor : MonoBehaviour
{
    public enum Clase { Compuerta, Puente }

    [Header("Destino")]
    [SerializeField] Clase clase = Clase.Compuerta;
    [SerializeField] Compuerta compuerta;
    [SerializeField] TemporaryBridge puente;

    [Header("Red")]
    [SerializeField] Vector2Int casilla;
    [SerializeField] int puerto = 1;

    bool alimentado;

    public Vector2Int Casilla { get { return casilla; } }
    public int Puerto { get { return puerto; } }

    public void Aplicar(bool activo)
    {
        alimentado = activo;

        if (clase == Clase.Compuerta)
        {
            if (compuerta != null) compuerta.Abrir(activo);
            return;
        }

        if (puente == null) return;

        if (activo) { puente.Activar(true); return; }
        if (!puente.Desplegado || NiloEncima()) return;

        puente.Activar(false);
    }

    void Update()
    {
        if (alimentado || clase != Clase.Puente) return;
        if (puente == null || !puente.Desplegado || NiloEncima()) return;

        puente.Activar(false);
    }

    bool NiloEncima()
    {
        PlayerController nilo = RelevoGame.Jugador;
        return puente != null && nilo != null && puente.Contiene(nilo.transform.position);
    }
}
