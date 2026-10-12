# Bitácora — Mejora gráfica de RELEVO

Registro cronológico de los cambios de la rama `mejora-grafica`. Cada entrada indica
qué se hizo, por qué, qué archivos tocó y cómo se verificó, con capturas en `img/`.

Proyecto: Unity 6000.3.8f1 + URP 17.3. Repositorio: github.com/DahlBrick/RelevoZIP6.

---

## 2026-10-12 · Entrada 0 — Punto de partida

Estado del Nivel 1 (Muelle de Cobre) antes de cualquier cambio gráfico. Los niveles se
generan a partir de un mapa de texto (`LevelLayouts.cs`) y `DioramaBuilder.cs` los arma con
cajas primitivas de colores; Nilo, Maia, la batería, las bases y las palancas se construyen
con primitivas en `ActorFactory.cs`.

![Nivel 1 antes](img/00_nivel1_antes.jpg)

**Objetivo de la mejora:** reemplazar las primitivas por modelos 3D estilizados y animar el
escenario y los personajes para que el mundo se sienta vivo, **sin cambiar las reglas del
juego** (batería única, puentes temporales, pasarelas permanentes, agua que bloquea).

**Criterio de trabajo:**
- La lógica y las colisiones invisibles se conservan; solo cambia la capa visual.
- Primero el Nivel 1 completo; después se replica en los Niveles 2, 3 y el Cuaderno.
- Todo en la rama `mejora-grafica`; `main` queda intacta como respaldo.

---

## 2026-10-12 · Entrada 1 — Descarga e importación de assets gratuitos (CC0)

Se descargaron cuatro packs con licencia **CC0 1.0** (dominio público: uso comercial y
académico libre, sin obligación de atribución). Cada carpeta conserva su `License.txt`.

| Pack | Autor | Fuente | Uso previsto | Peso en el proyecto |
|---|---|---|---|---|
| LowPoly Robot (animado, 14 animaciones) | Quaternius | quaternius.itch.io/lowpoly-robot | Base del modelo de Nilo | 3.2 MB |
| Stylized Nature MegaKit (Standard, 68 modelos) | Quaternius | quaternius.itch.io/stylized-nature-megakit | Árboles, plantas, flores, rocas, caminos de piedra | 46 MB |
| Pirate Kit 2.1 | Kenney | kenney.nl/assets/pirate-kit | Muelles, tablones, barriles, cajas, torre (faro) | 3.2 MB |
| Watercraft Kit 2.1 | Kenney | kenney.nl/assets/watercraft-kit | Barcas, boyas, botes de pesca | 1.9 MB |

**Qué se importó:** solo los modelos `.fbx` y sus texturas, en `Assets/ThirdParty/<Pack>/`.
Los `.zip` originales y los demás formatos (OBJ, glTF, Blend) quedan fuera del proyecto, en
`Downloads/RelevoAssets_src/`, para no inflar el repositorio.

**Archivos:** `Assets/ThirdParty/**` (nuevo).

![Modelos importados con la luz por defecto](img/01_assets_importados.jpg)

*Vista de prueba de los modelos recién importados (árboles, rocas, tarimas, barriles, torre,
barcas), antes de aplicarles los materiales de RELEVO.*

---

## 2026-10-12 · Entrada 2 — Kit visual y shaders propios

Para que los modelos de dos autores distintos se lean como un solo mundo, y para que el arte se
pueda regenerar entero, se creó un **kit visual** y dos **shaders propios**.

**Kit visual (`Assets/Resources/KitRelevo.asset`).** Catálogo con los modelos y materiales que
usa el generador de niveles. Si el kit no existe, los niveles se construyen como antes, con
primitivas. Lo crea y rellena el menú **Relevo → Preparar kit visual**
(`Assets/Editor/PrepararKitVisual.cs`), que además:
- Crea los materiales de RELEVO en `Assets/Materials/Kit/` (agua, follaje, piedra, madera, Nilo).
- Reasigna los materiales de los modelos importados (remap del importador), así que cualquier
  árbol o el robot ya salen con el aspecto de RELEVO.
