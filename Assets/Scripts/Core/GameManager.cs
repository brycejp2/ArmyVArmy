using ArmyVArmy.Data;
using ArmyVArmy.Save;
using UnityEngine;

namespace ArmyVArmy.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] UnitDef swordsmanDef;
        [SerializeField] UnitDef spearmanDef;
        [SerializeField] UnitDef archerDef;

        const int StartingSwordsmen = 6;
        const int StartingSpearmen = 6;
        const int StartingArchers = 3;

        public RunState CurrentRun { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartNewRun()
        {
            CurrentRun = CreateDefaultRun();
            SaveService.Save(CurrentRun);
            SceneRouter.LoadMetaHub();
        }

        public void ContinueRun()
        {
            CurrentRun = SaveService.Load();
            SceneRouter.LoadMetaHub();
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        RunState CreateDefaultRun()
        {
            var run = new RunState();
            int slot = 0;

            AddUnits(run, swordsmanDef, StartingSwordsmen, ref slot);
            AddUnits(run, spearmanDef, StartingSpearmen, ref slot);
            AddUnits(run, archerDef, StartingArchers, ref slot);

            return run;
        }

        static void AddUnits(RunState run, UnitDef def, int count, ref int slot)
        {
            for (int i = 0; i < count; i++)
            {
                run.Roster.Add(new RosterUnit
                {
                    UnitDefName = def.DisplayName,
                    CurrentHealth = def.Health,
                    FormationSlot = slot
                });
                slot++;
            }
        }
    }
}
