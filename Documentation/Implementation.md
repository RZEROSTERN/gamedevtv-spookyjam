# Uninvited

## Scope and decisions

A complete five-wave defense prototype for **Unity 6000.3.5f2**, the editor installed on the development Mac. The initial prototype was merged through PR #1 into `master` in `RZEROSTERN/gamedevtv-spookyjam`. Production changes go through functional PRs from separate branches; work pauses when a PR is opened for review.

The project uses the Built-in Render Pipeline, uGUI 2.0.0, and Unity's built-in Input manager. No downloaded images, external fonts, accounts, backend, or online services are required to play. Only uGUI and editor-bundled modules are required.

The included scene contains a `GameBootstrap` referencing the balance asset. It creates the board, camera, actors, and UI at startup. **Uninvited → Generate game scene** preserves an existing balance asset but replaces the generated scene; avoid using it on a scene containing manual changes you want to keep.

Graphics use generated shape sprites, lines, a built-in font, and a local material in Resources. Enemies follow waypoints without physics or navigation. Actors do not block each other. The game UI, defense descriptions, wave titles, Web loading page, and delivery documentation are in English.

## Organization

| File | Responsibility |
| --- | --- |
| `GameConfig` | Editable stats, five wave sequences, positions, and balancing |
| `GameSession` | Game state, transitions, restart, and simulation coordination |
| `Economy` | Candy reserves, resources, and transactions |
| `Actors` | Independent enemy/defense state and waypoint movement |
| `WaveDirector` | Pending enemies and spawn timing |
| `CombatSystem` | Target selection, automatic attacks, and strongest applicable slow |
| `GuardianInput`, `VirtualJoystick` | Keyboard, mouse, touch, and joystick pointer ownership |
| `GameWorldView`, `Geometry` | Board presentation and combat feedback |
| `GameHud` | Event-driven HUD, purchase confirmation, dialogs, and safe-area layout |
| `DisplayMetrics.jslib` | Web canvas render pixels per visible CSS pixel |
| `GameBootstrap` | Startup, time, focus loss, and locally saved best result |
| `ProjectSetup`, `GameplayVerification`, `RuntimeVerification` | Generation, builds, simulation, and scene checks |

ScriptableObjects are read-only templates during a match. Money, remaining resistance, upgrades, and timers live in session objects. Restart does not modify the balance asset.

## Getting started

1. Install/open **Unity 6000.3.5f2** with Web Build Support and activate a license in Unity Hub.
2. Add this repository's root folder as a project and allow it to import.
3. Open `Assets/Uninvited/Scenes/Uninvited.unity` and press Play. Use **Uninvited → Generate game scene** if regeneration is needed.
4. Select **Open the party**, then select numbered slots to build defenses. The initial 100 resources can purchase two Pumpkins.
5. Select **Start wave**. Move with WASD, arrow keys, or the joystick; the joystick also supports mouse dragging. Attacks are automatic.

Preparation has no time limit. Construction and upgrades are blocked during combat and pause. A wave ends only when no pending spawns or active enemies remain. Losing focus pauses the game; resuming requires an explicit action.

## Mobile input and layout

The joystick belongs to the pointer that pressed it. Another pointer cannot drag or release it. Release, disable, opening a modal, pause, and viewport changes clear its direction and recenter the knob. A new press is required after cancellation. The joystick converts screen coordinates into its own RectTransform coordinates, subtracts the rectangle's visible center to account for its bottom-left pivot, and does not wait for a drag threshold.

Board selection raycasts UI at the current press position using a reused pointer event and result list. It does not depend on cached pointer-over-UI state from the previous frame. A press on the joystick, header, footer, or dialog cannot select a slot underneath; once one board touch opens a modal, later touches in the same frame cannot replace it.

Web HUD scaling uses the ratio between the canvas render width and its visible CSS width. This keeps control sizes independent of render pixel density. At the minimum HUD scale, 64-unit buttons have a height of 48 CSS pixels. This sizing rule is not a physical-device usability certification.

Portrait retains the main layout. Landscape uses shorter header/footer bars to give the board more height while retaining the whole board. Touch devices receive a rotation suggestion that remains present across phase changes. Dialog paragraphs grow to fit wrapped text; content can be scrolled when it exceeds the viewport. The loading page and canvas container follow `visualViewport` resize events, with a window-size fallback and safe-area padding.

## Web build

Use **Uninvited → Build Web** with Web Build Support installed. Output goes to `Builds/Web`, excluded from Git. Serve it over HTTP instead of opening `index.html` via `file://`:

```sh
python3 -m http.server 8080 --directory Builds/Web
```

Open `http://localhost:8080`. The local template includes loading feedback, safe-area padding, a render pixel-ratio cap of 2, visible-viewport sizing, and pause on tab hiding. It has no external Web dependencies.

Build and simulation verification on the development Mac:

```sh
/Applications/6000.3.5f2/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$PWD" -buildTarget WebGL \
  -executeMethod Uninvited.Editor.ProjectSetup.VerifyAndBuild \
  -logFile /tmp/uninvited-build.log
```

Compression is disabled for simple prototype hosting. Brotli and the corresponding HTTP headers are a future hosting improvement.

