# Colados / Uninvited

## Alcance y decisiones

Proyecto nuevo para Unity **6000.3.5f2**, versión encontrada en este equipo. No había proyecto Unity previo ni remoto Git. `master` conserva el commit inicial; el prototipo se desarrolla en `feature/uninvited-prototype`.

Renderizado integrado, uGUI 2.0.0 y el sistema de entrada integrado de Unity. No se mezclan sistemas de entrada. No se requieren imágenes descargadas, fuentes externas, cuentas, backend ni servicios para jugar. Los únicos paquetes son uGUI y módulos incluidos con el editor.

La escena incluida contiene un `GameBootstrap` con referencia al balance. Este construye tablero, cámara, actores e interfaz localmente al iniciar. Escena, balance y metadatos se escribieron con la herramienta de respaldo al estar bloqueado el editor; su importación en Unity aún está pendiente. El menú **Uninvited → Generate game scene** genera la escena mediante las APIs de Unity, conserva un balance existente y reemplaza la escena generada. No debe usarse sobre una escena con cambios manuales que quieras conservar.

La representación utiliza sprites de formas generadas, líneas, una fuente integrada y un material local en Resources. El camino usa waypoints; no se utiliza física ni navegación. Ningún actor bloquea el movimiento de otro.

## Organización

| Archivo | Responsabilidad |
| --- | --- |
| `GameConfig` | Estadísticas, cinco secuencias de oleadas, posiciones y balance editable |
| `GameSession` | Estado de partida, transiciones, reinicio y coordinación de la simulación |
| `Economy` | Reservas, recursos y transacciones |
| `Actors` | Estado independiente de cada enemigo y defensa; recorrido por waypoints |
| `WaveDirector` | Enemigos pendientes y temporizador de aparición |
| `CombatSystem` | Selección de objetivos, ataque automático y ralentización más fuerte |
| `GuardianInput`, `VirtualJoystick` | Teclado, mouse, touch y captura del puntero del joystick |
| `GameWorldView`, `Geometry` | Presentación del tablero y feedback de combate |
| `GameHud` | HUD por eventos, compras con confirmación, menús y adaptación al área segura |
| `GameBootstrap` | Arranque, tiempo, pérdida de foco y mejor resultado local |
| `ProjectSetup`, `GameplayVerification` | Generación, build y comprobaciones deterministas |

Los ScriptableObjects son plantillas de lectura durante la partida. El dinero, resistencia restante, mejoras y temporizadores se guardan en objetos de sesión; reiniciar no modifica el asset de balance.

## Cómo iniciar

1. Instala/abre Unity **6000.3.5f2** con Web Build Support y activa tu licencia en Unity Hub.
2. Añade la carpeta raíz de este repositorio como proyecto.
3. Espera la importación. Ejecuta **Uninvited → Generate game scene** para configurar el proyecto y guardar la escena mediante Unity.
4. Abre esa escena y pulsa Play.
5. Pulsa **Abrir la fiesta**. Toca los espacios numerados para comprar defensas; con 100 recursos puedes comprar dos calabazas.
6. Pulsa **Iniciar oleada**. Muévete con WASD, flechas o el joystick, también utilizable con mouse. El ataque es automático.

La preparación no tiene límite de tiempo. Construcción y mejoras están bloqueadas en combate y pausa. Una oleada acaba cuando no quedan apariciones pendientes ni enemigos activos. Al perder foco, la partida pausa; reanudar requiere una acción explícita.

## Build Web

Con el módulo Web instalado, ejecuta **Uninvited → Build Web**. Produce `Builds/Web`, excluido de Git. Para abrirlo, sirve la carpeta por HTTP; no abras `index.html` mediante `file://`.

```sh
python3 -m http.server 8080 --directory Builds/Web
```

Abre `http://localhost:8080`. La plantilla tiene tamaño adaptable, márgenes de área segura, carga visible, límite de resolución y pausa al ocultar la pestaña. No tiene dependencias web externas.

Build y comprobación por línea de comandos en este Mac:

```sh
/Applications/6000.3.5f2/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$PWD" -buildTarget WebGL \
  -executeMethod Uninvited.Editor.ProjectSetup.VerifyAndBuild \
  -logFile /tmp/uninvited-build.log
```

