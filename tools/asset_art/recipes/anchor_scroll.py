from math import cos, pi, sin

import bpy
from mathutils import Vector

from studio import toon_material


def build(scene, profile):
    collection = bpy.data.collections.new("AnchorScroll")
    scene.collection.children.link(collection)
    paper = toon_material("Warm parchment", "776053", "DAC69B", "FFF3D6")
    paper_edge = toon_material("Cut paper edges", "907355", "EAD6AF", "FFF8E4")
    crease = toon_material("Paper curl shadows", "433B38", "877255", "BDA278")
    cord = toon_material("Linen binding", "775634", "BF9561", "F4D5A0")
    wax = toon_material("Ocean blue sealing wax", "102958", "246BCE", "78CBF4")
    wax_glint = toon_material("Soft wax highlights", "255DA0", "60B4EC", "B0EAF9")
    rotation = scene.camera.rotation_euler.to_matrix()
    right = rotation @ Vector((1, 0, 0))
    up = rotation @ Vector((0, 1, 0))
    toward = rotation @ Vector((0, 0, 1))
    axis = (right * 0.52 + up * 0.82 + toward * 0.30).normalized()
    across = (right * 0.82 - up * 0.52).normalized()
    front = across.cross(axis).normalized()

    def mesh(name, vertices, faces, material):
        data = bpy.data.meshes.new(name)
        data.from_pydata(vertices, [], faces)
        data.update()
        obj = bpy.data.objects.new(name, data)
        collection.objects.link(obj)
        data.materials.append(material)
        return obj

    def stroke(name, points, thickness, material):
        data = bpy.data.curves.new(name, "CURVE")
        data.dimensions = "3D"
        data.bevel_depth = thickness
        data.bevel_resolution = 3
        data.use_fill_caps = True
        spline = data.splines.new("POLY")
        spline.points.add(len(points) - 1)
        for point, position in zip(spline.points, points):
            point.co = (*position, 1)
        obj = bpy.data.objects.new(name, data)
        collection.objects.link(obj)
        data.materials.append(material)
        return obj

    segments = 240
    spiral = []
    vertices = []
    for index in range(segments + 1):
        progress = index / segments
        angle = pi / 2 - 5 * pi * (1 - progress)
        radius = 0.10 + 0.37 * progress
        radial = across * (radius * cos(angle)) + front * (radius * sin(angle))
        spiral.append(radial)
        uneven_edge = 0.016 * sin(angle * 3) + 0.009 * sin(angle * 7)
        vertices.extend([radial - axis * (1.30 + uneven_edge),
                         radial + axis * (1.30 + uneven_edge)])
    faces = [(index * 2, index * 2 + 2, index * 2 + 3, index * 2 + 1)
             for index in range(segments)]
    roll = mesh("Continuous rolled parchment sheet", vertices, faces, paper)
    solidify = roll.modifiers.new("Paper thickness", "SOLIDIFY")
    solidify.thickness = 0.025
    bevel = roll.modifiers.new("Soft paper edges", "BEVEL")
    bevel.width = 0.009
    bevel.segments = 2
    for side, label in ((-1, "Lower"), (1, "Upper")):
        edge = [vertices[index * 2 + (1 if side > 0 else 0)] for index in range(segments + 1)]
        stroke(f"{label} parchment spiral edge", edge, 0.014, paper_edge)
        stroke(f"{label} spiral inner shade", [point - axis * side * 0.035 for point in edge],
               0.012, crease)
    stroke("Overlapping paper seam", [spiral[-1] + axis * distance
                                      for distance in (-1.30, -0.85, 0, 0.85, 1.30)], 0.012, crease)
    stroke("Paper seam lit edge", [spiral[-1] + across * 0.025 + axis * distance
                                   for distance in (-1.28, -0.8, 0, 0.8, 1.28)], 0.015, paper_edge)

    for offset in (-0.095, 0.095):
        points = []
        for index in range(97):
            angle = index * 2 * pi / 96
            distance = offset + 0.025 * sin(angle * 2)
            points.append(axis * distance + across * (0.485 * cos(angle)) + front * (0.485 * sin(angle)))
        stroke("Twine around sealed scroll", points, 0.021, cord)

    seal_center = front * 0.53 + toward * 0.10 - axis * 0.04

    def seal_point(horizontal, vertical, depth=0):
        return seal_center + right * horizontal + up * vertical + toward * depth

    outline = [(-0.075, 0.34), (0.075, 0.34), (0.075, 0.18), (0.30, 0.18),
               (0.30, 0.045), (0.075, 0.045), (0.075, -0.30), (0.23, -0.24),
               (0.32, -0.12), (0.24, -0.10), (0.44, 0.025), (0.47, -0.21),
               (0.38, -0.16), (0.29, -0.34), (0, -0.50), (-0.29, -0.34),
               (-0.38, -0.16), (-0.47, -0.21), (-0.44, 0.025), (-0.24, -0.10),
               (-0.32, -0.12), (-0.23, -0.24), (-0.075, -0.30), (-0.075, 0.045),
               (-0.30, 0.045), (-0.30, 0.18), (-0.075, 0.18)]
    count = len(outline)
    seal_vertices = [seal_point(horizontal, vertical, depth)
                     for depth in (0, 0.085) for horizontal, vertical in outline]
    seal_faces = [tuple(range(count)), tuple(reversed(range(count, count * 2)))]
    seal_faces.extend((index, index + count, (index + 1) % count + count, (index + 1) % count)
                      for index in range(count))
    seal = mesh("Anchor-shaped blue wax seal", seal_vertices, seal_faces, wax)
    bevel = seal.modifiers.new("Rounded pressed wax", "BEVEL")
    bevel.width = 0.028
    bevel.segments = 3
    bpy.ops.mesh.primitive_torus_add(major_radius=0.105, minor_radius=0.052,
                                    major_segments=40, minor_segments=12,
                                    location=seal_point(0, 0.405, 0.045))
    eye = bpy.context.object
    eye.name = "Wax anchor eye"
    eye.rotation_euler = scene.camera.rotation_euler
    for owner in list(eye.users_collection):
        owner.objects.unlink(eye)
    collection.objects.link(eye)
    eye.data.materials.append(wax)
    stroke("Wax shank glint", [seal_point(-0.027, vertical, 0.090) for vertical in (-0.24, 0.015)],
           0.012, wax_glint)
    stroke("Wax stock glint", [seal_point(horizontal, 0.143, 0.09) for horizontal in (-0.25, -0.13)],
           0.010, wax_glint)
    stroke("Wax curved fluke glint", [seal_point(horizontal, vertical, 0.09)
                                     for horizontal, vertical in ((-0.32, -0.22), (-0.23, -0.30), (-0.06, -0.38))],
           0.012, wax_glint)
    return collection