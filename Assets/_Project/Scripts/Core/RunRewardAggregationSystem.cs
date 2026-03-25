namespace TapMiner.Core
{
    /// <summary>
    /// Singular runtime authority for deterministic per-run reward aggregation.
    /// </summary>
    public sealed class RunRewardAggregationSystem
    {
        public int ActiveRunContextId { get; private set; }
        public RunRewardResult CurrentRewardResult { get; private set; } = new RunRewardResult(0, 0, 0, -1, -1);

        public void ResetForRun(int runContextId)
        {
            ActiveRunContextId = runContextId;
            CurrentRewardResult = new RunRewardResult(runContextId, 0, 0, -1, -1);
        }

        public bool TryAggregateLoot(LootResolutionResult lootResult, LootDropRecord grantedLoot)
        {
            if (lootResult != LootResolutionResult.LootGranted || grantedLoot == null)
            {
                return false;
            }

            if (grantedLoot.RunContextId != ActiveRunContextId)
            {
                return false;
            }

            CurrentRewardResult = new RunRewardResult(
                ActiveRunContextId,
                CurrentRewardResult.TotalRewardValue + grantedLoot.LootValue,
                CurrentRewardResult.GrantedLootCount + 1,
                grantedLoot.SegmentIndex,
                grantedLoot.LaneIndex);

            return true;
        }
    }
}
