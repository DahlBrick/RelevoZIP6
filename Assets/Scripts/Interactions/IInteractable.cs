using UnityEngine;

/// <summary>
/// Todo lo que Nilo puede usar con E. Cada interactuable dice si está disponible y,
/// cuando no lo está, explica el motivo: una acción inválida nunca se ejecuta en
/// silencio, se muestra su causa en el aviso contextual.
/// </summary>
public interface IInteractable
{
    /// <summary>Punto de referencia para el aviso contextual y la distancia.</summary>
    Transform Ancla { get; }

    /// <summary>¿Se puede interactuar ahora mismo?</summary>
    bool Disponible { get; }

    /// <summary>Verbo mostrado cuando sí se puede ("Recoger batería").</summary>
    string Verbo { get; }

    /// <summary>Explicación mostrada cuando no se puede ("Necesitas la batería").</summary>
    string Motivo { get; }

    /// <summary>Acción de pulsación única de E.</summary>
    void Interactuar(PlayerController jugador);
}
