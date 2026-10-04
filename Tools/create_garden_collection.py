"""Create five original garden decorations, their FBX files, and a Blender source shelf."""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Assets/Resources/Models"
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)


def material(name, color):
    """Create a matte flat-color material suitable for the existing Unity garden."""
    result = bpy.data.materials.new(name)
    result.diffuse_color = (*color, 1)
    result.use_nodes = True
    shader = result.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1)
    shader.inputs["Roughness"].default_value = .76
    return result


WOOD = material("Collection honey oak", (.56, .34, .18))
TRIM = material("Collection pale oak", (.78, .56, .31))
DARK = material("Collection shadow", (.20, .16, .10))
SAGE = material("Collection sage", (.29, .51, .31))
LEAF = material("Collection fresh leaf", (.46, .65, .29))
CREAM = material("Collection ivory", (.96, .88, .68))
ROSE = material("Collection rose", (.84, .37, .46))
LILAC = material("Collection lavender", (.64, .48, .76))
GOLD = material("Collection warm brass", (.88, .66, .28))
STONE = material("Collection limestone", (.65, .70, .60))
WATER = material("Collection turquoise water", (.31, .68, .73))
FOAM = material("Collection water glint", (.72, .91, .85))


def shape(name, position, scale, color, kind="sphere"):
    """Create a low-poly mesh part with Blender Z as the vertical axis."""
    if kind == "cube":
        bpy.ops.mesh.primitive_cube_add(size=1, location=position)
    elif kind == "cylinder":
        bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=1, depth=1, location=position)
    else:
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=1, location=position)
    result = bpy.context.object
    result.name = name
    result.scale = scale
    result.data.materials.append(color)
    return result


def rod(name, start, end, radius, color):
    """Create a cylindrical stem or structural rod between two points."""
    start, end = Vector(start), Vector(end)
    result = shape(name, (start + end) * .5, (radius, radius, (end - start).length), color, "cylinder")
    result.rotation_euler = (end - start).to_track_quat("Z", "Y").to_euler()
    return result


def ring(name, position, radius, thickness, color, rotation=None):
    """Create a rounded rim, wheel tire, or decorative brass loop."""
    bpy.ops.mesh.primitive_torus_add(major_segments=20, minor_segments=6,
        major_radius=radius, minor_radius=thickness, location=position)
    result = bpy.context.object
    result.name = name
    if rotation:
        result.rotation_euler = rotation
    result.data.materials.append(color)
    return result


def flower(x, y, height, color, radius=.12):
    """Create a flower with a slender stem, pointed leaves, petals, and a golden heart."""
    rod("Flower stem", (x, y, .40), (x, y, height), .018, SAGE)
    for side in (-1, 1):
        leaf = shape("Flower leaf", (x + side * .10, y, height - .25), (.14, .065, .025), LEAF)
        leaf.rotation_euler[1] = side * -.35
    for index in range(6):
        angle = index * math.tau / 6
        petal = shape("Rounded flower petal", (x + math.cos(angle) * radius,
            y + math.sin(angle) * radius, height), (radius, radius * .63, .05), color)
        petal.rotation_euler[2] = angle
    shape("Golden flower heart", (x, y, height + .035), (.07, .07, .045), GOLD)


def wooden_planter():
    """Build a slatted wooden flower box with mixed roses, daisies, and a leafy vine."""
    shape("Planter soil", (0, 0, .43), (1.12, .72, .13), DARK, "cube")
    for x in (-.58, .58):
        shape("Box end", (x, 0, .26), (.09, .86, .46), WOOD, "cube")
    for y in (-.41, .41):
        for index in range(6):
            shape("Honey oak slat", (-.50 + index * .20, y, .26), (.18, .08, .45), TRIM, "cube")
        shape("Box top rim", (0, y, .47), (1.27, .105, .08), WOOD, "cube")
    for x, y, height, color in [(-.34, -.12, .87, ROSE), (.05, -.20, 1.06, CREAM),
        (.35, -.06, .90, LILAC), (-.17, .19, 1.12, ROSE), (.30, .21, 1.18, CREAM)]:
        flower(x, y, height, color)
    for index in range(4):
        shape("Trailing planter leaf", (-.48 + index * .08, -.48, .54 - index * .11), (.13, .07, .035), SAGE)


