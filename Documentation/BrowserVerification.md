# Verificación Web en Chrome

Build generada por Unity 6000.3.5f2 mediante IL2CPP, 17 204 494 bytes. Ejecutada en Chrome de escritorio en este Mac, servida por HTTP desde `Builds/Web`.

## Comprobaciones ejecutadas

- El tablero completo se ve centrado en una ventana ancha, con camino, seis espacios, guardián, reservas y controles separados.
- La tecla D mueve al guardián hacia la derecha.
- Abrir la consola del navegador provoca pausa; cerrar la consola mantiene la pausa hasta reanudar explícitamente.
- Se inicia la primera oleada con el puntero. Sin defensas y con el guardián apartado del camino, las reservas bajan de 20 a 12; al finalizar vuelve a preparación y entrega el bono de 40 recursos.
- La segunda oleada incluye círculos comunes y triángulos corredores. La partida termina en derrota con 0 reservas, una oleada superada y 53 segundos de combate.
- «Volver a jugar» devuelve la partida a preparación para la primera oleada: 20 reservas, 100 recursos, sin enemigos y sin defensas, con el guardián en su posición inicial.
- Seleccionar el espacio 3 abre las opciones; elegir calabaza muestra costo, función, estadísticas y mejora posterior. Confirmar instala una calabaza, cierra el panel y reduce el saldo de 100 a 50 una sola vez.

La primera automatización de clics rápidos fue inconsistente; se usaron gestos de puntero para verificar los botones. El diagnóstico posterior aisló la causa y confirmó clics breves, como se detalla abajo. Sigue pendiente una prueba humana de comodidad de uso.

## Clics breves: comprobación del 10 de octubre de 2026

Se usó una copia local de la página con un indicador de eventos DOM y un componente temporal que mostraba `Input.mousePosition`, presión/liberación y el primer resultado de `EventSystem.RaycastAll`. Ambos indicadores se retiraron al terminar; no se modificó la lógica de entrada ni de compras.

La llamada nativa de automatización `click` trasladaba el cursor y enviaba `mousedown`/`mouseup`, pero no `mousemove`. Chrome recibía las coordenadas del clic; Unity recibía presión y liberación con una posición anterior del mouse, inicialmente `(0, 0)`. En ese caso el raycast detectaba el fondo del modal y el botón no se activaba. Un gesto que sí emitió `mousemove` actualizó la posición en Unity. Esto explica la inconsistencia anterior de esta automatización; no demuestra un fallo con un mouse físico.

Con la posición ya actualizada, se ejecutaron los siguientes clics **sin arrastre durante el clic**. La duración se midió entre los eventos DOM, porque el tiempo real no siempre coincidió con el solicitado a la herramienta.

| Acción | Duración medida | Resultado observado |
| --- | --- | --- |
| Reiniciar desde pausa | 48 ms | Vuelve a preparación con 20 reservas y 100 recursos; el raycast identifica «Reiniciar partida» |
| Seleccionar el espacio 3 | 51 ms | Abre el panel «Espacio 3 · 100 recursos» |
| Reiniciar nuevamente desde pausa | 97 ms | Vuelve a preparación con los valores iniciales; el raycast identifica «Reiniciar partida» |

Estas muestras confirman respuesta a clics breves en el botón de reinicio y la selección del tablero en Chrome de escritorio. No se declara probado cada botón con cada duración; la compra y su débito único se verificaron en la prueba anterior.

La consola inspeccionada mostró avisos de WebGL `INVALID_ENUM: getInternalformatParameter` y avisos de duración de callbacks; no mostró excepciones C# del juego durante esa inspección. Había un 404 de favicon de una carga anterior; la plantilla final incluye un favicon vacío.

## Pendiente

Victoria manual de cinco oleadas en navegador, joystick con touch real, multitouch simultáneo, ausencia de interferencia entre controles, orientación móvil horizontal, zonas seguras y rendimiento en teléfonos. La victoria de cinco oleadas ya se comprobó mediante simulación; no se declara victoria manual Web ni compatibilidad de dispositivos sin probarlos.
