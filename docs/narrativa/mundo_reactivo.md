# Liria, un mundo que recuerda · versión 0.1

Cómo hacer que **toda** Liria dependa de lo que hace el jugador, no solo los finales. Amplía la
[red narrativa](red_narrativa.md) y parte de la [biblia de Liria](biblia_de_liria.md). Todo es
**propuesta**.

---

## 1. Cinco reglas del mundo

1. **Todo deja rastro.** Encender un farol, empujar una caja o recoger un objeto cambia algo que
   el jugador verá después, en otro lugar.
2. **Las consecuencias llegan tarde y lejos.** El efecto rara vez se ve en el mismo nivel. Cerrar
   una compuerta en el Patio inunda el Mercado dos escenarios después. Así el jugador siente que el
   mundo es uno solo.
3. **Nada se marca como bueno o malo.** No hay barras de moral en pantalla. El jugador lo nota en
   el mundo: dónde se juntan las sombras, qué se inunda, qué dice Maia.
4. **No se puede ver todo en una partida.** La noche tiene horas contadas y la energía no alcanza
   para todo. Cada partida es una ruta.
5. **Cada lugar tiene estados, no una sola versión.** Un mismo lugar puede estar a oscuras,
   encendido, inundado o vacío según lo que pasó antes. Así hay muchos escenarios sin construir
   muchos niveles.

---

## 2. El mapa: Liria como red

Ya no hay capítulos en fila. Liria es un mapa de **15 lugares** unidos por cuatro tipos de camino.
El jugador elige el orden.

### Los caminos
| Camino | Se puede usar cuando |
|---|---|
| **Canal** | La marea está baja en ese tramo |
| **Cable** | Ese tramo de la red tiene energía. Nilo viaja *por dentro* del cable |
| **Azotea** | El agua subió tanto que solo quedan los tejados |
| **Muelle** | Siempre, pero es largo y gasta horas |

Lo que hace el jugador cambia qué caminos existen. Si inunda un distrito, se cierran sus canales
pero se abren sus azoteas.

### Los lugares
| Lugar | Qué es | Estados posibles |
|---|---|---|
| **La Subestación** (centro) | Donde Nilo despierta. Vuelve aquí entre zonas | Fría, Encendida, Desmantelada |
| **Muelle de Cobre** | Puerto, primer contacto con las sombras | Oscuro, Encendido, Ahogado |
| **Patio de Bombas** | Controla el agua de tres distritos | Parado, Bombeando, Inundado |
| **Mareas de Liria** | Canal principal que conecta el este y el oeste | Marea baja, Marea alta, Seco |
| **Los Condensadores** | Guardan energía para después | Vacíos, Cargados para la ciudad, Cargados para Maia |
| **Ciénaga de los Faroles Ahogados** | Donde murió más gente | Ahogada, Faroles encendidos, Velada (sombras de luto) |
| **Forja de Ascuas** | Donde Nilo fundió su memoria | Fría, Ardiendo, Derrumbada |
| **El Faro** (final) | Mira toda la ciudad | Apagado, Encendido, Transmitiendo |
| **La Antena de Maia** | Donde vivía la Maia real | Muda, Con estática, Transmitiendo |
| **El Barrio de las Sombras** | Donde se refugian los habitantes | Cerrado, Entreabierto, Abierto, Vacío |
| **El Taller del Olvido** | Las veces anteriores que Nilo se borró | Oculto, Encontrado |
| **El Mercado Flotante** (nuevo) | Mercado sobre barcas atadas | A flote, Hundido, Saqueado |
| **La Escuela de Radio** (nuevo) | Donde Maia aprendió a hablar y a cantar | Cerrada, Con eco de Maia, Borrada |
| **El Cementerio de Radios** (nuevo) | Radios viejas apiladas; voces de otras emisoras | Silencioso, Murmurando |
| **Dentro de la red** (nuevo) | Espacio abstracto dentro de los cables, hecho de recuerdos | Solo accesible por cable |

**Cuentas:** 15 lugares con 2 a 4 estados cada uno dan **más de 45 escenarios distintos**, sin
contar las variaciones de sombras, diálogos y recuerdos dentro de cada uno.

---

## 3. Los cuatro sistemas que mueven el mundo

Todo lo que hace el jugador termina en uno de estos cuatro sistemas, y cada sistema afecta a toda
la ciudad.

### 3.1 La energía es una sola y no alcanza
La batería (el corazón de Nilo) y lo que guardan los condensadores es **toda** la energía de
Liria. Lo que se le da a un distrito se le quita a otro, o a Maia. No hay forma de encender todo.

**Y el corazón se gasta.** La batería de Nilo es la última que queda en Liria, y no es eterna.
Cada vez que Nilo la deja en una base, se apaga sin reserva o la exige de más, pierde capacidad
para siempre. Al principio no se nota; con el tiempo la reserva dura menos, los puentes aguantan
menos y Nilo se mueve más lento. Si llega a cero, Nilo se apaga del todo (final 10, *La última
batería*).

