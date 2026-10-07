from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "LightweightClothRobe", tilt=(5, -10, -5))
    linen = art.material("Light linen", "938575", "DFD7B9", "FFFAE3")
    fold = art.material("Linen folds", "7E746C", "B9B29A", "E8DFC2")
    lining = art.material("Wine red lining", "481F38", "833B56", "C67982")
    cord = art.material("Red waist cord", "702B39", "C3595B", "F1A58F")
    gold = art.material("Small brass fasteners", "725334", "BE984D", "FCE1A0")
    outline = [(-0.60, -1.0), (0.13, -1.07), (0.66, -0.88), (0.40, 0.03),
               (0.83, -0.25), (1.02, 0.13), (0.66, 0.72), (0.25, 0.87),
               (-0.25, 0.87), (-0.66, 0.72), (-1.02, 0.13), (-0.83, -0.25), (-0.40, 0.03)]
    art.plate("Loose flowing robe", outline, 0.16, linen, bevel=0.05)
    art.plate("Inside hood", [(-0.28, 0.83), (-0.27, 1.08), (0, 1.29), (0.27, 1.08),
                              (0.28, 0.83), (0, 0.50)], 0.12, lining)
    art.stroke("Soft hood rim", [(-0.28, 0.83, 0.11), (-0.27, 1.08, 0.11), (0, 1.29, 0.11),
                                 (0.27, 1.08, 0.11), (0.28, 0.83, 0.11)], 0.074, linen)
    art.plate("Open robe lining", [(-0.055, 0.49), (0.06, 0.49), (0.25, -0.96), (-0.10, -1.02)],
              0.03, lining, (0, 0, 0.10))
    for side in (-1, 1):
        art.stroke("Front robe edging", [(side * 0.26, 0.84, 0.15), (side * 0.07, 0.36, 0.17),
                                          (side * 0.05, -0.11, 0.15), (side * 0.25, -0.96, 0.14)], 0.032, linen)
        art.stroke("Hanging fabric fold", [(side * 0.21, -0.28, 0.12), (side * 0.38, -0.79, 0.12)],
                   0.025, fold)
        art.stroke("Sleeve fold", [(side * 0.49, 0.52, 0.12), (side * 0.78, 0.10, 0.12)], 0.026, fold)
        art.stroke("Sleeve lining cuff", [(side * 0.84, -0.18, 0.13), (side * 0.96, 0.10, 0.13)],
                   0.042, lining, False)
    art.stroke("Waist tie", [(-0.41, -0.13, 0.15), (0, -0.19, 0.24), (0.41, -0.13, 0.15)], 0.033, cord)
    art.stroke("Dangling waist tie", [(0, -0.18, 0.24), (0.14, -0.39, 0.23), (0.22, -0.62, 0.17)], 0.025, cord)
    art.sphere("Cord knot", (0, -0.18, 0.26), (0.06, 0.06, 0.04), gold)
    return art.collection