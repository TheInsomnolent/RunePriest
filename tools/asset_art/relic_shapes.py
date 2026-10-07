from math import cos, pi, radians, sin

import bpy
from mathutils import Euler, Vector

from studio import linear_color, toon_material


class RelicShapes:
    def __init__(self, scene, name, collection=None, tilt=(0, 0, 0)):
        self.collection = collection if collection is not None else bpy.data.collections.new(name)
        if collection is None:
            scene.collection.children.link(self.collection)
        self.root = bpy.data.objects.new(name + " pose", None)
        self.collection.objects.link(self.root)
        self.root.rotation_euler = (scene.camera.rotation_euler.to_matrix()
                                   @ Euler(tuple(radians(angle) for angle in tilt)).to_matrix()).to_euler()

    def material(self, name, shadow, base, highlight):
        return toon_material(name, shadow, base, highlight)

    def glow(self, name, color, strength=1.3):
        material = bpy.data.materials.new(name)
        material.use_nodes = True
        nodes = material.node_tree.nodes
        nodes.clear()
        emission = nodes.new("ShaderNodeEmission")
        emission.inputs["Color"].default_value = linear_color(color)
        emission.inputs["Strength"].default_value = strength
        output = nodes.new("ShaderNodeOutputMaterial")
        material.node_tree.links.new(emission.outputs[0], output.inputs["Surface"])
        return material

    def attach(self, obj, name, material):
        obj.name = name
        for owner in list(obj.users_collection):
            owner.objects.unlink(obj)
        self.collection.objects.link(obj)
        obj.parent = self.root
        obj.data.materials.append(material)
        return obj

    def soften(self, obj, width=0.035):
        bevel = obj.modifiers.new("Soft illustrated edges", "BEVEL")
        bevel.width = width
        bevel.segments = 3
        return obj

    def mesh(self, name, vertices, faces, material):
        data = bpy.data.meshes.new(name)
        data.from_pydata(vertices, [], faces)
        data.update()
        obj = bpy.data.objects.new(name, data)
        self.collection.objects.link(obj)
        obj.parent = self.root
        data.materials.append(material)
        return obj

    def box(self, name, location, size, material, bevel=0.035, rotation=(0, 0, 0)):
        bpy.ops.mesh.primitive_cube_add(size=1, location=location)
        obj = self.attach(bpy.context.object, name, material)
        for vertex in obj.data.vertices:
            vertex.co = Vector(tuple(value * scale for value, scale in zip(vertex.co, size)))
        obj.rotation_euler = tuple(radians(angle) for angle in rotation)
        return self.soften(obj, bevel) if bevel else obj

    def sphere(self, name, location, size, material, subdivisions=3):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=location)
        obj = self.attach(bpy.context.object, name, material)
        for vertex in obj.data.vertices:
            vertex.co = Vector(tuple(value * scale for value, scale in zip(vertex.co, size)))
        return obj

    def ring(self, name, location, radius, thickness, material, rotation=(0, 0, 0), scale=(1, 1, 1)):
        bpy.ops.mesh.primitive_torus_add(major_radius=radius, minor_radius=thickness,
                                        major_segments=48, minor_segments=10, location=location)
        obj = self.attach(bpy.context.object, name, material)
        obj.rotation_euler = tuple(radians(angle) for angle in rotation)
        obj.scale = scale
        return obj

    def stroke(self, name, points, width, material, smooth=True):
        data = bpy.data.curves.new(name, "CURVE")
        data.dimensions = "3D"
        data.bevel_depth = width
        data.bevel_resolution = 3
        data.use_fill_caps = True
        spline = data.splines.new("BEZIER" if smooth else "POLY")
        if smooth:
            spline.bezier_points.add(len(points) - 1)
            for point, position in zip(spline.bezier_points, points):
                point.co = position
                point.handle_left_type = "AUTO"
                point.handle_right_type = "AUTO"
        else:
            spline.points.add(len(points) - 1)
            for point, position in zip(spline.points, points):
                point.co = (*position, 1)
        obj = bpy.data.objects.new(name, data)
        self.collection.objects.link(obj)
        obj.parent = self.root
        data.materials.append(material)
        return obj

    def plate(self, name, outline, depth, material, center=(0, 0, 0), bevel=0.025):
        count = len(outline)
        vertices = [(horizontal + center[0], vertical + center[1], height + center[2])
                    for height in (-depth / 2, depth / 2) for horizontal, vertical in outline]
        faces = [tuple(reversed(range(count))), tuple(range(count, count * 2))]
        faces.extend((index, (index + 1) % count, (index + 1) % count + count, index + count)
                     for index in range(count))
        obj = self.mesh(name, vertices, faces, material)
        return self.soften(obj, bevel) if bevel else obj

    def lathe(self, name, levels, material, segments=48):
        vertices = [(radius * cos(index * 2 * pi / segments), height, radius * sin(index * 2 * pi / segments))
                    for height, radius in levels for index in range(segments)]
        faces = [(level * segments + index, level * segments + (index + 1) % segments,
                  (level + 1) * segments + (index + 1) % segments, (level + 1) * segments + index)
                 for level in range(len(levels) - 1) for index in range(segments)]
        return self.mesh(name, vertices, faces, material)

    def spark(self, name, position, radius, material):
        outline = [(0, radius), (-radius * 0.19, radius * 0.19), (-radius * 0.7, 0),
                   (-radius * 0.19, -radius * 0.19), (0, -radius), (radius * 0.19, -radius * 0.19),
                   (radius * 0.7, 0), (radius * 0.19, radius * 0.19)]
        return self.plate(name, outline, 0.025, material, position, bevel=0)

    def rune(self, name, position, size, material, mirrored=False):
        horizontal, vertical, depth = position
        direction = -1 if mirrored else 1
        paths = [[(-0.25, -0.5), (-0.25, 0.5), (0.3, 0.15), (-0.25, -0.02), (0.3, -0.4)],
                 [(-0.45, 0.18), (-0.25, 0.32)]]
        for index, path in enumerate(paths):
            self.stroke(f"{name} {index}", [(horizontal + direction * size * across,
                                           vertical + size * height, depth) for across, height in path],
                        size * 0.052, material, smooth=False)


