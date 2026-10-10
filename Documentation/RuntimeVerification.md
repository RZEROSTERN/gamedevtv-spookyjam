# Unity runtime verification

Unity 6000.3.5f2, batch play mode. 64 checks passed.

Actual scene initialization, material/shader loading, menu, purchase buttons, spawning, pause/resume, browser focus-loss handler, restart cleanup and time restoration were exercised. No gameplay errors were logged.

Editor-only SearchInit.IndexationOnStartup exceptions recorded separately: 1. This Unity Search indexing exception is a known limitation of this verification environment, not counted as a passing scene check.

Physical keyboard/mouse, multitouch and real browser/device compatibility require separate checks.
