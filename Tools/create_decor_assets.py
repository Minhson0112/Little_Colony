"""Original colored backyard decorations, exported as Unity FBX plus Blender source."""
import bpy, math
from pathlib import Path
from mathutils import Vector

root=Path(__file__).resolve().parents[1]
out=root/'Assets'/'Resources'/'Models'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)

def mat(name,rgb):
    m=bpy.data.materials.new(name);m.diffuse_color=(*rgb,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*rgb,1)
    p.inputs['Roughness'].default_value=.8
    return m
oak=mat('Honey oak',(.47,.29,.13));bark=mat('Dark oak',(.25,.14,.07))
stone=mat('Warm limestone',(.70,.68,.57));shadow=mat('Stone shade',(.45,.49,.44))
leaf=mat('Fresh foliage',(.26,.51,.22));darkleaf=mat('Deep foliage',(.15,.33,.16))
gold=mat('Lantern light',(.98,.72,.26));cream=mat('Cream',(.95,.89,.67))
rose=mat('Garden rose',(.79,.34,.40));lilac=mat('Lilac',(.63,.43,.73))
red=mat('Toadstool red',(.78,.22,.15));soil=mat('Earth',(.30,.21,.12))

def shape(name,p,s,m,kind='sphere'):
    if kind=='cube':bpy.ops.mesh.primitive_cube_add(size=1,location=p)
    elif kind=='cyl':bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=1,depth=1,location=p)
    elif kind=='cone':bpy.ops.mesh.primitive_cone_add(vertices=16,radius1=1,radius2=.05,depth=1,location=p)
    else:bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10,radius=1,location=p)
    o=bpy.context.object;o.name=name;o.scale=s;o.data.materials.append(m);return o

def rod(name,a,b,r,m):
    a,b=Vector(a),Vector(b);o=shape(name,(a+b)/2,(r,r,(b-a).length),m,'cyl')
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()

def flower(p,color):
    x,y,z=p
    for i in range(5):
        a=i*math.tau/5
        shape('Petal',(x+math.cos(a)*.12,y+math.sin(a)*.12,z),(.12,.09,.06),color)
    shape('Flower heart',p,(.065,.065,.07),gold)

def lantern():
    shape('Stone footing',(0,0,.09),(.47,.47,.18),stone,'cyl')
    rod('Timber post',(0,0,.16),(0,0,1.5),.085,bark)
    shape('Lamp body',(0,0,1.57),(.43,.34,.56),gold,'cyl')
    for x in (-.31,.31):shape('Lamp frame',(x,0,1.57),(.055,.06,.64),oak,'cube')
    for y in (-.30,.30):shape('Lamp frame',(0,y,1.57),(.055,.06,.64),oak,'cube')
    shape('Lamp cap',(0,0,1.94),(.54,.47,.16),bark,'cone')
    shape('Lamp finial',(0,0,2.05),(.11,.11,.11),gold)

def bench():
    for y in (-.35,.35):
        for x in (-.58,.58):shape('Leg',(x,y,.32),(.11,.10,.64),bark,'cube')
    for y in (-.33,0,.33):shape('Seat plank',(0,y,.68),(1.48,.26,.12),oak,'cube')
    for z in (1.02,1.30):shape('Back slat',(0,.39,z),(1.48,.12,.22),oak,'cube')
    for x in (-.58,.58):rod('Back support',(x,.36,.65),(x,.42,1.41),.065,bark)

def birdbath():
    shape('Pedestal foot',(0,0,.13),(.52,.52,.21),stone,'cyl')
    shape('Pedestal',(0,0,.61),(.18,.18,.93),stone,'cyl')
    shape('Carved bowl',(0,0,1.13),(.78,.78,.18),stone,'cyl')
    shape('Water basin',(0,0,1.24),(.63,.63,.025),mat('Blue water',(.30,.62,.68)),'cyl')
    shape('Little bird',(.25,0,1.37),(.21,.13,.16),cream)
    shape('Bird beak',(.46,0,1.36),(.10,.06,.05),gold)

def arch():
    for x in (-.71,.71):
        rod('Vine pillar',(x,0,0),(x,0,1.75),.09,bark)
        for i in range(5):
            z=.36+i*.32
            shape('Climbing leaf',(x+(1 if x<0 else -1)*.14,.08,z),(.27,.15,.11),leaf)
    for i in range(9):
        a=math.pi-i*math.pi/8
        x=.72*math.cos(a);z=1.72+.34*math.sin(a)
        if i:rod('Arch branch',prev,(x,0,z),.085,bark)
        prev=(x,0,z)
        shape('Arch leaf',(x,.07,z+.06),(.22,.15,.11),darkleaf)
        if i%2==0:flower((x,.17,z+.13),rose if i%4==0 else lilac)

def mushrooms():
    shape('Mulch bed',(0,0,.055),(.88,.64,.1),soil)
    for x,y,h,r in ((-.4,-.15,.70,.34),(.28,.17,.48,.25),(.40,-.30,.31,.17)):
        shape('Ivory stem',(x,y,h*.43),(.09,.09,h*.86),cream,'cyl')
        shape('Red cap',(x,y,h), (r,r,.20 if r>.2 else .13),red)
        for a in (0,2.1,4.2):shape('Cap spot',(x+math.cos(a)*r*.48,y+math.sin(a)*r*.48,h+.14),(.055,.055,.025),cream)
    for x,y in ((-.6,.24),(.02,-.37),(.66,.30)):
        shape('Ground leaf',(x,y,.13),(.32,.12,.055),leaf)

for name,create in [('Lantern',lantern),('Bench',bench),('Birdbath',birdbath),('FlowerArch',arch),('MushroomPatch',mushrooms)]:
    before=set(bpy.data.objects);create();objects=list(set(bpy.data.objects)-before)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
    shelf=bpy.data.objects.new('AssetRoot_'+name,None);bpy.context.collection.objects.link(shelf)
    for obj in objects:obj.parent=shelf
    shelf.location.x=3.1*len([o for o in bpy.data.objects if o.name.startswith('AssetRoot_')])

bpy.ops.wm.save_as_mainfile(filepath=str(root/'ArtSource'/'GardenDecor.blend'))
print('GARDEN_DECOR_OK')
