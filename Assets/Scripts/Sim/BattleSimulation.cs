using ArmyVArmy.Combat;
using ArmyVArmy.Data;
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
        public readonly float[] MaxHealth;
        public readonly float[] Armor;
        public readonly float[] AttackDamage;
        public readonly float[] AttackRange;
        public readonly float[] AttackInterval;
        public readonly float[] AttackCooldown;
        public readonly float[] AttackFlashTimer;
        public readonly float[] MoveSpeed;
        public readonly float[] MinEngageRange;
        public readonly float[] ProjectileSpeed;
        public readonly int[] Team;
        public readonly int[] TargetIndex;
        public readonly bool[] Alive;

        public int UnitCount { get; private set; }
        public readonly int[] AliveCountByTeam = new int[2];

        public bool IsFinished { get; private set; }
        public int WinningTeam { get; private set; } = -1;

        public ProjectilePool Projectiles { get; }

        // Army-wide stances (Charge/Brace, ...), one slot per team.
        public readonly CommandDef[] ActiveCommand = new CommandDef[2];
        public readonly float[] CommandRemaining = new float[2];

        readonly SpatialGrid grid;
        readonly Vector2[] teamCentroid = new Vector2[2];

        const int MaxTrackedAbilities = 8;
        readonly AbilityDef[] cooldownAbilities = new AbilityDef[MaxTrackedAbilities];
        readonly float[] cooldownRemaining = new float[MaxTrackedAbilities];
        int cooldownCount;

        const float SeparationRadius = 0.5f;
        const float SeparationStrength = 2.5f;
        const int RetargetIntervalTicks = 10;
        const float AttackFlashDuration = 0.2f;

        int tickCounter;

        public BattleSimulation(int capacity, float worldWidth, float worldHeight, float worldOriginX, float worldOriginY, float cellSize)
        {
            Capacity = capacity;
            Positions = new Vector2[capacity];
            PrevPositions = new Vector2[capacity];
            Health = new float[capacity];
            MaxHealth = new float[capacity];
            Armor = new float[capacity];
            AttackDamage = new float[capacity];
            AttackRange = new float[capacity];
            AttackInterval = new float[capacity];
            AttackCooldown = new float[capacity];
            AttackFlashTimer = new float[capacity];
            MoveSpeed = new float[capacity];
            MinEngageRange = new float[capacity];
            ProjectileSpeed = new float[capacity];
            Team = new int[capacity];
            TargetIndex = new int[capacity];
            Alive = new bool[capacity];

            grid = new SpatialGrid(worldWidth, worldHeight, cellSize, worldOriginX, worldOriginY, capacity);
            Projectiles = new ProjectilePool(256);
        }

        public int SpawnUnit(Vector2 position, int team, UnitDef def)
        {
            int index = UnitCount;
            Positions[index] = position;
            PrevPositions[index] = position;
            Health[index] = def.Health;
            MaxHealth[index] = def.Health;
            Armor[index] = def.Armor;
            AttackDamage[index] = def.Damage;
            AttackRange[index] = def.AttackRange;
            AttackInterval[index] = def.AttackInterval;
            AttackCooldown[index] = 0f;
            AttackFlashTimer[index] = 0f;
            MoveSpeed[index] = def.MoveSpeed;
            MinEngageRange[index] = def.MinEngageRange;
            ProjectileSpeed[index] = def.ProjectileSpeed;
            Team[index] = team;
            TargetIndex[index] = -1;
            Alive[index] = true;

            UnitCount++;
            AliveCountByTeam[team]++;
            return index;
        }

        public void IssueCommand(int team, CommandDef command)
        {
            ActiveCommand[team] = command;
            CommandRemaining[team] = command.Duration;
        }

        public bool IsCommandActive(int team, CommandDef command)
        {
            return ActiveCommand[team] == command && CommandRemaining[team] > 0f;
        }

        public bool TryCastAbility(AbilityDef ability, Vector2 position, int casterTeam)
        {
            int slot = FindOrAddCooldownSlot(ability);
            if (cooldownRemaining[slot] > 0f)
                return false;

            ApplyAbilityEffect(ability, position, casterTeam);
            cooldownRemaining[slot] = ability.Cooldown;
            return true;
        }

        public float GetAbilityCooldownRemaining(AbilityDef ability)
        {
            for (int i = 0; i < cooldownCount; i++)
                if (cooldownAbilities[i] == ability)
                    return Mathf.Max(0f, cooldownRemaining[i]);
            return 0f;
        }

        int FindOrAddCooldownSlot(AbilityDef ability)
        {
            for (int i = 0; i < cooldownCount; i++)
                if (cooldownAbilities[i] == ability)
                    return i;

            int slot = cooldownCount++;
            cooldownAbilities[slot] = ability;
            cooldownRemaining[slot] = 0f;
            return slot;
        }

        void ApplyAbilityEffect(AbilityDef ability, Vector2 position, int casterTeam)
        {
            for (int i = 0; i < UnitCount; i++)
            {
                if (!Alive[i])
                    continue;

                bool isAlly = Team[i] == casterTeam;
                if (ability.Effect == AbilityEffect.Damage && isAlly)
                    continue;
                if (ability.Effect == AbilityEffect.Heal && !isAlly)
                    continue;

                float distSq = (Positions[i] - position).sqrMagnitude;
                if (distSq > ability.Radius * ability.Radius)
                    continue;

                if (ability.Effect == AbilityEffect.Damage)
                    ApplyDamage(i, ability.Magnitude);
                else
                    Health[i] = Mathf.Min(MaxHealth[i], Health[i] + ability.Magnitude);
            }
        }

        public void Tick(float dt)
        {
            if (IsFinished)
                return;

            UpdateAbilityCooldowns(dt);
            UpdateCommands(dt);

            System.Array.Copy(Positions, PrevPositions, UnitCount);

            grid.Rebuild(Positions, Alive, UnitCount);
            ComputeTeamCentroids();
            AcquireTargets();
            MoveAndSeparate(dt);
            ResolveCombat(dt);
            CheckWinLose();

            tickCounter++;
        }

        void UpdateAbilityCooldowns(float dt)
        {
            for (int i = 0; i < cooldownCount; i++)
                if (cooldownRemaining[i] > 0f)
                    cooldownRemaining[i] -= dt;
        }

        void UpdateCommands(float dt)
        {
            for (int t = 0; t < 2; t++)
            {
                if (CommandRemaining[t] <= 0f)
                    continue;

                CommandRemaining[t] -= dt;
                if (CommandRemaining[t] <= 0f)
                    ActiveCommand[t] = null;
            }
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

                CommandDef command = ActiveCommand[Team[i]];
                bool holdPosition = command != null && command.HoldPosition;
                float moveSpeedMult = command != null ? command.MoveSpeedMultiplier : 1f;

                Vector2 pos = Positions[i];
                Vector2 desired = Vector2.zero;

                if (!holdPosition)
                {
                    int target = TargetIndex[i];
                    if (target >= 0)
                    {
                        Vector2 toTarget = Positions[target] - pos;
                        float distance = toTarget.magnitude;

                        if (distance < MinEngageRange[i])
                            desired = -toTarget.normalized;
                        else if (distance > AttackRange[i])
                            desired = toTarget.normalized;
                    }
                    else
                    {
                        Vector2 toCentroid = teamCentroid[1 - Team[i]] - pos;
                        if (toCentroid.sqrMagnitude > 0.01f)
                            desired = toCentroid.normalized;
                    }
                }

                Vector2 separation = ComputeSeparation(i, pos);
                Vector2 velocity = desired * MoveSpeed[i] * moveSpeedMult + separation * SeparationStrength;
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

                if (AttackFlashTimer[i] > 0f)
                    AttackFlashTimer[i] -= dt;

                int target = TargetIndex[i];
                if (target < 0 || !Alive[target] || AttackCooldown[i] > 0f)
                    continue;

                float distance = (Positions[target] - Positions[i]).magnitude;
                if (distance > AttackRange[i])
                    continue;

                AttackCooldown[i] = AttackInterval[i];
                AttackFlashTimer[i] = AttackFlashDuration;

                CommandDef attackerCommand = ActiveCommand[Team[i]];
                float damageMult = attackerCommand != null ? attackerCommand.DamageMultiplier : 1f;
                float effectiveDamage = AttackDamage[i] * damageMult;

                if (ProjectileSpeed[i] > 0f)
                {
                    Vector2 targetVelocity = dt > 0f ? (Positions[target] - PrevPositions[target]) / dt : Vector2.zero;
                    float leadTime = distance / ProjectileSpeed[i];
                    Vector2 aimPoint = Positions[target] + targetVelocity * leadTime;
                    Projectiles.Spawn(Positions[i], aimPoint, target, effectiveDamage, ProjectileSpeed[i]);
                }
                else
                {
                    ApplyDamage(target, effectiveDamage);
                }
            }

            Projectiles.Tick(dt);
            foreach (var impact in Projectiles.PendingImpacts)
            {
                if (Alive[impact.targetIndex])
                    ApplyDamage(impact.targetIndex, impact.damage);
            }
        }

        void ApplyDamage(int targetIndex, float damageAmount)
        {
            CommandDef targetCommand = ActiveCommand[Team[targetIndex]];
            float armorMult = targetCommand != null ? targetCommand.ArmorMultiplier : 1f;

            Health[targetIndex] -= DamageModel.ResolveDamage(damageAmount, Armor[targetIndex] * armorMult);

            if (Health[targetIndex] <= 0f && Alive[targetIndex])
            {
                Alive[targetIndex] = false;
                AliveCountByTeam[Team[targetIndex]]--;
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
