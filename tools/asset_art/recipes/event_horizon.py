from math import cos, pi, sin

from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "EventHorizon", tilt=(0, 0, -12))
    ember = art.glow("Outer accretion glow", "E76B36", 1.0)
    amber = art.glow("Accretion disk", "FFBD60", 1.1)
    white = art.glow("Gravitationally bent light", "FFF2CC", 1.3)
    void = art.material("Lightless event horizon", "02050A", "060A12", "0D1420")
    art.ring("Outer lensed halo", (0, 0, -0.06), 0.71, 0.047, ember)
    art.ring("Amber photon halo", (0, 0, 0), 0.665, 0.051, amber)
    art.ring("Thin photon ring", (0, 0, 0.045), 0.627, 0.023, white)
    art.sphere("Black hole shadow", (0, 0, 0.015), (0.607, 0.607, 0.065), void, subdivisions=4)
    for radius, width, surface in ((1.29, 0.036, ember), (1.16, 0.075, amber), (1.10, 0.022, white)):
        points = []
        for index in range(129):
            angle = index * 2 * pi / 128
            points.append((radius * cos(angle), 0.22 * sin(angle) - 0.08,
                           -0.23 * sin(angle)))
        art.stroke("Accretion disk orbit", points, width, surface, smooth=False)
    art.stroke("Upper lensing arc", [(0.755 * cos(angle), 0.755 * sin(angle), -0.04)
                                     for angle in [0.25 + index * 2.45 / 48 for index in range(49)]],
               0.014, amber, smooth=False)
    art.stroke("Matter spiraling inward", [(-1.44, -0.22, 0), (-1.17, -0.31, 0.08),
                                           (-0.74, -0.32, 0.18), (-0.28, -0.26, 0.23)],
               0.022, ember)
    for horizontal, vertical, size in ((1.36, 0.14, 0.029), (-1.31, 0.14, 0.023), (0.97, -0.35, 0.025)):
        art.sphere("Captured dust ember", (horizontal, vertical, 0), (size * 1.7, size, size), amber)
    return art.collection