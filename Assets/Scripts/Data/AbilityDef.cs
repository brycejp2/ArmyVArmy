using UnityEngine;

namespace ArmyVArmy.Data
{
    public enum AbilityEffect
    {
        Damage,
        Heal
    }

    [CreateAssetMenu(fileName = "AbilityDef", menuName = "ArmyVArmy/Ability Def")]
    public class AbilityDef : ScriptableObject
    {
        public string DisplayName = "Ability";
        public AbilityEffect Effect = AbilityEffect.Damage;
        public float Cooldown = 8f;
        public float Radius = 3f;
        public float Magnitude = 50f;
    }
}
