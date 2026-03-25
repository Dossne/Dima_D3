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
            string hazardPresentation,
            EnemyHazardProfile enemyHazardProfile)
        {
            VariationId = variationId;
            SafePathPresentation = safePathPresentation;
            RewardPresentation = rewardPresentation;
            HazardPresentation = hazardPresentation;
            EnemyHazardProfile = enemyHazardProfile;
        }

        public SegmentVariationId VariationId { get; }
        public string SafePathPresentation { get; }
        public string RewardPresentation { get; }
        public string HazardPresentation { get; }
        public EnemyHazardProfile EnemyHazardProfile { get; }
    }
}
