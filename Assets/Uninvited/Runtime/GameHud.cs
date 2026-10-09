using System;
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
            gameObject.AddComponent<GraphicRaycaster>();
            var eventObject = new GameObject("UI EventSystem");
            eventObject.transform.SetParent(transform, false);
            eventObject.AddComponent<EventSystem>();
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
            pause = MakeButton(header, "Pausa", () => game.Pause());
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
            start = MakeButton(footer, "Iniciar oleada", () => { CloseDialog(); game.StartWave(); });
            Place((RectTransform)start.transform, Vector2.one, Vector2.one, new Vector2(-16, -20), new Vector2(280, 64));
            instructions = Label(footer, "", 17, Color.white, TextAnchor.MiddleLeft);
            var hintRect = (RectTransform)instructions.transform;
            hintRect.anchorMin = new Vector2(0, 0); hintRect.anchorMax = new Vector2(1, 0);
            hintRect.pivot = new Vector2(0, 0); hintRect.offsetMin = new Vector2(160, 44); hintRect.offsetMax = new Vector2(-14, 90);
            var caption = Label(footer, "MOVER", 14, GameWorldView.Cream, TextAnchor.MiddleCenter);
            Place((RectTransform)caption.transform, Vector2.zero, Vector2.zero, new Vector2(18, 6), new Vector2(122, 24));
            CreateDialog();
            game.Changed += Refresh;
            UpdateLayout(true);
            Refresh();
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
        }
        void Paragraph(string text, float height = 70, int size = 18)
        {
            var label = Label(content, text, size, Color.white, TextAnchor.UpperLeft);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(-8, height);
            rect.anchoredPosition = new Vector2(0, -contentY);
            contentY += height + 8;
        }
        Button DialogButton(string caption, Action action, bool enabled = true)
        {
            var button = MakeButton(content, caption, action);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(-8, 64);
            rect.anchoredPosition = new Vector2(0, -contentY);
            contentY += 76;
            button.interactable = enabled;
            return button;
        }
        void EndDialog() { content.sizeDelta = new Vector2(0, contentY); UpdateLayout(true); }
        public void CloseDialog() { overlay.SetActive(false); selected = -1; world.Select(-1); Joystick.Clear(); }
        static string Stats(DefenseSpec spec, bool upgraded)
        {
            var range = upgraded ? spec.upgradedRange : spec.range;
            if (spec.kind == DefenseKind.Web) return $"Alcance {range:0.0} · velocidad ×{(upgraded ? spec.upgradedSlowMultiplier : spec.slowMultiplier):0.00}";
            return $"Alcance {range:0.0} · daño {(upgraded ? spec.upgradedDamage : spec.damage):0} cada {spec.cooldown:0.0} s";
        }
        public void OpenSite(int index)
        {
            if (game.Phase != GamePhase.Preparation) return;
            selected = index;
            world.Select(index);
            var defense = game.Sites[index];
            BeginDialog($"Espacio {index + 1} · {game.Economy.Money} recursos");
            if (defense == null)
            {
                foreach (var spec in game.Config.defenses)
                {
                    var kind = spec.kind;
                    Paragraph($"{spec.title} · {spec.cost} recursos\n{spec.description}\n{Stats(spec, false)}", 94);
                    DialogButton("Elegir " + spec.title, () => ConfirmBuild(index, kind), game.Economy.Money >= spec.cost);
                }
            }
            else
            {
                var spec = defense.Spec;
                Paragraph(spec.title + "\n" + spec.description, 82, 22);
                Paragraph("Actual: " + Stats(spec, defense.Upgraded), 60);
                if (!defense.Upgraded)
                {
                    Paragraph($"Mejora única · {spec.upgradeCost} recursos\nDespués: {Stats(spec, true)}", 90);
                    DialogButton($"Confirmar mejora · {spec.upgradeCost}", () => { if (game.Upgrade(index)) CloseDialog(); }, game.Economy.Money >= spec.upgradeCost);
                }
                else Paragraph("Mejora completa.", 48);
            }
            DialogButton("Cerrar", CloseDialog);
            EndDialog();
        }
        void ConfirmBuild(int index, DefenseKind kind)
        {
            var spec = game.Config.Defense(kind);
            BeginDialog("Construir " + spec.title);
            Paragraph($"Costo: {spec.cost} recursos\nDisponibles: {game.Economy.Money}\n{spec.description}", 110, 21);
            Paragraph(Stats(spec, false), 58);
            Paragraph($"Mejora posterior: {spec.upgradeCost} recursos\n{Stats(spec, true)}", 90);
            DialogButton($"Confirmar compra · {spec.cost}", () => { if (game.Build(index, kind)) CloseDialog(); }, game.Economy.Money >= spec.cost);
            DialogButton("Volver", () => OpenSite(index));
            DialogButton("Cerrar", CloseDialog);
            EndDialog();
        }
        void ShowStateDialog()
        {
            selected = -1;
            world.Select(-1);
            if (game.Phase == GamePhase.Menu)
            {
                BeginDialog("COLADOS / UNINVITED");
                Paragraph("Una fiesta privada. Una fila de colados. Tú cuidas la entrada.", 98, 25);
                Paragraph("Construye defensas junto al camino y protege las 20 reservas de dulces. Supera cinco oleadas para salvar la fiesta.", 108);
                Paragraph("Empiezas con 100 recursos: alcanzan para dos calabazas. El guardián ataca automáticamente. Muévete con WASD, flechas o el joystick.", 124);
                DialogButton("Abrir la fiesta", () => { CloseDialog(); game.Reset(); });
                Paragraph($"Mejor resultado: {PlayerPrefs.GetInt("Uninvited.BestWave", 0)}/5 oleadas · {PlayerPrefs.GetInt("Uninvited.BestReserves", 0)} reservas", 55);
                DialogButton("Alcance del guardián: " + (world.ShowRange ? "visible" : "oculto"), () => { world.ShowRange = !world.ShowRange; PlayerPrefs.SetInt("Uninvited.ShowRange", world.ShowRange ? 1 : 0); PlayerPrefs.Save(); ShowStateDialog(); });
            }
            else if (game.Phase == GamePhase.Paused)
            {
                BeginDialog("Fiesta en pausa");
                Paragraph("La partida está detenida. Al volver a la ventana debes reanudarla aquí.", 90);
                DialogButton("Reanudar", () => { CloseDialog(); game.Resume(); });
                DialogButton("Reiniciar partida", () => { CloseDialog(); game.Reset(); });
            }
            else
            {
                var won = game.Phase == GamePhase.Victory;
                BeginDialog(won ? "¡Fiesta a salvo!" : "Se acabaron los dulces");
                Paragraph(won ? "Superaste las cinco oleadas. Los invitados pueden celebrar." : "Los colados agotaron las reservas. Prueba distribuir tus defensas y apoyar con el guardián.", 110, 23);
                Paragraph($"Reservas: {game.Economy.Reserves}\nOleadas superadas: {(won ? game.WaveNumber : game.WaveNumber - 1)}/{game.Config.waves.Length}\nCombate: {game.CombatSeconds:0} segundos", 108);
                DialogButton("Volver a jugar", () => { CloseDialog(); game.Reset(); });
            }
            EndDialog();
        }
        void Refresh()
        {
            stats.text = $"Reservas {game.Economy.Reserves}  ·  Recursos {game.Economy.Money}";
            status.text = game.Phase == GamePhase.Preparation ? $"Preparación · próxima {(game.WaveNumber + 1)}/{game.Config.waves.Length}" : $"Oleada {Mathf.Max(1, game.WaveNumber)}/{game.Config.waves.Length} · {PhaseName(game.Phase)}";
            pause.interactable = game.Phase == GamePhase.Preparation || game.Phase == GamePhase.Wave;
            start.gameObject.SetActive(game.Phase == GamePhase.Preparation);
            Joystick.gameObject.SetActive(game.Phase == GamePhase.Preparation || game.Phase == GamePhase.Wave);
            if (shownPhase == game.Phase) return;
            shownPhase = game.Phase;
            if (game.Phase == GamePhase.Menu || game.Phase == GamePhase.Paused || game.Phase == GamePhase.Victory || game.Phase == GamePhase.Defeat) ShowStateDialog();
            else CloseDialog();
            instructions.text = game.Phase == GamePhase.Preparation ? "Toca un espacio para construir.\nMuévete con WASD / flechas o joystick." : "Ataque automático.\nApoya las zonas con más colados.";
        }
        static string PhaseName(GamePhase phase) => phase == GamePhase.Wave ? "En combate" : phase == GamePhase.Paused ? "Pausa" : phase == GamePhase.Victory ? "Victoria" : phase == GamePhase.Defeat ? "Derrota" : "Menú";
        void Update() => UpdateLayout(false);
        void UpdateLayout(bool force)
        {
            var screenSafe = Screen.safeArea;
            if (screenSafe.width <= 0 || screenSafe.height <= 0) screenSafe = new Rect(0, 0, Screen.width, Screen.height);
            if (!force && lastWidth == Screen.width && lastHeight == Screen.height && lastSafe == screenSafe) return;
            lastWidth = Screen.width; lastHeight = Screen.height; lastSafe = screenSafe;
            scaler.scaleFactor = Mathf.Clamp(Mathf.Min(screenSafe.width / 540f, screenSafe.height / 800f), .75f, 1.6f);
            safe.anchorMin = new Vector2(screenSafe.xMin / Screen.width, screenSafe.yMin / Screen.height);
            safe.anchorMax = new Vector2(screenSafe.xMax / Screen.width, screenSafe.yMax / Screen.height);
            var width = screenSafe.width / scaler.scaleFactor;
            var height = screenSafe.height / scaler.scaleFactor;
            var statsRect = (RectTransform)stats.transform;
            statsRect.sizeDelta = new Vector2(width - 148, 32);
            ((RectTransform)status.transform).sizeDelta = new Vector2(width - 148, 36);
            ((RectTransform)start.transform).sizeDelta = new Vector2(Mathf.Min(280, width - 176), 64);
            dialog.sizeDelta = new Vector2(Mathf.Min(480, width - 24), Mathf.Min(690, height - 24));
            // In landscape retain the whole board between the two control bars.
            var bottom = 180 * scaler.scaleFactor;
            var top = 96 * scaler.scaleFactor;
            var board = new Rect(screenSafe.x, screenSafe.y + bottom, screenSafe.width, Mathf.Max(60, screenSafe.height - top - bottom));
            world.FitCamera(board);
            var mobile = Application.isMobilePlatform || Input.touchSupported;
            if (mobile && Screen.width > Screen.height)
                instructions.text = "Puedes seguir jugando.\nGira el dispositivo para ver mejor.";
            else if (game != null)
                instructions.text = game.Phase == GamePhase.Preparation ? "Toca un espacio para construir.\nWASD / flechas o joystick para moverte." : "Ataque automático.\nWASD / flechas o joystick para moverte.";
        }
        void OnDestroy() { if (game != null) game.Changed -= Refresh; }
    }
}
