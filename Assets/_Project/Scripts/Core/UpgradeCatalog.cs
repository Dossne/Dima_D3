using System;
using System.Collections.Generic;

namespace TapMiner.Core
{
    public static class UpgradeCatalog
    {
        private static readonly UpgradeDefinition[] Definitions =
        {
            new UpgradeDefinition(
                UpgradeId.DrillPower,
                "Drill Power",
                10,
                15,
                "+8% block break speed per level"),
            new UpgradeDefinition(
                UpgradeId.MaxHp,
                "Max HP",
                3,
                25,
                "+1 HP at each purchased milestone"),
            new UpgradeDefinition(
                UpgradeId.MoveSpeed,
                "Move Speed",
                10,
                20,
                "-4% lane transition time per level"),
            new UpgradeDefinition(
                UpgradeId.LootValue,
                "Loot Value",
                10,
                20,
                "+10% soft from blocks per level"),
            new UpgradeDefinition(
                UpgradeId.CollapseResistance,
                "Collapse Resistance",
                10,
                20,
                "-4% effective collapse catch rate per level")
        };

        public static IReadOnlyList<UpgradeDefinition> All => Definitions;

        public static UpgradeDefinition GetDefinition(UpgradeId id)
        {
            var index = (int)id;
            if (index < 0 || index >= Definitions.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown upgrade id.");
            }

            return Definitions[index];
        }
    }
}
