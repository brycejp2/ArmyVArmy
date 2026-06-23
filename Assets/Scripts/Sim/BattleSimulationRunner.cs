using System.Collections.Generic;
using ArmyVArmy.Core;
using ArmyVArmy.Data;
using ArmyVArmy.Save;
using ArmyVArmy.Units;
using UnityEngine;

namespace ArmyVArmy.Sim
{
    // Scene-level driver: owns the BattleSimulation and its pooled UnitViews/projectile views,
    // advances the sim on a fixed tick independent of frame rate, and interpolates views between
    // ticks. The player's side spawns from GameManager.CurrentRun (roster/formation/upgrades/
    // wounds) when available, falling back to a fixed test formation when the scene is played
    // directly without going through Main Menu -> Meta Hub first. Once the battle ends, applies
    // the result to RunState (gold + surviving wounds on a win, permadeath on a loss) exactly
    // once - BattleResolutionController just reads BattleEnded/PlayerWon to show a result screen.
    public class BattleSimulationRunner : MonoBehaviour
    {
        public const int PlayerTeam = 0;

        [SerializeField] UnitDef swordsmanDef;
        [SerializeField] UnitDef spearmanDef;
        [SerializeField] UnitDef archerDef;

        [SerializeField] UpgradeDef swordsmanArmorUpgrade;
        [SerializeField] UpgradeDef spearmanArmorUpgrade;
        [SerializeField] UpgradeDef archerArmorUpgrade;

        [SerializeField] int meleePerTypePerSide = 20;
        [SerializeField] int archersPerSide = 10;
        [SerializeField] float tickRate = 25f;
        [SerializeField] Color team0Color = new(0.3f, 0.55f, 0.95f);
        [SerializeField] Color team1Color = new(0.9f, 0.35f, 0.3f);

        const float WorldWidth = 80f;
        const float WorldHeight = 40f;
        const float WorldOriginX = -40f;
        const float WorldOriginY = -20f;
        const float CellSize = 1.5f;

        const float UnitSpacing = 0.8f;
        const float RowSpacing = 1f;

        const int MaxTicksPerFrame = 5;
        const int ProjectileViewCapacity = 256;

        const int VictoryGoldReward = 50;
        const int EnemyMeleeScalePerNode = 2;
        const int EnemyArcherScalePerNode = 1;
        const int MaxScalingNodes = 10;

        BattleSimulation sim;
        UnitView[] views;
        RosterUnit[] simIndexToRosterUnit;
        Transform[] projectileViews;
        Camera cam;
        float tickInterval;
        float accumulator;
        bool resultApplied;

        public Camera BattleCamera => cam;
        public float TimeScale { get; set; } = 1f;

        public bool BattleEnded => sim.IsFinished;
        public bool PlayerWon => sim.WinningTeam == PlayerTeam;

        public bool TryCastAbility(AbilityDef ability, Vector2 worldPosition) =>
            sim.TryCastAbility(ability, worldPosition, PlayerTeam);

        public void IssueCommand(CommandDef command) => sim.IssueCommand(PlayerTeam, command);

        public float GetAbilityCooldownRemaining(AbilityDef ability) => sim.GetAbilityCooldownRemaining(ability);

        public bool IsCommandActive(CommandDef command) => sim.IsCommandActive(PlayerTeam, command);

        void Start()
        {
            cam = Camera.main;
            tickInterval = 1f / tickRate;

            RunState run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
            int nodeIndex = run != null ? Mathf.Min(run.NodeIndex, MaxScalingNodes) : 0;
            int enemyMelee = meleePerTypePerSide + nodeIndex * EnemyMeleeScalePerNode;
            int enemyArchers = archersPerSide + nodeIndex * EnemyArcherScalePerNode;
            int enemyCount = enemyMelee * 2 + enemyArchers;
            int playerCount = run != null ? run.Roster.Count : meleePerTypePerSide * 2 + archersPerSide;
            int capacity = playerCount + enemyCount;

            sim = new BattleSimulation(capacity, WorldWidth, WorldHeight, WorldOriginX, WorldOriginY, CellSize);
            simIndexToRosterUnit = new RosterUnit[capacity];

            BuildUnitViewPool(capacity);
            BuildProjectileViewPool();

            if (run != null)
                SpawnPlayerArmyFromRunState(run, new Vector2(-15f, 0f));
            else
                SpawnArmy(0, new Vector2(-15f, 0f), facing: 1f, team0Color, meleePerTypePerSide, archersPerSide);

            SpawnArmy(1, new Vector2(15f, 0f), facing: -1f, team1Color, enemyMelee, enemyArchers);
        }

        void SpawnPlayerArmyFromRunState(RunState run, Vector2 armyCenter)
        {
            foreach (RosterUnit unit in run.Roster)
            {
                if (unit.FormationSlot < 0 || unit.CurrentHealth <= 0f)
                    continue;

                UnitDef def = FindDef(unit.UnitDefName);
                if (def == null)
                    continue;

                Vector2 pos = armyCenter + FormationGrid.SlotToWorldOffset(unit.FormationSlot);
                float armorBonus = GetArmorBonus(run, def);

                int index = sim.SpawnUnit(pos, PlayerTeam, def, unit.CurrentHealth, armorBonus);
                simIndexToRosterUnit[index] = unit;
                views[index].Initialize(def.Shape, team0Color * TintFor(def));
                views[index].transform.position = pos;
            }
        }

