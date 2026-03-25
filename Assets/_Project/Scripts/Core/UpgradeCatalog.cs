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
                "+8% block break speed per level",
                "Cheapest opener for players who want immediate power growth.",
                12, 18, 25, 33, 42, 52, 63, 75, 88, 102),
            new UpgradeDefinition(
                UpgradeId.MaxHp,
                "Max HP",
                3,
                25,
                "+1 HP at each purchased milestone",
                "Pricier survival spike with the clearest defensive payoff.",
                18, 30, 45),
            new UpgradeDefinition(
                UpgradeId.MoveSpeed,
                "Move Speed",
                10,
                20,
                "-4% lane transition time per level",
                "Mobility pick for more readable lane recovery.",
                14, 20, 27, 35, 44, 54, 65, 77, 90, 104),
            new UpgradeDefinition(
                UpgradeId.LootValue,
                "Loot Value",
                10,
                20,
                "+10% soft from blocks per level",
                "Economy pick that trades short-term cost for faster later buying power.",
                16, 22, 29, 37, 46, 56, 67, 79, 92, 106),
            new UpgradeDefinition(
                UpgradeId.CollapseResistance,
                "Collapse Resistance",
                10,
                20,
                "-4% effective collapse catch rate per level",
                "Safety pick that smooths pressure without overtaking Max HP early.",
                14, 20, 27, 35, 44, 54, 65, 77, 90, 104)
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

        public static int GetCostForLevel(UpgradeId id, int currentLevel)
        {
            var definition = GetDefinition(id);
            if (currentLevel < 0 || currentLevel >= definition.Cap)
            {
                return -1;
            }

            return definition.LevelCosts[currentLevel];
        }
    }
}
