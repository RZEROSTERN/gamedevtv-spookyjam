# Uninvited

A 2D, five-wave defense game for GameDevTV Spooky Game Jam 2026.
Built with **Unity 6000.3.5f2**, geometric placeholders, keyboard/mouse input, and a touch joystick.

`master` is the production branch. Changes are developed on separate branches and reviewed through pull requests targeting `master`.

Open `Assets/Uninvited/Scenes/Uninvited.unity` in Unity and press Play.
**Uninvited → Generate game scene** can regenerate the scene; **Uninvited → Build Web** exports to `Builds/Web`.
Serve the output over HTTP:

```sh
python3 Tools/serve-web.py --port 8082
```

Open the party, build defenses during preparation, and start each wave when ready. Move with WASD, arrow keys, or the joystick. Attacks are automatic. Save the party by surviving all five waves with candy reserves remaining.

[Implementation, balancing, export, and verification guide](Documentation/Implementation.md).

[Manual itch.io deployment brief](Documentation/ItchDeploymentBrief.md) and [release verification](Documentation/ReleaseVerification.md). Create the upload ZIP with `python3 Tools/package-web.py`; output is `Builds/Release/uninvited-web.zip`. The Web build uses gzip and requires the matching HTTP encoding headers; the preview server above supplies them.

The game, Web loading page, and delivery documentation are in English.
Simulation and Play Mode checks are documented separately from browser checks. Chrome on a connected Pixel 10 has been tested using ADB touch events and a subsequent human finger session. The tester reported comfortable controls; physical two-contact input and cancellation cases were corroborated by touchscreen traces and video. See the [browser verification report](Documentation/BrowserVerification.md) and the [finger/multitouch report](Documentation/TouchVerification.md) for exact coverage and remaining checks. Other phones remain unverified.
