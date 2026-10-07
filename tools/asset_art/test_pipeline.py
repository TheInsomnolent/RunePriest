import unittest
import tempfile
import os
from pathlib import Path
from unittest.mock import patch

from PIL import Image, ImageChops, ImageDraw, ImageStat

from pipeline import load_profile, validate_profile
from export import color_icon, export_images
from render import publish, save_review
from review_relics import load_icons


class ProfileTests(unittest.TestCase):
    def test_event_scene_and_vignette_controls(self):
        profile = load_profile("event_scene", "enchanted_forge")
        self.assertEqual(profile["size"], [1280, 720])
        self.assertFalse(profile["transparent"])
        self.assertEqual(profile["framing"], "manual")
        self.assertLess(profile["postprocess"]["vignette_x"], 0.5)
        for key, value in (("vignette_strength", 1.1), ("vignette_x", -0.1),
                           ("vignette_y", float("nan")), ("vignette_width", 0),
                           ("vignette_height", True)):
            with self.subTest(key=key):
                invalid = load_profile("event_scene", "enchanted_forge")
                invalid["postprocess"][key] = value
                with self.assertRaises(ValueError):
                    validate_profile(invalid, "enchanted_forge")

    def test_shared_icon_postprocess(self):
        relic = load_profile("relic", "calibration")
        for name in ("potion", "map_pin"):
            profile = load_profile(name, "calibration")
            self.assertEqual(profile["postprocess"], relic["postprocess"])
            profile["postprocess"]["strength"] = 0
            self.assertNotEqual(load_profile(name, "calibration")["postprocess"]["strength"], 0)

    def test_postprocess_reference_rejection(self):
        with tempfile.TemporaryDirectory() as directory:
            settings = Path(directory) / "settings.json"
            for reference in ("missing", "relic"):
                settings.write_text('{"postprocess":"' + reference + '"}')
                with self.assertRaises(ValueError):
                    load_profile("relic", "calibration", settings)

    def test_tonal_controls(self):
        profile = load_profile("relic", "calibration")
        for settings in ({"tone_steps": 1}, {"tone_steps": 33}, {"tone_steps": 8.5},
                         {"tone_steps": True}, {"tone_strength": -0.1}, {"tone_strength": 1.1},
                         {"tone_strength": float("nan")}, {"tone_strength": True}):
            with self.subTest(settings=settings):
                profile["postprocess"] = settings
                with self.assertRaises(ValueError):
                    validate_profile(profile, "calibration")
        for settings in ({"tone_steps": 2, "tone_strength": 0}, {"tone_steps": 32, "tone_strength": 1}):
            profile["postprocess"] = settings
            self.assertIs(validate_profile(profile, "calibration"), profile)

    def test_postprocess_settings(self):
        for settings in (None, {"enabled": 1}, {"radius": 0}, {"radius": 17},
                         {"strength": 1.1}, {"strength": True}, {"strength": float("nan")},
                         {"pigment_strength": -1}, {"pigment_scale": 0}, {"strenght": 0.5},
                         {"brush_strength": -0.1}, {"brush_strength": 1.1}, {"brush_strength": True},
                         {"brush_width": 0}, {"brush_width": float("inf")}, {"brush_width": 33},
                         {"brush_length": 0}, {"brush_length": 33}, {"brush_angle": 181},
                         {"brush_angle": -181}, {"brush_angle": "diagonal"}):
            with self.subTest(settings=settings):
                profile = load_profile("relic", "calibration")
                profile["postprocess"] = settings
                with self.assertRaises(ValueError):
                    validate_profile(profile, "calibration")
        profile = load_profile("relic", "calibration")
        profile["postprocess"] = {"enabled": False, "strength": 0, "pigment_strength": 0}
        self.assertIs(validate_profile(profile, "calibration"), profile)
        del profile["postprocess"]
        self.assertIs(validate_profile(profile, "calibration"), profile)

    def test_brush_control_boundaries(self):
        profile = load_profile("relic", "calibration")
        for strength, width, length, angle in ((0, 1, 1, -180), (1, 32, 32, 180)):
            profile["postprocess"].update(brush_strength=strength, brush_width=width,
                                          brush_length=length, brush_angle=angle)
            self.assertIs(validate_profile(profile, "calibration"), profile)

    def test_builtin_dimensions(self):
        for name, size in (("relic", [256, 256]), ("potion", [256, 256]), ("map_pin", [49, 64])):
            with self.subTest(name=name):
                self.assertEqual(load_profile(name, "calibration")["size"], size)

    def test_custom_scene(self):
        profile = {
            "size": [1200, 500], "supersample": 1, "transparent": False,
            "camera": "PERSP", "framing": "manual",
            "outputs": [{"name": "background", "size": [1200, 500], "path": "{slug}.png"}],
        }
        self.assertIs(validate_profile(profile, "event_scene"), profile)

    def test_bad_paths_and_duplicates(self):
        for path in ("../escape.png", "/escape.png", "C:/escape.png", "folder\\escape.png", "{unknown}.png"):
            with self.subTest(path=path):
                profile = load_profile("relic", "calibration")
                profile["outputs"][0]["path"] = path
                with self.assertRaises(ValueError):
                    validate_profile(profile, "calibration")

    def test_aspect_ratio_and_perspective_guard(self):
        profile = load_profile("map_pin", "calibration")
        profile["outputs"][0]["size"] = [64, 64]
        with self.assertRaises(ValueError):
            validate_profile(profile, "calibration")
        profile = load_profile("relic", "calibration")
        profile["camera"] = "PERSP"
        with self.assertRaises(ValueError):
            validate_profile(profile, "calibration")

    def test_invalid_studio_settings(self):
        for key, value in (("padding", "wide"), ("lens", 0), ("world_color", "#ffffff"),
                           ("camera_position", [1, 2]), ("world_strength", float("nan")),
                           ("lights", [{"name": "Key", "position": [1, 2, 3], "energy": -1}])):
            with self.subTest(key=key):
                profile = load_profile("relic", "calibration")
                profile[key] = value
                with self.assertRaises(ValueError):
                    validate_profile(profile, "calibration")

    def test_reserved_and_normalized_duplicate_paths(self):
        profile = load_profile("relic", "calibration")
        profile["outputs"][0]["path"] = "preview.png"
        with self.assertRaises(ValueError):
            validate_profile(profile, "calibration")
        profile["outputs"][0]["path"] = "./calibration.png"
        with self.assertRaises(ValueError):
            validate_profile(profile, "calibration")

    def test_custom_dimensions_override(self):
        with tempfile.TemporaryDirectory() as directory:
            settings = Path(directory) / "settings.json"
            settings.write_text('{"size":[800,300],"outputs":[{"name":"view","size":[800,300],"path":"{slug}.png"}]}')
            self.assertEqual(load_profile("scene", "custom_scene", settings)["size"], [800, 300])


