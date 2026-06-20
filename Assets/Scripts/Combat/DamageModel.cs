using UnityEngine;

namespace ArmyVArmy.Combat
{
    public static class DamageModel
    {
        public const float MinDamage = 1f;

        public static float ResolveMeleeDamage(float attackDamage, float targetArmor)
        {
            return Mathf.Max(MinDamage, attackDamage - targetArmor);
        }
    }
}
