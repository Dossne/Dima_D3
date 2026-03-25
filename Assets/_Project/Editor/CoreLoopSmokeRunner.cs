using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TapMiner.Core;

namespace TapMiner.EditorTools
{
    public static class CoreLoopSmokeRunner
    {
        private const string SessionKey = "TapMiner.CoreLoopSmokeRunner.Active";
        private static readonly Queue<SmokeStep> Steps = new Queue<SmokeStep>();

        private static double nextStepTime;

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;

            if (IsRunning() && EditorApplication.isPlaying)
            {
                PrepareStepsForPlayMode();
            }
        }

        [MenuItem("Tools/Tap Miner/Run Core Loop Smoke")]
        public static void RunCoreLoopSmoke()
        {
            if (IsRunning())
            {
                Debug.LogWarning("[CoreLoopSmokeRunner] Smoke run is already active.");
                return;
            }

            SetRunning(true);
            EditorApplication.isPlaying = true;
            Debug.Log("[CoreLoopSmokeRunner] Requested play mode for T010 smoke.");
        }

        [MenuItem("Tools/Tap Miner/Run T012 Upgrade Smoke")]
        public static void RunT012UpgradeSmoke()
        {
            if (IsRunning())
            {
                Debug.LogWarning("[CoreLoopSmokeRunner] Smoke run is already active.");
                return;
            }

            SetRunning(true);
            EditorApplication.isPlaying = true;
            Debug.Log("[CoreLoopSmokeRunner] Requested play mode for T012 smoke.");
        }

        [MenuItem("Tools/Tap Miner/Run T013 Balance Smoke")]
        public static void RunT013BalanceSmoke()
        {
            if (IsRunning())
            {
                Debug.LogWarning("[CoreLoopSmokeRunner] Smoke run is already active.");
                return;
            }

            SetRunning(true);
            EditorApplication.isPlaying = true;
            Debug.Log("[CoreLoopSmokeRunner] Requested play mode for T013 smoke.");
        }