def birdhouse():
    """Build a little timber birdhouse with a leaf roof, brass details, and a blue bird."""
    shape("Post base", (0, 0, .09), (.32, .28, .15), STONE, "cylinder")
    rod("Oak support post", (0, 0, .12), (0, 0, 1.35), .075, WOOD)
    shape("Birdhouse room", (0, 0, 1.50), (.66, .51, .55), TRIM, "cube")
    shape("House bottom trim", (0, 0, 1.22), (.78, .62, .10), WOOD, "cube")
    for side in (-1, 1):
        roof = shape("Overlapping leaf roof", (side * .22, 0, 1.86), (.61, .74, .09), SAGE, "cube")
        roof.rotation_euler[1] = side * .45
        for index in range(3):
            leaf = shape("Roof leaf", (side * .19, -.21 + index * .21, 1.91), (.33, .13, .045), LEAF)
            leaf.rotation_euler[1] = side * .42
    ring("Doorway rim", (0, -.275, 1.55), .12, .025, CREAM, (math.pi / 2, 0, 0))
    shape("Round doorway shadow", (0, -.285, 1.55), (.10, .022, .10), DARK)
    rod("Front perch", (0, -.27, 1.32), (0, -.61, 1.32), .035, WOOD)
    shape("Blue bird body", (0, -.49, 1.45), (.11, .15, .12), WATER)
    shape("Bird head", (0, -.59, 1.55), (.08, .08, .08), CREAM)
    shape("Bird beak", (0, -.68, 1.54), (.035, .055, .025), GOLD)
    for side in (-1, 1):
        shape("Bird wing", (side * .095, -.48, 1.45), (.025, .10, .065), SAGE)
    flower(.20, .12, .58, ROSE, .09)


def wind_chime():
    """Build a branch-mounted brass wind chime with jade leaf pendants."""
    shape("Chime stone footing", (-.43, 0, .07), (.31, .28, .13), STONE, "cylinder")
    rod("Chime timber post", (-.43, 0, .10), (-.43, 0, 1.93), .065, WOOD)
    rod("Chime overhanging branch", (-.43, 0, 1.90), (.44, 0, 2.02), .05, WOOD)
    for index in range(4):
        shape("Branch leaf", (-.23 + index * .20, .05, 1.99 + index * .025), (.15, .09, .035), LEAF)
    rod("Hanging cord", (.05, 0, 1.99), (.05, 0, 1.78), .012, DARK)
    shape("Chime crown", (.05, 0, 1.73), (.39, .24, .09), SAGE)
    for index, length in enumerate((.40, .59, .73, .59, .40)):
        x = -.25 + index * .15
        rod("Fine chime thread", (x, 0, 1.73), (x, 0, 1.59), .008, DARK)
        rod("Brass chime tube", (x, 0, 1.59), (x, 0, 1.59 - length), .035, GOLD)
        ring("Tube lower rim", (x, 0, 1.59 - length), .035, .008, CREAM)
    rod("Pendant cord", (.05, -.10, 1.60), (.05, -.10, .58), .008, DARK)
    pendant = shape("Jade leaf pendant", (.05, -.10, .48), (.10, .025, .20), SAGE)
    pendant.rotation_euler[1] = -.25
    flower(-.25, .18, .56, LILAC, .085)


