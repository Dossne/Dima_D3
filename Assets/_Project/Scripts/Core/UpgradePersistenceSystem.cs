using System.Collections.Generic;
using UnityEngine;

namespace TapMiner.Core
{
    public sealed class UpgradePersistenceSystem
    {
        private const string BalanceKey = "TapMiner.T012.SoftBalance";
        private const string UpgradeKeyPrefix = "TapMiner.T012.Upgrade.";

        private readonly Dictionary<UpgradeId, int> levels = new Dictionary<UpgradeId, int>();

        public UpgradePersistenceSystem()
        {
            ReloadFromDisk();
        }

        public int SoftCurrencyBalance { get; private set; }
        public UpgradeStatsSnapshot CurrentStats { get; private set; } =
            UpgradeStatsSnapshot.FromLevels(new Dictionary<UpgradeId, int>());
        public IReadOnlyDictionary<UpgradeId, int> Levels => levels;

        public void ReloadFromDisk()
        {
            levels.Clear();

            foreach (var definition in UpgradeCatalog.All)
            {
                var savedLevel = PlayerPrefs.GetInt(GetUpgradeLevelKey(definition.Id), 0);
                levels[definition.Id] = Mathf.Clamp(savedLevel, 0, definition.Cap);
            }

            SoftCurrencyBalance = Mathf.Max(0, PlayerPrefs.GetInt(BalanceKey, 0));
            CurrentStats = UpgradeStatsSnapshot.FromLevels(levels);
        }

        public int GetLevel(UpgradeId id)
        {
            return levels.TryGetValue(id, out var level) ? level : 0;
        }

        public int GetNextPurchaseCost(UpgradeId id)
        {
            return UpgradeCatalog.GetDefinition(id).BaseCost;
        }

        public bool TryPurchase(UpgradeId id)
        {
            var definition = UpgradeCatalog.GetDefinition(id);
            var currentLevel = GetLevel(id);
            if (currentLevel >= definition.Cap)
            {
                return false;
            }

            var cost = GetNextPurchaseCost(id);
            if (SoftCurrencyBalance < cost)
            {
                return false;
            }

            SoftCurrencyBalance -= cost;
            levels[id] = currentLevel + 1;
            Save();
            return true;
        }

        public void BankReward(int rewardValue)
        {
            if (rewardValue <= 0)
            {
                return;
            }

            SoftCurrencyBalance += rewardValue;
            Save();
        }

        public void Save()
        {
            PlayerPrefs.SetInt(BalanceKey, SoftCurrencyBalance);

            foreach (var entry in levels)
            {
                PlayerPrefs.SetInt(GetUpgradeLevelKey(entry.Key), entry.Value);
            }

            PlayerPrefs.Save();
            CurrentStats = UpgradeStatsSnapshot.FromLevels(levels);
        }

#if UNITY_EDITOR
        public void DebugResetAllProgress()
        {
            PlayerPrefs.DeleteKey(BalanceKey);

            foreach (var definition in UpgradeCatalog.All)
            {
                PlayerPrefs.DeleteKey(GetUpgradeLevelKey(definition.Id));
            }

            PlayerPrefs.Save();
            ReloadFromDisk();
        }

        public void DebugGrantSoftCurrency(int amount)
        {
            SoftCurrencyBalance = Mathf.Max(0, SoftCurrencyBalance + amount);
            Save();
        }
#endif

        private static string GetUpgradeLevelKey(UpgradeId id)
        {
            return UpgradeKeyPrefix + id;
        }
    }
}
