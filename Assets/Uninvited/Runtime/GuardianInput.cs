using UnityEngine;
using UnityEngine.EventSystems;

namespace Uninvited
{
    // One input system: Unity's built-in Input manager, also used by StandaloneInputModule.
    public sealed class GuardianInput : MonoBehaviour
    {
        GameSession game;
        GameWorldView world;
        GameHud hud;
        public Vector2 Movement { get; private set; }
        public void Initialize(GameSession session, GameWorldView view, GameHud ui) { game = session; world = view; hud = ui; }
        void Update()
        {
            Movement = Vector2.zero;
            if (game == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { if (game.Phase == GamePhase.Paused) game.Resume(); else game.Pause(); }
            if (game.Phase != GamePhase.Preparation && game.Phase != GamePhase.Wave) return;
            if (!hud.ModalOpen)
            {
                var x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
                var y = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
                Movement = Vector2.ClampMagnitude(new Vector2(x, y) + hud.Joystick.Direction, 1);
            }
            if (game.Phase != GamePhase.Preparation || hud.ModalOpen) return;
            if (Input.touchCount > 0)
            {
                for (var i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began && !EventSystem.current.IsPointerOverGameObject(touch.fingerId)) Select(touch.position);
                }
            }
            else if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) Select(Input.mousePosition);
        }
        void Select(Vector2 screenPoint)
        {
            if (!world.Camera.pixelRect.Contains(screenPoint)) return;
            var point = (Vector2)world.Camera.ScreenToWorldPoint(screenPoint);
            for (var i = 0; i < game.Sites.Length; i++)
                if ((point - game.Config.buildSites[i]).sqrMagnitude < .7f * .7f) { hud.OpenSite(i); return; }
        }
        public void Clear() { Movement = Vector2.zero; hud?.Joystick.Clear(); }
    }
}
