from __future__ import annotations

from math import radians

import bpy


def configure_compositor(scene, profile: dict) -> None:
    settings = profile.get("postprocess", {})
    if not settings.get("enabled", False):
        return
    tree = bpy.data.node_groups.new("Painterly Finish", "CompositorNodeTree")
    tree.interface.new_socket(name="Image", in_out="OUTPUT", socket_type="NodeSocketColor")
    scene.compositing_node_group = tree
    scene.render.use_compositing = True
    nodes, links = tree.nodes, tree.links

    def node(kind, label, horizontal, vertical=0):
        result = nodes.new(kind)
        result.label = label
        result.location = (horizontal, vertical)
        result.width = 200
        return result

    source = node("CompositorNodeRLayers", "Unprocessed render", 0)
    straight = node("CompositorNodePremulKey", "Unpremultiply for color filtering", 240)
    straight.inputs["Type"].default_value = "To Straight"
    links.new(source.outputs["Image"], straight.inputs["Image"])
    extension = node("CompositorNodeInpaint", "Extend color beneath transparent edges", 480)
    radius = settings.get("radius", 3.0) * profile.get("supersample", 1)
    extension.inputs["Size"].default_value = max(1, round(radius * 2))
    links.new(straight.outputs["Image"], extension.inputs["Image"])
    paint = node("CompositorNodeKuwahara", "Directional paint simplification", 720)
    paint.inputs["Size"].default_value = radius
    coordinates = node("CompositorNodeImageCoordinates", "Resolution-independent pigment coordinates", 0, -400)
    links.new(source.outputs["Image"], coordinates.inputs["Image"])
    noise = node("ShaderNodeTexNoise", "Broad pigment variation", 240, -400)
    noise.noise_dimensions = "3D"
    noise.inputs["Scale"].default_value = settings.get("pigment_scale", 45.0)
    noise.inputs["Detail"].default_value = 2.0
    links.new(coordinates.outputs["Uniform"], noise.inputs["Vector"])
    tonal_range = node("ShaderNodeMapRange", "Pigment density range", 480, -400)
    tonal_range.inputs["To Min"].default_value = 0.65
    tonal_range.inputs["To Max"].default_value = 1.35
    links.new(noise.outputs["Fac"], tonal_range.inputs["Value"])
    pigment = node("ShaderNodeMix", "Pigment strength", 720, -400)
    pigment.data_type = "RGBA"
    pigment.blend_type = "MULTIPLY"
    pigment.inputs[0].default_value = settings.get("pigment_strength", 0.35)
    links.new(extension.outputs["Image"], pigment.inputs[6])
    links.new(tonal_range.outputs["Result"], pigment.inputs[7])
    links.new(pigment.outputs[2], paint.inputs["Image"])
    finish = paint.outputs["Image"]
    if settings.get("tone_strength", 0) > 0:
        separate = node("CompositorNodeSeparateColor", "Preserve hue and saturation", 960, 420)
        separate.mode = "HSV"
        links.new(finish, separate.inputs["Image"])
        perceptual = node("ShaderNodeMath", "Approximate perceptual brightness", 1200, 420)
        perceptual.operation = "POWER"
        perceptual.inputs[1].default_value = 1 / 2.2
        links.new(separate.outputs[2], perceptual.inputs[0])
        scale = node("ShaderNodeMath", "Number of tonal intervals", 1440, 420)
        scale.operation = "MULTIPLY"
        intervals = settings.get("tone_steps", 8) - 1
        scale.inputs[1].default_value = intervals
        links.new(perceptual.outputs[0], scale.inputs[0])
        bands = node("ShaderNodeMath", "Group into painted value bands", 1680, 420)
        bands.operation = "ROUND"
        links.new(scale.outputs[0], bands.inputs[0])
        normalize = node("ShaderNodeMath", "Normalize tonal bands", 1920, 420)
        normalize.operation = "DIVIDE"
        normalize.inputs[1].default_value = intervals
        links.new(bands.outputs[0], normalize.inputs[0])
        linear = node("ShaderNodeMath", "Return brightness to scene linear", 2160, 420)
        linear.operation = "POWER"
        linear.inputs[1].default_value = 2.2
        links.new(normalize.outputs[0], linear.inputs[0])
        combine = node("CompositorNodeCombineColor", "Original hues with simpler values", 2400, 420)
        combine.mode = "HSV"
        links.new(separate.outputs[0], combine.inputs[0])
        links.new(separate.outputs[1], combine.inputs[1])
        links.new(linear.outputs[0], combine.inputs[2])
        tones = node("ShaderNodeMix", "Tonal simplification strength", 2640, 420)
        tones.data_type = "RGBA"
        tones.inputs[0].default_value = settings["tone_strength"]
        links.new(finish, tones.inputs[6])
        links.new(combine.outputs["Image"], tones.inputs[7])
        finish = tones.outputs[2]
    if settings.get("brush_strength", 0) > 0:
        direction = node("ShaderNodeVectorRotate", "Brush direction", 0, -800)
        direction.rotation_type = "Z_AXIS"
        direction.inputs["Angle"].default_value = radians(-settings.get("brush_angle", 25.0))
        links.new(coordinates.outputs["Uniform"], direction.inputs["Vector"])
        stretch = node("ShaderNodeVectorMath", "Elongated brush marks", 240, -800)
        stretch.operation = "MULTIPLY"
        frequency = min(profile["size"]) / settings.get("brush_width", 2.5)
        stretch.inputs[1].default_value = (frequency / settings.get("brush_length", 8.0), frequency, 1)
        links.new(direction.outputs["Vector"], stretch.inputs[0])
        strokes = node("ShaderNodeTexNoise", "Broken bristle texture", 480, -800)
        strokes.noise_dimensions = "3D"
        strokes.inputs["Scale"].default_value = 1.0
        strokes.inputs["Detail"].default_value = 1.5
        strokes.inputs["Roughness"].default_value = 0.6
        strokes.inputs["Distortion"].default_value = 0.3
        links.new(stretch.outputs["Vector"], strokes.inputs["Vector"])
        density = node("ShaderNodeMapRange", "Subtle brush density", 720, -800)
        density.inputs["To Min"].default_value = 0.4
        density.inputs["To Max"].default_value = 1.6
        links.new(strokes.outputs["Fac"], density.inputs["Value"])
        brush = node("ShaderNodeMix", "Brush texture strength", 960, -800)
        brush.data_type = "RGBA"
        brush.blend_type = "MULTIPLY"
        brush.inputs[0].default_value = settings["brush_strength"]
        links.new(finish, brush.inputs[6])
        links.new(density.outputs["Result"], brush.inputs[7])
        finish = brush.outputs[2]
    blend = node("ShaderNodeMix", "Painterly strength", 960)
    blend.data_type = "RGBA"
    blend.inputs[0].default_value = settings.get("strength", 0.75)
    links.new(straight.outputs["Image"], blend.inputs[6])
    links.new(finish, blend.inputs[7])
    finish = blend.outputs[2]
    if settings.get("glow_strength", 0) > 0:
        glow = node("CompositorNodeGlare", "Magical light bloom", 1200, -1200)
        glow.inputs["Type"].default_value = "Fog Glow"
        glow.inputs["Threshold"].default_value = 1.2
        glow.inputs["Strength"].default_value = settings["glow_strength"]
        glow.inputs["Size"].default_value = 0.35
        links.new(finish, glow.inputs["Image"])
        finish = glow.outputs["Image"]
    if settings.get("vignette_strength", 0) > 0:
        center = node("ShaderNodeVectorMath", "Event focal area", 1440, -1200)
        center.operation = "SUBTRACT"
        center.inputs[1].default_value = (settings.get("vignette_x", 0.5), settings.get("vignette_y", 0.5), 0)
        links.new(coordinates.outputs["Normalized"], center.inputs[0])
        radius = node("ShaderNodeVectorMath", "Text-safe framing", 1680, -1200)
        radius.operation = "MULTIPLY"
        radius.inputs[1].default_value = (1 / settings.get("vignette_width", 0.7),
                                          1 / settings.get("vignette_height", 0.7), 0)
        links.new(center.outputs["Vector"], radius.inputs[0])
        distance = node("ShaderNodeVectorMath", "Distance from subject", 1920, -1200)
        distance.operation = "LENGTH"
        links.new(radius.outputs["Vector"], distance.inputs[0])
        falloff = node("ShaderNodeMapRange", "Lose detail into shadow", 2160, -1200)
        falloff.interpolation_type = "SMOOTHERSTEP"
        falloff.inputs["From Min"].default_value = 0.25
        falloff.inputs["From Max"].default_value = 1.05
        falloff.inputs["To Min"].default_value = 1
        falloff.inputs["To Max"].default_value = 0.035
        links.new(distance.outputs["Value"], falloff.inputs["Value"])
        vignette = node("ShaderNodeMix", "Quiet negative space", 2400, -1200)
        vignette.data_type = "RGBA"
        vignette.blend_type = "MULTIPLY"
        vignette.inputs[0].default_value = settings["vignette_strength"]
        links.new(finish, vignette.inputs[6])
        links.new(falloff.outputs["Result"], vignette.inputs[7])
        finish = vignette.outputs[2]
    alpha = node("CompositorNodeSetAlpha", "Restore original silhouette", 1200)
    alpha.inputs["Type"].default_value = "Replace Alpha"
    links.new(finish, alpha.inputs["Image"])
    links.new(source.outputs["Alpha"], alpha.inputs["Alpha"])
    premultiplied = node("CompositorNodePremulKey", "Premultiply for render output", 1440)
    links.new(alpha.outputs["Image"], premultiplied.inputs["Image"])
    output = node("NodeGroupOutput", "Finished image", 1680)
    links.new(premultiplied.outputs["Image"], output.inputs["Image"])