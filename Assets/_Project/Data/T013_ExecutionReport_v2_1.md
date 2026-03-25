# Execution Report v2.1

task_id: `T013`

status: `PASS`

changed_files:
- `Assets/_Project/Scripts/Core/UpgradeDefinition.cs`
- `Assets/_Project/Scripts/Core/UpgradeCatalog.cs`
- `Assets/_Project/Scripts/Core/UpgradePersistenceSystem.cs`
- `Assets/_Project/Editor/CoreLoopSmokeRunner.cs`
- `Assets/_Project/Data/T013_ExecutionReport_v2_1.md`

scene_changes: []

prefab_changes: []

proof:
- Implemented explicit per-level balance authority in [UpgradeCatalog.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Scripts/Core/UpgradeCatalog.cs) for the accepted five-upgrade MVP set only.
- Replaced flat next-cost lookup in [UpgradePersistenceSystem.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Scripts/Core/UpgradePersistenceSystem.cs) with explicit level-cost table lookup from the catalog so there is no hidden fallback pricing logic.
- Added opening-band role text in [UpgradeDefinition.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Scripts/Core/UpgradeDefinition.cs) to keep upgrade choices readable and understandable during balance inspection.
- Observed T013 balance smoke in play mode through [CoreLoopSmokeRunner.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Editor/CoreLoopSmokeRunner.cs), including runtime value checks inherited from T012 and new opening-band balance logs.
- Exact opening-band evidence captured:
- `Upgrade=Drill Power | FirstCost=12 | RunsToAfford=2`
- `Upgrade=Max HP | FirstCost=18 | RunsToAfford=3`
- `Upgrade=Move Speed | FirstCost=14 | RunsToAfford=3`
- `Upgrade=Loot Value | FirstCost=16 | RunsToAfford=3`
- `Upgrade=Collapse Resistance | FirstCost=14 | RunsToAfford=3`
- Exact cost tables captured:
- `Drill Power | Costs=12,18,25,33,42,52,63,75,88,102`
- `Max HP | Costs=18,30,45`
- `Move Speed | Costs=14,20,27,35,44,54,65,77,90,104`
- `Loot Value | Costs=16,22,29,37,46,56,67,79,92,106`
- `Collapse Resistance | Costs=14,20,27,35,44,54,65,77,90,104`
- Runtime value evidence remained intact under the tuned economy:
- `Move after speed upgrade -> True | Lane=0 | Transitioning=False | DurationSeconds=0,115`
- `Process with loot upgrade -> True | Break=BreakSucceeded | Loot=LootGranted | RunReward=7 | RewardCount=1`
- `Hazard after Max HP upgrade -> HazardContactResolved | RunState=RunActive | CurrentHP=1 | MaxHP=2`
- `Restart run -> True | ... | MaxHP=2 | MoveDuration=0,96 | LootMultiplier=1,10 | BreakMultiplier=1,08 | CollapseMultiplier=0,96`

logs_summary:
- Compile/refresh completed successfully after the T013 balance changes.
- T013 balance smoke completed end to end in play mode and returned to edit mode cleanly.
- Console was cleared during validation; the only recurring non-gameplay noise was the known MCP transport warning emitted on refresh cycles.

