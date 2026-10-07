from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "ThrummingElixir", tilt=(8, -13, 8))
    copper = art.material("Resonant copper", "713C31", "C67B4C", "FFE3A0")
    glass = art.material("Amber amplifier glass", "72383A", "D76952", "FFC599")
    red = art.material("Thrumming concentrate", "721F37", "D93651", "FF8D84")
    dark = art.material("Insulated flask base", "29353B", "49636B", "9CAEA6")
    pulse = art.glow("Visible vibration", "FFF4B8", 1.1)
    art.plate("Hexagonal resonance flask", [(-0.29, -0.73), (0.29, -0.73), (0.38, -0.49),
                                          (0.33, 0.34), (0.20, 0.56), (-0.20, 0.56),
                                          (-0.33, 0.34), (-0.38, -0.49)], 0.38, glass, bevel=0.055)
    art.plate("Elixir level window", [(-0.23, -0.60), (0.23, -0.60), (0.26, -0.41),
                                      (0.23, 0.23), (-0.23, 0.23), (-0.26, -0.41)], 0.025, red,
              (0, 0, 0.21), bevel=0.03)
    art.stroke("Tuning fork frame", [(-0.52, 1.10, 0), (-0.52, -0.58, 0),
                                     (0, -0.87, 0), (0.52, -0.58, 0), (0.52, 1.10, 0)], 0.065, copper)
    for side in (-1, 1):
        art.box("Weighted resonator tip", (side * 0.52, 1.05, 0), (0.21, 0.31, 0.20), copper, 0.055)
        art.stroke("Resonance wave", [(side * 0.74, 0.78, 0), (side * 0.84, 0.94, 0),
                                      (side * 0.79, 1.11, 0)], 0.021, pulse)
    art.box("Weighted base", (0, -0.88, 0), (0.88, 0.19, 0.51), dark, 0.065)
    art.box("Copper cap", (0, 0.57, 0), (0.43, 0.16, 0.41), copper, 0.03)
    art.stroke("Pulse through liquid", [(-0.20, -0.16, 0.25), (-0.10, -0.16, 0.25),
                                        (-0.025, 0.06, 0.25), (0.06, -0.39, 0.25),
                                        (0.13, -0.16, 0.25), (0.22, -0.16, 0.25)], 0.023, pulse, False)
    return art.collection