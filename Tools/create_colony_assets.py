"""Original, colored v0.6 homes and worksite tiers for Little Colony."""
from pathlib import Path
import math
import bpy
from mathutils import Vector

root=Path(__file__).resolve().parents[1]
out=root/'Assets'/'Resources'/'Models'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)

def mat(name,rgb):
    m=bpy.data.materials.new(name);m.diffuse_color=(*rgb,1);m.use_nodes=True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*rgb,1)
    m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.8
    return m
soil=mat('Cocoa soil',(.24,.14,.08));clay=mat('Warm clay',(.58,.30,.17))
bark=mat('Chestnut bark',(.38,.21,.10));wood=mat('Pale wood',(.72,.49,.27))
green=mat('Leaf green',(.24,.45,.18));light=mat('Fresh leaf',(.45,.66,.27))
cream=mat('Cream',(.93,.85,.63));gold=mat('Honey gold',(.95,.60,.13))
amber=mat('Dark honey',(.62,.33,.09));purple=mat('Lavender',(.55,.37,.72))
pink=mat('Petal rose',(.82,.39,.42));yellow=mat('Sunflower',(.96,.72,.15))
black=mat('Entrance shadow',(.10,.075,.055));stone=mat('River stone',(.49,.52,.47))

def shape(name,p,s,m,kind='sphere'):
    if kind=='cube':bpy.ops.mesh.primitive_cube_add(size=1,location=p)
    elif kind=='cyl':bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=1,depth=1,location=p)
    elif kind=='cone':bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=1,radius2=.4,depth=1,location=p)
    else:bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=1,location=p)
    o=bpy.context.object;o.name=name;o.scale=s;o.data.materials.append(m);return o
def rod(name,a,b,r,m):
    a,b=Vector(a),Vector(b);o=shape(name,(a+b)*.5,(r,r,(b-a).length),m,'cyl')
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()

def door(z=.27):
    shape('Dark tunnel entrance',(0,-.59,z),(.23,.08,.28),black)
    shape('Entrance lintel',(0,-.66,z+.24),(.38,.11,.08),wood,'cube')
    shape('Small doorstep',(0,-.76,.11),(.48,.27,.08),stone,'cube')

def burrow(tier):
    shape('Grassy earth mound',(0,0,.40),(.81,.77,.53),soil)
    shape('Moss roof',(0,.06,.68),(.85,.73,.19),green)
    door()
    for i in range(tier+1):
        x=(i-(tier*.5))*.36
        shape('Room window',(x,.40,.55),(.14,.055,.14),gold)
        shape('Window frame',(x,.45,.55),(.18,.04,.19),wood)
    if tier>1:
        for side in (-1,1):
            shape('Side earth room',(side*.66,.11,.36),(.37,.48,.38),soil)
            shape('Side leaf roof',(side*.66,.11,.62),(.41,.47,.12),light)
    if tier==3:
        rod('Tall lookout stem',(.1,.19,.79),(.1,.19,1.36),.06,bark)
        shape('Lookout leaf',(.19,.19,1.36),(.40,.22,.08),light)
    for i in range(5):
        a=i*math.tau/5
        shape('Tiny grass sprout',(.7*math.cos(a),.67*math.sin(a),.49),(.09,.10,.22),light)

def lantern_hive(tier):
    for side in (-1,1):rod('Hanging garden post',(side*.56,0,.08),(side*.56,0,1.45+.13*tier),.055,bark)
    rod('Hanging garden beam',(-.60,0,1.45+.13*tier),(.60,0,1.45+.13*tier),.07,bark)
    shape('Honey lantern body',(0,0,.73),(.49,.48,.50+.1*tier),gold)
    shape('Honey roof',(0,0,1.16+.10*tier),(.69,.64,.13),green)
    shape('Hive entrance',(0,-.49,.64),(.16,.055,.16),black)
    shape('Landing perch',(0,-.56,.48),(.30,.24,.05),wood,'cube')
    for i in range(tier):
        h=.48+i*.28
        shape('Amber honey band',(0,0,h),(.51,.50,.045),amber,'cyl')
    if tier>1:
        for side in (-1,1):
            shape('Companion honey cell',(side*.40,.06,.48),(.21,.23,.26),gold)
            shape('Cell dark entrance',(side*.40,-.18,.48),(.07,.025,.07),black)
    if tier==3:
        for i in range(5):
            a=i*math.tau/5
            shape('Roof blossom petal',(.36+math.cos(a)*.15,.12+math.sin(a)*.15,1.40),(.12,.10,.05),pink)
        shape('Roof blossom center',(.36,.12,1.44),(.08,.08,.05),yellow)

