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
                    $"[CoreLoopSmokeRunner] Process segment 0 -> {result} | Lane={bootstrap.CurrentCommittedLaneIndex} | Break={bootstrap.LastBreakResolutionResult} | Loot={bootstrap.LastLootResolutionResult}");
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
                var result = bootstrap.RequestRestartRun();
                Debug.Log(
                    $"[CoreLoopSmokeRunner] Restart run -> {result} | RunState={bootstrap.CurrentRunState} | Context={bootstrap.CurrentRunContextId}");
            }));

            Steps.Enqueue(new SmokeStep("Report final state", bootstrap => bootstrap.CurrentRunState == RunState.RunActive && !bootstrap.IsLaneTransitioning, 0.1d, bootstrap =>
            {
                Debug.Log(
                    $"[CoreLoopSmokeRunner] Final state | RunState={bootstrap.CurrentRunState} | Lane={bootstrap.CurrentCommittedLaneIndex} | Segments={bootstrap.CurrentSpawnedSegmentCount} | Break={bootstrap.LastBreakResolutionResult} | Loot={bootstrap.LastLootResolutionResult} | Hazard={bootstrap.LastHazardContactResult}");
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
