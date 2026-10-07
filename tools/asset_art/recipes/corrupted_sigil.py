from math import cos, pi, sin

from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "CorruptedSigil", tilt=(8, -14, -14))
    stone = art.material("Blighted stone", "171B29", "3A3E51", "737388")
    gold = art.material("Tarnished sigil rim", "55453B", "B29862", "ECDD9A")
    light = art.glow("Corruption in the cracks", "F25B79")
    ring = [(0.93 * cos(index * pi / 4), 0.93 * sin(index * pi / 4)) for index in range(8)]
    art.plate("Broken gold octagon", ring, 0.20, gold)
    art.plate("Crimson core", [(across * 0.89, height * 0.89) for across, height in ring], 0.23, light)
    art.plate("Left sigil fragment", [(-0.80, -0.31), (-0.56, -0.72), (-0.08, -0.79),
                                      (-0.19, -0.26), (-0.02, 0.04), (-0.20, 0.32),
                                      (-0.10, 0.82), (-0.56, 0.67), (-0.82, 0.27)], 0.15, stone, (0, 0, 0.18))
    art.plate("Right sigil fragment", [(0.04, -0.80), (0.57, -0.67), (0.82, -0.27),
                                       (0.80, 0.31), (0.56, 0.72), (0.04, 0.82),
                                       (-0.04, 0.33), (0.14, 0.04), (-0.04, -0.26)], 0.15, stone, (0, 0, 0.18))
    art.rune("Twisted holy inscription", (0.12, 0.02, 0.28), 0.88, light, mirrored=True)
    art.ring("Broken sigil hanger", (0, 0.96, 0), 0.16, 0.047, gold)
    for horizontal, vertical, radius in ((-1.01, -0.17, 0.1), (1.04, 0.18, 0.13), (0.74, -0.94, 0.08)):
        art.plate("Orbiting stone shard", [(-radius, 0), (0, -radius), (radius * 0.7, radius)], 0.11,
                  stone, (horizontal, vertical, 0))
    return art.collection