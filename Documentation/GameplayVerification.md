# Gameplay verification
Execution host: .NET 8, using Unity 6000.3.5f2 managed assemblies. Unity editor, native engine, rendering and devices were NOT executed.

## Full matches
- Defense strategy, wave 1: 20 reserves, 0 resources.
- Defense strategy, wave 2: 20 reserves, 16 resources.
- Defense strategy, wave 3: 20 reserves, 0 resources.
- Defense strategy, wave 4: 17 reserves, 46 resources.
- Defense strategy, wave 5: 17 reserves, 88 resources.
- Result: Victory, 17 reserves, 155.9 seconds of combat.
- Guardian only, wave 1: 20 reserves, 100 resources.
- Guardian only, wave 2: 19 reserves, 189 resources.
- Guardian only, wave 3: 13 reserves, 298 resources.
- Result: Defeat, 0 reserves, 90.9 seconds of combat.

Assertions passed: 44.

Pending: real mouse/keyboard and multitouch, browser full match, mobile landscape and safe areas, focus-loss, native devices.
