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

        private RunStateMachine runStateMachine = null!;
        private SwipeInputInterpreter swipeInputInterpreter = null!;
        private LaneTransitionController laneTransitionController = null!;

        public RunState CurrentRunState => runStateMachine.CurrentState;
        public int CurrentRunContextId => runStateMachine.CurrentRunContextId;
        public bool IsMovementProcessingEnabled => runStateMachine.IsMovementProcessingEnabled;
        public bool IsDamageProcessingEnabled => runStateMachine.IsDamageProcessingEnabled;
        public bool HasActiveRunAuthority => runStateMachine.HasActiveRunAuthority;
        public int CurrentCommittedLaneIndex => laneTransitionController.CommittedLaneIndex;
        public bool IsLaneTransitioning => laneTransitionController.IsTransitioning;

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

            SyncDebugState();

            Debug.Log($"[AppBootstrap] Started v{bootstrapVersion}");
            Debug.Log($"[AppBootstrap] Run state authority initialized in {CurrentRunState} (context {CurrentRunContextId}).");
        }

        private void Update()
        {
            laneTransitionController.Tick(Time.deltaTime);

            if (swipeInputInterpreter.TryConsumeSwipeDirection(out var direction))
            {
                HandleMovementSwipe(direction);
            }

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

        private bool HandleMovementSwipe(int direction)
        {
            if (!runStateMachine.CanAcceptGameplayInput())
            {
                return false;
            }

            return laneTransitionController.TryStartTransition(direction);
        }

        private void HandleStateChanged(RunState previousState, RunState newState)
        {
            if (newState == RunState.RunRestarting)
            {
                laneTransitionController.ResetForNewRun();
            }
            else if (newState != RunState.RunActive)
            {
                laneTransitionController.CancelActiveTransition();
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
        }
    }
}
