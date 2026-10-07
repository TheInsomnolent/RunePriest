# Procedural asset art

Blender builds and renders the entire scene from Python. A separate Python environment
uses Pillow to resize the master, add pixel-sized borders, generate outline masks, and
produce review sheets. No Blender add-ons, Godot editor, or image-generation service is required.

The included `calibration_sigil` is a placeholder for tuning the rendering style, not a
finished Rune Priest relic. No game assets are replaced by rendering it.

## Quick start

Run these PowerShell commands from the repository root. Tested with Blender **5.2.2 LTS**
and Python **3.11.9**. Use the same Blender version for consistent results across machines.

```powershell
py -3.11 -m venv tools/asset_art/.venv
./tools/asset_art/.venv/Scripts/python.exe -m pip install -r tools/asset_art/requirements.txt
./tools/asset_art/.venv/Scripts/python.exe tools/asset_art/render.py calibration_sigil
```

The environment is already created on the development machine. Blender is found using
`--blender`, then `BLENDER_PATH`, then PATH, then standard Windows installation folders.
For explicit selection:

```powershell
./tools/asset_art/.venv/Scripts/python.exe tools/asset_art/render.py calibration_sigil --blender "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe"
```

Look in `build/calibration_sigil/relic/` for:

- `scene.blend`: editable complete scene, including materials, camera, and lights.
- `master.png`: supersampled RGBA render before export borders.
- `profile.json`: resolved settings used for the render.
- `render.json`: Blender version, renderer, recipe path, and camera scale.
- `exports/`: final PNGs, `manifest.json` with sizes/alpha bounds, and `preview.png`.

The review sheet displays outputs at actual pixel size over light, dark, and checkerboard
backgrounds. `build/`, virtual environments, and Python caches are ignored by Git.

## Profiles and custom sizes

| Profile | Base size | Outputs | Camera |
| --- | --- | --- | --- |
| `relic` | 256 x 256 | 256 icon, 94 icon, 94 white outline | Auto orthographic |
| `potion` | 256 x 256 | 256 icon, 256 white outline | Auto orthographic |
| `map_pin` | 49 x 64 | 49 x 64 icon | Auto orthographic |
| `event_scene` | 1280 x 720 | Opaque left-composed event draft | Manual perspective |
| `scene` | 1200 x 500 | Opaque background | Manual perspective |

Relic sizes match this repository's template assets. Potion/map sizes follow the existing
asset conventions; confirm framing and appearance in-game before publishing. **The scene
size is illustrative**, not a verified event-background requirement.

Potion and map-pin profiles reference the relic's postprocess object, so icon tuning remains
shared. Event scenes have their own stronger settings; changing them does not alter icons.

## Enchanted Forge draft

The event composition follows the supplied widescreen screenshots: anvil and magical forge
on the left, restrained near-black space behind the menu on the right. The recipe uses stepped
matte materials, irregular stone planes, red kanji, and a focused glow rather than a realistic
workshop render. The 1280 x 720 size is a **16:9 review draft**, not a verified native asset
size or proof that the existing event portrait loader accepts a full-screen background.
Nothing is published or installed automatically.

```powershell
./tools/asset_art/.venv/Scripts/python.exe tools/asset_art/render.py enchanted_forge --profile event_scene --build-dir tools/asset_art/build/event_iteration
```

The result is `build/event_iteration/enchanted_forge/event_scene/exports/enchanted_forge.png`.
The complete editable scene is beside the master image. Kanji use the installed Yu Gothic
font; set `ASSET_ART_KANJI_FONT` to another Japanese font path if needed.

Event-only postprocess settings:

- `glow_strength`: emission bloom (0-2), default 0.
- `vignette_strength`: fade outside the focal area (0-1), default 0, so icons are unchanged.
- `vignette_x`, `vignette_y`: normalized focal center (0-1), with Y measured from the bottom.
- `vignette_width`, `vignette_height`: elliptical falloff radii in canvas fractions (0.1-2).

The vignette runs after texture and bloom to keep the menu area dark, then the original alpha
is restored. The draft deliberately uses a leftward focal center. Increase the radii to reveal
more surroundings. Set `ASSET_ART_CHECK_EVENT=1` when running tests after rendering this draft
to check opacity, subject visibility, and low brightness in the text-safe area. This does not
replace artistic inspection or eventual in-game framing verification.

## Custom settings

Profiles describe outputs independently of recipes. Render the same model through another
profile using `--profile potion`, `--profile map_pin`, or `--profile scene`.

Use `--settings path/to/settings.json` for per-asset overrides. Overrides replace top-level
fields; `outputs` and `lights` replace their entire lists. For example:

