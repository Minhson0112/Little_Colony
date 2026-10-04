"""v0.2 original Blender assets. Keeps the original models/source untouched."""
import bpy, math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets'/'Resources'/'Models'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def material(name,color):
    m=bpy.data.materials.new(name)
    m.diffuse_color=(*color,1)
    m.use_nodes=True
    bsdf=m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value=(*color,1)
    bsdf.inputs['Roughness'].default_value=.78
    return m
cream=material('Linen',(.90,.78,.52));red=material('Rose clay',(.64,.20,.13))
wood=material('Warm oak',(.32,.19,.10));gold=material('Amber',(.93,.56,.12))
dark=material('Deep brown',(.12,.075,.045));white=material('Buttercream',(.98,.93,.76))
green=material('Garden leaf',(.28,.43,.14));teal=material('Enamel sage',(.24,.48,.43))
clay=material('Terracotta',(.64,.31,.19));soil=material('Potting soil',(.16,.10,.055))
def shape(name,p,s,m,kind='sphere'):
    if kind=='cube':bpy.ops.mesh.primitive_cube_add(size=1,location=p)
    elif kind=='cylinder':bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=1,depth=1,location=p)
    elif kind=='cone':bpy.ops.mesh.primitive_cone_add(vertices=16,radius1=1,radius2=.78,depth=1,location=p)
    else:bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10,radius=1,location=p)
    o=bpy.context.object;o.name=name;o.scale=s;o.data.materials.append(m);return o
def rod(n,a,b,r,m):
    a,b=Vector(a),Vector(b);o=shape(n,(a+b)/2,(r,r,(b-a).length),m,'cylinder');o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o
def torus(n,p,r,t,m,rot=(0,0,0)):
    bpy.ops.mesh.primitive_torus_add(major_segments=24,minor_segments=8,location=p,major_radius=r,minor_radius=t,rotation=rot)
    o=bpy.context.object;o.name=n;o.data.materials.append(m);return o
def window(x,y,z):
    shape('Window frame',(x,y,z),(.14,.055,.16),wood)
    shape('Golden glass',(x,y-.042,z),(.105,.025,.125),gold)
    rod('Mullion',(x,y-.07,z-.11),(x,y-.07,z+.11),.016,cream)
def house(tier):
    h=1.02+(tier-1)*.36
    shape('Stump home',(0,0,h/2),(.64,.62,h),cream,'cylinder')
    shape('Cap roof',(0,0,h+.14),(.88,.83,.35),red)
    for i in range(9):
        a=i*math.tau/9;shape('Ivory spot',(.57*math.cos(a),.55*math.sin(a),h+.39),(.085,.085,.025),white)
    shape('Door',(0,-.625,.34),(.21,.045,.34),wood)
    shape('Door handle',(.10,-.677,.35),(.027,.025,.027),gold)
    for side in [-1,1]:window(side*.38,-.515,.78)
    if tier>=2:
        shape('Upper leaf balcony',(0,-.63,1.02),(.58,.30,.055),green)
        for x in [-.45,0,.45]:rod('Balcony post',(x,-.79,.99),(x,-.79,1.24),.022,wood)
        rod('Balcony rail',(-.45,-.79,1.23),(.45,-.79,1.23),.023,cream)
        window(0,-.62,1.27)
    if tier==3:
        for side in [-1,1]:
            shape('Side room',(side*.58,.04,.38),(.25,.35,.72),cream,'cylinder')
            shape('Side roof',(side*.58,.04,.81),(.36,.44,.17),red)
        window(0,-.62,1.65)
        rod('Flag pole',(.38,.12,h+.25),(.38,.12,h+.83),.022,wood)
        shape('Garden pennant',(.55,.12,h+.72),(.31,.025,.17),gold,'cube')
    for i in range(3):shape('Front step',(0,-.70-i*.14,.08-i*.012),(.45,.22,.12),wood,'cube')
    rod('Chimney',(-.4,.13,h+.1),(-.4,.13,h+.62),.10,wood)
def hive(tier):
    h=5+(tier-1)*2
    for i in range(h):
        s=.64-abs(i-(h-1)*.42)*.070
        shape('Honey coil',(0,0,.3+i*.17),(s,s,.16),gold)
    for i in range(tier):
        z=.43+i*.35;shape('Bee doorway',(0,-.61,z),(.125,.04,.12),dark)
        shape('Landing porch',(0,-.68,z-.10),(.24,.17,.028),wood)
    for x in [-.38,.38]:rod('Foot',(x,0,0),(x,0,.3),.075,wood)
    z=.3+h*.17
    shape('Leaf roof',(0,0,z),(.78,.55,.08),green).rotation_euler[1]=.12
    if tier>1:
        rod('Flower stem',(.32,0,z),(.32,0,z+.35),.025,green)
        for i in range(5):
            a=i*math.tau/5;shape('Roof blossom',(.32+math.cos(a)*.13,math.sin(a)*.13,z+.35),(.12,.10,.05),white)
        shape('Blossom heart',(.32,0,z+.39),(.085,.085,.045),gold)
def pot():
    shape('Clay pot',(0,0,.5),(.55,.55,1),clay,'cone')
    torus('Rim',(0,0,.96),.55,.07,clay)
    shape('Soil',(0,0,.96),(.49,.49,.025),soil,'cylinder')
    for i in range(7):
        a=i*math.tau/7
        o=shape('Broad leaf',(.28*math.cos(a),.28*math.sin(a),1.25),(.18,.55,.075),green)
        o.rotation_euler=(.48,0,a)
def can():
    shape('Enamel body',(0,0,.6),(.62,.49,1.1),teal,'cylinder')
    torus('Top lip',(0,0,1.15),.49,.055,cream)
    shape('Dark opening',(0,0,1.17),(.40,.36,.025),dark,'cylinder')
    rod('Spout',(.42,0,.35),(1.28,0,.97),.12,teal)
    o=shape('Sprinkler rose',(1.34,0,1.02),(.27,.27,.09),cream,'cylinder');o.rotation_euler[1]=-.8
    torus('Handle',(-.68,0,.64),.43,.06,teal,(math.pi/2,0,0))
def export(name,fn):
    before=set(bpy.data.objects);fn();objects=list(set(bpy.data.objects)-before)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
    # Arrange source models in a readable asset shelf, AFTER exporting each origin-centered model.
    offset=len([o for o in bpy.data.objects if o.name.startswith('AssetRoot')])*3.1
    root=bpy.data.objects.new('AssetRoot_'+name,None);bpy.context.collection.objects.link(root)
    for o in objects:o.parent=root
    root.location.x=offset
export('AntHome2',lambda:house(2));export('AntHome3',lambda:house(3))
export('BeeHome2',lambda:hive(2));export('BeeHome3',lambda:hive(3))
export('GardenPot',pot);export('WateringCan',can)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource'/'BackyardAssets.blend'))
print('BACKYARD_ASSETS_OK')
