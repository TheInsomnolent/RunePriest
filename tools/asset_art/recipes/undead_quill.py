from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "UndeadQuill", tilt=(0, -6, -28))
    feather = art.material("Raven feather", "172331", "344B54", "718F8C")
    bone = art.material("Old bone quill", "8E8268", "D9D9B5", "FFFFDD")
    hollow = art.material("Empty sockets", "101C23", "263E3E", "506858")
    gold = art.material("Nib binding", "695239", "B89456", "F0D490")
    green = art.glow("Restless ink", "8BF1B7", 1.1)
    art.plate("Ragged feather vane", [(-0.03, -0.46), (-0.43, -0.05), (-0.20, -0.10),
                                     (-0.50, 0.35), (-0.33, 0.30), (-0.46, 0.71),
                                     (-0.30, 1.05), (0, 1.42), (0.30, 1.09),
                                     (0.37, 0.82), (0.23, 0.64), (0.41, 0.70),
                                     (0.29, 0.30), (0.16, 0.10), (0.29, 0.15), (0.02, -0.46)],
              0.055, feather)
    art.stroke("Bone rachis", [(0, -1.11, 0.08), (0, 0.10, 0.08), (0.025, 1.24, 0.08)], 0.032, bone)
    for index, height in enumerate((-0.15, 0.12, 0.39, 0.66, 0.9)):
        width = 0.3 if index < 4 else 0.19
        for side in (-1, 1):
            art.stroke("Skeletal feather rib", [(0, height, 0.09), (side * width, height + 0.20, 0.07)],
                       0.016, bone, False)
    art.sphere("Tiny skull ferrule", (0, -0.55, 0.08), (0.19, 0.20, 0.13), bone)
    for side in (-1, 1):
        art.sphere("Skull eye socket", (side * 0.077, -0.53, 0.193), (0.05, 0.065, 0.025), hollow)
    art.box("Bone jaw", (0, -0.72, 0.09), (0.20, 0.10, 0.13), bone, 0.02)
    art.plate("Quill nib", [(-0.07, -0.85), (0, -1.19), (0.07, -0.85), (0, -0.78)], 0.055, gold)
    art.stroke("Green ink wisp", [(0.08, -1.17, 0), (0.41, -0.98, 0), (0.47, -0.65, 0),
                                  (0.36, -0.44, 0)], 0.027, green)
    art.spark("Ink mote", (0.49, -0.22, 0), 0.09, green)
    return art.collection