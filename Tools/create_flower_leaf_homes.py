"""Create colored leaf-tent and flower-hive tier models for Little Colony."""
from pathlib import Path
import math
import bpy
from mathutils import Vector

root = Path(__file__).resolve().parents[1]
out = root / 'Assets' / 'Resources' / 'Models'
out.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def material(name, color):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (*color, 1)
    m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .78
    return m

leaf = material('Layered emerald leaf', (.20,.43,.19))
light = material('Fresh lime leaf', (.43,.68,.27))
vein = material('Leaf vein', (.74,.77,.38))
wood = material('Tent twigs', (.46,.27,.14))
earth = material('Garden soil', (.33,.22,.13))
shadow = material('Dark doorway', (.09,.075,.07))
pink = material('Rose petals', (.91,.42,.49))
rose = material('Petal shade', (.68,.26,.40))
yellow = material('Golden flower center', (.95,.71,.20))
honey = material('Warm honey', (.82,.44,.12))
cream = material('Flower highlight', (.97,.85,.61))

def part(name, pos, scale, mat, primitive='sphere'):
    if primitive=='cube': bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    elif primitive=='cyl': bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=1, depth=1, location=pos)
    else: bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, radius=1, location=pos)
    obj=bpy.context.object
    obj.name=name
    obj.scale=scale
    obj.data.materials.append(mat)
    return obj

def stick(name, a, b, radius, mat):
    a,b=Vector(a),Vector(b)
    obj=part(name,(a+b)*.5,(radius,radius,(b-a).length),mat,'cyl')
    obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return obj

def leaf_tent(tier):
    part('Soil foundation',(0,0,.13),(.85,.78,.17),earth)
    part('Triangular doorway',(0,-.70,.40),(.26,.045,.34),shadow)
    part('Entrance stone',(0,-.78,.13),(.42,.22,.07),wood,'cube')
    for side in (-1,1):
        roof=part('Sloping layered leaf roof',(side*.39,0,.82),(.99,1.52,.13),leaf,'cube')
        roof.rotation_euler[1]=side*math.radians(48)
        outer=part('Fresh overlapping roof leaf',(side*.42,-.12,.90),(.92,1.25,.055),light,'cube')
        outer.rotation_euler[1]=side*math.radians(48)
        stick('Leaf center vein',(side*.08,-.73,1.34),(side*.77,-.73,.36),.025,vein)
        stick('Tent branch',(side*.70,-.61,.2),(0,-.61,1.48),.045,wood)
    stick('Roof ridge',(0,-.76,1.48),(0,.76,1.48),.055,wood)
    for i in range(tier):
        x=(i-(tier-1)/2)*.40
        part('Tier leaf pennant',(x,.1,1.57+i*.025),(.19,.31,.075),light)
    if tier>=2:
        for side in (-1,1):
            part('Side sleeping leaf',(side*.75,.27,.32),(.37,.42,.12),light)
            stick('Side leaf vein',(side*.58,-.05,.33),(side*.96,.47,.33),.025,vein)
    if tier==3:
        part('Lookout leaf',(0,.15,1.81),(.36,.24,.10),leaf)
        stick('Lookout twig',(0,.15,1.44),(0,.15,1.83),.04,wood)

def flower_hive(tier):
    part('Leaf ground base',(0,0,.12),(.86,.82,.15),leaf)
    part('Honey bulb',(0,0,.73),(.60,.56,.61),honey)
    part('Flower heart',(0,0,1.37),(.51,.51,.20),yellow)
    for index in range(8):
        angle=2*math.pi*index/8
        x,y=.69*math.cos(angle),.69*math.sin(angle)
        petal=part('Wide flower petal',(x,y,1.22),(.29,.52,.15),pink if index%2==0 else rose)
        petal.rotation_euler[2]=angle-math.pi/2
        part('Petal highlight',(x*.92,y*.92,1.34),(.16,.28,.05),cream).rotation_euler[2]=angle-math.pi/2
    part('Bee entrance',(0,-.57,.65),(.18,.055,.21),shadow)
    part('Landing leaf',(0,-.67,.39),(.36,.24,.07),leaf)
    for i in range(tier):
        x=(i-(tier-1)/2)*.30
        part('Honey cell',(x,-.46,1.02),(.12,.065,.13),cream)
    if tier>=2:
        for side in (-1,1):
            part('Side flower bud',(side*.71,.05,.56),(.26,.25,.28),yellow)
            part('Side bud petals',(side*.76,.06,.75),(.35,.31,.13),pink)
    if tier==3:
        part('Crown blossom',(0,.09,1.65),(.34,.33,.13),pink)
        part('Crown center',(0,-.03,1.69),(.16,.15,.11),yellow)

def export(name, generator):
    before=set(bpy.data.objects)
    generator()
    objects=list(set(bpy.data.objects)-before)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects: obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
    group=bpy.data.objects.new('AssetRoot_'+name,None)
    bpy.context.collection.objects.link(group)
    for obj in objects: obj.parent=group
    group.location.x=3*len([o for o in bpy.data.objects if o.name.startswith('AssetRoot_')])

for kind, generator in [('AntLeafTent',leaf_tent),('BeeFlowerHome',flower_hive)]:
    for tier in (1,2,3):
        export(kind+(str(tier) if tier>1 else ''),lambda generator=generator,tier=tier:generator(tier))
bpy.ops.wm.save_as_mainfile(filepath=str(root/'ArtSource'/'LeafAndFlowerHomes.blend'))
print('LEAF_AND_FLOWER_HOMES_OK')
