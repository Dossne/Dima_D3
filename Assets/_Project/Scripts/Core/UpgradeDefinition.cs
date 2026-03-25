using System;

namespace TapMiner.Core
{
    [Serializable]
    public sealed class UpgradeDefinition
    {
        public UpgradeDefinition(
            UpgradeId id,
            string displayName,
            int cap,
            int baseCost,
            string playerFacingEffect)
        {
            Id = id;
            DisplayName = displayName;
            Cap = cap;
            BaseCost = baseCost;
            PlayerFacingEffect = playerFacingEffect;
        }

        public UpgradeId Id { get; }
        public string DisplayName { get; }
        public int Cap { get; }
        public int BaseCost { get; }
        public string PlayerFacingEffect { get; }
    }
}
