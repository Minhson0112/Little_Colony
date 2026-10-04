"""Create original colored food models for the placeable village meals."""
from pathlib import Path
import math
import bpy

root=Path(__file__).resolve().parents[1]
out=root/'Assets'/'Resources'/'Models'
out.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def material(name,color,roughness=.75):
    mat=bpy.data.materials.new(name)
    mat.diffuse_color=(*color,1)
    mat.use_nodes=True
    node=mat.node_tree.nodes['Principled BSDF']
    node.inputs['Base Color'].default_value=(*color,1)
    node.inputs['Roughness'].default_value=roughness
    return mat

sugar=material('Sugar ivory',(.90,.96,.90),.45)
sugar_side=material('Sugar pale blue',(.69,.85,.88),.55)
sparkle=material('Sugar crystal glint',(.99,.99,.92),.28)
cookie=material('Baked cookie',(.75,.43,.19))
edge=material('Golden cookie edge',(.91,.61,.29))
chips=material('Chocolate chips',(.27,.13,.095))
plate=material('Little leaf plate',(.35,.53,.24))
vein=material('Leaf plate vein',(.68,.76,.39))

def cube(name,position,size,mat,bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=position)
    obj=bpy.context.object;obj.name=name;obj.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=obj.modifiers.new('Soft corners','BEVEL');mod.width=bevel;mod.segments=2
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=obj.modifiers.new('Weighted face normals','WEIGHTED_NORMAL')
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.materials.append(mat)
    return obj

def sphere(name,position,size,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=1,location=position)
    obj=bpy.context.object;obj.name=name;obj.scale=size;obj.data.materials.append(mat)
    return obj

def cylinder(name,position,radius,depth,mat,vertices=20):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=position)
    obj=bpy.context.object;obj.name=name;obj.data.materials.append(mat)
    return obj

def sugar_cube():
    sphere('Leaf serving mat',(0,0,.07),(.74,.61,.07),plate)
    cube('Rounded sugar cube',(0,0,.56),(.90,.90,.90),sugar,.08)
    for side in (-1,1):
        crystal=cube('Sugar side crystal',(side*.46,.02,.59),(.025,.52,.57),sugar_side,.015)
        crystal.rotation_euler[2]=side*.06
    for x,y,z in ((-.27,-.46,.77),(.18,-.46,.51),(.36,-.46,.83),(-.14,.02,1.03),(.24,.17,1.03)):
        sphere('Shimmering crystal',(x,y,z),(.055,.025,.055),sparkle)

def cookie_model():
    sphere('Leaf serving mat',(0,0,.07),(.86,.70,.07),plate)
    cylinder('Baked cookie body',(0,0,.25),.66,.27,cookie,24)
    cylinder('Golden top crust',(0,0,.39),.65,.055,edge,24)
    for i in range(14):
        a=i*math.tau/14
        sphere('Cookie crumb rim',(.62*math.cos(a),.62*math.sin(a),.30),(.11,.10,.09),edge)
    for x,y,size in ((-.31,-.24,.105),(.17,-.29,.08),(.39,.10,.095),(-.22,.31,.095),(.02,.09,.11),(.13,.39,.07),(-.47,.05,.06)):
        sphere('Chocolate chip',(x,y,.43),(size,size*.8,.045),chips)
    cube('Leaf plate stem',(0,-.63,.075),(.05,.23,.035),vein,.01)

def export(name,generator):
    before=set(bpy.data.objects)
    generator()
    objects=list(set(bpy.data.objects)-before)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
    group=bpy.data.objects.new('AssetRoot_'+name,None)
    bpy.context.collection.objects.link(group)
    for obj in objects:obj.parent=group
    group.location.x=2.5*len([obj for obj in bpy.data.objects if obj.name.startswith('AssetRoot_')])

export('SugarCube',sugar_cube)
export('Cookie',cookie_model)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'ArtSource'/'VillageFood.blend'))
print('VILLAGE_FOOD_OK')
