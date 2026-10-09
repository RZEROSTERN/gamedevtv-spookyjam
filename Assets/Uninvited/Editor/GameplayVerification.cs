using System;
using System.Text;
using System.IO;
using UnityEngine;

namespace Uninvited.Editor
{
    public static class GameplayVerification
    {
        static int assertions;
        static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException("Gameplay check failed: " + message);
        }
        public static void Run()
        {
            assertions = 0;
#if UNINVITED_STANDALONE
            // Allocate a data-only SO without Unity native calls. This harness runs the
            // real game model and Unity managed Vector2/Mathf; it cannot verify a scene.
            var config = (GameConfig)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GameConfig));
#else
            var config = ScriptableObject.CreateInstance<GameConfig>();
#endif
            config.SetDefaults();
            try
            {
                CheckWaves(config);
                var game = new GameSession(config);
                Check(game.Phase == GamePhase.Menu, "menu on startup");
                Check(!game.Build(0, DefenseKind.Pumpkin), "no purchase in menu");
                game.Reset();
                Check(game.Build(0, DefenseKind.Pumpkin), "first purchase");
                Check(game.Economy.Money == 50, "exactly one debit");
                Check(!game.Build(0, DefenseKind.Pumpkin) && game.Economy.Money == 50, "duplicate purchase rejected");
                Check(!game.Build(1, DefenseKind.Speaker) && game.Economy.Money == 50, "insufficient funds rejected");
                Check(game.Build(1, DefenseKind.Pumpkin) && game.Economy.Money == 0, "two initial defenses");
                game.Economy.Reward(200);
                Check(game.Upgrade(0), "upgrade purchased");
                var money = game.Economy.Money;
                Check(!game.Upgrade(0) && money == game.Economy.Money, "only one upgrade");
                game.StartWave();
                Check(!game.Build(2, DefenseKind.Web) && !game.Upgrade(1), "combat construction blocked");
                game.Tick(.2f, Vector2.up);
                Check(game.Enemies.Count == 1 && game.Waves.Remaining == 7, "first spawn");
                game.Pause();
                var position = game.GuardianPosition;
                var enemyPosition = game.Enemies[0].Position;
                var pending = game.Waves.Remaining;
                var seconds = game.CombatSeconds;
                game.Tick(100, Vector2.up);
                Check(game.GuardianPosition == position && game.Enemies[0].Position == enemyPosition && game.Waves.Remaining == pending && game.CombatSeconds == seconds, "pause freezes timers and movement");
                Check(!game.Build(2, DefenseKind.Web) && !game.Upgrade(1), "pause does not enable construction");
                game.Resume();
                Check(game.Phase == GamePhase.Wave, "resume combat state");
                game.Tick(.1f, Vector2.up);
                Check(game.CombatSeconds > seconds, "resume advances timer");
                var enemy = game.Enemies[0];
                money = game.Economy.Money;
                game.Hit(enemy, 1000, Vector2.zero);
                game.Resolve(enemy, false); game.Resolve(enemy, true);
                Check(game.Economy.Money == money + enemy.Spec.reward && game.Economy.Reserves == 20, "kill resolves once");
                game.Spawn(EnemyKind.Heavy);
                enemy = game.Enemies[game.Enemies.Count - 1];
                money = game.Economy.Money;
                game.Resolve(enemy, true); game.Resolve(enemy, true);
                Check(game.Economy.Reserves == 17 && game.Economy.Money == money, "breach once, no reward");
                game.Reset();
                Check(game.Enemies.Count == 0 && game.Waves.Remaining == 0 && game.Economy.Money == 100 && game.Economy.Reserves == 20 && game.WaveNumber == 0 && game.Sites[0] == null, "restart cleans simulation");
                game.Pause(); game.Resume();
                Check(game.Phase == GamePhase.Preparation, "resume preparation");
                CheckTargetingAndSlow(config);
                CheckWaveCompletion(config);
                var report = new StringBuilder();
                report.AppendLine("# Gameplay verification");
#if UNINVITED_STANDALONE
                report.AppendLine("Execution host: .NET 8, using Unity 6000.3.5f2 managed assemblies. Unity editor, native engine, rendering and devices were NOT executed.");
#else
                report.AppendLine("Execution host: Unity " + Application.unityVersion + ". Deterministic simulation; not device testing.");
#endif
                report.AppendLine("\n## Full matches");
                var victory = SimulateMatch(config, true, report);
                var defeat = SimulateMatch(config, false, report);
                Check(victory == GamePhase.Victory, "strategy can win all five waves");
                Check(defeat == GamePhase.Defeat, "guardian alone cannot win");
                report.AppendLine($"\nAssertions passed: {assertions}.");
                report.AppendLine("\nPending: real mouse/keyboard and multitouch, browser full match, mobile landscape and safe areas, focus-loss, native devices.");
                Directory.CreateDirectory("Documentation");
                File.WriteAllText("Documentation/GameplayVerification.md", report.ToString());
#if UNINVITED_STANDALONE
                Console.WriteLine("UNINVITED: " + assertions + " gameplay assertions passed.\n" + report);
#else
                Debug.Log("UNINVITED: " + assertions + " gameplay assertions passed.\n" + report);
#endif
            }
            finally
            {
#if !UNINVITED_STANDALONE
                UnityEngine.Object.DestroyImmediate(config);
#endif
            }
        }
        static void CheckWaves(GameConfig config)
        {
            var expected = new[,] { {8,0,0}, {10,4,0}, {10,0,3}, {10,8,3}, {14,8,5} };
            for (var wave = 0; wave < config.waves.Length; wave++)
            {
                var counts = new int[3];
                foreach (var kind in config.waves[wave].sequence) counts[(int)kind]++;
                for (var kind = 0; kind < 3; kind++) Check(counts[kind] == expected[wave, kind], "wave totals " + (wave + 1));
            }
        }
        static void CheckTargetingAndSlow(GameConfig config)
        {
            var game = new GameSession(config); game.Reset();
            game.Spawn(EnemyKind.Common); game.Spawn(EnemyKind.Common);
            game.Enemies[0].Position = new Vector2(-1, 0);
            game.Enemies[1].Position = new Vector2(1, 0);
            game.Enemies[1].Progress = 3;
            Check(CombatSystem.Nearest(game, Vector2.zero, 2) == game.Enemies[1], "tie prioritizes progress");
            Check(CombatSystem.Nearest(game, Vector2.zero, .5f) == null, "range respected");
            game.Sites[0] = new DefenseState(config.Defense(DefenseKind.Web), Vector2.zero);
            game.Sites[1] = new DefenseState(config.Defense(DefenseKind.Web), Vector2.zero) { Upgraded = true };
            new CombatSystem().Tick(game, .01f);
            Check(Mathf.Approximately(game.Enemies[0].Slow, .4f), "strongest slow only");
            game.Enemies[0].Position = new Vector2(4, 4);
            new CombatSystem().Tick(game, .01f);
            Check(game.Enemies[0].Slow == 1, "slow expires outside zone");
            var mover = new EnemyState(99, config.Enemy(EnemyKind.Runner), config.path[0]);
            Check(mover.Move(config.path, 100), "multiple waypoints traversed");
        }
        static void CheckWaveCompletion(GameConfig config)
        {
            var game = new GameSession(config); game.Reset(); game.StartWave();
            for (var i = 0; i < 200 && game.Waves.Remaining > 0; i++) game.Tick(.1f, Vector2.zero);
            Check(game.Phase == GamePhase.Wave && game.Enemies.Count > 0, "await active enemies after final spawn");
            foreach (var enemy in game.Enemies) game.Hit(enemy, 1000, Vector2.zero);
            var beforeBonus = game.Economy.Money;
            game.Tick(.01f, Vector2.zero);
            Check(game.Phase == GamePhase.Preparation && game.Economy.Money == beforeBonus + config.waves[0].bonus, "bonus after wave cleared");
            game.Tick(20, Vector2.zero);
            Check(game.Economy.Money == beforeBonus + config.waves[0].bonus, "bonus awarded only once");
        }
        static GamePhase SimulateMatch(GameConfig config, bool defenses, StringBuilder report)
        {
            var game = new GameSession(config); game.Reset();
            if (defenses) { game.Build(2, DefenseKind.Pumpkin); game.Build(3, DefenseKind.Pumpkin); }
            for (var step = 0; step < 10000 && game.Phase != GamePhase.Defeat && game.Phase != GamePhase.Victory; step++)
            {
                if (game.Phase == GamePhase.Preparation)
                {
                    if (defenses && game.WaveNumber > 0)
                    {
                        if (game.Sites[4] == null) game.Build(4, DefenseKind.Speaker);
                        if (game.Sites[0] == null) game.Build(0, DefenseKind.Web);
                        if (game.Sites[5] == null) game.Build(5, DefenseKind.Pumpkin);
                        for (var site = 2; site < game.Sites.Length; site++) game.Upgrade(site);
                        if (game.Sites[1] == null) game.Build(1, DefenseKind.Pumpkin);
                        game.Upgrade(0); game.Upgrade(1);
                    }
                    report.AppendLine($"- {(defenses ? "Defense strategy" : "Guardian only")}, wave {game.WaveNumber + 1}: {game.Economy.Reserves} reserves, {game.Economy.Money} resources.");
                    game.StartWave();
                }
                EnemyState advanced = null;
                foreach (var enemy in game.Enemies)
                    if (enemy.Active && (advanced == null || enemy.Progress > advanced.Progress)) advanced = enemy;
                var move = advanced == null ? Vector2.zero : Vector2.ClampMagnitude(advanced.Position - game.GuardianPosition, 1);
                game.Tick(.05f, move);
            }
            report.AppendLine($"- Result: {game.Phase}, {game.Economy.Reserves} reserves, {game.CombatSeconds:0.0} seconds of combat.");
            return game.Phase;
        }
    }
}
