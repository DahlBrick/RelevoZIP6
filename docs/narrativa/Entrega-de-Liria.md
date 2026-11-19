# RELEVO · Entrega de la campaña

El proyecto contiene doce puertos jugables. El Mercado de la Sal y el Varadero de las Campanas cierran el recorrido disponible. El desenlace, las decisiones de los actos posteriores y la puesta en escena quedan para la fase de narrativa. La referencia de continuidad está en [la biblia de Liria](biblia_de_liria.md) y en [el resumen del juego](resumen_para_compartir.md).

## Abrir el proyecto

Descomprime el paquete, añade la carpeta `RELEVO` a Unity Hub y ábrela con **Unity 6000.3.8f1**. La primera importación necesita acceso a los registros de paquetes y a los repositorios Git que declara `Packages/manifest.json`. Para jugar desde el comienzo, abre `Assets/Scenes/Juego/Cinematica_Liria.unity`. Para probar un puerto por separado, abre su escena y pulsa Play. La lista de escenas de la campaña está en Build Profiles.

## Escribir en el Inspector

Abre `Assets/Resources/Bitacora/Guiones`. Hay una ficha `Puerto01` a `Puerto12` por nivel. Puedes cambiar el nombre, la presentación, el cierre y el título y texto del recuerdo. El juego reutiliza la ficha en el puerto, el cuaderno y la carta de viaje. Conserva el índice y el identificador del recuerdo para mantener la compatibilidad con las partidas guardadas.

El campo **Pista Maia** es el texto base. Las pistas que cambian al mover una caja, retirar una batería o reproducir una cinta viven en `LevelManager.PistaActual` y `EcoDeTurno.Campana`. Las tarjetas ilustradas de las explicaciones están en `TutorialMaia` e `IlustracionTutorial`.

## Añadir una cinemática con Timeline

1. Abre el puerto e incorpora un objeto raíz de escena para el director. Un objeto propio en la raíz se conserva al volver a hornear los muelles.
2. Añade **Secuencia De Puerto**. Unity incorpora el **Playable Director**. Asigna tu Timeline y enlaza sus pistas a los objetos de esa escena. Deja **Play On Awake** desactivado.
3. Elige **Llegada** para reproducirla antes de la presentación del puerto, o **Cierre** para hacerlo después de registrar la salida con batería y antes del menú de resultados. Usa un director por momento. Puedes reunir varios planos en su Timeline.
4. Prueba el final de la Timeline y la acción Cancelar o Pausa para saltarla. Al terminar la llegada, vuelve el control habitual del nivel. Al terminar el cierre, aparece el resultado. Los reintentos omiten la secuencia de llegada.

El nivel 12 dispone del mismo enlace de cierre. Ahí puede incorporarse la escena final que decida el equipo. La opción **Elegir puerto** permite seguir explorando después.

## Dos recorridos de referencia

| Puerto | Solución jugable | Recuerdo opcional |
| --- | --- | --- |
| **11 · Mercado de la Sal** | Empujar el cajón al rombo, pasar la reja, insertar la batería en A y cruzar hasta la palanca. Volver por la pasarela fija, recuperar la batería, cargar D y cruzar con ella al pórtico. | **Pesa de media arroba**, en el secadero de madera al sur del mercado. |
| **12 · Varadero de las Campanas** | Dejar el primer cajón en el rombo del puente de carga. Grabar una espera sobre I, usar el torno durante la repetición y desembarcar con la batería. Cargar D y cruzar el último puente. La palanca de la orilla final abre un regreso permanente. | **Badajo de repuesto**, en el almacén occidental. La segunda caja mantiene abierta su reja. |

El plano transitable se edita en `Assets/Scripts/Level/LevelLayouts.cs`. Los toldos, bancales, moldes y campanas de estos dos puertos se colocan desde `PuertosDeSal.cs`. Las campanas siguen el estado de su puente y sirven de señal a distancia. Las ilustraciones de los doce recuerdos comparten la lámina `Assets/Resources/Bitacora/LaminasDelCanal.png`.

## Alcance de las pruebas

Los dos puertos nuevos se recorrieron en Play con las colisiones y los actores del juego. Las pruebas incluyeron recoger ambos recuerdos, llegar a ambas salidas con la batería y pasar del puerto 11 al 12. Las capturas del cuaderno y de la carta de viaje proceden de Game View. El paquete contiene el proyecto editable de Unity. La compilación para la plataforma de distribución y la prueba de las cinemáticas definitivas corresponden a la entrega final.

