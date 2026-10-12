# itch.io release verification — October 12, 2026

## Artifact and scope

Unity **6000.3.5f2**, release Web/IL2CPP build. Archive: `Builds/Release/uninvited-web.zip`, **4,971,898 bytes** (4.74 MiB), SHA-256 **`097b8f0d669f6c72b1db0170de1be7c24f4499f2ca98835fc6bb1aa403585539`**. The archive contains a root `index.html` and four `Build/` files. Its manifest records every asset hash. The previous uncompressed build was 17,232,614 bytes; the gzip build payload is 4,971,320 bytes, approximately 71% smaller. The ZIP is kept outside Git as a generated artifact.

The user requested the ZIP and an English brief for manual deployment, and deferred human playtesting. No upload, project creation, public release or jam submission was performed. The exact ZIP was extracted into a local iframe preview. Actual itch.io CDN/iframe preview remains a manual post-upload check.

## Coverage of the five requested areas

| Area | Executed / result | Still pending |
| --- | --- | --- |
| Rendering after resize/focus loss | Reproduced blank canvas in an unfocused local iframe. Enabled the player update/render loop while retaining explicit simulation pause. The same resized iframe repainted and stayed paused; Pixel pause, rotation and background return remained usable. Short dialogs fit their content and use a lighter backdrop. | Other browsers/devices; long OS suspension/context-loss cases |
| Balance and session duration | 44 simulation assertions passed. The corrected Web package won all five waves on Pixel via visible UI/ADB inputs, with all defense kinds and upgrades, 9 reserves, 374 resources and 158 seconds of combat. Balance values and wave composition were unchanged. | Human strategies, first-time-player difficulty, and the 4–6 minute human session target |
| Mobile adaptation | Whole board and controls observed in Pixel portrait/landscape; actual fullscreen/bar restoration changes the viewport. Local desktop iframe tested at 320×568, 568×320, 375×667 and 540×960, plus simulated safe-area padding. | Other physical phones, iOS/Safari, scroll-driven browser-bar collapse and other actual cutouts |
| Controls | 102 Play Mode checks passed, including pointer ownership/cancellation. Release purchases, joystick placement, wave starts, upgrades, pause/resume and replay were exercised through the phone UI. Earlier human comfort/multitouch evidence remains in the separate touch report. | New-build thumb fine movement, independently captured second-finger-first release, physical mouse usability and wider human comfort testing |
| Performance | 296 metric samples over 300.36 seconds on Pixel, covering menu, preparation, five-wave combat and victory; memory/engine FPS and thermal endpoints recorded below. | Longer sessions, slower devices, internet/CDN loading, GPU/whole-browser memory and detailed CPU/GPU profiler analysis |

The deferred checks have concrete procedures in [ItchDeploymentBrief.md](ItchDeploymentBrief.md). They are not marked passed by simulation, automation or previous-build finger feedback.

## Rendering and adaptation

With `runInBackground` disabled, the original player stopped updating an unfocused iframe after its canvas resized, leaving it blank until another interaction. This was reproduced separately from the modal backdrop, which also darkened and covered most of the board. The corrected player keeps visual/layout updates alive; `GameBootstrap` continues to pause gameplay explicitly on focus/visibility loss. Returning focus never resumes the match automatically. Hidden tabs can still be throttled or suspended by the browser.

A local sandboxed iframe used the ZIP's extracted files and relative asset URLs. After the correction, all four viewport sizes repainted without another focus-restoring click. In portrait the six sites, base, Pause, joystick and Start wave stayed visible. At 568×320 the complete board became small, as expected; the real touch phone separately showed the rotate suggestion. Simulated padding was 24 px top, 16 px sides and 34 px bottom; the 540×960 frame produced a 508×902 canvas offset by 16/24 px. This is an inset simulation, not another physical-device result.

Pixel 10, Android 16, Chrome 154.0.8037.126, device display 1080×2424. Desktop verification used an isolated **headless Chrome 155.0.8059.39** on the development Mac, not a physical mouse session or Safari. An early screenshot attempt clipped content outside the headless window; final captures included the full requested area. The local iframe is not the deployed itch.io iframe.

While paused, Pixel fullscreen changed the CSS canvas from approximately **411×778** to **411×923**; rotating fullscreen produced render dimensions **1847×823**. Exiting fullscreen restored **823×1555** render pixels / approximately **411×778** CSS pixels. The pause remained explicit, and the board returned after Resume. This exercised hiding/restoring Chrome bars via fullscreen; scroll-driven bar collapse was not tested. Display rotation used ADB, and automatic rotation was restored afterward.

Evidence: [320×568 preparation](ReleaseEvidence/iframe-320x568.png), [unfocused iframe repainted while paused](ReleaseEvidence/iframe-paused-resize.png), [Pixel pause with recognizable board](ReleaseEvidence/pixel10-paused.png).

## Full-match package run

All inputs used ADB taps, visible buttons recognized in screenshots, and the on-screen joystick. No game debug methods, altered statistics, accelerated time or automatic victory calls were used. The guardian was placed near the central bend during preparation.

