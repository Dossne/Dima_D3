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

        private bool pointerTrackingActive;
        private Vector2 pointerStartPosition;

        public SwipeInputInterpreter(float minimumSwipeDistancePixels)
        {
            this.minimumSwipeDistancePixels = Mathf.Max(1f, minimumSwipeDistancePixels);
        }

        public bool TryConsumeSwipeDirection(out int direction)
        {
            direction = 0;

            if (TryConsumeTouchSwipe(out direction))
            {
                return true;
            }

            return TryConsumeMouseSwipe(out direction);
        }

        private bool TryConsumeTouchSwipe(out int direction)
        {
            direction = 0;
            var pointer = Pointer.current;
            if (pointer == null)
            {
                return false;
            }

            if (pointer.press.wasPressedThisFrame)
            {
                pointerTrackingActive = true;
                pointerStartPosition = pointer.position.ReadValue();
                return false;
            }

            if (pointer.press.wasReleasedThisFrame)
            {
                if (!pointerTrackingActive)
                {
                    return false;
                }

                pointerTrackingActive = false;
                return TryClassifySwipe(pointerStartPosition, pointer.position.ReadValue(), out direction);
            }

            return false;
        }

        private bool TryConsumeMouseSwipe(out int direction)
        {
            direction = 0;
            return false;
        }

        private bool TryClassifySwipe(Vector2 startPosition, Vector2 endPosition, out int direction)
        {
            direction = 0;
            var delta = endPosition - startPosition;
            var horizontalDistance = Mathf.Abs(delta.x);
            var verticalDistance = Mathf.Abs(delta.y);

            if (horizontalDistance < minimumSwipeDistancePixels)
            {
                return false;
            }

            if (horizontalDistance <= verticalDistance)
            {
                return false;
            }

            direction = delta.x < 0f ? -1 : 1;
            return true;
        }
    }
}
