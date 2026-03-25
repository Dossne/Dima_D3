using UnityEngine;

namespace TapMiner.Core
{
    /// <summary>
    /// Executes adjacent-lane transitions with a single active transition at a time.
    /// </summary>
    public sealed class LaneTransitionController
    {
        private readonly Transform hostTransform;
        private readonly Vector3[] laneLocalPositions;
        private readonly float baseTransitionDurationSeconds;
        private readonly int initialLaneIndex;

        private float transitionElapsedSeconds;
        private float transitionDurationMultiplier = 1f;
        private int targetLaneIndex;

        public int CurrentLaneIndex { get; private set; }
        public int CommittedLaneIndex => IsTransitioning ? SourceLaneIndex : CurrentLaneIndex;
        public int SourceLaneIndex { get; private set; }
        public int TargetLaneIndex => targetLaneIndex;
        public bool IsTransitioning { get; private set; }
        public float CurrentTransitionDurationSeconds => baseTransitionDurationSeconds * transitionDurationMultiplier;

        public LaneTransitionController(
            Transform hostTransform,
            Vector3[] laneLocalPositions,
            float transitionDurationSeconds,
            int initialLaneIndex)
        {
            this.hostTransform = hostTransform;
            this.laneLocalPositions = laneLocalPositions;
            baseTransitionDurationSeconds = Mathf.Max(0.01f, transitionDurationSeconds);
            this.initialLaneIndex = Mathf.Clamp(initialLaneIndex, 0, laneLocalPositions.Length - 1);

            ResetForNewRun();
        }

        public void SetTransitionDurationMultiplier(float multiplier)
        {
            transitionDurationMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public void ResetForNewRun()
        {
            IsTransitioning = false;
            transitionElapsedSeconds = 0f;
            CurrentLaneIndex = initialLaneIndex;
            SourceLaneIndex = initialLaneIndex;
            targetLaneIndex = initialLaneIndex;
            hostTransform.localPosition = laneLocalPositions[CurrentLaneIndex];
        }

        public bool TryStartTransition(int direction)
        {
            if (IsTransitioning)
            {
                return false;
            }

            if (direction != -1 && direction != 1)
            {
                return false;
            }

            var requestedLane = CurrentLaneIndex + direction;
            if (requestedLane < 0 || requestedLane >= laneLocalPositions.Length)
            {
                return false;
            }

            IsTransitioning = true;
            transitionElapsedSeconds = 0f;
            SourceLaneIndex = CurrentLaneIndex;
            targetLaneIndex = requestedLane;
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsTransitioning)
            {
                return;
            }

            transitionElapsedSeconds += Mathf.Max(0f, deltaTime);
            var normalizedTime = Mathf.Clamp01(transitionElapsedSeconds / CurrentTransitionDurationSeconds);

            hostTransform.localPosition = Vector3.Lerp(
                laneLocalPositions[SourceLaneIndex],
                laneLocalPositions[targetLaneIndex],
                normalizedTime);

            if (normalizedTime < 1f)
            {
                return;
            }

            CurrentLaneIndex = targetLaneIndex;
            hostTransform.localPosition = laneLocalPositions[CurrentLaneIndex];
            IsTransitioning = false;
            transitionElapsedSeconds = 0f;
        }

        public void CancelActiveTransition()
        {
            if (!IsTransitioning)
            {
                hostTransform.localPosition = laneLocalPositions[CurrentLaneIndex];
                return;
            }

            IsTransitioning = false;
            transitionElapsedSeconds = 0f;
            targetLaneIndex = SourceLaneIndex;
            CurrentLaneIndex = SourceLaneIndex;
            hostTransform.localPosition = laneLocalPositions[CurrentLaneIndex];
        }
    }
}
