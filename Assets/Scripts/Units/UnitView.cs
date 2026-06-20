using UnityEngine;

namespace ArmyVArmy.Units
{
    // Pooled visual for one simulated unit. Holds no per-frame logic of its own - the
    // simulation runner loops over the pool centrally and writes into these each frame.
    // Phase 2: placeholder shape only; sprite-sheet animation/culling/LOD land in Phase 3.
    [RequireComponent(typeof(SpriteRenderer))]
    public class UnitView : MonoBehaviour
    {
        public SpriteRenderer Renderer { get; private set; }

        void Awake()
        {
            Renderer = GetComponent<SpriteRenderer>();
            Renderer.sprite = PlaceholderSprite.Shared;
        }
    }
}
