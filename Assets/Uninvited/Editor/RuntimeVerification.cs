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
                        Click("Abrir la fiesta");
                        break;
                    case 1:
                        Check(bootstrap.Session.Phase == GamePhase.Preparation && !hud.ModalOpen, "UI enters preparation");
                        hud.OpenSite(2); Click("Elegir Calabaza"); Click("Confirmar compra · 50");
                        Check(bootstrap.Session.Economy.Money == 50 && bootstrap.Session.Sites[2] != null && !hud.ModalOpen, "purchase UI debits and closes");
                        hud.OpenSite(3); Click("Elegir Calabaza"); Click("Confirmar compra · 50");
                        Check(bootstrap.Session.Economy.Money == 0, "second purchase UI");
                        Click("Iniciar oleada");
                        break;
                    case 2:
                        Check(bootstrap.Session.Phase == GamePhase.Wave && bootstrap.Session.Enemies.Count > 0, "actual scene spawns enemies");
                        Click("Pausa");
                        pausedPosition = bootstrap.Session.GuardianPosition;
                        pausedSeconds = bootstrap.Session.CombatSeconds;
                        break;
                    case 3:
                        Check(bootstrap.Session.Phase == GamePhase.Paused && Time.timeScale == 0 && bootstrap.Session.GuardianPosition == pausedPosition && bootstrap.Session.CombatSeconds == pausedSeconds, "pause freezes scene");
                        Check(hud.ModalOpen, "pause menu visible");
                        Click("Reanudar");
                        break;
                    case 4:
                        Check(bootstrap.Session.Phase == GamePhase.Wave && Time.timeScale == 1 && bootstrap.Session.CombatSeconds > pausedSeconds, "resume advances scene");
                        bootstrap.HandleBrowserFocusLost();
                        break;
                    case 5:
                        Check(bootstrap.Session.Phase == GamePhase.Paused && hud.ModalOpen, "browser focus-loss handler pauses explicitly");
                        Click("Reiniciar partida");
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
            File.WriteAllText("Documentation/RuntimeVerification.md", $"# Unity runtime verification\n\nUnity {Application.unityVersion}, batch play mode. {checks} checks passed.\n\nActual scene initialization, material/shader loading, menu, purchase buttons, spawning, pause/resume, browser focus-loss handler, restart cleanup and time restoration were exercised. No gameplay errors were logged.\n\nEditor-only SearchInit.IndexationOnStartup exceptions recorded separately: {searchStartupErrors}. This Unity Search indexing exception is a known limitation of this verification environment, not counted as a passing scene check.\n\nPhysical keyboard/mouse, multitouch and real browser/device compatibility require separate checks.\n");
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