```json
{
  "size": [1600, 900],
  "supersample": 1,
  "camera_position": [5, -9, 5],
  "camera_target": [0, 0, 1],
  "lens": 45,
  "world_color": "394653",
  "outputs": [
    {"name": "background", "size": [1600, 900], "path": "{slug}.png"}
  ]
}
```

Apply that example to `--profile scene`. For a transparent custom-size icon, start with
`relic` or `map_pin` and replace both `size` and `outputs`. All outputs must preserve the
master aspect ratio; a different crop requires a separate render configuration.

Supported studio settings:

- `camera`: `ORTHO` or `PERSP`; `framing`: `auto` or `manual` (perspective requires manual).
- `camera_position`, `camera_target`, `lens`, and `ortho_scale` for manual composition.
- `padding`: per-side canvas fraction for auto framing, default 0.12.
- `transparent`, `world_color` (six-digit RGB), and `world_strength`.
- `lights`: area lights with `name`, `position`, `energy`, optional `target`, `size`, and `color`.
- `seed`: Python random seed for procedural recipes. Recipes should use seeded randomness.
- `border`: dark silhouette width in **each output's final pixels**, not world units.
- `supersample`: render multiplier, 1 through 8, bounded to a 16384-pixel dimension.
- Output `kind`: `color` or `outline`; outline `radius` is additional dilation in final pixels.

An outline is an expanded white silhouette behind the small icon, not a hollow ring. It
includes the color icon's dark border so the two remain aligned. Keep enough padding for
both. Opaque profiles do not receive silhouette borders and cannot request outline masks.

## Painterly compositor

Relic renders enable a Blender 5.2 compositor node group called **Painterly Finish**.
The recipe, geometry, lighting, and material shaders are unchanged. Open the generated
scene in Blender's Compositing workspace to inspect the labeled, editable graph.

The graph unpremultiplies the render, extends color under transparent edges, adds mild
procedural pigment variation, applies anisotropic Kuwahara filtering, optional tonal banding,
and directional brush texture, blends with the
original, restores the original alpha, and premultiplies for output. Export borders and
white outlines are still generated afterwards, so they remain crisp and aligned.

Use a settings override to tune or disable it:

```json
{
  "postprocess": {
    "enabled": true,
    "radius": 3.0,
    "strength": 0.75,
    "pigment_strength": 0.35,
    "pigment_scale": 45.0,
    "brush_strength": 0.3,
    "brush_width": 2.5,
    "brush_length": 8.0,
    "brush_angle": 25.0,
    "tone_steps": 8,
    "tone_strength": 0.8
  }
}
```

- `radius`: filter radius in base-profile pixels (0.1-16); scaled by supersampling.
- `strength`: original/processed blend (0-1); zero bypasses all color changes.
- `pigment_strength`: tonal variation before filtering (0-1); zero disables pigment variation.
- `pigment_scale`: image-space noise frequency (1-500); higher means smaller patches.
- `brush_strength`: directional texture after filtering (0-1); zero disables it. Default 0,
  explicitly set to 0.3 in the relic profile. The overall `strength` also affects this pass.
- `brush_width`: approximate noise feature spacing in base-profile pixels (1-32), default 2.5.
- `brush_length`: elongation ratio (1-32), default 8; higher values produce longer streaks.
- `brush_angle`: image-space rotation in degrees (-180 to 180), default 25; zero is horizontal.
- `tone_steps`: brightness bands (integer 2-32), default 8; fewer produces broader, flatter tones.
- `tone_strength`: blend between original and banded values (0-1), default 0; relic profile
  uses 0.8. Zero bypasses banding without changing pigment or brush settings.
- `enabled: false`: bypass the compositor. Profiles without this section remain unchanged.

