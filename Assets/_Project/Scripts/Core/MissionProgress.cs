using System;

namespace TapMiner.Core
{
    [Serializable]
    public sealed class MissionProgress
    {
        public MissionProgress(MissionDefinition definition, int currentValue)
        {
            Definition = definition;
            CurrentValue = currentValue;
        }

        public MissionDefinition Definition { get; }
        public int CurrentValue { get; }
        public bool IsComplete => CurrentValue >= Definition.TargetValue;
    }
}
