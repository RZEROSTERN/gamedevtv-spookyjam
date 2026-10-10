using System.Collections.Generic;
using UnityEngine;

namespace Uninvited
{
    public sealed class GameWorldView : MonoBehaviour
    {
        public Camera Camera { get; private set; }
        public Geometry Art { get; private set; }
        GameSession game;
        Transform actorsRoot;
        Transform effectsRoot;
        SpriteRenderer guardian;
        SpriteRenderer facing;
        SpriteRenderer baseTable;
        SpriteRenderer guardianRange;
        SpriteRenderer selectedRange;
        SpriteRenderer[] siteBodies;
        SpriteRenderer[] siteMarks;
        SpriteRenderer[] siteStars;
        readonly Dictionary<int, EnemyVisual> actors = new Dictionary<int, EnemyVisual>();
        readonly List<int> stale = new List<int>(64);
        readonly List<Effect> effects = new List<Effect>(64);
        float baseFlash;
        public bool ShowRange { get => guardianRange.gameObject.activeSelf; set => guardianRange.gameObject.SetActive(value); }
        public static readonly Color Blue = new Color(.3f, .7f, 1);
        public static readonly Color Orange = new Color(1, .48f, .2f);
        public static readonly Color Purple = new Color(.7f, .4f, .95f);
        public static readonly Color Cream = new Color(1, .89f, .65f);
        public static readonly Color Mint = new Color(.4f, .9f, .7f);
        static readonly Color TableColor = new Color(.48f, .32f, .23f);
        sealed class EnemyVisual
        {
            public GameObject Root;
            public SpriteRenderer Body, Health, Slow;
            public float Flash;
        }
        sealed class Effect
        {
            public GameObject Root;
            public SpriteRenderer Sprite;
            public float Remaining, Duration;
            public Vector3 Velocity;
            public bool Expand;
        }

        public void Initialize(GameSession session)
        {
            game = session;
            Art = new Geometry();
            Camera = new GameObject("Board camera").AddComponent<Camera>();
            Camera.transform.SetParent(transform, false);
            Camera.transform.localPosition = new Vector3(0, .2f, -10);
            Camera.orthographic = true;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = new Color(.065f, .08f, .12f);
            Camera.nearClipPlane = .1f;
            Camera.farClipPlane = 30;
            Art.Draw(transform, "Courtyard", Shape.Square, Vector2.zero, new Vector2(10, 12.5f), new Color(.1f, .13f, .18f), -10);
            Art.Line(transform, "Waypoint path", game.Config.path, 1.1f, new Color(.29f, .32f, .38f), -8);
            Art.Line(transform, "Path guide", game.Config.path, .045f, new Color(.48f, .5f, .54f), -7);
            Art.Draw(transform, "Entrance", Shape.Ring, game.Config.path[0], new Vector2(1.2f, 1.2f), Cream, -6);
            WorldText("COLADOS", new Vector2(0, 6), .12f, 40, Cream);
            baseTable = Art.Draw(transform, "Reserve table", Shape.Square, new Vector2(0, -5.15f), new Vector2(2.9f, .65f), TableColor, -5);
            for (var i = 0; i < 7; i++)
                Art.Draw(transform, "Candy", i % 2 == 0 ? Shape.Disc : Shape.Triangle, new Vector2(-1.05f + .35f * i, -5.1f), Vector2.one * .22f, i % 2 == 0 ? Orange : Purple, -4);
            WorldText("Fiesta privada — Solo con invitación", new Vector2(0, -5.8f), .09f, 34, Cream);
            siteBodies = new SpriteRenderer[game.Sites.Length];
            siteMarks = new SpriteRenderer[game.Sites.Length];
            siteStars = new SpriteRenderer[game.Sites.Length];
            for (var i = 0; i < game.Sites.Length; i++)
            {
                var p = game.Config.buildSites[i];
                Art.Draw(transform, "Site border " + (i + 1), Shape.Ring, p, Vector2.one * .95f, new Color(.5f, .6f, .7f), -2);
                siteBodies[i] = Art.Draw(transform, "Defense " + (i + 1), Shape.Disc, p, Vector2.one * .68f, new Color(.16f, .22f, .28f), 1);
                siteMarks[i] = Art.Draw(transform, "Symbol " + (i + 1), Shape.Square, p, new Vector2(.28f, .06f), Cream, 2);
                Art.Draw(siteBodies[i].transform, "Plus", Shape.Square, Vector2.zero, new Vector2(.09f, .4f), Cream, 2);
                siteStars[i] = Art.Draw(transform, "Upgrade mark", Shape.Triangle, p + new Vector2(.35f, .35f), Vector2.one * .2f, Color.white, 3);
                WorldText((i + 1).ToString(), p + new Vector2(0, -.65f), .1f, 30, Cream);
            }
            guardianRange = Art.Draw(transform, "Guardian range", Shape.Ring, game.GuardianPosition, Vector2.one * game.Config.guardianRange * 2, new Color(.3f, .7f, 1, .1f), -3);
            selectedRange = Art.Draw(transform, "Selected defense range", Shape.Ring, Vector2.zero, Vector2.one, new Color(1, 1, 1, .15f), -3);
            selectedRange.gameObject.SetActive(false);
            guardian = Art.Draw(transform, "Guardian", Shape.Disc, game.GuardianPosition, Vector2.one * .55f, Blue, 6);
            facing = Art.Draw(transform, "Facing", Shape.Triangle, game.GuardianPosition, Vector2.one * .25f, Color.white, 7);
            actorsRoot = new GameObject("Enemy visuals").transform;
            actorsRoot.SetParent(transform, false);
            effectsRoot = new GameObject("Transient effects").transform;
            effectsRoot.SetParent(transform, false);
            game.HitEffect += OnHit;
            game.PulseEffect += OnPulse;
            game.EnemyResolved += OnResolved;
            game.Resetting += Clear;
            game.Changed += RefreshSites;
            RefreshSites();
        }