def flower_head(x,y,z,petal,large=False):
    r=.31 if large else .21
    rod('Flower stalk',(x,y,.12),(x,y,z),.045,green)
    for i in range(8):
        a=i*math.tau/8
        p=shape('Colored petal',(x+math.cos(a)*r,y+math.sin(a)*r,z),(.17 if large else .12,.11 if large else .08,.045),petal)
        p.rotation_euler[2]=a
    shape('Pollen disk',(x,y,z+.035),(.16 if large else .10,.16 if large else .10,.055),amber)
    shape('Wide leaf',(x-.13,y,.42),(.29,.14,.065),light)

def work(kind,tier):
    shape('Worksite soil bed',(0,0,.10),(.87,.70,.15),soil)
    if kind in ('Pile','TwigYard','SeedMill'):
        for i in range(tier+2):
            x=(i-(tier+1)*.5)*.31
            if kind=='TwigYard':
                o=shape('Stacked twig',(x,.02,.33+i%2*.09),(.065,.58,.07),bark,'cyl');o.rotation_euler[1]=math.pi/2
                shape('Twig leaf',(x,.21,.43),(.19,.10,.04),light)
            elif kind=='SeedMill':
                shape('Golden seed bin',(x,.02,.36),(.16,.18,.15),gold)
                shape('Seed cap',(x,.02,.48),(.10,.10,.04),cream)
            else:
                shape('Grain heap',(x,.04,.32),(.22,.25,.19),gold)
        if kind=='SeedMill':
            shape('Small sorting wheel',(0,.28,.47),(.28,.09,.28),wood,'cyl')
            rod('Wheel handle',(0,.28,.47),(.31,.28,.73),.055,bark)
        if kind=='TwigYard':
            for x in (-.64,.64):rod('Yard fence',(x,-.35,.17),(x,-.35,.67),.05,wood)
    else:
        petals={'Flower':pink,'Lavender':purple,'Sunflower':yellow}[kind]
        for i in range(tier+1):
            x=(i-tier*.5)*.47
            z=(1.04 if kind=='Sunflower' else .82)+(i%2)*.14
            flower_head(x,(i%2)*.16,z,petals,kind=='Sunflower')
        if kind=='Lavender':
            for i in range(5):
                x=-.66+i*.33
                rod('Lavender spike',(x,-.22,.17),(x,-.22,.68),.025,green)
                for j in range(3):shape('Tiny violet bloom',(x,-.22,.48+j*.10),(.085,.085,.065),purple)

def export(name,fn):
    before=set(bpy.data.objects);fn();objects=list(set(bpy.data.objects)-before)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
    shelf=bpy.data.objects.new('AssetRoot_'+name,None);bpy.context.collection.objects.link(shelf)
    for o in objects:o.parent=shelf
    shelf.location.x=3.0*len([o for o in bpy.data.objects if o.name.startswith('AssetRoot_')])

for name,fn in [('AntBurrow',burrow),('BeeLantern',lantern_hive)]:
    for tier in (1,2,3):export(name+(str(tier) if tier>1 else ''),lambda tier=tier,fn=fn:fn(tier))
for name in ('Pile','TwigYard','SeedMill','Flower','Lavender','Sunflower'):
    for tier in ((2,3) if name in ('Pile','Flower') else (1,2,3)):
        export(name+(str(tier) if tier>1 else ''),lambda name=name,tier=tier:work(name,tier))
bpy.ops.wm.save_as_mainfile(filepath=str(root/'ArtSource'/'ColonyExpansion.blend'))
print('COLONY_EXPANSION_OK')
