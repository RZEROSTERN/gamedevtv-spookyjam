using System;
using UnityEngine;

namespace Uninvited
{
    public enum EnemyKind { Common, Runner, Heavy }
    public enum DefenseKind { Pumpkin, Web, Speaker }
    public enum GamePhase { Menu, Preparation, Wave, Paused, Victory, Defeat }

    [Serializable]
    public class EnemySpec
    {
        public EnemyKind kind;
        [Min(1)] public float resistance = 18;
        [Min(.1f)] public float speed = .8f;
        [Min(1)] public int reserveDamage = 1;
        [Min(0)] public int reward = 7;
    }

    [Serializable]
    public class DefenseSpec
    {
        public DefenseKind kind;
        public string title;
        public string description;
        [Min(1)] public int cost;
        [Min(1)] public int upgradeCost;
        [Min(.1f)] public float range;
        [Min(.1f)] public float upgradedRange;
        [Min(0)] public float damage;
        [Min(0)] public float upgradedDamage;
        [Min(.1f)] public float cooldown;
        [Range(.1f, 1)] public float slowMultiplier = 1;
        [Range(.1f, 1)] public float upgradedSlowMultiplier = 1;
    }

    [Serializable]
    public class WaveSpec
    {
        public string title;
        [Min(.1f)] public float spawnInterval = 1.3f;
        [Min(0)] public int bonus = 40;
        public EnemyKind[] sequence;
    }

    // Templates only. Health, money, timers and upgrades live in GameSession.
    [CreateAssetMenu(menuName = "Uninvited/Game balance")]
    public class GameConfig : ScriptableObject
    {
        [Min(1)] public int initialReserves = 20;
        [Min(0)] public int initialMoney = 100;
        [Min(.1f)] public float guardianSpeed = 3.6f;
        [Min(.1f)] public float guardianRange = 2;
        [Min(.1f)] public float guardianCooldown = .65f;
        [Min(0)] public float guardianDamage = 4;
        public EnemySpec[] enemies;
        public DefenseSpec[] defenses;
        public WaveSpec[] waves;
        public Vector2[] path;
        public Vector2[] buildSites;

        public EnemySpec Enemy(EnemyKind kind) => enemies[(int)kind];
        public DefenseSpec Defense(DefenseKind kind) => defenses[(int)kind];

        public void SetDefaults()
        {
            initialReserves = 20;
            initialMoney = 100;
            guardianSpeed = 3.6f;
            guardianRange = 2;
            guardianCooldown = .65f;
            guardianDamage = 4;
            enemies = new[] {
                new EnemySpec { kind = EnemyKind.Common },
                new EnemySpec { kind = EnemyKind.Runner, resistance = 10, speed = 1.35f, reward = 6 },
                new EnemySpec { kind = EnemyKind.Heavy, resistance = 60, speed = .55f, reserveDamage = 3, reward = 18 }
            };
            defenses = new[] {
                new DefenseSpec { kind = DefenseKind.Pumpkin, title = "Calabaza", description = "Ataca al colado más cercano.", cost = 50, upgradeCost = 55, range = 2.65f, upgradedRange = 3, damage = 6, upgradedDamage = 10, cooldown = .8f },
                new DefenseSpec { kind = DefenseKind.Web, title = "Telaraña", description = "Ralentiza a todos en su zona. Se aplica la más fuerte.", cost = 60, upgradeCost = 50, range = 2.3f, upgradedRange = 2.8f, cooldown = 1, slowMultiplier = .65f, upgradedSlowMultiplier = .4f },
                new DefenseSpec { kind = DefenseKind.Speaker, title = "Bocina", description = "Pulso de daño a todos en su zona.", cost = 80, upgradeCost = 70, range = 2.4f, upgradedRange = 2.7f, damage = 9, upgradedDamage = 15, cooldown = 1.8f }
            };
            // Hand-authored interleaving; each wave retains the requested totals.
            waves = new[] {
                MakeWave("Primeros colados", 1.5f, 40, "CCCCCCCC"),
                MakeWave("Prisa por entrar", 1.4f, 55, "CCRCCCRCCCRCCR"),
                MakeWave("Invitados enormes", 1.5f, 65, "HCCCCHCCCHCCC"),
                MakeWave("La fila crece", 1.25f, 80, "CRCRHCRCRHCCRCRHCCRCR"),
                MakeWave("Último intento", 1.2f, 100, "HRRCRCHCCRCRHCCRCRHCCCRCHCC")
            };
            path = new[] { new Vector2(0, 5.5f), new Vector2(-.9f, 3.8f), new Vector2(.8f, 2.1f), new Vector2(1, 0), new Vector2(-.8f, -2), new Vector2(0, -4.9f) };
            buildSites = new[] { new Vector2(-3, 3.5f), new Vector2(2.9f, 3.2f), new Vector2(-2.8f, .5f), new Vector2(3.2f, 0), new Vector2(-3, -2.8f), new Vector2(2.7f, -3) };
        }

        static WaveSpec MakeWave(string title, float interval, int bonus, string order)
        {
            var sequence = new EnemyKind[order.Length];
            for (var i = 0; i < order.Length; i++)
                sequence[i] = order[i] == 'R' ? EnemyKind.Runner : order[i] == 'H' ? EnemyKind.Heavy : EnemyKind.Common;
            return new WaveSpec { title = title, spawnInterval = interval, bonus = bonus, sequence = sequence };
        }
    }
}
