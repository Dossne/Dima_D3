using System;
using System.Collections.Generic;

namespace TapMiner.Core
{
    [Serializable]
    public sealed class PlaytestTelemetryEvent
    {
        public string EventType = string.Empty;
        public int RunContextId;
        public float SessionTimeSeconds;
        public int Depth;
        public int SegmentIndex = -1;
        public int LaneIndex = -1;
        public int PreviousLaneIndex = -1;
        public int RewardDelta;
        public int RewardTotal;
        public int RemainingHealth = -1;
        public int MaxHealth = -1;
        public int RestartTargetRunContextId = -1;
        public float RunDurationSeconds = -1f;
        public float RestartLatencySeconds = -1f;
        public string DeathCause = string.Empty;
        public string Detail = string.Empty;
    }

    [Serializable]
    public sealed class PlaytestRunSummary
    {
        public int RunContextId;
        public float StartedAtSessionSeconds;
        public float EndedAtSessionSeconds;
        public float DurationSeconds;
        public int Depth;
        public string DeathCause = string.Empty;
        public int TotalRewardsGranted;
        public float RestartLatencySeconds = -1f;
    }

    [Serializable]
    public sealed class PlaytestSessionLog
    {
        public string SessionId = string.Empty;
        public string SchemaVersion = string.Empty;
        public string GeneratedAtUtc = string.Empty;
        public int EventCount;
        public List<PlaytestTelemetryEvent> Events = new List<PlaytestTelemetryEvent>();
        public List<PlaytestRunSummary> CompletedRuns = new List<PlaytestRunSummary>();
    }

    [Serializable]
    public sealed class PlaytestReportRecord
    {
        public string ReportId = string.Empty;
        public string SessionId = string.Empty;
        public string GeneratedAtUtc = string.Empty;
        public int RunsObserved;
        public int FirstRunDepth;
        public float FirstRunDurationSeconds;
        public string FirstRunDeathCause = string.Empty;
        public int FirstRunRewards;
        public float FirstRunRestartLatencySeconds = -1f;
        public int LaneInputsObserved;
        public int BlocksBrokenObserved;
        public int DamageEventsObserved;
        public int UpgradePurchasesObserved;
        public int MissionCompletionsObserved;
        public string ClarityQuestion = string.Empty;
        public string FairnessQuestion = string.Empty;
        public string RewardQuestion = string.Empty;
        public string RestartIntentQuestion = string.Empty;
    }

    [Serializable]
    public sealed class PlaytestSchemaValidationResult
    {
        public bool IsValid;
        public List<string> MissingFields = new List<string>();
    }
}
