from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "CursedRing", tilt=(12, -20, -13))
    metal = art.material("Blackened silver", "232232", "606175", "BEC4C8")
    gold = art.material("Old gem setting", "5B424A", "A17B67", "DEBC8A")
    ruby = art.material("Cursed garnet", "440E26", "B42D54", "F97786")
    pupil = art.material("Unblinking slit", "080C13", "111423", "232E3D")
    glint = art.glow("Ruby reflection", "FFC2B6", 1.0)
    art.ring("Heavy cursed band", (0, -0.25, 0), 0.66, 0.135, metal)
    art.plate("Eye bezel", [(-0.69, 0.53), (-0.39, 0.25), (0, 0.13), (0.39, 0.25),
                            (0.69, 0.53), (0.35, 0.81), (0, 0.88), (-0.35, 0.81)], 0.18, gold)
    art.plate("Garnet eye", [(-0.53, 0.53), (-0.27, 0.34), (0, 0.27), (0.27, 0.34),
                             (0.53, 0.53), (0.27, 0.72), (0, 0.77), (-0.27, 0.72)], 0.12, ruby,
              center=(0, 0, 0.13), bevel=0.04)
    art.plate("Eye vertical pupil", [(0, 0.30), (0.065, 0.53), (0, 0.74), (-0.065, 0.53)],
              0.025, pupil, center=(0, 0, 0.205), bevel=0.006)
    art.stroke("Gem glint", [(-0.31, 0.60, 0.225), (-0.17, 0.66, 0.225)], 0.022, glint)
    for side in (-1, 1):
        art.stroke("Claw setting", [(side * 0.56, 0.34, 0.10), (side * 0.40, 0.47, 0.24)], 0.038, metal)
        art.stroke("Band etched thorn", [(side * 0.58, -0.17, 0.12), (side * 0.66, -0.36, 0.12),
                                          (side * 0.45, -0.56, 0.12)], 0.022, gold, False)
    return art.collection