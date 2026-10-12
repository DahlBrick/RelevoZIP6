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

---

## 2026-10-12 · Entrada 6 — Segunda tanda de assets (HUD y piezas)

Con autorización del equipo se descargaron (todo CC0, salvo las fuentes, que son OFL y también
libres para uso comercial y académico):

| Pack | Autor | Uso |
|---|---|---|
| Input Prompts 1.5 | Kenney | Teclas del teclado (E, Esc, Tab, R, WASD, Enter, flechas) |
| UI Pack 2.0 | Kenney | Panel redondeado con relieve para todos los paneles del HUD |
| Fantasy Town Kit 2.0 | Kenney | Pilares, faroles, estandartes y jardinera |
| Space Kit | Kenney | Máquinas: batería, caja de la palanca, cuerpo y pantalla de Maia |
| Lilita One | Google Fonts (OFL) | Fuente de títulos |
| Varela Round | Google Fonts (OFL) | Fuente de textos |

Solo se copiaron al proyecto las piezas que se usan: modelos en `Assets/ThirdParty/Kenney_FantasyTown`
y `Kenney_Space`, y sprites y fuentes en `Assets/Resources/UI/` (para que el HUD los cargue al
construirse). Cada carpeta conserva su licencia.

---

## 2026-10-12 · Entrada 7 — Piezas del puzle del Nivel 1

`ActorVestido.cs` (nuevo) viste cada pieza **encima** de la que ya construye `ActorFactory`.
Lo que los scripts del juego necesitan se conserva intacto: las anclas, los pivotes y los
renderers que cambian de color para mostrar el estado (anillo de la base, indicador de la
palanca, umbral de la salida, orbe del recuerdo). Las primitivas que solo eran forma se ocultan.

| Pieza | Antes | Ahora |
|---|---|---|
| Batería | Caja ámbar | Celda de máquina (Space Kit) ámbar con franja de carga luminosa |
| Base de energía | Prisma marrón | Prisma de piedra tallada con pernos de cobre (conserva el conector hexagonal del GDD) |
| Palanca | Cajas de cobre | Caja de mecanismo de cobre (Space Kit); la barra y su indicador no cambian |
| Salida | Postes de cobre | Pórtico de pilares de piedra, dintel de madera, estandartes verdes y dos faroles |
| Recuerdo | Pedestal de cobre | Orbe sobre un pilar corto de piedra |
| Maia | Torre verde de cajas | Terminal verde con pantalla y ojos sobre una jardinera de piedra con helechos que respiran |
| Faroles del muelle | Poste y cubo ámbar | Farol de Fantasy Town (con su luz cálida que titila) |

![Piezas del puzle](img/09_piezas_del_puzle.jpg)

*De izquierda a derecha y de arriba abajo: Maia, batería, base de energía, palanca, salida y recuerdo.*

**Problema resuelto:** en los modelos del Space Kit el cuerpo usa el material `metalRed` y las
franjas usan `metal`, al revés de lo que parecía. El primer intento puso el ámbar emisivo en
todo el cuerpo de la batería y el brillo la saturó a blanco. Se corrigió el mapeo y se bajó la
emisión del ámbar.

**Archivos:** `Assets/Scripts/Level/ActorVestido.cs` (nuevo), `ActorFactory.cs` (llama al
vestido de cada pieza), `DioramaBuilder.cs` (faroles), `KitRelevo.cs` (piezas y materiales
nuevos, `ColocarCentrado`, `Tenir`), `PrepararKitVisual.cs`.

---

## 2026-10-12 · Entrada 8 — HUD nuevo y tecla animada

El HUD se construye por código desde `RelevoUIEstilo.cs`, así que el cambio de estilo se hizo
ahí y llega a todas las pantallas a la vez (objetivo, estado, aviso, diálogo, pausa y cierre):

- **Tipografía:** títulos en **Lilita One** y textos en **Varela Round** (antes, la fuente por
  defecto de Unity). Las dos tienen tildes y eñes.
- **Paneles:** panel redondeado con relieve de Kenney, teñido con el azul verdoso oscuro de
  RELEVO, con un contorno redondeado de cobre.
- **Teclas:** las leyendas de controles usan las teclas dibujadas de Kenney (WASD aparece como
  cuatro teclas). **Cada tecla de la leyenda se hunde cuando el jugador pulsa esa tecla.**
- **Menús:** la opción seleccionada crece con un pequeño rebote de muelle (`ReboteUI.cs`).

**Tecla E animada (`TeclaAnimada.cs`).** En el aviso de interacción ("[E] Recoger batería",
"[E] Hablar con Maia"...) la tecla E está dibujada en dos capas: la tapa y, debajo, la misma
tecla oscurecida, que hace de canto. Así se ve como una tecla física:
- En reposo, cada 1,5 s hace una **pulsación suave**, para invitar a presionarla.
- Cuando el jugador **presiona E de verdad**, la tecla se hunde a fondo, se achata, se oscurece,
  vuelve con un rebote y suelta una **onda** hexagonal.
- La tecla toma el color de la acción: ámbar para la batería y la energía, turquesa para los
  mecanismos, coral para los avisos.

![Tecla E presionándose, fotograma a fotograma](img/13_tecla_e_animada.jpg)

*Ocho fotogramas cada 0,06 s: reposo, hundimiento, fondo y vuelta con rebote.*

![HUD nuevo en juego](img/10_hud_nuevo_en_juego.jpg)

![Diálogo de Maia](img/11_hud_dialogo_maia.jpg)

![Menú de pausa](img/12_hud_menu_pausa.jpg)

**Problema resuelto:** el primer sprite de "contorno" elegido para los marcos no era hueco y
tapaba los paneles de color cobre, escondiendo las etiquetas. Se reemplazó por un contorno
redondeado generado por código, del mismo radio que los paneles.

![Primer intento: marcos opacos](img/14_primer_intento_hud_marcos_opacos.jpg)

**Herramientas de la bitácora** (solo editor, no entran en el juego): `SecuenciaCapturas.cs`
pulsa una tecla simulada y saca capturas cada pocos centésimos de segundo, con la interfaz
incluida; `CapturaBitacora.HojaDeContactos` compone varias imágenes en una hoja.

**Archivos:** `Assets/Scripts/UI/RelevoUIEstilo.cs`, `UIManager.cs`, `TeclaAnimada.cs` (nuevo),
`ReboteUI.cs` (nuevo), `Assets/Scripts/Herramientas/SecuenciaCapturas.cs` (nuevo),
`Assets/Resources/UI/**` (sprites y fuentes).

### Verificación

Se volvió a jugar la solución completa del Nivel 1 (recoger, insertar, palanca, recuperar,
salir): todo funciona, sin errores en la consola, y la escena sigue con sus 74 colisionadores.

### Pendiente

- Los muelles vecinos del fondo siguen siendo cajas lisas.
- Llevar el arte, las piezas y el HUD al Nivel 2, el Nivel 3 y el Cuaderno (el HUD nuevo ya
  aplica a todos los niveles porque se construye por código). El Cuaderno ya toma las fuentes y los paneles nuevos; su leyenda aún usa las teclas en cápsula.

