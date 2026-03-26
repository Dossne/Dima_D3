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
            var target = laneLocalPositions[CurrentLaneIndex];
            var cur = hostTransform.localPosition;
            hostTransform.localPosition = new Vector3(target.x, cur.y, cur.z);
        }

        public bool TryStartTransition(int direction)
        {
            if (IsTransitioning)
            {
                Debug.Log($"[LANE] TryStartTransition({direction}) -> result=False"); // TM-BUILD-15-TEMP
                return false;
            }

            if (direction != -1 && direction != 1)
            {
                Debug.Log($"[LANE] TryStartTransition({direction}) -> result=False"); // TM-BUILD-15-TEMP
                return false;
            }

            var requestedLane = CurrentLaneIndex + direction;
            if (requestedLane < 0 || requestedLane >= laneLocalPositions.Length)
            {
                Debug.Log($"[LANE] TryStartTransition({direction}) -> result=False"); // TM-BUILD-15-TEMP
                return false;
            }

            IsTransitioning = true;
            transitionElapsedSeconds = 0f;
            SourceLaneIndex = CurrentLaneIndex;
            targetLaneIndex = requestedLane;
            Debug.Log($"[LANE] TryStartTransition({direction}) -> result=True"); // TM-BUILD-15-TEMP
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

            var from = hostTransform.localPosition;
            var to = new Vector3(laneLocalPositions[targetLaneIndex].x, from.y, from.z);
            hostTransform.localPosition = Vector3.Lerp(from, to, normalizedTime);

            if (normalizedTime < 1f)
            {
                return;
            }

            CurrentLaneIndex = targetLaneIndex;
            var target = laneLocalPositions[CurrentLaneIndex];
            var cur = hostTransform.localPosition;
            hostTransform.localPosition = new Vector3(target.x, cur.y, cur.z);
            IsTransitioning = false;
            transitionElapsedSeconds = 0f;
        }

        public void CancelActiveTransition()
        {
            if (!IsTransitioning)
            {
                var target = laneLocalPositions[CurrentLaneIndex];
                var cur = hostTransform.localPosition;
                hostTransform.localPosition = new Vector3(target.x, cur.y, cur.z);
                return;
            }

            IsTransitioning = false;
            transitionElapsedSeconds = 0f;
            targetLaneIndex = SourceLaneIndex;
            CurrentLaneIndex = SourceLaneIndex;
            var resetTarget = laneLocalPositions[CurrentLaneIndex];
            var resetCurrent = hostTransform.localPosition;
            hostTransform.localPosition = new Vector3(resetTarget.x, resetCurrent.y, resetCurrent.z);
        }
    }
}
