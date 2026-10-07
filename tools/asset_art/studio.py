from __future__ import annotations

import bpy
from mathutils import Vector


def linear_color(hex_color: str) -> tuple[float, float, float, float]:
    channels = [int(hex_color[index:index + 2], 16) / 255 for index in (0, 2, 4)]
    return tuple(channel / 12.92 if channel <= 0.04045 else ((channel + 0.055) / 1.055) ** 2.4
                 for channel in channels) + (1.0,)


def toon_material(name: str, shadow: str, base: str, highlight: str):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    diffuse = nodes.new("ShaderNodeBsdfDiffuse")
    diffuse.inputs["Color"].default_value = (1, 1, 1, 1)
    lighting = nodes.new("ShaderNodeShaderToRGB")
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.interpolation = "EASE"
    ramp.color_ramp.elements.remove(ramp.color_ramp.elements[1])
    for index, (position, color) in enumerate(((0.08, shadow), (0.40, base), (0.85, highlight))):
        element = ramp.color_ramp.elements[0] if index == 0 else ramp.color_ramp.elements.new(position)
        element.position = position
        element.color = linear_color(color)
    emission = nodes.new("ShaderNodeEmission")
    output = nodes.new("ShaderNodeOutputMaterial")
    links = material.node_tree.links
    links.new(diffuse.outputs["BSDF"], lighting.inputs["Shader"])
    links.new(lighting.outputs["Color"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], emission.inputs["Color"])
    links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return material


def point_at(obj, target) -> None:
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def configure_scene(profile: dict):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = (
        component * profile.get("supersample", 1) for component in profile["size"])
    scene.render.resolution_percentage = 100
    scene.render.pixel_aspect_x = scene.render.pixel_aspect_y = 1
    scene.render.film_transparent = profile.get("transparent", True)
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0
    scene.view_settings.gamma = 1
    scene.world = bpy.data.worlds.new("StudioWorld")
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = linear_color(profile.get("world_color", "343943"))
    background.inputs["Strength"].default_value = profile.get("world_strength", 0.35)
    camera_data = bpy.data.cameras.new("Camera")
    camera = bpy.data.objects.new("Camera", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera.data.type = profile.get("camera", "ORTHO")
    camera.data.lens = profile.get("lens", 50)
    camera.data.ortho_scale = profile.get("ortho_scale", 4)
    camera.location = profile.get("camera_position", [3, -6, 4])
    point_at(camera, profile.get("camera_target", [0, 0, 0]))
    lights = profile.get("lights", [
        {"name": "Key", "position": [-3, -4, 7], "energy": 650, "size": 4, "color": "FFF0D5"},
        {"name": "Fill", "position": [4, -1, 3], "energy": 180, "size": 5, "color": "C8E4FF"},
    ])
    for settings in lights:
        data = bpy.data.lights.new(settings["name"], "AREA")
        data.energy = settings["energy"]
        data.shape = "DISK"
        data.size = settings.get("size", 4)
        data.color = linear_color(settings.get("color", "FFFFFF"))[:3]
        light = bpy.data.objects.new(settings["name"], data)
        scene.collection.objects.link(light)
        light.location = settings["position"]
        point_at(light, settings.get("target", [0, 0, 0]))
    return scene


def frame_collection(scene, collection, padding: float) -> None:
    bpy.context.view_layer.update()
    graph = bpy.context.evaluated_depsgraph_get()
    points = []
    for obj in collection.all_objects:
        if obj.type in ("MESH", "CURVE", "FONT", "SURFACE", "META") and not obj.hide_render:
            evaluated = obj.evaluated_get(graph)
            mesh = evaluated.to_mesh()
            try:
                if mesh:
                    points.extend(evaluated.matrix_world @ vertex.co for vertex in mesh.vertices)
            finally:
                evaluated.to_mesh_clear()
    if not points:
        raise ValueError("Auto framing requires renderable geometry in the returned collection.")
    camera = scene.camera
    inverse_rotation = camera.rotation_euler.to_matrix().transposed()
    projected = [inverse_rotation @ point for point in points]
    minimum = Vector(tuple(min(point[axis] for point in projected) for axis in range(3)))
    maximum = Vector(tuple(max(point[axis] for point in projected) for axis in range(3)))
    center = camera.rotation_euler.to_matrix() @ ((minimum + maximum) / 2)
    distance = max((camera.location - center).length, (maximum - minimum).length * 2, 1)
    camera.location = center + camera.rotation_euler.to_matrix() @ Vector((0, 0, distance))
    camera.data.clip_end = max(100, distance * 4)
    camera.data.ortho_scale = 1
    frame = camera.data.view_frame(scene=scene)
    frame_width = max(point.x for point in frame) - min(point.x for point in frame)
    frame_height = max(point.y for point in frame) - min(point.y for point in frame)
    camera.data.ortho_scale = max((maximum.x - minimum.x) / frame_width,
                                  (maximum.y - minimum.y) / frame_height) / (1 - padding * 2)