# Execution Report v2.1

task_id: `T012`

status: `PASS`

changed_files:
- `Assets/_Project/Scripts/Core/AppBootstrap.cs`
- `Assets/_Project/Scripts/Core/BreakableBlockResolutionSystem.cs`
- `Assets/_Project/Scripts/Core/HazardContactResolutionSystem.cs`
- `Assets/_Project/Scripts/Core/LaneTransitionController.cs`
- `Assets/_Project/Scripts/Core/LootDropResolutionSystem.cs`
- `Assets/_Project/Scripts/Core/RunHealthSystem.cs`
- `Assets/_Project/Scripts/Core/UpgradeCatalog.cs`
- `Assets/_Project/Scripts/Core/UpgradeDefinition.cs`
- `Assets/_Project/Scripts/Core/UpgradeId.cs`
- `Assets/_Project/Scripts/Core/UpgradePersistenceSystem.cs`
- `Assets/_Project/Scripts/Core/UpgradeStatsSnapshot.cs`
- `Assets/_Project/Editor/CoreLoopSmokeRunner.cs`
- `Assets/_Project/Data/T012_ExecutionReport_v2_1.md`

scene_changes: []

prefab_changes: []

proof:
- Implemented a single persistent upgrade authority in [UpgradePersistenceSystem.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Scripts/Core/UpgradePersistenceSystem.cs) using the accepted five-upgrade MVP baseline only.
- Implemented the accepted upgrade definitions in [UpgradeCatalog.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Scripts/Core/UpgradeCatalog.cs): `Drill Power`, `Max HP`, `Move Speed`, `Loot Value`, and `Collapse Resistance`.
- Bound persistent upgrade stats into accepted runtime systems through [AppBootstrap.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Scripts/Core/AppBootstrap.cs), [LaneTransitionController.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Scripts/Core/LaneTransitionController.cs), [LootDropResolutionSystem.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Scripts/Core/LootDropResolutionSystem.cs), [BreakableBlockResolutionSystem.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Scripts/Core/BreakableBlockResolutionSystem.cs), and [HazardContactResolutionSystem.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Scripts/Core/HazardContactResolutionSystem.cs).
- Added per-run health application in [RunHealthSystem.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Scripts/Core/RunHealthSystem.cs) so `Max HP` changes the next run and hazard damage now resolves through the locked T001 health contract before death.
- Observed T012 smoke in play mode through [CoreLoopSmokeRunner.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Editor/CoreLoopSmokeRunner.cs):
- `Process segment 0 -> True | Lane=0 | Break=BreakSucceeded | Loot=LootGranted | RunReward=6 | RewardCount=1`
- `Purchases -> Drill=True/1 | MaxHP=True/1 | Move=True/1 | Loot=True/1 | Collapse=True/1 | Balance=6`
- `Reload progress -> Balance=6 | Drill=1 | MaxHP=1 | Move=1 | Loot=1 | Collapse=1`
- `Restart run -> True | RunState=RunActive | Context=2 | RunReward=0 | RewardCount=0 | Balance=6 | MaxHP=2 | MoveDuration=0,96 | LootMultiplier=1,10 | BreakMultiplier=1,08 | CollapseMultiplier=0,96`
- `Move after speed upgrade -> True | Lane=0 | Transitioning=False | DurationSeconds=0,115`
- `Process with loot upgrade -> True | Break=BreakSucceeded | Loot=LootGranted | RunReward=7 | RewardCount=1`
- `Hazard after Max HP upgrade -> HazardContactResolved | RunState=RunActive | CurrentHP=1 | MaxHP=2`

logs_summary:
- Compile/refresh completed successfully with editor ready for tools after the T012 changes.
- T012 smoke completed end to end in play mode and returned to edit mode without gameplay errors.
- Console was cleared after validation and settled at `0` entries.