Esto convierte cada uso de la batería en una decisión: dar energía ayuda a otros, pero acorta la
vida de Nilo. Y repite el destino de Maia: él intentó que ella no se apagara, y termina
apagándose de la misma forma.

### 3.2 El agua se mueve, no desaparece
Las bombas no "sacan" el agua: la mandan a otro lado. **Secar un distrito inunda otro.** El
jugador decide a quién le toca el agua, y eso cambia caminos, estados y dónde viven las sombras.

### 3.3 Las sombras se mueven por la ciudad
Las sombras van hacia la luz y huyen de Nilo. Se juntan donde hay faroles encendidos, se van de
donde hay agua, y recuerdan lo que Nilo hizo en su presencia. Donde estén, cambia el lugar: un
distrito con sombras tiene velas, voces y objetos; uno vacío tiene puertas abiertas y silencio.

### 3.4 La noche se acaba
La historia pasa en **una sola noche de 12 horas**. Cada lugar visitado gasta tiempo, y los
caminos largos gastan más. Algunas cosas solo pasan a ciertas horas: la marea más baja a las 3,
la última transmisión de Maia a las 4, el amanecer a las 6 de la mañana del reloj del juego. Cuando
amanece, Nilo tiene que estar en el Faro. Lo que no alcanzó a hacer queda así para siempre.

---

## 4. Interacciones sencillas, consecuencias grandes

La regla: **la acción es pequeña, la consecuencia es grande y llega después.**

| Acción sencilla | Efecto inmediato | Lo que pasa después |
|---|---|---|
| **Encender un farol** | Hay luz en la pasarela | Las sombras se juntan ahí. Ese farol gasta energía que no tendrá Maia. Si se apaga más tarde, las sombras quedan a oscuras y empiezan a temerle más a Nilo. |
| **Empujar una caja sobre una placa** | Se abre una compuerta | La compuerta deja pasar agua a otro distrito, que puede inundarse horas después. |
| **Dejar una caja tapando un desagüe** | Nada visible | El Mercado Flotante se hunde al llegar la marea alta. |
| **Accionar una palanca** | Se mueve un puente | Algunas palancas también cortan cables: un distrito lejano se apaga. |
| **Quedarse quieto cerca de una sombra** | La sombra no huye | Si se repite en varios lugares, las sombras empiezan a seguir a Nilo de lejos. Sube la confianza. |
| **Acercarse rápido a una sombra** | Huye | Las sombras de ese distrito se van a otro. El distrito queda vacío. |
| **Recoger un objeto** (una muñeca, una taza, una foto) | Va al cuaderno | Su dueño, una sombra con nombre, lo echa de menos. Devolverlo cambia su historia; quedárselo también. |
| **Sintonizar la radio** | Se escucha a Maia | Si se busca otra frecuencia, aparecen otras voces: habitantes que hablan de lo que pasó esa noche. |
| **Seguir a Maia** | Pistas más fáciles | Te aleja de los recuerdos que ella no quiere que veas. |
| **Desobedecer a Maia** | Camino más difícil | Encuentras lo que ella esconde. Su señal baja. |
| **Recoger un recuerdo** | Nilo recuerda | Maia se debilita. La gente que aparece en el recuerdo cambia cómo reacciona a Nilo. |
| **Borrar un recuerdo** | Maia se fortalece | Sube *Olvidos*. Las sombras que estaban en ese recuerdo dejan de reconocer a Nilo. |
| **Quedarse sin reserva** | Nilo se apaga | Despierta en la Subestación horas después. La noche avanzó, y algo cambió mientras no estaba. |
| **Dejar la batería en una base y alejarse** | Nilo pierde energía | Esa base queda alimentando a alguien. Si es el Barrio, las sombras pasan la noche con luz. El corazón de Nilo se desgasta un poco, para siempre. |
| **Tocar la canción** (en un fonógrafo, en la Antena) | Suena la canción de Maia | Las sombras que la conocían se acercan. Algunas lloran. Una lo reconoce. |

---

## 5. Las sombras con nombre

Las siluetas no son todas iguales. Algunas tienen nombre e historia, y pueden vivir, irse o
desaparecer según lo que haga Nilo, como los personajes secundarios de Detroit. **[propuesta,
nombres]** No tienen nombre en pantalla: Nilo los olvidó. Se llaman por lo que son, y recuperar el
nombre de una sombra es un recuerdo más.

| Sombra | Quién es | De qué depende su destino |
|---|---|---|
| **La farolera** | Encendía los faroles de la Ciénaga cada noche | Si Nilo enciende los faroles de la Ciénaga, ella vuelve a su trabajo. Si no, se queda velando a sus muertos. |
| **El niño del muelle** | Sigue esperando un barco que no llegó | Si Nilo le devuelve el barquito de juguete, lo deja de esperar. Si el Muelle se ahoga, se va al Barrio. |
| **El bombero** | Intentó reabrir las bombas esa noche | Recuerda a Nilo cerrándolas. Si Nilo lo ayuda en el Patio, le cuenta lo que vio. Si no, es el que más le teme. |
| **La comerciante** | Vive en el Mercado Flotante | Si el Mercado se hunde, lo pierde todo y culpa a Nilo en voz alta. |
| **El aprendiz de bombas** | **[abierto]** ¿Era Maia? ¿Otro? | — |
| **La que reconoce a Nilo** | Conoció a Nilo y a Maia juntos | Solo aparece si Nilo toca la canción delante de ella. Es la única que puede contarle la verdad completa. |

