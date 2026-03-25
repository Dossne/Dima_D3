using System;
using System.Collections.Generic;

namespace TapMiner.Core
{
    /// <summary>
    /// Single runtime authority for legal segment sequence creation under T002.
    /// </summary>
    public sealed class SegmentSpawnSystem
    {
        private readonly int totalSegmentCount;
        private readonly List<SegmentDescriptor> spawnedSegments = new List<SegmentDescriptor>();

        public SegmentSpawnSystem(int totalSegmentCount)
        {
            this.totalSegmentCount = Math.Max(1, totalSegmentCount);
        }

        public IReadOnlyList<SegmentDescriptor> SpawnedSegments => spawnedSegments;

        public void ResetForRun()
        {
            spawnedSegments.Clear();

            for (var segmentIndex = 0; segmentIndex < totalSegmentCount; segmentIndex += 1)
            {
                var descriptor = CreateSegmentDescriptor(segmentIndex);
                ValidateSegmentDescriptor(descriptor);

                spawnedSegments.Add(descriptor);
                ValidateSequencePrefix(spawnedSegments);
            }
        }

        private SegmentDescriptor CreateSegmentDescriptor(int segmentIndex)
        {
            var bucket = ResolveDepthBucket(segmentIndex);
            var segmentType = ResolveSegmentType(segmentIndex, bucket);
            var safeLaneIndex = ResolveSafeLaneIndex(segmentIndex);
            var hasRewardPath = ResolveRewardPath(segmentIndex, segmentType);
            var rewardLaneIndex = hasRewardPath ? ResolveRewardLaneIndex(segmentIndex, safeLaneIndex) : -1;
            var hazardMask = ResolveHazardLaneMask(segmentType, safeLaneIndex, hasRewardPath, rewardLaneIndex);
            var breakableMask = ResolveBreakableLaneMask(hazardMask, safeLaneIndex);
            var variationProfile = SegmentContentVariationSetA.Resolve(
                segmentIndex,
                bucket,
                segmentType,
                safeLaneIndex,
                hasRewardPath);

            return new SegmentDescriptor(
                segmentIndex,
                bucket,
                segmentType,
                variationProfile.VariationId,
                variationProfile.SafePathPresentation,
                variationProfile.RewardPresentation,
                variationProfile.HazardPresentation,
                safeLaneIndex,
                hasRewardPath,
                rewardLaneIndex,
                hazardMask,
                breakableMask);
        }

        public int GetUniqueVariationCount()
        {
            var uniqueVariations = new HashSet<SegmentVariationId>();

            for (var index = 0; index < spawnedSegments.Count; index += 1)
            {
                uniqueVariations.Add(spawnedSegments[index].VariationId);
            }

            return uniqueVariations.Count;
        }

        private static DepthBucket ResolveDepthBucket(int segmentIndex)
        {
            if (segmentIndex <= 1)
            {
                return DepthBucket.D0_Intro;
            }

            if (segmentIndex <= 4)
            {
                return DepthBucket.D1_Early;
            }

            if (segmentIndex <= 7)
            {
                return DepthBucket.D2_Mid;
            }

            return DepthBucket.D3_Late;
        }

        private static SegmentType ResolveSegmentType(int segmentIndex, DepthBucket bucket)
        {
            if (segmentIndex == 0)
            {
                return SegmentType.S0_StartSafe;
            }

            switch (bucket)
            {
                case DepthBucket.D0_Intro:
                    return segmentIndex % 2 == 0 ? SegmentType.S0_StartSafe : SegmentType.S1_StandardChoice;

                case DepthBucket.D1_Early:
                    switch ((segmentIndex - 2) % 3)
                    {
                        case 0:
                            return SegmentType.S1_StandardChoice;
                        case 1:
                            return SegmentType.S0_StartSafe;
                        default:
                            return SegmentType.S2_RewardRisk;
                    }

                case DepthBucket.D2_Mid:
                    switch ((segmentIndex - 5) % 4)
                    {
                        case 0:
                            return SegmentType.S1_StandardChoice;
                        case 1:
                            return SegmentType.S3_HazardPressure;
                        case 2:
                            return SegmentType.S1_StandardChoice;
                        default:
                            return SegmentType.S2_RewardRisk;
                    }

                default:
                    switch ((segmentIndex - 8) % 5)
                    {
                        case 0:
                            return SegmentType.S2_RewardRisk;
                        case 1:
                            return SegmentType.S3_HazardPressure;
                        case 2:
                            return SegmentType.S1_StandardChoice;
                        case 3:
                            return SegmentType.S3_HazardPressure;
                        default:
                            return SegmentType.S1_StandardChoice;
                    }
            }
        }

