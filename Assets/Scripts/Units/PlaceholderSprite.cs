using UnityEngine;

namespace ArmyVArmy.Units
{
    // Procedural circle sprite shared by every pooled UnitView until Phase 3 brings real
    // sprite-sheet art. One shared sprite + material keeps all units in a single batch.
    public static class PlaceholderSprite
    {
        static Sprite shared;

        public static Sprite Shared => shared != null ? shared : (shared = CreateCircleSprite(16));

        static Sprite CreateCircleSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float radius = size * 0.5f;
            var center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(radius - dist);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
