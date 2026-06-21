using ArmyVArmy.Data;
using ArmyVArmy.Units;
using UnityEngine;

namespace ArmyVArmy.Sim
{
    // Scene-level driver: owns the BattleSimulation and its pooled UnitViews/projectile views,
    // advances the sim on a fixed tick independent of frame rate, and interpolates views between
    // ticks. Spawns a small mixed-type formation per side so the three unit types' distinct
    // behavior (ranged kite, melee close, spears hold) is easy to watch.
    public class BattleSimulationRunner : MonoBehaviour
    {
        [SerializeField] UnitDef swordsmanDef;
        [SerializeField] UnitDef spearmanDef;
        [SerializeField] UnitDef archerDef;

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

        BattleSimulation sim;
        UnitView[] views;
        Transform[] projectileViews;
        Camera cam;
        float tickInterval;
        float accumulator;

        void Start()
        {
            cam = Camera.main;
            tickInterval = 1f / tickRate;

            int capacity = (meleePerTypePerSide * 2 + archersPerSide) * 2;
            sim = new BattleSimulation(capacity, WorldWidth, WorldHeight, WorldOriginX, WorldOriginY, CellSize);

            BuildUnitViewPool(capacity);
            BuildProjectileViewPool();

            SpawnArmy(0, new Vector2(-15f, 0f), facing: 1f, team0Color);
            SpawnArmy(1, new Vector2(15f, 0f), facing: -1f, team1Color);
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

        void SpawnArmy(int team, Vector2 frontCenter, float facing, Color color)
        {
            // Front two rows hold the melee line (spearmen first - longest reach); archers sit
            // further back so they get to fire before the melee line closes. Each type gets a
            // small tint on top of the team color - shape alone reads thin at battle zoom.
            SpawnRow(team, spearmanDef, meleePerTypePerSide, frontCenter, facing, rowIndex: 0, color * SpearmanTint);
            SpawnRow(team, swordsmanDef, meleePerTypePerSide, frontCenter, facing, rowIndex: 1, color * SwordsmanTint);
            SpawnRow(team, archerDef, archersPerSide, frontCenter, facing, rowIndex: 3, color * ArcherTint);
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
            accumulator += Time.deltaTime;

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