- Configura los bucles de las animaciones del robot y crea el Animator de Nilo
  (`Assets/Animation/Nilo.controller`).

**Shader de agua (`Assets/Shaders/RelevoAgua.shader`).** Las superficies de agua son cajas
planas, así que la vida del agua se dibuja en el fragmento:
- El color cambia según la profundidad real (lee el *depth texture* de URP): se aclara junto a
  muelles, rocas y pilotes.
- Una banda de espuma "respira" contra las orillas.
- Vetas finas de corriente que derivan y destellos de sol que parpadean.
- Recibe la sombra del sol, así que los muelles y los árboles se proyectan sobre el canal.
- Funciona con la cámara ortográfica del juego y con la perspectiva del editor.

**Shader de follaje (`Assets/Shaders/RelevoFollaje.shader`).** Para árboles, matas, flores y hierba:
- Viento en el vértice: cada planta se mece más cuanto más alta es, con un desfase por posición
  para que no se muevan todas a la vez.
- Luz "envolvente": la cara en sombra de la copa no se vuelve negra (aspecto de ilustración).
- Recorte por alfa, dos caras, sombras y oclusión ambiental (SSAO).

**Archivos:** `Assets/Scripts/Visual/KitRelevo.cs`, `Assets/Editor/PrepararKitVisual.cs`,
`Assets/Shaders/*.shader`, `Assets/Materials/Kit/*`, `Assets/Animation/Nilo.controller`,
`Assets/Resources/KitRelevo.asset`.

---

## 2026-10-12 · Entrada 3 — Escenario del Nivel 1 vestido con los modelos

`DioramaVestido.cs` (nuevo) se ejecuta después de `DioramaBuilder` y coloca los modelos
**encima** de lo que ya existía. Las cajas de terreno, los volúmenes invisibles que bloquean el
agua y el perímetro son exactamente los mismos.

| Casilla / zona | Antes | Ahora |
|---|---|---|
| Piedra (`#`) | Caja crema con juntas pintadas | Adoquines de piedra (Quaternius) teñidos al crema de la paleta |
| Madera (`m`) | Caja marrón con juntas | Tarima de tablones (Kenney) teñida a madera |
| Vegetación (`v`) | Esferas verdes y cilindro | Árbol, mata, flores y hierba con viento, y farol que titila |
| Orillas | Recta | Rocas, guijarros y juncos en el agua, con espuma alrededor |
| Esquinas de muelle | — | Algunos barriles y cajas (sin colisión, pegados al canto) |
| Fondo | Esferas ámbar y cajas | Barcas y boyas que se mecen (`Flotador.cs`), gaviotas planeando (`Gaviotas.cs`) |
| Aire | — | Polen flotando sobre el canal y luciérnagas junto a los faroles |

Toda la variación (qué árbol, qué giro, dónde va una roca) sale de un ruido determinista por
casilla, no de `Random`: hornear dos veces el mismo mapa da exactamente la misma escena.

**Luz de tarde.** El GDD sitúa la aventura "durante una tarde", así que con el kit activo el sol
es más bajo y dorado, el ambiente más claro y la niebla pasó de exponencial a **lineal**: la
cámara está a unos 45 m del muelle y la exponencial lavaba un tercio del color de todo el nivel.

**Archivos:** `Assets/Scripts/Level/DioramaVestido.cs` (nuevo), `DioramaBuilder.cs` (usa el kit
si existe), `ActorFactory.cs` (luz y ambiente), `Assets/Scripts/Visual/Flotador.cs`,
`LuzTitilante.cs`, `Gaviotas.cs` (nuevos).

### Problemas encontrados y cómo se resolvieron

![Primer intento: árboles rojos y agua con manchas](img/02_primer_intento_arboles_rojos.jpg)

1. **Árboles rojos.** Las texturas `_C` de Quaternius no son colores sino *máscaras* pensadas
   para su shader de pago (la de los árboles retorcidos es roja). Se usan las versiones blancas
   de las hojas y se tiñen desde el material con los verdes apagados de la paleta del GDD.
   Además se activaron *alpha is transparency* y *preservar cobertura* en las texturas con alfa,
   para que las copas no se vean ralas de lejos.