        void WorldText(string value, Vector2 position, float scale, int size, Color color)
        {
            var obj = new GameObject(value);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = Vector3.one;
            var text = obj.AddComponent<TextMesh>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.characterSize = scale;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = color;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = text.font.material;
            renderer.sortingOrder = 8;
        }
        void RefreshSites()
        {
            for (var i = 0; i < game.Sites.Length; i++)
            {
                var defense = game.Sites[i];
                var body = siteBodies[i];
                body.sprite = Art.Sprite(defense == null || defense.Spec.kind == DefenseKind.Pumpkin ? Shape.Disc : defense.Spec.kind == DefenseKind.Web ? Shape.Ring : Shape.Square);
                body.color = defense == null ? new Color(.16f, .22f, .28f) : defense.Spec.kind == DefenseKind.Pumpkin ? Orange : defense.Spec.kind == DefenseKind.Web ? Mint : Purple;
                siteMarks[i].sprite = Art.Sprite(defense != null && defense.Spec.kind == DefenseKind.Pumpkin ? Shape.Triangle : Shape.Square);
                siteMarks[i].transform.localScale = defense == null ? new Vector3(.28f, .06f, 1) : defense.Spec.kind == DefenseKind.Web ? new Vector3(.06f, .6f, 1) : new Vector3(.25f, .25f, 1);
                body.transform.GetChild(0).gameObject.SetActive(defense == null);
                siteStars[i].gameObject.SetActive(defense != null && defense.Upgraded);
            }
        }
        public void Select(int site)
        {
            var valid = site >= 0 && site < game.Sites.Length;
            selectedRange.gameObject.SetActive(valid && game.Sites[site] != null);
            if (!valid || game.Sites[site] == null) return;
            selectedRange.transform.localPosition = game.Config.buildSites[site];
            selectedRange.transform.localScale = Vector3.one * game.Sites[site].Range * 2;
        }
        public void FitCamera(Rect boardPixels)
        {
            Camera.rect = new Rect(boardPixels.x / Screen.width, boardPixels.y / Screen.height, boardPixels.width / Screen.width, boardPixels.height / Screen.height);
            Camera.orthographicSize = Mathf.Max(6.4f, 5.1f / Mathf.Max(.1f, boardPixels.width / boardPixels.height));
        }
        EnemyVisual CreateEnemy(EnemyState enemy)
        {
            var root = new GameObject("Colado " + enemy.Id);
            root.transform.SetParent(actorsRoot, false);
            var kind = enemy.Spec.kind;
            var visual = new EnemyVisual { Root = root };
            var size = kind == EnemyKind.Heavy ? .75f : .48f;
            visual.Body = Art.Draw(root.transform, "Body", kind == EnemyKind.Common ? Shape.Disc : kind == EnemyKind.Runner ? Shape.Triangle : Shape.Square, Vector2.zero, Vector2.one * size, EnemyColor(kind), 4);
            Art.Draw(root.transform, "Resistance background", Shape.Square, new Vector2(0, .53f), new Vector2(.65f, .07f), new Color(.15f, .15f, .2f), 5);
            visual.Health = Art.Draw(root.transform, "Resistance", Shape.Square, new Vector2(0, .53f), new Vector2(.65f, .07f), Cream, 6);
            visual.Slow = Art.Draw(root.transform, "Slow ring", Shape.Ring, Vector2.zero, Vector2.one * (size + .2f), Mint, 5);
            return visual;
        }
        static Color EnemyColor(EnemyKind kind) => kind == EnemyKind.Common ? Orange : kind == EnemyKind.Runner ? new Color(1, .85f, .3f) : Purple;
        public void Render(float delta)
        {
            guardian.transform.localPosition = game.GuardianPosition;
            guardianRange.transform.localPosition = game.GuardianPosition;
            facing.transform.localPosition = game.GuardianPosition + game.GuardianFacing * .3f;
            facing.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(game.GuardianFacing.y, game.GuardianFacing.x) * Mathf.Rad2Deg - 90);
            foreach (var enemy in game.Enemies)
            {
                if (!enemy.Active) continue;
                if (!actors.TryGetValue(enemy.Id, out var visual)) { visual = CreateEnemy(enemy); actors.Add(enemy.Id, visual); }
                visual.Root.transform.localPosition = enemy.Position;
                visual.Health.transform.localScale = new Vector3(.65f * enemy.Resistance / enemy.Spec.resistance, .07f, 1);
                visual.Slow.gameObject.SetActive(enemy.Slow < 1);
                visual.Flash = Mathf.Max(0, visual.Flash - delta);
                visual.Body.color = visual.Flash > 0 ? Color.white : EnemyColor(enemy.Spec.kind);
            }
            stale.Clear();
            foreach (var pair in actors)
            {
                var found = false;
                foreach (var enemy in game.Enemies) if (enemy.Id == pair.Key && enemy.Active) { found = true; break; }
                if (!found) { Destroy(pair.Value.Root); stale.Add(pair.Key); }
            }
            foreach (var id in stale) actors.Remove(id);
            for (var i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i];
                effect.Remaining -= delta;
                if (effect.Remaining <= 0) { Destroy(effect.Root); effects.RemoveAt(i); continue; }
                effect.Root.transform.localPosition += effect.Velocity * delta;
                if (effect.Expand) effect.Root.transform.localScale += Vector3.one * delta * 2;
                if (effect.Sprite != null) { var c = effect.Sprite.color; c.a = effect.Remaining / effect.Duration; effect.Sprite.color = c; }
            }
            baseFlash = Mathf.Max(0, baseFlash - delta);
            baseTable.color = baseFlash > 0 ? Color.white : TableColor;
        }
        void OnHit(EnemyState enemy, Vector2 source)
        {
            if (actors.TryGetValue(enemy.Id, out var visual)) visual.Flash = .12f;
            var line = Art.Line(effectsRoot, "Attack", new[] { source, enemy.Position }, .035f, Cream, 9);
            effects.Add(new Effect { Root = line.gameObject, Duration = .1f, Remaining = .1f });
        }
        void OnPulse(Vector2 position, float radius)
        {
            var ring = Art.Draw(effectsRoot, "Sound pulse", Shape.Ring, position, Vector2.one * radius * 2, Purple, 9);
            effects.Add(new Effect { Root = ring.gameObject, Sprite = ring, Duration = .35f, Remaining = .35f, Expand = true });
        }
        void OnResolved(EnemyState enemy, bool breach)
        {
            if (breach) baseFlash = .35f;
            var shape = enemy.Spec.kind == EnemyKind.Runner ? Shape.Triangle : enemy.Spec.kind == EnemyKind.Heavy ? Shape.Square : Shape.Disc;
            var sprite = Art.Draw(effectsRoot, breach ? "Lost reserves" : "Fleeing colado", shape, enemy.Position, Vector2.one * .5f, breach ? Color.white : EnemyColor(enemy.Spec.kind), 9);
            effects.Add(new Effect { Root = sprite.gameObject, Sprite = sprite, Duration = .4f, Remaining = .4f, Velocity = breach ? Vector3.down : new Vector3(enemy.Position.x < 0 ? -3 : 3, 2, 0) });
        }
        void Clear()
        {
            foreach (var actor in actors.Values) Destroy(actor.Root);
            actors.Clear();
            foreach (var effect in effects) Destroy(effect.Root);
            effects.Clear();
            baseFlash = 0;
            Select(-1);
        }
        void OnDestroy()
        {
            if (game != null)
            {
                game.HitEffect -= OnHit;
                game.PulseEffect -= OnPulse;
                game.EnemyResolved -= OnResolved;
                game.Resetting -= Clear;
                game.Changed -= RefreshSites;
            }
            Art?.Dispose();
        }
    }
}
