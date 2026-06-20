using ArmyVArmy.Combat;
using UnityEngine;

namespace ArmyVArmy.Sim
{
    // Owns all unit state as flat arrays and advances it on a fixed tick. No MonoBehaviour,
    // no per-unit Update - BattleSimulationRunner drives Tick() and syncs pooled views from it.
    public class BattleSimulation
    {
        public readonly int Capacity;
        public readonly Vector2[] Positions;
        public readonly Vector2[] PrevPositions;
        public readonly float[] Health;
        public readonly float[] Armor;
        public readonly float[] AttackDamage;
        public readonly float[] AttackRange;
        public readonly float[] AttackInterval;
        public readonly float[] AttackCooldown;
        public readonly float[] MoveSpeed;
        public readonly int[] Team;
        public readonly int[] TargetIndex;
        public readonly bool[] Alive;

        public int UnitCount { get; private set; }
        public readonly int[] AliveCountByTeam = new int[2];

        public bool IsFinished { get; private set; }
        public int WinningTeam { get; private set; } = -1;

        readonly SpatialGrid grid;
        readonly Vector2[] teamCentroid = new Vector2[2];

        const float SeparationRadius = 0.5f;
        const float SeparationStrength = 2.5f;
        const int RetargetIntervalTicks = 10;

        int tickCounter;

        public BattleSimulation(int capacity, float worldWidth, float worldHeight, float worldOriginX, float worldOriginY, float cellSize)
        {
            Capacity = capacity;
            Positions = new Vector2[capacity];
            PrevPositions = new Vector2[capacity];
            Health = new float[capacity];
            Armor = new float[capacity];
            AttackDamage = new float[capacity];
            AttackRange = new float[capacity];
            AttackInterval = new float[capacity];
            AttackCooldown = new float[capacity];
            MoveSpeed = new float[capacity];
            Team = new int[capacity];
            TargetIndex = new int[capacity];
            Alive = new bool[capacity];

            grid = new SpatialGrid(worldWidth, worldHeight, cellSize, worldOriginX, worldOriginY, capacity);
        }

        public int SpawnUnit(Vector2 position, int team, float health, float armor, float damage, float range, float attackInterval, float moveSpeed)
        {
            int index = UnitCount;
            Positions[index] = position;
            PrevPositions[index] = position;
            Health[index] = health;
            Armor[index] = armor;
            AttackDamage[index] = damage;
            AttackRange[index] = range;
            AttackInterval[index] = attackInterval;
            AttackCooldown[index] = 0f;
            MoveSpeed[index] = moveSpeed;
            Team[index] = team;
            TargetIndex[index] = -1;
            Alive[index] = true;

            UnitCount++;
            AliveCountByTeam[team]++;
            return index;
        }

        public void Tick(float dt)
        {
            if (IsFinished)
                return;

            System.Array.Copy(Positions, PrevPositions, UnitCount);

            grid.Rebuild(Positions, Alive, UnitCount);
            ComputeTeamCentroids();
            AcquireTargets();
            MoveAndSeparate(dt);
            ResolveCombat(dt);
            CheckWinLose();

            tickCounter++;
        }

        void ComputeTeamCentroids()
        {
            Vector2 sum0 = Vector2.zero, sum1 = Vector2.zero;
            int n0 = 0, n1 = 0;

            for (int i = 0; i < UnitCount; i++)
            {
                if (!Alive[i])
                    continue;

                if (Team[i] == 0)
                {
                    sum0 += Positions[i];
                    n0++;
                }
                else
                {
                    sum1 += Positions[i];
                    n1++;
                }
            }

            teamCentroid[0] = n0 > 0 ? sum0 / n0 : Vector2.zero;
            teamCentroid[1] = n1 > 0 ? sum1 / n1 : Vector2.zero;
        }

        void AcquireTargets()
        {
            for (int i = 0; i < UnitCount; i++)
            {
                if (!Alive[i])
                    continue;

                bool needsTarget = TargetIndex[i] < 0 || !Alive[TargetIndex[i]];
                bool isRetargetTick = (i + tickCounter) % RetargetIntervalTicks == 0;

                if (!needsTarget && !isRetargetTick)
                    continue;

                TargetIndex[i] = FindNearestEnemy(i);
            }
        }

