# Living garden presentation

The lawn now has organic color patches and slow dappled shading, with darker
grass roots and brighter tips. More grass grows in the central garden while the
creek and stone crossing stay clear. Small cream, pink and lavender meadow
blossoms use one combined mesh and have no gameplay colliders.

Sunlight is warmer and brighter; nighttime ambient light is easier to read.
The creek blends from deeper teal at its center to pale shallows at its banks,
with moving ripples and small glints. Six pastel butterflies are larger, while
swaying scenery preserves each plant's original authored rotation.

`GardenAtmosphere` renders 48 camera-facing pollen/firefly particles using a
single dynamic mesh. Pollen drifts during daylight; evening fireflies pulse
softly. Rain hides them, and they return when the shower ends. This effect uses
192 vertices, no textures, no particle lights, and no colliders. The combined
blossoms and ambient particles add two renderers to the world; shader animation
handles the meadow wind and creek without per-blade components.

The Unity batch Play Mode check
`LittleColony.Editor.RuntimeCreationChecks.Run` covers movement, day/night
appearance, rain shelter/recovery, noninteractive scenery and native-resource
teardown. The WebGL build and browser visual checks remain necessary for shader
compatibility. Physical phone performance is not inferred from desktop browser
viewport checks.

All new visuals are procedural project code. No external artwork or asset
licenses were added. Economy, placement rules, unlocks and save v9 are unchanged.

The farthest camera view is framed vertically by the foreground trunk midpoint
and rear fence. Vertical panning becomes available as the player zooms in;
zooming back out progressively returns to that frame. Mouse-wheel and pinch
gestures share the same limit. The Play Mode check projects the actual scenery
anchors after extreme pan/zoom input at desktop, landscape-phone and portrait
aspect ratios.


## Lantern illumination

Lanterns use a forced per-pixel point light with a 3.2-unit range and 1.8 maximum intensity. This prevents automatic vertex lighting from interpolating a local lamp across the large ground mesh. The lamp body has its own emissive material, faded with the same day/night amount; the hidden glowing sphere was removed. Light position follows the imported shade bounds, including after relocation and rotation. Owned materials are released on teardown.

Unity Play Mode checks cover night emission, bounded light range and render mode, day shutdown, and relocation. `Docs/lantern-night-check.png` is a rendered Unity runtime check at night.
