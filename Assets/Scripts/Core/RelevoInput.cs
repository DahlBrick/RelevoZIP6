using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Envoltorio del asset de entrada del juego (Assets/Input/Relevo.inputactions).
/// Todas las acciones de interacción son de pulsación única: el código escucha
/// únicamente 'performed' / WasPressedThisFrame, nunca el estado sostenido.
/// </summary>
public sealed class RelevoInput
{
    public readonly InputActionAsset Asset;

    readonly InputActionMap juego;
    readonly InputActionMap ui;

    public readonly InputAction Mover;
    public readonly InputAction Interactuar;
    public readonly InputAction Cuaderno;
    public readonly InputAction Pausa;
    public readonly InputAction Reiniciar;
    public readonly InputAction Confirmar;
    public readonly InputAction Cancelar;

    public RelevoInput(InputActionAsset asset)
    {
        Asset = asset;
        if (asset == null)
        {
            Debug.LogError("RELEVO: no hay asset de entrada asignado.");
            return;
        }

        // throwIfNotFound = false: si falta una acción el juego sigue funcionando sin ella.
        juego = asset.FindActionMap("Gameplay", false);
        ui = asset.FindActionMap("UI", false);

        Mover = Buscar(juego, "Move");
        Interactuar = Buscar(juego, "Interact");
        Cuaderno = Buscar(juego, "Notebook");
        Pausa = Buscar(juego, "Pause");
        Reiniciar = Buscar(juego, "Restart");
        Confirmar = Buscar(juego, "Confirm");
        Cancelar = Buscar(juego, "Cancel");

        Activar();
    }

    static InputAction Buscar(InputActionMap mapa, string nombre)
    {
        if (mapa == null) return null;
        InputAction accion = mapa.FindAction(nombre, false);
        if (accion == null) Debug.LogWarning("RELEVO: falta la acción '" + nombre + "' en el asset de entrada.");
        return accion;
    }

    public void Activar()
    {
        if (juego != null) juego.Enable();
        if (ui != null) ui.Enable();
    }

    public void Desactivar()
    {
        if (juego != null) juego.Disable();
        if (ui != null) ui.Disable();
    }

    /// <summary>Movimiento como Vector2 (0 si la acción no está disponible).</summary>
    public Vector2 LeerMovimiento()
    {
        return Mover != null ? Mover.ReadValue<Vector2>() : Vector2.zero;
    }

    public bool Pulsado(InputAction accion)
    {
        return accion != null && accion.WasPressedThisFrame();
    }
}
