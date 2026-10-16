# Red narrativa de RELEVO · versión 0.1

Cómo convertir la historia de la [biblia de Liria](biblia_de_liria.md) en una red de escenarios y
finales, al estilo de *Detroit: Become Human*. Todo lo de este documento es **propuesta**.

---

## 1. Qué hace Detroit, y qué se puede copiar

- **Diagrama al final de cada capítulo.** Muestra las ramas que tomaste y deja ver, bloqueadas, las
  que no. No dice qué contienen; solo que existen. [1][2]
- **Muchos finales.** La prensa reporta desde 40 hasta 85 puntos donde la historia puede terminar,
  y su guionista habló de más de mil combinaciones. [1][3]
- **Contenido que pocos ven.** Cage aceptó crear escenas que quizá solo vería un 10 % de los
  jugadores. [2]
- **Los protagonistas pueden morir** antes del final, y la historia sigue sin ellos. [2]
- **El costo:** el guion tuvo entre 2.000 y 3.000 páginas, según la fuente. [1][4]

**Lo que RELEVO puede hacer con un equipo pequeño:** una red de **rama y cuello de botella**.
Todas las rutas pasan por los mismos niveles principales, que ya existen o están en maqueta. Lo
que cambia entre rutas es el *estado* del nivel (luz, siluetas, diálogos, qué recuerdo aparece) y
unos pocos escenarios exclusivos. Así hay muchos finales sin tener que construir el triple de
niveles.

---

## 2. El diagrama de la red *es* la red eléctrica de Liria

En vez de un diagrama abstracto como el de Detroit, al terminar cada nivel se muestra **el mapa de
la red eléctrica de Liria**. Cada escenario es un nodo, cada decisión es un cable, y las ramas que
no tomaste son faroles apagados.

Así el diagrama cuenta la historia: Nilo *era* esa red. Ver cómo se enciende o se apaga es ver su
memoria y su culpa.

---

## 3. Las variables que mueven la red

| Variable | Rango | Sube cuando | Baja cuando |
|---|---|---|---|
| **Culpa** | -3 (la pasa) a +3 (la asume) | Nilo admite, devuelve, se queda | Nilo culpa a otro, huye, borra |
| **Señal de Maia** | 0 a 100 | Se le da energía a la antena | Se recuperan recuerdos o se le da energía a la ciudad |
| **Luz de Liria** | 0 a 6 distritos | Se deja la batería en un distrito | Se desvía la energía a Maia |
| **Confianza de las sombras** | 0 a 3 | Nilo ayuda sin que nadie lo vea | Nilo las asusta o las ignora |
| **Recuerdos** | Lista | Se encuentra uno (algunos son opcionales) | — |
| **Olvidos** | 0 a 2 | Nilo elige borrar un recuerdo | — |
| **Desgaste del corazón** | 100 a 0 | — | Cada vez que Nilo deja la batería en una base, se apaga sin reserva o se le pide más de lo que tiene. Nunca se recupera |
| **Recuerdo falso** | Guardado / destruido | — | — |

**Regla de oro:** las decisiones se toman jugando, no en un menú. Dónde dejas la batería, qué
recuerdo recoges, a quién iluminas primero, si te quedas o huyes.

**Cambio respecto al cuestionario:** antes acordamos que el jugador no podía dejar de recuperar
recuerdos. Con la red, algunos recuerdos pasan a ser opcionales y otros siguen siendo obligatorios.

---

## 4. Las decisiones que el jugador toma jugando

1. **¿A quién le das la energía?** En cada nivel hay una base que alimenta un distrito (las
   sombras) y otra que alimenta la antena de Maia. Solo alcanza para una.
2. **¿Recoges el recuerdo?** Los opcionales están detrás de un camino más difícil o a contramano
   de lo que pide Maia.
3. **¿Borras el recuerdo?** Algunos, al recogerlos, dan la opción de "olvidar". Olvidar sube
   *Olvidos*, mantiene la señal de Maia y baja *Culpa*.
4. **¿Le haces caso a Maia?** A veces la Maia guía pide un camino que lleva lejos de los recuerdos.
5. **¿Te acercas a las sombras?** Si Nilo se acerca, huyen. Si las ayuda sin acercarse (enciende
   su farol y se va), la confianza sube.
6. **¿Te quedas sin reserva?** En ciertos puntos, apagarse a propósito es una decisión: Nilo puede
   quedarse sin energía para que la batería quede alimentando a otros.

---

## 5. Los escenarios

### Columna principal (todas las rutas)

| # | Escenario | Estado del nivel según la ruta | Recuerdo central |
|---|---|---|---|
| 0 | **Despertar** (nuevo) | Siempre igual | La canción, a medias |
| 1 | **Muelle de Cobre** | Las sombras huyen o miran desde lejos | Nilo conoce a Maia |
| 2 | **Patio de Bombas** | Las bombas giran o quedan paradas | El aprendiz de bombas |
| 3 | **Mareas de Liria** | El agua baja o sigue subiendo | Las mareas que Nilo dejó subir |
| 4 | **Los Condensadores** | Se cargan para la ciudad o para Maia | Nilo desvía la energía |
| 5 | **Ciénaga de los Faroles Ahogados** | Faroles encendidos o ahogados | Las muertes |
| 6 | **Forja de Ascuas** | Siempre; cambia lo que Nilo funde | Nilo intenta olvidar |
| 7 | **El Faro** | Depende de todas las variables | El recuerdo verdadero |

