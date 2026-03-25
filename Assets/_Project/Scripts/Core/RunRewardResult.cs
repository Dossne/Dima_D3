using System;

namespace TapMiner.Core
{
    /// <summary>
    /// Immutable per-run reward summary for downstream runtime use.
    /// </summary>
    [Serializable]
    public sealed class RunRewardResult
    {
        public RunRewardResult(
            int runContextId,
            int totalRewardValue,
            int grantedLootCount,
            int lastGrantedSegmentIndex,
            int lastGrantedLaneIndex)
        {
            RunContextId = runContextId;
            TotalRewardValue = totalRewardValue;
            GrantedLootCount = grantedLootCount;
            LastGrantedSegmentIndex = lastGrantedSegmentIndex;
            LastGrantedLaneIndex = lastGrantedLaneIndex;
        }

        public int RunContextId { get; }
        public int TotalRewardValue { get; }
        public int GrantedLootCount { get; }
        public int LastGrantedSegmentIndex { get; }
        public int LastGrantedLaneIndex { get; }
    }
}
