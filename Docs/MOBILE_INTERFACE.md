# Mobile interface

Shop lists accept single-finger content drags, including gestures that begin on
a Select button. Moving at least eight rendering pixels turns a tap into a drag;
releasing that drag cannot select or purchase a building. Scrolling clamps at
the first and last rows, retains each category's position, and releases capture
when the shop closes or changes category. Mouse dragging, wheel scrolling and
the native scrollbar remain available on desktop. Two-finger input does not
scroll or activate list buttons.

The building inspector uses a separate responsive scale on small screens. Its
close button has a minimum 48 CSS-pixel target, measured using the WebGL canvas
pixel ratio, with additional header space to avoid covering the preview. Its
input bounds follow the same scale so touches do not pass through to the map.

Run `Little Colony > Check Mobile Interface` in Unity, or execute
`LittleColony.Editor.MobileInterfaceChecks.Run` in batch mode, for eight gesture
regression scenarios. These cover tap preservation, two-axis dragging, pointer
ownership, end bounds, release suppression, cancellation, outside starts and
native scrollbar tracks. Browser drag and responsive viewport checks complement
these checks; a physical mobile-device check is still useful for device-specific
touch and browser behavior.

This update changes presentation and input only; village economy and save data
retain their existing rules and format.
