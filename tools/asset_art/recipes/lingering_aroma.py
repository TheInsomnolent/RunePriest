from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "LingeringAroma", tilt=(5, -9, -7))
    silk = art.material("Rose silk sachet", "794357", "C98291", "F6C7C2")
    panel = art.material("Embroidered linen", "A58473", "E2CDB4", "FFF0D0")
    leaf = art.material("Dried aromatic leaves", "375549", "659A72", "C1DBA0")
    gold = art.material("Sachet gold thread", "82603E", "C9A66B", "FFE7AF")
    scent = art.glow("Lingering perfumed smoke", "E1C1EB", 1.0)
    art.sphere("Soft filled sachet", (0, -0.20, 0), (0.64, 0.67, 0.25), silk)
    art.plate("Gathered cloth mouth", [(-0.28, 0.27), (-0.45, 0.67), (-0.27, 0.63),
                                      (-0.10, 0.77), (0.05, 0.65), (0.32, 0.73),
                                      (0.42, 0.62), (0.25, 0.27)], 0.12, silk)
    art.plate("Stitched linen panel", [(-0.35, -0.48), (0.27, -0.55), (0.39, -0.02),
                                      (0.27, 0.16), (-0.34, 0.13)], 0.025, panel, (0, 0, 0.265))
    art.stroke("Leaf embroidery stem", [(-0.12, -0.43, 0.30), (0.02, -0.18, 0.30),
                                        (0.06, 0.08, 0.30)], 0.018, leaf)
    for horizontal, height, side in ((0, -0.21, -1), (0.03, -0.10, 1), (0.06, 0.01, -1)):
        art.plate("Embroidered leaf", [(horizontal, height), (horizontal + side * 0.20, height + 0.04),
                                       (horizontal + side * 0.22, height + 0.15),
                                       (horizontal + side * 0.05, height + 0.13)], 0.02, leaf, (0, 0, 0.31))
    art.stroke("Gathering cord", [(-0.30, 0.34, 0.10), (0, 0.30, 0.23), (0.30, 0.34, 0.10)], 0.03, gold)
    for side in (-1, 1):
        art.stroke("Silk bow", [(0, 0.32, 0.26), (side * 0.29, 0.45, 0.22),
                                (side * 0.42, 0.28, 0.20), (0, 0.32, 0.26)], 0.026, gold)
        art.stroke("Tassel cord", [(side * 0.18, 0.32, 0.23), (side * 0.53, -0.02, 0.23),
                                   (side * 0.65, -0.45, 0.18)], 0.021, gold)
        art.plate("Rose tassel", [(side * 0.65 - 0.08, -0.66), (side * 0.65 + 0.08, -0.66),
                                  (side * 0.65 + 0.04, -0.43), (side * 0.65 - 0.04, -0.43)],
                  0.06, silk, (0, 0, 0.19))
    art.stroke("Long perfume curl", [(-0.12, 0.68, 0), (-0.33, 0.99, 0),
                                      (0.10, 1.18, 0), (0.04, 1.47, 0)], 0.027, scent)
    art.stroke("Trailing perfume curl", [(0.17, 0.73, 0), (0.38, 0.96, 0), (0.27, 1.16, 0)], 0.018, scent)
    return art.collection