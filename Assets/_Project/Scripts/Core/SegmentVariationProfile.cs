using System;

namespace TapMiner.Core
{
    [Serializable]
    public sealed class SegmentVariationProfile
    {
        public SegmentVariationProfile(
            SegmentVariationId variationId,
            string safePathPresentation,
            string rewardPresentation,
            string hazardPresentation)
        {
            VariationId = variationId;
            SafePathPresentation = safePathPresentation;
            RewardPresentation = rewardPresentation;
            HazardPresentation = hazardPresentation;
        }

        public SegmentVariationId VariationId { get; }
        public string SafePathPresentation { get; }
        public string RewardPresentation { get; }
        public string HazardPresentation { get; }
    }
}
