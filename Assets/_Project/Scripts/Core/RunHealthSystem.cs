namespace TapMiner.Core
{
    public sealed class RunHealthSystem
    {
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }

        public void ResetForRun(int maxHealth)
        {
            MaxHealth = maxHealth < 1 ? 1 : maxHealth;
            CurrentHealth = MaxHealth;
        }

        public bool TryApplyDamage(int damageAmount, bool canApplyDamage, out bool isLethal)
        {
            isLethal = false;

            if (!canApplyDamage || damageAmount <= 0)
            {
                return false;
            }

            CurrentHealth -= damageAmount;
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }

            isLethal = CurrentHealth <= 0;
            return true;
        }
    }
}
