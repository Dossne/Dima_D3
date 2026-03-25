using System.Collections.Generic;
using UnityEngine;

namespace TapMiner.Core
{
    public sealed class MissionLayerLiteSystem
    {
        private const string RotationKeyPrefix = "TapMiner.T014.MissionRotation.";
        private const string ProgressKeyPrefix = "TapMiner.T014.MissionProgress.";
        private const string TotalMissionRewardsKey = "TapMiner.T014.TotalMissionRewards";

        private static readonly MissionTemplateId[] TemplateOrder =
        {
            MissionTemplateId.BreakBlocks,
            MissionTemplateId.CompleteSegments,
            MissionTemplateId.EarnSoft
        };

        private static readonly Dictionary<MissionTemplateId, MissionDefinition[]> DefinitionsByTemplate =
            new Dictionary<MissionTemplateId, MissionDefinition[]>
            {
                {
                    MissionTemplateId.BreakBlocks,
                    new[]
                    {
                        new MissionDefinition(MissionTemplateId.BreakBlocks, "Break Blocks", "Break 2 blocks.", 2, 2),
                        new MissionDefinition(MissionTemplateId.BreakBlocks, "Break Blocks", "Break 3 blocks.", 3, 3),
                        new MissionDefinition(MissionTemplateId.BreakBlocks, "Break Blocks", "Break 4 blocks.", 4, 4)
                    }
                },
                {
                    MissionTemplateId.CompleteSegments,
                    new[]
                    {
                        new MissionDefinition(MissionTemplateId.CompleteSegments, "Finish Segments", "Complete 3 segments.", 3, 3),
                        new MissionDefinition(MissionTemplateId.CompleteSegments, "Finish Segments", "Complete 5 segments.", 5, 4),
                        new MissionDefinition(MissionTemplateId.CompleteSegments, "Finish Segments", "Complete 7 segments.", 7, 5)
                    }
                },
                {
                    MissionTemplateId.EarnSoft,
                    new[]
                    {
                        new MissionDefinition(MissionTemplateId.EarnSoft, "Earn Soft", "Earn 12 soft from blocks.", 12, 3),
                        new MissionDefinition(MissionTemplateId.EarnSoft, "Earn Soft", "Earn 18 soft from blocks.", 18, 4),
                        new MissionDefinition(MissionTemplateId.EarnSoft, "Earn Soft", "Earn 24 soft from blocks.", 24, 5)
                    }
                }
            };

        private readonly Dictionary<MissionTemplateId, int> rotationIndices = new Dictionary<MissionTemplateId, int>();
        private readonly Dictionary<MissionTemplateId, MissionProgress> activeMissions = new Dictionary<MissionTemplateId, MissionProgress>();

        public MissionLayerLiteSystem()
        {
            ReloadFromDisk();
        }

        public MissionTemplateId LastCompletedTemplateId { get; private set; }
        public int LastGrantedRewardValue { get; private set; }
        public string LastCompletedMissionDescription { get; private set; } = string.Empty;
        public int TotalMissionRewardsGranted { get; private set; }

        public IReadOnlyDictionary<MissionTemplateId, MissionProgress> ActiveMissions => activeMissions;

        public void ReloadFromDisk()
        {
            rotationIndices.Clear();
            activeMissions.Clear();

            foreach (var templateId in TemplateOrder)
            {
                var definitions = DefinitionsByTemplate[templateId];
                var rotationIndex = Mathf.Clamp(PlayerPrefs.GetInt(GetRotationKey(templateId), 0), 0, definitions.Length - 1);
                var progressValue = Mathf.Max(0, PlayerPrefs.GetInt(GetProgressKey(templateId), 0));
                var definition = definitions[rotationIndex];
                var clampedProgress = Mathf.Min(progressValue, definition.TargetValue);

                rotationIndices[templateId] = rotationIndex;
                activeMissions[templateId] = new MissionProgress(definition, clampedProgress);
            }

            LastCompletedTemplateId = MissionTemplateId.BreakBlocks;
            LastGrantedRewardValue = 0;
            LastCompletedMissionDescription = string.Empty;
            TotalMissionRewardsGranted = Mathf.Max(0, PlayerPrefs.GetInt(TotalMissionRewardsKey, 0));
        }

        public MissionProgress GetMission(MissionTemplateId templateId)
        {
            return activeMissions[templateId];
        }

        public int RecordBreakSuccess()
        {
            return TryAdvanceMission(MissionTemplateId.BreakBlocks, 1);
        }

        public int RecordSegmentCompleted()
        {
            return TryAdvanceMission(MissionTemplateId.CompleteSegments, 1);
        }

        public int RecordSoftEarned(int softValue)
        {
            if (softValue <= 0)
            {
                return 0;
            }

            return TryAdvanceMission(MissionTemplateId.EarnSoft, softValue);
        }

#if UNITY_EDITOR
        public void DebugResetAllProgress()
        {
            foreach (var templateId in TemplateOrder)
            {
                PlayerPrefs.DeleteKey(GetRotationKey(templateId));
                PlayerPrefs.DeleteKey(GetProgressKey(templateId));
            }

            PlayerPrefs.DeleteKey(TotalMissionRewardsKey);
            PlayerPrefs.Save();
            ReloadFromDisk();
        }
#endif

        private int TryAdvanceMission(MissionTemplateId templateId, int amount)
        {
            var currentMission = activeMissions[templateId];
            if (currentMission.IsComplete)
            {
                return 0;
            }

            var updatedValue = Mathf.Min(currentMission.Definition.TargetValue, currentMission.CurrentValue + amount);
            activeMissions[templateId] = new MissionProgress(currentMission.Definition, updatedValue);

            if (updatedValue < currentMission.Definition.TargetValue)
            {
                SaveProgress(templateId);
                return 0;
            }

            var grantedReward = currentMission.Definition.RewardValue;
            LastCompletedTemplateId = templateId;
            LastGrantedRewardValue = grantedReward;
            LastCompletedMissionDescription = currentMission.Definition.Description;
            TotalMissionRewardsGranted += grantedReward;

            var definitions = DefinitionsByTemplate[templateId];
            var nextRotationIndex = (rotationIndices[templateId] + 1) % definitions.Length;
            rotationIndices[templateId] = nextRotationIndex;
            activeMissions[templateId] = new MissionProgress(definitions[nextRotationIndex], 0);

            SaveProgress(templateId);
            PlayerPrefs.SetInt(TotalMissionRewardsKey, TotalMissionRewardsGranted);
            PlayerPrefs.Save();
            return grantedReward;
        }

        private void SaveProgress(MissionTemplateId templateId)
        {
            PlayerPrefs.SetInt(GetRotationKey(templateId), rotationIndices[templateId]);
            PlayerPrefs.SetInt(GetProgressKey(templateId), activeMissions[templateId].CurrentValue);
        }

        private static string GetRotationKey(MissionTemplateId templateId)
        {
            return RotationKeyPrefix + templateId;
        }

        private static string GetProgressKey(MissionTemplateId templateId)
        {
            return ProgressKeyPrefix + templateId;
        }
    }
}
