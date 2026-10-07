from math import cos, pi, sin

from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "HolySparkler", tilt=(0, -8, -25))
    ivory = art.material("Carved ivory grip", "827967", "E0D6B1", "FFF9DB")
    gold = art.material("Sanctified gold", "8B632D", "E3AF4F", "FFF0A3")
    steel = art.material("Sparkler wire", "3C535C", "95B0B6", "DAEAE1")
    warm = art.glow("Golden sparks", "FFD765", 1.2)
    white = art.glow("Holy white heart", "FFFAD2", 1.5)
    art.stroke("Sparkler stem", [(0, -1.18, 0), (0, 0.67, 0)], 0.035, steel, False)
    art.box("Ivory sparkler grip", (0, -0.91, 0), (0.19, 0.71, 0.18), ivory, 0.06)
    for height in (-1.19, -0.65):
        art.box("Grip gold collar", (0, height, 0), (0.23, 0.09, 0.22), gold, 0.02)
    art.spark("Holy starburst", (0, 0.67, 0), 0.47, warm)
    art.spark("White star center", (0, 0.67, 0.04), 0.27, white)
    for index in range(8):
        angle = index * pi / 4 + pi / 8
        radius = 0.63 if index % 2 == 0 else 0.52
        art.stroke("Spray of sparks", [(0.33 * cos(angle), 0.67 + 0.33 * sin(angle), 0),
                                        (radius * cos(angle), 0.67 + radius * sin(angle), 0)], 0.021, warm, False)
    for horizontal, vertical in ((-0.66, 1.07), (0.64, 0.57), (-0.34, 0.09)):
        art.spark("Free golden spark", (horizontal, vertical, 0), 0.085, white)
    return art.collection