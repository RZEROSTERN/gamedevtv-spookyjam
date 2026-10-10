# Unity runtime verification

Unity 6000.3.5f2, batch play mode. 87 checks passed.

Actual scene initialization, material/shader loading, English menu and purchase buttons, spawning, pause/resume, browser focus-loss handler, restart cleanup and time restoration were exercised. No gameplay errors were logged.

Additional pointer-event checks covered local joystick coordinates at canvas scales 0.75, 1 and 1.6; second-pointer ownership; normalized drag; release, disable, modal and pause cancellation; and UI raycasts at the current press position without relying on cached pointer state. These events were constructed in Play Mode, not sent by a real touchscreen.

Editor-only SearchInit.IndexationOnStartup exceptions recorded separately: 1. This Unity Search indexing exception is a known limitation of this verification environment, not counted as a passing scene check.

Physical mouse/touch, simultaneous multitouch and real mobile browser/device compatibility require separate checks. See BrowserVerification.md for desktop Chrome and responsive emulation checks.
