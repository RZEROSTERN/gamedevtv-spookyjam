using UnityEngine;

namespace Uninvited
{
    public sealed class CombatSystem
    {
        float guardianTimer;
        public void Reset() => guardianTimer = 0;
        public static EnemyState Nearest(GameSession game, Vector2 position, float range)
        {
            EnemyState target = null;
            var nearest = range * range;
            for (var i = 0; i < game.Enemies.Count; i++)
            {
                var enemy = game.Enemies[i];
                if (!enemy.Active) continue;
                var distance = (enemy.Position - position).sqrMagnitude;
                if (distance > range * range) continue;
                if (target == null || distance < nearest - .0001f || (Mathf.Abs(distance - nearest) <= .0001f && enemy.Progress > target.Progress))
                { target = enemy; nearest = distance; }
            }
            return target;
        }

        public void Tick(GameSession game, float delta)
        {
            foreach (var enemy in game.Enemies) enemy.Slow = 1;
            foreach (var defense in game.Sites)
            {
                if (defense == null || defense.Spec.kind != DefenseKind.Web) continue;
                foreach (var enemy in game.Enemies)
                    if (enemy.Active && (enemy.Position - defense.Position).sqrMagnitude <= defense.Range * defense.Range)
                        enemy.Slow = Mathf.Min(enemy.Slow, defense.Slow);
            }
            guardianTimer -= delta;
            if (guardianTimer <= 0)
            {
                var target = Nearest(game, game.GuardianPosition, game.Config.guardianRange);
                if (target != null)
                {
                    game.Hit(target, game.Config.guardianDamage, game.GuardianPosition);
                    guardianTimer = game.Config.guardianCooldown;
                }
            }
            foreach (var defense in game.Sites)
            {
                if (defense == null || defense.Spec.kind == DefenseKind.Web) continue;
                defense.Timer -= delta;
                if (defense.Timer > 0) continue;
                var target = Nearest(game, defense.Position, defense.Range);
                if (target == null) continue;
                if (defense.Spec.kind == DefenseKind.Pumpkin) game.Hit(target, defense.Damage, defense.Position);
                else
                {
                    game.Pulse(defense.Position, defense.Range);
                    foreach (var enemy in game.Enemies)
                        if (enemy.Active && (enemy.Position - defense.Position).sqrMagnitude <= defense.Range * defense.Range)
                            game.Hit(enemy, defense.Damage, defense.Position);
                }
                defense.Timer = defense.Spec.cooldown;
            }
        }
    }
}
