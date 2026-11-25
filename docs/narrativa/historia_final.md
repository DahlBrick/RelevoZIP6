# RELEVO · Historia final

Guion de referencia del juego completo. Los textos de cada puerto viven en `Assets/Scripts/Level/LevelDefinition.cs` y se copian a `Resources/Bitacora/Guiones` con **Relevo → Sincronizar guiones con el código**. Las reglas de finales, decisiones y voz de Maia están en `Assets/Scripts/Core/Historia.cs`.

## La historia en una frase

Nilo, un robot operador de la subestación de Liria, despierta sin memoria la noche del apagón. Ayuda a Maia, la voz de Radio Liria, a devolver la luz puerto a puerto, y descubre que fue él quien causó el apagón: desvió la carga de las bombas a la emisora para que Maia, que iba a cerrar, siguiera sonando. Después fundió su disco de memoria para olvidarlo.

## Reglas fijas

- Nilo no habla. Maia es una radio, nunca una persona. No aparecen habitantes ni sus sombras: Liria se cuenta por objetos (cartas, etiquetas, cintas, huellas).
- Un solo estilo visual: cómic con tinta, el de los niveles.
- Flujo: **Título → cinemática de apertura → 13 puertos → final → créditos → título**.

## Arco

| Tramo | Puertos | Qué cree el jugador | Qué es verdad |
|---|---|---|---|
| Despertar | 1-3 | Nilo es un técnico que ayuda a una radio amable | Maia lo conoce de antes y no se lo dice |
| Sospecha | 4-6 | Algo pasaba cada noche al empezar el programa | El 6 da un recuerdo **falso**: el de un Nilo héroe |
| Revelación | 7-10 | Alguien cambió un cable | En el 10 se confirma: el operador era Nilo |
| Culpa | 11-12 | Nilo sabe lo que hizo | Ya intentó olvidarlo una vez |
| El Eco | 13 | Hay que derrotar al Eco del Apagón | El Eco es Nilo aquella noche |

## Los 12 puertos

### 1 · Muelle de Cobre

- **Entrada:** «Buenas noches a quienes siguen de turno». La voz sale de una radio junto al muelle. Se llama Maia y dice que llevaba mucho tiempo esperando a alguien. Liria está a oscuras y los puentes siguen bajo el agua. Junto a Nilo queda una batería: encaja en su pecho como si siempre hubiera sido suya.
- **Cierre:** Las tres orillas del Muelle vuelven a estar conectadas. Maia le da las gracias a Nilo como si hubiera vuelto de un viaje muy largo.
- **Recuerdo 1 · La primera canción:** Entre el zumbido de las subestaciones, una noche, algo cantó. No era una alarma ni una avería. Era una voz que decía: «Buenas noches a quienes siguen de turno». Nilo dejó de contar voltios para escucharla.
- **Voz de Maia:** Limpia

### 2 · Patio de Bombas

- **Entrada:** Las bombas que drenaban Liria están paradas desde la noche de la crecida. Maia dice que Nilo sabe cómo funcionan, aunque no lo recuerde. Aquí la batería no solo abre caminos: también es lo que mantiene a Nilo en pie. Cada paso lejos de ella cuenta.
- **Cierre:** Las bombas vuelven a girar y el patio se vacía. En el panel de mando, alguien dejó pegada con cinta la programación de la radio.
- **Recuerdo 2 · El horario de la emisora:** En el panel de las bombas está la programación de Radio Liria. A las 00:00, Maia. A las 00:15, la revisión de las bombas. Una mano de metal tachó las 00:15 y escribió encima: «después».
- **Voz de Maia:** Limpia

### 3 · Faro de Liria

- **Entrada:** El faro necesita la última batería. Aquí los puentes tienen condensadores: guardan un poco de energía cuando la batería se va. Mientras Nilo trabaja, Maia tararea. Es la misma canción que sonaba en el muelle.
- **Cierre:** El faro vuelve a señalar la ruta segura. Durante un segundo, su luz parpadea al ritmo de la canción.
- **Recuerdo 3 · El faro que bailaba:** Los barqueros se quejaban: de noche, el faro parpadeaba al compás de la radio. Nadie supo quién movía la luz. En el cuaderno del farero se lee: «Parece que alguien está bailando con ella».
- **Voz de Maia:** Limpia

### 4 · Aduana de la Marea

- **Entrada:** El agua ya lame los sellos de la aduana. Al otro lado del canal queda una puerta abierta para los botes del correo. En el mostrador se apilan cartas dirigidas a la emisora.
- **Cierre:** El primer saco de cartas sale en la barca de la tarde. En el mostrador queda un sobre para la emisora. El matasellos es de la noche de la crecida.
- **Recuerdo 4 · Cartas a la emisora:** Un sobre para Radio Liria, sin enviar: «Cada noche, cuando empieza su programa, las farolas de la calle bajan. Las bombas también. ¿Es casualidad?». Alguien lo guardó en el cajón de las cartas sin dueño.
- **Voz de Maia:** Limpia

### 5 · Dársena del Torno

