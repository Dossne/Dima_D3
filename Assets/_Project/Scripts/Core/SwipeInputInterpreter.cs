using UnityEngine;
using UnityEngine.InputSystem;

namespace TapMiner.Core
{
    /// <summary>
    /// Interprets completed touch or mouse drags into horizontal swipe intents only.
    /// </summary>
    public sealed class SwipeInputInterpreter
    {
        private readonly float minimumSwipeDistancePixels;

        private int lastPolledFrame = -1;
        private bool pointerTrackingActive;
        private bool pendingTap;
        private int pendingSwipeDirection;
        private Vector2 pointerStartPosition;

        public SwipeInputInterpreter(float minimumSwipeDistancePixels)
        {
            this.minimumSwipeDistancePixels = Mathf.Max(1f, minimumSwipeDistancePixels);
        }

        public void PollFrame()
        {
            if (lastPolledFrame == Time.frameCount)
            {
                return;
            }

            lastPolledFrame = Time.frameCount;
            PollPointerFrame();
        }

        public bool TryConsumeTap()
        {
            if (!pendingTap)
            {
                return false;
            }

            pendingTap = false;
            return true;
        }

        public bool TryConsumeSwipeDirection(out int direction)
        {
            if (pendingSwipeDirection == 0)
            {
                direction = 0;
                return false;
            }

            direction = pendingSwipeDirection;
            pendingSwipeDirection = 0;
            return true;
        }

        private void PollPointerFrame()
        {
            var pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            if (pointer.press.wasPressedThisFrame)
            {
                pointerTrackingActive = true;
                pointerStartPosition = pointer.position.ReadValue();
                return;
            }

            if (pointer.press.wasReleasedThisFrame)
            {
                if (!pointerTrackingActive)
                {
                    return;
                }

                pointerTrackingActive = false;
                ClassifyPointerRelease(pointerStartPosition, pointer.position.ReadValue());
            }
        }

        private void ClassifyPointerRelease(Vector2 startPosition, Vector2 endPosition)
        {
            var delta = endPosition - startPosition;
            var horizontalDistance = Mathf.Abs(delta.x);
            var verticalDistance = Mathf.Abs(delta.y);

            if (horizontalDistance < minimumSwipeDistancePixels)
            {
                pendingTap = true;
                pendingSwipeDirection = 0;
                return;
            }

            if (horizontalDistance <= verticalDistance)
            {
                pendingTap = false;
                pendingSwipeDirection = 0;
                return;
            }

            pendingTap = false;
            pendingSwipeDirection = delta.x < 0f ? -1 : 1;
        }
    }
}
