"""Original low-poly art for Little Colony. Run with Blender --background --python.
Uses only Blender primitives; no downloaded models, textures or add-ons.
"""
import bpy
import math
import random
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets' / 'Resources' / 'Models'
SOURCE = ROOT / 'ArtSource'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(exist_ok=True)
random.seed(17)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def mat(name, color):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = .78
    return m

cream = mat('Warm cream', (.96, .83, .58))
red = mat('Terracotta', (.78, .23, .13))
wood = mat('Cinnamon wood', (.34, .16, .08))
dark = mat('Espresso', (.09, .055, .035))
gold = mat('Honey gold', (.98, .62, .10))
green = mat('Leaf green', (.22, .43, .16))
mint = mat('Sage', (.46, .63, .28))
white = mat('Petal ivory', (1, .96, .79))
wing = mat('Wing blue', (.68, .88, .91))
brown = mat('Ant amber', (.46, .19, .09))
pink = mat('Rose', (.91, .46, .44))
rock = mat('Pebble', (.52, .57, .49))

def shape(name, pos, scale, material, kind='sphere', vertices=12):
    if kind == 'cube':
        bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    elif kind == 'cone':
        bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=1, radius2=0, depth=1, location=pos)
    elif kind == 'cylinder':
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=1, depth=1, location=pos)
    else:
        bpy.ops.mesh.primitive_uv_sphere_add(segments=vertices, ring_count=8, radius=1, location=pos)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    return obj

def rod(name, a, b, radius, material):
    a,b = Vector(a),Vector(b)
    obj = shape(name, (a+b)/2, (radius,radius,(b-a).length), material, 'cylinder', 8)
    obj.rotation_euler = (b-a).to_track_quat('Z','Y').to_euler()
    return obj

assets = {}
def export(name, builder):
    before = set(bpy.data.objects)
    builder()
    objects = list(set(bpy.data.objects)-before)
    assets[name] = objects
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects: o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.ops.export_scene.fbx(filepath=str(OUT / (name+'.fbx')), use_selection=True,
        object_types={'MESH'}, apply_unit_scale=True, axis_forward='-Z', axis_up='Y',
        bake_anim=False, add_leaf_bones=False, mesh_smooth_type='FACE')
    for o in objects: o.hide_render=True; o.hide_set(True)

def hut():
    shape('Stump home', (0,0,.53), (.65,.62,1.06), cream, 'cylinder')
    shape('Mushroom roof', (0,0,1.18), (.88,.84,.38), red)
    for a in range(7):
        angle = a * math.tau/7
        shape('Roof spots', (.55*math.cos(angle),.53*math.sin(angle),1.45), (.105,.105,.036), white)
    shape('Door', (0,-.62,.34), (.22,.06,.34), wood)
    shape('Knob', (.11,-.685,.35), (.036,.025,.036), gold)
    shape('Window rim', (.40,-.50,.74), (.16,.07,.16), wood)
    shape('Window glow', (.40,-.553,.74), (.115,.028,.115), gold)
    shape('Step', (0,-.78,.06), (.6,.34,.12), rock, 'cube')
    rod('Chimney', (.38,.18,1.2), (.38,.18,1.75), .12, wood)

def hive():
    for i in range(5):
        s = .68 - abs(i-1.6)*.13
        shape('Hive coil', (0,0,.35+i*.22), (s,s,.21), gold)
    shape('Hive entrance', (0,-.57,.49), (.18,.045,.19), dark)
    for x in [-.38,.38]: rod('Stilt', (x,0,0), (x,0,.38), .085, wood)
    shape('Leaf awning', (0,0,1.44), (.72,.35,.09), green).rotation_euler[1]=.2

def ant():
    for y,z,s in [(.33,.34,.25),(0,.33,.18),(-.30,.40,.24)]:
        shape('Ant body', (0,y,z), (s,s*1.2,s), brown)
    for x in [-.11,.11]:
        shape('Eye', (x,-.49,.47), (.08,.046,.09), white)
        shape('Pupil', (x,-.525,.48), (.037,.025,.046), dark)
        rod('Antenna', (x,-.36,.56), (x*1.7,-.47,.79), .02, dark)
    for side in [-1,1]:
        for i in range(3):
            y = -.19+i*.2
            rod('Upper leg', (side*.12,y,.32), (side*.37,y+.08,.22), .027, brown)
            rod('Lower leg', (side*.37,y+.08,.22), (side*.45,y-.04,.04), .025, dark)

def bee():
    shape('Bee abdomen', (0,.15,.34), (.26,.38,.27), gold)
    for y in [0,.23]: shape('Bee stripe', (0,y,.34), (.264,.066,.268), dark)
    shape('Bee head', (0,-.24,.38), (.235,.22,.225), gold)
    for x in [-.105,.105]:
        shape('Eye', (x,-.425,.43), (.065,.035,.075), dark)
        rod('Antenna', (x,-.29,.55), (x*1.5,-.31,.74), .02, dark)
    for x in [-.30,.30]:
        o = shape('Wing', (x,.07,.61), (.33,.19,.045), wing)
        o.rotation_euler[1]= -.3 if x<0 else .3

