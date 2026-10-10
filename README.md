# Uninvited

A 2D, five-wave defense game for GameDevTV Halloween Jam 2026.
Built with **Unity 6000.3.5f2**, geometric placeholders, keyboard/mouse input, and a touch joystick.

`master` is the production branch. Changes are developed on separate branches and reviewed through pull requests targeting `master`.

Open `Assets/Uninvited/Scenes/Uninvited.unity` in Unity and press Play.
**Uninvited → Generate game scene** can regenerate the scene; **Uninvited → Build Web** exports to `Builds/Web`.
Serve the output over HTTP:

```sh
python3 -m http.server 8080 --directory Builds/Web
```

Open the party, build defenses during preparation, and start each wave when ready. Move with WASD, arrow keys, or the joystick. Attacks are automatic. Save the party by surviving all five waves with candy reserves remaining.

[Implementation, balancing, export, and verification guide](Documentation/Implementation.md).

The game, Web loading page, and delivery documentation are in English.
Simulation and Play Mode checks are documented separately from browser checks. Chrome on a connected Pixel 10 has been tested using ADB touch events; simultaneous physical multitouch and other phones remain unverified. See the [browser verification report](Documentation/BrowserVerification.md).
