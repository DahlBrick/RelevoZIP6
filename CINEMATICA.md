# Prólogo «Liria se apaga» — handoff
1. **Objetivo:** Liria viva de noche → apagón en cadena (de lejos a cerca) → el agua sube → título 3D «RELEVO» sale del mar. Tono: melancolía. 38 s, bandas 2.39:1, estilo cómic actual.
2. **Planos** (Timeline, pista «Camaras»; cada uno = CinemachineCamera + raíl Spline Dolly):
| # | Seg | Qué pasa | Cámara | Estado |
|---|---|---|---|---|
| 01 | 0–5 | General de Liria; Maia: «Buenas noches a quienes siguen de turno» | dron lento, fov 40 | hecho |
| 02 | 5–8 | Rueda de la bomba con chispas | raíl corto, fov 45 | pendiente: oscura, la tapan pilotes |
| 03 | 8–11 | Ropa tendida y humo de chimeneas | travelling calle, fov 50 | pendiente: ropa casi invisible |
| 04 | 11–14 | Antena con luz roja y cables | contrapicado, fov 42 | pendiente: cámara dentro de un muro |
| 05 | 14–17 | General desde el faro | fov 42 | hecho |
| 06 | 17–20 | Se apagan los barrios lejanos | fov 44 | hecho |
| 07 | 20–22 | Farolas de la calle mueren en fila | fov 50 | revisar (calle muy oscura) |
| 08 | 22–23 | La rueda se para | fijo, fov 38 | pendiente (mismo problema que 02) |
| 09 | 23–25 | Ropa a oscuras | fov 46 | pendiente (igual que 03) |
| 10 | 25–27 | Últimas luces; radio a estática | fov 44 | hecho |
| 11 | 27–30.2 | El agua sube sobre el malecón | bajo, fov 40 | pendiente: el muelle tapa; se ve el título antes de tiempo |
| 12 | 30.2–38 | Muy amplio; «RELEVO» sube del mar; fundido | fov 42 | hecho (quizá título más grande) |
3. **Decisiones técnicas:** Timeline 1.8.10 + Cinemachine 3.1.8 (Splines 2.0.0), corte seco (Brain DefaultBlend = Cut). `PlanoPrologo` mueve cámara y mira según su clip, así que basta mover el clip para cambiar el tiempo. `Prologo` lee `director.time` y lleva luces, rueda, ropa, humo, agua, luna, título, UI y sonido. Música por «radio» (filtros + pitch 1→0.62, corte a estática en `ultimaLuz-0.6`). Se lanza desde el título (`TituloRelevo.EscenaApertura = "Cinematica_Liria"`). Al acabar o con Esc mantenido 1.1 s carga `Nivel1_MuelleDeCobre`.
4. **Rutas:**
- Escena: `Assets/Scenes/Juego/Cinematica_Liria.unity`. Copia de la antigua: `Cinematica_Liria_Antigua.unity`.
- Timeline: `Assets/Cinematicas/Prologo.playable`. Materiales: `Assets/Cinematicas/Materiales/`.
- Scripts: `Assets/Scripts/Cinematica/{Prologo,PlanoPrologo,LiriaNocturnaNueva,LiriaNocturna,EstiloComic,SonidosApertura,GrabadorCamara}.cs`.
- Constructor: `Assets/Editor/ConstruirPrologo.cs` (menú Relevo/Construir prólogo). **Rehace la escena desde cero y borra los retoques a mano.**
- Música: `Assets/Audio/Musica/musica_prologo.mp3` (The Last Port at Twilight). Letras del título: `Assets/Resources/Cinematica/Titulo/letra_*.png`, `silueta_*.png`.
- Modelos: `Assets/Meshes/Liria/*.fbx`, `Assets/Prefabs/Liria/`, origen en Blender `Arte/Blender/scripts/construir_liria.py`. Bloques vía `KitRelevo.Activo.bloquesLiria`.
5. **Problemas abiertos:**
- Planos 02, 03, 04, 08, 09 y 11: tienen los raíles mal colocados (ver tabla).
- Las letras del título se ven bajo el agua antes de `tituloSube`: hay que ocultar sus renderers hasta entonces.
- La calle está oscura y tiene pocas luces cálidas.
- Sin verificar: tiempos de corte de luces, subtítulo de Maia, sonido y salto con Esc.
- El `.meta` del mp3 se escribió a mano con el editor cerrado; comprobar que Unity lo importa.
- Nada está en git. También hay cambios previos en `RELEVO_MaiaCarcasa.mat` y `ProjectSettings.asset`.
6. **Convenciones:** Unity 6000.3.8f1, URP. Nombres en español: «Plano NN Nombre» > Rail / Mira inicio / Mira fin / Mira / Camara. Coordenadas: mar hacia -z, calle de piedra z≈35, canal x 24–32, rueda (24.7,-0.9,6), antena (6,-1.5,60), agua y=-1.5.
7. **Próximos pasos:**
   1. Recolocar los raíles de los planos 04, 02/08, 03/09 y 11.
   2. Ocultar el título hasta `tituloSube` y agrandarlo en el plano 12.
   3. Añadir faroles o luces cálidas en la calle (z≈35).
   4. Ver el prólogo completo con UI y sonido, y ajustar la mezcla de la música.
   5. Commit en una rama y PR.
