using UnityEngine;

/// <summary>
/// Contexto de la partida en curso. Se rellena una sola vez al construir el nivel,
/// de modo que los interactuables puedan consultar al jugador o a la batería sin
/// llamar a FindObjectOfType durante el juego.
/// </summary>
public static class RelevoGame
{
    public static PlayerController Jugador;
    public static PlayerInteractor Interactor;
    public static BatteryController Bateria;
    public static LevelManager Nivel;
    public static UIManager UI;
    public static RelevoInput Input;
    public static LevelDefinition Definicion;
    public static LevelLayout Layout;
    public static ReservaEnergia Reserva;

    public static void Limpiar()
    {
        Jugador = null;
        Interactor = null;
        Bateria = null;
        Nivel = null;
        UI = null;
        Input = null;
        Definicion = null;
        Layout = null;
        Reserva = null;
    }
}
