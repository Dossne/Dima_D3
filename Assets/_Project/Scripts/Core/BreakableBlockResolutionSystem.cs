using System;
using System.Collections.Generic;

namespace TapMiner.Core
{
    /// <summary>
    /// Singular runtime authority for deterministic breakable block outcomes.
    /// </summary>
    public sealed class BreakableBlockResolutionSystem
    {
        private readonly List<bool[]> remainingBreakableMasks = new List<bool[]>();
        private IReadOnlyList<SegmentDescriptor> segmentDescriptors = Array.Empty<SegmentDescriptor>();

        public BreakResolutionResult LastResolutionResult { get; private set; }
        public int LastResolvedSegmentIndex { get; private set; } = -1;
        public int LastResolvedLaneIndex { get; private set; } = -1;
        public int SuccessfulBreakCount { get; private set; }

        public void ResetForRun(IReadOnlyList<SegmentDescriptor> spawnedSegments)
        {
            segmentDescriptors = spawnedSegments;
            remainingBreakableMasks.Clear();
            LastResolutionResult = BreakResolutionResult.None;
            LastResolvedSegmentIndex = -1;
            LastResolvedLaneIndex = -1;
            SuccessfulBreakCount = 0;

            for (var segmentIndex = 0; segmentIndex < spawnedSegments.Count; segmentIndex += 1)
            {
                var sourceMask = spawnedSegments[segmentIndex].BreakableLaneMask;
                var runtimeMask = new bool[sourceMask.Length];
                Array.Copy(sourceMask, runtimeMask, sourceMask.Length);
                remainingBreakableMasks.Add(runtimeMask);
            }
        }

        public int GetRemainingBreakableTargetCount(int segmentIndex)
        {
            if (!IsValidSegmentIndex(segmentIndex))
            {
                return 0;
            }

            var count = 0;
            var mask = remainingBreakableMasks[segmentIndex];

            for (var laneIndex = 0; laneIndex < mask.Length; laneIndex += 1)
            {
                if (mask[laneIndex])
                {
                    count += 1;
                }
            }

            return count;
        }

        public bool HasBreakableTargetAt(int segmentIndex, int laneIndex)
        {
            return IsValidSegmentIndex(segmentIndex) &&
                   laneIndex >= 0 &&
                   laneIndex < remainingBreakableMasks[segmentIndex].Length &&
                   remainingBreakableMasks[segmentIndex][laneIndex];
        }

        public BreakResolutionResult TryResolveBreak(
            int segmentIndex,
            int laneIndex,
            int currentLaneIndex,
            bool canResolveBreak)
        {
            if (!canResolveBreak)
            {
                return RecordResult(BreakResolutionResult.RejectedState, segmentIndex, laneIndex);
            }

            if (!IsValidSegmentIndex(segmentIndex))
            {
                return RecordResult(BreakResolutionResult.RejectedInvalidSegment, segmentIndex, laneIndex);
            }

            if (laneIndex != currentLaneIndex)
            {
                return RecordResult(BreakResolutionResult.RejectedLaneMismatch, segmentIndex, laneIndex);
            }

            if (!HasBreakableTargetAt(segmentIndex, laneIndex))
            {
                if (laneIndex >= 0 &&
                    laneIndex < segmentDescriptors[segmentIndex].BreakableLaneMask.Length &&
                    segmentDescriptors[segmentIndex].BreakableLaneMask[laneIndex])
                {
                    return RecordResult(BreakResolutionResult.RejectedAlreadyBroken, segmentIndex, laneIndex);
                }

                return RecordResult(BreakResolutionResult.RejectedNoTarget, segmentIndex, laneIndex);
            }

            remainingBreakableMasks[segmentIndex][laneIndex] = false;
            SuccessfulBreakCount += 1;
            return RecordResult(BreakResolutionResult.BreakSucceeded, segmentIndex, laneIndex);
        }

        private bool IsValidSegmentIndex(int segmentIndex)
        {
            return segmentIndex >= 0 && segmentIndex < remainingBreakableMasks.Count;
        }

        private BreakResolutionResult RecordResult(BreakResolutionResult result, int segmentIndex, int laneIndex)
        {
            LastResolutionResult = result;
            LastResolvedSegmentIndex = segmentIndex;
            LastResolvedLaneIndex = laneIndex;
            return result;
        }
    }
}
