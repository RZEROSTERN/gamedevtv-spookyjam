using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uninvited
{
    // Plain simulation: usable by the player, editor verification and future native builds.
    public sealed class GameSession
    {
        public GameConfig Config { get; }
        public Economy Economy { get; } = new Economy();
        public WaveDirector Waves { get; } = new WaveDirector();
        public List<EnemyState> Enemies { get; } = new List<EnemyState>(64);
        public DefenseState[] Sites { get; }
        public Vector2 GuardianPosition { get; private set; }
        public Vector2 GuardianFacing { get; private set; } = Vector2.up;
        public GamePhase Phase { get; private set; } = GamePhase.Menu;
        public int WaveNumber { get; private set; }
        public float CombatSeconds { get; private set; }
        public event Action Changed;
        public event Action Resetting;
        public event Action<EnemyState, Vector2> HitEffect;
        public event Action<Vector2, float> PulseEffect;
        public event Action<EnemyState, bool> EnemyResolved;
        readonly CombatSystem combat = new CombatSystem();
        GamePhase resumePhase;
        int nextId;
        public GameSession(GameConfig config)
        {
            Config = config;
            Sites = new DefenseState[config.buildSites.Length];
            Economy.Changed += Notify;
            Reset(false);
        }
        void Notify() => Changed?.Invoke();
        void SetPhase(GamePhase phase) { Phase = phase; Notify(); }
        public void Reset(bool start = true)
        {
            Resetting?.Invoke();
            Enemies.Clear();
            Array.Clear(Sites, 0, Sites.Length);
            nextId = 0;
            WaveNumber = 0;
            CombatSeconds = 0;
            Waves.Reset();
            combat.Reset();
            GuardianPosition = new Vector2(2, -3.5f);
            GuardianFacing = Vector2.up;
            Phase = start ? GamePhase.Preparation : GamePhase.Menu;
            Economy.Reset(Config.initialMoney, Config.initialReserves);
            Notify();
        }
        public bool Build(int site, DefenseKind kind)
        {
            if (Phase != GamePhase.Preparation || site < 0 || site >= Sites.Length || Sites[site] != null || (int)kind < 0 || (int)kind >= Config.defenses.Length) return false;
            var spec = Config.Defense(kind);
            if (Economy.Money < spec.cost) return false;
            // Install before the money event, so UI sees a consistent purchase.
            Sites[site] = new DefenseState(spec, Config.buildSites[site]);
            Economy.Spend(spec.cost);
            return true;
        }
        public bool Upgrade(int site)
        {
            if (Phase != GamePhase.Preparation || site < 0 || site >= Sites.Length) return false;
            var defense = Sites[site];
            if (defense == null || defense.Upgraded || Economy.Money < defense.Spec.upgradeCost) return false;
            defense.Upgraded = true;
            Economy.Spend(defense.Spec.upgradeCost);
            return true;
        }
        public bool StartWave()
        {
            if (Phase != GamePhase.Preparation || WaveNumber >= Config.waves.Length) return false;
            Waves.Begin(Config.waves[WaveNumber++]);
            SetPhase(GamePhase.Wave);
            return true;
        }
        public void Pause()
        {
            if (Phase != GamePhase.Wave && Phase != GamePhase.Preparation) return;
            resumePhase = Phase;
            SetPhase(GamePhase.Paused);
        }
        public void Resume() { if (Phase == GamePhase.Paused) SetPhase(resumePhase); }
        public void Spawn(EnemyKind kind) => Enemies.Add(new EnemyState(++nextId, Config.Enemy(kind), Config.path[0]));
        public void Hit(EnemyState enemy, float damage, Vector2 source)
        {
            if (!enemy.Active) return;
            enemy.Resistance = Mathf.Max(0, enemy.Resistance - damage);
            HitEffect?.Invoke(enemy, source);
            if (enemy.Resistance <= 0) Resolve(enemy, false);
        }
        public void Pulse(Vector2 center, float radius) => PulseEffect?.Invoke(center, radius);
        public void Resolve(EnemyState enemy, bool breach)
        {
            if (enemy.Resolved) return;
            enemy.Resolved = true;
            if (breach) Economy.Breach(enemy.Spec.reserveDamage);
            else Economy.Reward(enemy.Spec.reward);
            EnemyResolved?.Invoke(enemy, breach);
            if (Economy.Reserves <= 0) SetPhase(GamePhase.Defeat);
        }
        public void Tick(float delta, Vector2 move)
        {
            if (Phase != GamePhase.Preparation && Phase != GamePhase.Wave) return;
            delta = Mathf.Max(0, delta);
            move = Vector2.ClampMagnitude(move, 1);
            if (move.sqrMagnitude > .001f) GuardianFacing = move.normalized;
            GuardianPosition += move * Config.guardianSpeed * delta;
            GuardianPosition = new Vector2(Mathf.Clamp(GuardianPosition.x, -4.5f, 4.5f), Mathf.Clamp(GuardianPosition.y, -4.5f, 5.3f));
            if (Phase != GamePhase.Wave) return;
            CombatSeconds += delta;
            Waves.Tick(this, delta);
            combat.Tick(this, delta);
            foreach (var enemy in Enemies)
            {
                if (Phase != GamePhase.Wave) break;
                if (enemy.Active && enemy.Move(Config.path, delta)) Resolve(enemy, true);
            }
            for (var i = Enemies.Count - 1; i >= 0; i--) if (Enemies[i].Resolved) Enemies.RemoveAt(i);
            if (Phase == GamePhase.Wave && Waves.Remaining == 0 && Enemies.Count == 0)
            {
                Economy.Reward(Config.waves[WaveNumber - 1].bonus);
                SetPhase(WaveNumber == Config.waves.Length ? GamePhase.Victory : GamePhase.Preparation);
            }
        }
    }
}
