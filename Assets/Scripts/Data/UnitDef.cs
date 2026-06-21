using ArmyVArmy.Units;
using UnityEngine;

namespace ArmyVArmy.Data
{
    [CreateAssetMenu(fileName = "UnitDef", menuName = "ArmyVArmy/Unit Def")]
    public class UnitDef : ScriptableObject
    {
        public string DisplayName = "Unit";
        public UnitShape Shape = UnitShape.Circle;

        [Header("Stats")]
        public float Health = 100f;
        public float Armor = 5f;
        public float Damage = 12f;
        public float AttackInterval = 1f;
        public float AttackRange = 0.6f;
        public float MoveSpeed = 2.5f;

        [Tooltip("Units stay at least this far from their target before holding to attack. " +
                 "0 for melee (close fully); a fraction of AttackRange for kiting ranged units.")]
        public float MinEngageRange = 0f;

        [Tooltip(">0 fires a pooled projectile instead of dealing damage on contact.")]
        public float ProjectileSpeed = 0f;

        [Header("Cost")]
        public int Cost = 10;
    }
}
