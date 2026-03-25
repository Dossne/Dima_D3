namespace TapMiner.Core
{
    public static class SegmentContentVariationSetA
    {
        public static SegmentVariationProfile Resolve(
            int segmentIndex,
            DepthBucket depthBucket,
            SegmentType segmentType,
            int safeLaneIndex,
            bool hasRewardPath)
        {
            var selector = (segmentIndex + (int)depthBucket + safeLaneIndex) % 2;

            switch (segmentType)
            {
                case SegmentType.S0_StartSafe:
                    return hasRewardPath
                        ? new SegmentVariationProfile(
                            SegmentVariationId.StartSafe_RewardPreview,
                            "Center-to-edge safe anchor",
                            "Previewed optional reward split",
                            "Single readable outer hazard")
                        : new SegmentVariationProfile(
                            SegmentVariationId.StartSafe_CenterBeacon,
                            "Immediate safe-path beacon",
                            "No reward branch",
                            "Single readable outer hazard");

                case SegmentType.S1_StandardChoice:
                    if (!hasRewardPath)
                    {
                        return new SegmentVariationProfile(
                            SegmentVariationId.StandardChoice_ClearSplit,
                            "Clear safe-route anchor",
                            "No reward branch",
                            "Single side threat");
                    }

                    return selector == 0
                        ? new SegmentVariationProfile(
                            SegmentVariationId.StandardChoice_RewardSplit,
                            "Anchored safe lane",
                            "Adjacent readable reward split",
                            "Single side threat")
                        : new SegmentVariationProfile(
                            SegmentVariationId.StandardChoice_MirrorSplit,
                            "Anchored safe lane",
                            "Mirrored readable reward split",
                            "Offset side threat");

                case SegmentType.S2_RewardRisk:
                    return selector == 0
                        ? new SegmentVariationProfile(
                            SegmentVariationId.RewardRisk_HighlightedBait,
                            "Clear safe anchor under reward pressure",
                            "Highlighted risk-reward branch",
                            "Reward-lane hazard lock")
                        : new SegmentVariationProfile(
                            SegmentVariationId.RewardRisk_OffsetBait,
                            "Clear safe anchor under reward pressure",
                            "Offset risk-reward branch",
                            "Reward-lane hazard lock");

                default:
                    return hasRewardPath
                        ? new SegmentVariationProfile(
                            SegmentVariationId.HazardPressure_RewardWindow,
                            "Safe lane cut through pressure",
                            "Optional reward window inside pressure",
                            "Wide pressure wall")
                        : new SegmentVariationProfile(
                            SegmentVariationId.HazardPressure_WideWall,
                            "Safe lane cut through pressure",
                            "No reward branch",
                            "Wide pressure wall");
            }
        }
    }
}
