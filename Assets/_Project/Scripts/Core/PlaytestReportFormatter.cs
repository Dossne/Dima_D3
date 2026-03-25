using System.Globalization;
using System.Text;

namespace TapMiner.Core
{
    public static class PlaytestReportFormatter
    {
        public static string FormatMarkdown(PlaytestReportRecord reportRecord)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# T018 Mock Playtest Report");
            builder.AppendLine();
            builder.AppendLine($"report_id: `{reportRecord.ReportId}`");
            builder.AppendLine($"session_id: `{reportRecord.SessionId}`");
            builder.AppendLine($"generated_at_utc: `{reportRecord.GeneratedAtUtc}`");
            builder.AppendLine();
            builder.AppendLine("## First-Session Snapshot");
            builder.AppendLine($"- runs_observed: `{reportRecord.RunsObserved}`");
            builder.AppendLine($"- first_run_depth: `{reportRecord.FirstRunDepth}`");
            builder.AppendLine($"- first_run_duration_seconds: `{reportRecord.FirstRunDurationSeconds.ToString("0.00", CultureInfo.InvariantCulture)}`");
            builder.AppendLine($"- first_run_death_cause: `{reportRecord.FirstRunDeathCause}`");
            builder.AppendLine($"- first_run_rewards: `{reportRecord.FirstRunRewards}`");
            builder.AppendLine($"- first_run_restart_latency_seconds: `{reportRecord.FirstRunRestartLatencySeconds.ToString("0.00", CultureInfo.InvariantCulture)}`");
            builder.AppendLine();
            builder.AppendLine("## Session Event Counts");
            builder.AppendLine($"- lane_inputs_observed: `{reportRecord.LaneInputsObserved}`");
            builder.AppendLine($"- blocks_broken_observed: `{reportRecord.BlocksBrokenObserved}`");
            builder.AppendLine($"- damage_events_observed: `{reportRecord.DamageEventsObserved}`");
            builder.AppendLine($"- upgrade_purchases_observed: `{reportRecord.UpgradePurchasesObserved}`");
            builder.AppendLine($"- mission_completions_observed: `{reportRecord.MissionCompletionsObserved}`");
            builder.AppendLine();
            builder.AppendLine("## Design Questions");
            builder.AppendLine($"- clarity: {reportRecord.ClarityQuestion}");
            builder.AppendLine($"- fairness: {reportRecord.FairnessQuestion}");
            builder.AppendLine($"- rewards: {reportRecord.RewardQuestion}");
            builder.AppendLine($"- restart_intent: {reportRecord.RestartIntentQuestion}");
            return builder.ToString();
        }
    }
}