def leaf_bowl(name, center, radii, color):
    """Create a pointed, curved leaf bowl that remains visible from the gameplay camera."""
    vertices = [center]
    count = 24
    for index in range(count):
        angle = index * math.tau / count
        vertices.append((center[0] + math.cos(angle) * radii[0],
            center[1] + math.sin(angle) * radii[1] * abs(math.sin(angle)) ** .35,
            center[2] + .10 + .035 * math.cos(angle)))
    faces = [(0, index + 1, (index + 1) % count + 1) for index in range(count)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    result = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(result)
    result.data.materials.append(color)
    rod("Leaf bowl vein", (center[0] - radii[0], center[1], center[2] + .065),
        (center[0] + radii[0], center[1], center[2] + .10), .013, CREAM)


def leaf_fountain():
    """Build a circular stone basin with two sculpted leaves and turquoise cascades."""
    shape("Fountain basin", (0, 0, .12), (.70, .65, .23), STONE, "cylinder")
    ring("Rounded basin rim", (0, 0, .25), .64, .075, STONE)
    shape("Lower turquoise pool", (0, 0, .245), (.58, .58, .02), WATER, "cylinder")
    rod("Carved fountain stem", (0, .13, .25), (-.15, .12, 1.48), .095, SAGE)
    leaf_bowl("Lower leaf basin", (.18, -.06, .78), (.50, .30), LEAF)
    leaf_bowl("Upper leaf basin", (-.20, .08, 1.40), (.44, .27), SAGE)
    shape("Lower leaf water", (.18, -.06, .82), (.35, .21, .02), WATER)
    shape("Upper leaf water", (-.20, .08, 1.44), (.30, .18, .02), WATER)
    rod("Upper falling water", (.21, .07, 1.45), (.22, -.07, .86), .026, WATER)
    rod("Lower falling water", (.62, -.06, .86), (.53, -.12, .27), .026, WATER)
    for position in ((.22, -.07, .87), (.53, -.12, .27)):
        ring("Water ripple", position, .09, .012, FOAM)
    for index in range(3):
        shape("Water splash", (.46 + index * .07, -.17, .30 + (index % 2) * .06), (.025, .025, .045), FOAM)


def flower_cart():
    """Build an oak flower wagon with spoke wheels and a scalloped striped canopy."""
    shape("Flower wagon tray", (0, 0, .52), (1.06, .80, .28), WOOD, "cube")
    shape("Tray flower soil", (0, 0, .67), (.98, .72, .08), DARK, "cube")
    for x in (-.55, .55):
        for y in (-.30, .30):
            ring("Wagon wheel tire", (x, y, .24), .22, .032, WOOD, (0, math.pi / 2, 0))
            shape("Wheel brass hub", (x, y, .24), (.045, .065, .065), GOLD)
            for angle in (0, math.pi / 3, 2 * math.pi / 3):
                rod("Wheel spoke", (x, y - .19 * math.cos(angle), .24 - .19 * math.sin(angle)),
                    (x, y + .19 * math.cos(angle), .24 + .19 * math.sin(angle)), .016, TRIM)
    for x in (-.48, .48):
        rod("Canopy upright", (x, .32, .56), (x, .32, 1.70), .028, WOOD)
        rod("Wagon handle", (x * .66, .38, .56), (x * .66, .81, .78), .035, WOOD)
    for index in range(8):
        x = -.56 + index * .16
        arch_height = 1.69 + .16 * (1 - (x / .65) ** 2)
        shape("Canopy fabric stripe", (x, 0, arch_height), (.16, 1.05, .055),
            CREAM if index % 2 == 0 else SAGE, "cube")
        shape("Scalloped canopy fringe", (x, -.51, arch_height - .065), (.081, .028, .085),
            CREAM if index % 2 == 0 else SAGE)
    for x, y, height, color in [(-.30, -.16, 1.08, ROSE), (.03, -.21, 1.23, CREAM),
        (.32, -.12, 1.05, LILAC), (-.21, .15, 1.25, LILAC), (.18, .15, 1.30, ROSE)]:
        flower(x, y, height, color, .11)
    shape("Wagon front leaf badge", (0, -.42, .51), (.16, .024, .08), LEAF)


def export_model(name, create, index):
    """Join parts by material to bound draw calls, export at the origin, and shelf the source."""
    before = set(bpy.data.objects)
    create()
    objects = list(set(bpy.data.objects) - before)
    groups = {}
    for obj in objects:
        groups.setdefault(obj.data.materials[0].name, []).append(obj)
    joined = []
    for color_name, parts in groups.items():
        bpy.ops.object.select_all(action="DESELECT")
        for obj in parts:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = parts[0]
        if len(parts) > 1:
            bpy.ops.object.join()
        result = bpy.context.object
        result.name = name + "_" + color_name
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        bpy.context.scene.cursor.location = (0, 0, 0)
        bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        joined.append(result)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in joined:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = joined[0]
    bpy.ops.export_scene.fbx(filepath=str(OUTPUT / (name + ".fbx")), use_selection=True,
        object_types={"MESH"}, axis_forward="-Z", axis_up="Y", bake_anim=False, add_leaf_bones=False)
    group = bpy.data.objects.new("AssetRoot_" + name, None)
    bpy.context.collection.objects.link(group)
    for obj in joined:
        obj.parent = group
    group.location.x = (index - 2) * 2.2
    print(name + ": " + str(len(joined)) + " material meshes")


def render_preview():
    """Render a labeled-by-filename Blender art preview separate from gameplay screenshots."""
    shape("Preview ground", (0, 0, -.08), (100, 100, .10), material("Preview cream ground", (.72, .77, .64)), "cube")
    bpy.ops.object.camera_add(location=(2, -9, 8))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0, 0, .8)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 11.8
    scene = bpy.context.scene
    scene.camera = camera
    bpy.ops.object.light_add(type="AREA", location=(-3, -4, 9))
    bpy.context.object.data.energy = 1800
    bpy.context.object.data.size = 8
    scene.world.color = (.6, .6, .6)
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 16
    scene.render.resolution_x = 1500
    scene.render.resolution_y = 650
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(ROOT / "Docs/garden-collection-art-preview.png")
    bpy.ops.render.render(write_still=True)


for index, (name, create) in enumerate([
    ("WoodenPlanter", wooden_planter), ("Birdhouse", birdhouse),
    ("WindChime", wind_chime), ("LeafFountain", leaf_fountain), ("FlowerCart", flower_cart)
]):
    export_model(name, create, index)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / "ArtSource/GardenCollection.blend"))
if "--render-preview" in sys.argv:
    render_preview()
print("GARDEN_COLLECTION_OK: five original decorations exported")