        [MenuItem("Tools/Tap Miner/Run T014 Mission Smoke")]
        public static void RunT014MissionSmoke()
        {
            if (IsRunning())
            {
                Debug.LogWarning("[CoreLoopSmokeRunner] Smoke run is already active.");
                return;
            }

            SetRunning(true);
            EditorApplication.isPlaying = true;
            Debug.Log("[CoreLoopSmokeRunner] Requested play mode for T014 smoke.");
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (!IsRunning())
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                PrepareStepsForPlayMode();
                Debug.Log("[CoreLoopSmokeRunner] Entered play mode.");
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                Debug.Log("[CoreLoopSmokeRunner] Returned to edit mode.");
                StopRunner();
            }
        }

        private static void OnEditorUpdate()
        {
            if (!IsRunning() || !EditorApplication.isPlaying)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < nextStepTime)
            {
                return;
            }

            if (Steps.Count == 0)
            {
                Debug.Log("[CoreLoopSmokeRunner] Smoke sequence complete.");
                EditorApplication.isPlaying = false;
                return;
            }

            var bootstrap = UnityEngine.Object.FindAnyObjectByType<AppBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError("[CoreLoopSmokeRunner] AppBootstrap was not found in play mode.");
                EditorApplication.isPlaying = false;
                return;
            }

            var step = Steps.Peek();
            if (!step.IsReady(bootstrap))
            {
                nextStepTime = EditorApplication.timeSinceStartup + 0.1d;
                return;
            }

            Steps.Dequeue();
            step.Execute(bootstrap);
            nextStepTime = EditorApplication.timeSinceStartup + step.DelaySeconds;
        }

        private static void EnqueueSteps()
        {
            Steps.Clear();
            Steps.Enqueue(new SmokeStep("Reset T012 validation progress", bootstrap => true, 0.2d, bootstrap =>
            {
                bootstrap.DebugResetUpgradeProgressForValidation();
                bootstrap.DebugResetMissionProgressForValidation();
                Debug.Log(
                    $"[CoreLoopSmokeRunner] T012 reset -> Balance={bootstrap.SoftCurrencyBalance} | HP={bootstrap.CurrentRunMaxHealth} | MoveDuration={bootstrap.CurrentUpgradeStats.LaneTransitionDurationMultiplier:0.00} | LootMultiplier={bootstrap.CurrentUpgradeStats.LootValueMultiplier:0.00} | BreakMultiplier={bootstrap.CurrentUpgradeStats.BlockBreakSpeedMultiplier:0.00} | CollapseMultiplier={bootstrap.CurrentUpgradeStats.CollapseCatchRateMultiplier:0.00}");
                Debug.Log(
                    $"[CoreLoopSmokeRunner] T014 missions -> Break={FormatMission(bootstrap, MissionTemplateId.BreakBlocks)} | Segments={FormatMission(bootstrap, MissionTemplateId.CompleteSegments)} | Soft={FormatMission(bootstrap, MissionTemplateId.EarnSoft)}");
            }));

            Steps.Enqueue(new SmokeStep("Start run", bootstrap => true, 0.2d, bootstrap =>
            {
                var result = bootstrap.RequestStartRun();
                Debug.Log($"[CoreLoopSmokeRunner] Start run -> {result} | RunState={bootstrap.CurrentRunState}");
            }));

            Steps.Enqueue(new SmokeStep("Move left to break lane", bootstrap => bootstrap.CurrentRunState == RunState.RunActive, 0.2d, bootstrap =>
            {
                var result = bootstrap.RequestLaneTransitionLeft();
                bootstrap.DebugAdvanceRuntimeLoop(0.2f);
                Debug.Log($"[CoreLoopSmokeRunner] Move left -> {result} | Lane={bootstrap.CurrentCommittedLaneIndex} | Transitioning={bootstrap.IsLaneTransitioning}");
            }));

            Steps.Enqueue(new SmokeStep("Process segment 0", bootstrap => bootstrap.CurrentRunState == RunState.RunActive && !bootstrap.IsLaneTransitioning && bootstrap.CurrentCommittedLaneIndex == 0, 0.2d, bootstrap =>
            {
                var result = bootstrap.RequestProcessCurrentSegment();
                Debug.Log(
                    $"[CoreLoopSmokeRunner] Process segment 0 -> {result} | Lane={bootstrap.CurrentCommittedLaneIndex} | Break={bootstrap.LastBreakResolutionResult} | Loot={bootstrap.LastLootResolutionResult} | RunReward={bootstrap.CurrentRunRewardResult.TotalRewardValue} | RewardCount={bootstrap.CurrentRunRewardResult.GrantedLootCount}");
            }));

            Steps.Enqueue(new SmokeStep("Move right to center", bootstrap => bootstrap.CurrentRunState == RunState.RunActive && !bootstrap.IsLaneTransitioning && bootstrap.CurrentCommittedLaneIndex == 0, 0.2d, bootstrap =>
            {
                var result = bootstrap.RequestLaneTransitionRight();
                bootstrap.DebugAdvanceRuntimeLoop(0.2f);
                Debug.Log($"[CoreLoopSmokeRunner] Move right -> {result} | Lane={bootstrap.CurrentCommittedLaneIndex} | Transitioning={bootstrap.IsLaneTransitioning}");
            }));

            Steps.Enqueue(new SmokeStep("Move right to hazard lane", bootstrap => bootstrap.CurrentRunState == RunState.RunActive && !bootstrap.IsLaneTransitioning && bootstrap.CurrentCommittedLaneIndex == 1, 0.2d, bootstrap =>
            {
                var result = bootstrap.RequestLaneTransitionRight();
                bootstrap.DebugAdvanceRuntimeLoop(0.2f);
                Debug.Log($"[CoreLoopSmokeRunner] Move right again -> {result} | Lane={bootstrap.CurrentCommittedLaneIndex} | Transitioning={bootstrap.IsLaneTransitioning}");
            }));

            Steps.Enqueue(new SmokeStep("Trigger hazard", bootstrap => bootstrap.CurrentRunState == RunState.RunActive && !bootstrap.IsLaneTransitioning && bootstrap.CurrentCommittedLaneIndex == 2, 0.2d, bootstrap =>
            {
                var result = bootstrap.RequestResolveCurrentLaneHazardContact();
                Debug.Log(
                    $"[CoreLoopSmokeRunner] Resolve hazard -> {result} | RunState={bootstrap.CurrentRunState} | Lane={bootstrap.CurrentCommittedLaneIndex}");
            }));

            Steps.Enqueue(new SmokeStep("Restart run", bootstrap => bootstrap.CurrentRunState == RunState.RunDeathResolved, 0.2d, bootstrap =>
            {
                bootstrap.DebugGrantSoftCurrencyForValidation(100);
                Debug.Log($"[CoreLoopSmokeRunner] Grant validation funds -> Balance={bootstrap.SoftCurrencyBalance}");
                var drill = bootstrap.TryPurchaseUpgrade(UpgradeId.DrillPower);
                var maxHp = bootstrap.TryPurchaseUpgrade(UpgradeId.MaxHp);
                var moveSpeed = bootstrap.TryPurchaseUpgrade(UpgradeId.MoveSpeed);
                var lootValue = bootstrap.TryPurchaseUpgrade(UpgradeId.LootValue);
                var collapseResistance = bootstrap.TryPurchaseUpgrade(UpgradeId.CollapseResistance);
                Debug.Log(
                    $"[CoreLoopSmokeRunner] Purchases -> Drill={drill}/{bootstrap.GetUpgradeLevel(UpgradeId.DrillPower)} | MaxHP={maxHp}/{bootstrap.GetUpgradeLevel(UpgradeId.MaxHp)} | Move={moveSpeed}/{bootstrap.GetUpgradeLevel(UpgradeId.MoveSpeed)} | Loot={lootValue}/{bootstrap.GetUpgradeLevel(UpgradeId.LootValue)} | Collapse={collapseResistance}/{bootstrap.GetUpgradeLevel(UpgradeId.CollapseResistance)} | Balance={bootstrap.SoftCurrencyBalance}");
                bootstrap.ReloadUpgradeProgress();
                Debug.Log(
                    $"[CoreLoopSmokeRunner] Reload progress -> Balance={bootstrap.SoftCurrencyBalance} | Drill={bootstrap.GetUpgradeLevel(UpgradeId.DrillPower)} | MaxHP={bootstrap.GetUpgradeLevel(UpgradeId.MaxHp)} | Move={bootstrap.GetUpgradeLevel(UpgradeId.MoveSpeed)} | Loot={bootstrap.GetUpgradeLevel(UpgradeId.LootValue)} | Collapse={bootstrap.GetUpgradeLevel(UpgradeId.CollapseResistance)}");
                var result = bootstrap.RequestRestartRun();
                Debug.Log(
                    $"[CoreLoopSmokeRunner] Restart run -> {result} | RunState={bootstrap.CurrentRunState} | Context={bootstrap.CurrentRunContextId} | RunReward={bootstrap.CurrentRunRewardResult.TotalRewardValue} | RewardCount={bootstrap.CurrentRunRewardResult.GrantedLootCount} | Balance={bootstrap.SoftCurrencyBalance} | MaxHP={bootstrap.CurrentRunMaxHealth} | MoveDuration={bootstrap.CurrentUpgradeStats.LaneTransitionDurationMultiplier:0.00} | LootMultiplier={bootstrap.CurrentUpgradeStats.LootValueMultiplier:0.00} | BreakMultiplier={bootstrap.CurrentUpgradeStats.BlockBreakSpeedMultiplier:0.00} | CollapseMultiplier={bootstrap.CurrentUpgradeStats.CollapseCatchRateMultiplier:0.00}");
            }));

            Steps.Enqueue(new SmokeStep("Verify upgraded move speed", bootstrap => bootstrap.CurrentRunState == RunState.RunActive && !bootstrap.IsLaneTransitioning, 0.2d, bootstrap =>
            {
                var result = bootstrap.RequestLaneTransitionLeft();
                bootstrap.DebugAdvanceRuntimeLoop(0.12f);
                Debug.Log(
                    $"[CoreLoopSmokeRunner] Move after speed upgrade -> {result} | Lane={bootstrap.CurrentCommittedLaneIndex} | Transitioning={bootstrap.IsLaneTransitioning} | DurationSeconds={bootstrap.CurrentLaneTransitionDurationSeconds:0.000}");
            }));

            Steps.Enqueue(new SmokeStep("Verify upgraded loot value", bootstrap => bootstrap.CurrentRunState == RunState.RunActive && !bootstrap.IsLaneTransitioning, 0.2d, bootstrap =>
            {
                var result = bootstrap.RequestProcessCurrentSegment();
                Debug.Log(
                    $"[CoreLoopSmokeRunner] Process with loot upgrade -> {result} | Break={bootstrap.LastBreakResolutionResult} | Loot={bootstrap.LastLootResolutionResult} | RunReward={bootstrap.CurrentRunRewardResult.TotalRewardValue} | RewardCount={bootstrap.CurrentRunRewardResult.GrantedLootCount}");
            }));

            Steps.Enqueue(new SmokeStep("Move to hazard setup center", bootstrap => bootstrap.CurrentRunState == RunState.RunActive && !bootstrap.IsLaneTransitioning, 0.2d, bootstrap =>
            {
                var result = bootstrap.RequestLaneTransitionRight();
                bootstrap.DebugAdvanceRuntimeLoop(0.12f);
                Debug.Log(
                    $"[CoreLoopSmokeRunner] Move to hazard setup center -> {result} | Lane={bootstrap.CurrentCommittedLaneIndex} | Transitioning={bootstrap.IsLaneTransitioning}");
            }));

            Steps.Enqueue(new SmokeStep("Move to hazard lane on next segment", bootstrap => bootstrap.CurrentRunState == RunState.RunActive && !bootstrap.IsLaneTransitioning, 0.2d, bootstrap =>
            {
                var result = bootstrap.RequestLaneTransitionRight();
                bootstrap.DebugAdvanceRuntimeLoop(0.12f);
                Debug.Log(
                    $"[CoreLoopSmokeRunner] Move to hazard lane on next segment -> {result} | Lane={bootstrap.CurrentCommittedLaneIndex} | Transitioning={bootstrap.IsLaneTransitioning}");
            }));

            Steps.Enqueue(new SmokeStep("Verify upgraded hazard survival", bootstrap => bootstrap.CurrentRunState == RunState.RunActive && !bootstrap.IsLaneTransitioning, 0.2d, bootstrap =>
            {
                var result = bootstrap.RequestResolveCurrentLaneHazardContact();
                Debug.Log(
                    $"[CoreLoopSmokeRunner] Hazard after Max HP upgrade -> {result} | RunState={bootstrap.CurrentRunState} | CurrentHP={bootstrap.CurrentRunHealth} | MaxHP={bootstrap.CurrentRunMaxHealth}");
            }));

            Steps.Enqueue(new SmokeStep("Report final state", bootstrap => bootstrap.CurrentRunState == RunState.RunActive && !bootstrap.IsLaneTransitioning, 0.1d, bootstrap =>
            {
                Debug.Log(
                    $"[CoreLoopSmokeRunner] Final state | RunState={bootstrap.CurrentRunState} | Lane={bootstrap.CurrentCommittedLaneIndex} | Segments={bootstrap.CurrentSpawnedSegmentCount} | Break={bootstrap.LastBreakResolutionResult} | Loot={bootstrap.LastLootResolutionResult} | Hazard={bootstrap.LastHazardContactResult} | RunReward={bootstrap.CurrentRunRewardResult.TotalRewardValue} | RewardCount={bootstrap.CurrentRunRewardResult.GrantedLootCount} | Balance={bootstrap.SoftCurrencyBalance}");
            }));

            Steps.Enqueue(new SmokeStep("Report T013 opening band", bootstrap => bootstrap.CurrentRunState == RunState.RunActive, 0.1d, bootstrap =>
            {
                const int baselineRewardPerRun = 6;

                foreach (var definition in UpgradeCatalog.All)
                {
                    var firstCost = UpgradeCatalog.GetCostForLevel(definition.Id, 0);
                    var runsToAfford = Mathf.CeilToInt(firstCost / (float)baselineRewardPerRun);
                    Debug.Log(
                        $"[CoreLoopSmokeRunner] T013 opening band | Upgrade={definition.DisplayName} | FirstCost={firstCost} | RunsToAfford={runsToAfford} | Role={definition.OpeningBandRole} | Effect={definition.PlayerFacingEffect}");
                }
            }));

            Steps.Enqueue(new SmokeStep("Report T013 cost table", bootstrap => bootstrap.CurrentRunState == RunState.RunActive, 0.1d, bootstrap =>
            {
                foreach (var definition in UpgradeCatalog.All)
                {
                    Debug.Log(
                        $"[CoreLoopSmokeRunner] T013 cost table | Upgrade={definition.DisplayName} | Costs={string.Join(",", definition.LevelCosts)}");
                }
            }));

            Steps.Enqueue(new SmokeStep("Report T014 mission progress after two runs", bootstrap => bootstrap.CurrentRunState == RunState.RunActive, 0.1d, bootstrap =>
            {
                Debug.Log(
                    $"[CoreLoopSmokeRunner] T014 mission progress -> Break={FormatMission(bootstrap, MissionTemplateId.BreakBlocks)} | Segments={FormatMission(bootstrap, MissionTemplateId.CompleteSegments)} | Soft={FormatMission(bootstrap, MissionTemplateId.EarnSoft)} | LastReward={bootstrap.LastGrantedMissionReward} | TotalMissionRewards={bootstrap.TotalMissionRewardsGranted} | Balance={bootstrap.SoftCurrencyBalance}");
            }));

            Steps.Enqueue(new SmokeStep("Force end upgraded run for T014", bootstrap => bootstrap.CurrentRunState == RunState.RunActive, 0.2d, bootstrap =>
            {
                var killed = bootstrap.NotifyLethalDamage();
                Debug.Log($"[CoreLoopSmokeRunner] T014 force death -> {killed} | RunState={bootstrap.CurrentRunState}");
            }));

            Steps.Enqueue(new SmokeStep("Restart for T014 third run", bootstrap => bootstrap.CurrentRunState == RunState.RunDeathResolved, 0.2d, bootstrap =>
            {
                var restarted = bootstrap.RequestRestartRun();
                Debug.Log(
                    $"[CoreLoopSmokeRunner] T014 restart third run -> {restarted} | Context={bootstrap.CurrentRunContextId} | Break={FormatMission(bootstrap, MissionTemplateId.BreakBlocks)} | Segments={FormatMission(bootstrap, MissionTemplateId.CompleteSegments)} | Soft={FormatMission(bootstrap, MissionTemplateId.EarnSoft)}");
            }));

            Steps.Enqueue(new SmokeStep("Move to break lane on T014 third run", bootstrap => bootstrap.CurrentRunState == RunState.RunActive && !bootstrap.IsLaneTransitioning, 0.2d, bootstrap =>
            {
                var result = bootstrap.RequestLaneTransitionLeft();
                bootstrap.DebugAdvanceRuntimeLoop(0.12f);
                Debug.Log($"[CoreLoopSmokeRunner] T014 move left third run -> {result} | Lane={bootstrap.CurrentCommittedLaneIndex}");
            }));

            Steps.Enqueue(new SmokeStep("Process T014 third run segment", bootstrap => bootstrap.CurrentRunState == RunState.RunActive && !bootstrap.IsLaneTransitioning && bootstrap.CurrentCommittedLaneIndex == 0, 0.2d, bootstrap =>
            {
                var result = bootstrap.RequestProcessCurrentSegment();
                Debug.Log(
                    $"[CoreLoopSmokeRunner] T014 third run process -> {result} | Break={bootstrap.LastBreakResolutionResult} | Loot={bootstrap.LastLootResolutionResult} | MissionBreak={FormatMission(bootstrap, MissionTemplateId.BreakBlocks)} | MissionSegments={FormatMission(bootstrap, MissionTemplateId.CompleteSegments)} | MissionSoft={FormatMission(bootstrap, MissionTemplateId.EarnSoft)} | LastMissionReward={bootstrap.LastGrantedMissionReward} | TotalMissionRewards={bootstrap.TotalMissionRewardsGranted} | Balance={bootstrap.SoftCurrencyBalance}");
            }));
        }

        private static void StopRunner()
        {
            SetRunning(false);
            Steps.Clear();
        }

        private static void PrepareStepsForPlayMode()
        {
            EnqueueSteps();
            nextStepTime = EditorApplication.timeSinceStartup + 0.5d;
        }

        private static bool IsRunning()
        {
            return SessionState.GetBool(SessionKey, false);
        }

        private static void SetRunning(bool value)
        {
            SessionState.SetBool(SessionKey, value);
        }

        private static string FormatMission(AppBootstrap bootstrap, MissionTemplateId templateId)
        {
            var mission = bootstrap.GetMission(templateId);
            return $"{mission.Definition.Description} {mission.CurrentValue}/{mission.Definition.TargetValue} reward={mission.Definition.RewardValue}";
        }

        private readonly struct SmokeStep
        {
            public SmokeStep(string name, Func<AppBootstrap, bool> isReady, double delaySeconds, Action<AppBootstrap> execute)
            {
                Name = name;
                IsReady = isReady;
                DelaySeconds = delaySeconds;
                Execute = bootstrap =>
                {
                    Debug.Log($"[CoreLoopSmokeRunner] Step: {name}");
                    execute(bootstrap);
                };
            }

            public string Name { get; }
            public Func<AppBootstrap, bool> IsReady { get; }
            public double DelaySeconds { get; }
            public Action<AppBootstrap> Execute { get; }
        }
    }
}