        private static int ResolveSafeLaneIndex(int segmentIndex)
        {
            int[] pattern = { 1, 0, 2, 1, 2, 0 };
            return pattern[segmentIndex % pattern.Length];
        }

        private static bool ResolveRewardPath(int segmentIndex, SegmentType segmentType)
        {
            switch (segmentType)
            {
                case SegmentType.S0_StartSafe:
                    return segmentIndex > 0 && segmentIndex % 2 == 0;
                case SegmentType.S1_StandardChoice:
                    return segmentIndex % 2 == 1;
                case SegmentType.S2_RewardRisk:
                    return true;
                case SegmentType.S3_HazardPressure:
                    return segmentIndex % 4 == 0;
                default:
                    return false;
            }
        }

        private static int ResolveRewardLaneIndex(int segmentIndex, int safeLaneIndex)
        {
            if (safeLaneIndex == 1)
            {
                return segmentIndex % 2 == 0 ? 0 : 2;
            }

            return 1;
        }

        private static bool[] ResolveHazardLaneMask(
            SegmentType segmentType,
            int safeLaneIndex,
            bool hasRewardPath,
            int rewardLaneIndex)
        {
            var hazardMask = new bool[3];

            switch (segmentType)
            {
                case SegmentType.S0_StartSafe:
                    ApplySingleHazardCluster(hazardMask, safeLaneIndex, rewardLaneIndex);
                    break;

                case SegmentType.S1_StandardChoice:
                    ApplySingleHazardCluster(hazardMask, safeLaneIndex, rewardLaneIndex);
                    break;

                case SegmentType.S2_RewardRisk:
                    if (hasRewardPath)
                    {
                        hazardMask[rewardLaneIndex] = true;
                    }
                    break;

                case SegmentType.S3_HazardPressure:
                    for (var laneIndex = 0; laneIndex < hazardMask.Length; laneIndex += 1)
                    {
                        if (laneIndex != safeLaneIndex)
                        {
                            hazardMask[laneIndex] = true;
                        }
                    }
                    break;
            }

            hazardMask[safeLaneIndex] = false;
            return hazardMask;
        }

        private static bool[] ResolveBreakableLaneMask(bool[] hazardMask, int safeLaneIndex)
        {
            var breakableMask = new bool[hazardMask.Length];

            for (var laneIndex = 0; laneIndex < hazardMask.Length; laneIndex += 1)
            {
                breakableMask[laneIndex] = laneIndex != safeLaneIndex && hazardMask[laneIndex];
            }

            return breakableMask;
        }

        private static void ApplySingleHazardCluster(bool[] hazardMask, int safeLaneIndex, int rewardLaneIndex)
        {
            for (var laneIndex = 0; laneIndex < hazardMask.Length; laneIndex += 1)
            {
                if (laneIndex == safeLaneIndex || laneIndex == rewardLaneIndex)
                {
                    continue;
                }

                hazardMask[laneIndex] = true;
                return;
            }
        }

