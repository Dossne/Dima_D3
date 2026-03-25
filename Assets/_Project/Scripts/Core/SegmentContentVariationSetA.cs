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
                            "Single readable outer hazard",
                            StationaryHazard())
                        : new SegmentVariationProfile(
                            SegmentVariationId.StartSafe_CenterBeacon,
                            "Immediate safe-path beacon",
                            "No reward branch",
                            "Single readable outer hazard",
                            StationaryHazard());

                case SegmentType.S1_StandardChoice:
                    if (!hasRewardPath)
                    {
                        return new SegmentVariationProfile(
                            SegmentVariationId.StandardChoice_ClearSplit,
                            "Clear safe-route anchor",
                            "No reward branch",
                            "Single side threat",
                            StationaryHazard());
                    }

                    return selector == 0
                        ? new SegmentVariationProfile(
                            SegmentVariationId.StandardChoice_RewardSplit,
                            "Anchored safe lane",
                            "Adjacent readable reward split",
                            "Single side threat",
                            LaneCrawler())
                        : new SegmentVariationProfile(
                            SegmentVariationId.StandardChoice_MirrorSplit,
                            "Anchored safe lane",
                            "Mirrored readable reward split",
                            "Offset side threat",
                            StationaryHazard());

                case SegmentType.S2_RewardRisk:
                    return selector == 0
                        ? new SegmentVariationProfile(
                            SegmentVariationId.RewardRisk_HighlightedBait,
                            "Clear safe anchor under reward pressure",
                            "Highlighted risk-reward branch",
                            "Reward-lane hazard lock",
                            LaneCrawler())
                        : new SegmentVariationProfile(
                            SegmentVariationId.RewardRisk_OffsetBait,
                            "Clear safe anchor under reward pressure",
                            "Offset risk-reward branch",
                            "Reward-lane hazard lock",
                            LaneCrawler());

                default:
                    return hasRewardPath
                        ? new SegmentVariationProfile(
                            SegmentVariationId.HazardPressure_RewardWindow,
                            "Safe lane cut through pressure",
                            "Optional reward window inside pressure",
                            "Wide pressure wall",
                            PressurePacer())
                        : new SegmentVariationProfile(
                            SegmentVariationId.HazardPressure_WideWall,
                            "Safe lane cut through pressure",
                            "No reward branch",
                            "Wide pressure wall",
                            PressurePacer());
            }
        }

        private static EnemyHazardProfile StationaryHazard()
        {
            return new EnemyHazardProfile(
                EnemyHazardVariantId.None,
                "Stationary hazard",
                "Static threat; route is fully readable on first look.",
                0f,
                0f);
        }

        private static EnemyHazardProfile LaneCrawler()
        {
            return new EnemyHazardProfile(
                EnemyHazardVariantId.LaneCrawler,
                "Slow lane crawler",
                "Telegraphs before crossing its hazard lane and never enters the safe lane.",
                0.45f,
                1.25f);
        }

        private static EnemyHazardProfile PressurePacer()
        {
            return new EnemyHazardProfile(
                EnemyHazardVariantId.PressurePacer,
                "Pressure pacer pair",
                "Alternates pressure on already-hazard lanes with a long readable tell and no safe-lane intrusion.",
                0.55f,
                1.5f);
        }
    }
}
