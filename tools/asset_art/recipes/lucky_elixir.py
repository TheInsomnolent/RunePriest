from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "LuckyElixir", tilt=(10, -15, 10))
    bamboo = art.material("Bamboo travel flask", "977240", "D7BA75", "FFF0BA")
    leaf = art.material("Folded emerald leaves", "235C43", "52A16C", "AFE4A0")
    dark = art.material("Deep leaf folds", "244B38", "397A4C", "7BB76D")
    gold = art.material("Lucky brass coin", "976A31", "DBB65A", "FFF2AB")
    cord = art.material("Red lucky cord", "883740", "D66059", "FFB38E")
    art.box("Square bamboo canteen", (0, -0.05, 0), (0.96, 1.27, 0.53), bamboo, 0.16)
    art.plate("Left folded leaf", [(-0.49, -0.63), (0.42, -0.63), (0.21, -0.11), (-0.45, 0.44)],
              0.03, leaf, (0, 0, 0.29))
    art.plate("Right folded leaf", [(0.48, -0.59), (0.48, 0.47), (-0.37, -0.14), (-0.12, -0.62)],
              0.03, dark, (0, 0, 0.33))
    art.stroke("Leaf vein", [(-0.40, -0.53, 0.34), (-0.20, -0.17, 0.35), (-0.40, 0.28, 0.34)], 0.018, bamboo)
    art.box("Bamboo flask neck", (0, 0.69, 0), (0.34, 0.28, 0.34), bamboo, 0.045)
    art.box("Wooden plug", (0, 0.89, 0), (0.43, 0.18, 0.38), gold, 0.05)
    art.stroke("Lucky cord wrap", [(-0.48, 0.33, 0.25), (0.03, 0.20, 0.37), (0.46, 0.32, 0.25)], 0.025, cord)
    art.stroke("Dangling coin cord", [(0.26, 0.27, 0.32), (0.60, 0.10, 0.30), (0.64, -0.21, 0.30)], 0.023, cord)
    art.ring("Lucky coin", (0.64, -0.36, 0.31), 0.15, 0.052, gold)
    art.stroke("Clover stem", [(0.03, 0.95, 0), (-0.05, 1.22, 0)], 0.023, dark)
    for horizontal, vertical in ((-0.15, 1.32), (0.09, 1.32), (-0.15, 1.13), (0.09, 1.13)):
        art.sphere("Clover stopper leaf", (horizontal, vertical, 0), (0.15, 0.13, 0.055), leaf)
    return art.collection