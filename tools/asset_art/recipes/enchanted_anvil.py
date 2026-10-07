from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "EnchantedAnvil", tilt=(14, -19, -5))
    iron = art.material("Forged iron", "273242", "637889", "B6CAD2")
    steel = art.material("Polished striking face", "56667B", "A4BDC5", "E1F0DD")
    gold = art.material("Brass foot band", "5E4438", "BB9154", "FAD994")
    glow = art.glow("Charged anvil inscriptions", "5CE9F1")
    art.plate("Anvil body and horn", [(-0.94, -0.65), (0.86, -0.65), (0.71, -0.38),
                                     (0.45, -0.26), (0.39, 0.15), (0.90, 0.31),
                                     (0.90, 0.64), (-0.54, 0.64), (-0.85, 0.5),
                                     (-1.37, 0.46), (-1.1, 0.19), (-0.48, 0.12),
                                     (-0.43, -0.28), (-0.75, -0.41)], 0.68, iron, bevel=0.055)
    art.box("Anvil striking face", (0.17, 0.655, 0), (1.53, 0.11, 0.73), steel, 0.035)
    art.box("Anvil foot trim", (-0.025, -0.6, 0.365), (1.69, 0.11, 0.05), gold, 0.02)
    art.rune("Anvil rune", (-0.10, -0.02, 0.366), 0.50, glow)
    art.spark("Hammering spark", (-0.50, 1.04, 0), 0.17, glow)
    art.spark("Hammering spark small", (0.36, 0.98, 0), 0.10, glow)
    return art.collection