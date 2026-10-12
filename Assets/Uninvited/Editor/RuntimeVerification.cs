using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Uninvited.Editor
{
    // Runs the actual generated scene and its UI callbacks. No simulated Input API.
    public static class RuntimeVerification
    {
        static int stage, frames, checks;
        static double started;
        static bool failed;
        static bool previousOptionsEnabled;
        static EnterPlayModeOptions previousOptions;
        static int searchStartupErrors;
        static GameBootstrap bootstrap;
        static GameHud hud;
        static Vector2 pausedPosition;
        static float pausedSeconds;
        public static void Run()
        {
            stage = frames = checks = 0;
            failed = false;
            searchStartupErrors = 0;
            started = EditorApplication.timeSinceStartup;
            previousOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            previousOptions = EditorSettings.enterPlayModeOptions;
            // Preserve the verification callback across entering/leaving play mode.
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Tick;
            EditorSceneManager.OpenScene(ProjectSetup.ScenePath);
            EditorApplication.EnterPlaymode();
        }
        static void Check(bool condition, string label)
        {
            checks++;
            if (!condition) throw new InvalidOperationException("Runtime check failed: " + label);
        }
        static void Click(string name)
        {
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
                if (button.name == name && button.gameObject.activeInHierarchy)
                { Check(button.interactable, "button enabled: " + name); button.onClick.Invoke(); return; }
            throw new InvalidOperationException("Active button missing: " + name);
        }
        static void OnLog(string condition, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            // Unity 6000.3.5f2 reports this editor-only indexing exception on startup
            // in this project. Record it separately; never discard gameplay errors.
            if (condition.StartsWith("ArgumentOutOfRangeException") && stack.Contains("UnityEditor.Search.SearchInit.IndexationOnStartup"))
            { searchStartupErrors++; return; }
            failed = true;
        }
        static PointerEventData Pointer(int id, Vector2 position) => new PointerEventData(EventSystem.current) { pointerId = id, position = position, button = PointerEventData.InputButton.Left };
        static Vector2 StickPoint(Vector2 offset)
        {
            var rect = (RectTransform)hud.Joystick.transform;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center + offset * rect.rect.width * .33f));
        }
        static void VerifyPointerControls()
        {
            Canvas.ForceUpdateCanvases();
            var controls = bootstrap.GetComponent<GuardianInput>();
            var joystick = hud.Joystick;
            var canvas = hud.GetComponent<Canvas>();
            var originalScale = canvas.scaleFactor;
            foreach (var scale in new[] { .75f, 1f, 1.6f })
            {
                canvas.scaleFactor = scale;
                Canvas.ForceUpdateCanvases();
                var centered = Pointer(11, StickPoint(Vector2.zero));
                joystick.OnPointerDown(centered);
                Check(joystick.Direction.sqrMagnitude < .001f && joystick.Knob.anchoredPosition.sqrMagnitude < .001f, "pressing visible joystick center is neutral at scale " + scale);
                foreach (var direction in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down })
                {
                    centered.position = StickPoint(direction);
                    joystick.OnDrag(centered);
                    Check((joystick.Direction - direction).sqrMagnitude < .001f, "visible joystick cardinal direction " + direction + " at scale " + scale);
                }
                joystick.OnPointerUp(centered);
                var owner = Pointer(11, StickPoint(new Vector2(.5f, 0)));
                joystick.OnPointerDown(owner);
                Check((joystick.Direction - new Vector2(.5f, 0)).sqrMagnitude < .001f && !owner.useDragThreshold, "joystick uses local coordinates and starts without drag threshold at scale " + scale);
                var other = Pointer(12, StickPoint(Vector2.left));
                joystick.OnPointerDown(other); joystick.OnDrag(other); joystick.OnPointerUp(other);
                Check((joystick.Direction - new Vector2(.5f, 0)).sqrMagnitude < .001f, "second pointer cannot steal, drag or release joystick");
                owner.position = StickPoint(Vector2.up * 4);
                joystick.OnDrag(owner);
                Check((joystick.Direction - Vector2.up).sqrMagnitude < .001f, "joystick direction remains normalized outside its radius");
                controls.TrySelect(StickPoint(Vector2.zero));
                Check(!hud.ModalOpen && hud.BlocksBoardInput(StickPoint(Vector2.zero)), "current press on joystick is blocked from board without cached pointer state");
                joystick.OnPointerUp(owner);
                Check(joystick.Direction == Vector2.zero && joystick.Knob.anchoredPosition == Vector2.zero, "owning pointer release recenters joystick");
            }
            canvas.scaleFactor = originalScale;
            Canvas.ForceUpdateCanvases();
            var start = Array.Find(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None), button => button.name == "Start wave");
            var startPoint = RectTransformUtility.WorldToScreenPoint(null, start.transform.position);
            controls.TrySelect(startPoint);
            Check(hud.BlocksBoardInput(startPoint) && !hud.ModalOpen, "start button press cannot select board");
            var world = UnityEngine.Object.FindFirstObjectByType<GameWorldView>();
            var sitePoint = world.Camera.WorldToScreenPoint(bootstrap.Session.Config.buildSites[2]);
            Check(!hud.BlocksBoardInput(sitePoint), "construction slot is not blocked by controls");
            joystick.OnPointerDown(Pointer(11, StickPoint(Vector2.right)));
            controls.TrySelect(sitePoint);
            Check(hud.ModalOpen && joystick.Direction == Vector2.zero, "board selection opens modal and cancels held movement");
            controls.TrySelect(world.Camera.WorldToScreenPoint(bootstrap.Session.Config.buildSites[3]));
            Check(GameObject.Find("Dialog").GetComponentInChildren<Text>().text.StartsWith("Slot 3"), "second board press cannot replace open modal");
            hud.CloseDialog();
            joystick.OnDrag(Pointer(11, StickPoint(Vector2.right)));
            Check(joystick.Direction == Vector2.zero, "cleared pointer cannot resume movement without a new press");
            joystick.OnPointerDown(Pointer(13, StickPoint(Vector2.right)));
            joystick.enabled = false;
            Check(joystick.Direction == Vector2.zero && joystick.Knob.anchoredPosition == Vector2.zero, "disabled joystick releases its pointer");
            joystick.enabled = true;
        }
        static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - started > 90) throw new TimeoutException("Runtime verification timed out.");
                if (stage == 9)
                {
                    if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
                    Check(Time.timeScale == 1, "time restored after exiting play mode");
                    Finish(); return;
                }
                if (!EditorApplication.isPlaying) return;
                if (failed) throw new InvalidOperationException("Scene logged an error; inspect the Unity log.");
                frames++;
                if (frames < 5) return;
                frames = 0;
                switch (stage)
                {
                    case 0:
                        var roots = UnityEngine.Object.FindObjectsByType<GameBootstrap>(FindObjectsSortMode.None);
                        Check(roots.Length == 1, "exactly one game root");
                        bootstrap = roots[0];
                        hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
                        Check(bootstrap.Session != null && bootstrap.Session.Phase == GamePhase.Menu, "scene starts in menu");
                        Check(hud != null && hud.ModalOpen, "menu is visible");
                        Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "one event system");
                        Check(Resources.Load<Material>("UninvitedPlaceholder")?.shader.name == "Sprites/Default", "visible sprite shader loads rather than stencil mask shader");
                        foreach (var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                            Check(renderer.transform.lossyScale.z > 0 && renderer.sprite.vertices.Length >= 3 && renderer.sharedMaterial.shader.name == "Sprites/Default", "sprite has drawable geometry and non-singular transform: " + renderer.name);
                        Click("Open the party");
                        break;
                    case 1:
                        Check(bootstrap.Session.Phase == GamePhase.Preparation && !hud.ModalOpen, "UI enters preparation");
                        VerifyPointerControls();
                        hud.OpenSite(2); Click("Choose Pumpkin"); Click("Confirm purchase · 50");
                        Check(bootstrap.Session.Economy.Money == 50 && bootstrap.Session.Sites[2] != null && !hud.ModalOpen, "purchase UI debits and closes");
                        hud.OpenSite(3); Click("Choose Pumpkin"); Click("Confirm purchase · 50");
                        Check(bootstrap.Session.Economy.Money == 0, "second purchase UI");
                        Click("Start wave");
                        break;
                    case 2:
                        Check(bootstrap.Session.Phase == GamePhase.Wave && bootstrap.Session.Enemies.Count > 0, "actual scene spawns enemies");
                        hud.Joystick.OnPointerDown(Pointer(14, StickPoint(Vector2.right)));
                        Check(hud.Joystick.Direction.x > .9f, "joystick held before pause");
                        Click("Pause");
                        Check(hud.Joystick.Direction == Vector2.zero, "pause cancels held joystick");
                        pausedPosition = bootstrap.Session.GuardianPosition;
                        pausedSeconds = bootstrap.Session.CombatSeconds;
                        break;
                    case 3:
                        Check(bootstrap.Session.Phase == GamePhase.Paused && Time.timeScale == 0 && bootstrap.Session.GuardianPosition == pausedPosition && bootstrap.Session.CombatSeconds == pausedSeconds, "pause freezes scene");
                        Check(hud.ModalOpen, "pause menu visible");
                        Click("Resume");
                        break;
                    case 4:
                        Check(bootstrap.Session.Phase == GamePhase.Wave && Time.timeScale == 1 && bootstrap.Session.CombatSeconds > pausedSeconds, "resume advances scene");
                        bootstrap.HandleBrowserFocusLost();
                        break;
                    case 5:
                        Check(bootstrap.Session.Phase == GamePhase.Paused && hud.ModalOpen, "browser focus-loss handler pauses explicitly");
                        Click("Restart game");
                        break;
                    case 6:
                        Check(bootstrap.Session.Phase == GamePhase.Preparation && bootstrap.Session.Enemies.Count == 0 && bootstrap.Session.Economy.Money == 100 && bootstrap.Session.Sites[2] == null && Time.timeScale == 1, "UI restart cleans session and time");
                        Check(UnityEngine.Object.FindObjectsByType<GameBootstrap>(FindObjectsSortMode.None).Length == 1 && UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "restart has no duplicate roots or event systems");
                        Check(GameObject.Find("Enemy visuals").transform.childCount == 0 && GameObject.Find("Transient effects").transform.childCount == 0, "restart removes enemy and effect visuals");
                        bootstrap.Session.StartWave();
                        break;
                    case 7:
                        bootstrap.Session.Pause();
                        break;
                    case 8:
                        Check(Time.timeScale == 0, "paused before leaving play mode");
                        EditorApplication.ExitPlaymode();
                        break;
                }
                stage++;
            }
            catch (Exception exception)
            {
                EditorApplication.update -= Tick;
                Application.logMessageReceived -= OnLog;
                RestoreOptions();
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
        static void Finish()
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            RestoreOptions();
            File.WriteAllText("Documentation/RuntimeVerification.md", $"# Unity runtime verification\n\nUnity {Application.unityVersion}, batch play mode. {checks} checks passed.\n\nActual scene initialization, material/shader loading, English menu and purchase buttons, spawning, pause/resume, browser focus-loss handler, restart cleanup and time restoration were exercised. No gameplay errors were logged.\n\nAdditional pointer-event checks covered the visible joystick center and all four cardinal directions with the HUD's bottom-left pivot at canvas scales 0.75, 1 and 1.6; second-pointer ownership; normalized drag; release, disable, modal and pause cancellation; and UI raycasts at the current press position without relying on cached pointer state. These events were constructed in Play Mode, not sent by a real touchscreen.\n\nEditor-only SearchInit.IndexationOnStartup exceptions recorded separately: {searchStartupErrors}. This Unity Search indexing exception is a known limitation of this verification environment, not counted as a passing scene check.\n\nSee [BrowserVerification.md](BrowserVerification.md) for separate desktop, emulation and Pixel 10 hardware checks, and [TouchVerification.md](TouchVerification.md) for subsequent human finger and physical two-contact results. Constructed pointer events do not replace these physical checks; uncorroborated gesture sequences and other mobile devices remain pending as specified in the touch report.\n");
            Debug.Log("UNINVITED: " + checks + " runtime scene checks passed.");
            EditorApplication.Exit(0);
        }
        static void RestoreOptions()
        {
            EditorSettings.enterPlayModeOptionsEnabled = previousOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousOptions;
        }
    }
}
