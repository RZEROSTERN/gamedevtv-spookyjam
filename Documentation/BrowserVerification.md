# Web verification in Chrome

## English UI and mobile layout: October 10, 2026

Unity 6000.3.5f2 IL2CPP Web build: **17,232,559 bytes**. Served locally over HTTP from `Builds/Web` and inspected in desktop Chrome on the development Mac. Mobile cases below use Chrome DevTools emulation, not physical phones or Safari.

| Viewport/profile | Checks performed | Observed result |
| --- | --- | --- |
| Wide desktop window | English menu, opening the party, board layout | Menu instructions and buttons render in English; the whole board is centered between separate HUD/control bars |
| Responsive, 320 × 568 | Menu, slot selection, defense descriptions, purchase confirmation, joystick | English text fits; selecting slot 3 and buying a Pumpkin changes resources from 100 to 50 once; held joystick input moves the guardian and the knob recenters on release |
| Responsive, rotated to 568 × 320 | Board visibility, starting a wave, pause and restart dialogs | Whole board remains visible; controls use shorter bars; the rotation suggestion remains visible after starting a wave; enemies spawn; pause/restart buttons remain accessible |
| Chrome iPhone SE profile, 375 × 667, device pixel ratio 2 | Reload with density 2 active; canvas dimensions; menu layout | The console reports 375 CSS pixels and 750 render pixels for canvas width. English menu paragraphs and buttons remain visible at a readable layout size |

The profile's advertised touch capability was one touch point. This checks single-pointer emulation only. A real iPhone was not used. Higher canvas density was applied by reloading after choosing the profile; changing a profile without reloading retains the render pixel ratio chosen at startup.

During the rotated responsive run, the rotation suggestion persisted from preparation into combat. The pause dialog rendered in English and the restart action restored the initial 100 resources. Constructed Play Mode events separately check joystick cancellation on pause and opening a modal, second-pointer ownership, and presses on controls blocking board selection; see `RuntimeVerification.md`.

Chrome's console showed **No errors** in the inspection performed, alongside WebGL `INVALID_ENUM: getInternalformatParameter` and callback duration warnings. This is an inspection result rather than a guarantee about every subsequent frame or device.

Changing dimensions through the DevTools toolbar temporarily cleared the canvas while focus was outside the game. Clicking the canvas restored rendering at the new dimensions without a page reload. Real mobile rotation and browser-bar focus behavior still need device testing.

## Earlier prototype checks (PR #1)

The original 17,204,494-byte build was also checked in desktop Chrome on this Mac:

- Full board, six construction slots, guardian, reserves, and controls visible in a wide window.
- D moves the guardian to the right.
- Opening the browser console pauses the game; closing it leaves the game paused until explicit resumption.
- With no defenses and the guardian away from the path, the first wave reduces reserves from 20 to 12, ends in preparation, and awards the 40-resource bonus.
- Wave two includes common circles and runner triangles. The match ends in defeat with zero reserves, one completed wave, and 53 seconds of combat.
- Replay restores preparation with 20 reserves and 100 resources, no enemies or defenses, and the guardian at its initial position.
- Selecting slot 3, choosing a Pumpkin, and confirming the purchase installs the defense and deducts 50 exactly once.

### Brief clicks and native automation

A temporary DOM event indicator and Unity component reported `Input.mousePosition`, press/release events, and the first `EventSystem.RaycastAll` hit. Both were removed after diagnosis.

The native automation `click` moved the visible cursor and sent `mousedown`/`mouseup` without `mousemove`. Chrome received the click coordinates, but Unity still used an earlier mouse position, initially `(0, 0)`. The raycast hit the modal background instead of the intended button. A gesture emitting `mousemove` updated Unity's position. This explains the automation inconsistency; it does not establish a physical mouse failure.

With the position updated, clicks without dragging during the click produced these results. Durations were measured between DOM events rather than assumed from the requested tool delay.

| Action | Measured duration | Observed result |
| --- | --- | --- |
| Restart from pause | 48 ms | Preparation with 20 reserves and 100 resources; raycast identifies the restart button |
| Select slot 3 | 51 ms | Opens the slot panel with 100 resources |
| Restart from pause again | 97 ms | Restores initial values; raycast identifies the restart button |

These samples cover restart and board selection, not every button at every duration. The current-position UI raycast added in the English delivery addresses stale UI hit state; it does not synthesize a missing mouse movement event for native automation.

## Pending checks

A complete manual five-wave browser victory, physical mouse usability, real-device joystick/touch, simultaneous physical multitouch, mobile browser bars and safe areas, real landscape usability, phone performance, and other browsers. Five-wave victory is verified in simulation. Responsive emulation and constructed pointer events are documented separately and do not establish physical-device compatibility.
