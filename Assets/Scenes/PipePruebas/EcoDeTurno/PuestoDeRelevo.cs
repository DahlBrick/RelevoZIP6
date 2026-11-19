using UnityEngine;

/// <summary>Punto de uso en tierra del magnetófono o de uno de los dos tornos.</summary>
public sealed class PuestoDeRelevo : MonoBehaviour, IInteractable
{
    [SerializeField] EcoDeTurno cinta;
    [SerializeField] TransbordadorDeMaroma barca;
    [SerializeField] bool regreso;

    public void Configurar(EcoDeTurno grabadora, TransbordadorDeMaroma nave, bool vuelta)
    {
        cinta = grabadora;
        barca = nave;
        regreso = vuelta;
    }

    public Transform Ancla { get { return transform; } }
    public bool Disponible { get { return barca == null ? cinta.PuedeGrabar : barca.PuedeZarpar(regreso); } }
    public string Verbo { get { return barca == null ? "Grabar un turno" : regreso ? "Volver al magnetófono" : "Embarcar en el transbordador"; } }
    public string Motivo { get { return barca == null ? "Turno registrado" : barca.Motivo(regreso); } }
    public void Interactuar(PlayerController jugador)
    {
        if (!Disponible) return;
        if (barca == null) cinta.IniciarToma();
        else barca.Zarpar(jugador, regreso);
    }
    void OnEnable() { PlayerInteractor.Registrar(this); }
    void OnDisable() { PlayerInteractor.Desregistrar(this); }
}
