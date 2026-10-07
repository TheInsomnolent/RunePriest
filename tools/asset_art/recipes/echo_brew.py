from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "EchoBrew", tilt=(8, -12, -7))
    glass = art.material("Blue echo glass", "23456C", "5E93BF", "D0EEF1")
    brew = art.material("Indigo echo brew", "35325F", "7973B6", "C8B9E9")
    silver = art.material("Pewter bottle harness", "505B6B", "A5B5BB", "F2E9CF")
    cork = art.material("Brew cork", "71573D", "BBA079", "EAD9B2")
    shine = art.glow("Glass reflections", "DCF8F3", 1.0)
    for horizontal, height, radius in ((-0.37, -0.26, 0.54), (0.49, -0.17, 0.39)):
        art.sphere("Resonance chamber", (horizontal, height, 0), (radius, radius * 1.12, radius * 0.68), glass)
        art.sphere("Brew visible through glass", (horizontal, height - 0.07, radius * 0.50),
                   (radius * 0.80, radius * 0.74, radius * 0.23), brew)
        art.stroke("Long glass neck", [(horizontal, height + radius * 0.70, 0),
                                        (horizontal, 0.76, 0)], radius * 0.24, glass, False)
        art.ring("Pewter mouth", (horizontal, 0.76, 0), radius * 0.28, 0.035, silver, rotation=(90, 0, 0))
        art.box("Stopper", (horizontal, 0.87, 0), (radius * 0.44, 0.20, radius * 0.44), cork, 0.035)
        art.stroke("Glass shoulder glint", [(horizontal - radius * 0.56, height + radius * 0.31, radius * 0.48),
                                             (horizontal - radius * 0.37, height + radius * 0.65, radius * 0.42)],
                   0.024, shine)
    art.stroke("Connecting glass siphon", [(-0.27, 0.20, -0.06), (-0.03, 0.57, -0.06),
                                           (0.24, 0.48, -0.06), (0.44, 0.18, -0.06)], 0.065, glass)
    art.stroke("Shared bottle foot", [(-0.73, -0.76, 0), (-0.34, -0.88, 0),
                                      (0.41, -0.68, 0), (0.72, -0.56, 0)], 0.06, silver)
    for horizontal, height, radius in ((-0.38, -0.27, 0.21), (0.48, -0.18, 0.145)):
        art.stroke("Echo ripple", [(horizontal - radius * 0.4, height + radius, 0.47),
                                  (horizontal + radius, height, 0.47),
                                  (horizontal - radius * 0.4, height - radius, 0.47)], 0.028, shine)
    return art.collection