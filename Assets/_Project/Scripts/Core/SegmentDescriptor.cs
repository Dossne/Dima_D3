using System;

namespace TapMiner.Core
{
    /// <summary>
    /// Immutable runtime description for a spawned segment.
    /// </summary>
    [Serializable]
    public sealed class SegmentDescriptor
    {
        public SegmentDescriptor(
            int segmentIndex,
            DepthBucket depthBucket,
            SegmentType segmentType,
            SegmentVariationId variationId,
            string safePathPresentation,
            string rewardPresentation,
            string hazardPresentation,
            int safeLaneIndex,
            bool hasRewardPath,
            int rewardLaneIndex,
            bool[] hazardLaneMask,
            bool[] breakableLaneMask)
        {
            SegmentIndex = segmentIndex;
            DepthBucket = depthBucket;
            SegmentType = segmentType;
            VariationId = variationId;
            SafePathPresentation = safePathPresentation;
            RewardPresentation = rewardPresentation;
            HazardPresentation = hazardPresentation;
            SafeLaneIndex = safeLaneIndex;
            HasRewardPath = hasRewardPath;
            RewardLaneIndex = rewardLaneIndex;
            HazardLaneMask = hazardLaneMask;
            BreakableLaneMask = breakableLaneMask;
        }

        public int SegmentIndex { get; }
        public DepthBucket DepthBucket { get; }
        public SegmentType SegmentType { get; }
        public SegmentVariationId VariationId { get; }
        public string SafePathPresentation { get; }
        public string RewardPresentation { get; }
        public string HazardPresentation { get; }
        public int SafeLaneIndex { get; }
        public bool HasRewardPath { get; }
        public int RewardLaneIndex { get; }
        public bool[] HazardLaneMask { get; }
        public bool[] BreakableLaneMask { get; }
        public bool HasHazardOnSafeLane => HazardLaneMask[SafeLaneIndex];
        public bool HasBreakableOnSafeLane => BreakableLaneMask[SafeLaneIndex];

        public int HazardClusterCount
        {
            get
            {
                var clusters = 0;
                var wasPreviousHazard = false;

                for (var laneIndex = 0; laneIndex < HazardLaneMask.Length; laneIndex += 1)
                {
                    var isHazard = HazardLaneMask[laneIndex];
                    if (isHazard && !wasPreviousHazard)
                    {
                        clusters += 1;
                    }

                    wasPreviousHazard = isHazard;
                }

                return clusters;
            }
        }

        public int BreakableTargetCount
        {
            get
            {
                var count = 0;

                for (var laneIndex = 0; laneIndex < BreakableLaneMask.Length; laneIndex += 1)
                {
                    if (BreakableLaneMask[laneIndex])
                    {
                        count += 1;
                    }
                }

                return count;
            }
        }
    }
}
