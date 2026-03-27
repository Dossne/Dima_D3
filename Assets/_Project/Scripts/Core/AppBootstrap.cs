using System.Globalization;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TapMiner.Core
{
    /// <summary>
    /// Bootstrap owner for the runtime run-state machine.
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
        [SerializeField]
        private string bootstrapVersion = "0.1.0";

        [Header("Lane Runtime")]
        [SerializeField]
        private Vector3[] laneLocalPositions = null!;

        [SerializeField]
        private int initialLaneIndex = 1;

        [SerializeField]
        private float laneTransitionDurationSeconds = 0.12f;

        [SerializeField]
        private float minimumSwipeDistancePixels = 48f;

        [Header("Runtime Debug")]
        [SerializeField]
        private int debugCommittedLaneIndex;

        [SerializeField]
        private int debugTargetLaneIndex;

        [SerializeField]
        private bool debugIsLaneTransitioning;

        [Header("Loop Debug Commands")]
        [SerializeField]
        private bool debugRequestStartRun;

        [SerializeField]
        private bool debugRequestRestartRun;

        [SerializeField]
        private bool debugRequestProcessCurrentSegment;

        [SerializeField]
        private bool debugRequestBreakCurrentLaneTarget;

        [SerializeField]
        private bool debugRequestResolveCurrentLaneHazardContact;

        [SerializeField]
        private bool debugRequestLaneLeft;

        [SerializeField]
        private bool debugRequestLaneRight;

        [Header("Segment Runtime")]
        [SerializeField]
        private int initialSegmentBatchCount = 12;

        [SerializeField]
        private int debugSpawnedSegmentCount;

        [SerializeField]
        private SegmentType debugFirstSegmentType;

        [SerializeField]
        private DepthBucket debugFirstSegmentDepthBucket;

        [SerializeField]
        private int debugFirstSegmentSafeLaneIndex;

        [SerializeField]
        private SegmentVariationId debugFirstSegmentVariationId;

        [SerializeField]
        private int debugUniqueSegmentVariationCount;

        [SerializeField]
        private int debugEnemyHazardVariantCount;

        [Header("Break Runtime")]
        [SerializeField]
        private int debugActiveSegmentIndex;

        [SerializeField]
        private int debugCurrentSegmentBreakableTargetCount;

        [SerializeField]
        private BreakResolutionResult debugLastBreakResolutionResult;

        [SerializeField]
        private int debugSuccessfulBreakCount;

        [Header("Loot Runtime")]
        [SerializeField]
        private LootResolutionResult debugLastLootResolutionResult;

        [SerializeField]
        private int debugSuccessfulLootDropCount;

        [SerializeField]
        private int debugTotalGrantedLootValue;

        [SerializeField]
        private int debugLastGrantedLootValue;

        [Header("Run Reward Runtime")]
        [SerializeField]
        private int debugCurrentRunRewardValue;

        [SerializeField]
        private int debugCurrentRunRewardCount;

        [SerializeField]
        private int debugLastRewardSegmentIndex = -1;

        [SerializeField]
        private int debugLastRewardLaneIndex = -1;

        [Header("Hazard Runtime")]
        [SerializeField]
        private HazardContactResult debugLastHazardContactResult;

        [SerializeField]
        private int debugCurrentSegmentHazardTargetCount;

        [SerializeField]
        private int debugSuccessfulHazardContactCount;

        [Header("Upgrade Runtime")]
        [SerializeField]
        private int debugSoftCurrencyBalance;

        [SerializeField]
        private int debugDrillPowerLevel;

        [SerializeField]
        private int debugMaxHpLevel;

        [SerializeField]
        private int debugMoveSpeedLevel;

        [SerializeField]
        private int debugLootValueLevel;

        [SerializeField]
        private int debugCollapseResistanceLevel;

        [SerializeField]
        private float debugAppliedBreakSpeedMultiplier = 1f;

        [SerializeField]
        private int debugAppliedMaxHealth = 1;

        [SerializeField]
        private int debugCurrentHealth = 1;

        [SerializeField]
        private float debugAppliedLaneTransitionDurationSeconds;

        [SerializeField]
        private float debugAppliedLootValueMultiplier = 1f;

        [SerializeField]
        private float debugAppliedCollapseCatchRateMultiplier = 1f;

        [Header("Mission Runtime")]
        [SerializeField]
        private string debugBreakMission = string.Empty;

        [SerializeField]
        private string debugSegmentMission = string.Empty;

        [SerializeField]
        private string debugEarnSoftMission = string.Empty;

        [SerializeField]
        private MissionTemplateId debugLastCompletedMissionTemplate;

        [SerializeField]
        private int debugLastGrantedMissionReward;

        [SerializeField]
        private int debugTotalMissionRewardsGranted;

        [Header("Loop Runtime")]
        [SerializeField]
        private int debugCompletedSegmentCount;

        [SerializeField]
        private int debugLastCompletedSegmentIndex = -1;

        [Header("Feedback Runtime")]
        [SerializeField]
        private string debugCurrentFeedbackText = string.Empty;

        [SerializeField]
        private Color debugCurrentFeedbackColor = Color.white;

        [SerializeField]
        private bool debugFeedbackActive;

        [SerializeField]
        private AudioClip laneTransitionFeedbackClip;

        [SerializeField]
        private AudioClip breakFeedbackClip;

        [SerializeField]
        private AudioClip lootFeedbackClip;

        [SerializeField]
        private AudioClip hazardFeedbackClip;

        private RunStateMachine runStateMachine = null!;
        private SwipeInputInterpreter swipeInputInterpreter = null!;
        private LaneTransitionController laneTransitionController = null!;
        private SegmentSpawnSystem segmentSpawnSystem = null!;
        private BreakableBlockResolutionSystem breakableBlockResolutionSystem = null!;
        private LootDropResolutionSystem lootDropResolutionSystem = null!;
        private RunRewardAggregationSystem runRewardAggregationSystem = null!;
        private HazardContactResolutionSystem hazardContactResolutionSystem = null!;
        private UpgradePersistenceSystem upgradePersistenceSystem = null!;
        private RunHealthSystem runHealthSystem = null!;
        private MissionLayerLiteSystem missionLayerLiteSystem = null!;
        private PlaytestInstrumentationSystem playtestInstrumentationSystem = null!;
        private Text feedbackText = null!;
        private AudioSource feedbackAudioSource = null!;
        private string defaultFeedbackText = "Bootstrap OK";
        private Color defaultFeedbackColor = Color.white;
        private Vector3 defaultFeedbackScale = Vector3.one;
        private float feedbackTimerSeconds;
        private PlaytestDeathCause pendingPlaytestDeathCause;
        private bool _isPaused;
        private bool pendingStartAfterRestart;
        private RunPresentationController runPresentationController;

        // TM-CORE-01: time-driven collapse + scroll blocking
        [Header("Collapse Tuning")]
        [SerializeField] private float collapseSpeedBase = 0.04f;
        [SerializeField] private float collapseSpeedMid = 0.053f;
        [SerializeField] private float collapseSpeedLate = 0.071f;
        private float _collapseProximity;
        private bool _scrollBlocked;

        public RunState CurrentRunState => runStateMachine.CurrentState;
        public int CurrentRunContextId => runStateMachine.CurrentRunContextId;
        public bool IsMovementProcessingEnabled => runStateMachine.IsMovementProcessingEnabled;
        public bool IsDamageProcessingEnabled => runStateMachine.IsDamageProcessingEnabled;
        public bool HasActiveRunAuthority => runStateMachine.HasActiveRunAuthority;
        public bool IsPaused => _isPaused;
        public int CurrentCommittedLaneIndex => laneTransitionController != null
            ? laneTransitionController.CommittedLaneIndex
            : initialLaneIndex;
        public bool IsLaneTransitioning => laneTransitionController != null && laneTransitionController.IsTransitioning;
        public int CurrentSpawnedSegmentCount => segmentSpawnSystem.SpawnedSegments.Count;
        public int CurrentUniqueSegmentVariationCount => segmentSpawnSystem.GetUniqueVariationCount();
        public int CurrentEnemyHazardVariantCount => segmentSpawnSystem.GetEnemyHazardVariantCount();
        public BreakResolutionResult LastBreakResolutionResult => breakableBlockResolutionSystem.LastResolutionResult;
        public LootResolutionResult LastLootResolutionResult => lootDropResolutionSystem.LastResolutionResult;
        public RunRewardResult CurrentRunRewardResult => runRewardAggregationSystem.CurrentRewardResult;
        public HazardContactResult LastHazardContactResult => hazardContactResolutionSystem.LastHazardContactResult;
        public int SoftCurrencyBalance => upgradePersistenceSystem.SoftCurrencyBalance;
        public int CurrentCompletedSegmentCount => debugCompletedSegmentCount;
        public int CurrentRunHealth => runHealthSystem.CurrentHealth;
        public int CurrentRunMaxHealth => runHealthSystem.MaxHealth;
        public UpgradeStatsSnapshot CurrentUpgradeStats => upgradePersistenceSystem.CurrentStats;
        public float CurrentLaneTransitionDurationSeconds => laneTransitionController.CurrentTransitionDurationSeconds;
        public int LastGrantedMissionReward => missionLayerLiteSystem.LastGrantedRewardValue;
        public int TotalMissionRewardsGranted => missionLayerLiteSystem.TotalMissionRewardsGranted;
        public string CurrentFeedbackText => feedbackText != null ? feedbackText.text : string.Empty;
        public bool IsFeedbackActive => feedbackTimerSeconds > 0f;
        public float CollapseProximity => _collapseProximity;
        public bool IsScrollBlocked => _scrollBlocked;
        public string CurrentPlaytestSessionId => playtestInstrumentationSystem.SessionId;

        public SegmentDescriptor GetCurrentSegmentDescriptor()
        {
            if (!HasValidActiveSegment())
            {
                return null;
            }

            return segmentSpawnSystem.SpawnedSegments[debugActiveSegmentIndex];
        }

        private void Awake()
        {
            var mainCamera = Camera.main;
            var hw = mainCamera != null ? mainCamera.orthographicSize * mainCamera.aspect : 2f;
            laneLocalPositions = new[]
            {
                new Vector3(-hw * 0.46f, 0f, 0f),
                new Vector3(0f, 0f, 0f),
                new Vector3(hw * 0.46f, 0f, 0f)
            };

            runStateMachine = new RunStateMachine();
            runStateMachine.StateChanged += HandleStateChanged;
            swipeInputInterpreter = new SwipeInputInterpreter(minimumSwipeDistancePixels);
            upgradePersistenceSystem = new UpgradePersistenceSystem();
            runHealthSystem = new RunHealthSystem();
            missionLayerLiteSystem = new MissionLayerLiteSystem();
            playtestInstrumentationSystem = new PlaytestInstrumentationSystem();
            runPresentationController = GetComponent<RunPresentationController>();
            var laneHostTransform = runPresentationController != null
                ? runPresentationController.GetPlayerTransform()
                : transform;
            laneTransitionController = new LaneTransitionController(
                laneHostTransform,
                laneLocalPositions,
                laneTransitionDurationSeconds,
                initialLaneIndex);
            segmentSpawnSystem = new SegmentSpawnSystem(initialSegmentBatchCount);
            breakableBlockResolutionSystem = new BreakableBlockResolutionSystem();
            lootDropResolutionSystem = new LootDropResolutionSystem();
            runRewardAggregationSystem = new RunRewardAggregationSystem();
            hazardContactResolutionSystem = new HazardContactResolutionSystem();
            segmentSpawnSystem.ResetForRun();
            breakableBlockResolutionSystem.ResetForRun(segmentSpawnSystem.SpawnedSegments);
            lootDropResolutionSystem.ResetForRun(CurrentRunContextId);
            runRewardAggregationSystem.ResetForRun(CurrentRunContextId);
            hazardContactResolutionSystem.ResetForRun(CurrentRunContextId, segmentSpawnSystem.SpawnedSegments);
            ApplyUpgradeStatsToRuntime();
            ResetHealthForCurrentRun();
            SetupFeedbackHooks();

            SyncDebugState();

            Debug.Log($"[AppBootstrap] Started v{bootstrapVersion}");
            Debug.Log($"[AppBootstrap] Run state authority initialized in {CurrentRunState} (context {CurrentRunContextId}).");
            Debug.Log($"[AppBootstrap] Segment batch prepared with {CurrentSpawnedSegmentCount} legal segments.");
            Debug.Log($"[BOOT] Initial run state: {runStateMachine.CurrentState}"); // TM-BUILD-13-TEMP
        }

        private void Update()
        {
            if (laneTransitionController == null || swipeInputInterpreter == null || runStateMachine == null)
            {
                return;
            }

            if (!_isPaused)
            {
                swipeInputInterpreter.PollFrame();
                laneTransitionController.Tick(Time.deltaTime);

                if (swipeInputInterpreter.TryConsumeTap())
                {
                    if (!IsPointerOverUi())
                    {
                        HandleTapInteraction();
                    }
                }

                if (swipeInputInterpreter.TryConsumeSwipeDirection(out var direction))
                {
                    Debug.Log($"[INPUT] Swipe consumed: {direction} | overUI={IsPointerOverUi()}"); // TM-BUILD-15-TEMP
                    HandleMovementSwipe(direction);
                }
            }

            // TM-CORE-01: per-frame collapse + block detection
            if (CurrentRunState == RunState.RunActive && !_isPaused)
            {
                UpdateCollapseByTime();
                UpdateBlockDetection();
            }

            TickFeedback(Time.deltaTime);
            ProcessDebugCommands();
            SyncDebugState();
        }

        private static bool IsPointerOverUi()
        {
            var eventSystem = EventSystem.current;
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                return eventSystem != null &&
                       eventSystem.IsPointerOverGameObject(0);
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                return eventSystem != null &&
                       eventSystem.IsPointerOverGameObject();
            }

            return false;
        }

        private void OnDestroy()
        {
            if (runStateMachine != null)
            {
                runStateMachine.StateChanged -= HandleStateChanged;
            }
        }

        public bool RequestStartRun()
        {
            var result = TryCommand("StartRun", runStateMachine.TryStartRun);
            if (result)
            {
                playtestInstrumentationSystem.RecordRunStart(
                    CurrentRunContextId,
                    CurrentCommittedLaneIndex,
                    CurrentRunMaxHealth,
                    SoftCurrencyBalance);
            }

            return result;
        }

        public bool RequestMenuPlay()
        {
            if (CurrentRunState == RunState.RunReady)
            {
                return RequestStartRun();
            }

            if (CurrentRunState == RunState.RunDeathResolved)
            {
                pendingStartAfterRestart = true;
                return RequestRestartRun();
            }

            return false;
        }

        public bool RequestRestartRun()
        {
            SetPaused(false);
            var completedRunContextId = CurrentRunContextId;
            var result = TryCommand("RestartRun", runStateMachine.TryRestartRun);
            if (result)
            {
                playtestInstrumentationSystem.RecordRestartPressed(completedRunContextId, CurrentRunContextId);
            }

            return result;
        }

        public bool RequestTogglePause()
        {
            if (CurrentRunState != RunState.RunActive)
            {
                SetPaused(false);
                return false;
            }

            SetPaused(!_isPaused);
            return true;
        }

        public bool NotifyLethalDamage()
        {
            var previousPendingCause = pendingPlaytestDeathCause;
            pendingPlaytestDeathCause = PlaytestDeathCause.HazardContact;
            var result = TryCommand("NotifyLethalDamage", runStateMachine.TryResolveLethalDamage);
            if (!result)
            {
                pendingPlaytestDeathCause = previousPendingCause;
            }

            return result;
        }

        public bool NotifyRunInvalidFailure()
        {
            var previousPendingCause = pendingPlaytestDeathCause;
            pendingPlaytestDeathCause = PlaytestDeathCause.RunInvalidFailure;
            var result = TryCommand("NotifyRunInvalidFailure", runStateMachine.TryResolveRunInvalidFailure);
            if (!result)
            {
                pendingPlaytestDeathCause = previousPendingCause;
            }

            return result;
        }

        public bool RequestLaneTransitionLeft()
        {
            return HandleMovementSwipe(-1);
        }

        public bool RequestLaneTransitionRight()
        {
            return HandleMovementSwipe(1);
        }

        public BreakResolutionResult RequestBreakCurrentLaneTarget()
        {
            return TryResolveBreakAtLane(CurrentCommittedLaneIndex);
        }

        public BreakResolutionResult RequestBreakLane(int laneIndex)
        {
            return TryResolveBreakAtLane(laneIndex);
        }

        public bool RequestProcessCurrentSegment()
        {
            return TryProcessCurrentSegment();
        }

        public bool TryPurchaseUpgrade(UpgradeId upgradeId)
        {
            if (CurrentRunState == RunState.RunActive || CurrentRunState == RunState.RunRestarting)
            {
                return false;
            }

            var purchaseCost = upgradePersistenceSystem.GetNextPurchaseCost(upgradeId);
            var result = upgradePersistenceSystem.TryPurchase(upgradeId);
            if (!result)
            {
                return false;
            }

            playtestInstrumentationSystem.RecordUpgradePurchase(
                upgradeId,
                upgradePersistenceSystem.GetLevel(upgradeId),
                purchaseCost,
                SoftCurrencyBalance);
            ApplyUpgradeStatsToRuntime();
            ResetHealthForCurrentRun();
            SyncDebugState();
            return true;
        }

        public int GetUpgradeLevel(UpgradeId upgradeId)
        {
            return upgradePersistenceSystem.GetLevel(upgradeId);
        }

        public int GetNextUpgradeCost(UpgradeId upgradeId)
        {
            return upgradePersistenceSystem.GetNextPurchaseCost(upgradeId);
        }

        public void ReloadUpgradeProgress()
        {
            upgradePersistenceSystem.ReloadFromDisk();
            ApplyUpgradeStatsToRuntime();
            ResetHealthForCurrentRun();
            SyncDebugState();
        }

        public MissionProgress GetMission(MissionTemplateId templateId)
        {
            return missionLayerLiteSystem.GetMission(templateId);
        }

        public PlaytestSessionLog CreatePlaytestSessionLogSnapshot()
        {
            return playtestInstrumentationSystem.CreateSessionLogSnapshot();
        }

        public PlaytestReportRecord CreatePlaytestReportRecord()
        {
            return playtestInstrumentationSystem.CreateReportRecord();
        }

        public PlaytestSchemaValidationResult ValidatePlaytestSessionLogSnapshot()
        {
            return PlaytestSchemaValidator.ValidateSessionLog(CreatePlaytestSessionLogSnapshot());
        }

        public PlaytestSchemaValidationResult ValidatePlaytestReportRecord()
        {
            return PlaytestSchemaValidator.ValidateReportRecord(CreatePlaytestReportRecord());
        }

        public string CreatePlaytestCsvPreview()
        {
            var snapshot = CreatePlaytestSessionLogSnapshot();
            var builder = new StringBuilder();
            builder.AppendLine("event_type,run_context_id,depth,segment_index,lane_index,reward_delta,reward_total,death_cause,restart_latency_seconds,detail");

            for (var index = 0; index < snapshot.Events.Count; index += 1)
            {
                var entry = snapshot.Events[index];
                builder.AppendLine(
                    $"{entry.EventType},{entry.RunContextId},{entry.Depth},{entry.SegmentIndex},{entry.LaneIndex},{entry.RewardDelta},{entry.RewardTotal},{entry.DeathCause},{entry.RestartLatencySeconds.ToString("0.00", CultureInfo.InvariantCulture)},\"{entry.Detail}\"");
            }

            return builder.ToString();
        }

        public string GetSegmentSummary(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= segmentSpawnSystem.SpawnedSegments.Count)
            {
                return "Invalid segment index";
            }

            var descriptor = segmentSpawnSystem.SpawnedSegments[segmentIndex];
            var rewardLaneText = descriptor.HasRewardPath ? descriptor.RewardLaneIndex.ToString() : "none";
            return
                $"Index={descriptor.SegmentIndex} | Bucket={descriptor.DepthBucket} | Type={descriptor.SegmentType} | Variation={descriptor.VariationId} | EnemyHazard={descriptor.EnemyHazardVariantId} | Safe={descriptor.SafeLaneIndex} | Reward={rewardLaneText} | Hazards={FormatLaneMask(descriptor.HazardLaneMask)} | Breakables={FormatLaneMask(descriptor.BreakableLaneMask)} | SafePresentation={descriptor.SafePathPresentation} | RewardPresentation={descriptor.RewardPresentation} | HazardPresentation={descriptor.HazardPresentation} | EnemyBehavior={descriptor.EnemyHazardBehavior} | EnemyReadability={descriptor.EnemyHazardReadabilityNote} | Telegraph={descriptor.EnemyHazardTelegraphSeconds:0.00} | Repeat={descriptor.EnemyHazardRepeatSeconds:0.00}";
        }

        public HazardContactResult RequestResolveCurrentLaneHazardContact()
        {
            return TryResolveHazardAtLaneInternal(CurrentCommittedLaneIndex);
        }

        public HazardContactResult RequestResolveHazardAtLane(int laneIndex)
        {
            return TryResolveHazardAtLaneInternal(laneIndex);
        }