La **Forja** es donde Nilo se borró la memoria: fundió su propio disco para olvidar. Ahí el jugador
descubre que no fue un accidente.

### Escenarios exclusivos (solo algunas rutas)

| Escenario | Se abre si | Qué pasa |
|---|---|---|
| **La Antena de Maia** | Señal de Maia ≥ 70 tras el nivel 4 | El lugar donde vivía la Maia real. Nilo encuentra lo que queda de ella. |
| **El Barrio de las Sombras** | Confianza ≥ 2 tras el nivel 5 | Las sombras dejan entrar a Nilo. Por primera vez alguien le cuenta lo que hizo. |
| **El Taller del Olvido** | Olvidos ≥ 1 | Nilo encuentra las veces anteriores en que se borró. Confirma que el ciclo ya pasó. |
| **Sin guía** | Señal de Maia llega a 0 antes del Faro | Maia desaparece antes de tiempo, como un personaje que muere en Detroit. Los niveles que quedan se juegan sin sus pistas, en silencio. |

---

## 6. Los finales

Están agrupados por lo que Nilo hace con la culpa.

### Pasa la culpa
| Final | Condición | Qué pasa |
|---|---|---|
| **1. Estática** | Olvidos ≥ 1 y se elige a Maia en el Faro | Nilo desvía todo a Maia y se vuelve a borrar. Vuelve a empezar el Despertar, con un detalle cambiado que demuestra que no es la primera vez. |
| **2. El héroe de Liria** | Recuerdo falso guardado, Luz ≥ 4, Culpa ≤ -1 | Nilo enciende la ciudad convencido de que es su salvador. Las sombras nunca se acercan. Los créditos muestran una estatua suya rodeada de casas vacías. |
| **3. Fue ella** | Culpa ≤ -2, se visitó la Antena | Nilo decide que Maia lo obligó. Corta su señal con rabia. |
| **4. Fueron ellos** | Culpa ≤ -2, Luz ≤ 1 | Nilo decide que la ciudad iba a apagar a Maia y deja Liria a oscuras para siempre. |

### Culpa sin dueño
| Final | Condición | Qué pasa |
|---|---|---|
| **5. Encender Liria** | Culpa entre -1 y +1 | La luz vuelve, la señal de Maia se apaga y Nilo queda solo. Repara pero no confiesa. |
| **6. Apagón** | Nilo se queda sin reserva en la Forja o el Faro | Nilo se apaga sin terminar de recordar. Final temprano y **accidental**, como una muerte en Detroit: un mal cálculo de la reserva en el peor lugar. Distinto del 10, que llega por **desgaste**. |
| **10. La última batería** | Desgaste del corazón llega a 0, en cualquier momento | La batería de Nilo, la última de Liria, se agota. No por un error, sino por haberla dado tantas veces. Nilo se apaga igual que Maia: poco a poco. El mundo queda exactamente como estaba en ese momento, y los epílogos lo cuentan así. |

### Asume la culpa
| Final | Condición | Qué pasa |
|---|---|---|
| **7. Confesión** | Culpa ≥ 2, se visitó el Barrio | Nilo enciende la ciudad y se queda con las sombras a reconstruir. Ellas no lo perdonan, pero lo dejan quedarse. |
| **8. El relevo** | Todos los recuerdos y Culpa ≥ 2 | Nilo deja su corazón en el faro y se apaga. El faro transmite la canción de Maia a toda Liria. |
| **9. La última canción** (secreto) | El relevo + Recuerdo falso destruido + se visitó la Antena | Antes de apagarse, la Maia real (un resto de su señal) le dice a Nilo la frase que él nunca entendió. |

Diez finales con siete niveles principales, cuatro escenarios exclusivos y un nivel nuevo
(el Despertar).

---

## 7. Orden de trabajo sugerido

1. **Cerrar las piezas abiertas de la biblia** que necesita la red: la frase de Maia, la canción y
   por qué se estaba apagando.
2. **Sistema de estado narrativo:** un componente que guarde las variables junto al progreso que
   ya maneja `SaveManager.cs`, y condiciones simples en diálogos y niveles ("si Señal ≥ 70…").
3. **El mapa de la red eléctrica** entre niveles, aunque al principio tenga pocos nodos.
4. **Un nivel con dos estados** (por ejemplo, el Patio de Bombas con las bombas girando o paradas)
   como prueba de que la red funciona antes de escribir todo.
5. Después, los escenarios exclusivos y los finales, del más común al más raro.

---

## Fuentes

1. *Detroit: Become Human endings*, WePC. https://www.wepc.com/gaming/detroit-become-human-endings/
2. *Detroit: Become Human*, GameGeeker. https://gamegeeker.com/en/games/detroit-become-human-1222140
3. *All the feels*, NZ Herald (2018-05-31), vía PressReader.
   https://www.pressreader.com/new-zealand/the-new-zealand-herald/20180531/282673278001386
4. *Detroit: Become Human is a game where your choices matter*, AOL.
   https://www.aol.com/news/detroit-become-human-explores-true-dangers-artificial-intelligence-145457583.html

Las cifras de Detroit vienen de prensa secundaria y no coinciden entre sí; son aproximadas.
