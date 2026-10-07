from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "RunicSphere", tilt=(8, -10, 0))
    glass = art.material("Deep aquamarine sphere", "143C54", "287F92", "80D0CD")
    brass = art.material("Runic orbit rings", "776136", "C5AA60", "FFF1B3")
    light = art.glow("Runes suspended in glass", "A8FFE9", 1.15)
    art.sphere("Runic crystal sphere", (0, 0, 0), (0.73, 0.73, 0.73), glass, subdivisions=4)
    art.ring("Inclined brass orbit", (0, 0, 0), 0.89, 0.035, brass, rotation=(65, 12, -22))
    art.ring("Crossed brass orbit", (0, 0, 0), 0.90, 0.027, brass, rotation=(22, 67, 18))
    art.rune("Central suspended rune", (0.01, 0.02, 0.765), 0.65, light)
    art.rune("Small left rune", (-0.43, 0.14, 0.62), 0.23, light, mirrored=True)
    art.rune("Small right rune", (0.43, -0.15, 0.62), 0.22, light)
    art.stroke("Glass reflection", [(-0.47, 0.32, 0.51), (-0.37, 0.49, 0.47),
                                    (-0.16, 0.58, 0.43)], 0.035, light)
    art.sphere("Orbit bead", (-0.77, -0.41, 0.18), (0.09, 0.09, 0.09), brass)
    art.spark("Orbiting rune mote", (0.84, 0.63, 0), 0.12, light)
    return art.collection