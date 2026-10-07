from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "Teardrop", tilt=(5, -8, -10))
    glass = art.material("Blue crystal ampoule", "315C87", "80BDE0", "DFFFFF")
    water = art.material("Condensed azure tear", "154D80", "338FC7", "8DE6ED")
    silver = art.material("Silver filigree cap", "526A80", "ACCAD2", "F5F4DE")
    shine = art.glow("Tear reflection", "E9FFF7", 1.0)
    art.lathe("Sealed teardrop vial", [(-0.82, 0), (-0.76, 0.25), (-0.55, 0.48),
                                      (-0.21, 0.57), (0.10, 0.50), (0.38, 0.31),
                                      (0.65, 0.13), (0.83, 0.055), (0.9, 0)], glass)
    art.plate("Liquid tear lens", [(0, -0.67), (-0.30, -0.56), (-0.43, -0.28),
                                   (-0.38, 0.03), (-0.20, 0.28), (0, 0.53),
                                   (0.20, 0.28), (0.38, 0.03), (0.43, -0.28), (0.30, -0.56)],
              0.055, water, (0, 0, 0.57), bevel=0.045)
    art.ring("Hanging cap eye", (0, 1.0, 0), 0.13, 0.037, silver)
    art.sphere("Silver cap", (0, 0.76, 0), (0.15, 0.13, 0.13), silver)
    for side in (-1, 1):
        art.stroke("Silver cap petal", [(0, 0.84, 0.08), (side * 0.15, 0.60, 0.12),
                                       (side * 0.27, 0.38, 0.22)], 0.027, silver)
    art.stroke("Long tear glint", [(-0.25, -0.32, 0.625), (-0.28, -0.05, 0.625),
                                   (-0.14, 0.19, 0.625)], 0.036, shine)
    art.sphere("Duplicated tiny tear", (0.20, -0.38, 0.63), (0.072, 0.11, 0.025), shine)
    return art.collection