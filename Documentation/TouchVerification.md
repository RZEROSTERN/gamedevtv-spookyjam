# Physical finger and multitouch verification

## Test setup

Pixel 10, Android 16, Chrome 154.0.8037.126. The merged PR #3 Web build is served at `http://127.0.0.1:8081/` over USB forwarding. The phone reports a 1080 × 2424 display with density 420. Its `focal_ts` touchscreen exposes slots 0–9 through `/dev/input/event2`, with raw X range 0–10799 and Y range 0–24239; that capability alone does not prove simultaneous input or game behavior.

Testing starts in portrait preparation with 20 reserves, 100 resources and six empty slots. A person uses their own fingers. After initial setup, the agent sends no automated input during the gesture cases. Read-only touchscreen events and a screen recording are used to corroborate observations. Comfort remains a subjective report from this tester on this phone, not a general usability certification.

No gameplay code, control sizes, balance values or build assets were changed for this verification. The tested WASM SHA-256 remains `c9ed5669441ff4b1353004b783d6f8fbb4e4e630e34ff8c4ece5bd4ed0d26eb2`.

## Cases

| ID | Physical action | Expected behavior | Result |
| --- | --- | --- | --- |
| F1 | Move the joystick with a thumb in four directions, make small movements, then release | Direction follows the thumb; movement stops and knob recenters on release; controls feel reachable | Partially observed: manual movement and release visible; an isolated four-direction/small-motion finger sequence was not established |
| M1 | Hold the joystick with one finger; add, move and release another finger on the joystick | The first finger retains control; releasing the second does not cancel the owner | Tester reported a pass; the recorded same-joystick pair released the owner first, so the second-finger-first release order is not independently corroborated |
| M2 | Hold two fingers on the joystick; release the first owner while the second remains down | Movement stops; the remaining finger cannot take over until a new press | Tester reported a pass; trace confirms the second finger remained for 3.68 seconds after owner release, with a centered knob visible in the video; see the boundary limitation below |
| M3 | Hold the joystick and tap a construction slot with another finger; close the dialog while the original finger remains down | Dialog opens, movement cancels, no automatic movement resumes after closing | Passed on the final repeat: slot 6 opens and closes with the owner still down; centered knob and stationary guardian visible; tester confirmed a pass |
| M4 | Hold the joystick and press Pause with another finger, then resume | Pause cancels movement; resume requires a new joystick press | Passed on the synchronized repeat: two physical contacts logged, pause/resume visible, neutral after resume while the owner remained down; tester confirmed the expected behavior |
| F2 | Report comfort of joystick, construction slots and dialog/pause buttons | Identify missed taps, thumb occlusion, reach problems or small targets | Tester reported that the controls felt comfortable; no individual target-size complaint was supplied |

Lift all fingers before starting a capture. Repeat any failed or ambiguous case independently before changing the game. Opening a dialog or pausing deliberately cancels joystick ownership; a fresh press is required afterward.

## Evidence and limits

### Initial capture — October 10, 2026

The touchscreen log contains 43 contact starts, all in slot 0, with a peak of **one simultaneous contact**. The first-to-last contact interval is 82.94 seconds. No `ABS_MT_SLOT` switch was emitted. This capture therefore does not demonstrate physical multitouch, regardless of the controller's advertised ten-slot capability.

The local screen recording is 153.84 seconds long. Reviewed game frames show joystick displacement and guardian movement, two Pumpkin purchases reducing resources from 100 to 50 to 0, and the start of wave 1. These observations demonstrate some manual single-contact interactions, but do not establish every F1 direction, small-motion behavior, or any M1–M4 case.

The tester answered yes to a follow-up asking whether two fingers were held down simultaneously and reported that the controls felt comfortable. The affirmative answer is treated as reported simultaneous use, not instrumented confirmation: it conflicts with the one-contact log and does not identify the timing or the result of each ownership/cancellation case. This prompted synchronized repeats, which did capture two contacts. The reason the initial log contained only one contact was not established. No multitouch result is marked passed on the strength of controller capability, a general comfort report, or constructed pointer tests.

### Synchronized pause repeat — October 10, 2026

The repeat recorded eight contact starts and a peak of **two simultaneous physical contacts**. Contact 2153 remained in slot 0 at the joystick while slot 1 independently pressed Pause (2154), then Resume (2155). No automated taps or drags were sent during this capture. The tester explicitly confirmed that Pause opened with the first finger still down and that resuming left the guardian stationary.

