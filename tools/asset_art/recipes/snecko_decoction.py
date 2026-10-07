from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "SneckoDecoction", tilt=(6, -12, -5))
    glaze = art.material("Snecko green glaze", "355F48", "76AA66", "D4E6A2")
    belly = art.material("Mottled violet glaze", "51324F", "9B648B", "DDB1C1")
    amber = art.material("Hypnotic amber eyes", "93632F", "F2BD54", "FFF4AC")
    pupil = art.material("Dark ceramic pupils", "14242D", "263944", "55716C")
    cork = art.material("Decoction cork", "886546", "C7A47A", "EFDAAC")
    shine = art.glow("Glaze reflection", "DFFFF0", 1.0)
    art.sphere("Coiled ceramic reservoir", (0, -0.35, 0), (0.69, 0.50, 0.34), glaze)
    art.stroke("Coiled belly tube", [(0.51, -0.24, 0.30), (0.38, -0.60, 0.34),
                                     (-0.36, -0.63, 0.32), (-0.55, -0.34, 0.28),
                                     (-0.35, -0.11, 0.29), (0.12, -0.20, 0.35)], 0.115, belly)
    art.stroke("Curved bottle neck", [(0.41, -0.25, 0), (0.45, 0.30, 0),
                                      (0.12, 0.65, 0), (-0.13, 0.89, 0)], 0.17, glaze)
    art.sphere("Snecko head bottle mouth", (-0.25, 0.85, 0), (0.42, 0.25, 0.24), glaze)
    art.box("Cork in snout", (-0.64, 0.85, 0), (0.18, 0.29, 0.27), cork, 0.045, rotation=(0, 0, 5))
    for horizontal, vertical, radius in ((-0.36, 0.97, 0.14), (-0.02, 0.89, 0.13), (0.06, 0.50, 0.10)):
        art.sphere("Hypnotic ceramic eye", (horizontal, vertical, 0.23), (radius, radius, 0.068), amber)
        art.stroke("Vertical eye slit", [(horizontal, vertical - radius * 0.52, 0.30),
                                          (horizontal, vertical + radius * 0.52, 0.30)], 0.026, pupil, False)
    for horizontal, vertical in ((-0.43, -0.30), (-0.20, -0.45), (0.09, -0.45), (0.38, -0.31)):
        art.stroke("Ceramic belly rib", [(horizontal - 0.04, vertical + 0.10, 0.46),
                                         (horizontal + 0.035, vertical - 0.07, 0.46)], 0.021, amber, False)
    art.stroke("Glaze glint", [(-0.39, -0.05, 0.26), (-0.14, 0.035, 0.29)], 0.025, shine)
    return art.collection