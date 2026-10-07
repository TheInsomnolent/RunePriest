from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "BaptisedIdol", tilt=(9, -12, -4))
    jade = art.material("Pale carved jade", "42695F", "A0C6A8", "E3EBC3")
    shade = art.material("Recessed jade", "23443F", "4D7B6C", "90B59A")
    gold = art.material("Idol gold leaf", "795C3F", "CEAB61", "FFEDAB")
    water = art.material("Luminous baptism", "216C8B", "6BDADD", "D5FFF0")
    art.box("Stepped idol plinth", (0, -0.86, 0), (1.06, 0.24, 0.62), gold, 0.04)
    art.plate("Carved robe", [(-0.45, -0.73), (0.45, -0.73), (0.25, 0.45), (-0.25, 0.45)], 0.48, jade)
    art.sphere("Hood", (0, 0.69, 0), (0.43, 0.49, 0.31), jade)
    art.sphere("Hood opening", (0, 0.68, 0.255), (0.27, 0.32, 0.105), shade)
    art.sphere("Serene stone face", (0, 0.69, 0.33), (0.18, 0.235, 0.12), jade)
    for side in (-1, 1):
        art.stroke("Folded idol arm", [(side * 0.25, 0.29, 0.2), (side * 0.42, -0.04, 0.28),
                                       (side * 0.17, -0.17, 0.40)], 0.13, jade)
        art.stroke("Closed eye", [(side * 0.05, 0.72, 0.448), (side * 0.13, 0.72, 0.428)], 0.014, shade)
        art.stroke("Gown fold", [(side * 0.13, -0.26, 0.25), (side * 0.22, -0.67, 0.26)], 0.022, shade)
    art.sphere("Baptism bowl", (0, -0.16, 0.46), (0.27, 0.12, 0.2), gold)
    art.sphere("Bowl of living water", (0, -0.065, 0.48), (0.22, 0.027, 0.16), water)
    art.stroke("Spilling blessing", [(0.07, -0.08, 0.63), (0.09, -0.28, 0.67),
                                    (0.06, -0.40, 0.67)], 0.04, water)
    art.sphere("Suspended blessing drop", (0.07, -0.55, 0.67), (0.06, 0.09, 0.06), water)
    art.ring("Idol halo", (0, 0.93, -0.14), 0.47, 0.032, gold)
    return art.collection