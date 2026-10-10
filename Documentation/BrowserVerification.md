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

La automatización de clics rápidos fue inconsistente. Para verificar los botones se usaron gestos de puntero sostenidos con un desplazamiento pequeño dentro del mismo botón. Esto limita las conclusiones sobre pulsaciones breves y no sustituye pruebas humanas de comodidad de uso.

La consola inspeccionada mostró avisos de WebGL `INVALID_ENUM: getInternalformatParameter` y avisos de duración de callbacks; no mostró excepciones C# del juego durante esa inspección. Había un 404 de favicon de una carga anterior; la plantilla final incluye un favicon vacío.

## Pendiente

Victoria manual de cinco oleadas en navegador, joystick con touch real, multitouch simultáneo, ausencia de interferencia entre controles, orientación móvil horizontal, zonas seguras y rendimiento en teléfonos. La victoria de cinco oleadas ya se comprobó mediante simulación; no se declara victoria manual Web ni compatibilidad de dispositivos sin probarlos.