        private static void ValidateSegmentDescriptor(SegmentDescriptor descriptor)
        {
            if (descriptor.SafeLaneIndex < 0 || descriptor.SafeLaneIndex >= descriptor.HazardLaneMask.Length)
            {
                throw new InvalidOperationException("Safe lane index is out of bounds.");
            }

            if (descriptor.HasHazardOnSafeLane)
            {
                throw new InvalidOperationException("Spawned segment violates safe path guarantee.");
            }

            if (descriptor.HasBreakableOnSafeLane)
            {
                throw new InvalidOperationException("Breakable target cannot exist on the safe lane.");
            }

            if (descriptor.HasRewardPath)
            {
                if (descriptor.RewardLaneIndex < 0 || descriptor.RewardLaneIndex >= descriptor.HazardLaneMask.Length)
                {
                    throw new InvalidOperationException("Reward lane index is out of bounds.");
                }

                if (descriptor.RewardLaneIndex == descriptor.SafeLaneIndex)
                {
                    throw new InvalidOperationException("Reward path cannot be identical to the safe path.");
                }
            }
            else if (descriptor.RewardLaneIndex != -1)
            {
                throw new InvalidOperationException("Reward lane must be unset when no reward path exists.");
            }

            switch (descriptor.SegmentType)
            {
                case SegmentType.S0_StartSafe:
                    if (descriptor.HazardClusterCount > 1)
                    {
                        throw new InvalidOperationException("S0_StartSafe cannot spawn with more than one hazard cluster.");
                    }
                    break;

                case SegmentType.S1_StandardChoice:
                    break;

                case SegmentType.S2_RewardRisk:
                    if (!descriptor.HasRewardPath)
                    {
                        throw new InvalidOperationException("S2_RewardRisk must always include a reward path.");
                    }
                    break;

                case SegmentType.S3_HazardPressure:
                    break;
            }

            for (var laneIndex = 0; laneIndex < descriptor.BreakableLaneMask.Length; laneIndex += 1)
            {
                if (descriptor.BreakableLaneMask[laneIndex] && !descriptor.HazardLaneMask[laneIndex])
                {
                    throw new InvalidOperationException("Breakable target must map to a legal hazard lane.");
                }
            }
        }

        private static void ValidateSequencePrefix(IReadOnlyList<SegmentDescriptor> descriptors)
        {
            for (var index = 0; index < descriptors.Count; index += 1)
            {
                var descriptor = descriptors[index];

                if (index == 0 && descriptor.SegmentType != SegmentType.S0_StartSafe)
                {
                    throw new InvalidOperationException("First segment must be S0_StartSafe.");
                }

                if (!IsTypeAllowedByBucket(descriptor.SegmentType, descriptor.DepthBucket))
                {
                    throw new InvalidOperationException("Segment type is not legal for its depth bucket.");
                }

                if (descriptor.DepthBucket == DepthBucket.D1_Early && index >= 2)
                {
                    var hasSafeRecoveryType =
                        descriptors[index].SegmentType == SegmentType.S0_StartSafe || descriptors[index].SegmentType == SegmentType.S1_StandardChoice ||
                        descriptors[index - 1].SegmentType == SegmentType.S0_StartSafe || descriptors[index - 1].SegmentType == SegmentType.S1_StandardChoice ||
                        descriptors[index - 2].SegmentType == SegmentType.S0_StartSafe || descriptors[index - 2].SegmentType == SegmentType.S1_StandardChoice;

                    if (!hasSafeRecoveryType)
                    {
                        throw new InvalidOperationException("D1 three-segment window violated required safe recovery type.");
                    }
                }

                if (descriptor.DepthBucket == DepthBucket.D2_Mid && index > 0)
                {
                    if (descriptor.SegmentType == SegmentType.S3_HazardPressure &&
                        descriptors[index - 1].SegmentType == SegmentType.S3_HazardPressure)
                    {
                        throw new InvalidOperationException("D2 cannot spawn consecutive S3_HazardPressure segments.");
                    }
                }

                if (descriptor.DepthBucket == DepthBucket.D3_Late && index >= 2)
                {
                    if (descriptors[index].SegmentType == SegmentType.S3_HazardPressure &&
                        descriptors[index - 1].SegmentType == SegmentType.S3_HazardPressure &&
                        descriptors[index - 2].SegmentType == SegmentType.S3_HazardPressure)
                    {
                        throw new InvalidOperationException("D3 cannot spawn three consecutive S3_HazardPressure segments.");
                    }
                }
            }
        }

        private static bool IsTypeAllowedByBucket(SegmentType segmentType, DepthBucket bucket)
        {
            switch (bucket)
            {
                case DepthBucket.D0_Intro:
                    return segmentType == SegmentType.S0_StartSafe || segmentType == SegmentType.S1_StandardChoice;
                case DepthBucket.D1_Early:
                    return segmentType == SegmentType.S0_StartSafe ||
                           segmentType == SegmentType.S1_StandardChoice ||
                           segmentType == SegmentType.S2_RewardRisk;
                case DepthBucket.D2_Mid:
                case DepthBucket.D3_Late:
                    return segmentType == SegmentType.S1_StandardChoice ||
                           segmentType == SegmentType.S2_RewardRisk ||
                           segmentType == SegmentType.S3_HazardPressure;
                default:
                    return false;
            }
        }
    }
}
