using System.Collections.Generic;

namespace ArmyVArmy.Core
{
    [System.Serializable]
    public class RosterUnit
    {
        public string UnitDefName;
        public float CurrentHealth;
        public int FormationSlot = -1;
    }

    [System.Serializable]
    public class UpgradeLevelEntry
    {
        public string UpgradeDefName;
        public int Level;
    }

    [System.Serializable]
    public class RunState
    {
        public int Gold = 100;
        public int NodeIndex = 0;
        public List<RosterUnit> Roster = new();
        public List<UpgradeLevelEntry> UpgradeLevels = new();

        public int GetUpgradeLevel(string upgradeDefName)
        {
            foreach (var entry in UpgradeLevels)
                if (entry.UpgradeDefName == upgradeDefName)
                    return entry.Level;
            return 0;
        }

        public void IncrementUpgradeLevel(string upgradeDefName)
        {
            foreach (var entry in UpgradeLevels)
            {
                if (entry.UpgradeDefName == upgradeDefName)
                {
                    entry.Level++;
                    return;
                }
            }

            UpgradeLevels.Add(new UpgradeLevelEntry { UpgradeDefName = upgradeDefName, Level = 1 });
        }
    }
}
