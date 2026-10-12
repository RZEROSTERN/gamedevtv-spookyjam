# Uninvited — manual itch.io deployment brief

## Delivery

After the release PR is reviewed and merged into `master`, upload `Builds/Release/uninvited-web.zip`. Its companion `uninvited-web.manifest.json` records the archive and individual-file SHA-256 hashes. Do not upload the repository, source project, baseline build, or local preview harness.

The ZIP has `index.html` at its root and uses relative, case-sensitive `Build/` paths. Unity 6000.3.5f2 exports gzip assets; itch.io detects gzip and supplies the encoding header. No backend, external fonts, downloaded game assets or credentials are required.

## Project settings on itch.io

1. Create or edit the project. Title: **Uninvited**. Classification: **Games**. Kind of project: **HTML**.
2. Upload the ZIP and mark **This file will be played in the browser**.
3. Use **Click to launch in fullscreen** and retain Click to Play. The canvas adapts to the available viewport. If choosing an in-page embed instead, use 540 × 960 with the fullscreen button enabled and scrollbars disabled.
4. Enable **Mobile Friendly**. Verify the uploaded preview on the Pixel before making the page public; this label does not establish iOS compatibility. On mobile, itch.io launches the game fullscreen regardless of desktop embed settings.
5. Start in **Draft** while verifying the uploaded package. Launch the actual itch preview, open the party, buy a Pumpkin, start a wave, pause/resume, rotate on mobile and restart. Check for missing assets or a persistent loading error. This checks itch's CDN/iframe; a local preview is not a substitute.
6. After review and the desired human checks below, switch the page to **Public**. Uploading a game is separate from entering it into a jam; follow the event's submission process separately.

Source: [itch.io HTML5 creator documentation](https://itch.io/docs/creators/html5), checked October 12, 2026.

## Store page copy

**Short description**

Protect a private Halloween party from five waves of gatecrashers. Build defenses, move your guardian, and keep the candy reserves safe.

**Description**

A private party. A growing line of uninvited guests. You guard the entrance.

Build Pumpkins to target individual gatecrashers, Webs to slow them down, and Speakers to deal area damage. Between waves, spend your resources on new defenses or a single upgrade per defense. During combat, move your guardian to support the busiest part of the path; attacks are automatic.

Survive all five waves with candy reserves remaining to save the party. If the reserves run out, restart and try a different strategy.

Created with Unity 6 for GameDevTV Spooky Game Jam 2026. This prototype uses geometric Halloween placeholders and an English interface.

**Controls**

- Desktop: WASD or arrow keys to move; click slots and buttons. The joystick also supports mouse dragging.
- Mobile: drag the joystick; tap a numbered slot to build or upgrade. Portrait is recommended for a larger board.
- Build and upgrade during preparation. Start each wave when ready.
- Pause freezes the match. Returning from another tab/window requires explicit Resume.

**Compatibility note**

Tested in Chrome on Pixel 10 and an automated desktop Chrome environment. Other phones and iOS/Safari remain unverified; desktop automation is not a physical mouse usability test. See the repository's release verification report for exact coverage.

## Human checks prepared for later

These remain pending because the tester was unavailable for this release session. Earlier finger comfort and physical multitouch results are documented separately; they are not a new finger test of this build.

| Check | Procedure | Record / acceptance |
| --- | --- | --- |
| Session duration and balance | Start a stopwatch at Open the party; play all five waves with your own strategy, including preparation. Record deliberate pauses separately. Repeat with a different strategy or a first-time player. | Outcome, reserves, strategies, total elapsed time, pause time, preparation time and difficulty. Target: 4–6 minutes of play including preparation; change balance only if these observations justify it. |
| Fine joystick movement | In preparation, use a thumb to move left/right/up/down, then make small movements around the center and release. | Direction follows the finger, no unwanted drift, release recenters/stops, comfortable reach. |
| Release the second finger first | Hold the joystick with one finger; add and move another on it, then release only the second while the owner stays down. | Original owner retains control. Capture matching physical contacts if independent evidence is required. |
| Browser bars and safe areas | On a real mobile browser, expand/collapse browser chrome when possible, rotate and return from the background while paused. | All controls stay usable, no stuck motion, pause requires explicit Resume; note device/browser and any clipping. |
| Other devices/browsers and mouse | Try a physical mouse and another Android phone; use a real iPhone/iPad for Safari checks. Complete a wave, purchase, pause and restart. | Record actual device/OS/browser versions and results. Responsive emulation does not count as testing those devices. |

## Rebuild and local preview

After a reviewed change is merged, build with **Uninvited → Build Web** (which runs gameplay verification), then:

```sh
python3 Tools/package-web.py
python3 Tools/serve-web.py --port 8082
```

Open `http://127.0.0.1:8082/`. A generic static server without `Content-Encoding: gzip` is unsuitable for this compressed build. To access it on the connected Android device:

```sh
adb reverse tcp:8082 tcp:8082
```

For repeatable optional metric sampling, Node 22+ can read Unity's built-in Web metrics without injecting input:

```sh
adb forward tcp:9222 localabstract:chrome_devtools_remote
node Tools/profile-web.mjs --seconds 300
```

A build can restart ADB; restore the forwarding afterward. Record charging state and thermal conditions, and distinguish local USB loading from internet/CDN loading. See [ReleaseVerification.md](ReleaseVerification.md) for executed checks and limits.
