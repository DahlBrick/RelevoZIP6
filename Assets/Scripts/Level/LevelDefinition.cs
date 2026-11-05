using UnityEngine;

/// <summary>
/// Configuración de un nivel (textos y ajustes). Es un ScriptableObject para poder
/// ajustar la narrativa desde el Inspector sin tocar código; si no hay asset
/// asignado se usan los valores por defecto de DefinicionesPorDefecto.
/// </summary>
[CreateAssetMenu(fileName = "Nivel", menuName = "RELEVO/Definición de nivel")]
public class LevelDefinition : ScriptableObject
{
    [Header("Identidad")]
    public int indice = 1;
    public string nombre = "Nivel";

    [Header("Narrativa")]
    public string tituloIntro = "Liria";
    [TextArea(2, 4)] public string textoIntro = "";
    [TextArea(1, 3)] public string pistaMaia = "";
    [TextArea(1, 3)] public string textoCierre = "";

    [Header("Recuerdo opcional")]
    public string recuerdoIdentificador = "nivel1_recuerdo";
    public string recuerdoTitulo = "Recuerdo de Liria";
    [TextArea(2, 5)] public string recuerdoTexto = "";

    [Header("Patio de Bombas")]
    [Tooltip("Casillas que Nilo puede recorrer lejos de la batería. 0 = sin reserva (Nivel 1).")]
    public int reservaCasillas = 0;
    [Tooltip("Maia explica la reserva y las cajas con tarjetas ilustradas al empezar.")]
    public bool tutorialPatio = false;

    [Header("Faro de Liria")]
    [Tooltip("Maia explica los condensadores al empezar y los añade a su menú.")]
    public bool tutorialCondensadores = false;

    [Header("Final")]
    [Tooltip("Nivel 3: la salida es el faro y la batería se entrega físicamente.")]
    public bool salidaEsFaro = false;
}

/// <summary>Valores por defecto cuando el nivel no tiene un asset asignado.</summary>
public static class DefinicionesPorDefecto
{
    public static LevelDefinition Obtener(int indice)
    {
        LevelDefinition d = ScriptableObject.CreateInstance<LevelDefinition>();
        d.indice = indice;

        switch (indice)
        {
            case 2:
                d.nombre = "Nivel 2 - Patio de Bombas";
                d.tituloIntro = "Patio de Bombas";
                d.textoIntro = "Las bombas que drenaban Liria están paradas. Aquí la batería no solo abre caminos: también es lo que mantiene a Nilo en pie. Cada paso lejos de ella cuenta.";
                d.pistaMaia = "La caja del suroeste va a su placa empujándola primero al este. Desde la base A, ve directo a la palanca y vuelve. En el muelle este no hace falta hacerlo todo de un viaje: acércate a la base D para recargar.";
                d.textoCierre = "Las bombas vuelven a girar y el patio se vacía. El faro espera al final del canal.";
                d.reservaCasillas = 12;
                d.tutorialPatio = true;
                d.recuerdoIdentificador = "nivel2_recuerdo";
                d.recuerdoTitulo = "Recuerdo de Liria - El aprendiz de bombas";
                d.recuerdoTexto = "\"Aprendí a leer los cables antes que las letras. Turquesa es manual; ámbar, prestado.\"";
                break;

            case 3:
                d.nombre = "Nivel 3 - Faro de Liria";
                d.tituloIntro = "Faro de Liria";
                d.textoIntro = "El faro necesita la última batería. Aquí los puentes tienen condensadores: guardan un poco de energía cuando la batería se va.";
                d.pistaMaia = "Carga el condensador del todo antes de retirar la batería. La caja del pasillo va al norte, no al fondo. La palanca está demasiado lejos de D: llévale la batería en la mano.";
                d.reservaCasillas = 10;
                d.tutorialCondensadores = true;
                d.textoCierre = "El faro vuelve a señalar la ruta segura. Los primeros botes cruzan el canal.";
                d.recuerdoIdentificador = "nivel3_recuerdo";
                d.recuerdoTitulo = "Recuerdo de Liria - El farero";
                d.recuerdoTexto = "\"Si la luz se apaga, esperen. Alguien siempre vuelve con la energía.\"";
                d.salidaEsFaro = true;
                break;

            default:
                d.nombre = "Nivel 1 - Muelle de Cobre";
                d.tituloIntro = "Muelle de Cobre";
                d.textoIntro = "Una crecida cortó los caminos y la energía de Liria. Solo queda una batería compatible con el faro.";
                d.pistaMaia = "La batería abre el paso, pero la palanca abre el regreso. En el muelle hay dos orillas que cruzar: haz lo mismo dos veces.";
                d.textoCierre = "Las tres orillas del Muelle vuelven a estar conectadas. El Patio de Bombas sigue anegado.";
                d.recuerdoIdentificador = "nivel1_recuerdo";
                d.recuerdoTitulo = "Recuerdo de Liria - La barquera";
                d.recuerdoTexto = "\"Remaba al amanecer con las redes llenas. Si el faro vuelve a encenderse, volveré a cruzar el canal.\"";
                break;
        }

        return d;
    }
}
