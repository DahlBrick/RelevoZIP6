using UnityEngine;

/// <summary>
/// PROTOTIPO - Circuitos de energia. Adaptador de entrada.
///
/// Lee el estado que ya exponen los mecanismos compartidos, sin modificarlos:
/// PlacaPresion.Presionada o EnergySocket.Energizada. Aporta la casilla y el puerto por el que
/// la energia entra en la red.
/// </summary>
public class PruebaCircuitoFuente : MonoBehaviour
{
    public enum Clase { Placa, BaseEnergia }

    [Header("Origen")]
    [SerializeField] Clase clase = Clase.Placa;
    [SerializeField] PlacaPresion placa;
    [SerializeField] EnergySocket baseEnergia;

    [Header("Red")]
    [SerializeField] Vector2Int casilla;
    [SerializeField] int puerto = 2;

    public Vector2Int Casilla { get { return casilla; } }
    public int Puerto { get { return puerto; } }

    /// <summary>Tipo de mecanismo que alimenta la red. Decide el color con el que se pinta.</summary>
    public Clase Tipo { get { return clase; } }

    /// <summary>Verdadero mientras la placa este pisada o la base tenga la bateria insertada.</summary>
    public bool Activa
    {
        get
        {
            if (clase == Clase.Placa) return placa != null && placa.Presionada;
            return baseEnergia != null && baseEnergia.Energizada;
        }
    }
}