        UnitDef FindDef(string unitDefName)
        {
            if (swordsmanDef.DisplayName == unitDefName) return swordsmanDef;
            if (spearmanDef.DisplayName == unitDefName) return spearmanDef;
            if (archerDef.DisplayName == unitDefName) return archerDef;
            return null;
        }

        float GetArmorBonus(RunState run, UnitDef def)
        {
            UpgradeDef upgrade = def == swordsmanDef ? swordsmanArmorUpgrade
                : def == spearmanDef ? spearmanArmorUpgrade
                : def == archerDef ? archerArmorUpgrade
                : null;

            return upgrade != null ? upgrade.ArmorBonusPerLevel * run.GetUpgradeLevel(upgrade.DisplayName) : 0f;
        }

        Color TintFor(UnitDef def)
        {
            if (def == spearmanDef) return SpearmanTint;
            if (def == archerDef) return ArcherTint;
            return SwordsmanTint;
        }

        void BuildUnitViewPool(int capacity)
        {
            views = new UnitView[capacity];

            for (int i = 0; i < capacity; i++)
            {
                var go = new GameObject("Unit");
                go.transform.SetParent(transform, false);
                views[i] = go.AddComponent<UnitView>();
            }
        }

        void BuildProjectileViewPool()
        {
            projectileViews = new Transform[ProjectileViewCapacity];

            for (int i = 0; i < ProjectileViewCapacity; i++)
            {
                var go = new GameObject("Projectile");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = PlaceholderSpriteSet.ProjectileSprite;
                renderer.color = Color.yellow;
                go.SetActive(false);
                projectileViews[i] = go.transform;
            }
        }

        void SpawnArmy(int team, Vector2 frontCenter, float facing, Color color, int meleeCount, int archerCount)
        {
            // Front two rows hold the melee line (spearmen first - longest reach); archers sit
            // further back so they get to fire before the melee line closes. Each type gets a
            // small tint on top of the team color - shape alone reads thin at battle zoom.
            SpawnRow(team, spearmanDef, meleeCount, frontCenter, facing, rowIndex: 0, color * SpearmanTint);
            SpawnRow(team, swordsmanDef, meleeCount, frontCenter, facing, rowIndex: 1, color * SwordsmanTint);
            SpawnRow(team, archerDef, archerCount, frontCenter, facing, rowIndex: 3, color * ArcherTint);
        }

        static readonly Color SwordsmanTint = Color.white;
        static readonly Color SpearmanTint = new(0.7f, 0.7f, 0.85f);
        static readonly Color ArcherTint = new(1.3f, 1.3f, 0.6f);

        void SpawnRow(int team, UnitDef def, int count, Vector2 frontCenter, float facing, int rowIndex, Color color)
        {
            if (count <= 0)
                return;

            Vector2 rowCenter = frontCenter - new Vector2(facing * rowIndex * RowSpacing, 0f);
            Vector2 origin = rowCenter - new Vector2(0f, (count - 1) * UnitSpacing * 0.5f);

            for (int n = 0; n < count; n++)
            {
                Vector2 pos = origin + new Vector2(0f, n * UnitSpacing);
                int index = sim.SpawnUnit(pos, team, def);

                views[index].Initialize(def.Shape, color);
                views[index].transform.position = pos;
            }
        }

        void Update()
        {
            accumulator += Time.deltaTime * TimeScale;

            int ticksThisFrame = 0;
            while (accumulator >= tickInterval && ticksThisFrame < MaxTicksPerFrame)
            {
                sim.Tick(tickInterval);
                accumulator -= tickInterval;
                ticksThisFrame++;
            }

            float t = accumulator / tickInterval;
            SyncUnitViews(t);
            SyncProjectileViews();

            if (sim.IsFinished && !resultApplied)
            {
                resultApplied = true;
                ApplyBattleResult();
            }
        }

        void ApplyBattleResult()
        {
            RunState run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
            if (run == null)
                return;

            ApplyBattleResultTo(run);
        }

        void ApplyBattleResultTo(RunState run)
        {
            if (PlayerWon)
            {
                var survivors = new List<RosterUnit>();
                for (int i = 0; i < sim.UnitCount; i++)
                {
                    RosterUnit rosterUnit = simIndexToRosterUnit[i];
                    if (rosterUnit == null || !sim.Alive[i])
                        continue;

                    rosterUnit.CurrentHealth = sim.Health[i];
                    survivors.Add(rosterUnit);
                }

                run.Roster = survivors;
                run.Gold += VictoryGoldReward;
                run.NodeIndex++;
                SaveService.Save(run);
            }
            else
            {
                SaveService.DeleteSave();
            }
        }

        void SyncUnitViews(float t)
        {
            for (int i = 0; i < sim.UnitCount; i++)
            {
                bool alive = sim.Alive[i];
                if (views[i].gameObject.activeSelf != alive)
                    views[i].gameObject.SetActive(alive);

                if (!alive)
                    continue;

                views[i].transform.position = Vector2.Lerp(sim.PrevPositions[i], sim.Positions[i], t);
                views[i].Tick(Time.deltaTime, sim.AttackFlashTimer[i] > 0f, cam);
            }
        }

        void SyncProjectileViews()
        {
            var pool = sim.Projectiles;
            for (int i = 0; i < pool.Capacity; i++)
            {
                bool active = pool.Active[i];
                if (projectileViews[i].gameObject.activeSelf != active)
                    projectileViews[i].gameObject.SetActive(active);

                if (active)
                    projectileViews[i].position = pool.Positions[i];
            }
        }
    }
}
