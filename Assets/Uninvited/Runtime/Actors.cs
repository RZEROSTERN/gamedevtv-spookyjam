using UnityEngine;

namespace Uninvited
{
    public sealed class EnemyState
    {
        public readonly int Id;
        public readonly EnemySpec Spec;
        public Vector2 Position;
        public float Resistance;
        public float Progress;
        public float Slow = 1;
        public int NextWaypoint = 1;
        public bool Resolved;
        public bool Active => !Resolved && Resistance > 0;
        public EnemyState(int id, EnemySpec spec, Vector2 start) { Id = id; Spec = spec; Position = start; Resistance = spec.resistance; }
        public bool Move(Vector2[] path, float delta)
        {
            var remaining = Spec.speed * Slow * delta;
            while (NextWaypoint < path.Length)
            {
                var distance = Vector2.Distance(Position, path[NextWaypoint]);
                if (remaining < distance)
                {
                    Position = Vector2.MoveTowards(Position, path[NextWaypoint], remaining);
                    Progress += remaining;
                    return false;
                }
                Position = path[NextWaypoint++];
                Progress += distance;
                remaining -= distance;
            }
            return true;
        }
    }

    public sealed class DefenseState
    {
        public readonly DefenseSpec Spec;
        public readonly Vector2 Position;
        public bool Upgraded;
        public float Timer;
        public float Range => Upgraded ? Spec.upgradedRange : Spec.range;
        public float Damage => Upgraded ? Spec.upgradedDamage : Spec.damage;
        public float Slow => Upgraded ? Spec.upgradedSlowMultiplier : Spec.slowMultiplier;
        public DefenseState(DefenseSpec spec, Vector2 position) { Spec = spec; Position = position; }
    }
}