@unittest.skipUnless(os.environ.get("ASSET_ART_CHECK_EVENT"), "Requires the event_iteration forge render")
class EventRenderTests(unittest.TestCase):
    def test_text_safe_composition(self):
        image_path = Path(__file__).resolve().parent / "build/event_iteration/enchanted_forge/event_scene/exports/enchanted_forge.png"
        with Image.open(image_path) as image:
            self.assertEqual(image.size, (1280, 720))
            self.assertEqual(image.getchannel("A").getextrema(), (255, 255))
            subject = image.crop((100, 100, 650, 680)).convert("L")
            text_area = image.crop((710, 120, 1180, 580)).convert("L")
            self.assertGreater(ImageStat.Stat(subject).mean[0], 20)
            self.assertGreater(subject.getextrema()[1], 180)
            self.assertLess(ImageStat.Stat(text_area).mean[0], 15)
            self.assertLess(text_area.getextrema()[1], 60)


class PublishTests(unittest.TestCase):
    def test_review_saved_outside_build_and_refreshed(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            exports = root / "build/exports"
            exports.mkdir(parents=True)
            source = exports / "preview.png"
            Image.new("RGB", (12, 12), "red").save(source)
            target = save_review(exports, "relic", "event_horizon", root / "reviews")
            self.assertEqual(target, root / "reviews/relic/event_horizon.png")
            self.assertEqual(target.read_bytes(), source.read_bytes())
            Image.new("RGB", (12, 12), "blue").save(source)
            save_review(exports, "relic", "event_horizon", root / "reviews")
            self.assertEqual(target.read_bytes(), source.read_bytes())

    def test_review_path_cannot_escape(self):
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaises(ValueError):
                save_review(Path(directory), "../../outside", "test", Path(directory) / "reviews")

    def test_overwrite_is_explicit_and_preflighted(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            exports = root / "exports"
            target = root / "published"
            exports.mkdir()
            target.mkdir()
            (exports / "first.png").write_bytes(b"new first")
            (exports / "second.png").write_bytes(b"new second")
            (target / "second.png").write_bytes(b"existing")
            report = {"outputs": [{"path": "first.png"}, {"path": "second.png"}]}
            with self.assertRaises(ValueError):
                publish(exports, target, report, False)
            self.assertFalse((target / "first.png").exists())
            self.assertEqual((target / "second.png").read_bytes(), b"existing")
            publish(exports, target, report, True)
            self.assertEqual((target / "second.png").read_bytes(), b"new second")

    def test_destination_escape_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with self.assertRaises(ValueError):
                publish(root, root / "published", {"outputs": [{"path": "../escape.png"}]}, True)


class ExportTests(unittest.TestCase):
    def test_potion_review_without_local_build(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            asset_root = root / "tools/asset_art"
            source = Image.new("RGBA", (1024, 1024))
            ImageDraw.Draw(source).ellipse((280, 200, 744, 824), fill=(70, 140, 220, 255))
            source.save(root / "master.png")
            installed = root / "RunePriest/images/potions"
            export_images(root / "master.png", load_profile("potion", "test"), "test", installed)
            with patch("review_relics.ROOT", asset_root):
                images = load_icons("test", installed=True, profile="potion")
            self.assertEqual([image.size for image in images], [(256, 256), (64, 64), (256, 256)])
            self.assertFalse(ImageChops.subtract(images[0].getchannel("A"), images[2].getchannel("A")).getbbox())
            self.assertFalse((installed / "small/test.png").exists())
            for image in images:
                image.close()

    def test_installed_review_without_local_build(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            asset_root = root / "tools/asset_art"
            source = Image.new("RGBA", (1024, 1024))
            ImageDraw.Draw(source).ellipse((280, 200, 744, 824), fill=(220, 140, 40, 255))
            source.save(root / "master.png")
            installed = root / "RunePriest/images/relics"
            export_images(root / "master.png", load_profile("relic", "test"), "test", installed)
            with patch("review_relics.ROOT", asset_root):
                images = load_icons("test", installed=True)
            self.assertEqual([image.size for image in images], [(256, 256), (94, 94), (94, 94)])
            for image in images:
                image.close()

    def test_relic_outputs_and_outline_alignment(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = Image.new("RGBA", (1024, 1024))
            ImageDraw.Draw(source).ellipse((280, 200, 744, 824), fill=(220, 140, 40, 255))
            source.save(root / "master.png")
            report = export_images(root / "master.png", load_profile("relic", "test"), "test", root / "exports")
            self.assertEqual(len(report["outputs"]), 3)
            with Image.open(root / "exports/test.png") as small, Image.open(root / "exports/test_outline.png") as outline:
                self.assertEqual(small.size, (94, 94))
                self.assertEqual(outline.size, small.size)
                for icon_alpha, outline_alpha in zip(small.getchannel("A").getdata(), outline.getchannel("A").getdata()):
                    self.assertGreaterEqual(outline_alpha, icon_alpha)
            self.assertTrue((root / "exports/preview.png").is_file())

    def test_transparent_rgb_does_not_bleed(self):
        source = Image.new("RGBA", (100, 100), (255, 0, 0, 0))
        ImageDraw.Draw(source).rectangle((25, 25, 75, 75), fill=(0, 255, 0, 255))
        result = color_icon(source, (25, 25), 0)
        self.assertTrue(all(red == 0 for red, green, blue, alpha in result.getdata() if alpha))

    def test_non_square_opaque_export(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            Image.new("RGBA", (120, 50), (80, 100, 150, 255)).save(root / "master.png")
            profile = {"size": [120, 50], "transparent": False,
                       "outputs": [{"name": "scene", "size": [120, 50], "path": "scene.png"}]}
            export_images(root / "master.png", profile, "scene", root / "exports")
            with Image.open(root / "exports/scene.png") as result:
                self.assertEqual(result.size, (120, 50))

    def test_empty_opaque_and_clipped_transparent_renders_fail(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for color in ((0, 0, 0, 0), (255, 255, 255, 255)):
                Image.new("RGBA", (1024, 1024), color).save(root / "master.png")
                with self.assertRaises(ValueError):
                    export_images(root / "master.png", load_profile("relic", "test"), "test", root / "exports")
            source = Image.new("RGBA", (1024, 1024))
            ImageDraw.Draw(source).rectangle((0, 200, 700, 800), fill="white")
            source.save(root / "master.png")
            with self.assertRaises(ValueError):
                export_images(root / "master.png", load_profile("relic", "test"), "test", root / "exports")


@unittest.skipUnless(os.environ.get("ASSET_ART_COMPARE_RENDERS"), "Requires baseline and painterly Blender renders")
class CompositorRenderTests(unittest.TestCase):
    @unittest.skipUnless(os.environ.get("ASSET_ART_COMPARE_TONES"), "Requires tonal and tonal-bypass renders")
    def test_tonal_render_and_bypass(self):
        root = Path(__file__).resolve().parent / "build"
        for relative in ("master.png", "exports/big/calibration_sigil.png", "exports/calibration_sigil.png",
                         "exports/calibration_sigil_outline.png"):
            with self.subTest(image=relative):
                with Image.open(root / "brushstrokes/calibration_sigil/relic" / relative) as before, \
                        Image.open(root / "tonal_palette/calibration_sigil/relic" / relative) as toned, \
                        Image.open(root / "tone_disabled/calibration_sigil/relic" / relative) as disabled:
                    self.assertEqual(before.size, toned.size)
                    self.assertEqual(before.tobytes(), disabled.tobytes())
                    self.assertEqual(before.getchannel("A").tobytes(), toned.getchannel("A").tobytes())
                    if "outline" in relative:
                        self.assertEqual(before.tobytes(), toned.tobytes())
                    else:
                        self.assertIsNotNone(ImageChops.difference(before.convert("RGB"), toned.convert("RGB")).getbbox())

    @unittest.skipUnless(os.environ.get("ASSET_ART_COMPARE_BRUSH"), "Requires brush-enabled and brush-disabled renders")
    def test_brush_render_and_bypass(self):
        root = Path(__file__).resolve().parent / "build"
        for relative in ("master.png", "exports/big/calibration_sigil.png", "exports/calibration_sigil.png",
                         "exports/calibration_sigil_outline.png"):
            with self.subTest(image=relative):
                with Image.open(root / "painterly/calibration_sigil/relic" / relative) as before, \
                        Image.open(root / "brushstrokes/calibration_sigil/relic" / relative) as brushed, \
                        Image.open(root / "brush_disabled/calibration_sigil/relic" / relative) as disabled:
                    self.assertEqual(before.size, brushed.size)
                    self.assertEqual(before.tobytes(), disabled.tobytes())
                    self.assertIsNone(ImageChops.difference(before.getchannel("A"), brushed.getchannel("A")).getbbox())
                    if "outline" in relative:
                        self.assertEqual(before.tobytes(), brushed.tobytes())
                    else:
                        self.assertIsNotNone(ImageChops.difference(before.convert("RGB"), brushed.convert("RGB")).getbbox())

    def test_saved_render_alpha_and_color(self):
        root = Path(__file__).resolve().parent / "build"
        before_root = root / "calibration_sigil/relic"
        after_root = root / "painterly/calibration_sigil/relic"
        for relative in ("master.png", "exports/big/calibration_sigil.png", "exports/calibration_sigil.png"):
            with self.subTest(image=relative), Image.open(before_root / relative) as before, Image.open(after_root / relative) as after:
                self.assertEqual(before.size, after.size)
                self.assertIsNone(ImageChops.difference(before.getchannel("A"), after.getchannel("A")).getbbox())
                difference = ImageChops.difference(before.convert("RGB"), after.convert("RGB"))
                self.assertIsNotNone(difference.getbbox())
        with Image.open(before_root / "exports/calibration_sigil_outline.png") as before, Image.open(after_root / "exports/calibration_sigil_outline.png") as after:
            self.assertIsNone(ImageChops.difference(before, after).getbbox())


if __name__ == "__main__":
    unittest.main()