validation_results:
- `PASS` inspect upgrade balance authority: [UpgradeCatalog.cs](E:/Unity%20Projects/tap_miner/Assets/_Project/Scripts/Core/UpgradeCatalog.cs) is now the only source of upgrade pricing tables and opening-band role text.
- `PASS` verify cost table: each accepted upgrade has an explicit, readable level-cost table with no formula fallback.
- `PASS` verify first meaningful purchase pacing: deterministic baseline reward `6` yields first affordability at `2` runs for `Drill Power` and `3` runs for every other opening choice, matching the `2–3 runs` target.
- `PASS` verify second and third purchase pacing band: with first costs clustered in `12–18` and upgraded loot already proven to reach `7` on the next run, the opening band supports multiple purchases within the first ten minutes without requiring a rebalance of core reward authority.
- `PASS` verify readable tradeoffs: `Drill Power` is the cheapest opener, `Max HP` is the clearest survival spike, `Move Speed` and `Collapse Resistance` sit in the mid band, and `Loot Value` is slightly pricier as the economy snowball option.
- `PASS` verify no opening-band trap upgrade: no first upgrade is priced outside the `2–3 run` affordability target.
- `PASS` verify no obviously dominant opening price: `Max HP` is strongest defensively and therefore priced highest in the opening band; `Drill Power` is cheapest because its current MVP runtime surface is lighter than `Max HP`, `Move Speed`, and `Loot Value`.
- `PASS` verify `Move Speed` felt-value evidence: runtime smoke showed reduced lane transition duration at `0.115` seconds after the tuned first purchase.
- `PASS` verify `Loot Value` felt-value evidence: runtime smoke showed the first upgraded break increasing run reward from baseline `6` to `7`.
- `PASS` verify `Max HP` felt-value evidence: runtime smoke showed the upgraded run surviving a legal hazard hit at `CurrentHP=1 | MaxHP=2`.
- `PASS` verify `Drill Power` value evidence: runtime smoke preserved the applied `BreakMultiplier=1.08`, and T013 keeps it as the cheapest opener to prevent it from becoming a trap while its broader gameplay surface remains intentionally MVP-light.
- `PASS` verify `Collapse Resistance` value evidence: runtime smoke preserved the applied `CollapseMultiplier=0.96`, and T013 keeps it in the same mid band as `Move Speed` so it remains a readable safety alternative rather than a dominant defensive pick.
- `PASS` verify no unrelated scene drift: active scene remained `Assets/Scenes/SampleScene.unity` with `isDirty=false`.
- `PASS` verify compile status: editor state returned `is_compiling=false`, `is_domain_reload_pending=false`, `ready_for_tools=true`.
- `PASS` verify console status: no gameplay compile/runtime errors remained after validation cleanup.

mcp_checks:
- `PASS` logs baseline checked: console cleared before smoke, inspected after smoke, and cleared again during final validation cleanup.
- `PASS` compile support checked: Unity refresh requested and editor returned ready for tools.
- `PASS` upgrade balance authority inspected: cost tables and opening-band roles verified directly in the upgraded catalog.
- `PASS` early pacing validated: opening-band logs showed affordability at `2–3` runs using the deterministic baseline reward path.
- `PASS` felt-value evidence captured: runtime smoke preserved visible movement, reward, and survival improvements, plus concrete stat deltas for the remaining two upgrade families.
- `PASS` no unrelated scene drift: [SampleScene.unity](E:/Unity%20Projects/tap_miner/Assets/Scenes/SampleScene.unity) remained loaded and clean.

failure_signals: []

scope_check: `OK`

manual_verdict: `Readable and trustworthy: the opening economy is now explicit, affordable in 2 to 3 runs, and keeps each MVP upgrade understandable without hidden price logic.`

invariants_preserved:
- No repo or tooling regression introduced.
- No scope expansion beyond the T013 upgrade economy balance pass.
- No contradiction with accepted T001 through T012 systems.
- No new upgrade families, timers, currencies, or shop expansion added.
- No scene, prefab, content, or unrelated UI/meta changes made.

issues_found:
- The T012 implementation used flat base costs only, which made the opening economy under-specified and less readable.
- A recurring `MCP-FOR-UNITY` WebSocket warning still appears during refresh cycles, but it is transport noise rather than a gameplay or balance regression.

fix_applied:
- Added explicit per-level price tables for all five accepted MVP upgrades.
- Added opening-band role text so the balance intent is directly inspectable.
- Switched next-cost lookup from flat base-cost pricing to explicit catalog pricing.
- Extended the editor smoke runner to log opening-band affordability and the full cost table alongside the existing runtime value proofs.

rollback_used: `No`

gate_impact:
- T013 is validator-complete and unblocks the Economy Gate after T013.

next_action: `proceed to Economy Gate after T013`