validation_results:
- `PASS` inspect upgrade model authority: only [UpgradePersistenceSystem.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Scripts/Core/UpgradePersistenceSystem.cs) owns saved upgrade levels and soft balance.
- `PASS` verify accepted MVP upgrade set only: exactly five upgrade families were implemented and no extra family was added.
- `PASS` verify purchase flow: purchases succeeded only when sufficient soft balance existed and when not in `RunActive` or `RunRestarting`.
- `PASS` verify persistence save/load: purchased levels reloaded from disk as `Drill=1 | MaxHP=1 | Move=1 | Loot=1 | Collapse=1`.
- `PASS` verify stat application on next run: restarted context `2` showed `MaxHP=2 | MoveDuration=0,96 | LootMultiplier=1,10 | BreakMultiplier=1,08 | CollapseMultiplier=0,96`.
- `PASS` verify `Move Speed` runtime effect: lane transition completed with `DurationSeconds=0,115` after the upgrade, matching the reduced next-run duration.
- `PASS` verify `Loot Value` runtime effect: upgraded next-run break granted `RunReward=7`, higher than the baseline `RunReward=6` on the same first segment path.
- `PASS` verify `Max HP` runtime effect: a legal hazard hit after restart left the run in `RunActive` with `CurrentHP=1` and `MaxHP=2`.
- `PASS` verify `Drill Power` stat hook: next-run applied stats reported `BreakMultiplier=1,08` and the break system stored the upgraded break-speed multiplier.
- `PASS` verify `Collapse Resistance` stat hook: next-run applied stats reported `CollapseMultiplier=0,96` and the hazard system stored the upgraded collapse multiplier.
- `PASS` verify reward authority preservation: T011 run reward aggregation remained the source of run reward totals and banked into persistent soft balance only on run end.
- `PASS` verify no unrelated scene drift: active scene remained `Assets/Scenes/SampleScene.unity` with `isDirty=false`.
- `PASS` verify compile status: editor state returned `is_compiling=false`, `is_domain_reload_pending=false`, `ready_for_tools=true`.
- `PASS` verify console status: no gameplay errors or script errors remained after validation cleanup.

mcp_checks:
- `PASS` logs baseline checked: console cleared before validation and re-checked after smoke.
- `PASS` compile support checked: Unity refresh requested, domain reload completed, and editor returned ready.
- `PASS` upgrade defs inspected: accepted five-upgrade baseline present in [UpgradeCatalog.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Scripts/Core/UpgradeCatalog.cs).
- `PASS` save/load validation completed: smoke reloaded purchased levels and preserved balance.
- `PASS` stat application proof captured: next-run logs recorded upgraded HP, movement duration, loot multiplier, break multiplier, and collapse multiplier.
- `PASS` no unrelated scene drift: [SampleScene.unity](E:/Unity%20Projects/tap_miner/Assets/Scenes/SampleScene.unity) remained loaded and clean.

failure_signals: []

scope_check: `OK`

manual_verdict: `Clear and trustworthy: the exact five-upgrade MVP now persists, reloads, and applies next-run stats without hidden upgrade logic.`

invariants_preserved:
- No repo or tooling regression introduced.
- No scope expansion beyond T012 upgrade persistence MVP.
- No contradiction with accepted T001 through T011 authority flow.
- No branching tree, timers, new currencies, or unrelated meta systems added.
- No scene, prefab, content, or unrelated UI changes made.

issues_found:
- The repo had no existing T012 upgrade layer, so persistence, stat snapshotting, and validation hooks had to be introduced from scratch inside the accepted core authority path.
- A recurring `MCP-FOR-UNITY` WebSocket warning appeared during refresh cycles, but it was unrelated to gameplay and cleared cleanly before final console validation.

fix_applied:
- Added a single upgrade persistence layer and stat snapshot.
- Added per-run health reset/application so `Max HP` affects the next run.
- Applied move-speed and loot-value stats directly into existing runtime systems.
- Added explicit stat-hook storage for drill power and collapse resistance in their existing accepted systems.
- Extended the editor smoke runner to validate purchase, reload, restart, and next-run effects end to end.

rollback_used: `No`

gate_impact:
- T012 is validator-complete and unblocks T013.

next_action: `proceed to T013`