2. **Agua con manchas blancas.** La espuma salía de un umbral sobre una sola capa de ruido y
   dibujaba manchas. Ahora son líneas finas donde se cruzan dos capas, solo en algunas zonas, y
   los destellos son mucho más escasos.
3. **Tablones mal orientados.** La primera pieza de Kenney (`platform-planks`) era una pila de
   tablas, no un suelo. Se cambió por `platform` y su escala se calcula a partir del tamaño real
   del modelo.
4. **Imagen lavada.** Era la niebla exponencial (ver arriba).

![Texturas y agua corregidas](img/03_texturas_y_agua_corregidas.jpg)

---

## 2026-10-12 · Entrada 4 — Nilo animado

Nilo ahora es el **robot animado de Quaternius**, pintado con los colores del GDD (carcasa
clara, extremidades azul oscuro) y con sus señas propias: **bufanda naranja** colgada del hueso
del cuello y **antena con luz cian** sobre la cabeza.

- **Locomoción:** un árbol de mezcla por velocidad pasa de reposo a paso y a carrera sin cortes.
- **Gestos:** se disparan con las mismas acciones que ya pedían un sonido, así la animación
  reacciona exactamente a lo mismo que el audio, sin tocar la lógica:

| Acción del jugador | Gesto de Nilo |
|---|---|
| Recoger la batería | Asiente ("sí") |
| Insertarla en una base | Pulgar arriba |
| Accionar una palanca | Golpe a la palanca |
| Examinar un recuerdo | Saluda |
| Acción no válida (p. ej. salir sin batería) | Niega con la cabeza |
| Completar el nivel | Baila |

- Si Nilo echa a andar durante un gesto, el gesto se corta: el control nunca se siente pegajoso.
- La bufanda ondea y la antena se balancea con la marcha.
- **La batería va a la espalda**, colgada del hueso del torso, para que el balanceo de los brazos
  al caminar no la atraviese.

El `CharacterController` y todos los componentes de juego de Nilo son los mismos de antes.

**Archivos:** `Assets/Scripts/Actors/AnimadorNiloModelo.cs` (nuevo), `ActorFactory.cs`
(`CrearNiloModelo`; la batería se engancha buscando `ManoBateria` por nombre).

![Primer plano de Nilo en el muelle](img/07_primer_plano_nilo.jpg)

![Nilo con la batería a la espalda](img/08_primer_plano_bateria_a_la_espalda.jpg)

---

## 2026-10-12 · Entrada 5 — Verificación del Nivel 1

**Colisiones intactas.** La escena horneada tiene los mismos **74 colisionadores** que en `main`:
el arte nuevo no añade ni quita ningún obstáculo.

**Partida completa.** Se jugó la solución del Nivel 1 dentro del editor, paso a paso:

| Paso | Resultado |
|---|---|
| Recoger la batería | Batería en manos de Nilo ✔ |
| Insertarla en la base A | Batería en la base, puente A–B desplegado ✔ |
| Accionar la palanca de B | Pasarela permanente D–B abierta ✔ |
| Recuperar la batería en A | Puente A–B retraído, batería en manos ✔ |
| Salir por B con la batería | "Nivel restaurado", progreso guardado ✔ |

Consola sin errores del juego (solo avisos de licencia del paquete de IA de Unity, ajenos al
proyecto).

![Nivel 1 en juego](img/04_nivel1_en_juego.jpg)

![Puente eléctrico y pasarela manual activos](img/05_puente_y_pasarela_activos.jpg)

![Nivel completado](img/06_nivel_completado.jpg)

### Pendiente para la siguiente etapa

- **Maia, la base de energía, la palanca, la salida y el recuerdo** siguen hechos con
  primitivas. Junto al arte nuevo desentonan (se nota en el primer plano). Los packs no traen
  equivalentes de estas piezas, así que hay que diseñarlas: es el siguiente paso.
- Los muelles vecinos del fondo siguen siendo cajas lisas.
- Replicar todo en el Nivel 2, el Nivel 3 y el Cuaderno.

