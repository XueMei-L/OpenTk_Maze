import bpy
import os
import json
import re

sobjects = bpy.context.selected_objects

object = sobjects[0]

object_data=object.data
object_name = object.name

pattern_ellipsoid=re.compile("^Sphere")
pattern_box=re.compile("^Cube")

object_type=None
if pattern_box.match(object_name):
    object_type="box"
elif pattern_ellipsoid.match(object_name):
    object_type="ellipsoid"
else:
    object_type="nocollision"

collision_data={}

collision_data["type"]=object_type
location=[x for x in object.location]
collision_data["location"]=[location[0],location[2],-location[1]]
scale=[x for x in object.scale]
collision_data["scale"]=[scale[0],scale[2],scale[1]]
rotation=[x for x in object.rotation_euler]
collision_data["rotation"]=[rotation[0],rotation[2],-rotation[1]]


vertices = object_data.vertices

default_material={}
default_material["name"]="default"
default_material["diffuse_color"]=[1.0,1.0,1.0,1.0]



indexdata = {}
slots=[]

mesh_data = {}

mesh_data["collision"]=collision_data
mesh_data["materials"]=[default_material]


material_slots=object.material_slots
for ms in material_slots:
    indexdata[ms.material.name]=[]
    slots.append(ms.material.name)
    mat={}
    mat["name"]=ms.material.name
    mat["diffuse_color"]=[v for v in ms.material.diffuse_color]
    if ms.material.get("texture_id") is not None:
        print("Localizada textura")
        mat["texture_id"]=ms.material.get("texture_id")
    mesh_data["materials"].append(mat)



mesh_data["nvertex"] = len(vertices)
operated_vertices = []
normal_vertices=[]
for vert in vertices:
    coord = vert.co
    normal=vert.normal
    operated_vertices.append(coord[0])
    operated_vertices.append(coord[2])
    operated_vertices.append(-coord[1])
    normal_vertices.append(normal[0])
    normal_vertices.append(normal[2])
    normal_vertices.append(-normal[1])

mesh_data["vertexdata"] = operated_vertices
mesh_data["normaldata"] = normal_vertices
mesh_data["weightdata"] = [1.0] * len(vertices)

polygons = object_data.polygons

nslots=len(mesh_data["materials"])-1
mesh_data["nindex"] = [0] * nslots
mesh_data["indexdata"]=[ [] for _ in mesh_data["materials"]]
mesh_data["indexdata"].pop()


nindices=0   
for polygon in polygons:
    loops = polygon.loop_indices
    slot=polygon.material_index
    for loopindex in loops:
        loop=object_data.loops[loopindex]
        vertex_index=loop.vertex_index
        
        print(f"{slot} {vertex_index}")
        mesh_data["indexdata"][slot].append(vertex_index)
        mesh_data["nindex"][slot]+=1
        nindices+=1


uvs=[ [] for _ in mesh_data["materials"]]

for polygon in polygons:
    loops=polygon.loop_indices
    slot=polygon.material_index
    for loopindex in loops:
        loop=object_data.loops[loopindex]
        uvloop=object_data.uv_layers.active.data[loopindex]
        u=uvloop.uv[0]
        v=uvloop.uv[1]
        uvs[slot].append(u)
        uvs[slot].append(v)
        
uvs_flat=[x for l in uvs for x in l]
mesh_data["uvs"]=uvs_flat

dir_path = os.path.dirname(os.path.dirname(os.path.realpath(__file__)))
# new_path=os.path.dirname(os.path.join(dir_path,'..\..'))

new_path = os.path.join(dir_path, "assets/sm_cube_dragon_uv.json")

print(new_path)

with open(new_path, "w") as file:
    json.dump(mesh_data, file)

print("Finalizada la escritura de los datos")
