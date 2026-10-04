"""Create original three-tier hive, flower, and ant-worksite models in Blender."""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / 'Assets/Resources/Models'
OUTPUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def material(name, color):
    """Create a flat-color material that retains its color in Unity's FBX importer."""
    result = bpy.data.materials.new(name)
    result.diffuse_color = (*color, 1)
    result.use_nodes = True
    shader = result.node_tree.nodes['Principled BSDF']
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Roughness'].default_value = .78
    return result


WOOD = material('Harvest warm wood', (.46, .27, .12))
TRIM = material('Harvest cream wood', (.83, .64, .33))
HONEY = material('Honeycomb golden wax', (.95, .61, .14))
DARK = material('Honeycomb doorway shadow', (.16, .10, .055))
GREEN = material('Harvest emerald leaf', (.23, .46, .18))
LIME = material('Harvest fresh leaf', (.48, .68, .25))
SOIL = material('Flowerbed soil', (.32, .23, .14))
PINK = material('Tulip rose petals', (.94, .32, .46))
CORAL = material('Tulip coral petals', (1, .53, .39))
BLUE = material('Bluebell sky petals', (.35, .48, .91))
VIOLET = material('Bluebell violet shade', (.34, .27, .63))
STONE = material('Pebble slate blue', (.46, .55, .58))
LIGHT_STONE = material('Pebble warm limestone', (.77, .75, .61))


def shape(name, position, scale, surface, primitive='sphere'):
    """Add a colored low-poly primitive to the active model."""
    if primitive == 'cube':
        bpy.ops.mesh.primitive_cube_add(size=1, location=position)
    elif primitive == 'cylinder':
        bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=1, depth=1, location=position)
    elif primitive == 'rock':
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=position)
    else:
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=1, location=position)
    result = bpy.context.object
    result.name = name
    result.scale = scale
    result.data.materials.append(surface)
    return result


def rod(name, start, end, radius, surface):
    """Connect two positions with a cylindrical stem or support."""
    start, end = Vector(start), Vector(end)
    result = shape(name, (start + end) * .5, (radius, radius, (end - start).length), surface, 'cylinder')
    result.rotation_euler = (end - start).to_track_quat('Z', 'Y').to_euler()
    return result


def mesh(name, vertices, faces, surface):
    """Create a custom mesh with consistent outward-facing normals."""
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    result = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(result)
    result.data.materials.append(surface)
    return result


def honeycomb_cell(x, z, entrance=False):
    """Build a hollow hexagonal wax chamber facing the front of the hive."""
    vertices = []
    for y, radius in [(-.36, .30), (-.36, .225), (.17, .30), (.17, .225)]:
        for index in range(6):
            angle = math.tau * index / 6 + math.pi / 6
            vertices.append((x + radius * math.cos(angle), y, z + radius * math.sin(angle)))
    faces = []
    for index in range(6):
        next_index = (index + 1) % 6
        faces.extend([(index, next_index, next_index + 6, index + 6),
                      (index, index + 12, next_index + 12, next_index),
                      (index + 6, next_index + 6, next_index + 18, index + 18),
                      (index + 12, index + 18, next_index + 18, next_index + 12)])
    mesh('Hexagonal wax room', vertices, faces, HONEY)
    mesh('Recessed chamber', vertices[18:24], [tuple(range(6))], DARK if entrance else TRIM)


def honeycomb_home(tier):
    """Add wax rooms, raised timber decking, and a layered leaf roof at each tier."""
    shape('Timber landing deck', (0, -.02, .18), (1.75, 1.15, .15), WOOD, 'cube')
    for x in (-.61, .61):
        rod('Raised hive foot', (x, .12, 0), (x, .12, .38), .09, WOOD)
    cells = [(0, 1.0), (-.26, .55), (.26, .55)]
    if tier >= 2:
        cells += [(-.52, 1.0), (.52, 1.0)]
    if tier == 3:
        cells += [(-.26, 1.45), (.26, 1.45)]
    for index, (x, z) in enumerate(cells):
        honeycomb_cell(x, z, index == 1)
    roof_height = 1.39 if tier < 3 else 1.87
    for side in (-1, 1):
        leaf = shape('Overlapping roof leaf', (side * .38, .02, roof_height), (.58, .68, .09), GREEN)
        leaf.rotation_euler[1] = side * .15
        rod('Roof leaf vein', (side * .08, -.61, roof_height + .07),
            (side * .83, .40, roof_height + .02), .018, LIME)
    shape('Entrance landing leaf', (-.26, -.57, .28), (.35, .32, .055), LIME)
    for index in range(tier):
        shape('Honey jar', (-.58 + index * .58, .34, .37), (.10, .10, .17), HONEY)
        shape('Honey jar lid', (-.58 + index * .58, .34, .54), (.115, .115, .035), TRIM)