Sin compresión para simplificar el alojamiento del prototipo. Configurar Brotli y los encabezados HTTP correspondientes es una mejora futura; no es requisito de juego.

Según la [documentación de Unity 6.3](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/webgl/intro/browsercompatibility), Web requiere WebGL 2 y WebAssembly; el soporte móvil incluye Safari iOS 15+ y Chrome Android 58+. Esto describe soporte del motor, **no pruebas de este juego**. Hay que verificar dispositivos concretos. Los módulos iOS y Android están instalados, pero no se han generado ni probado builds nativas ni publicaciones en tiendas.

## Ajustar balance

Selecciona `Assets/Uninvited/Data/GameBalance.asset` en el Inspector. Puedes editar costos, mejora, alcance, daño, intervalos, velocidades, reservas, recompensas y bonos, además de las secuencias de aparición. `GameConfig.SetDefaults()` define los valores de creación; modificarlo no sobrescribe un asset ya creado.

Los índices de los arrays de defensas y enemigos corresponden al orden de sus enums. Conserva tres entradas en ese orden. Las cinco secuencias iniciales respetan los totales solicitados y mezclan los enemigos intencionalmente.

Balance inicial: 156 segundos aproximados de combate en la estrategia automatizada ganadora, más preparación ilimitada. El objetivo de 4–6 minutos contempla el tiempo para decidir compras y mejoras; aún necesita medición con jugadores. No es una dificultad calibrada mediante pruebas humanas.

## Sustituir los placeholders

La lógica no conoce sprites. Sustituye la creación de formas en `GameWorldView` por prefabs o sprites; conserva la asociación con `EnemyState.Id` y las lecturas de posición, resistencia y ralentización. Los eventos `HitEffect`, `PulseEffect` y `EnemyResolved` sirven para conectar animaciones. Cambia el tema visual sin alterar tipos de movimiento, economía ni recorrido.

## Comprobaciones y límites actuales

- Rosetta 2 se instaló con autorización: era un requisito de arranque del editor en este Mac.
- Unity pudo arrancar en batch, pero se detuvo por falta de licencia activa. No llegó a importar o ejecutar la escena ni a producir una build Web. Los archivos de escena y balance incluidos se generaron mediante la herramienta de respaldo usando los datos predeterminados verificados.
- Los scripts completos, incluida la interfaz y herramientas de editor, compilaron con Roslyn contra los ensamblados reales instalados de Unity 6000.3.5f2 y uGUI.
- La simulación pasó 44 comprobaciones ejecutadas en .NET 8 contra el código de juego y `Vector2`/`Mathf` de Unity, sin ejecutar el motor nativo. El informe está en `GameplayVerification.md`.
- Esta ejecución independiente no verifica renderizado, serialización de Unity, eventos de UI, touch, foco, navegador ni build.

Para repetir las comprobaciones sin iniciar el editor:

```sh
python3 Tools/compile-check.py
dotnet run --project Tools/Simulation/Simulation.csproj
```

El segundo comando exige .NET 8 y la instalación local de Unity. Puedes cambiar la ruta con `-p:UnityPath=/ruta/Unity.app`. No descarga paquetes NuGet. Dentro del editor, usa **Uninvited → Verify gameplay rules** para ejecutar las mismas comprobaciones en Unity.

Pruebas pendientes: importar y ejecutar sin errores, partida completa en navegador, reinicio visual sin objetos duplicados, mouse y teclado, multitouch simultáneo, controles sin interferencia, orientación horizontal, zonas seguras, pérdida de foco con reanudación explícita y rendimiento en dispositivos reales.

No se abrió un pull request: no hay remoto configurado y falta completar la validación del editor y Web. La revisión debe partir de un PR funcional, con estas limitaciones visibles.

## Referencia de arquitectura

El artículo de DEV [The ScriptableObject Trap](https://dev.to/gamedevnotes/the-scriptableobject-trap-why-your-game-data-wont-reset-after-you-quit-play-mode-5f6e), de [GameDevNotes](https://dev.to/gamedevnotes), recomienda separar plantillas compartidas de estado mutable. Se adoptó esa separación. Es una referencia comunitaria, no prueba de funcionamiento de este prototipo.
