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
            fichas[pagina] = Resources.Load<LevelDefinition>("Bitacora/Guiones/Puerto"+indice.ToString("00"));
            if (fichas[pagina] == null)
            {
                fichas[pagina] = Obtener(indice);
                fichas[pagina].hideFlags = HideFlags.HideAndDontSave;
            }
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
                d.textoIntro = "Las bombas que drenaban Liria están paradas desde la noche de la crecida. Maia dice que Nilo sabe cómo funcionan, aunque no lo recuerde. Aquí la batería no solo abre caminos: también es lo que mantiene a Nilo en pie. Cada paso lejos de ella cuenta.";
                d.pistaMaia = "La caja del suroeste va a su placa empujándola primero al este. Desde la base A, ve directo a la palanca y vuelve. En el muelle este no hace falta hacerlo todo de un viaje: acércate a la base D para recargar.";
                d.textoCierre = "Las bombas vuelven a girar y el patio se vacía. En el panel de mando, alguien dejó pegada con cinta la programación de la radio.";
                d.reservaCasillas = 12;
                d.tutorialPatio = true;
                d.recuerdoIdentificador = "nivel2_recuerdo";
                d.recuerdoTitulo = "Recuerdo 2 · El horario de la emisora";
                d.recuerdoTexto = "En el panel de las bombas está la programación de Radio Liria. A las 00:00, Maia. A las 00:15, la revisión de las bombas. Una mano de metal tachó las 00:15 y escribió encima: «después».";
                break;

            case 3:
                d.nombre = "Nivel 3 - Faro de Liria";
                d.tituloIntro = "Faro de Liria";
                d.textoIntro = "El faro necesita la última batería. Aquí los puentes tienen condensadores: guardan un poco de energía cuando la batería se va. Mientras Nilo trabaja, Maia tararea. Es la misma canción que sonaba en el muelle.";
                d.pistaMaia = "Carga el condensador del todo antes de retirar la batería. La caja del pasillo va al norte, no al fondo. La palanca está demasiado lejos de D: llévale la batería en la mano.";
                d.reservaCasillas = 10;
                d.tutorialCondensadores = true;
                d.textoCierre = "El faro vuelve a señalar la ruta segura. Durante un segundo, su luz parpadea al ritmo de la canción.";
                d.recuerdoIdentificador = "nivel3_recuerdo";
                d.recuerdoTitulo = "Recuerdo 3 · El faro que bailaba";
                d.recuerdoTexto = "Los barqueros se quejaban: de noche, el faro parpadeaba al compás de la radio. Nadie supo quién movía la luz. En el cuaderno del farero se lee: «Parece que alguien está bailando con ella».";
                d.salidaEsFaro = true;
                break;

            case 4:
                d.nombre = "Nivel 4 - Aduana de la Marea";
                d.tituloIntro = "Aduana de la Marea";
                d.textoIntro = "El agua ya lame los sellos de la aduana. Al otro lado del canal queda una puerta abierta para los botes del correo. En el mostrador se apilan cartas dirigidas a la emisora.";
                d.pistaMaia = "Lleva la caja al rombo y gira los discos con E hasta que el turquesa alcance la orilla. En el taller, sigue el ámbar hasta la pasarela de la galería.";
                d.reservaCasillas = 18;
                d.textoCierre = "El primer saco de cartas sale en la barca de la tarde. En el mostrador queda un sobre para la emisora. El matasellos es de la noche de la crecida.";
                d.recuerdoIdentificador = "nivel4_recuerdo";
                d.recuerdoTitulo = "Recuerdo 4 · Cartas a la emisora";
                d.recuerdoTexto = "Un sobre para Radio Liria, sin enviar: «Cada noche, cuando empieza su programa, las farolas de la calle bajan. Las bombas también. ¿Es casualidad?». Alguien lo guardó en el cajón de las cartas sin dueño.";
                break;

            case 5:
                d.nombre = "Nivel 5 - Dársena del Torno";
                d.tituloIntro = "Dársena del Torno";
                d.textoIntro = "El torno sigue amarrado en la punta oeste. Si Nilo suelta el cable, las dos mitades de la dársena volverán a comunicarse. Maia le pide que no se entretenga con los avisos clavados en el torno.";
                d.pistaMaia = "Cruza con la carga de D. Desde el taller, manda corriente hacia el pequeño muelle del oeste. Su torno abre el camino a la otra orilla.";
                d.reservaCasillas = 20;
                d.textoCierre = "Vuelve a oírse el torno. Alguien del otro lado responde con tres golpes de remo. Maia tarda un poco en volver a hablar.";
                d.recuerdoIdentificador = "nivel5_recuerdo";
                d.recuerdoTitulo = "Recuerdo 5 · El cierre de la emisora";
                d.recuerdoTexto = "Un aviso clavado en el torno: «Radio Liria dejará de emitir a fin de mes por falta de energía». El papel tiene tres marcas de óxido, como si una pinza de metal lo hubiera apretado demasiado.";
                break;

            case 6:
                d.nombre = "Nivel 6 - Esclusa de las Dos Llaves";
                d.tituloIntro = "Esclusa de las Dos Llaves";
                d.textoIntro = "La esclusa tiene dos llaves de presión. Los estibadores se llevaron todas las cajas menos una. Maia está contenta: dice que aquí, la noche de la crecida, Nilo fue un héroe.";
                d.pistaMaia = "Empieza por el panel del oeste. Cuando la palanca abra el paso sur, recupera la misma caja para los barrotes. En la sala de máquinas hay otro camino de vuelta.";
                d.reservaCasillas = 22;
                d.textoCierre = "Las dos hojas de la esclusa se abren a la vez. «¿Lo ves? Siempre fuiste bueno en esto», dice Maia. Nilo no recuerda haberlo hecho nunca.";
                d.recuerdoIdentificador = "nivel6_recuerdo";
                d.recuerdoTitulo = "Recuerdo 6 · El héroe de la esclusa";
                d.recuerdoTexto = "Aquella noche Nilo abrió las dos hojas de la esclusa y todas las barcas salieron antes de la crecida. Nadie se quedó atrás. Liria entera le dio las gracias. (El recuerdo es nítido, cálido y perfecto. Demasiado perfecto.)";
                break;

            case 7:
                d.nombre = "Nivel 7 - Lonja de las Dos Voces";
                d.tituloIntro = "Lonja de las Dos Voces";
                d.textoIntro = "La radio de la lonja enciende su piloto. «Buenas noches a quienes siguen de turno». Al norte esperan los puestos del mercado. Al sur está la emisora. Solo la primera palanca que bajes recibirá corriente.";
                d.pistaMaia = "Desde el muelle de carga, la caja alimenta la línea turquesa del mercado. La batería alimenta la línea ámbar de la emisora.";
                d.textoCierre = "En la taquilla queda una llave del archivo. El papel que la envuelve lleva la fecha de la crecida.";
                d.recuerdoIdentificador = "nivel7_recuerdo";
                d.recuerdoTitulo = "Recuerdo 7 · Las dos etiquetas";
                d.recuerdoTexto = "En el tablero, la etiqueta «RADIO» se despega por una esquina. Debajo todavía se lee «BOMBA DE LA LONJA». Las dos tienen los mismos agujeros. Alguien cambió un cable por otro.";
                d.reservaCasillas = 22;
                break;

            case 8:
                d.nombre = "Nivel 8 - Archivo de la Frecuencia";
                d.tituloIntro = "Archivo de la Frecuencia";
                d.textoIntro = "En el archivo, una cinta se pone en marcha. «Buenas noches a quienes siguen de turno». El reloj de la cabina marca las doce y diecisiete. Alguien ha dejado dos cajas junto a la reja.";
                d.pistaMaia = "Una caja sostiene los barrotes. Empuja la otra hasta el panel del taller. La palanca del archivo deja libre el regreso a por la batería.";
                d.textoCierre = "La cinta llega al final. Tres segundos de agua. Un clic. Maia vuelve a dar las buenas noches. En la etiqueta alguien escribió «conservar el original».";
                d.recuerdoIdentificador = "nivel8_recuerdo";
                d.recuerdoTitulo = "Recuerdo 8 · Cinta de las 00:17";
                d.recuerdoTexto = "El carrete dice «Emisión de la noche de la crecida». Antes de la voz de Maia se oyen dos golpes secos: dos interruptores al bajar. En la tapa, donde iría la firma del operador, queda un rectángulo más claro.";
                d.reservaCasillas = 22;
                break;

            case 9:
                d.nombre = "Nivel 9 - Astillero de la Maroma";
                d.tituloIntro = "Astillero de la Maroma";
                d.textoIntro = "Los tablones del astillero siguen gastados por dos pares de botas. Un par de huellas acaba en el torno. El otro, en una placa con un solo trazo. En la cabina espera un carrete vacío. Maia tarda en contestar.";
                d.pistaMaia = "Deja la batería en la base A para llegar a la placa I. Graba una espera allí y, durante el eco, recupera la batería y embarca.";
                d.textoCierre = "El pontón vuelve a rozar la grada. Entre las tablillas aparece un destino tachado del registro: Casa de las Dos Guardias.";
                d.reservaCasillas = 10;
                d.recuerdoIdentificador = "nivel9_recuerdo";
                d.recuerdoTitulo = "Recuerdo 9 · Ficha de embarque partida";
                d.recuerdoTexto = "Una mitad lleva un trazo y la otra un número de turno. Al juntarlas se lee a lápiz: «Hoy cruzó mi hermano. Yo me quedé en el torno. Se fue la luz y no pude volver».";
                break;

            case 10:
                d.nombre = "Nivel 10 - Casa de las Dos Guardias";
                d.tituloIntro = "Casa de las Dos Guardias";
                d.textoIntro = "En las dos orillas hay sendas placas con dos trazos. En el libro de guardia de la noche de la crecida solo figura una firma. La segunda casilla tiene una mancha circular, del tamaño de un carrete.";
                d.pistaMaia = "Graba primero la señal de embarque en I. Termina sobre II. En la otra orilla, coloca la batería en la base A y cruza a la placa gemela.";
                d.textoCierre = "Al cerrarse los dos contactos, la radio deja pasar el final de la cinta. El libro de guardia queda abierto en la página de la crecida. Maia no dice nada. Por primera vez, su radio se apaga sola.";
                d.reservaCasillas = 8;
                d.recuerdoIdentificador = "nivel10_recuerdo";
                d.recuerdoTitulo = "Recuerdo 10 · El renglón del operador";
                d.recuerdoTexto = "Al trasluz aparece el renglón que faltaba del registro: «00:17. Carga de las bombas desviada a la emisora. Operador de guardia: NILO». No hay firma. Hay un código. Nilo lo reconoce: es el suyo.";
                break;

            case 11:
                d.nombre = "Nivel 11 - Mercado de la Sal";
                d.tituloIntro = "Mercado de la Sal";
                d.textoIntro = "Todavía queda sal en los bordes de las mesas. La reja del mercado está echada y el montacargas espera al otro lado del canal. Ahora Nilo sabe por qué se inundó el mercado. La radio de Maia suena a ratos, entre ruido.";
                d.pistaMaia = "El cajón sostiene la reja. Deja la batería en la base del mercado y busca la palanca al otro lado. Cuando vuelvas a por ella, la pasarela seguirá tendida.";
                d.textoCierre = "La balanza recupera el equilibrio. Un hilo de agua vuelve a correr entre los bancales. «No fue culpa tuya», dice la radio. La voz ya no se parece a la de Maia.";
                d.reservaCasillas = 20;
                d.recuerdoIdentificador = "nivel11_recuerdo";
                d.recuerdoTitulo = "Recuerdo 11 · Nada que pesar";
                d.recuerdoTexto = "Las mesas guardan las marcas de lo que pesaban: redes, sal, pan. En la última, una nota para nadie: «Volvimos cuando bajó el agua. No quedaba nada que pesar». Debajo de la balanza, un trocito de cobre para que volviera a marcar lo justo.";
                break;

            case 12:
                d.nombre = "Nivel 12 - Varadero de las Campanas";
                d.tituloIntro = "Varadero de las Campanas";
                d.textoIntro = "Las campanas del varadero avisaban a quienes trabajaban de espaldas al agua. Una cuelga junto al puente de carga. La otra, frente al horno. Al fondo, el banco de fundición sigue templado. Nilo ha estado aquí antes.";
                d.pistaMaia = "Deja el cajón en el rombo del taller. La campana anuncia que puedes cruzar. En la cabina, graba una espera sobre I y embarca cuando el eco tome el relevo.";
                d.textoCierre = "Las dos campanas vuelven a distinguirse desde el agua. Sobre el banco de fundición queda espacio para la próxima pieza. Más allá del varadero, en la subestación, algo enorme se pone en pie.";
                d.reservaCasillas = 18;
                d.recuerdoIdentificador = "nivel12_recuerdo";
                d.recuerdoTitulo = "Recuerdo 12 · El disco fundido";
                d.recuerdoTexto = "En el banco de fundición queda un molde con la forma de un disco de memoria. El metal se enfrió a medias. Nilo vino aquí después de la crecida, a olvidar. En el borde del molde, raspado con una pinza: «La esclusa nunca se abrió».";
                break;

            case 13:
                d.nombre = "Nivel 13 - La Sala del Relevo";
                d.tituloIntro = "La Sala del Relevo";
                d.textoIntro = "En la subestación todo sigue como aquella noche. Una figura enorme de tinta y cobre se levanta en el centro de la sala. Tiene la forma de Nilo y viene a por él. Mientras exista, Liria seguirá a oscuras.";
                d.pistaMaia = "No puedes golpearlo, Nilo, pero las torres sí. Pon la batería en una torre, espera a que se cargue y atrae al Eco hasta ella. Esquiva sus embestidas y aléjate cuando brille: su pulso hace daño y repliega los puentes.";
                d.textoCierre = "El Eco se deshace en la luz y, una a una, las ventanas de Liria se encienden. En la radio, la voz de Maia suena muy lejos: «Apagarse también es parte de la canción». Después, solo silencio.";
                d.reservaCasillas = 22;
                d.recuerdoIdentificador = "";
                d.recuerdoTitulo = "";
                break;

            default:
                d.nombre = "Nivel 1 - Muelle de Cobre";
                d.tituloIntro = "Muelle de Cobre";
                d.textoIntro = "«Buenas noches a quienes siguen de turno». La voz sale de una radio junto al muelle. Se llama Maia y dice que llevaba mucho tiempo esperando a alguien. Liria está a oscuras y los puentes siguen bajo el agua. Junto a Nilo queda una batería: encaja en su pecho como si siempre hubiera sido suya.";
                d.pistaMaia = "La batería abre el paso, pero la palanca abre el regreso. En el muelle hay dos orillas que cruzar: haz lo mismo dos veces.";
                d.textoCierre = "Las tres orillas del Muelle vuelven a estar conectadas. Maia le da las gracias a Nilo como si hubiera vuelto de un viaje muy largo.";
                d.recuerdoIdentificador = "nivel1_recuerdo";
                d.recuerdoTitulo = "Recuerdo 1 · La primera canción";
                d.recuerdoTexto = "Entre el zumbido de las subestaciones, una noche, algo cantó. No era una alarma ni una avería. Era una voz que decía: «Buenas noches a quienes siguen de turno». Nilo dejó de contar voltios para escucharla.";
                break;
        }

        return d;
    }
}
