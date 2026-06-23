using UnityEngine;

namespace ArmyVArmy.Core
{
    // Shared slot layout used by both the MetaHub formation editor (UI grid) and the Battle scene
    // (world-space spawn offsets), so a unit placed in slot N ends up in the same relative spot.
    public static class FormationGrid
    {
        public const int Columns = 4;
        public const int Rows = 10;
        public const int SlotCount = Columns * Rows;

        const float SlotSpacing = 0.8f;

        public static Vector2 SlotToWorldOffset(int slot)
        {
            int col = slot % Columns;
            int row = slot / Columns;
            return new Vector2(
                (col - (Columns - 1) * 0.5f) * SlotSpacing,
                (row - (Rows - 1) * 0.5f) * SlotSpacing);
        }
    }
}