Cada sombra con nombre tiene un **epílogo propio** al final, como en los finales de Fallout:
"La farolera volvió a encender los faroles cada noche, aunque nunca le dirigió la palabra a
Nilo." Con 6 sombras y 2 o 3 destinos cada una, los epílogos multiplican los finales sin escribir
finales nuevos.

---

## 6. Rutas

Las rutas no se eligen en un menú: aparecen según cómo juega cada uno. Estas son las más claras.

| Ruta | Cómo se juega | Qué ve el jugador | Hacia qué finales empuja |
|---|---|---|---|
| **Ruta de la Luz** | Le da la energía a los distritos | Ciudad cada vez más encendida, sombras que salen, Maia que se apaga rápido | 5, 7, 8 |
| **Ruta de la Señal** | Le da la energía a Maia | Ciudad a oscuras, Maia fuerte y cariñosa, sombras que se esconden | 1, 3, 4 |
| **Ruta del Agua** | Mueve el agua para abrirse paso | Distritos ahogados, azoteas, muertes nuevas que son culpa del jugador | 2, 4, 6 |
| **Ruta del Silencio** | Evita a las sombras y borra recuerdos | Mundo vacío, Taller del Olvido, el ciclo | 1, 6 |
| **Ruta de las Sombras** | Se acerca, devuelve objetos, toca la canción | Barrio abierto, sombras con nombre, la verdad contada por otros | 7, 8, 9 |

Las rutas se mezclan: una partida puede empezar por la Luz y terminar en el Silencio.

---

## 7. Los recuerdos cambian según quién te los da

El mismo hecho se puede recordar de varias formas. **[propuesta]**

- Si lo recupera Nilo solo, en un nivel, lo ve como él quiere verlo.
- Si se lo cuenta una sombra, lo ve como lo vio ella.
- Si lo encuentra Dentro de la red, lo ve sin filtros.

Ejemplo: **La noche en que se pararon las bombas.** Contado por Nilo: "Las bombas fallaron, yo
intenté arreglarlas." Contado por el bombero: Nilo cerrando las válvulas una por una. Dentro de la
red: Nilo escuchando la canción de Maia mientras el agua sube.

---

## 8. Cómo hacerlo posible con un equipo pequeño

1. **Un registro de consecuencias.** Un componente que anota cada acción importante ("farol
   Ciénaga 2 encendido a las 1:40", "caja en desagüe del Patio"). Cada lugar, al cargar, lee el
   registro y elige su estado. Encaja con `SaveManager.cs`.
2. **Estados por datos.** Cada lugar se describe con sus estados en un archivo de datos
   (ScriptableObject): qué objetos aparecen, qué luz, qué agua, qué sombras. Los niveles ya se
   generan desde mapas de texto con `LevelLayouts.cs` y `DioramaBuilder.cs`, así que un estado
   puede ser una variante del mapa.
3. **Reglas pequeñas y globales.** Agua, energía, sombras y tiempo se calculan con pocas reglas
   que valen para toda la ciudad. No se escribe cada consecuencia a mano.
4. **Prototipo de tres lugares primero.** Subestación, Patio de Bombas y Mercado Flotante: basta
   para probar que cerrar una compuerta en uno inunda el otro, y que el jugador lo nota.
5. **Después crecer de a un lugar.** Cada lugar nuevo se suma a la red sin rehacer los anteriores.

**Advertencia honesta:** Detroit tuvo un guion de miles de páginas y un estudio grande detrás.
La forma de lograr un mundo así con pocas personas es que las consecuencias salgan de **sistemas**
(agua, energía, sombras, tiempo) y no de escenas escritas una por una. Las escenas escritas a mano
se guardan para los momentos que importan: recuerdos, sombras con nombre y finales.

---

## Preguntas para la siguiente ronda

1. ¿Te gusta que todo pase en **una sola noche** con horas contadas, o prefieres sin límite de
   tiempo?
2. ¿Nilo puede **viajar por dentro de los cables**? Cambia mucho cómo se mueve por el mapa.
3. ¿Los **lugares nuevos** (Mercado Flotante, Escuela de Radio, Cementerio de Radios, Dentro de
   la red) te sirven, o cambias alguno?
4. ¿Las **sombras con nombre** te convencen? ¿Cuántas quieres?
5. ¿El jugador puede **volver a jugar la misma noche** sabiendo lo que pasó (como un ciclo), o cada
   partida empieza de cero?