def flowerbed():
    """Lay out a compact garden bed that fits the existing building footprint."""
    shape('Oval garden bed', (0, 0, .11), (.83, .68, .13), SOIL)
    for index in range(9):
        angle = math.tau * index / 9
        shape('Bed edging pebble', (.75 * math.cos(angle), .60 * math.sin(angle), .13),
              (.14, .12, .09), LIGHT_STONE, 'rock')


def stem_leaf(x, y, height, side):
    """Place a tapered leaf beside a flowering stem."""
    leaf = shape('Upright stem leaf', (x + side * .12, y, height), (.075, .055, .30), LIME)
    leaf.rotation_euler[1] = side * .55


def tulips(tier):
    """Grow three, five, or seven upright pink tulip cups."""
    flowerbed()
    for index in range(tier * 2 + 1):
        angle = index * 2.39996
        radius = .18 + .10 * (index % 4)
        x, y = radius * math.cos(angle), radius * math.sin(angle)
        height = .79 + (index % 3) * .16
        rod('Tulip stem', (x, y, .14), (x, y, height), .023, GREEN)
        stem_leaf(x, y, .43, -1 if index % 2 else 1)
        for petal in range(6):
            turn = petal * math.tau / 6
            part = shape('Upright tulip petal', (x + .12 * math.cos(turn), y + .12 * math.sin(turn), height + .12),
                         (.115, .075, .23), PINK if index % 2 else CORAL)
            part.rotation_euler[2] = turn
        shape('Tulip pollen', (x, y, height + .17), (.065, .065, .055), HONEY)


def bell(x, y, z):
    """Create an open blue bell with a flared, scalloped lower rim."""
    vertices = []
    for ring, (height, radius) in enumerate([(0, .035), (-.08, .085), (-.19, .13), (-.23, .16)]):
        for index in range(12):
            angle = index * math.tau / 12
            scallop = .035 * (index % 2) if ring == 3 else 0
            vertices.append((x + radius * math.cos(angle), y + radius * math.sin(angle), z + height + scallop))
    faces = []
    for ring in range(3):
        for index in range(12):
            next_index = (index + 1) % 12
            face = (ring * 12 + index, (ring + 1) * 12 + index,
                    (ring + 1) * 12 + next_index, ring * 12 + next_index)
            faces.extend([face, tuple(reversed(face))])
    mesh('Scalloped blue bell', vertices, faces, BLUE)
    shape('Bell inner shadow', (x, y, z - .17), (.08, .08, .022), VIOLET)
    rod('Bell stamen', (x, y, z - .12), (x, y, z - .25), .014, TRIM)


def bluebells(tier):
    """Grow branching stalks with hanging blue bells and long green leaves."""
    flowerbed()
    for index in range(tier + 2):
        angle = index * 2.39996
        x, y = .38 * math.cos(angle), .32 * math.sin(angle)
        height = .96 + .12 * (index % 3)
        rod('Bluebell stalk', (x, y, .13), (x, y, height + .17), .024, GREEN)
        stem_leaf(x, y, .40, -1 if index % 2 else 1)
        for flower in range(3):
            side = -1 if flower % 2 else 1
            top = height - flower * .23
            end = (x + side * .18, y - .025, top + .02)
            rod('Curved flower hook', (x, y, top + .13), end, .016, GREEN)
            bell(*end)


