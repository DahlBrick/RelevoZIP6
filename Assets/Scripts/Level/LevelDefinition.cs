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
    static readonly LevelDefinition[] fichas = new LevelDefinition[LevelLayouts.Cantidad];

    public static LevelDefinition FichaDelCuaderno(int indice)
    {
        int pagina = indice - 1;
        if (pagina < 0 || pagina >= fichas.Length) return null;
        if (fichas[pagina] == null)
        {
            fichas[pagina] = Obtener(indice);
            fichas[pagina].hideFlags = HideFlags.HideAndDontSave;
        }
        return fichas[pagina];
    }

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

            case 4:
                d.nombre = "Nivel 4 - Aduana de la Marea";
                d.tituloIntro = "Aduana de la Marea";
                d.textoIntro = "El agua ya lame los sellos de la aduana. Al otro lado del canal queda una puerta abierta para los botes del correo.";
                d.pistaMaia = "Lleva la caja al rombo y gira los discos con E hasta que el turquesa alcance la orilla. En el taller, sigue el ámbar hasta la pasarela de la galería.";
                d.reservaCasillas = 18;
                d.textoCierre = "El primer saco de cartas sale en la barca de la tarde. En el mostrador queda un sobre para la emisora. El matasellos es de la noche de la crecida.";
                d.recuerdoIdentificador = "nivel4_recuerdo";
                d.recuerdoTitulo = "La última saca";
                d.recuerdoTexto = "«Guardábamos arriba las cartas sin dueño. Cuando bajaba la marea, alguien siempre venía a preguntar.»";
                break;

            case 5:
                d.nombre = "Nivel 5 - Dársena del Torno";
                d.tituloIntro = "Dársena del Torno";
                d.textoIntro = "El torno sigue amarrado en la punta oeste. Si Nilo suelta el cable, las dos mitades de la dársena volverán a comunicarse.";
                d.pistaMaia = "Cruza con la carga de D. Desde el taller, manda corriente hacia el pequeño muelle del oeste. Su torno abre el camino a la otra orilla.";
                d.reservaCasillas = 20;
                d.textoCierre = "Vuelve a oírse el torno. Alguien del otro lado responde con tres golpes de remo.";
                d.recuerdoIdentificador = "nivel5_recuerdo";
                d.recuerdoTitulo = "Las marcas del torno";
                d.recuerdoTexto = "«Una raya en la baranda por cada vuelta de manivela. Las de abajo las hice cuando apenas llegaba al torno.»";
                break;

            case 6:
                d.nombre = "Nivel 6 - Esclusa de las Dos Llaves";
                d.tituloIntro = "Esclusa de las Dos Llaves";
                d.textoIntro = "La esclusa tiene dos llaves de presión. Los estibadores se llevaron todas las cajas menos una.";
                d.pistaMaia = "Empieza por el panel del oeste. Cuando la palanca abra el paso sur, recupera la misma caja para los barrotes. En la sala de máquinas hay otro camino de vuelta.";
                d.reservaCasillas = 22;
                d.textoCierre = "Las dos hojas de la esclusa se abren a la vez. Por primera vez desde la crecida, una barca recorre el canal entero.";
                d.recuerdoIdentificador = "nivel6_recuerdo";
                d.recuerdoTitulo = "El turno de noche";
                d.recuerdoTexto = "«Mi hermana abría la primera hoja. Yo esperaba el golpe de su llave para abrir la segunda.»";
                break;

            case 7:
                d.nombre = "Nivel 7 - Lonja de las Dos Voces";
                d.tituloIntro = "Lonja de las Dos Voces";
                d.textoIntro = "La radio de la lonja enciende su piloto. «Buenas noches a quienes siguen de turno». Al norte esperan los puestos del mercado. Al sur está la emisora. Solo la primera palanca que bajes recibirá corriente.";
                d.pistaMaia = "Desde el muelle de carga, la caja alimenta la línea turquesa del mercado. La batería alimenta la línea ámbar de la emisora.";
                d.textoCierre = "En la taquilla queda una llave del archivo. El papel que la envuelve lleva la fecha de la crecida.";
                d.recuerdoIdentificador = "nivel7_recuerdo";
                d.recuerdoTitulo = "Las dos etiquetas";
                d.recuerdoTexto = "En el tablero, la etiqueta «RADIO» se despega por una esquina. Debajo todavía se lee «BOMBA DE LA LONJA». Las dos tienen los mismos agujeros.";
                d.reservaCasillas = 22;
                break;

            case 8:
                d.nombre = "Nivel 8 - Archivo de la Frecuencia";
                d.tituloIntro = "Archivo de la Frecuencia";
                d.textoIntro = "En el archivo, una cinta se pone en marcha. «Buenas noches a quienes siguen de turno». El reloj de la cabina marca las doce y diecisiete. Alguien ha dejado dos cajas junto a la reja.";
                d.pistaMaia = "Una caja sostiene los barrotes. Empuja la otra hasta el panel del taller. La palanca del archivo deja libre el regreso a por la batería.";
                d.textoCierre = "La cinta llega al final. Tres segundos de agua. Un clic. Maia vuelve a dar las buenas noches. En la etiqueta alguien escribió «conservar el original».";
                d.recuerdoIdentificador = "nivel8_recuerdo";
                d.recuerdoTitulo = "Cinta de las 00:17";
                d.recuerdoTexto = "El carrete dice «Emisión de la noche de la crecida». Antes de la voz de Maia se oyen dos golpes secos. En la tapa, donde iría la firma del operador, queda un rectángulo más claro.";
                d.reservaCasillas = 22;
                break;

            default:
                d.nombre = "Nivel 1 - Muelle de Cobre";
                d.tituloIntro = "Muelle de Cobre";
                d.textoIntro = "«Buenas noches a quienes siguen de turno». La voz sale de una radio junto al muelle. Tras la crecida, los puentes siguen bajo el agua. Junto a Nilo queda una batería compatible con el faro.";
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
