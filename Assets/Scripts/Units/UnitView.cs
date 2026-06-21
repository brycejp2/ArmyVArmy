using UnityEngine;

namespace ArmyVArmy.Units
{
    // Pooled visual for one simulated unit. Holds no per-frame logic of its own beyond its own
    // animation bookkeeping - the simulation runner loops over the pool centrally and calls Tick.
    [RequireComponent(typeof(SpriteRenderer))]
    public class UnitView : MonoBehaviour
    {
        public SpriteRenderer Renderer { get; private set; }

        PlaceholderSpriteSet.FrameSet frames;
        float animTimer;
        int walkFrameIndex;

        const float FrameDuration = 0.15f;

        // Beyond this camera zoom, individual unit animation isn't perceptible - freeze on one
        // frame instead of cycling. A placeholder hook for the LOD that matters once the battle
        // camera can actually zoom out (Phase 4+); harmless no-op at today's fixed zoom.
        const float LodOrthographicSize = 25f;

        void Awake()
        {
            Renderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(UnitShape shape, Color color)
        {
            Renderer ??= GetComponent<SpriteRenderer>();
            frames = PlaceholderSpriteSet.Get(shape);
            Renderer.color = color;
            Renderer.sprite = frames.WalkFrames[0];
            animTimer = 0f;
            walkFrameIndex = 0;
        }

        public void Tick(float deltaTime, bool isAttacking, Camera viewCamera)
        {
            if (!Renderer.isVisible)
                return;

            if (isAttacking)
            {
                Renderer.sprite = frames.AttackFrame;
                return;
            }

            bool isLod = viewCamera != null && viewCamera.orthographicSize > LodOrthographicSize;
            if (isLod)
            {
                Renderer.sprite = frames.WalkFrames[0];
                return;
            }

            animTimer += deltaTime;
            if (animTimer >= FrameDuration)
            {
                animTimer -= FrameDuration;
                walkFrameIndex = (walkFrameIndex + 1) % frames.WalkFrames.Length;
                Renderer.sprite = frames.WalkFrames[walkFrameIndex];
            }
        }
    }
}
