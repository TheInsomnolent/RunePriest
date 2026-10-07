from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "EternalCandle", tilt=(12, -8, -5))
    ivory = art.material("Everlasting beeswax", "97744F", "E9D5A2", "FFF4D4")
    brass = art.material("Old candleholder", "654839", "BE8A45", "FFE0A0")
    wick = art.material("Charred wick", "1A2024", "40333A", "79605A")
    outer = art.glow("Amber eternal flame", "FF913D", 1.1)
    inner = art.glow("Flame heart", "FFF1AC", 1.4)
    art.lathe("Broad brass candle dish", [(-0.84, 0), (-0.84, 0.52), (-0.74, 0.73),
                                          (-0.60, 0.79), (-0.56, 0.75), (-0.66, 0.40)], brass)
    art.lathe("Pillar of wax", [(-0.69, 0), (-0.69, 0.34), (0.58, 0.34), (0.63, 0.30),
                                (0.56, 0.15), (0.57, 0)], ivory)
    for horizontal, end in ((-0.25, 0.15), (-0.08, -0.16), (0.19, 0.25)):
        art.stroke("Slow wax drip", [(horizontal, 0.55, 0.26), (horizontal, end, 0.31)], 0.052, ivory)
    art.stroke("Wick", [(0, 0.56, 0), (0.03, 0.79, 0)], 0.024, wick, False)
    art.plate("Unending flame", [(0, 0.72), (-0.19, 0.87), (-0.22, 1.08), (-0.1, 1.32),
                                (0.06, 1.65), (0.09, 1.28), (0.25, 1.04), (0.19, 0.83)], 0.075, outer)
    art.plate("Bright flame center", [(0, 0.76), (-0.1, 0.9), (-0.07, 1.08), (0.035, 1.28),
                                     (0.04, 1.04), (0.13, 0.91)], 0.035, inner, center=(0, 0, 0.055))
    art.stroke("Infinity mark", [(-0.20, -0.36, 0.34), (-0.11, -0.23, 0.36),
                                 (0.12, -0.43, 0.36), (0.21, -0.31, 0.34),
                                 (0.12, -0.23, 0.36), (-0.11, -0.43, 0.36), (-0.20, -0.36, 0.34)], 0.022, brass)
    art.ring("Candleholder finger loop", (0.72, -0.45, 0), 0.23, 0.053, brass)
    return art.collection