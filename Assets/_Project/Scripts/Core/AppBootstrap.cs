using UnityEngine;

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
        private Vector3[] laneLocalPositions =
        {
            new Vector3(-2f, 0f, 0f),
            Vector3.zero,
            new Vector3(2f, 0f, 0f)
        };

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

        [Header("Hazard Runtime")]
        [SerializeField]
        private HazardContactResult debugLastHazardContactResult;

        [SerializeField]
        private int debugCurrentSegmentHazardTargetCount;

        [SerializeField]
        private int debugSuccessfulHazardContactCount;

        [Header("Loop Runtime")]
        [SerializeField]
        private int debugCompletedSegmentCount;

        [SerializeField]
        private int debugLastCompletedSegmentIndex = -1;

        private RunStateMachine runStateMachine = null!;
        private SwipeInputInterpreter swipeInputInterpreter = null!;
        private LaneTransitionController laneTransitionController = null!;
        private SegmentSpawnSystem segmentSpawnSystem = null!;
        private BreakableBlockResolutionSystem breakableBlockResolutionSystem = null!;
        private LootDropResolutionSystem lootDropResolutionSystem = null!;
        private HazardContactResolutionSystem hazardContactResolutionSystem = null!;

        public RunState CurrentRunState => runStateMachine.CurrentState;
        public int CurrentRunContextId => runStateMachine.CurrentRunContextId;
        public bool IsMovementProcessingEnabled => runStateMachine.IsMovementProcessingEnabled;
        public bool IsDamageProcessingEnabled => runStateMachine.IsDamageProcessingEnabled;
        public bool HasActiveRunAuthority => runStateMachine.HasActiveRunAuthority;
        public int CurrentCommittedLaneIndex => laneTransitionController.CommittedLaneIndex;
        public bool IsLaneTransitioning => laneTransitionController.IsTransitioning;
        public int CurrentSpawnedSegmentCount => segmentSpawnSystem.SpawnedSegments.Count;
        public BreakResolutionResult LastBreakResolutionResult => breakableBlockResolutionSystem.LastResolutionResult;
        public LootResolutionResult LastLootResolutionResult => lootDropResolutionSystem.LastResolutionResult;
        public HazardContactResult LastHazardContactResult => hazardContactResolutionSystem.LastHazardContactResult;

        private void Awake()
        {
            runStateMachine = new RunStateMachine();
            runStateMachine.StateChanged += HandleStateChanged;
            swipeInputInterpreter = new SwipeInputInterpreter(minimumSwipeDistancePixels);
            laneTransitionController = new LaneTransitionController(
                transform,
                laneLocalPositions,
                laneTransitionDurationSeconds,
                initialLaneIndex);
            segmentSpawnSystem = new SegmentSpawnSystem(initialSegmentBatchCount);
            breakableBlockResolutionSystem = new BreakableBlockResolutionSystem();
            lootDropResolutionSystem = new LootDropResolutionSystem();
            hazardContactResolutionSystem = new HazardContactResolutionSystem();
            segmentSpawnSystem.ResetForRun();
            breakableBlockResolutionSystem.ResetForRun(segmentSpawnSystem.SpawnedSegments);
            lootDropResolutionSystem.ResetForRun(CurrentRunContextId);
            hazardContactResolutionSystem.ResetForRun(CurrentRunContextId, segmentSpawnSystem.SpawnedSegments);

            SyncDebugState();

            Debug.Log($"[AppBootstrap] Started v{bootstrapVersion}");
            Debug.Log($"[AppBootstrap] Run state authority initialized in {CurrentRunState} (context {CurrentRunContextId}).");
            Debug.Log($"[AppBootstrap] Segment batch prepared with {CurrentSpawnedSegmentCount} legal segments.");
        }

        private void Update()
        {
            if (laneTransitionController == null || swipeInputInterpreter == null || runStateMachine == null)
            {
                return;
            }

            swipeInputInterpreter.PollFrame();
            laneTransitionController.Tick(Time.deltaTime);

            if (swipeInputInterpreter.TryConsumeTap())
            {
                HandleTapInteraction();
            }

            if (swipeInputInterpreter.TryConsumeSwipeDirection(out var direction))
            {
                HandleMovementSwipe(direction);
            }
            ProcessDebugCommands();
            SyncDebugState();
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
            return TryCommand("StartRun", runStateMachine.TryStartRun);
        }

        public bool RequestRestartRun()
        {
            return TryCommand("RestartRun", runStateMachine.TryRestartRun);
        }

        public bool NotifyLethalDamage()
        {
            return TryCommand("NotifyLethalDamage", runStateMachine.TryResolveLethalDamage);
        }

        public bool NotifyRunInvalidFailure()
        {
            return TryCommand("NotifyRunInvalidFailure", runStateMachine.TryResolveRunInvalidFailure);
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
#endif

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
                    RequestStartRun();
                    break;
                case RunState.RunActive:
                    RequestProcessCurrentSegment();
                    break;
                case RunState.RunDeathResolved:
                    RequestRestartRun();
                    break;
            }
        }

        private bool HandleMovementSwipe(int direction)
        {
            if (!runStateMachine.CanAcceptGameplayInput())
            {
                return false;
            }

            return laneTransitionController.TryStartTransition(direction);
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
                NotifyLethalDamage();
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
                RequestStartRun();
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
            if (newState == RunState.RunRestarting)
            {
                laneTransitionController.ResetForNewRun();
                segmentSpawnSystem.ResetForRun();
                breakableBlockResolutionSystem.ResetForRun(segmentSpawnSystem.SpawnedSegments);
                lootDropResolutionSystem.ResetForRun(CurrentRunContextId);
                hazardContactResolutionSystem.ResetForRun(CurrentRunContextId, segmentSpawnSystem.SpawnedSegments);
                debugActiveSegmentIndex = 0;
                debugCompletedSegmentCount = 0;
                debugLastCompletedSegmentIndex = -1;
            }
            else if (newState != RunState.RunActive)
            {
                laneTransitionController.CancelActiveTransition();
            }

            if (newState == RunState.RunActive && previousState != RunState.RunActive)
            {
                lootDropResolutionSystem.ResetForRun(CurrentRunContextId);
                breakableBlockResolutionSystem.ResetForRun(segmentSpawnSystem.SpawnedSegments);
                hazardContactResolutionSystem.ResetForRun(CurrentRunContextId, segmentSpawnSystem.SpawnedSegments);
                debugActiveSegmentIndex = 0;
                debugCompletedSegmentCount = 0;
                debugLastCompletedSegmentIndex = -1;
            }

            SyncDebugState();
            Debug.Log($"[AppBootstrap] Run state changed {previousState} -> {newState} (context {CurrentRunContextId}).");
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
            debugLastHazardContactResult = hazardContactResolutionSystem.LastHazardContactResult;
            debugCurrentSegmentHazardTargetCount =
                hazardContactResolutionSystem.GetHazardTargetCount(debugActiveSegmentIndex);
            debugSuccessfulHazardContactCount = hazardContactResolutionSystem.SuccessfulHazardContactCount;
        }
    }
}
