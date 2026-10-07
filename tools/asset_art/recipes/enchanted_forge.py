from math import cos, pi, radians, sin
import os
from pathlib import Path
import random

import bpy

from studio import linear_color, toon_material


def build(scene, profile):
    rng = random.Random(profile.get("seed", 0))
    collection = bpy.data.collections.new("EnchantedForge")
    scene.collection.children.link(collection)

    def material(name, color, metallic=0, emission=0):
        if not emission:
            result = toon_material(name, "050C10", color, color)
            ramp = next(node for node in result.node_tree.nodes if node.bl_idname == "ShaderNodeValToRGB")
            ramp.color_ramp.interpolation = "CONSTANT"
            ramp.color_ramp.elements[1].position = 0.12
            ramp.color_ramp.elements[2].position = 0.6
            ramp.color_ramp.elements[1].color = tuple(channel * 0.45 for channel in linear_color(color)[:3]) + (1,)
            return result
        result = bpy.data.materials.new(name)
        result.use_nodes = True
        shader = result.node_tree.nodes.get("Principled BSDF")
        shader.inputs["Base Color"].default_value = linear_color(color)
        shader.inputs["Roughness"].default_value = 0.7
        shader.inputs["Metallic"].default_value = metallic
        shader.inputs["Emission Color"].default_value = linear_color(color)
        shader.inputs["Emission Strength"].default_value = emission
        return result

    iron = material("Blue black iron", "34434B", 0.65)
    face = material("Worn steel working face", "81A6AB", 0.55)
    bands = material("Iron bands", "20282C", 0.5)
    wood = material("Old stump", "584F43")
    endgrain = material("Cut timber", "89745A")
    mortar = material("Cold masonry recesses", "101C22")
    stones = [material(f"Masonry {index}", color) for index, color in enumerate(
        ("34444B", "40515A", "263840", "536067", "303C43"))]
    dark = material("Furnace soot", "100F17")
    coal = material("Hot coals", "FF240B", emission=2)
    flame = material("Fire gold", "FF8C36", emission=3)
    magic = material("Crimson inscription", "FF160B", emission=5)
    copper = material("Rune inlay", "C66846", metallic=0.3)

    def attach(obj, name, surface):
        obj.name = name
        for owner in list(obj.users_collection):
            owner.objects.unlink(obj)
        collection.objects.link(obj)
        obj.data.materials.append(surface)
        return obj

    def box(name, location, scale, surface, bevel=0.04, rotation=0):
        bpy.ops.mesh.primitive_cube_add(size=1, location=location)
        obj = attach(bpy.context.object, name, surface)
        obj.scale = scale
        obj.rotation_euler.z = rotation
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        if bevel:
            modifier = obj.modifiers.new("Broad worn corners", "BEVEL")
            modifier.width = bevel
            modifier.segments = 1
        return obj

    def cylinder(name, location, radius, depth, surface, vertices=12):
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
        return attach(bpy.context.object, name, surface)

    def mesh(name, vertices, faces, surface, bevel=0):
        data = bpy.data.meshes.new(name)
        data.from_pydata(vertices, [], faces)
        data.update()
        obj = bpy.data.objects.new(name, data)
        collection.objects.link(obj)
        data.materials.append(surface)
        if bevel:
            modifier = obj.modifiers.new("Forged edge bevel", "BEVEL")
            modifier.width = bevel
            modifier.segments = 1
        return obj

    box("Floor foundation", (0, 0, -0.3), (40, 40, 0.5), mortar, 0)
    for row in range(6):
        for column in range(6):
            horizontal = -4.5 + column * 1.3 + (row % 2) * 0.6
            depth = -4 + row * 1.3
            vertices = [(horizontal + cos(angle) * rng.uniform(0.5, 0.85),
                         depth + sin(angle) * rng.uniform(0.4, 0.7), rng.uniform(0.0, 0.025))
                        for angle in [index * 2 * pi / 6 for index in range(6)]]
            mesh("Broken floor planes", vertices, [tuple(range(6))], rng.choice(stones))
    box("Workshop back wall", (0, 4.4, 4), (40, 0.7, 16), mortar, 0)
    for row in range(5):
        for column in range(5):
            horizontal = -3.8 + column * 1.4 + (row % 2) * 0.6
            height = 0.5 + row * 1.05
            vertices = [(horizontal + cos(angle) * rng.uniform(0.55, 0.9),
                         4.02, height + sin(angle) * rng.uniform(0.35, 0.65))
                        for angle in [index * 2 * pi / 6 for index in range(6)]]
            mesh("Broken masonry planes", vertices, [tuple(reversed(range(6)))], rng.choice(stones[:3]))
    for horizontal in (-4.8,):
        box("Shadowed timber pillar", (horizontal, 2.7, 3.4), (0.55, 0.7, 6.8), wood)
    box("Overhead beam", (-1.5, 2.7, 6), (7, 0.8, 0.5), bands)

    center_x, center_y, arch_z = 0.85, 2.65, 2.45
    box("Hearth base", (center_x, center_y, 0.6), (4.4, 2.6, 1.1), stones[2], 0.13)
    box("Hearth front lip", (center_x, 1.28, 1.05), (4.5, 0.4, 0.25), stones[3], 0.07)
    box("Furnace dark back", (center_x, 3.6, 2.2), (2.9, 0.2, 2.4), dark)
    for side in (-1, 1):
        for row in range(3):
            box("Forge jamb", (center_x + side * 1.65, 2.25, 1.3 + row * 0.46),
                (0.72, 1.8, 0.43), stones[(row + 2) % len(stones)], 0.05)
    for index in range(11):
        start = index * pi / 11 + 0.016
        end = (index + 1) * pi / 11 - 0.016
        vertices = [(center_x + radius * cos(angle), depth, arch_z + radius * sin(angle))
                    for depth in (1.35, 3.3) for radius, angle in
                    ((1.28, start), (1.99, start), (1.99, end), (1.28, end))]
        mesh("Arch voussoir", vertices, [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
                                        (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)],
             stones[index % len(stones)], 0.035)
    box("Chimney hood", (center_x, 3.2, 5.5), (2.65, 1.6, 2.6), stones[2], 0.12)
    for height in (4.45, 5.05, 5.7):
        box("Chimney iron strap", (center_x, 2.35, height), (2.75, 0.1, 0.12), bands)

    for index in range(32):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=rng.uniform(0.09, 0.22),
            location=(center_x + rng.uniform(-1.15, 1.15), rng.uniform(1.7, 3.25), rng.uniform(1.12, 1.3)))
        attach(bpy.context.object, "Glowing coal", coal if index % 3 else flame)
    for index in range(7):
        horizontal = center_x + rng.uniform(-1.05, 1.05)
        depth = rng.uniform(2.2, 3.0)
        height = rng.uniform(0.3, 0.95)
        width = rng.uniform(0.10, 0.23)
        phase = rng.uniform(-pi, pi)
        vertices = []
        for level in range(6):
            progress = level / 5
            radius = width * (1 - progress) ** 0.7 + 0.005
            bend = sin(progress * 5 + phase) * progress * 0.23
            vertices.extend((horizontal + bend + radius * cos(angle),
                             depth + radius * sin(angle), 1.2 + height * progress)
                            for angle in [segment * 2 * pi / 8 for segment in range(8)])
        faces = [(level * 8 + segment, level * 8 + (segment + 1) % 8,
                  (level + 1) * 8 + (segment + 1) % 8, (level + 1) * 8 + segment)
                 for level in range(5) for segment in range(8)]
        mesh("Curling flame", vertices, faces, flame if index % 3 == 0 else coal)

    anvil_x, anvil_y = -0.65, -1.0
    cylinder("Anvil stump", (anvil_x, anvil_y, 0.62), 0.85, 1.12, wood, 14)
    cylinder("Stump top", (anvil_x, anvil_y, 1.19), 0.83, 0.08, endgrain, 14)
    for height in (0.30, 0.95):
        bpy.ops.mesh.primitive_torus_add(major_radius=0.84, minor_radius=0.045,
                                       major_segments=28, minor_segments=6, location=(anvil_x, anvil_y, height))
        attach(bpy.context.object, "Stump iron hoop", bands)
    levels = [(1.23, 1.02, 0.60), (1.42, 0.85, 0.47), (1.63, 0.48, 0.30),
              (1.98, 0.62, 0.38), (2.18, 1.04, 0.47)]
    vertices = [(anvil_x + horizontal * half_width, anvil_y + depth * half_depth, height)
                for height, half_width, half_depth in levels
                for horizontal, depth in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    faces = [(0, 3, 2, 1), (16, 17, 18, 19)]
    faces.extend((level * 4 + corner, level * 4 + (corner + 1) % 4,
                  (level + 1) * 4 + (corner + 1) % 4, (level + 1) * 4 + corner)
                 for level in range(4) for corner in range(4))
    mesh("Forged anvil waist", vertices, faces, iron, 0.06)
    box("Anvil working face", (anvil_x, anvil_y, 2.21), (2.32, 1.02, 0.17), face, 0.045)
    horn_vertices = []
    for horizontal, half_depth, half_height in ((-1.05, 0.45, 0.18), (-1.6, 0.27, 0.13), (-2.25, 0.015, 0.015)):
        horn_vertices.extend((anvil_x + horizontal, anvil_y + half_depth * cos(angle), 2.09 + half_height * sin(angle))
                             for angle in [index * 2 * pi / 8 for index in range(8)])
    horn_faces = [(level * 8 + index, level * 8 + (index + 1) % 8,
                   (level + 1) * 8 + (index + 1) % 8, (level + 1) * 8 + index)
                  for level in range(2) for index in range(8)]
    horn_faces.extend([tuple(reversed(range(8))), tuple(range(16, 24))])
    mesh("Tapered anvil horn", horn_vertices, horn_faces, face, 0.025)
    box("Hardy hole shadow", (anvil_x + 0.76, anvil_y + 0.17, 2.302), (0.16, 0.18, 0.008), dark, 0.008)
    box("Hammer handle", (anvil_x + 0.95, anvil_y - 0.1, 2.36), (1.25, 0.12, 0.12), wood, 0.03, radians(-35))
    box("Hammer head", (anvil_x + 0.48, anvil_y + 0.2, 2.44), (0.25, 0.57, 0.25), iron, 0.05, radians(-35))

    font_path = Path(os.environ.get("ASSET_ART_KANJI_FONT", "C:/Windows/Fonts/YuGothB.ttc"))
    if not font_path.is_file():
        raise ValueError("Set ASSET_ART_KANJI_FONT to a font containing Japanese kanji.")
    font = bpy.data.fonts.load(str(font_path))
    glyphs = [("\u706b", (-0.7, 1.2, 2.9), 0.45, -15),
              ("\u708e", (0.1, 1.0, 3.5), 0.62, 8),
              ("\u935b", (1.05, 0.9, 4.15), 0.73, -9),
              ("\u9b42", (1.8, 1.25, 4.85), 0.58, 15)]
    for index, (glyph, location, size, angle) in enumerate(glyphs):
        data = bpy.data.curves.new(f"Magic kanji {index}", "FONT")
        data.body = glyph
        data.font = font
        data.size = size
        data.align_x = "CENTER"
        data.align_y = "CENTER"
        data.extrude = 0.004
        obj = bpy.data.objects.new(data.name, data)
        collection.objects.link(obj)
        obj.location = location
        obj.rotation_euler = (scene.camera.location - obj.location).to_track_quat("Z", "Y").to_euler()
        obj.rotation_euler.rotate_axis("Z", radians(angle))
        data.materials.append(magic)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.ops.object.convert(target="MESH")
        light_data = bpy.data.lights.new(f"Kanji red spill {index}", "POINT")
        light_data.energy = 22
        light_data.color = (1, 0.018, 0.008)
        light_data.shadow_soft_size = 0.45
        light = bpy.data.objects.new(light_data.name, light_data)
        collection.objects.link(light)
        light.location = location
    for index in range(18):
        height = rng.uniform(1.5, 4.9)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=rng.uniform(0.008, 0.028),
            location=(center_x + rng.uniform(-1.1, 0.8), rng.uniform(0.4, 2.4), height))
        attach(bpy.context.object, "Rising ember", magic if index % 3 else flame)

    for index in range(3):
        horizontal = -3.5 + index * 0.35
        box("Rack tool shaft", (horizontal, 3.35, 2.2), (0.045, 0.05, 1.25), bands, 0.01,
            rng.uniform(-0.12, 0.12))
        box("Rack tool head", (horizontal, 3.35, 2.85), (0.22, 0.12, 0.15), iron)
    box("Tool rack", (-2.8, 3.3, 3), (2.25, 0.18, 0.15), wood)
    for index in range(3):
        box("Anvil decorative copper mark", (anvil_x - 0.2 + index * 0.2, anvil_y - 0.39, 1.91),
            (0.035, 0.018, 0.16 - index * 0.025), copper, 0.003)
    return collection