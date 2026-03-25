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
            int safeLaneIndex,
            bool hasRewardPath,
            int rewardLaneIndex,
            bool[] hazardLaneMask)
        {
            SegmentIndex = segmentIndex;
            DepthBucket = depthBucket;
            SegmentType = segmentType;
            SafeLaneIndex = safeLaneIndex;
            HasRewardPath = hasRewardPath;
            RewardLaneIndex = rewardLaneIndex;
            HazardLaneMask = hazardLaneMask;
        }

        public int SegmentIndex { get; }
        public DepthBucket DepthBucket { get; }
        public SegmentType SegmentType { get; }
        public int SafeLaneIndex { get; }
        public bool HasRewardPath { get; }
        public int RewardLaneIndex { get; }
        public bool[] HazardLaneMask { get; }
        public bool HasHazardOnSafeLane => HazardLaneMask[SafeLaneIndex];

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
    }
}
