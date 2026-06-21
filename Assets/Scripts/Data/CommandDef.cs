using UnityEngine;

namespace ArmyVArmy.Data
{
    [CreateAssetMenu(fileName = "CommandDef", menuName = "ArmyVArmy/Command Def")]
    public class CommandDef : ScriptableObject
    {
        public string DisplayName = "Command";
        public float Duration = 5f;
        public float MoveSpeedMultiplier = 1f;
        public float DamageMultiplier = 1f;
        public float ArmorMultiplier = 1f;
        public bool HoldPosition = false;
    }
}
