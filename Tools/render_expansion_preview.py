"""Render the Blender source shelf, for checking the exported asset palette."""
import bpy
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource'/'BackyardAssets.blend'))
floor=bpy.data.materials.new('Preview background');floor.diffuse_color=(.68,.73,.60,1)
bpy.ops.mesh.primitive_plane_add(size=200,location=(8,0,-.04));bpy.context.object.data.materials.append(floor)
bpy.ops.object.camera_add(location=(10,-18,12));camera=bpy.context.object
camera.rotation_euler=(Vector((8,0,.75))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=19
scene=bpy.context.scene;scene.camera=camera
bpy.ops.object.light_add(type='AREA',location=(5,-6,10));bpy.context.object.data.energy=2200;bpy.context.object.data.size=9
scene.world.color=(.6,.6,.6);scene.render.engine='CYCLES';scene.cycles.samples=32
scene.render.resolution_x=1600;scene.render.resolution_y=680;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(root/'ArtSource'/'backyard-assets-preview.png')
bpy.ops.render.render(write_still=True)
