using UnityEngine;

namespace Uninvited
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        public GameSession Session { get; private set; }
        GameWorldView world;
        GuardianInput controls;
        public void Configure(GameConfig balance) => config = balance;
        void Awake()
        {
            Time.timeScale = 1;
            Input.simulateMouseWithTouches = false;
            Application.targetFrameRate = 60;
            if (config == null)
            {
                Debug.LogError("Missing GameConfig. Run Uninvited > Generate game scene.");
                enabled = false;
                return;
            }
            Session = new GameSession(config);
            var worldObject = new GameObject("World view");
            worldObject.transform.SetParent(transform, false);
            world = worldObject.AddComponent<GameWorldView>();
            world.Initialize(Session);
            world.ShowRange = PlayerPrefs.GetInt("Uninvited.ShowRange", 1) == 1;
            var uiObject = new GameObject("Adaptive HUD", typeof(RectTransform));
            uiObject.transform.SetParent(transform, false);
            var hud = uiObject.AddComponent<GameHud>();
            hud.Initialize(Session, world);
            controls = gameObject.AddComponent<GuardianInput>();
            controls.Initialize(Session, world, hud);
            Session.Changed += OnStateChanged;
        }
        void LateUpdate()
        {
            if (Session == null) return;
            Session.Tick(Mathf.Min(Time.unscaledDeltaTime, .1f), controls.Movement);
            var active = Session.Phase == GamePhase.Preparation || Session.Phase == GamePhase.Wave;
            world.Render(active ? Mathf.Min(Time.unscaledDeltaTime, .1f) : 0);
        }
        void OnStateChanged()
        {
            Time.timeScale = Session.Phase == GamePhase.Paused ? 0 : 1;
            if (Session.Phase == GamePhase.Paused) controls.Clear();
            if (Session.Phase != GamePhase.Victory && Session.Phase != GamePhase.Defeat) return;
            var completed = Session.Phase == GamePhase.Victory ? Session.WaveNumber : Mathf.Max(0, Session.WaveNumber - 1);
            var bestWave = PlayerPrefs.GetInt("Uninvited.BestWave", 0);
            var bestReserves = PlayerPrefs.GetInt("Uninvited.BestReserves", 0);
            if (completed > bestWave || (completed == bestWave && Session.Economy.Reserves > bestReserves))
            {
                PlayerPrefs.SetInt("Uninvited.BestWave", completed);
                PlayerPrefs.SetInt("Uninvited.BestReserves", Session.Economy.Reserves);
                PlayerPrefs.Save();
            }
        }
        void OnApplicationFocus(bool focused) { if (!focused) { controls?.Clear(); Session?.Pause(); } }
        void OnApplicationPause(bool paused) { if (paused) { controls?.Clear(); Session?.Pause(); } }
        public void HandleBrowserFocusLost() { controls?.Clear(); Session?.Pause(); }
        void OnDestroy() { if (Session != null) Session.Changed -= OnStateChanged; Time.timeScale = 1; }
    }
}