def toolbox(scene, ascended=False):
    art = RelicShapes(scene, "AscendedToolbox" if ascended else "BlessedToolbox", tilt=(16, -18, -8))
    enamel = art.material("Blessed enamel", "17413F", "438C79", "A3D3AA")
    ivory = art.material("Ivory tool grips", "897157", "DDCDA5", "FFF4D7")
    gold = art.material("Blessed brass", "66412C", "C89848", "FFEAB0")
    steel = art.material("Chisel steel", "253641", "7299A3", "CDE6E3")
    dark = art.material("Toolbox inner shadow", "182728", "283D38", "4C6753")
    magic = art.glow("Consecrated light", "AEFFE2" if not ascended else "FFF2AE")
    body = ivory if ascended else enamel
    art.box("Toolbox body", (0, -0.25, 0), (1.8, 0.86, 0.85), body, 0.09)
    art.box("Open dark tray", (0, 0.20, 0), (1.62, 0.09, 0.68), dark)
    art.box("Raised lid", (0, 0.58, -0.35), (1.85, 0.55, 0.12), body, rotation=(-18, 0, 0))
    art.stroke("Carrying handle", [(-0.4, 0.83, -0.3), (-0.4, 1.07, -0.3),
                                  (0.4, 1.07, -0.3), (0.4, 0.83, -0.3)], 0.055, gold)
    for horizontal in (-0.68, 0.68):
        art.box("Brass case strap", (horizontal, -0.25, 0.438), (0.12, 0.84, 0.035), gold, 0.012)
        art.box("Brass foot", (horizontal, -0.74, 0.12), (0.28, 0.15, 0.68), gold)
    art.box("Clasp", (0, -0.02, 0.47), (0.25, 0.33, 0.10), gold)
    art.rune("Case blessing", (-0.23, -0.35, 0.47), 0.32, magic)
    art.stroke("Hammer shaft", [(-0.57, 0.16, 0), (-0.77, 0.91, 0)], 0.065, ivory, False)
    art.box("Hammer head", (-0.77, 0.95, 0), (0.49, 0.23, 0.25), steel, rotation=(0, 0, 14))
    art.stroke("Chisel grip", [(0.40, 0.12, 0.06), (0.59, 0.65, 0.06)], 0.075, ivory, False)
    art.plate("Chisel blade", [(0.52, 0.58), (0.68, 0.92), (0.82, 0.86), (0.64, 0.54)], 0.10, steel)
    if ascended:
        art.ring("Floating blessing halo", (0, 1.43, -0.18), 0.45, 0.038, gold, rotation=(68, 0, 0))
        for horizontal, vertical in ((-1.10, 0.6), (1.02, 1.0), (0.62, 1.57)):
            art.spark("Ascension light", (horizontal, vertical, 0.1), 0.13, magic)
        art.rune("Ascended lid sigil", (0, 0.57, -0.24), 0.36, magic)
    return art.collection


def sealed_scroll(scene, profile, healing=False):
    from recipes.anchor_scroll import build

    collection = build(scene, profile)
    seal = collection.objects.get("Anchor-shaped blue wax seal")
    rotation = scene.camera.rotation_euler.to_matrix()
    center = seal.data.vertices[0].co - rotation @ Vector((-0.075, 0.34, 0))
    for obj in list(collection.objects):
        if obj.name == "Anchor-shaped blue wax seal" or obj.name.startswith("Wax "):
            bpy.data.objects.remove(obj, do_unlink=True)
    art = RelicShapes(scene, "Healing seal" if healing else "Execution seal", collection)
    art.root.location = center
    wax = art.material("Green healing wax" if healing else "Crimson sentencing wax",
                       "174937" if healing else "57192C", "389B66" if healing else "AF3045",
                       "9BDFA0" if healing else "F57874")
    ivory = art.material("Seal impression", "A5AB82", "E8EBC5", "FFFFE0")
    if healing:
        art.plate("Heart-shaped wax", [(0, -0.43), (0.37, -0.06), (0.43, 0.19), (0.32, 0.36),
                                       (0.15, 0.39), (0, 0.25), (-0.15, 0.39), (-0.32, 0.36),
                                       (-0.43, 0.19), (-0.37, -0.06)], 0.12, wax)
        art.box("Healing mark upright", (0, 0.02, 0.08), (0.13, 0.40, 0.055), ivory, 0.016)
        art.box("Healing mark crossbar", (0, 0.02, 0.08), (0.37, 0.13, 0.055), ivory, 0.016)
    else:
        art.plate("Sword-shaped sealing wax", [(-0.08, -0.46), (0.08, -0.46), (0.08, -0.27),
                                               (0.32, -0.27), (0.32, -0.13), (0.11, -0.13),
                                               (0.11, 0.32), (0, 0.54), (-0.11, 0.32),
                                               (-0.11, -0.13), (-0.32, -0.13), (-0.32, -0.27),
                                               (-0.08, -0.27)], 0.12, wax)
        art.stroke("Sword impression", [(0, -0.04, 0.071), (0, 0.33, 0.071)], 0.016, ivory, False)
    return collection