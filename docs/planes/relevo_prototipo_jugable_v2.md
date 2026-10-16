# RELEVO — Prototipo jugable académico (Unity 6000.3.8f1 + URP 17.3)

## Objetivo
Construir por fases un prototipo 3D estilizado 2.5D de RELEVO (3 niveles, cámara ortográfica fija, una sola batería) validando una vertical slice 100% jugable del Nivel 1 antes de prefabs, arte y Niveles 2–3.

## Cambios
- 'Assets/Input/Relevo.inputactions' - (crear) mapas Gameplay (Move, Interact, Notebook, Pause, Restart, Confirm) y UI; **Interact con interacción Press de pulsación única, sin Hold**.
- 'Assets/InputSystem_Actions.inputactions' - cambiar sólo la acción Player/Interact de interaction "Hold" a "Press" (mantener bindings E/Gamepad buttonNorth y el resto del asset sin tocar) para que E actúe de inmediato.
- 'Assets/Scripts/Core/' - (crear) GameEvents.cs, LevelManager.cs, AudioTonePlayer.cs.
- 'Assets/Scripts/Player/' - (crear) PlayerController.cs, PlayerInteractor.cs.
- 'Assets/Scripts/Interactions/' - (crear) IInteractable.cs, InteractableBase.cs, BatteryController.cs, EnergySocket.cs, TemporaryBridge.cs, PermanentWalkway.cs, LeverInteractable.cs, ExitInteractable.cs, MemoryInteractable.cs, MaiaTerminal.cs.
- 'Assets/Scripts/Level/' - (crear) LevelDefinition.cs (ScriptableObject), CameraRig.cs, LevelBounds.cs, BridgeLink.cs.
- 'Assets/Scripts/UI/' - (crear) UIManager.cs, HUDView.cs, PauseMenu.cs, NotebookView.cs, ContextPrompt.cs (UGUI + TMP construido por código).
- 'Assets/Scripts/Save/' - (crear) SaveManager.cs, SaveData.cs (JSON local).
- 'Assets/Materials/*.mat' - (crear) URP/Lit de paleta: piedra crema, madera oscura, cobre envejecido, agua azul verdosa, vegetación verde apagado, ámbar emisivo (energía), turquesa (pasarelas/palancas), coral (desgaste).
- 'Assets/Prefabs/' - (crear) Nilo, Maia, Bateria, BaseEnergia, PuenteTemporal, PasarelaPermanente, Palanca, Salida, Recuerdo, Modulos (suelo, muro, agua, vegetación).
- 'Assets/Scenes/' - (crear) Cuaderno.unity, Nivel1_MuelleDeCobre.unity, Nivel2_PatioDeBombas.unity, Nivel3_FaroDeLiria.unity + Build Settings en ese orden.
- 'ProjectSettings/TagManager.asset' - capas Ground, Water, Interactable, Player, Battery.

## Activos y particularidades relevantes
- Unity 6000.3.8f1; URP 17.3.0 activo por 'Assets/Settings/PC_RPAsset.asset' (guid 4b83569d67af61e458304325a23e5dfd), color space Linear. No migrar ni reimportar.
- 'ProjectSettings.asset' → activeInputHandler: 1 = **solo Input System nuevo**; prohibido Input.GetKey / Input.GetAxis.
- 'Assets/InputSystem_Actions.inputactions' es el asset de acciones global del proyecto (registrado en EditorBuildSettings) y su Player/Interact tenía interaction **Hold**: se cambia a **Press** (pulsación única, `.performed` inmediato) para que E dispare la interacción contextual en el mismo frame; el asset no se elimina ni se reestructura.
- En 'Assets/Input/Relevo.inputactions' toda acción de interacción usa tipo Button sin interacción Hold; ningún canal de entrada introduce retardo ni "hold time". Convención de código: sólo `action.performed`, nunca lógica basada en `IsPressed()` sostenido para interactuar.
- Sin Cinemachine: cámara ortográfica propia (pitch 55°, yaw 0, tamaño ajustado por nivel), sin rotación manual; el movimiento se calcula en XZ a partir del forward de cámara proyectado, así W y D coinciden con pantalla.
- Capas ya ocupadas: 0,1,2,4,5 → las nuevas van en 6–10. No hay TextMeshPro Essentials importados; TMP viene con com.unity.ugui 2.0.0 (fuente SDF por defecto soporta acentos del español).
- Movimiento con CharacterController (Y fijo, sin gravedad ni salto) para evitar física caótica; el agua se bloquea con volúmenes sólidos invisibles (cara superior en y=+1.2) más un quad visual de agua en y=-0.35; nunca hay "ahogo" ni triggers de castigo.
- Puente retraído = Collider desactivado + malla oculta; puente activo = Collider activo + despliegue interpolado. Paso de agua o puente retraído imposible por construcción.
- Regla dura: la batería es un único prefab persistente; nunca se instancia ni se destruye, sólo cambia de estado/padre (Carrying, InSocket, Grounded).
- Interactuables con Collider en modo trigger en la capa Interactable para no atrapar a Nilo; detección por Physics.OverlapSphere cacheado (sin FindObjectOfType en Update).
- Ruta del proyecto en OneDrive ('C:/Users/pipef/OneDrive/Desktop/RelevoPrime'): los reimportados pueden ser lentos, mantener el árbol de assets mínimo.

