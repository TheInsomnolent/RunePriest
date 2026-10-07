from math import radians

import bpy

from studio import toon_material


def build(scene, profile):
    stone = toon_material("Ink stone", "151B2B", "34475E", "6E8B99")
    gold = toon_material("Old gold", "68402E", "C89140", "FFE29A")
    turquoise = toon_material("Enamel", "123F48", "278E93", "8FE6CA")
    inscription = toon_material("Ivory inscription", "83B2A8", "D6EFE0", "FFFFFF")
    collection = bpy.data.collections.new("CalibrationSigil")
    scene.collection.children.link(collection)
    root = bpy.data.objects.new("SigilPose", None)
    collection.objects.link(root)
    root.rotation_euler = (radians(60), radians(-8), radians(-18))

    def attach(obj, name, material):
        obj.name = name
        for owner in list(obj.users_collection):
            owner.objects.unlink(obj)
        collection.objects.link(obj)
        obj.parent = root
        obj.data.materials.append(material)
        return obj

    def disc(name, radius, depth, height, material, vertices=8):
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=(0, 0, height))
        obj = attach(bpy.context.object, name, material)
        bevel = obj.modifiers.new("Soft carved edges", "BEVEL")
        bevel.width = 0.055
        bevel.segments = 2
        return obj

    disc("Octagonal stone body", 1.02, 0.23, 0, stone)
    disc("Gold face rim", 0.91, 0.09, 0.13, gold)
    disc("Recessed enamel", 0.77, 0.08, 0.185, turquoise)
    disc("Inner stone field", 0.63, 0.035, 0.235, stone)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.18, minor_radius=0.065,
                                    major_segments=24, minor_segments=8, location=(0, 1.04, 0))
    attach(bpy.context.object, "Hanging loop", gold)
    for horizontal, vertical in ((-0.58, 0.58), (0.58, 0.58), (-0.58, -0.58), (0.58, -0.58)):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=0.075,
                                           location=(horizontal, vertical, 0.21))
        attach(bpy.context.object, "Gold rivet", gold)
    paths = [((-0.19, -0.36), (-0.19, 0.38), (0.23, 0.15), (-0.19, -0.02), (0.24, -0.32)),
             ((-0.38, 0.15), (-0.19, 0.27))]
    for index, points in enumerate(paths):
        curve = bpy.data.curves.new(f"Rune stroke {index}", "CURVE")
        curve.dimensions = "3D"
        curve.bevel_depth = 0.036
        curve.bevel_resolution = 2
        curve.use_fill_caps = True
        spline = curve.splines.new("POLY")
        spline.points.add(len(points) - 1)
        for point, (horizontal, vertical) in zip(spline.points, points):
            point.co = (horizontal, vertical, 0.28, 1)
        obj = bpy.data.objects.new(curve.name, curve)
        collection.objects.link(obj)
        obj.parent = root
        curve.materials.append(inscription)
    return collection