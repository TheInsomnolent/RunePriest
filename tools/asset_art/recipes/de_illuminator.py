from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "DeIlluminator", tilt=(12, -18, -8))
    silver = art.material("Brushed pocket silver", "45515C", "9BB5BC", "E6E9D4")
    dark = art.material("Lightless enamel", "111B28", "273D4D", "557C85")
    gold = art.material("Lighter brass", "74553A", "CAA368", "FFE5B0")
    void = art.material("Captured darkness", "030610", "080E1C", "172639")
    cyan = art.glow("Absorbed light", "7BEEE5", 1.15)
    art.box("Pocket lighter body", (0, -0.30, 0), (0.95, 1.16, 0.43), silver, 0.10)
    art.box("Dark enamel face", (0, -0.29, 0.229), (0.71, 0.85, 0.035), dark, 0.06)
    art.box("Open hinged cap", (-0.71, 0.53, 0), (0.88, 0.47, 0.46), silver, 0.08, rotation=(0, 0, -34))
    art.box("Wick chimney", (0, 0.39, 0), (0.49, 0.28, 0.26), gold)
    for horizontal in (-0.15, 0, 0.15):
        art.box("Chimney vent", (horizontal, 0.39, 0.14), (0.055, 0.12, 0.025), dark, 0.01)
    art.ring("Engraved eclipse", (0, -0.27, 0.259), 0.23, 0.03, gold)
    art.sphere("Eclipse shadow", (0.075, -0.22, 0.285), (0.2, 0.2, 0.018), dark)
    art.plate("Dark flame", [(0, 0.49), (-0.21, 0.68), (-0.20, 0.94), (0.05, 1.37),
                            (0.05, 1.02), (0.23, 0.82), (0.16, 0.57)], 0.05, void)
    art.stroke("Light curling into void", [(-0.16, 0.60, 0.045), (-0.29, 0.83, 0.045),
                                           (-0.17, 1.10, 0.045), (0.05, 1.37, 0.045)], 0.025, cyan)
    art.spark("Last mote of light", (0.44, 1.06, 0), 0.11, cyan)
    return art.collection