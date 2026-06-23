using UnityEngine;

namespace ArmyVArmy.Data
{
    [CreateAssetMenu(fileName = "UpgradeDef", menuName = "ArmyVArmy/Upgrade Def")]
    public class UpgradeDef : ScriptableObject
    {
        public string DisplayName = "Upgrade";
        public UnitDef TargetUnit;
        public float ArmorBonusPerLevel = 3f;
        public int BaseCost = 50;
        public int CostIncreasePerLevel = 25;

        public int CostForNextLevel(int currentLevel) => BaseCost + currentLevel * CostIncreasePerLevel;
    }
}