#if UNITY_EDITOR
        public void DebugAdvanceRuntimeLoop(float deltaTime)
        {
            if (laneTransitionController == null)
            {
                return;
            }

            laneTransitionController.Tick(deltaTime);
            SyncDebugState();
        }

        public void DebugResetUpgradeProgressForValidation()
        {
            upgradePersistenceSystem.DebugResetAllProgress();
            ApplyUpgradeStatsToRuntime();
            ResetHealthForCurrentRun();
            SyncDebugState();
        }

        public void DebugGrantSoftCurrencyForValidation(int amount)
        {
            upgradePersistenceSystem.DebugGrantSoftCurrency(amount);
            SyncDebugState();
        }

        public void DebugResetMissionProgressForValidation()
        {
            missionLayerLiteSystem.DebugResetAllProgress();
            SyncDebugState();
        }
#endif

        // TM-CORE-01: collapse by time
        private void UpdateCollapseByTime()
        {
            _collapseProximity += Time.deltaTime * GetCollapseSpeed();
            if (_collapseProximity >= 1.0f)
            {
                _collapseProximity = 1.0f;
                NotifyLethalDamage();
            }
        }

        private float GetCollapseSpeed()
        {
            var depth = debugCompletedSegmentCount;
            if (depth >= 25) return collapseSpeedLate;
            if (depth >= 10) return Mathf.Lerp(collapseSpeedBase, collapseSpeedMid, (depth - 10) / 15f);
            return Mathf.Lerp(collapseSpeedBase, collapseSpeedMid, depth / 10f);
        }

        // TM-CORE-01: block detection — stops scroll when a breakable block is in player's lane
        private void UpdateBlockDetection()
        {
            if (!HasValidActiveSegment())
            {
                _scrollBlocked = false;
                return;
            }

            _scrollBlocked = breakableBlockResolutionSystem.HasBreakableTargetAt(
                debugActiveSegmentIndex, CurrentCommittedLaneIndex);
        }

        // ── end TM-CORE-01 helpers ──────────────────────────────────────────────

        private bool TryCommand(string commandName, System.Func<bool> command)
        {
            var previousState = CurrentRunState;
            var result = command.Invoke();

            if (!result)
            {
                Debug.LogWarning($"[AppBootstrap] Rejected {commandName} while in {previousState}.");
            }

            return result;
        }

        private void HandleTapInteraction()
        {
            switch (CurrentRunState)
            {
                case RunState.RunReady:
                    break;
                case RunState.RunActive:
                    // TM-CORE-01: tap only drills when a block is stopping scroll
                    if (_scrollBlocked)
                    {
                        RequestProcessCurrentSegment();
                    }
                    break;
                case RunState.RunDeathResolved:
                    break;
            }
        }

        private bool HandleMovementSwipe(int direction)
        {
            Debug.Log($"[SWIPE] HandleMovementSwipe called: {direction}"); // TM-BUILD-15-TEMP
            if (!runStateMachine.CanAcceptGameplayInput())
            {
                return false;
            }

            var previousLaneIndex = CurrentCommittedLaneIndex;
            var result = laneTransitionController.TryStartTransition(direction);
            if (result)
            {
                playtestInstrumentationSystem.RecordLaneInput(
                    CurrentRunContextId,
                    previousLaneIndex,
                    laneTransitionController.TargetLaneIndex,
                    debugCompletedSegmentCount,
                    direction);
                TriggerFeedback("SHIFT", new Color(0.45f, 0.8f, 1f, 1f), 0.18f, laneTransitionFeedbackClip);
            }

            return result;
        }

        private bool TryProcessCurrentSegment()
        {
            if (!runStateMachine.CanAcceptGameplayInput() || laneTransitionController.IsTransitioning)
            {
                return false;
            }

            if (!HasValidActiveSegment())
            {
                NotifyRunInvalidFailure();
                return false;
            }

            if (breakableBlockResolutionSystem.HasBreakableTargetAt(debugActiveSegmentIndex, CurrentCommittedLaneIndex))
            {
                return TryResolveBreakAtLane(CurrentCommittedLaneIndex) == BreakResolutionResult.BreakSucceeded &&
                       AdvanceToNextSegment();
            }

            var currentSegment = segmentSpawnSystem.SpawnedSegments[debugActiveSegmentIndex];
            if (currentSegment.HazardLaneMask[CurrentCommittedLaneIndex])
            {
                return TryResolveHazardAtLaneInternal(CurrentCommittedLaneIndex) ==
                       HazardContactResult.HazardContactResolved;
            }

            return AdvanceToNextSegment();
        }

        private BreakResolutionResult TryResolveBreakAtLane(int laneIndex)
        {
            var breakResult = breakableBlockResolutionSystem.TryResolveBreak(
                debugActiveSegmentIndex,
                laneIndex,
                CurrentCommittedLaneIndex,
                runStateMachine.CanAcceptGameplayInput());

            lootDropResolutionSystem.TryResolveLoot(
                breakResult,
                CurrentRunContextId,
                debugActiveSegmentIndex,
                laneIndex);

            runRewardAggregationSystem.TryAggregateLoot(
                lootDropResolutionSystem.LastResolutionResult,
                lootDropResolutionSystem.LastGrantedLoot);

            if (breakResult == BreakResolutionResult.BreakSucceeded)
            {
                var rewardDelta = lootDropResolutionSystem.LastResolutionResult == LootResolutionResult.LootGranted &&
                                  lootDropResolutionSystem.LastGrantedLoot != null
                    ? lootDropResolutionSystem.LastGrantedLoot.LootValue
                    : 0;
                playtestInstrumentationSystem.RecordBlockBreak(
                    CurrentRunContextId,
                    debugActiveSegmentIndex,
                    laneIndex,
                    debugCompletedSegmentCount + 1,
                    rewardDelta,
                    CurrentRunRewardResult.TotalRewardValue);

                if (lootDropResolutionSystem.LastResolutionResult == LootResolutionResult.LootGranted &&
                    lootDropResolutionSystem.LastGrantedLoot != null)
                {
                    TriggerFeedback(
                        $"BREAK +{lootDropResolutionSystem.LastGrantedLoot.LootValue}",
                        new Color(1f, 0.84f, 0.25f, 1f),
                        0.26f,
                        lootFeedbackClip != null ? lootFeedbackClip : breakFeedbackClip);
                }
                else
                {
                    TriggerFeedback("BREAK", new Color(1f, 0.85f, 0.3f, 1f), 0.22f, breakFeedbackClip);
                }

                ApplyMissionReward(missionLayerLiteSystem.RecordBreakSuccess());
            }

            if (lootDropResolutionSystem.LastResolutionResult == LootResolutionResult.LootGranted &&
                lootDropResolutionSystem.LastGrantedLoot != null)
            {
                ApplyMissionReward(missionLayerLiteSystem.RecordSoftEarned(lootDropResolutionSystem.LastGrantedLoot.LootValue));
            }

            SyncDebugState();
            return breakResult;
        }

        private HazardContactResult TryResolveHazardAtLaneInternal(int laneIndex)
        {
            var hazardResult = hazardContactResolutionSystem.TryResolveHazardContact(
                CurrentRunContextId,
                debugActiveSegmentIndex,
                laneIndex,
                CurrentCommittedLaneIndex,
                runStateMachine.CanAcceptGameplayInput());

            if (hazardResult == HazardContactResult.HazardContactResolved)
            {
                TriggerFeedback("HIT", new Color(1f, 0.35f, 0.35f, 1f), 0.28f, hazardFeedbackClip);

                var acceptedDamage = runHealthSystem.TryApplyDamage(
                    damageAmount: 1,
                    canApplyDamage: runStateMachine.IsDamageProcessingEnabled,
                    out var isLethal);

                if (acceptedDamage)
                {
                    playtestInstrumentationSystem.RecordDamageTaken(
                        CurrentRunContextId,
                        debugActiveSegmentIndex,
                        laneIndex,
                        debugCompletedSegmentCount + 1,
                        runHealthSystem.CurrentHealth,
                        runHealthSystem.MaxHealth,
                        PlaytestDeathCause.HazardContact);
                }

                if (acceptedDamage && isLethal)
                {
                    NotifyLethalDamage();
                }
            }

            SyncDebugState();
            return hazardResult;
        }

        private bool AdvanceToNextSegment()
        {
            if (!HasValidActiveSegment())
            {
                NotifyRunInvalidFailure();
                return false;
            }

            debugLastCompletedSegmentIndex = debugActiveSegmentIndex;
            debugCompletedSegmentCount += 1;
            debugActiveSegmentIndex += 1;
            ApplyMissionReward(missionLayerLiteSystem.RecordSegmentCompleted());

            if (debugActiveSegmentIndex >= segmentSpawnSystem.SpawnedSegments.Count)
            {
                NotifyRunInvalidFailure();
                SyncDebugState();
                return false;
            }

            SyncDebugState();
            return true;
        }

        private bool HasValidActiveSegment()
        {
            return debugActiveSegmentIndex >= 0 &&
                   debugActiveSegmentIndex < segmentSpawnSystem.SpawnedSegments.Count;
        }

        private void ProcessDebugCommands()
        {
            if (debugRequestStartRun)
            {
                debugRequestStartRun = false;
            }

            if (debugRequestRestartRun)
            {
                debugRequestRestartRun = false;
                RequestRestartRun();
            }

            if (debugRequestLaneLeft)
            {
                debugRequestLaneLeft = false;
                RequestLaneTransitionLeft();
            }

            if (debugRequestLaneRight)
            {
                debugRequestLaneRight = false;
                RequestLaneTransitionRight();
            }

            if (debugRequestBreakCurrentLaneTarget)
            {
                debugRequestBreakCurrentLaneTarget = false;
                RequestBreakCurrentLaneTarget();
            }

            if (debugRequestResolveCurrentLaneHazardContact)
            {
                debugRequestResolveCurrentLaneHazardContact = false;
                RequestResolveCurrentLaneHazardContact();
            }

            if (debugRequestProcessCurrentSegment)
            {
                debugRequestProcessCurrentSegment = false;
                RequestProcessCurrentSegment();
            }
        }

        private void HandleStateChanged(RunState previousState, RunState newState)
        {
            if (newState == RunState.RunDeathResolved || newState == RunState.RunRestarting || newState == RunState.RunReady)
            {
                SetPaused(false);
            }

            if (newState == RunState.RunReady && pendingStartAfterRestart)
            {
                pendingStartAfterRestart = false;
                RequestStartRun();
            }

            if (previousState == RunState.RunActive && newState == RunState.RunDeathResolved)
            {
                playtestInstrumentationSystem.RecordDeath(
                    CurrentRunContextId,
                    pendingPlaytestDeathCause,
                    debugCompletedSegmentCount,
                    CurrentRunRewardResult.TotalRewardValue);
                playtestInstrumentationSystem.RecordRunResults(
                    CurrentRunContextId,
                    debugCompletedSegmentCount,
                    CurrentRunRewardResult.TotalRewardValue);
                upgradePersistenceSystem.BankReward(CurrentRunRewardResult.TotalRewardValue);
                pendingPlaytestDeathCause = PlaytestDeathCause.None;
            }

            if (newState == RunState.RunRestarting)
            {
                laneTransitionController.ResetForNewRun();
                segmentSpawnSystem.ResetForRun();
                breakableBlockResolutionSystem.ResetForRun(segmentSpawnSystem.SpawnedSegments);
                lootDropResolutionSystem.ResetForRun(CurrentRunContextId);
                runRewardAggregationSystem.ResetForRun(CurrentRunContextId);
                hazardContactResolutionSystem.ResetForRun(CurrentRunContextId, segmentSpawnSystem.SpawnedSegments);
                debugActiveSegmentIndex = 0;
                debugCompletedSegmentCount = 0;
                debugLastCompletedSegmentIndex = -1;
                // TM-CORE-01: reset collapse + block on restart
                _collapseProximity = 0f;
                _scrollBlocked = false;
                StartCoroutine(CompleteRestartNextFrame());
            }
            else if (newState != RunState.RunActive)
            {
                laneTransitionController.CancelActiveTransition();
            }

            if (newState == RunState.RunActive && previousState != RunState.RunActive)
            {
                lootDropResolutionSystem.ResetForRun(CurrentRunContextId);
                runRewardAggregationSystem.ResetForRun(CurrentRunContextId);
                breakableBlockResolutionSystem.ResetForRun(segmentSpawnSystem.SpawnedSegments);
                hazardContactResolutionSystem.ResetForRun(CurrentRunContextId, segmentSpawnSystem.SpawnedSegments);
                ApplyUpgradeStatsToRuntime();
                ResetHealthForCurrentRun();
                debugActiveSegmentIndex = 0;
                debugCompletedSegmentCount = 0;
                debugLastCompletedSegmentIndex = -1;
                // TM-CORE-01: reset collapse + block on run start
                _collapseProximity = 0f;
                _scrollBlocked = false;
            }

            SyncDebugState();
            Debug.Log($"[AppBootstrap] Run state changed {previousState} -> {newState} (context {CurrentRunContextId}).");
        }

        private void SetPaused(bool isPaused)
        {
            _isPaused = isPaused;
            if (runPresentationController != null)
            {
                runPresentationController.SetPauseOverlayVisible(_isPaused);
            }
        }

        private IEnumerator CompleteRestartNextFrame()
        {
            yield return null;

            if (runStateMachine == null || CurrentRunState != RunState.RunRestarting)
            {
                yield break;
            }

            var result = runStateMachine.TryCompleteRestart();
            Debug.Log("[STATE] TryCompleteRestart result: " + result);
        }

        private void SyncDebugState()
        {
            if (laneTransitionController == null)
            {
                return;
            }

            debugCommittedLaneIndex = laneTransitionController.CommittedLaneIndex;
            debugTargetLaneIndex = laneTransitionController.TargetLaneIndex;
            debugIsLaneTransitioning = laneTransitionController.IsTransitioning;

            if (segmentSpawnSystem.SpawnedSegments.Count <= 0)
            {
                debugSpawnedSegmentCount = 0;
                return;
            }

            var firstSegment = segmentSpawnSystem.SpawnedSegments[0];
            debugSpawnedSegmentCount = segmentSpawnSystem.SpawnedSegments.Count;
            debugFirstSegmentType = firstSegment.SegmentType;
            debugFirstSegmentDepthBucket = firstSegment.DepthBucket;
            debugFirstSegmentSafeLaneIndex = firstSegment.SafeLaneIndex;
            debugFirstSegmentVariationId = firstSegment.VariationId;
            debugUniqueSegmentVariationCount = segmentSpawnSystem.GetUniqueVariationCount();
            debugEnemyHazardVariantCount = segmentSpawnSystem.GetEnemyHazardVariantCount();
            debugCurrentSegmentBreakableTargetCount =
                breakableBlockResolutionSystem.GetRemainingBreakableTargetCount(debugActiveSegmentIndex);
            debugLastBreakResolutionResult = breakableBlockResolutionSystem.LastResolutionResult;
            debugSuccessfulBreakCount = breakableBlockResolutionSystem.SuccessfulBreakCount;
            debugLastLootResolutionResult = lootDropResolutionSystem.LastResolutionResult;
            debugSuccessfulLootDropCount = lootDropResolutionSystem.SuccessfulLootDropCount;
            debugTotalGrantedLootValue = lootDropResolutionSystem.TotalGrantedLootValue;
            debugLastGrantedLootValue = lootDropResolutionSystem.LastGrantedLoot != null
                ? lootDropResolutionSystem.LastGrantedLoot.LootValue
                : 0;
            debugCurrentRunRewardValue = runRewardAggregationSystem.CurrentRewardResult.TotalRewardValue;
            debugCurrentRunRewardCount = runRewardAggregationSystem.CurrentRewardResult.GrantedLootCount;
            debugLastRewardSegmentIndex = runRewardAggregationSystem.CurrentRewardResult.LastGrantedSegmentIndex;
            debugLastRewardLaneIndex = runRewardAggregationSystem.CurrentRewardResult.LastGrantedLaneIndex;
            debugLastHazardContactResult = hazardContactResolutionSystem.LastHazardContactResult;
            debugCurrentSegmentHazardTargetCount =
                hazardContactResolutionSystem.GetHazardTargetCount(debugActiveSegmentIndex);
            debugSuccessfulHazardContactCount = hazardContactResolutionSystem.SuccessfulHazardContactCount;
            debugSoftCurrencyBalance = upgradePersistenceSystem.SoftCurrencyBalance;
            debugDrillPowerLevel = upgradePersistenceSystem.GetLevel(UpgradeId.DrillPower);
            debugMaxHpLevel = upgradePersistenceSystem.GetLevel(UpgradeId.MaxHp);
            debugMoveSpeedLevel = upgradePersistenceSystem.GetLevel(UpgradeId.MoveSpeed);
            debugLootValueLevel = upgradePersistenceSystem.GetLevel(UpgradeId.LootValue);
            debugCollapseResistanceLevel = upgradePersistenceSystem.GetLevel(UpgradeId.CollapseResistance);
            debugAppliedBreakSpeedMultiplier = CurrentUpgradeStats.BlockBreakSpeedMultiplier;
            debugAppliedMaxHealth = CurrentUpgradeStats.MaxHealth;
            debugCurrentHealth = runHealthSystem.CurrentHealth;
            debugAppliedLaneTransitionDurationSeconds = laneTransitionController.CurrentTransitionDurationSeconds;
            debugAppliedLootValueMultiplier = lootDropResolutionSystem.LootValueMultiplier;
            debugAppliedCollapseCatchRateMultiplier = hazardContactResolutionSystem.CollapseCatchRateMultiplier;
            debugBreakMission = FormatMissionDebug(missionLayerLiteSystem.GetMission(MissionTemplateId.BreakBlocks));
            debugSegmentMission = FormatMissionDebug(missionLayerLiteSystem.GetMission(MissionTemplateId.CompleteSegments));
            debugEarnSoftMission = FormatMissionDebug(missionLayerLiteSystem.GetMission(MissionTemplateId.EarnSoft));
            debugLastCompletedMissionTemplate = missionLayerLiteSystem.LastCompletedTemplateId;
            debugLastGrantedMissionReward = missionLayerLiteSystem.LastGrantedRewardValue;
            debugTotalMissionRewardsGranted = missionLayerLiteSystem.TotalMissionRewardsGranted;
            debugCurrentFeedbackText = feedbackText != null ? feedbackText.text : string.Empty;
            debugCurrentFeedbackColor = feedbackText != null ? feedbackText.color : Color.white;
            debugFeedbackActive = IsFeedbackActive;
        }

        private void ApplyUpgradeStatsToRuntime()
        {
            var stats = upgradePersistenceSystem.CurrentStats;
            laneTransitionController.SetTransitionDurationMultiplier(stats.LaneTransitionDurationMultiplier);
            lootDropResolutionSystem.SetLootValueMultiplier(stats.LootValueMultiplier);
            breakableBlockResolutionSystem.SetBreakSpeedMultiplier(stats.BlockBreakSpeedMultiplier);
            hazardContactResolutionSystem.SetCollapseCatchRateMultiplier(stats.CollapseCatchRateMultiplier);
        }

        private void ResetHealthForCurrentRun()
        {
            runHealthSystem.ResetForRun(CurrentUpgradeStats.MaxHealth);
        }

        private void ApplyMissionReward(int rewardValue)
        {
            if (rewardValue <= 0)
            {
                return;
            }

            upgradePersistenceSystem.BankReward(rewardValue);
            playtestInstrumentationSystem.RecordMissionComplete(
                missionLayerLiteSystem.LastCompletedTemplateId,
                missionLayerLiteSystem.LastCompletedMissionDescription,
                rewardValue);
        }

        private static string FormatMissionDebug(MissionProgress missionProgress)
        {
            return $"{missionProgress.Definition.DisplayName}: {missionProgress.CurrentValue}/{missionProgress.Definition.TargetValue} | Reward={missionProgress.Definition.RewardValue}";
        }

        private static string FormatLaneMask(bool[] laneMask)
        {
            return $"{(laneMask[0] ? 1 : 0)}{(laneMask[1] ? 1 : 0)}{(laneMask[2] ? 1 : 0)}";
        }

        private void SetupFeedbackHooks()
        {
            feedbackText = GameObject.Find("BootstrapStatusText")?.GetComponent<Text>();
            if (feedbackText != null)
            {
                defaultFeedbackText = feedbackText.text;
                defaultFeedbackColor = feedbackText.color;
                defaultFeedbackScale = feedbackText.rectTransform.localScale;
            }

            feedbackAudioSource = GetComponent<AudioSource>();
            if (feedbackAudioSource == null)
            {
                feedbackAudioSource = gameObject.AddComponent<AudioSource>();
                feedbackAudioSource.playOnAwake = false;
                feedbackAudioSource.loop = false;
                feedbackAudioSource.spatialBlend = 0f;
                feedbackAudioSource.volume = 0.65f;
            }
        }

        private void TriggerFeedback(string message, Color color, float durationSeconds, AudioClip clip)
        {
            if (feedbackText == null)
            {
                return;
            }

            feedbackText.text = message;
            feedbackText.color = color;
            feedbackText.rectTransform.localScale = defaultFeedbackScale * 1.08f;
            feedbackTimerSeconds = Mathf.Max(0.01f, durationSeconds);

            if (clip != null && feedbackAudioSource != null)
            {
                feedbackAudioSource.PlayOneShot(clip);
            }
        }

        private void TickFeedback(float deltaTime)
        {
            if (feedbackText == null || feedbackTimerSeconds <= 0f)
            {
                return;
            }

            feedbackTimerSeconds = Mathf.Max(0f, feedbackTimerSeconds - Mathf.Max(0f, deltaTime));
            feedbackText.rectTransform.localScale = Vector3.Lerp(
                feedbackText.rectTransform.localScale,
                defaultFeedbackScale,
                16f * Mathf.Max(0f, deltaTime));

            if (feedbackTimerSeconds > 0f)
            {
                return;
            }

            feedbackText.text = defaultFeedbackText;
            feedbackText.color = defaultFeedbackColor;
            feedbackText.rectTransform.localScale = defaultFeedbackScale;
        }
    }
}
