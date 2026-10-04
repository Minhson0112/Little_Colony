using UnityEngine;

namespace LittleColony
{
    /// <summary>Tracks a content drag independently of IMGUI button capture and scrollbars.</summary>
    public sealed class DragScrollView
    {
        private Rect viewport;
        private Vector2 contentSize;
        private Vector2 startPosition;
        private Vector2 startScroll;
        private int pointerId;
        private bool tracking;
        private bool dragging;

        /// <summary>Gets whether buttons must ignore this frame's drag or release.</summary>
        public bool SuppressClicks { get; private set; }

        /// <summary>Registers the current viewport and scrollable content in interface coordinates.</summary>
        public void Configure(Rect bounds, Vector2 size)
        {
            viewport = bounds;
            contentSize = size;
        }

        /// <summary>Clears capture when a list is closed or its content is replaced.</summary>
        public void Cancel()
        {
            tracking = false;
            dragging = false;
            SuppressClicks = false;
        }

        /// <summary>Updates one touch, or the desktop mouse, once per frame in scaled GUI coordinates.</summary>
        public Vector2 UpdateInput(Vector2 scroll, float scale)
        {
            SuppressClicks = dragging;
            if (Input.touchCount > 1)
            {
                tracking = false;
                dragging = false;
                SuppressClicks = true;
                return scroll;
            }

            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                Vector2 position = new Vector2(touch.position.x, Screen.height - touch.position.y) / scale;
                return ProcessPointer(scroll, position, touch.fingerId,
                    touch.phase == TouchPhase.Began,
                    touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled, scale);
            }

            if (tracking && pointerId >= 0)
            {
                // A lost touch must not leave buttons suppressed until the next gesture.
                tracking = false;
                dragging = false;
                return scroll;
            }

            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y) / scale;
            return ProcessPointer(scroll, mouse, -1, Input.GetMouseButtonDown(0),
                !Input.GetMouseButton(0), scale);
        }

        /// <summary>Distinguishes taps from drags, clamps scrolling, and suppresses drag-release actions.</summary>
        public Vector2 ProcessPointer(Vector2 scroll, Vector2 position, int id, bool began, bool ended, float scale)
        {
            if (began)
            {
                // Leave the scrollbar tracks available to their native IMGUI controls.
                Rect contentArea = new Rect(viewport.x, viewport.y,
                    viewport.width - 16, viewport.height - 16);
                tracking = contentArea.Contains(position);
                dragging = false;
                SuppressClicks = false;
                pointerId = id;
                startPosition = position;
                startScroll = scroll;
            }

            if (!tracking || pointerId != id)
            {
                return scroll;
            }

            Vector2 movement = position - startPosition;
            if (movement.magnitude * scale >= 8)
            {
                dragging = true;
            }

            if (dragging)
            {
                SuppressClicks = true;
                scroll = startScroll - movement;
                scroll.x = Mathf.Clamp(scroll.x, 0, Mathf.Max(0, contentSize.x - viewport.width));
                scroll.y = Mathf.Clamp(scroll.y, 0, Mathf.Max(0, contentSize.y - viewport.height));
            }

            if (ended)
            {
                tracking = false;
                dragging = false;
            }

            return scroll;
        }
    }
}
