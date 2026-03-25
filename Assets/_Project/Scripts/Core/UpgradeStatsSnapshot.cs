using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapMiner.Core
{
    [Serializable]
    public sealed class UpgradeStatsSnapshot
    {
        public UpgradeStatsSnapshot(
            float blockBreakSpeedMultiplier,
            int maxHealth,
            float laneTransitionDurationMultiplier,
            float lootValueMultiplier,
            float collapseCatchRateMultiplier)
        {
            BlockBreakSpeedMultiplier = blockBreakSpeedMultiplier;
            MaxHealth = maxHealth;
            LaneTransitionDurationMultiplier = laneTransitionDurationMultiplier;
            LootValueMultiplier = lootValueMultiplier;
            CollapseCatchRateMultiplier = collapseCatchRateMultiplier;
        }

        public float BlockBreakSpeedMultiplier { get; }
        public int MaxHealth { get; }
        public float LaneTransitionDurationMultiplier { get; }
        public float LootValueMultiplier { get; }
        public float CollapseCatchRateMultiplier { get; }

        public static UpgradeStatsSnapshot FromLevels(IReadOnlyDictionary<UpgradeId, int> levels)
        {
            var drillPowerLevel = ReadLevel(levels, UpgradeId.DrillPower);
            var maxHpLevel = ReadLevel(levels, UpgradeId.MaxHp);
            var moveSpeedLevel = ReadLevel(levels, UpgradeId.MoveSpeed);
            var lootValueLevel = ReadLevel(levels, UpgradeId.LootValue);
            var collapseResistanceLevel = ReadLevel(levels, UpgradeId.CollapseResistance);

            return new UpgradeStatsSnapshot(
                1f + (0.08f * drillPowerLevel),
                1 + maxHpLevel,
                Mathf.Max(0.1f, 1f - (0.04f * moveSpeedLevel)),
                1f + (0.10f * lootValueLevel),
                Mathf.Max(0.1f, 1f - (0.04f * collapseResistanceLevel)));
        }

        private static int ReadLevel(IReadOnlyDictionary<UpgradeId, int> levels, UpgradeId id)
        {
            return levels.TryGetValue(id, out var level) ? level : 0;
        }
    }
}
