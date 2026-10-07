from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "ImbuedTeacup", tilt=(22, -8, -7))
    porcelain = art.material("Ivory porcelain", "817895", "DBDCE9", "FFF8E2")
    blue = art.material("Cobalt glaze", "182C57", "346FB1", "85CCD9")
    gold = art.material("Gilt china rim", "886A3A", "D8B969", "FFF2AF")
    tea = art.material("Enchanted tea", "153B32", "357D5E", "8FCE94")
    steam = art.glow("Luminous tea steam", "BAF9DD", 1.05)
    art.lathe("Porcelain saucer", [(-0.67, 0.05), (-0.67, 0.7), (-0.58, 1.02),
                                  (-0.51, 1.07), (-0.48, 0.98), (-0.55, 0.5), (-0.55, 0)], porcelain)
    art.lathe("Hollow teacup", [(-0.54, 0.23), (-0.47, 0.35), (-0.22, 0.55), (0.29, 0.72),
                               (0.37, 0.72), (0.37, 0.66), (0.22, 0.63), (-0.20, 0.40), (-0.34, 0)], porcelain)
    art.ring("Gilt lip", (0, 0.365, 0), 0.697, 0.025, gold, rotation=(90, 0, 0))
    art.ring("Blue foot", (0, -0.47, 0), 0.32, 0.035, blue, rotation=(90, 0, 0))
    art.ring("Curved cup handle", (0.76, 0, 0), 0.32, 0.074, porcelain, scale=(0.78, 1.0, 1))
    art.lathe("Tea surface", [(0.27, 0), (0.27, 0.635)], tea)
    art.rune("Painted cup rune", (0, -0.03, 0.635), 0.40, blue)
    art.stroke("Curl of fragrant magic", [(-0.15, 0.3, 0), (-0.34, 0.65, 0),
                                          (0.08, 0.98, 0), (-0.1, 1.31, 0)], 0.038, steam)
    art.stroke("Second tea curl", [(0.24, 0.37, 0), (0.42, 0.68, 0), (0.31, 0.9, 0)], 0.022, steam)
    art.spark("Tea spark", (0.38, 1.15, 0), 0.11, gold)
    return art.collection