using System;

namespace TapMiner.Core
{
    [Serializable]
    public sealed class EnemyHazardProfile
    {
        public EnemyHazardProfile(
            EnemyHazardVariantId variantId,
            string behaviorLabel,
            string readabilityNote,
            float telegraphSeconds,
            float repeatSeconds)
        {
            VariantId = variantId;
            BehaviorLabel = behaviorLabel;
            ReadabilityNote = readabilityNote;
            TelegraphSeconds = telegraphSeconds;
            RepeatSeconds = repeatSeconds;
        }

        public EnemyHazardVariantId VariantId { get; }
        public string BehaviorLabel { get; }
        public string ReadabilityNote { get; }
        public float TelegraphSeconds { get; }
        public float RepeatSeconds { get; }
        public bool IsEnemyHazard => VariantId != EnemyHazardVariantId.None;
    }
}