def flower():
    shape('Flower bed', (0,0,.07), (.65,.65,.10), wood)
    rod('Stem', (0,0,.12), (0,0,1.05), .055, green)
    for side in [-1,1]:
        o=shape('Leaf', (side*.22,0,.54), (.33,.14,.055), green)
        o.rotation_euler[1]=side*-.35
    for i in range(8):
        a=i*math.tau/8
        o=shape('Petal', (.30*math.cos(a),.30*math.sin(a),1.1), (.28,.14,.09), white)
        o.rotation_euler[2]=a
    shape('Flower heart', (0,0,1.17), (.22,.22,.12), gold)

def pile():
    shape('Work bed', (0,0,.05), (.66,.58,.07), wood)
    for i in range(12):
        x=random.uniform(-.4,.4); y=random.uniform(-.32,.32)
        shape('Acorn', (x,y,.15+max(0,.20-abs(x)*.3)), (.15,.19,.17), cream if i%3 else gold)
    for x in [-.52,.52]: rod('Edge twig',(x,-.49,.13),(x,.49,.13),.06,wood)

def clover():
    for i in range(3):
        a=i*math.tau/3
        shape('Clover leaf', (.18*math.cos(a),.18*math.sin(a),.35), (.24,.24,.07), mint)
    rod('Stem', (0,0,.02), (0,0,.34), .035, green)

def bridge():
    for i in range(13):
        x=-1.5+i*.25
        shape('Bridge plank', (x,0,.12+.12*math.cos(x)), (.24,1.45,.14), wood, 'cube')
    for y in [-.68,.68]:
        for x in [-1.4,0,1.4]: rod('Post', (x,y,.08), (x,y,.72), .065, wood)
        rod('Handrail', (-1.45,y,.68), (1.45,y,.68), .046, cream)

def ladybug():
    shape('Ladybug shell', (0,0,.28), (.32,.39,.22), red)
    shape('Head', (0,-.34,.22), (.19,.16,.17), dark)
    rod('Shell seam', (0,-.2,.49), (0,.28,.44), .019, dark)
    for x in [-.16,.16]:
        for y in [-.10,.13]: shape('Spot', (x,y,.456), (.058,.065,.025), dark)
        shape('Eye', (x*.6,-.47,.27), (.045,.022,.05), white)

for name, builder in [('AntHome',hut),('BeeHome',hive),('Ant',ant),('Bee',bee),('Flower',flower),('Pile',pile),('Clover',clover),('Bridge',bridge),('Ladybug',ladybug)]: export(name,builder)

# A separate, reproducible art preview. This is not a Unity gameplay screenshot.
def instance(name, pos, scale=1):
    for original in assets[name]:
        o=original.copy(); o.data=original.data
        bpy.context.collection.objects.link(o)
        o.hide_render=False; o.hide_set(False)
        o.location=Vector(pos)+original.location*scale
        o.scale=original.scale*scale

grass=mat('Meadow',(.40,.57,.27)); soil=mat('Earth',(.25,.17,.105)); water=mat('Stream',(.24,.58,.61))
shape('Island base',(0,0,-.62),(9.5,6,.65),soil)
shape('Moss carpet',(0,0,-.15),(9.4,5.95,.30),grass)
shape('Creek',(0,0,-.01),(1,5.7,.07),water)
instance('Bridge',(0,0,0))
instance('AntHome',(-4,-1,0)); instance('AntHome',(-6,2,0)); instance('BeeHome',(-3,3,0))
instance('Pile',(4,-1,0)); instance('Flower',(3,2,0)); instance('Flower',(5,3,0))
for name,pos in [('Ant',(-2,-.6,.03)),('Ant',(2,-1,.03)),('Bee',(3,1.4,1.1)),('Ladybug',(4,-3,.03))]:instance(name,pos)
for i in range(45):
    a=random.random()*math.tau
    instance('Clover',(8.6*math.cos(a),5.0*math.sin(a),0),random.uniform(.6,1.4))
for i in range(14):
    x=random.choice([-1,1])*random.uniform(7,8.5); y=random.uniform(-4,4)
    shape('Stone',(x,y,.05),(.3,.23,.2),rock)
shape('Backdrop',(0,0,-1.3),(200,200,.1),mat('Backdrop cream',(.79,.81,.66)),'cube')
bpy.ops.object.camera_add(location=(14,-21,22))
camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'; camera.data.ortho_scale=23
bpy.context.scene.camera=camera
bpy.ops.object.light_add(type='AREA', location=(-6,-8,14))
bpy.context.object.data.energy=2300; bpy.context.object.data.shape='DISK'; bpy.context.object.data.size=9
scene=bpy.context.scene
scene.world.color=(.5,.5,.5)
scene.render.engine='CYCLES'; scene.cycles.samples=32
scene.render.resolution_x=1440; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(SOURCE/'art-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'LittleColony.blend'))
bpy.ops.render.render(write_still=True)
print('LITTLE_COLONY_ASSETS_OK')
