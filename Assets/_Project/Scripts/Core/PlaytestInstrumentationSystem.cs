using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace TapMiner.Core
{
    public sealed class PlaytestInstrumentationSystem
    {
        private const string SchemaVersion = "T018.v1";

        private readonly List<PlaytestTelemetryEvent> events = new List<PlaytestTelemetryEvent>();
        private readonly List<PlaytestRunSummary> completedRuns = new List<PlaytestRunSummary>();
        private readonly Dictionary<int, float> runStartTimes = new Dictionary<int, float>();
        private readonly Dictionary<int, int> maxDepthReached = new Dictionary<int, int>();
        private readonly Dictionary<int, PlaytestDeathCause> deathCauses = new Dictionary<int, PlaytestDeathCause>();
        private readonly Dictionary<int, float> deathTimes = new Dictionary<int, float>();

        private float sessionStartTimeSeconds;

        public PlaytestInstrumentationSystem()
        {
            ResetSession();
        }

        public string SessionId { get; private set; } = string.Empty;

        public void ResetSession()
        {
            SessionId = "T018-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            sessionStartTimeSeconds = Time.realtimeSinceStartup;
            events.Clear();
            completedRuns.Clear();
            runStartTimes.Clear();
            maxDepthReached.Clear();
            deathCauses.Clear();
            deathTimes.Clear();
        }

        public void RecordRunStart(int runContextId, int laneIndex, int maxHealth, int softBalance)
        {
            var sessionTime = GetSessionTimeSeconds();
            runStartTimes[runContextId] = sessionTime;
            maxDepthReached[runContextId] = 0;
            deathCauses[runContextId] = PlaytestDeathCause.None;

            RecordEvent(new PlaytestTelemetryEvent
            {
                EventType = PlaytestTelemetryEventType.RunStart.ToString(),
                RunContextId = runContextId,
                SessionTimeSeconds = sessionTime,
                Depth = 0,
                LaneIndex = laneIndex,
                RewardTotal = softBalance,
                MaxHealth = maxHealth,
                Detail = "run_start"
            });
        }

        public void RecordLaneInput(int runContextId, int fromLaneIndex, int toLaneIndex, int depth, int direction)
        {
            TrackDepth(runContextId, depth);
            RecordEvent(new PlaytestTelemetryEvent
            {
                EventType = PlaytestTelemetryEventType.LaneInput.ToString(),
                RunContextId = runContextId,
                SessionTimeSeconds = GetSessionTimeSeconds(),
                Depth = depth,
                PreviousLaneIndex = fromLaneIndex,
                LaneIndex = toLaneIndex,
                Detail = direction < 0 ? "swipe_left" : "swipe_right"
            });
        }

        public void RecordBlockBreak(int runContextId, int segmentIndex, int laneIndex, int depth, int rewardDelta, int rewardTotal)
        {
            TrackDepth(runContextId, depth);
            RecordEvent(new PlaytestTelemetryEvent
            {
                EventType = PlaytestTelemetryEventType.BlockBreak.ToString(),
                RunContextId = runContextId,
                SessionTimeSeconds = GetSessionTimeSeconds(),
                Depth = depth,
                SegmentIndex = segmentIndex,
                LaneIndex = laneIndex,
                RewardDelta = rewardDelta,
                RewardTotal = rewardTotal,
                Detail = "legal_break"
            });
        }

        public void RecordDamageTaken(
            int runContextId,
            int segmentIndex,
            int laneIndex,
            int depth,
            int remainingHealth,
            int maxHealth,
            PlaytestDeathCause damageCause)
        {
            TrackDepth(runContextId, depth);
            RecordEvent(new PlaytestTelemetryEvent
            {
                EventType = PlaytestTelemetryEventType.DamageTaken.ToString(),
                RunContextId = runContextId,
                SessionTimeSeconds = GetSessionTimeSeconds(),
                Depth = depth,
                SegmentIndex = segmentIndex,
                LaneIndex = laneIndex,
                RemainingHealth = remainingHealth,
                MaxHealth = maxHealth,
                DeathCause = damageCause.ToString(),
                Detail = "damage_applied"
            });
        }

        public void RecordDeath(int runContextId, PlaytestDeathCause deathCause, int depth, int rewardTotal)
        {
            TrackDepth(runContextId, depth);
            deathCauses[runContextId] = deathCause;
            deathTimes[runContextId] = GetSessionTimeSeconds();

            RecordEvent(new PlaytestTelemetryEvent
            {
                EventType = PlaytestTelemetryEventType.Death.ToString(),
                RunContextId = runContextId,
                SessionTimeSeconds = GetSessionTimeSeconds(),
                Depth = depth,
                RewardTotal = rewardTotal,
                DeathCause = deathCause.ToString(),
                Detail = "run_death"
            });
        }

        public void RecordRunResults(int runContextId, int depth, int rewardTotal)
        {
            TrackDepth(runContextId, depth);
            var startedAt = runStartTimes.TryGetValue(runContextId, out var startTime) ? startTime : 0f;
            var endedAt = deathTimes.TryGetValue(runContextId, out var deathTime) ? deathTime : GetSessionTimeSeconds();
            var summary = FindOrCreateRunSummary(runContextId);
            summary.RunContextId = runContextId;
            summary.StartedAtSessionSeconds = startedAt;
            summary.EndedAtSessionSeconds = endedAt;
            summary.DurationSeconds = Mathf.Max(0f, endedAt - startedAt);
            summary.Depth = maxDepthReached.TryGetValue(runContextId, out var maxDepth) ? maxDepth : depth;
            summary.DeathCause = deathCauses.TryGetValue(runContextId, out var cause)
                ? cause.ToString()
                : PlaytestDeathCause.None.ToString();
            summary.TotalRewardsGranted = rewardTotal;

            RecordEvent(new PlaytestTelemetryEvent
            {
                EventType = PlaytestTelemetryEventType.RunResults.ToString(),
                RunContextId = runContextId,
                SessionTimeSeconds = GetSessionTimeSeconds(),
                Depth = summary.Depth,
                RewardTotal = rewardTotal,
                RunDurationSeconds = summary.DurationSeconds,
                RestartLatencySeconds = summary.RestartLatencySeconds,
                DeathCause = summary.DeathCause,
                Detail = "run_summary"
            });
        }

        public void RecordUpgradePurchase(UpgradeId upgradeId, int newLevel, int purchaseCost, int remainingBalance)
        {
            RecordEvent(new PlaytestTelemetryEvent
            {
                EventType = PlaytestTelemetryEventType.UpgradePurchase.ToString(),
                RunContextId = 0,
                SessionTimeSeconds = GetSessionTimeSeconds(),
                Depth = 0,
                RewardDelta = -purchaseCost,
                RewardTotal = remainingBalance,
                Detail = upgradeId + " -> level " + newLevel
            });
        }

        public void RecordRestartPressed(int completedRunContextId, int nextRunContextId)
        {
            var sessionTime = GetSessionTimeSeconds();
            var summary = FindOrCreateRunSummary(completedRunContextId);
            if (summary.EndedAtSessionSeconds > 0f && summary.RestartLatencySeconds < 0f)
            {
                summary.RestartLatencySeconds = Mathf.Max(0f, sessionTime - summary.EndedAtSessionSeconds);
            }

            RecordEvent(new PlaytestTelemetryEvent
            {
                EventType = PlaytestTelemetryEventType.RestartPressed.ToString(),
                RunContextId = completedRunContextId,
                SessionTimeSeconds = sessionTime,
                Depth = summary.Depth,
                RewardTotal = summary.TotalRewardsGranted,
                RestartTargetRunContextId = nextRunContextId,
                RestartLatencySeconds = summary.RestartLatencySeconds,
                DeathCause = summary.DeathCause,
                Detail = "restart_pressed"
            });
        }

        public void RecordMissionComplete(MissionTemplateId templateId, string description, int rewardValue)
        {
            RecordEvent(new PlaytestTelemetryEvent
            {
                EventType = PlaytestTelemetryEventType.MissionComplete.ToString(),
                RunContextId = 0,
                SessionTimeSeconds = GetSessionTimeSeconds(),
                RewardDelta = rewardValue,
                Detail = templateId + " | " + description
            });
        }

        public PlaytestSessionLog CreateSessionLogSnapshot()
        {
            return new PlaytestSessionLog
            {
                SessionId = SessionId,
                SchemaVersion = SchemaVersion,
                GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
                EventCount = events.Count,
                Events = events.Select(CloneEvent).ToList(),
                CompletedRuns = completedRuns.Select(CloneSummary).OrderBy(entry => entry.RunContextId).ToList()
            };
        }

        public PlaytestReportRecord CreateReportRecord()
        {
            var firstRun = completedRuns.OrderBy(entry => entry.RunContextId).FirstOrDefault() ?? new PlaytestRunSummary();
            var report = new PlaytestReportRecord
            {
                ReportId = "T018-Report-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ"),
                SessionId = SessionId,
                GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
                RunsObserved = completedRuns.Count,
                FirstRunDepth = firstRun.Depth,
                FirstRunDurationSeconds = firstRun.DurationSeconds,
                FirstRunDeathCause = firstRun.DeathCause,
                FirstRunRewards = firstRun.TotalRewardsGranted,
                FirstRunRestartLatencySeconds = firstRun.RestartLatencySeconds,
                LaneInputsObserved = CountEvents(PlaytestTelemetryEventType.LaneInput),
                BlocksBrokenObserved = CountEvents(PlaytestTelemetryEventType.BlockBreak),
                DamageEventsObserved = CountEvents(PlaytestTelemetryEventType.DamageTaken),
                UpgradePurchasesObserved = CountEvents(PlaytestTelemetryEventType.UpgradePurchase),
                MissionCompletionsObserved = CountEvents(PlaytestTelemetryEventType.MissionComplete)
            };

            report.ClarityQuestion =
                $"First run reached depth {report.FirstRunDepth} with {report.LaneInputsObserved} lane input events before death, which is enough to inspect first-session route reading.";
            report.FairnessQuestion =
                $"First death resolved as {report.FirstRunDeathCause} after {report.FirstRunDurationSeconds.ToString("0.00", CultureInfo.InvariantCulture)}s, which exposes whether the hazard read was fair in the opening session.";
            report.RewardQuestion =
                $"First completed run granted {report.FirstRunRewards} soft across {report.BlocksBrokenObserved} legal breaks, which is enough to inspect whether early rewards feel visible but not dominant.";
            report.RestartIntentQuestion =
                $"Restart latency measured at {report.FirstRunRestartLatencySeconds.ToString("0.00", CultureInfo.InvariantCulture)}s, which directly answers whether the tester showed immediate one-more-run intent.";
            return report;
        }

        private int CountEvents(PlaytestTelemetryEventType eventType)
        {
            var eventName = eventType.ToString();
            return events.Count(entry => entry.EventType == eventName);
        }

        private void TrackDepth(int runContextId, int depth)
        {
            if (!maxDepthReached.TryGetValue(runContextId, out var currentDepth) || depth > currentDepth)
            {
                maxDepthReached[runContextId] = depth;
            }
        }

        private PlaytestRunSummary FindOrCreateRunSummary(int runContextId)
        {
            for (var index = 0; index < completedRuns.Count; index += 1)
            {
                if (completedRuns[index].RunContextId == runContextId)
                {
                    return completedRuns[index];
                }
            }

            var summary = new PlaytestRunSummary { RunContextId = runContextId };
            completedRuns.Add(summary);
            return summary;
        }

        private void RecordEvent(PlaytestTelemetryEvent telemetryEvent)
        {
            events.Add(telemetryEvent);
        }

        private float GetSessionTimeSeconds()
        {
            return Mathf.Max(0f, Time.realtimeSinceStartup - sessionStartTimeSeconds);
        }

        private static PlaytestTelemetryEvent CloneEvent(PlaytestTelemetryEvent source)
        {
            return new PlaytestTelemetryEvent
            {
                EventType = source.EventType,
                RunContextId = source.RunContextId,
                SessionTimeSeconds = source.SessionTimeSeconds,
                Depth = source.Depth,
                SegmentIndex = source.SegmentIndex,
                LaneIndex = source.LaneIndex,
                PreviousLaneIndex = source.PreviousLaneIndex,
                RewardDelta = source.RewardDelta,
                RewardTotal = source.RewardTotal,
                RemainingHealth = source.RemainingHealth,
                MaxHealth = source.MaxHealth,
                RestartTargetRunContextId = source.RestartTargetRunContextId,
                RunDurationSeconds = source.RunDurationSeconds,
                RestartLatencySeconds = source.RestartLatencySeconds,
                DeathCause = source.DeathCause,
                Detail = source.Detail
            };
        }

        private static PlaytestRunSummary CloneSummary(PlaytestRunSummary source)
        {
            return new PlaytestRunSummary
            {
                RunContextId = source.RunContextId,
                StartedAtSessionSeconds = source.StartedAtSessionSeconds,
                EndedAtSessionSeconds = source.EndedAtSessionSeconds,
                DurationSeconds = source.DurationSeconds,
                Depth = source.Depth,
                DeathCause = source.DeathCause,
                TotalRewardsGranted = source.TotalRewardsGranted,
                RestartLatencySeconds = source.RestartLatencySeconds
            };
        }
    }
}
