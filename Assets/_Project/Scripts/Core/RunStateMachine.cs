using System;

namespace TapMiner.Core
{
    /// <summary>
    /// Sole runtime authority for the locked run-state flow defined in T001.
    /// </summary>
    public sealed class RunStateMachine
    {
        public event Action<RunState, RunState> StateChanged;

        public RunState CurrentState { get; private set; }
        public bool IsMovementProcessingEnabled { get; private set; }
        public bool IsDamageProcessingEnabled { get; private set; }
        public bool IsTerminalRun { get; private set; }
        public bool HasActiveRunAuthority { get; private set; }
        public int CurrentRunContextId { get; private set; }

        private bool isTransitionInProgress;

        public RunStateMachine()
        {
            CurrentState = RunState.RunReady;
            EnterState(CurrentState, previousState: null);
        }

        public bool TryStartRun()
        {
            return TryTransition(RunState.RunActive, "Start run command accepted.");
        }

        public bool TryResolveLethalDamage()
        {
            return TryTransition(RunState.RunDeathResolved, "Lethal damage resolved.");
        }

        public bool TryResolveRunInvalidFailure()
        {
            return TryTransition(RunState.RunDeathResolved, "Run-invalid failure resolved.");
        }

        public bool TryRestartRun()
        {
            return TryTransition(RunState.RunRestarting, "One-tap restart accepted.");
        }

        public bool TryCompleteRestart()
        {
            return TryTransition(RunState.RunReady, "Restart complete, waiting for player.");
        }

        public bool CanAcceptGameplayInput()
        {
            return CurrentState == RunState.RunActive && IsMovementProcessingEnabled;
        }

        private bool TryTransition(RunState targetState, string reason)
        {
            if (isTransitionInProgress)
            {
                return false;
            }

            if (!IsAllowedTransition(CurrentState, targetState))
            {
                return false;
            }

            isTransitionInProgress = true;

            try
            {
                var previousState = CurrentState;
                ExitState(previousState);
                CurrentState = targetState;
                EnterState(targetState, previousState);
                StateChanged?.Invoke(previousState, targetState);
                return true;
            }
            finally
            {
                isTransitionInProgress = false;
            }
        }

        private static bool IsAllowedTransition(RunState currentState, RunState targetState)
        {
            return currentState switch
            {
                RunState.RunReady => targetState == RunState.RunActive,
                RunState.RunActive => targetState == RunState.RunDeathResolved,
                RunState.RunDeathResolved => targetState == RunState.RunRestarting,
                RunState.RunRestarting => targetState == RunState.RunReady,
                _ => false
            };
        }

        private void ExitState(RunState state)
        {
            switch (state)
            {
                case RunState.RunReady:
                    // T001 exit action: opening gameplay processing on accepted start.
                    break;
                case RunState.RunActive:
                    // T001 exit action: close movement/damage and freeze terminal run.
                    IsMovementProcessingEnabled = false;
                    IsDamageProcessingEnabled = false;
                    IsTerminalRun = true;
                    HasActiveRunAuthority = false;
                    break;
                case RunState.RunDeathResolved:
                    // T001 exit action: begin creation of a fresh run context.
                    break;
                case RunState.RunRestarting:
                    // T001 repaired exit action: open the fresh run immediately.
                    break;
            }
        }

        private void EnterState(RunState state, RunState? previousState)
        {
            switch (state)
            {
                case RunState.RunReady:
                    CurrentRunContextId += 1;
                    IsMovementProcessingEnabled = false;
                    IsDamageProcessingEnabled = false;
                    IsTerminalRun = false;
                    HasActiveRunAuthority = false;
                    break;

                case RunState.RunActive:
                    if (previousState == RunState.RunRestarting)
                    {
                        // Restarting already created the fresh run context and reset run-scoped flags.
                        IsTerminalRun = false;
                    }

                    IsMovementProcessingEnabled = true;
                    IsDamageProcessingEnabled = true;
                    HasActiveRunAuthority = true;
                    break;

                case RunState.RunDeathResolved:
                    IsMovementProcessingEnabled = false;
                    IsDamageProcessingEnabled = false;
                    IsTerminalRun = true;
                    HasActiveRunAuthority = false;
                    break;

                case RunState.RunRestarting:
                    CurrentRunContextId += 1;
                    IsMovementProcessingEnabled = false;
                    IsDamageProcessingEnabled = false;
                    IsTerminalRun = false;
                    HasActiveRunAuthority = false;
                    break;
            }
        }
    }
}
