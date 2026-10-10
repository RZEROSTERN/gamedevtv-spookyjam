namespace Uninvited
{
    public sealed class WaveDirector
    {
        WaveSpec wave;
        float timer;
        int cursor;
        public int Remaining => wave == null ? 0 : wave.sequence.Length - cursor;
        public void Begin(WaveSpec spec) { wave = spec; timer = 0; cursor = 0; }
        public void Reset() { wave = null; timer = 0; cursor = 0; }
        public void Tick(GameSession game, float delta)
        {
            if (wave == null) return;
            timer -= delta;
            while (Remaining > 0 && timer <= 0)
            {
                game.Spawn(wave.sequence[cursor++]);
                timer += UnityEngine.Mathf.Max(.1f, wave.spawnInterval);
            }
        }
    }
}
