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
            string playerFacingEffect,
            string openingBandRole,
            params int[] levelCosts)
        {
            Id = id;
            DisplayName = displayName;
            Cap = cap;
            BaseCost = baseCost;
            PlayerFacingEffect = playerFacingEffect;
            OpeningBandRole = openingBandRole;
            LevelCosts = levelCosts;
        }

        public UpgradeId Id { get; }
        public string DisplayName { get; }
        public int Cap { get; }
        public int BaseCost { get; }
        public string PlayerFacingEffect { get; }
        public string OpeningBandRole { get; }
        public int[] LevelCosts { get; }
    }
}
