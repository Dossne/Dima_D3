using System;

namespace TapMiner.Core
{
    /// <summary>
    /// Immutable runtime loot outcome tied to a specific run context and break event.
    /// </summary>
    [Serializable]
    public sealed class LootDropRecord
    {
        public LootDropRecord(int runContextId, int segmentIndex, int laneIndex, int lootValue)
        {
            RunContextId = runContextId;
            SegmentIndex = segmentIndex;
            LaneIndex = laneIndex;
            LootValue = lootValue;
        }

        public int RunContextId { get; }
        public int SegmentIndex { get; }
        public int LaneIndex { get; }
        public int LootValue { get; }
    }
}