| Event | Seconds since first logged contact | Evidence |
| --- | --- | --- |
| Joystick owner 2153 presses | 16.124 | Slot 0 starts at raw coordinates 1412 / 21631 |
| Second finger presses Pause | 17.373 | Slot 1 starts at 9620 / 3990 while slot 0 remains active |
| Second finger releases Pause | 17.444 | Slot 1 ends; slot 0 remains active |
| Second finger presses Resume | 21.824 | Slot 1 starts at 8252 / 10712 while slot 0 remains active |
| Second finger releases Resume | 21.908 | Slot 1 ends; slot 0 remains active |
| Original joystick finger releases | 27.504 | Slot 0 ends, 5.68 seconds after the Resume press |
| Fresh joystick press | 27.817 | New contact 2156 starts in slot 0 |

The 70.03-second video shows Pause at seconds 36–39, then the preparation board with a centered joystick and unchanged guardian position at seconds 40–45. A fresh joystick press produces movement at second 46. This supports cancellation even when resuming before the original finger lifts. Video seconds and event-log seconds have different origins; the table uses the event-log origin and the frame references use the video origin.

The [contact-transition CSV](TouchEvidence/pixel10-pause-contacts.csv) is derived from tracking-ID changes committed at `SYN_REPORT`, retaining only slot 0/1 IDs and their latest raw coordinates. Positions use the touchscreen ranges described in the setup, not CSS pixels. It is an Android input trace, not Unity runtime telemetry. Game-only frames: [two-finger pause](TouchEvidence/pixel10-two-finger-pause.png), [neutral after resume](TouchEvidence/pixel10-resumed-neutral.png). The complete recordings remain local.

### Joystick ownership and construction repeat — October 10, 2026

The final capture recorded nine contact starts and a peak of **two simultaneous physical contacts**. Its video is 83.13 seconds long. The tester reported that all three requested cases worked.

- Contacts 2157 and 2158 pressed the joystick together. The owner 2157 released at 12.776 seconds; 2158 remained until 16.457 seconds and changed position while held. Video seconds 30–32 show a centered knob during the remaining contact. The guardian had reached the left board boundary, so its stationary position alone cannot prove zero movement input; the centered knob and the tester's report provide additional evidence. This pair corroborates M2, not M1's opposite release order.
- A first construction interaction opened slot 6 but released the owner before closing. A subsequent interaction used owner 2163, with contact 2164 opening slot 6 at 33.550 seconds and contact 2165 pressing Close at 34.996 seconds. Owner 2163 remained until 36.357 seconds, 1.28 seconds after the Close contact released. Video seconds 51–52 show the panel; seconds 53–54 show the closed panel, centered knob and unchanged guardian position. This second interaction corroborates M3.

Event times above are relative to the first contact in this final capture, not the pause repeat. See the separate [ownership/construction transition CSV](TouchEvidence/pixel10-ownership-contacts.csv). Game-only frames: [neutral after owner release](TouchEvidence/pixel10-owner-released.png), [neutral after closing construction](TouchEvidence/pixel10-construction-closed.png).

The verification supports physical two-contact input, owner-release cancellation, construction cancellation, and pause/resume cancellation on this phone and browser. M1 remains a tester-reported pass without an independently captured matching release sequence; F1's isolated finger sequence and broader usability across people/devices remain pending. No control defect was reported in this session and no control changes were made.

Record the maximum concurrent physical contacts from slot/tracking-ID events and correlate their timing with the game recording. Multiple contacts in an Android log prove physical input to the OS, not by themselves correct Unity pointer ownership. Screen observation and the tester's report are required for the behavior results.

Only the touchscreen node is observed; hardware keys are excluded. Recording is limited to the game test, contains no audio, and is kept locally for review. A recording can affect performance, so these cases are not frame-rate or thermal benchmarks.

The capture uses the [Android getevent tool](https://source.android.com/docs/core/interaction/input/getevent). In a separate terminal:

```sh
adb shell getevent -lt /dev/input/event2
```

The input node must be rediscovered on another device with `adb shell getevent -pl`; it is not a portable fixed path.

## Research context

[Added Mobile Controls](https://dev.to/weirdcodesofficial/added-mobile-controls-moksha-devlog-issue-27-1jef), by [weirdcodesofficial](https://dev.to/weirdcodesofficial), describes touch press/release controls while listing simultaneous multitouch separately as future work. [Emulators Are Not Enough](https://dev.to/alpha_lay_6947683a9151300/emulators-are-not-enough-a-practical-checklist-for-real-android-device-testing-4b43) recommends recording device identity and test evidence, and discloses its author's device-testing service affiliation. These posts are context, not proof that this game's physical cases pass.