| Before wave | Actions | Observed preparation state before purchases |
| --- | --- | --- |
| 1 | Pumpkins at 3/4; move guardian | 20 reserves / 100 resources |
| 2 | Speaker at 5 | 20 / 96 |
| 3 | Web at 1; upgrade Pumpkin 3; Pumpkin at 6 | 20 / 165 |
| 4 | Upgrade Pumpkins 4/6 | 9 / 121 |
| 5 | Upgrade Web 1 and Speaker 5; build/upgrade Pumpkin 2 | 9 / 263 |

**Victory: 5/5 waves, 9 reserves, 374 resources, 158 seconds of combat.** The automated interval from Open the party to victory was approximately 208.27 seconds, including UI operations and polling/preparation delays. That is not a human session measurement or validation of the 4–6 minute target. The initial OCR tool warm-up before Open the party is excluded from that interval.

[Action trace](ReleaseEvidence/pixel10-match-actions.json), [victory frame](ReleaseEvidence/pixel10-victory.png). Play again subsequently restored preparation with 20 reserves, 100 resources, six empty sites and the guardian at its starting location.

## Five-minute Pixel measurement

`Tools/profile-web.mjs` sampled the actual player's exposed `GetMetricsInfo()` through a local Chrome DevTools USB forwarding connection. It injected no gameplay inputs. Sampling, ADB UI operations and occasional screenshots add measurement overhead; there was no continuous screen recording during the run. All 296 samples reported a visible page. The trace includes preparation/menu time as well as all five combat waves; it is not five uninterrupted minutes of combat.

| Measurement | Observed value |
| --- | --- |
| Duration / samples | 300.36 seconds / 296 samples at roughly 1-second intervals |
| Unity-reported instantaneous FPS, sampled minimum / median / maximum | 59.23 / 60.03 / 60.16 |
| Reserved WASM heap | 67,108,864 bytes (64 MiB), unchanged across the trace |
| Used WASM heap minimum / maximum | 26,811,264 / 27,777,272 bytes (25.57–26.49 MiB) |
| Reported JS heap used | Approximately 10,000,000 bytes; coarse browser estimate |
| Unity-reported jank counter | 4 at first sample, 40 at last; increase of 36 |
| Player-reported page load to first frame | 612 ms in this local USB session |
| Current HAL battery temperature, before / after | 31.7°C / 32.1°C |
| Current HAL virtual skin sensor, before / after | 32.47°C / 32.89°C |
| Framework thermal status, before / after | 0 / 0 |

[Raw metric samples](ReleaseEvidence/pixel10-metrics.jsonl), [thermal readings](ReleaseEvidence/pixel10-thermal.json). Current HAL readings were used rather than stale cached sensor entries. Battery service reported charging/AC power, USB power false and level 80; the device was connected for USB debugging. These are two thermal endpoint readings, not a continuous peak-temperature or throttling trace.

FPS values are sampled engine estimates, not a histogram of every frame. They do not establish the absence of transient frame stalls; the jank counter is preserved above. WASM/JS heap figures exclude GPU allocations and the full Chrome process footprint. Local loading/cache conditions do not predict internet/CDN loading. A five-minute, charging-device result cannot certify longer sessions or lower-end hardware.

## Build and archive checks

- Independent runtime C# compilation passed. Unity compiled editor/runtime scripts, imported the scene and passed 102 scene/pointer checks; the full gameplay simulation passed 44 assertions.
- Explicit empty strings fixed Unity's `TagManager.asset` parse failure for vacant layers. Subsequent verification/build logs no longer reported that parser failure.
- The editor-only Unity Search `IndexationOnStartup` exception remains separately recorded; it was not counted as a gameplay pass or hidden. No gameplay exception was observed in the executed scene checks.
- The build uses gzip without JavaScript decompression fallback. The local server supplied `Content-Encoding: gzip` and `application/wasm` for the compressed WASM; the player loaded in Android and desktop Chrome.
- ZIP integrity, root entry, required asset references, exact case, relative paths, file sizes/count and per-file hashes were checked. Failure cases rejected missing URLs/assets, wrong reference case, and placing the archive inside its source directory.

## Reference context

[itch.io's HTML5 documentation](https://itch.io/docs/creators/html5) confirms ZIP/relative-path requirements, responsive embeds, mobile fullscreen behavior and gzip header detection. [Unity's runInBackground reference](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-runInBackground.html) describes continued updates on focus loss; this game retains its own explicit pause rule.

[Mobile Game Optimization: A Unity Developer's Checklist](https://dev.to/oceanviewgames/mobile-game-optimization-a-unity-developers-checklist-dnk), by [Ocean View Games](https://dev.to/oceanviewgames), emphasizes measurement on real hardware before tuning. [How to Publish a Unity Game on Itch.io](https://dev.to/abdulrahman_maslmany/how-to-publish-a-unity-game-on-itchio-webgl-zip-html5-setup-step-by-step-30m9) discusses packaging but makes blanket decompression-fallback recommendations; itch.io's own gzip-header support was used to choose this configuration. These two posts are context, not a compatibility guarantee. The packaging post had no comments; comments for the optimization post could not be retrieved, so no comment consensus is claimed.