def leaf_depot(tier):
    """Build a leaf sorting rack with bound leaf bundles and extra storage shelves."""
    shape('Depot wooden base', (0, 0, .12), (1.70, 1.25, .18), WOOD, 'cube')
    for x in (-.66, .66):
        rod('Rack post', (x, .34, .15), (x, .34, .85 + tier * .12), .055, WOOD)
    for shelf in range(tier):
        height = .29 + shelf * .27
        shape('Sorting shelf', (0, .15, height), (1.45, .63, .065), TRIM, 'cube')
        for bundle in range(3):
            x = (bundle - 1) * .43
            for layer in range(3):
                leaf = shape('Stacked leaf', (x, .10, height + .065 + layer * .045), (.22, .29, .04), GREEN if layer % 2 else LIME)
                leaf.rotation_euler[2] = (bundle - 1) * .20
            rod('Leaf bundle tie', (x, -.19, height + .09), (x, .39, height + .19), .019, TRIM)
    roof = .89 + tier * .12
    shape('Leaf depot awning', (0, .18, roof), (.88, .47, .075), GREEN)
    rod('Awning vein', (-.78, .18, roof + .06), (.78, .18, roof + .06), .024, LIME)
    shape('Leaf sorting mat', (0, -.43, .24), (.53, .26, .035), LIME)


def pebble_yard(tier):
    """Build separate stone bins, a sorting sieve, and a timber handcart."""
    shape('Stone yard earth', (0, 0, .10), (.88, .74, .13), SOIL)
    for index in range(tier + 1):
        x = -.51 + (index % 2) * .58
        y = .30 - (index // 2) * .60
        shape('Sorting bin', (x, y, .21), (.52, .48, .18), WOOD, 'cube')
        for stone in range(5):
            angle = stone * 2.39996
            shape('Sorted pebble', (x + .13 * math.cos(angle), y + .12 * math.sin(angle), .32 + (stone % 2) * .08),
                  (.14, .115, .12), STONE if stone % 2 else LIGHT_STONE, 'rock')
    for x in (.51, .83):
        rod('Sieve stand', (x, .28, .12), (x, .28, .88), .045, WOOD)
    for level in range(4):
        rod('Sieve horizontal mesh', (.47, .24, .48 + level * .105), (.86, .24, .48 + level * .105), .014, TRIM)
    for index in range(4):
        rod('Sieve vertical mesh', (.48 + index * .12, .24, .45), (.48 + index * .12, .24, .83), .014, TRIM)
    shape('Handcart tray', (.61, -.39, .24), (.48, .42, .12), TRIM, 'cube')
    for x in (.37, .85):
        wheel = shape('Cart wheel', (x, -.39, .16), (.13, .13, .065), WOOD, 'cylinder')
        wheel.rotation_euler[1] = math.pi / 2
    for x in (.46, .76):
        rod('Cart handle', (x, -.40, .25), (x, -.77, .33), .028, WOOD)


def export_model(name, create, tier, column):
    """Export an origin-centered tier, then place it on the Blender source shelf."""
    before = set(bpy.data.objects)
    create(tier)
    objects = list(set(bpy.data.objects) - before)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.ops.export_scene.fbx(filepath=str(OUTPUT / (name + '.fbx')), use_selection=True,
                             object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                             bake_anim=False, add_leaf_bones=False)
    group = bpy.data.objects.new('AssetRoot_' + name, None)
    bpy.context.collection.objects.link(group)
    for obj in objects:
        obj.parent = group
    group.location = ((column - 2) * 3.0, (2 - tier) * 3.2, 0)


def render_preview():
    """Render the Blender asset shelf as art reference, not as gameplay evidence."""
    floor = material('Preview cream ground', (.76, .79, .66))
    shape('Preview ground', (0, 0, -.10), (200, 200, .1), floor, 'cube')
    bpy.ops.object.camera_add(location=(0, -17, 18))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0, 0, .5)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = 16
    scene = bpy.context.scene
    scene.camera = camera
    bpy.ops.object.light_add(type='AREA', location=(-3, -5, 12))
    bpy.context.object.data.energy = 2100
    bpy.context.object.data.size = 10
    scene.world.color = (.55, .55, .55)
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 16
    scene.render.resolution_x = 1800
    scene.render.resolution_y = 1100
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(ROOT / 'Docs/honeycomb-worksites-art-preview.png')
    bpy.ops.render.render(write_still=True)


for column, (kind, create) in enumerate([
    ('BeeHoneycombHome', honeycomb_home), ('Tulip', tulips), ('Bluebell', bluebells),
    ('LeafDepot', leaf_depot), ('PebbleYard', pebble_yard)
]):
    for tier in (1, 2, 3):
        export_model(kind + (str(tier) if tier > 1 else ''), create, tier, column)

bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'ArtSource/HoneycombWorksites.blend'))
if '--render-preview' in sys.argv:
    render_preview()
print('HONEYCOMB_WORKSITES_OK: 15 models exported')
