using System.Collections.Generic;

namespace TapMiner.Core
{
    /// <summary>
    /// Singular runtime authority for deterministic loot outcomes from legal break results.
    /// </summary>
    public sealed class LootDropResolutionSystem
    {
        private readonly List<LootDropRecord> grantedLoot = new List<LootDropRecord>();

        public LootResolutionResult LastResolutionResult { get; private set; }
        public LootDropRecord LastGrantedLoot { get; private set; }
        public int TotalGrantedLootValue { get; private set; }
        public int SuccessfulLootDropCount { get; private set; }

        public void ResetForRun(int runContextId)
        {
            ActiveRunContextId = runContextId;
            grantedLoot.Clear();
            LastResolutionResult = LootResolutionResult.None;
            LastGrantedLoot = null;
            TotalGrantedLootValue = 0;
            SuccessfulLootDropCount = 0;
        }

        public int ActiveRunContextId { get; private set; }
        public IReadOnlyList<LootDropRecord> GrantedLoot => grantedLoot;

        public LootResolutionResult TryResolveLoot(
            BreakResolutionResult breakResult,
            int runContextId,
            int segmentIndex,
            int laneIndex)
        {
            if (runContextId != ActiveRunContextId)
            {
                return RecordResult(LootResolutionResult.RejectedRunContextMismatch, null);
            }

            if (segmentIndex < 0)
            {
                return RecordResult(LootResolutionResult.RejectedInvalidSegment, null);
            }

            if (breakResult != BreakResolutionResult.BreakSucceeded)
            {
                return RecordResult(LootResolutionResult.RejectedNonLootBreakResult, null);
            }

            var lootValue = ResolveLootValue(segmentIndex, laneIndex);
            var record = new LootDropRecord(runContextId, segmentIndex, laneIndex, lootValue);
            grantedLoot.Add(record);
            LastGrantedLoot = record;
            LastResolutionResult = LootResolutionResult.LootGranted;
            TotalGrantedLootValue += lootValue;
            SuccessfulLootDropCount += 1;
            return LastResolutionResult;
        }

        private static int ResolveLootValue(int segmentIndex, int laneIndex)
        {
            return 1 + ((segmentIndex + laneIndex) % 3);
        }

        private LootResolutionResult RecordResult(LootResolutionResult result, LootDropRecord record)
        {
            LastResolutionResult = result;
            LastGrantedLoot = record;
            return result;
        }
    }
}
