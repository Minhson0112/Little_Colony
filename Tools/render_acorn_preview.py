import bpy, math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/AcornGarden.blend'))
for i,name in enumerate(['AntAcornHome','AntAcornHome2','AntAcornHome3','SmallTree','TallGrass','SmallParasol']):
    bpy.data.objects['AssetRoot_'+name].location=((i%3-1)*3.0, 1.6 if i<3 else -1.6, 0)
bpy.ops.mesh.primitive_plane_add(size=200)
floor=bpy.context.object
mat=bpy.data.materials.new('Preview cream');mat.diffuse_color=(.77,.79,.67,1);floor.data.materials.append(mat);floor.location.z=-.04
bpy.ops.object.camera_add(location=(6,-12,10))
camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.7))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=11.5
scene=bpy.context.scene;scene.camera=camera
bpy.ops.object.light_add(type='AREA',location=(0,-4,9));bpy.context.object.data.energy=1500;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=7
scene.world.color=(.5,.5,.5);scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.filepath=str(root/'Docs/acorn-garden-art-preview.png');bpy.ops.render.render(write_still=True)
