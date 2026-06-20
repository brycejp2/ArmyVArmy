using ArmyVArmy.Units;
using UnityEngine;

namespace ArmyVArmy.Sim
{
    // Scene-level driver: owns the BattleSimulation and its pooled UnitViews, advances the
    // sim on a fixed tick independent of frame rate, and interpolates views between ticks.
    public class BattleSimulationRunner : MonoBehaviour
    {
        [SerializeField] int unitsPerSide = 300;
        [SerializeField] float tickRate = 25f;
        [SerializeField] Color team0Color = new Color(0.3f, 0.55f, 0.95f);
        [SerializeField] Color team1Color = new Color(0.9f, 0.35f, 0.3f);

        const float WorldWidth = 80f;
        const float WorldHeight = 40f;
        const float WorldOriginX = -40f;
        const float WorldOriginY = -20f;
        const float CellSize = 1.5f;

        const float UnitHealth = 100f;
        const float UnitArmor = 5f;
        const float UnitDamage = 12f;
        const float UnitRange = 0.6f;
        const float UnitAttackInterval = 1f;
        const float UnitMoveSpeed = 2.5f;
        const float UnitSpacing = 0.8f;

        const int MaxTicksPerFrame = 5;

        BattleSimulation sim;
        UnitView[] views;
        float tickInterval;
        float accumulator;

        void Start()
        {
            tickInterval = 1f / tickRate;

            int capacity = unitsPerSide * 2;
            sim = new BattleSimulation(capacity, WorldWidth, WorldHeight, WorldOriginX, WorldOriginY, CellSize);

            BuildViewPool(capacity);
            SpawnLine(0, unitsPerSide, new Vector2(-15f, 0f), team0Color);
            SpawnLine(1, unitsPerSide, new Vector2(15f, 0f), team1Color);
        }

        void BuildViewPool(int capacity)
        {
            views = new UnitView[capacity];

            for (int i = 0; i < capacity; i++)
            {
                var go = new GameObject("Unit");
                go.transform.SetParent(transform, false);
                views[i] = go.AddComponent<UnitView>();
            }
        }

        void SpawnLine(int team, int count, Vector2 center, Color color)
        {
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt((float)count / columns);
            Vector2 origin = center - new Vector2((columns - 1) * UnitSpacing * 0.5f, (rows - 1) * UnitSpacing * 0.5f);

            for (int n = 0; n < count; n++)
            {
                int cx = n % columns;
                int cy = n / columns;
                Vector2 pos = origin + new Vector2(cx * UnitSpacing, cy * UnitSpacing);

                int index = sim.SpawnUnit(pos, team, UnitHealth, UnitArmor, UnitDamage, UnitRange, UnitAttackInterval, UnitMoveSpeed);

                views[index].transform.position = pos;
                views[index].Renderer.color = color;
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

            SyncViews(accumulator / tickInterval);
        }

        void SyncViews(float t)
        {
            for (int i = 0; i < sim.UnitCount; i++)
            {
                bool alive = sim.Alive[i];
                if (views[i].gameObject.activeSelf != alive)
                    views[i].gameObject.SetActive(alive);

                if (alive)
                    views[i].transform.position = Vector2.Lerp(sim.PrevPositions[i], sim.Positions[i], t);
            }
        }
    }
}
