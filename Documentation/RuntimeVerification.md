# Unity runtime verification

Unity 6000.3.5f2, batch play mode. 102 checks passed.

Actual scene initialization, material/shader loading, English menu and purchase buttons, spawning, pause/resume, browser focus-loss handler, restart cleanup and time restoration were exercised. No gameplay errors were logged.

Additional pointer-event checks covered the visible joystick center and all four cardinal directions with the HUD's bottom-left pivot at canvas scales 0.75, 1 and 1.6; second-pointer ownership; normalized drag; release, disable, modal and pause cancellation; and UI raycasts at the current press position without relying on cached pointer state. These events were constructed in Play Mode, not sent by a real touchscreen.

Editor-only SearchInit.IndexationOnStartup exceptions recorded separately: 1. This Unity Search indexing exception is a known limitation of this verification environment, not counted as a passing scene check.

See [BrowserVerification.md](BrowserVerification.md) for separate desktop, emulation and Pixel 10 hardware checks, and [TouchVerification.md](TouchVerification.md) for subsequent human finger and physical two-contact results. Constructed pointer events do not replace these physical checks; uncorroborated gesture sequences and other mobile devices remain pending as specified in the touch report.