- **Entrada:** El torno sigue amarrado en la punta oeste. Si Nilo suelta el cable, las dos mitades de la dársena volverán a comunicarse. Maia le pide que no se entretenga con los avisos clavados en el torno.
- **Cierre:** Vuelve a oírse el torno. Alguien del otro lado responde con tres golpes de remo. Maia tarda un poco en volver a hablar.
- **Recuerdo 5 · El cierre de la emisora:** Un aviso clavado en el torno: «Radio Liria dejará de emitir a fin de mes por falta de energía». El papel tiene tres marcas de óxido, como si una pinza de metal lo hubiera apretado demasiado.
- **Voz de Maia:** Cortes («…»)

### 6 · Esclusa de las Dos Llaves

- **Entrada:** La esclusa tiene dos llaves de presión. Los estibadores se llevaron todas las cajas menos una. Maia está contenta: dice que aquí, la noche de la crecida, Nilo fue un héroe.
- **Cierre:** Las dos hojas de la esclusa se abren a la vez. «¿Lo ves? Siempre fuiste bueno en esto», dice Maia. Nilo no recuerda haberlo hecho nunca.
- **Recuerdo 6 · El héroe de la esclusa:** Aquella noche Nilo abrió las dos hojas de la esclusa y todas las barcas salieron antes de la crecida. Nadie se quedó atrás. Liria entera le dio las gracias. (El recuerdo es nítido, cálido y perfecto. Demasiado perfecto.)
- **Voz de Maia:** Cortes («…»)

### 7 · Lonja de las Dos Voces

- **Entrada:** La radio de la lonja enciende su piloto. «Buenas noches a quienes siguen de turno». Al norte esperan los puestos del mercado. Al sur está la emisora. Solo la primera palanca que bajes recibirá corriente.
- **Cierre:** En la taquilla queda una llave del archivo. El papel que la envuelve lleva la fecha de la crecida.
- **Recuerdo 7 · Las dos etiquetas:** En el tablero, la etiqueta «RADIO» se despega por una esquina. Debajo todavía se lee «BOMBA DE LA LONJA». Las dos tienen los mismos agujeros. Alguien cambió un cable por otro.
- **Voz de Maia:** Ruido («—kssh—», repite palabras)

### 8 · Archivo de la Frecuencia

- **Entrada:** En el archivo, una cinta se pone en marcha. «Buenas noches a quienes siguen de turno». El reloj de la cabina marca las doce y diecisiete. Alguien ha dejado dos cajas junto a la reja.
- **Cierre:** La cinta llega al final. Tres segundos de agua. Un clic. Maia vuelve a dar las buenas noches. En la etiqueta alguien escribió «conservar el original».
- **Recuerdo 8 · Cinta de las 00:17:** El carrete dice «Emisión de la noche de la crecida». Antes de la voz de Maia se oyen dos golpes secos: dos interruptores al bajar. En la tapa, donde iría la firma del operador, queda un rectángulo más claro.
- **Voz de Maia:** Ruido

### 9 · Astillero de la Maroma

- **Entrada:** Los tablones del astillero siguen gastados por dos pares de botas. Un par de huellas acaba en el torno. El otro, en una placa con un solo trazo. En la cabina espera un carrete vacío. Maia tarda en contestar.
- **Cierre:** El pontón vuelve a rozar la grada. Entre las tablillas aparece un destino tachado del registro: Casa de las Dos Guardias.
- **Recuerdo 9 · Ficha de embarque partida:** Una mitad lleva un trazo y la otra un número de turno. Al juntarlas se lee a lápiz: «Hoy cruzó mi hermano. Yo me quedé en el torno. Se fue la luz y no pude volver».
- **Voz de Maia:** Ruido

### 10 · Casa de las Dos Guardias

- **Entrada:** En las dos orillas hay sendas placas con dos trazos. En el libro de guardia de la noche de la crecida solo figura una firma. La segunda casilla tiene una mancha circular, del tamaño de un carrete.
- **Cierre:** Al cerrarse los dos contactos, la radio deja pasar el final de la cinta. El libro de guardia queda abierto en la página de la crecida. Maia no dice nada. Por primera vez, su radio se apaga sola.
- **Recuerdo 10 · El renglón del operador:** Al trasluz aparece el renglón que faltaba del registro: «00:17. Carga de las bombas desviada a la emisora. Operador de guardia: NILO». No hay firma. Hay un código. Nilo lo reconoce: es el suyo.
- **Voz de Maia:** Se rompe (letras perdidas, estribillo intruso)

### 11 · Mercado de la Sal

- **Entrada:** Todavía queda sal en los bordes de las mesas. La reja del mercado está echada y el montacargas espera al otro lado del canal. Ahora Nilo sabe por qué se inundó el mercado. La radio de Maia suena a ratos, entre ruido.
- **Cierre:** La balanza recupera el equilibrio. Un hilo de agua vuelve a correr entre los bancales. «No fue culpa tuya», dice la radio. La voz ya no se parece a la de Maia.
- **Recuerdo 11 · Nada que pesar:** Las mesas guardan las marcas de lo que pesaban: redes, sal, pan. En la última, una nota para nadie: «Volvimos cuando bajó el agua. No quedaba nada que pesar». Debajo de la balanza, un trocito de cobre para que volviera a marcar lo justo.
- **Voz de Maia:** Se rompe

