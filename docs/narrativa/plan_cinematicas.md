# RELEVO · Plan de cinemáticas

Tres cinemáticas obligatorias, dos de decisión y la final, más dos opcionales. Todas siguen las
reglas de la [historia final](historia_final.md):

- Nilo no habla.
- Maia es una radio.
- No aparecen personas ni sus sombras.
- Se usa el mismo estilo cómic de los niveles.

Se construyen como la apertura (`Assets/Scripts/Cinematica/Apertura.cs`): una escena propia con un
guion por tiempo (`Evaluar(t)`), los subtítulos con `DialogoUI` y la tecla de saltar.

| # | Cinemática | Escena propuesta | Cuándo | Duración |
|---|---|---|---|---|
| 1 | La antena (decisión 1) | `Cinematica_Antena` | Al pulsar «Siguiente nivel» tras el puerto 6 | 60-75 s |
| 2 | El banco de fundición (decisión 2) | `Cinematica_Fundicion` | Al pulsar «Siguiente nivel» tras el puerto 12 | 75-90 s |
| 3 | El relevo (final, 4 variantes) | `Cinematica_Final` | Al vencer al Eco en el puerto 13, antes del panel de cierre | 60-90 s cada una |
| 4 | Revelación (opcional) | `Cinematica_Renglon` | Cierre del puerto 10 | 30 s |
| 5 | Apertura revisada (opcional) | `Cinematica_Liria` | Ya existe | — |

---

## 1 · La antena (decisión 1, tras el puerto 6)

**Qué cuenta:** Maia pide algo para sí por primera vez. Es el primer momento en que el jugador
puede elegir entre la radio y la ciudad.

**Planos:**

1. **(0-8 s)** Plano general de la esclusa recién abierta. El agua baja y la corriente vuelve por
   los cables de la orilla. Los cables son líneas ámbar que avanzan.
2. **(8-18 s)** La corriente llega a un cruce. Desde ahí sale un cable hacia la torre de la
   emisora, con la antena casi apagada, y otro hacia el barrio, con sus ventanas a oscuras.
3. **(18-30 s)** Primer plano de la radio de Maia. El piloto verde parpadea. Subtítulo: «Si me la
   das a mí, seguiré contigo un poco más».
4. **(30-40 s)** Nilo mira primero una dirección y luego la otra. La cámara se queda a su altura.
5. **(40 s) Elección.** Aparecen las dos opciones sobre la imagen congelada, en el estilo del
   diálogo:
   - «Dar la energía al barrio»
   - «Dar la energía a la antena de Maia»
6. **Resolución A, barrio (10 s):** las ventanas se encienden una a una. La antena se apaga con un
   chasquido, y Maia dice: «Está bien. Es lo correcto».
7. **Resolución B, antena (10 s):** la antena se ilumina en verde y el barrio sigue a oscuras.
   Maia dice: «Gracias, Nilo. No me olvides».

**Cómo se conecta:** sustituye a `UIManager.OfrecerDecision(1)`. Al elegir se llama a
`SaveManager.Decidir(1, "ciudad" | "maia")` y se carga `Nivel7_LonjaDeLasDosVoces`.

---

## 2 · El banco de fundición (decisión 2, tras el puerto 12)

> **Hecha (2026-11-24):** `Assets/Scripts/Cinematica/CinematicaFundicion.cs`, escena
> `Cinematica_Fundicion`. Dura unos 78 s. Mantener Esc salta hasta la elección, que no se puede
> saltar. Desde el editor se revisa con `Revisar(segundo, opcion)`.

**Qué cuenta:** Nilo encuentra el lugar donde se borró a sí mismo y puede volver a hacerlo. Es la
decisión moral del juego.

**Planos:**

1. **(0-10 s)** El varadero en silencio. Las dos campanas se balancean sin sonar, y al fondo
   brilla el banco de fundición.
2. **(10-25 s)** Nilo se acerca. Primer plano del molde con la forma de un disco de memoria y,
   raspado en el borde, «La esclusa nunca se abrió».
3. **(25-45 s)** Montaje en tinta, sin personas, con los recuerdos 7 a 10 como objetos que
   destellan uno tras otro:
   - la etiqueta «RADIO» sobre «BOMBA DE LA LONJA»;
   - la cinta de las 00:17;
   - la ficha partida;
   - el renglón «Operador de guardia: NILO».
