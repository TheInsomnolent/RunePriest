from math import cos, pi, sin

import bpy
from mathutils import Vector

from studio import linear_color, toon_material


def build(scene, profile):
    collection = bpy.data.collections.new("AetherInkwell")
    scene.collection.children.link(collection)
    glass = toon_material("Midnight glass", "111321", "29354C", "69819A")
    gold = toon_material("Antique brass", "513044", "BA8345", "FFE5A5")
    rim_shadow = toon_material("Ink-dark mouth", "080D18", "101728", "293B4C")
    ivory = toon_material("Quill pearl", "787AA8", "DDD8DB", "FFF8D9")
    feather_shadow = toon_material("Quill lavender shadow", "393858", "8B8FBA", "DDDDEC")
    feather_tip = toon_material("Quill peacock tip", "203951", "459FA3", "9BE9CC")

    def emissive(name, color, strength=1.5):
        material = bpy.data.materials.new(name)
        material.use_nodes = True
        nodes = material.node_tree.nodes
        nodes.clear()
        light = nodes.new("ShaderNodeEmission")
        light.inputs["Color"].default_value = linear_color(color)
        light.inputs["Strength"].default_value = strength
        output = nodes.new("ShaderNodeOutputMaterial")
        material.node_tree.links.new(light.outputs[0], output.inputs["Surface"])
        return material

    rainbow = bpy.data.materials.new("Living prismatic ink")
    rainbow.use_nodes = True
    nodes = rainbow.node_tree.nodes
    nodes.clear()
    coordinates = nodes.new("ShaderNodeTexCoord")
    split = nodes.new("ShaderNodeSeparateXYZ")
    palette = nodes.new("ShaderNodeValToRGB")
    palette.color_ramp.interpolation = "EASE"
    colors = ("FA267D", "FF8745", "FFE45E", "59EF91", "32DEEF", "657AFF", "DA60EF")
    palette.color_ramp.elements.remove(palette.color_ramp.elements[1])
    for index, color in enumerate(colors):
        stop = palette.color_ramp.elements[0] if index == 0 else palette.color_ramp.elements.new(index / (len(colors) - 1))
        stop.position = index / (len(colors) - 1)
        stop.color = linear_color(color)
    light = nodes.new("ShaderNodeEmission")
    light.inputs["Strength"].default_value = 1.6
    output = nodes.new("ShaderNodeOutputMaterial")
    links = rainbow.node_tree.links
    links.new(coordinates.outputs["Generated"], split.inputs[0])
    links.new(split.outputs["X"], palette.inputs[0])
    links.new(palette.outputs[0], light.inputs[0])
    links.new(light.outputs[0], output.inputs[0])
    pearl = emissive("White-hot ink glint", "D9FFF1", 2)
    cyan = emissive("Cyan ink spark", "35E4EF")
    pink = emissive("Rose ink spark", "FF4995")
    amber = emissive("Gold ink spark", "FFD16A")

    def attach(obj, name, material):
        obj.name = name
        for owner in list(obj.users_collection):
            owner.objects.unlink(obj)
        collection.objects.link(obj)
        obj.data.materials.append(material)
        return obj

    def mesh(name, vertices, faces, material):
        data = bpy.data.meshes.new(name)
        data.from_pydata(vertices, [], faces)
        data.update()
        obj = bpy.data.objects.new(name, data)
        collection.objects.link(obj)
        data.materials.append(material)
        return obj

    def vessel(name, levels, material, close_top=False):
        segments = 12
        vertices = [(radius * cos(2 * pi * index / segments + pi / 12),
                     radius * sin(2 * pi * index / segments + pi / 12), height)
                    for height, radius in levels for index in range(segments)]
        faces = [tuple(reversed(range(segments)))]
        faces.extend((level * segments + index, level * segments + (index + 1) % segments,
                      (level + 1) * segments + (index + 1) % segments, (level + 1) * segments + index)
                     for level in range(len(levels) - 1) for index in range(segments))
        if close_top:
            faces.append(tuple(range((len(levels) - 1) * segments, len(levels) * segments)))
        obj = mesh(name, vertices, faces, material)
        bevel = obj.modifiers.new("Soft glass edges", "BEVEL")
        bevel.width = 0.035
        bevel.segments = 2
        return obj

    def ring(name, height, radius, thickness, material):
        bpy.ops.mesh.primitive_torus_add(major_radius=radius, minor_radius=thickness,
                                        major_segments=36, minor_segments=8, location=(0, 0, height))
        return attach(bpy.context.object, name, material)

    def stroke(name, points, thickness, material):
        curve = bpy.data.curves.new(name, "CURVE")
        curve.dimensions = "3D"
        curve.bevel_depth = thickness
        curve.bevel_resolution = 3
        curve.use_fill_caps = True
        spline = curve.splines.new("BEZIER")
        spline.bezier_points.add(len(points) - 1)
        for point, position in zip(spline.bezier_points, points):
            point.co = position
            point.handle_left_type = "AUTO"
            point.handle_right_type = "AUTO"
        obj = bpy.data.objects.new(name, curve)
        collection.objects.link(obj)
        curve.materials.append(material)
        if material == rainbow:
            bpy.ops.object.select_all(action="DESELECT")
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.convert(target="MESH")
            obj = bpy.context.object
        return obj

    vessel("Cut glass inkwell", [(0.05, 0.54), (0.16, 0.79), (0.67, 0.87),
                                 (0.98, 0.72), (1.14, 0.43), (1.34, 0.43)], glass)
    vessel("Brass foot", [(0.07, 0.56), (0.12, 0.77), (0.21, 0.78), (0.24, 0.75)], gold)
    ring("Gold shoulder bead", 1.10, 0.46, 0.045, gold)
    ring("Mouth outer brass", 1.34, 0.45, 0.075, gold)
    ring("Dark inner lip", 1.345, 0.375, 0.042, rim_shadow)
    vessel("Open pool of magical ink", [(1.29, 0.355), (1.315, 0.355)], rainbow, True)
    right = scene.camera.rotation_euler.to_matrix() @ Vector((1, 0, 0))
    up = scene.camera.rotation_euler.to_matrix() @ Vector((0, 1, 0))
    toward = scene.camera.rotation_euler.to_matrix() @ Vector((0, 0, 1))
    front = Vector((scene.camera.location.x, scene.camera.location.y, 0)).normalized()
    window_center = front * 0.905 + Vector((0, 0, 0.62))
    window_vertices = [window_center + right * (0.58 * cos(index * 2 * pi / 32))
                       + Vector((0, 0, 0.27 * sin(index * 2 * pi / 32)))
                       for index in range(32)]
    mesh("Rainbow ink seen through glass", window_vertices, [tuple(range(32))], rainbow)
    stroke("Window gold surround", window_vertices + [window_vertices[0]], 0.027, gold)
    stroke("Glass reflected ribbon", [window_center + right * horizontal + Vector((0, 0, height)) + front * 0.015
                                      for horizontal, height in ((-0.42, 0.14), (-0.15, 0.23), (0.16, 0.21))], 0.022, pearl)
    stroke("Ink overflowing rim", [(0.22, -0.26, 1.33), (0.37, -0.40, 1.16),
                                    (0.50, -0.57, 0.95), (0.52, -0.59, 0.88)], 0.065, rainbow)

    base = Vector((0.08, 0.05, 1.28))
    axis = (right * 0.42 + up * 0.91).normalized()
    cross = (right * 0.91 - up * 0.42).normalized()
    stroke("Quill shaft", [base + axis * distance + cross * (0.07 * distance * distance) + toward * 0.05
                           for distance in (0, 0.45, 1.0, 1.5, 1.94)], 0.022, gold)
    mesh("Gold pen nib", [base - axis * 0.13, base + axis * 0.35 - cross * 0.07,
                          base + axis * 0.45, base + axis * 0.35 + cross * 0.07],
         [(0, 1, 2, 3)], gold)
    sections = [(0.43, 0.015), (0.65, 0.20), (0.91, 0.30), (1.19, 0.31),
                (1.47, 0.25), (1.70, 0.15), (1.96, 0)]
    for side in (-1, 1):
        vertices = []
        for index, (distance, width) in enumerate(sections):
            center = base + axis * distance + cross * (0.07 * distance * distance)
            edge = center + cross * width * side + axis * (-0.08 if index % 2 else 0)
            vertices.extend([center + toward * 0.04, edge])
        faces = [(index * 2, index * 2 + 1, index * 2 + 3, index * 2 + 2)
                 for index in range(len(sections) - 1)]
        feather = mesh("Quill bright vane" if side < 0 else "Quill shaded vane", vertices, faces,
                       ivory if side < 0 else feather_shadow)
        feather.data.materials.append(feather_tip)
        feather.data.polygons[-1].material_index = 1
        for index in (1, 3, 4):
            distance, width = sections[index]
            center = base + axis * distance + cross * (0.07 * distance * distance) + toward * 0.047
            stroke("Feather barb", [center, center + cross * width * side * 0.82 - axis * 0.09],
                   0.011, feather_shadow if side < 0 else ivory)

    stroke("Floating ink flourish", [Vector((0, 0, 1.34)) - right * 0.25,
                                      Vector((0, 0, 1.65)) - right * 0.62,
                                      Vector((0, 0, 1.92)) - right * 0.38,
                                      Vector((0, 0, 2.04)) - right * 0.48], 0.036, rainbow)
    for index, (horizontal, height, size, surface) in enumerate(
            ((-0.72, 1.70, 0.09, cyan), (-0.50, 2.16, 0.065, pink), (0.50, 1.65, 0.065, amber))):
        center = right * horizontal + Vector((0, 0, height)) + toward * 0.06
        vertices = [center + right * side + up * vertical for side, vertical in
                    ((0, size), (size * 0.18, size * 0.18), (size * 0.65, 0),
                     (size * 0.18, -size * 0.18), (0, -size), (-size * 0.18, -size * 0.18),
                     (-size * 0.65, 0), (-size * 0.18, size * 0.18))]
        mesh(f"Ink spark {index}", vertices, [tuple(range(8))], surface)
    return collection