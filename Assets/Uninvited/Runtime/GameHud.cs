using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Uninvited
{
    public sealed class GameHud : MonoBehaviour
    {
        public VirtualJoystick Joystick { get; private set; }
        public bool ModalOpen => overlay != null && overlay.activeSelf;
        GameSession game;
        GameWorldView world;
        Canvas canvas;
        CanvasScaler scaler;
        GraphicRaycaster raycaster;
        PointerEventData pointerData;
        readonly List<RaycastResult> uiHits = new List<RaycastResult>(16);
        readonly List<DialogRow> rows = new List<DialogRow>(16);
        struct DialogRow { public RectTransform Rect; public Text Text; public float MinimumHeight; }
        RectTransform safe, header, footer, dialog, content;
        GameObject overlay;
        Text stats, status, instructions, dialogTitle;
        Button pause, start;
        Font font;
        int selected = -1;
        GamePhase shownPhase = (GamePhase)(-1);
        int lastWidth, lastHeight;
        Rect lastSafe;
        float contentY;
        static readonly Color Panel = new Color(.09f, .12f, .18f);
        static readonly Color ButtonColor = new Color(.22f, .29f, .38f);

        public void Initialize(GameSession session, GameWorldView view)
        {
            game = session;
            world = view;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
            var eventObject = new GameObject("UI EventSystem");
            eventObject.transform.SetParent(transform, false);
            pointerData = new PointerEventData(eventObject.AddComponent<EventSystem>());
            eventObject.AddComponent<StandaloneInputModule>();
            safe = Rect("Safe area", transform);
            Stretch(safe);
            header = Rect("Header", safe);
            header.anchorMin = new Vector2(0, 1); header.anchorMax = Vector2.one;
            header.pivot = new Vector2(.5f, 1); header.sizeDelta = new Vector2(0, 96);
            Image(header, Panel);
            stats = Label(header, "", 18, GameWorldView.Cream, TextAnchor.MiddleLeft);
            Place((RectTransform)stats.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -10), new Vector2(350, 32));
            status = Label(header, "", 16, Color.white, TextAnchor.MiddleLeft);
            Place((RectTransform)status.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -45), new Vector2(340, 36));
            pause = MakeButton(header, "Pause", () => game.Pause());
            Place((RectTransform)pause.transform, Vector2.one, Vector2.one, new Vector2(-12, -12), new Vector2(116, 64));
            footer = Rect("Controls", safe);
            footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1, 0);
            footer.pivot = new Vector2(.5f, 0); footer.sizeDelta = new Vector2(0, 180);
            Image(footer, Panel);
            var stick = Rect("Virtual joystick", footer);
            Place(stick, Vector2.zero, Vector2.zero, new Vector2(18, 38), new Vector2(122, 122));
            var stickImage = Image(stick, new Color(.2f, .32f, .43f));
            stickImage.sprite = world.Art.Sprite(Shape.Disc);
            Joystick = stick.gameObject.AddComponent<VirtualJoystick>();
            var knob = Rect("Knob", stick);
            Place(knob, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(46, 46));
            var knobImage = Image(knob, GameWorldView.Blue);
            knobImage.sprite = world.Art.Sprite(Shape.Disc); knobImage.raycastTarget = false;
            Joystick.Knob = knob;
            start = MakeButton(footer, "Start wave", () => { CloseDialog(); game.StartWave(); });
            Place((RectTransform)start.transform, Vector2.one, Vector2.one, new Vector2(-16, -20), new Vector2(280, 64));
            instructions = Label(footer, "", 17, Color.white, TextAnchor.MiddleLeft);
            var hintRect = (RectTransform)instructions.transform;
            hintRect.anchorMin = new Vector2(0, 0); hintRect.anchorMax = new Vector2(1, 0);
            hintRect.pivot = new Vector2(0, 0); hintRect.offsetMin = new Vector2(160, 44); hintRect.offsetMax = new Vector2(-14, 90);
            var caption = Label(footer, "MOVE", 14, GameWorldView.Cream, TextAnchor.MiddleCenter);
            Place((RectTransform)caption.transform, Vector2.zero, Vector2.zero, new Vector2(18, 6), new Vector2(122, 24));
            CreateDialog();
            game.Changed += Refresh;
            UpdateLayout(true);
            Refresh();
        }

        public bool BlocksBoardInput(Vector2 screenPoint)
        {
            pointerData.Reset();
            pointerData.position = screenPoint;
            uiHits.Clear();
            raycaster.Raycast(pointerData, uiHits);
            return uiHits.Count > 0;
        }

        RectTransform Rect(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return (RectTransform)obj.transform;
        }
        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; }
        static Image Image(RectTransform rect, Color color) { var image = rect.gameObject.AddComponent<Image>(); image.color = color; return image; }
        Text Label(Transform parent, string value, int size, Color color, TextAnchor align)
        {
            var rect = Rect("Text", parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.color = color; text.text = value;
            text.alignment = align; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            Stretch(rect);
            return text;
        }
        Button MakeButton(Transform parent, string caption, Action action)
        {
            var rect = Rect(caption, parent);
            Image(rect, ButtonColor);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(.8f, .9f, 1);
            colors.pressedColor = GameWorldView.Mint;
            colors.disabledColor = new Color(.35f, .38f, .43f, .8f);
            button.colors = colors;
            var label = Label(rect, caption, 19, Color.white, TextAnchor.MiddleCenter);
            var labelRect = (RectTransform)label.transform;
            labelRect.offsetMin = new Vector2(10, 4); labelRect.offsetMax = new Vector2(-10, -4);
            button.onClick.AddListener(() => action());
            return button;
        }
        void CreateDialog()
        {
            var rect = Rect("Modal overlay", safe);
            Stretch(rect); Image(rect, new Color(0, 0, 0, .8f));
            overlay = rect.gameObject;
            dialog = Rect("Dialog", rect);
            Place(dialog, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(480, 660));
            Image(dialog, Panel);
            dialogTitle = Label(dialog, "", 26, GameWorldView.Cream, TextAnchor.MiddleLeft);
            var titleRect = (RectTransform)dialogTitle.transform;
            titleRect.anchorMin = new Vector2(0, 1); titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(.5f, 1); titleRect.offsetMin = new Vector2(20, -78); titleRect.offsetMax = new Vector2(-20, -12);
            var viewport = Rect("Scroll viewport", dialog);
            Stretch(viewport); viewport.offsetMin = new Vector2(16, 16); viewport.offsetMax = new Vector2(-16, -90);
            Image(viewport, Panel); viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect("Dialog content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1); content.sizeDelta = new Vector2(0, 600);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            overlay.SetActive(false);
        }
        void BeginDialog(string title)
        {
            Joystick.Clear();
            overlay.SetActive(true);
            dialogTitle.text = title;
            for (var i = content.childCount - 1; i >= 0; i--)
            { var child = content.GetChild(i).gameObject; child.SetActive(false); Destroy(child); }
            content.anchoredPosition = Vector2.zero;
            contentY = 0;
            rows.Clear();
        }
        void Paragraph(string text, float height = 70, int size = 18)
        {
            var label = Label(content, text, size, Color.white, TextAnchor.UpperLeft);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(-8, height);
            rect.anchoredPosition = new Vector2(0, -contentY);
            rows.Add(new DialogRow { Rect = rect, Text = label, MinimumHeight = height });
            contentY += height + 8;
        }
        Button DialogButton(string caption, Action action, bool enabled = true)
        {
            var button = MakeButton(content, caption, action);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(-8, 64);
            rect.anchoredPosition = new Vector2(0, -contentY);
            rows.Add(new DialogRow { Rect = rect, MinimumHeight = 64 });
            contentY += 76;
            button.interactable = enabled;
            return button;
        }
        void EndDialog() { content.sizeDelta = new Vector2(0, contentY); UpdateLayout(true); }
        void ReflowDialog()
        {
            contentY = 0;
            foreach (var row in rows)
            {
                var height = row.MinimumHeight;
                if (row.Text != null)
                {
                    var settings = row.Text.GetGenerationSettings(new Vector2(dialog.sizeDelta.x - 40, 0));
                    height = Mathf.Max(height, row.Text.cachedTextGeneratorForLayout.GetPreferredHeight(row.Text.text, settings) / row.Text.pixelsPerUnit + 8);
                }
                row.Rect.sizeDelta = new Vector2(-8, height);
                row.Rect.anchoredPosition = new Vector2(0, -contentY);
                contentY += height + (row.Text == null ? 12 : 8);
            }
            content.sizeDelta = new Vector2(0, contentY);
        }
        public void CloseDialog() { overlay.SetActive(false); selected = -1; world.Select(-1); Joystick.Clear(); }
        static string Stats(DefenseSpec spec, bool upgraded)
        {
            var range = upgraded ? spec.upgradedRange : spec.range;
            if (spec.kind == DefenseKind.Web) return $"Range {range:0.0} · speed ×{(upgraded ? spec.upgradedSlowMultiplier : spec.slowMultiplier):0.00}";
            return $"Range {range:0.0} · damage {(upgraded ? spec.upgradedDamage : spec.damage):0} every {spec.cooldown:0.0} s";
        }
        public void OpenSite(int index)
        {
            if (game.Phase != GamePhase.Preparation) return;
            selected = index;
            world.Select(index);
            var defense = game.Sites[index];
            BeginDialog($"Slot {index + 1} · {game.Economy.Money} resources");
            if (defense == null)
            {
                foreach (var spec in game.Config.defenses)
                {
                    var kind = spec.kind;
                    Paragraph($"{spec.title} · {spec.cost} resources\n{spec.description}\n{Stats(spec, false)}", 94);
                    DialogButton("Choose " + spec.title, () => ConfirmBuild(index, kind), game.Economy.Money >= spec.cost);
                }
            }
            else
            {
                var spec = defense.Spec;
                Paragraph(spec.title + "\n" + spec.description, 82, 22);
                Paragraph("Current: " + Stats(spec, defense.Upgraded), 60);
                if (!defense.Upgraded)
                {
                    Paragraph($"One upgrade · {spec.upgradeCost} resources\nAfter: {Stats(spec, true)}", 90);
                    DialogButton($"Confirm upgrade · {spec.upgradeCost}", () => { if (game.Upgrade(index)) CloseDialog(); }, game.Economy.Money >= spec.upgradeCost);
                }
                else Paragraph("Fully upgraded.", 48);
            }
            DialogButton("Close", CloseDialog);
            EndDialog();
        }
        void ConfirmBuild(int index, DefenseKind kind)
        {
            var spec = game.Config.Defense(kind);
            BeginDialog("Build " + spec.title);
            Paragraph($"Cost: {spec.cost} resources\nAvailable: {game.Economy.Money}\n{spec.description}", 110, 21);
            Paragraph(Stats(spec, false), 58);
            Paragraph($"Later upgrade: {spec.upgradeCost} resources\n{Stats(spec, true)}", 90);
            DialogButton($"Confirm purchase · {spec.cost}", () => { if (game.Build(index, kind)) CloseDialog(); }, game.Economy.Money >= spec.cost);
            DialogButton("Back", () => OpenSite(index));
            DialogButton("Close", CloseDialog);
            EndDialog();
        }
        void ShowStateDialog()
        {
            selected = -1;
            world.Select(-1);
            if (game.Phase == GamePhase.Menu)
            {
                BeginDialog("UNINVITED");
                Paragraph("A private party. A line of gatecrashers. You guard the entrance.", 98, 25);
                Paragraph("Build defenses along the path and protect the 20 candy reserves. Survive five waves to save the party.", 108);
                Paragraph("Start with 100 resources: enough for two Pumpkins. The guardian attacks automatically. Move with WASD, arrow keys, or the joystick.", 124);
                DialogButton("Open the party", () => { CloseDialog(); game.Reset(); });
                Paragraph($"Best result: {PlayerPrefs.GetInt("Uninvited.BestWave", 0)}/5 waves · {PlayerPrefs.GetInt("Uninvited.BestReserves", 0)} reserves", 55);
                DialogButton("Guardian range: " + (world.ShowRange ? "visible" : "hidden"), () => { world.ShowRange = !world.ShowRange; PlayerPrefs.SetInt("Uninvited.ShowRange", world.ShowRange ? 1 : 0); PlayerPrefs.Save(); ShowStateDialog(); });
            }
            else if (game.Phase == GamePhase.Paused)
            {
                BeginDialog("Party paused");
                Paragraph("The game is paused. When you return to this window, resume it here.", 90);
                DialogButton("Resume", () => { CloseDialog(); game.Resume(); });
                DialogButton("Restart game", () => { CloseDialog(); game.Reset(); });
            }
            else
            {
                var won = game.Phase == GamePhase.Victory;
                BeginDialog(won ? "Party saved!" : "Out of candy");
                Paragraph(won ? "You survived all five waves. Let the guests celebrate!" : "The gatecrashers emptied your reserves. Spread out your defenses and support them with the guardian.", 110, 23);
                Paragraph($"Reserves: {game.Economy.Reserves}\nWaves completed: {(won ? game.WaveNumber : game.WaveNumber - 1)}/{game.Config.waves.Length}\nCombat: {game.CombatSeconds:0} seconds", 108);
                DialogButton("Play again", () => { CloseDialog(); game.Reset(); });
            }
            EndDialog();
        }
        void Refresh()
        {
            stats.text = $"Reserves {game.Economy.Reserves}  ·  Resources {game.Economy.Money}";
            status.text = game.Phase == GamePhase.Preparation ? $"Preparation · next {(game.WaveNumber + 1)}/{game.Config.waves.Length}" : $"Wave {Mathf.Max(1, game.WaveNumber)}/{game.Config.waves.Length} · {PhaseName(game.Phase)}";
            pause.interactable = game.Phase == GamePhase.Preparation || game.Phase == GamePhase.Wave;
            start.gameObject.SetActive(game.Phase == GamePhase.Preparation);
            Joystick.gameObject.SetActive(game.Phase == GamePhase.Preparation || game.Phase == GamePhase.Wave);
            if (shownPhase == game.Phase) return;
            shownPhase = game.Phase;
            if (game.Phase == GamePhase.Menu || game.Phase == GamePhase.Paused || game.Phase == GamePhase.Victory || game.Phase == GamePhase.Defeat) ShowStateDialog();
            else CloseDialog();
            RefreshInstructions();
        }
        static string PhaseName(GamePhase phase) => phase == GamePhase.Wave ? "In combat" : phase == GamePhase.Paused ? "Pause" : phase == GamePhase.Victory ? "Victory" : phase == GamePhase.Defeat ? "Defeat" : "Menu";
        void Update() => UpdateLayout(false);
        static float DisplayPixelRatio()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return Mathf.Max(1, UninvitedPixelRatio());
#else
            return 1;
#endif
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern float UninvitedPixelRatio();
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern int UninvitedUsesTouchControls();
#endif
        void UpdateLayout(bool force)
        {
            var screenSafe = Screen.safeArea;
            if (screenSafe.width <= 0 || screenSafe.height <= 0) screenSafe = new Rect(0, 0, Screen.width, Screen.height);
            if (!force && lastWidth == Screen.width && lastHeight == Screen.height && lastSafe == screenSafe) return;
            lastWidth = Screen.width; lastHeight = Screen.height; lastSafe = screenSafe;
            Joystick.Clear();
            // UI size follows CSS pixels, independent of the Web canvas render
            // resolution. The minimum makes 64-unit buttons at least 48 CSS px.
            var pixelRatio = DisplayPixelRatio();
            scaler.scaleFactor = Mathf.Clamp(Mathf.Min(screenSafe.width / pixelRatio / 540f, screenSafe.height / pixelRatio / 800f), .75f, 1.6f) * pixelRatio;
            safe.anchorMin = new Vector2(screenSafe.xMin / Screen.width, screenSafe.yMin / Screen.height);
            safe.anchorMax = new Vector2(screenSafe.xMax / Screen.width, screenSafe.yMax / Screen.height);
            var width = screenSafe.width / scaler.scaleFactor;
            var height = screenSafe.height / scaler.scaleFactor;
            var statsRect = (RectTransform)stats.transform;
            statsRect.sizeDelta = new Vector2(width - 148, 32);
            ((RectTransform)status.transform).sizeDelta = new Vector2(width - 148, 36);
            ((RectTransform)start.transform).sizeDelta = new Vector2(Mathf.Min(280, width - 176), 64);
            dialog.sizeDelta = new Vector2(Mathf.Min(480, width - 24), Mathf.Min(690, height - 24));
            // Compact landscape bars retain readable controls and give the board
            // more height instead of shrinking every touch target.
            var landscape = screenSafe.width > screenSafe.height;
            var footerHeight = landscape ? 132 : 180;
            var headerHeight = landscape ? 80 : 96;
            header.sizeDelta = new Vector2(0, headerHeight);
            footer.sizeDelta = new Vector2(0, footerHeight);
            var stickSize = landscape ? 96 : 122;
            Place((RectTransform)Joystick.transform, Vector2.zero, Vector2.zero, new Vector2(18, landscape ? 30 : 38), new Vector2(stickSize, stickSize));
            ((RectTransform)start.transform).anchoredPosition = new Vector2(-16, landscape ? -8 : -20);
            var hint = (RectTransform)instructions.transform;
            hint.offsetMin = new Vector2(landscape ? 132 : 160, landscape ? 4 : 10);
            hint.offsetMax = new Vector2(-14, landscape ? 52 : 88);
            instructions.fontSize = landscape ? 16 : 17;
            ReflowDialog();
            var bottom = footerHeight * scaler.scaleFactor;
            var top = headerHeight * scaler.scaleFactor;
            var board = new Rect(screenSafe.x, screenSafe.y + bottom, screenSafe.width, Mathf.Max(60, screenSafe.height - top - bottom));
            world.FitCamera(board);
            RefreshInstructions();
        }
        void RefreshInstructions()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var touch = UninvitedUsesTouchControls() != 0;
#else
            var touch = Application.isMobilePlatform || Input.touchSupported;
#endif
            if (touch && lastSafe.width > lastSafe.height)
                instructions.text = "Rotate for a larger board.\nDrag the joystick to move.";
            else if (game.Phase == GamePhase.Preparation)
                instructions.text = touch ? "Tap a slot to build.\nDrag the joystick to move." : "Click a slot to build.\nWASD / arrows or joystick.";
            else
                instructions.text = touch ? "Automatic attacks.\nDrag the joystick to move." : "Automatic attacks.\nWASD / arrows or joystick.";
        }
        void OnDestroy() { if (game != null) game.Changed -= Refresh; }
    }
}
