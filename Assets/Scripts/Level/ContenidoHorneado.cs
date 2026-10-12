using UnityEngine;

/// <summary>
/// Marca los objetos raíz que genera LevelBootstrap: la maqueta ("Nivel"), la cámara
/// ("Camara") y los actores ("Actores").
///
/// La herramienta de horneado la usa para reconocer su propio contenido y limpiarlo antes
/// de reconstruirlo, de modo que hornear dos veces seguidas no duplique la escena y no
/// toque nunca objetos añadidos a mano. Es un componente vacío: no tiene lógica ni coste.
/// </summary>
[DisallowMultipleComponent]
public class ContenidoHorneado : MonoBehaviour
{
}
