from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "AetherQuill", tilt=(5, -8, -28))
    glass = art.material("Pearlescent glass ampoule", "54647F", "A4C8D7", "F1FFFF")
    pearl = art.material("Pearl feather", "8B87A8", "E3D9E4", "FFF9E0")
    shade = art.material("Feather shaded vane", "596580", "A2B4C7", "E5E9E6")
    gold = art.material("Quill nib gold", "815E37", "CBA761", "FFF0B4")
    art.lathe("Ink ampoule shaft", [(-1.0, 0), (-0.88, 0.12), (-0.62, 0.19),
                                    (-0.05, 0.19), (0.16, 0.11), (0.26, 0.09)], glass)
    colors = ("FF5F8A", "FFAD58", "FFE27C", "84EBBA", "61DDEB", "9B8DF4")
    for index, color in enumerate(colors):
        ink = art.glow(f"Prismatic ink {index}", color, 1.1)
        height = -0.76 + index * 0.13
        art.plate("Rainbow ink inside ampoule", [(-0.115, height), (0.115, height),
                                                 (0.13, height + 0.14), (-0.13, height + 0.14)],
                  0.025, ink, (0, 0, 0.19), bevel=0.01)
    art.plate("Golden writing nib", [(-0.09, -0.94), (0, -1.26), (0.09, -0.94), (0, -0.87)], 0.06, gold)
    art.ring("Sealed ampoule collar", (0, 0.16, 0), 0.12, 0.032, gold, rotation=(90, 0, 0))
    art.plate("Quill left vane", [(0, 0.19), (-0.31, 0.44), (-0.20, 0.47), (-0.42, 0.74),
                                  (-0.33, 0.97), (-0.22, 1.19), (0.03, 1.50), (0.02, 0.63)], 0.045, pearl)
    art.plate("Quill right vane", [(0, 0.19), (0.28, 0.46), (0.33, 0.79), (0.22, 1.17),
                                   (0.03, 1.50), (0.02, 0.63)], 0.045, shade)
    art.stroke("Golden quill spine", [(0, 0.18, 0.06), (0.015, 0.78, 0.06), (0.03, 1.42, 0.06)], 0.022, gold)
    for height, width in ((0.45, 0.23), (0.71, 0.29), (0.97, 0.23)):
        art.stroke("Pearl feather barb", [(0, height, 0.06), (-width, height + 0.15, 0.055)], 0.015, shade, False)
    art.stroke("Ampoule glass glint", [(-0.10, -0.65, 0.222), (-0.12, -0.18, 0.222)], 0.016, pearl)
    return art.collection