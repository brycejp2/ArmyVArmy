using System.Collections.Generic;
using UnityEngine;

namespace ArmyVArmy.Units
{
    public enum UnitShape
    {
        Circle,
        Diamond,
        Triangle
    }

    // Procedurally generated placeholder "sprite sheet" per unit shape, until Phase 7+ real art
    // lands. Each shape gets a few walk frames (size pulse, cheap stand-in for a walk cycle) and
    // one attack flash frame, all cached so every unit of a shape shares the same sprites.
    public static class PlaceholderSpriteSet
    {
        public readonly struct FrameSet
        {
            public readonly Sprite[] WalkFrames;
            public readonly Sprite AttackFrame;

            public FrameSet(Sprite[] walkFrames, Sprite attackFrame)
            {
                WalkFrames = walkFrames;
                AttackFrame = attackFrame;
            }
        }

        static readonly Dictionary<UnitShape, FrameSet> cache = new();
        static Sprite projectileSprite;

        public static FrameSet Get(UnitShape shape)
        {
            if (cache.TryGetValue(shape, out var set))
                return set;

            var walk = new[]
            {
                CreateSprite(shape, 16, 1f),
                CreateSprite(shape, 16, 0.85f),
                CreateSprite(shape, 16, 1f),
                CreateSprite(shape, 16, 1.15f),
            };
            var attack = CreateSprite(shape, 18, 1.3f);

            set = new FrameSet(walk, attack);
            cache[shape] = set;
            return set;
        }

        public static Sprite ProjectileSprite => projectileSprite != null
            ? projectileSprite
            : (projectileSprite = CreateSprite(UnitShape.Circle, 8, 1f));

        static Sprite CreateSprite(UnitShape shape, int size, float fillScale)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float radius = size * 0.5f * Mathf.Clamp(fillScale, 0.1f, 1.4f);
            var center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    float alpha = ShapeCoverage(shape, point, center, radius);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static float ShapeCoverage(UnitShape shape, Vector2 point, Vector2 center, float radius)
        {
            Vector2 offset = point - center;

            switch (shape)
            {
                case UnitShape.Diamond:
                    float diamondDist = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);
                    return Mathf.Clamp01(radius - diamondDist);

                case UnitShape.Triangle:
                    float t = Mathf.Clamp01((radius - offset.y) / (2f * radius));
                    float halfWidth = radius * t;
                    bool inside = offset.y <= radius && offset.y >= -radius && Mathf.Abs(offset.x) <= halfWidth;
                    return inside ? 1f : 0f;

                default:
                    return Mathf.Clamp01(radius - offset.magnitude);
            }
        }
    }
}
