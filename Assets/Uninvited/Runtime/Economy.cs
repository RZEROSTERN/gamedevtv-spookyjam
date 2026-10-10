using System;

namespace Uninvited
{
    public sealed class Economy
    {
        public int Money { get; private set; }
        public int Reserves { get; private set; }
        public event Action Changed;
        public void Reset(int money, int reserves) { Money = money; Reserves = reserves; Changed?.Invoke(); }
        public bool Spend(int cost)
        {
            if (cost < 0 || Money < cost) return false;
            Money -= cost;
            Changed?.Invoke();
            return true;
        }
        public void Reward(int amount) { Money += Math.Max(0, amount); Changed?.Invoke(); }
        public void Breach(int damage) { Reserves = Math.Max(0, Reserves - damage); Changed?.Invoke(); }
    }
}