## Pasos
- [ ] 1. Fase 0 — Andamiaje: carpetas del árbol exigido, capas Ground/Water/Interactable/Player/Battery y tags Player/Battery (manage_editor add_layer/add_tag), materiales de paleta (create_pbr_material), 'Assets/Input/Relevo.inputactions' (create_input_action_asset) con Interact en Press y cambio de Player/Interact Hold→Press en el asset de acciones global [depends: none]
- [ ] 2. Fase 1 — Sistemas núcleo en Assets/Scripts (Core/Player/Interactions/Level/Save/UI) con eventos C#, interacción por `performed` de pulsación única, sin FindObjectOfType en Update, Debug.Log de trazabilidad y AudioTonePlayer con tonos sintetizados; validar con validate_script + check_compile_errors [depends: 1]
- [ ] 3. Fase 2 — Vertical slice Nivel 1 con primitivas: escena 'Nivel1_MuelleDeCobre' (A con batería y base A, puente temporal A-B, palanca en B, pasarela permanente B-D, camino A-D, agua, salida en B, recuerdo en desvío, Maia), HUD en español (batería/objetivo/recuerdos/prompt "E"), pausa (reanudar, pedir pista, reiniciar, cuaderno) y R con confirmación [depends: 2]
- [ ] 4. Fase 3 — Prueba y corrección de la slice con unity_verification_agent (Play Mode: recoger→insertar→cruzar A-B→palanca→volver por D→recuperar batería→salida, verificando que una sola pulsación de E interactúa, más reinicio y guardado) y capturas de game_view; **parada para validación del usuario** antes de continuar [depends: 3]
- [ ] 5. Fase 4 — Prefabs definitivos y estética: Nilo (carcasa clara, extremidades azul oscuro, ojos cian, bufanda naranja), Maia (terminal verde con vegetación), batería ámbar, base hexagonal, puentes, palanca turquesa, salida y recuerdo; partículas sólo en energía, palanca y faro; reutilizar en la escena 1 [depends: 4]
- [ ] 6. Fase 5 — Niveles 2 y 3 con los prefabs verificados ('Nivel2_PatioDeBombas': dos bases, dos puentes temporales con cable visible y dos palancas, secuencia correcta sin estados imposibles; 'Nivel3_FaroDeLiria': entrega física de la batería al faro y celebración de restauración) + 'Cuaderno.unity' con 3 tarjetas bloqueado/disponible/completado, recuerdos, guardado JSON y Build Settings [depends: 5]
- [ ] 7. Fase 6 — QA por nivel (inicio, batería en base, puente activo, palanca, pasarela permanente, recuperación, salida, recuerdo, reinicio, carga de progreso), build Windows de prueba e informe de archivos creados, pruebas ejecutadas, resultados y limitaciones reales [depends: 6]

## Verificar
- 'check_compile_errors' sin errores y 'Cuaderno.unity' como escena 0 de Build Settings (arranque directo sin Play manual).
- E responde a una sola pulsación en todos los interactuables (batería, base, palanca, recuerdo, Maia, salida) sin mantener la tecla.
- Nivel 1: el puente A-B sólo es transitable con la batería en la base A; la palanca abre B-D de forma permanente; salir sin batería muestra "Necesitas la batería" y no completa; con batería completa y guarda el progreso en JSON.
- Batería única comprobada (GameObject persistente, sin duplicados ni pérdidas), imposible cruzar agua o puente retraído y sin quedarse atrapado en costuras (CharacterController con step offset 0.3 y topes de agua altos).
- Play Mode con capturas de game_view por nivel, consola sin errores/excepciones en 'get_unity_logs' y cuadros estables en PC medio.
- Tras reiniciar y volver al Cuaderno, los niveles completados y los recuerdos se conservan desde 'Application.persistentDataPath/relevo_save.json'.