`postprocess` overrides replace the complete settings object, with omitted controls using
the defaults above (brush and tone strength default to zero, not the example's values).
Changing these controls requires rendering, not `--export-only`.
Noise is deterministic and image-space: it is appropriate for still icons, but does not
follow material boundaries or create geometry-aware scratches and edge wear.

Tonal simplification separates HSV, quantizes Value using an approximate gamma-2.2 perceptual
spacing, and recombines it with the original hue and saturation. It acts after Kuwahara and
before brush texture, so the broad forms simplify without quantizing away the strokes. The
global postprocess `strength` also blends this effect with the unprocessed render.
This is **not** an indexed eight-color palette or a restriction of the color gamut: partial
blending, hue/saturation variation, brush texture, and antialiasing still create intermediate
colors. The intent is fewer dominant tones, like the broad painted shapes in the references,
not a claim that the original game art uses a fixed palette.

The tone comparison is saved separately in `build/tonal_palette/calibration_sigil/relic`.
Render it using `--build-dir tools/asset_art/build/tonal_palette`. The prior brush-only
profile is saved with its render, so the tonal bypass can be checked with:

```powershell
./tools/asset_art/.venv/Scripts/python.exe tools/asset_art/render.py calibration_sigil --settings tools/asset_art/build/brushstrokes/calibration_sigil/relic/profile.json --build-dir tools/asset_art/build/tone_disabled
```

Set `ASSET_ART_COMPARE_RENDERS=1` and `ASSET_ART_COMPARE_TONES=1` to test the saved tone
comparison: color changes, alpha and outline do not, and omitted tonal controls reproduce
the brush-only render pixel-for-pixel. Preserve the saved comparison profiles when tuning.

The initial comparison keeps the unprocessed render in `build/calibration_sigil/relic`
and the pigment-only render in `build/painterly/calibration_sigil/relic`. The brush version
uses a separate directory to preserve both earlier renders:

```powershell
./tools/asset_art/.venv/Scripts/python.exe tools/asset_art/render.py calibration_sigil --build-dir tools/asset_art/build/brushstrokes
```

Set `ASSET_ART_COMPARE_RENDERS=1` when running the tests to compare those existing renders:
color must change while the master/export alpha and outline stay identical. This optional
integration check needs both renders from the same recipe/camera; unit tests do not need Blender.

The brush texture is deterministic stretched noise, not surface-aware paint strokes. It is
added after smoothing so fine bristle detail survives, before restoring alpha so it never
alters the silhouette. Geometry, lighting, and shaders are unchanged. A uniform image-space
direction is intentional for this initial still-image treatment.

To verify the bypass against the saved pigment-only settings:

```powershell
./tools/asset_art/.venv/Scripts/python.exe tools/asset_art/render.py calibration_sigil --settings tools/asset_art/build/painterly/calibration_sigil/relic/profile.json --build-dir tools/asset_art/build/brush_disabled
```

Set both `ASSET_ART_COMPARE_RENDERS=1` and `ASSET_ART_COMPARE_BRUSH=1` when running tests
after generating those renders. Checks require changed color with unchanged alpha/outline,
and pixel-identical output when brush settings are omitted (zero-strength default).

## Adding a recipe

Create `recipes/<slug>.py` with `build(scene, profile)`. The function constructs its asset
and returns the Blender collection containing the renderable objects. See
`recipes/calibration_sigil.py` for a complete example. A different location can be supplied
with `--recipe`. Recipes are trusted executable Python, not sandboxed data.

The runner configures the studio before calling `build`. Recipes can use the full `bpy`
API, load artist-authored meshes, add scenery, create their own materials, and modify the
camera or lighting. With `framing: manual`, the runner leaves the recipe's composition alone.
For auto framing, evaluated mesh vertices are fitted in camera space; realize procedural
instances first. Use manual framing for volumes, complex instancing, or authored scenes.

`studio.toon_material(name, shadow, base, highlight)` provides the shared Eevee shading.
It uses diffuse lighting through Shader to RGB and a color ramp, with Standard color
management. This shader is Eevee-specific. The initial look prioritizes readable shapes;
more painterly highlights or asymmetry belong in the recipe and material palette.

Prefer Python as the source of truth. The saved `.blend` is useful for inspection and
experimentation, but rerunning the recipe rebuilds it and **discards manual scene edits**.
Move approved changes back into the recipe, or deliberately load an authored source model.

## Export and publish

Re-export after adjusting only `border` or `outputs`, without starting Blender:

```powershell
./tools/asset_art/.venv/Scripts/python.exe tools/asset_art/render.py calibration_sigil --export-only
```

Use the same `--settings` as the original render when applicable. Changes to render settings
require rerendering. `--build-dir` selects a separate output root for variations; the default
location is overwritten when the same slug/profile is rendered again.

Only `--publish <directory>` copies final PNGs into a destination. It never copies the
preview, master, scene, or manifests. Existing PNGs are refused unless `--force` is also
supplied. Review before publishing; rendering by itself never installs anything.

For an approved relic, the publish destination is `RunePriest/images/relics`. Its slug must
match the lower-case model entry without the mod prefix, as used by `RunePriestRelic`.
Potion destinations are `RunePriest/images/potions`. Unique assets need their actual loader
path configured separately. After publishing approved assets, use the repository's normal
`dotnet build RunePriest.csproj` to package and install them.

## Validation

```powershell
./tools/asset_art/.venv/Scripts/python.exe -m unittest discover -s tools/asset_art -p test_pipeline.py -v
```

Tests cover profile validation, custom dimensions, alpha-aware resampling, outline alignment,
empty/clipped renders, and publish overwrite/path protection. Blender renders are a separate
integration check: run the calibration recipe through the relevant profiles, then inspect
the review sheets. The exporter validates canvas dimensions and alpha coverage, but visual
quality, intended framing, and in-game appearance still require review.