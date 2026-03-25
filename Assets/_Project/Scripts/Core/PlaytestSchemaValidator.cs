using System.Linq;

namespace TapMiner.Core
{
    public static class PlaytestSchemaValidator
    {
        public static PlaytestSchemaValidationResult ValidateSessionLog(PlaytestSessionLog sessionLog)
        {
            var result = new PlaytestSchemaValidationResult();

            if (string.IsNullOrWhiteSpace(sessionLog.SessionId))
            {
                result.MissingFields.Add("session_id");
            }

            if (string.IsNullOrWhiteSpace(sessionLog.SchemaVersion))
            {
                result.MissingFields.Add("schema_version");
            }

            if (string.IsNullOrWhiteSpace(sessionLog.GeneratedAtUtc))
            {
                result.MissingFields.Add("generated_at_utc");
            }

            if (sessionLog.Events == null || sessionLog.Events.Count == 0)
            {
                result.MissingFields.Add("events");
            }

            if (sessionLog.CompletedRuns == null || sessionLog.CompletedRuns.Count == 0)
            {
                result.MissingFields.Add("completed_runs");
            }

            if (!HasEvent(sessionLog, PlaytestTelemetryEventType.RunStart))
            {
                result.MissingFields.Add("events.run_start");
            }

            if (!HasEvent(sessionLog, PlaytestTelemetryEventType.Death))
            {
                result.MissingFields.Add("events.death");
            }

            if (!HasEvent(sessionLog, PlaytestTelemetryEventType.RunResults))
            {
                result.MissingFields.Add("events.run_results");
            }

            if (!HasEvent(sessionLog, PlaytestTelemetryEventType.RestartPressed))
            {
                result.MissingFields.Add("events.restart_pressed");
            }

            if (sessionLog.CompletedRuns != null && sessionLog.CompletedRuns.Count > 0)
            {
                var firstRun = sessionLog.CompletedRuns[0];
                if (firstRun.Depth <= 0)
                {
                    result.MissingFields.Add("completed_runs[0].depth");
                }

                if (firstRun.DurationSeconds <= 0f)
                {
                    result.MissingFields.Add("completed_runs[0].duration_seconds");
                }

                if (string.IsNullOrWhiteSpace(firstRun.DeathCause) || firstRun.DeathCause == PlaytestDeathCause.None.ToString())
                {
                    result.MissingFields.Add("completed_runs[0].death_cause");
                }

                if (firstRun.TotalRewardsGranted < 0)
                {
                    result.MissingFields.Add("completed_runs[0].rewards");
                }

                if (firstRun.RestartLatencySeconds < 0f)
                {
                    result.MissingFields.Add("completed_runs[0].restart_latency_seconds");
                }
            }

            result.IsValid = result.MissingFields.Count == 0;
            return result;
        }

        public static PlaytestSchemaValidationResult ValidateReportRecord(PlaytestReportRecord reportRecord)
        {
            var result = new PlaytestSchemaValidationResult();

            if (string.IsNullOrWhiteSpace(reportRecord.ReportId))
            {
                result.MissingFields.Add("report_id");
            }

            if (string.IsNullOrWhiteSpace(reportRecord.SessionId))
            {
                result.MissingFields.Add("session_id");
            }

            if (string.IsNullOrWhiteSpace(reportRecord.GeneratedAtUtc))
            {
                result.MissingFields.Add("generated_at_utc");
            }

            if (reportRecord.RunsObserved <= 0)
            {
                result.MissingFields.Add("runs_observed");
            }

            if (reportRecord.FirstRunDepth <= 0)
            {
                result.MissingFields.Add("first_run_depth");
            }

            if (reportRecord.FirstRunDurationSeconds <= 0f)
            {
                result.MissingFields.Add("first_run_duration_seconds");
            }

            if (string.IsNullOrWhiteSpace(reportRecord.FirstRunDeathCause))
            {
                result.MissingFields.Add("first_run_death_cause");
            }

            if (reportRecord.FirstRunRewards < 0)
            {
                result.MissingFields.Add("first_run_rewards");
            }

            if (reportRecord.FirstRunRestartLatencySeconds < 0f)
            {
                result.MissingFields.Add("first_run_restart_latency_seconds");
            }

            if (string.IsNullOrWhiteSpace(reportRecord.ClarityQuestion))
            {
                result.MissingFields.Add("clarity_question");
            }

            if (string.IsNullOrWhiteSpace(reportRecord.FairnessQuestion))
            {
                result.MissingFields.Add("fairness_question");
            }

            if (string.IsNullOrWhiteSpace(reportRecord.RewardQuestion))
            {
                result.MissingFields.Add("reward_question");
            }

            if (string.IsNullOrWhiteSpace(reportRecord.RestartIntentQuestion))
            {
                result.MissingFields.Add("restart_intent_question");
            }

            result.IsValid = result.MissingFields.Count == 0;
            return result;
        }

        private static bool HasEvent(PlaytestSessionLog sessionLog, PlaytestTelemetryEventType eventType)
        {
            return sessionLog.Events != null &&
                   sessionLog.Events.Any(entry => entry.EventType == eventType.ToString());
        }
    }
}
