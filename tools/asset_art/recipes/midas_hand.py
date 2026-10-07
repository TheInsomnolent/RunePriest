from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "MidasHand", tilt=(7, -14, -12))
    gold = art.material("Solid enchanted gold", "986025", "E9AD3E", "FFF0A0")
    shade = art.material("Gold palm creases", "79501F", "B58331", "E4BC60")
    nails = art.material("Polished gold nails", "BA893B", "F1CF73", "FFFFC5")
    jewel = art.material("Cuff emerald", "1A6359", "3EAE88", "A1EDD0")
    art.sphere("Golden palm", (0, 0, 0), (0.43, 0.54, 0.18), gold)
    for index, (horizontal, tip) in enumerate(((-0.33, 0.91), (-0.11, 1.22), (0.12, 1.34), (0.34, 1.16))):
        end = horizontal * 1.13
        art.stroke(f"Golden finger {index}", [(horizontal, 0.24, 0), (end, 0.67, 0.01),
                                             (end, tip - 0.06, 0.01)], 0.092, gold)
        art.sphere(f"Rounded fingertip {index}", (end, tip - 0.06, 0.01), (0.092, 0.105, 0.092), gold)
        art.box(f"Golden nail {index}", (end, tip - 0.06, 0.098), (0.11, 0.15, 0.018), nails, 0.027)
        art.stroke(f"Finger crease {index}", [(end - 0.05, 0.63, 0.092), (end + 0.05, 0.63, 0.092)],
                   0.009, shade, False)
    art.stroke("Golden thumb", [(0.29, -0.23, 0.02), (0.57, -0.01, 0.01), (0.72, 0.37, 0.02)], 0.115, gold)
    art.sphere("Thumb tip", (0.72, 0.37, 0.02), (0.115, 0.12, 0.1), gold)
    art.stroke("Palm lifeline", [(0.22, 0.20, 0.16), (0.10, -0.03, 0.19), (0.22, -0.27, 0.15)], 0.018, shade)
    art.stroke("Palm line", [(-0.26, 0.12, 0.15), (-0.04, 0.04, 0.19), (0.15, 0.13, 0.18)], 0.016, shade)
    art.box("Heavy gold cuff", (0, -0.54, 0), (0.66, 0.29, 0.42), gold, 0.045)
    art.plate("Cuff emerald", [(0, -0.43), (-0.10, -0.54), (0, -0.65), (0.10, -0.54)],
              0.06, jewel, (0, 0, 0.25))
    art.spark("Golden glimmer", (-0.67, 0.74, 0), 0.12, nails)
    return art.collection