        int FindNearestEnemy(int unitIndex)
        {
            Vector2 pos = Positions[unitIndex];
            int myTeam = Team[unitIndex];
            int best = -1;
            float bestDistSq = float.MaxValue;

            grid.GetNeighborCellRange(pos, out int cxMin, out int cxMax, out int cyMin, out int cyMax);

            for (int cy = cyMin; cy <= cyMax; cy++)
            {
                for (int cx = cxMin; cx <= cxMax; cx++)
                {
                    int cell = grid.CellAt(cx, cy);
                    int start = grid.BucketStart(cell);
                    int end = start + grid.BucketCount(cell);

                    for (int s = start; s < end; s++)
                    {
                        int other = grid.UnitAt(s);
                        if (other == unitIndex || Team[other] == myTeam)
                            continue;

                        float distSq = (Positions[other] - pos).sqrMagnitude;
                        if (distSq < bestDistSq)
                        {
                            bestDistSq = distSq;
                            best = other;
                        }
                    }
                }
            }

            return best;
        }

        void MoveAndSeparate(float dt)
        {
            for (int i = 0; i < UnitCount; i++)
            {
                if (!Alive[i])
                    continue;

                Vector2 pos = Positions[i];
                Vector2 desired = Vector2.zero;
                int target = TargetIndex[i];

                if (target >= 0)
                {
                    Vector2 toTarget = Positions[target] - pos;
                    if (toTarget.magnitude > AttackRange[i])
                        desired = toTarget.normalized;
                }
                else
                {
                    Vector2 toCentroid = teamCentroid[1 - Team[i]] - pos;
                    if (toCentroid.sqrMagnitude > 0.01f)
                        desired = toCentroid.normalized;
                }

                Vector2 separation = ComputeSeparation(i, pos);
                Vector2 velocity = desired * MoveSpeed[i] + separation * SeparationStrength;
                Positions[i] = pos + velocity * dt;
            }
        }

        Vector2 ComputeSeparation(int unitIndex, Vector2 pos)
        {
            Vector2 push = Vector2.zero;

            grid.GetNeighborCellRange(pos, out int cxMin, out int cxMax, out int cyMin, out int cyMax);

            for (int cy = cyMin; cy <= cyMax; cy++)
            {
                for (int cx = cxMin; cx <= cxMax; cx++)
                {
                    int cell = grid.CellAt(cx, cy);
                    int start = grid.BucketStart(cell);
                    int end = start + grid.BucketCount(cell);

                    for (int s = start; s < end; s++)
                    {
                        int other = grid.UnitAt(s);
                        if (other == unitIndex)
                            continue;

                        Vector2 offset = pos - Positions[other];
                        float distSq = offset.sqrMagnitude;
                        if (distSq > 0.0001f && distSq < SeparationRadius * SeparationRadius)
                        {
                            float dist = Mathf.Sqrt(distSq);
                            push += offset / dist * (1f - dist / SeparationRadius);
                        }
                    }
                }
            }

            return push;
        }

        void ResolveCombat(float dt)
        {
            for (int i = 0; i < UnitCount; i++)
            {
                if (!Alive[i])
                    continue;

                if (AttackCooldown[i] > 0f)
                    AttackCooldown[i] -= dt;

                int target = TargetIndex[i];
                if (target < 0 || !Alive[target] || AttackCooldown[i] > 0f)
                    continue;

                float range = AttackRange[i];
                if ((Positions[target] - Positions[i]).sqrMagnitude > range * range)
                    continue;

                Health[target] -= DamageModel.ResolveMeleeDamage(AttackDamage[i], Armor[target]);
                AttackCooldown[i] = AttackInterval[i];

                if (Health[target] <= 0f && Alive[target])
                {
                    Alive[target] = false;
                    AliveCountByTeam[Team[target]]--;
                }
            }
        }

        void CheckWinLose()
        {
            if (AliveCountByTeam[0] > 0 && AliveCountByTeam[1] > 0)
                return;

            IsFinished = true;
            WinningTeam = AliveCountByTeam[0] > 0 ? 0 : AliveCountByTeam[1] > 0 ? 1 : -1;
        }
    }
}
