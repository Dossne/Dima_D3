using System;

namespace TapMiner.Core
{
    [Serializable]
    public sealed class MissionDefinition
    {
        public MissionDefinition(
            MissionTemplateId templateId,
            string displayName,
            string description,
            int targetValue,
            int rewardValue)
        {
            TemplateId = templateId;
            DisplayName = displayName;
            Description = description;
            TargetValue = targetValue;
            RewardValue = rewardValue;
        }

        public MissionTemplateId TemplateId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int TargetValue { get; }
        public int RewardValue { get; }
    }
}
