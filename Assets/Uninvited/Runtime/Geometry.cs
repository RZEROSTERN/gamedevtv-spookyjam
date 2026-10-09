using System.Collections.Generic;
using UnityEngine;

namespace Uninvited
{
    public enum Shape { Disc, Triangle, Square, Ring }

    // All art is generated locally. Swap this factory for sprites when art is ready.
    public sealed class Geometry
    {
        readonly Dictionary<Shape, Sprite> sprites = new Dictionary<Shape, Sprite>();
        public readonly Material LineMaterial = new Material(Resources.Load<Material>("UninvitedPlaceholder"));
        public Sprite Sprite(Shape shape)
        {
            if (sprites.TryGetValue(shape, out var sprite)) return sprite;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, name = "Placeholder " + shape };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var p = new Vector2((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1);
                var inside = shape == Shape.Square || (shape == Shape.Disc && p.sqrMagnitude <= .96f) ||
                    (shape == Shape.Ring && p.sqrMagnitude <= .96f && p.sqrMagnitude >= .79f) ||
                    (shape == Shape.Triangle && p.y > -.85f && p.y < .95f && Mathf.Abs(p.x) < (.95f - p.y) * .5f);
                pixels[y * size + x] = inside ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            sprites.Add(shape, sprite);
            return sprite;
        }
        public SpriteRenderer Draw(Transform parent, string name, Shape shape, Vector2 position, Vector2 scale, Color color, int layer)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite(shape);
            renderer.color = color;
            renderer.sortingOrder = layer;
            return renderer;
        }
        public LineRenderer Line(Transform parent, string name, Vector2[] points, float width, Color color, int layer, bool loop = false)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var line = obj.AddComponent<LineRenderer>();
            line.sharedMaterial = LineMaterial;
            line.useWorldSpace = false;
            line.positionCount = points.Length;
            for (var i = 0; i < points.Length; i++) line.SetPosition(i, points[i]);
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color;
            line.sortingOrder = layer;
            line.numCapVertices = 6;
            line.numCornerVertices = 6;
            line.loop = loop;
            return line;
        }
        public void Dispose()
        {
            foreach (var sprite in sprites.Values) { Object.Destroy(sprite.texture); Object.Destroy(sprite); }
            sprites.Clear();
            Object.Destroy(LineMaterial);
        }
    }
}
