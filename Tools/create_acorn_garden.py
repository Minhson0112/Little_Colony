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


wood=material('Acorn honey wood',(.64,.36,.14))
cap=material('Acorn warm cap',(.34,.18,.09))
trim=material('Cream carved trim',(.93,.76,.44))
dark=material('Door shadow',(.12,.085,.05))
green=material('Fresh foliage',(.27,.52,.16))
lime=material('Sunlit foliage',(.49,.69,.23))
teal=material('Parasol sage',(.23,.53,.42))
cream=material('Parasol linen',(.97,.88,.64))

def acorn(tier):
    part('Acorn body',(0,0,.66),(.63,.58,.66),wood)
    part('Cap brim',(0,0,1.19),(.76,.70,.18),cap)
    part('Cap dome',(0,0,1.34),(.63,.59,.32),cap)
    for ring in range(2):
        for i in range(10):
            a=(i+ring*.5)*math.tau/10
            part('Carved cap scale',(.58*math.cos(a),.54*math.sin(a),1.28+ring*.15),(.14,.12,.095),wood)
    stick('Curved stem',(0,0,1.5),(.13,.03,1.83),.075,cap)
    part('Stem leaf',(.30,.03,1.77),(.27,.11,.05),green)
    part('Round door frame',(0,-.54,.45),(.27,.055,.34),trim)
    part('Round doorway',(0,-.59,.43),(.21,.025,.28),dark)
    part('Doorstep',(0,-.72,.12),(.33,.23,.08),trim)
    for side in (-1,1):
        part('Round window',(side*.44,-.43,.82),(.13,.065,.14),trim)
        part('Window glass',(side*.44,-.49,.82),(.08,.025,.09),teal)
    for i in range(tier-1):
        side=-1 if i==0 else 1
        part('Extra sleeping room',(side*.60,.18,.42),(.30,.32,.37),wood)
        part('Room cap',(side*.60,.18,.73),(.36,.38,.12),cap)
        part('Room window',(side*.60,-.13,.43),(.10,.035,.12),trim)
    if tier==3:
        stick('Roof pennant',(0,.12,1.58),(0,.12,2.0),.025,trim)
        part('Leaf pennant',(.15,.12,1.95),(.18,.035,.09),green)

def tree():
    stick('Trunk',(0,0,0),(.04,0,1.3),.095,wood)
    for i in range(5):
        a=i*math.tau/5
        end=(.38*math.cos(a),.38*math.sin(a),1.25+(i%2)*.16)
        stick('Branch',(0,0,.7),end,.035,wood)
        part('Leaf crown',end,(.37,.34,.40),green if i%2 else lime)
    part('Top crown',(0,0,1.61),(.42,.40,.36),green)

def grass():
    for i in range(19):
        a=i*2.39996;r=.12+.36*(i%5)/4;h=.60+.52*((i*7)%11)/10
        x,y=r*math.cos(a),r*math.sin(a)
        verts=[(x-.045,y,0),(x+.045,y,0),(x+.13*math.cos(a),y+.13*math.sin(a),h*.65),(x+.28*math.cos(a),y+.28*math.sin(a),h)]
        mesh=bpy.data.meshes.new('Bent grass blade');mesh.from_pydata(verts,[],[(0,1,2),(0,2,3),(2,1,0),(3,2,0)]);mesh.update()
        obj=bpy.data.objects.new('Tall grass blade',mesh);bpy.context.collection.objects.link(obj);obj.data.materials.append(green if i%2 else lime)
    for i in range(3):
        x=(i-1)*.20;stick('Seed stem',(x,0,0),(x+.09,.06,1.25),.015,green)
        part('Grass seed head',(x+.09,.06,1.25),(.055,.045,.16),trim)

def parasol():
    part('Parasol foot',(0,0,.055),(.24,.24,.055),wood,'cyl')
    stick('Parasol pole',(0,0,.08),(0,0,1.48),.035,wood)
    for i in range(12):
        a=i*math.tau/12;b=(i+1)*math.tau/12
        verts=[(0,0,1.56),(.48*math.cos(a),.48*math.sin(a),1.40),(.48*math.cos(b),.48*math.sin(b),1.40),(.78*math.cos(a),.78*math.sin(a),1.14),(.78*math.cos(b),.78*math.sin(b),1.14)]
        faces=[(0,1,2),(1,3,4,2),(2,1,0),(2,4,3,1)]
        mesh=bpy.data.meshes.new('Striped canopy panel');mesh.from_pydata(verts,[],faces);mesh.update()
        obj=bpy.data.objects.new('Canopy stripe',mesh);bpy.context.collection.objects.link(obj);obj.data.materials.append(teal if i%2 else cream)
        stick('Canopy rib',(0,0,1.53),(.78*math.cos(a),.78*math.sin(a),1.13),.012,trim)
    part('Top finial',(0,0,1.60),(.065,.065,.085),trim)

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


for tier in (1,2,3):
    export('AntAcornHome'+(str(tier) if tier>1 else ''),lambda tier=tier:acorn(tier))
for name,create in [('SmallTree',tree),('TallGrass',grass),('SmallParasol',parasol)]:
    export(name,create)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'ArtSource'/'AcornGarden.blend'))
print('ACORN_GARDEN_OK')
