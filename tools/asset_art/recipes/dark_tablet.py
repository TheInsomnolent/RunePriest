from relic_shapes import RelicShapes


def build(scene, profile):
    art = RelicShapes(scene, "DarkTablet", tilt=(8, -18, 9))
    stone = art.material("Obsidian tablet", "161D28", "374855", "7B8B94")
    inset = art.material("Recessed inscription field", "101923", "24323F", "455963")
    red = art.glow("Blood inscription", "FF6976", 1.15)
    mint = art.glow("Mend inscription", "83EDD0", 1.1)
    trim = art.material("Ancient silver inlay", "425B60", "93AAA1", "DCE0BF")
    art.plate("Chipped dark slab", [(-0.72, -0.93), (0.52, -0.99), (0.75, -0.73), (0.70, 0.58),
                                   (0.46, 0.93), (-0.33, 1.00), (-0.69, 0.68), (-0.77, -0.23),
                                   (-0.64, -0.42)], 0.31, stone, bevel=0.045)
    art.plate("Inset tablet face", [(-0.54, -0.76), (0.53, -0.76), (0.53, 0.52), (0.29, 0.73),
                                   (-0.28, 0.78), (-0.54, 0.54)], 0.03, inset, (0, 0, 0.168))
    art.stroke("Tablet dividing inlay", [(0, -0.67, 0.20), (0, 0.48, 0.20)], 0.018, trim, False)
    for index, height in enumerate((0.38, -0.06, -0.50)):
        art.rune(f"Blood rune {index}", (-0.28, height, 0.21), 0.33, red)
        art.rune(f"Inverted mend rune {index}", (0.28, height, 0.21), 0.33, mint, mirrored=True)
    art.stroke("Broken tablet corner", [(-0.69, -0.30, 0.17), (-0.44, -0.39, 0.20),
                                       (-0.50, -0.61, 0.20)], 0.024, stone, False)
    return art.collection