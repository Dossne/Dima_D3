using System;
using System.Collections.Generic;

namespace TapMiner.Core
{
    /// <summary>
    /// Singular runtime authority for deterministic hazard-contact outcomes.
    /// </summary>
    public sealed class HazardContactResolutionSystem
    {
        private IReadOnlyList<SegmentDescriptor> segmentDescriptors = Array.Empty<SegmentDescriptor>();

        public int ActiveRunContextId { get; private set; }
        public HazardContactResult LastHazardContactResult { get; private set; }
        public int LastHazardSegmentIndex { get; private set; } = -1;
        public int LastHazardLaneIndex { get; private set; } = -1;
        public int SuccessfulHazardContactCount { get; private set; }

        public void ResetForRun(int runContextId, IReadOnlyList<SegmentDescriptor> spawnedSegments)
        {
            ActiveRunContextId = runContextId;
            segmentDescriptors = spawnedSegments;
            LastHazardContactResult = HazardContactResult.None;
            LastHazardSegmentIndex = -1;
            LastHazardLaneIndex = -1;
            SuccessfulHazardContactCount = 0;
        }

        public int GetHazardTargetCount(int segmentIndex)
        {
            if (!IsValidSegmentIndex(segmentIndex))
            {
                return 0;
            }

            var count = 0;
            var mask = segmentDescriptors[segmentIndex].HazardLaneMask;

            for (var laneIndex = 0; laneIndex < mask.Length; laneIndex += 1)
            {
                if (mask[laneIndex])
                {
                    count += 1;
                }
            }

            return count;
        }

        public HazardContactResult TryResolveHazardContact(
            int runContextId,
            int segmentIndex,
            int laneIndex,
            int currentLaneIndex,
            bool canResolveHazard)
        {
            if (runContextId != ActiveRunContextId)
            {
                return RecordResult(HazardContactResult.RejectedRunContextMismatch, segmentIndex, laneIndex);
            }

            if (!canResolveHazard)
            {
                return RecordResult(HazardContactResult.RejectedState, segmentIndex, laneIndex);
            }

            if (!IsValidSegmentIndex(segmentIndex))
            {
                return RecordResult(HazardContactResult.RejectedInvalidSegment, segmentIndex, laneIndex);
            }

            if (laneIndex != currentLaneIndex)
            {
                return RecordResult(HazardContactResult.RejectedLaneMismatch, segmentIndex, laneIndex);
            }

            if (!segmentDescriptors[segmentIndex].HazardLaneMask[laneIndex])
            {
                return RecordResult(HazardContactResult.RejectedNoHazard, segmentIndex, laneIndex);
            }

            SuccessfulHazardContactCount += 1;
            return RecordResult(HazardContactResult.HazardContactResolved, segmentIndex, laneIndex);
        }

        private bool IsValidSegmentIndex(int segmentIndex)
        {
            return segmentIndex >= 0 && segmentIndex < segmentDescriptors.Count;
        }

        private HazardContactResult RecordResult(HazardContactResult result, int segmentIndex, int laneIndex)
        {
            LastHazardContactResult = result;
            LastHazardSegmentIndex = segmentIndex;
            LastHazardLaneIndex = laneIndex;
            return result;
        }
    }
}