4. **(45-55 s)** El agua que sube en el mercado y el reloj que marca las 00:17. Fundido a negro.
5. **(55-65 s)** La radio de Maia, muy rota: «…no fue… —kssh—… culpa tuya…».
6. **(65 s) Elección:**
   - «Recordarlo todo»
   - «Fundir el disco otra vez»
7. **Resolución, recordar (10 s):** Nilo aparta el molde. La pantalla de su cara muestra el código
   del operador y se queda encendida. A lo lejos, en la subestación, algo se pone en pie.
8. **Resolución, borrar (10 s):** Nilo mete su disco en el molde, que brilla en naranja. El
   recuerdo 6, el de la esclusa abierta, aparece nítido y cálido. La subestación ruge igual.

**Cómo se conecta:** sustituye a `UIManager.OfrecerDecision(2)`. Al elegir se llama a
`SaveManager.Decidir(2, "recordar" | "borrar")` y se carga `Nivel13_LaSalaDelRelevo`.

**Detalle opcional:** si el jugador no tiene los 12 recuerdos, el montaje del plano 3 muestra
huecos negros en lugar de los recuerdos que le faltan.

---

## 3 · El relevo (final, cuatro variantes)

**Tronco común (0-20 s):**

1. El Eco se deshace en la luz, como en el nivel.
2. Silencio.
3. Nilo queda solo en la sala del núcleo, con la batería en las manos.
4. Al fondo se ve la radio de Maia.

Después se ramifica según `Historia.FinalActual`:

### 3A · El relevo (barrio + recordar + 12 recuerdos)

1. Nilo encaja la batería en el núcleo y no la retira.
2. Plano aéreo: Liria se enciende barrio a barrio, del muelle al varadero, en el orden de los
   puertos.
3. Maia, limpia por primera vez desde el puerto 4: «Apagarse también es parte de la canción».
4. Las luces de Nilo bajan despacio y la ciudad se queda iluminada.
5. Último plano: el muelle del puerto 1 con la batería de repuesto junto a la baranda, esperando
   al siguiente turno.

### 3B · Encender Liria (recordar en cualquier otro caso)

1. La luz vuelve, pero solo a medias.
2. La radio emite la frase de despedida y se apaga.
3. Nilo coge una caja de herramientas y sale a reparar.
4. Último plano: Nilo pequeño, recorriendo los puentes encendidos.

### 3C · Fue ella (borrar + barrio)

1. Liria se enciende.
2. Nilo se acerca a la radio y arranca el cable de la antena.
3. Sin música, solo el zumbido de la ciudad.
4. Último plano: la radio apagada en el muelle, con el piloto en negro.

### 3D · Estática (borrar + antena)

1. Ruido blanco y fundido.
2. Se repite el primer plano de la apertura: el muelle, la radio y «Buenas noches a quienes siguen
   de turno».
3. La cámara baja hasta la baranda y se ven trece rayas.
4. Corte a negro.

**Cómo se conecta:**

- `JefeEcoApagon.Vencer()` llama hoy a `LevelManager.CompletarPorJefe()` a los 4,5 s.
- La cinemática se cargaría ahí, con la variante en `Historia.FinalActual`.
- Al terminar irá directamente a `Creditos`. El panel de cierre del puerto 13 queda como respaldo
  si la escena no está en Build Settings.

---

## 4 · Revelación (opcional, cierre del puerto 10)

Dura 30 segundos:

1. El libro de guardia al trasluz y el renglón «Operador de guardia: NILO».
2. Primer plano de la pantalla de Nilo, que por un instante muestra el mismo código.
3. La radio de Maia se apaga sola.

Refuerza la confirmación del puerto 10 sin quitarle peso al texto del cierre.

## 5 · Apertura revisada (opcional)

La apertura actual ya funciona. Solo haría falta un ajuste: que el plano del muelle muestre la
baranda sin rayas. Así el final Estática se entiende como un bucle.

---

## Orden de trabajo recomendado

1. **El banco de fundición.** Es la decisión que más pesa y reutiliza objetos que ya existen.
2. **El relevo.** El tronco común y la variante 3B, que es el final que verá la mayoría.
3. **La antena.**
4. Las variantes 3A, 3C y 3D.
5. Las opcionales.