### 12 · Varadero de las Campanas

- **Entrada:** Las campanas del varadero avisaban a quienes trabajaban de espaldas al agua. Una cuelga junto al puente de carga. La otra, frente al horno. Al fondo, el banco de fundición sigue templado. Nilo ha estado aquí antes.
- **Cierre:** Las dos campanas vuelven a distinguirse desde el agua. Sobre el banco de fundición queda espacio para la próxima pieza. Más allá del varadero, en la subestación, algo enorme se pone en pie.
- **Recuerdo 12 · El disco fundido:** En el banco de fundición queda un molde con la forma de un disco de memoria. El metal se enfrió a medias. Nilo vino aquí después de la crecida, a olvidar. En el borde del molde, raspado con una pinza: «La esclusa nunca se abrió».
- **Voz de Maia:** Se rompe

### 13 · La Sala del Relevo (jefe final)

- **Entrada:** En la subestación todo sigue como aquella noche. Una figura enorme de tinta y cobre se levanta en el centro de la sala. Tiene la forma de Nilo y viene a por él.
- **Mecánica:** el Eco persigue, embiste y lanza pulsos que repliegan los puentes. Solo las tres torres cargadas con la batería le hacen daño. La barra de **señal de Maia** baja con cada golpe: si llega a cero, Maia se apaga y el nivel se reinicia.
- **Ambiente:** el mapa empieza de noche. Al vencer al Eco amanece, se encienden los faroles desde el núcleo hacia fuera y vuelve la música de Liria.
- **Voz de Maia:** se rompe según su señal; con menos del 30 % solo quedan fragmentos.
- **Cierre:** depende del final (ver abajo). No hay recuerdo.

## Recuerdos

Uno por puerto, del 1 al 12, opcionales. Cuentan la verdad en orden, salvo el **6**, que es el recuerdo falso que Nilo se fabricó al fundir el disco (el 12 lo desmiente: «La esclusa nunca se abrió»). Reunir los 12 es requisito del mejor final.

## La voz de Maia

`Historia.InterferenciaMaia(texto, puerto)` rompe cualquier diálogo cuyo título contenga «Maia». Es determinista: el mismo texto se rompe siempre igual.

| Puertos | Nivel | Efecto |
|---|---|---|
| 1-4 | 0 | Limpia |
| 5-6 | 1 | Pausas «…» ocasionales |
| 7-9 | 2 | «—kssh—» y palabras repetidas |
| 10-12 | 3 | Letras perdidas y el estribillo «buenas noches a quienes…» colándose |
| 13 | 4 | Solo fragmentos (en la línea del jefe, según la señal) |

Si en la decisión 1 se le dio la energía a la antena, la voz va **un nivel más limpia** desde el puerto 7. La luz verde de la radio (piloto de `MaiaTerminal`) baja con el mismo nivel y, desde el 7, parpadea.

## Las dos decisiones

Se guardan en `SaveManager.Decidir(punto, valor)`. Hoy se toman en un diálogo provisional al pulsar «Siguiente nivel»; las cinemáticas de decisión lo sustituirán (ver [plan_cinematicas.md](plan_cinematicas.md)).

| Punto | Cuándo | Opciones | Valor guardado | Efecto en juego |
|---|---|---|---|---|
| 1 · La antena | Tras el puerto 6 | Dar la energía al barrio / a la antena de Maia | `decisionEnergia` = `ciudad` / `maia` | Antena: voz más limpia y señal de Maia 270 en el jefe. Barrio: señal 225 |
| 2 · El banco de fundición | Tras el puerto 12 | Recordarlo todo / Fundir el disco otra vez | `decisionMemoria` = `recordar` / `borrar` | Decide el final |

## Los cuatro finales

`Historia.FinalActual` decide al vencer al Eco. Sin decisión tomada cuenta como «barrio» y «recordar».

| Final | Condición | Qué pasa |
|---|---|---|
| **El relevo** (el mejor) | Barrio + Recordar + 12 recuerdos | Nilo deja la batería en el núcleo para siempre. Liria se enciende entera. Maia emite por última vez «Apagarse también es parte de la canción». Nilo se apaga con la ciudad iluminada. |
| **Encender Liria** | Recordar, en cualquier otro caso | Vuelve la luz y la radio se apaga. Nilo se queda de turno, reparando lo que rompió. |
| **Fue ella** | Borrar + Barrio | Liria se enciende y Nilo corta la emisora, culpando a la radio. No vuelve a preguntarse por qué. |
| **Estática** | Borrar + Antena | Nilo vuelve a fundir su memoria. Despierta en el muelle; la radio da las buenas noches. En la baranda hay trece rayas: no es la primera vez. |

El texto de cada final aparece en el panel de cierre del puerto 13 y su nombre en los créditos.