The [Unity 6.3 browser compatibility documentation](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/webgl/intro/browsercompatibility) describes engine requirements, including WebGL 2 and WebAssembly. Engine support does not establish tested compatibility for this game. iOS and Android editor modules are installed, but native builds and store releases have not been generated or tested.

## Balancing

Select `Assets/Uninvited/Data/GameBalance.asset` in the Inspector. Costs, upgrades, ranges, damage, intervals, speeds, reserves, rewards, bonuses, and spawn sequences are editable. `GameConfig.SetDefaults()` defines creation defaults; changing it does not overwrite an existing asset.

Defense and enemy array indices correspond to their enum order. Keep three entries in that order. The five initial sequences retain the requested enemy totals and deliberately interleave mixed enemy types.

The winning automated strategy has approximately 156 seconds of combat, plus unlimited preparation. The target total duration of 4–6 minutes still needs player measurement. Difficulty has not been calibrated through human playtests. This delivery changes presentation and controls rather than combat values or wave composition.

## Replacing placeholders

The logic has no sprite dependencies. Replace shape creation in `GameWorldView` with prefabs or sprites while retaining associations with `EnemyState.Id` and reads of position, resistance, and slowing. `HitEffect`, `PulseEffect`, and `EnemyResolved` can drive animations. Visual theme changes do not require changing movement rules, economy, or waypoints.

## Verification and current limits

- The project has been imported and compiled in Unity 6000.3.5f2 with an active license. Rosetta 2 was installed with authorization to run the editor on this Mac.
- The separate Roslyn check compiles runtime scripts against installed Unity/uGUI assemblies; Unity itself compiles editor-only verification code.
- Gameplay simulation passes 44 checks. A defense strategy wins all five waves; the guardian alone loses during wave three. See `GameplayVerification.md`.
- Play Mode checks cover the generated scene, English menus, purchase callbacks, spawning, pause/resume, focus-loss handler, restart cleanup, and pointer-event cases. See `RuntimeVerification.md` for the latest count and exact coverage.
- Browser results and their version/date scope are recorded in `BrowserVerification.md`. Constructed Play Mode pointer events and Chrome responsive emulation do not substitute for real devices or physical multitouch.
- Unity 6000.3.5f2 reports an editor-only `UnityEditor.Search.SearchInit.IndexationOnStartup` exception during batch Play Mode checks. It is recorded separately, never counted as a passing check or gameplay error.

Repeat the independent compilation check without starting the editor:

```sh
python3 Tools/compile-check.py
```

For simulation within Unity, use **Uninvited → Verify gameplay rules**. For scene checks, run the editor with `-batchmode -nographics -executeMethod Uninvited.Editor.RuntimeVerification.Run`, a project path, and a log path, **without `-quit`**. The verifier exits after completing its checks and restores Play Mode settings. The optional .NET 8 simulation tool in `Tools/Simulation` also needs the local Unity installation; it uses no downloaded NuGet packages and writes its own simulation report.

Chrome on a connected Pixel 10 running Android 16 was tested with ADB-generated touch events, including cardinal joystick directions, purchases, upgrades, rotation, background pause, restart, and a complete five-wave victory with 8 reserves remaining. This hardware run exposed and verified the fix for a joystick origin error missed by earlier constructed events. See the browser report for the exact build and scope.

A subsequent human finger session on the same Pixel 10 build received a comfortable-controls report. Touchscreen traces captured two simultaneous physical contacts; video and tester feedback corroborated owner-release, construction and pause/resume cancellation. See [TouchVerification.md](TouchVerification.md) for the evidence and its limits, including the second-finger-first release case supported only by the tester's report.

Pending: broader finger usability, independent capture of the remaining finger sequences, physical mouse usability, browser-bar expansion/collapse, other safe-area shapes, human session timing, sustained performance profiling, and other browser/device combinations. Reported automation limitations remain visible in the browser report.

## References

Joystick coordinate conversion and pivot semantics follow Unity's documentation for [ScreenPointToLocalPointInRectangle](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/RectTransformUtility.ScreenPointToLocalPointInRectangle.html) and [RectTransform.pivot](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/RectTransform-pivot.html). Real-device observation, rather than a community article, identified the specific offset bug.

[The ScriptableObject Trap](https://dev.to/gamedevnotes/the-scriptableobject-trap-why-your-game-data-wont-reset-after-you-quit-play-mode-5f6e), by [GameDevNotes](https://dev.to/gamedevnotes), informed the separation of shared templates and mutable session state.

[Mobile UI Design for Complex Games](https://dev.to/oceanviewgames/mobile-ui-design-for-complex-games-lessons-from-runescape-mobile-2928), by [Ocean View Games](https://dev.to/oceanviewgames), emphasizes readable touch targets and UI/gameplay separation. [Mobile Touch Controls: Scale-Independent Fix](https://dev.to/weirdcodesofficial/mobile-touch-controls-scale-independent-fix-1jem), by [weirdcodesofficial](https://dev.to/weirdcodesofficial), describes controls shrinking with the game container and changing browser viewport dimensions. These are community design references, not verification of this prototype. JavaScript interoperability follows [Unity's browser scripting documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-interactingwithbrowserscripting.html).
