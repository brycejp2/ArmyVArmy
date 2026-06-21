using System.Collections.Generic;
using UnityEngine;

namespace ArmyVArmy.Combat
{
    // Flat-array pool of in-flight projectiles. Damage is applied by the caller (BattleSimulation)
    // by draining PendingImpacts after Tick() - the pool only owns motion/arrival, not unit state.
    public class ProjectilePool
    {
        public readonly int Capacity;
        public readonly Vector2[] Positions;
        public readonly bool[] Active;

        readonly Vector2[] aimPoints;
        readonly int[] targetUnitIndex;
        readonly float[] damage;
        readonly float[] speed;

        public readonly List<(int targetIndex, float damage)> PendingImpacts = new(64);

        public ProjectilePool(int capacity)
        {
            Capacity = capacity;
            Positions = new Vector2[capacity];
            Active = new bool[capacity];
            aimPoints = new Vector2[capacity];
            targetUnitIndex = new int[capacity];
            damage = new float[capacity];
            speed = new float[capacity];
        }

        public void Spawn(Vector2 origin, Vector2 aimPoint, int targetIndex, float damageAmount, float projectileSpeed)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (Active[i])
                    continue;

                Positions[i] = origin;
                aimPoints[i] = aimPoint;
                targetUnitIndex[i] = targetIndex;
                damage[i] = damageAmount;
                speed[i] = projectileSpeed;
                Active[i] = true;
                return;
            }
            // Pool exhausted: drop the shot. Capacity is sized generously, so this should be rare.
        }

        public void Tick(float dt)
        {
            PendingImpacts.Clear();

            for (int i = 0; i < Capacity; i++)
            {
                if (!Active[i])
                    continue;

                Vector2 toAim = aimPoints[i] - Positions[i];
                float step = speed[i] * dt;

                if (toAim.magnitude <= step)
                {
                    Active[i] = false;
                    PendingImpacts.Add((targetUnitIndex[i], damage[i]));
                    continue;
                }

                Positions[i] += toAim.normalized * step;
            }
        }
    }
}
