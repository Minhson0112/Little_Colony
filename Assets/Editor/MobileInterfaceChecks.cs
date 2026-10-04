using System;
using UnityEditor;
using UnityEngine;

namespace LittleColony.Editor
{
    /// <summary>Checks gesture behavior that cannot be verified by a desktop mouse wheel.</summary>
    public static class MobileInterfaceChecks
    {
        /// <summary>Verifies tap, drag, release, bounds, pointer ownership, and cancellation behavior.</summary>
        [MenuItem("Little Colony/Check Mobile Interface")]
        public static void Run()
        {
            var list = new DragScrollView();
            list.Configure(new Rect(100, 100, 300, 200), new Vector2(600, 800));
            Vector2 scroll = new Vector2(40, 100);
            list.ProcessPointer(scroll, new Vector2(150, 150), 7, true, false, .5f);
            scroll = list.ProcessPointer(scroll, new Vector2(150, 156), 7, false, true, .5f);
            Require(scroll == new Vector2(40, 100) && !list.SuppressClicks, "A tap must remain clickable and retain its scroll position.");

            list.ProcessPointer(scroll, new Vector2(150, 150), 7, true, false, .5f);
            scroll = list.ProcessPointer(scroll, new Vector2(130, 50), 7, false, false, .5f);
            Require(scroll == new Vector2(60, 200) && list.SuppressClicks, "Dragging content must scroll both axes and suppress buttons.");
            Vector2 otherPointer = list.ProcessPointer(scroll, Vector2.zero, 8, false, true, .5f);
            Require(otherPointer == scroll, "An unrelated finger must not change the captured gesture.");
            scroll = list.ProcessPointer(scroll, new Vector2(-1000, -1000), 7, false, true, .5f);
            Require(scroll == new Vector2(300, 600) && list.SuppressClicks, "Release must clamp to the last row and suppress accidental purchases.");

            list.ProcessPointer(scroll, new Vector2(150, 150), 9, true, false, 1);
            scroll = list.ProcessPointer(scroll, new Vector2(1000, 1000), 9, false, true, 1);
            Require(scroll == Vector2.zero, "Dragging back must clamp to the first row.");
            list.Cancel();
            Require(!list.SuppressClicks, "Closing or switching a list must release capture.");
            scroll = list.ProcessPointer(new Vector2(0, 100), new Vector2(500, 500), 10, true, false, 1);
            scroll = list.ProcessPointer(scroll, Vector2.zero, 10, false, true, 1);
            Require(scroll == new Vector2(0, 100) && !list.SuppressClicks, "A gesture outside the list must leave its content untouched.");
            list.ProcessPointer(scroll, new Vector2(395, 150), 11, true, false, 1);
            scroll = list.ProcessPointer(scroll, new Vector2(395, 50), 11, false, true, 1);
            Require(scroll == new Vector2(0, 100), "Native scrollbar tracks must remain available.");
            Debug.Log("MOBILE_INTERFACE_CHECKS: PASS (8 gesture scenarios)");
        }

        /// <summary>Stops the check command when a required interaction fails.</summary>
        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
