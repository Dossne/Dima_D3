# T018 Mock Playtest Report

report_id: `T018-Report-20260325T225412Z`
session_id: `T018-20260325T225406Z`
generated_at_utc: `2026-03-25T22:54:12.8844240Z`

## First-Session Snapshot
- runs_observed: `1`
- first_run_depth: `2`
- first_run_duration_seconds: `1.04`
- first_run_death_cause: `HazardContact`
- first_run_rewards: `6`
- first_run_restart_latency_seconds: `0.44`

## Session Event Counts
- lane_inputs_observed: `3`
- blocks_broken_observed: `1`
- damage_events_observed: `1`
- upgrade_purchases_observed: `1`
- mission_completions_observed: `0`

## Design Questions
- clarity: First run reached depth 2 with 3 lane input events before death, which is enough to inspect first-session route reading.
- fairness: First death resolved as HazardContact after 1.04s, which exposes whether the hazard read was fair in the opening session.
- rewards: First completed run granted 6 soft across 1 legal breaks, which is enough to inspect whether early rewards feel visible but not dominant.
- restart_intent: Restart latency measured at 0.44s, which directly answers whether the tester showed immediate one-more-run intent.
