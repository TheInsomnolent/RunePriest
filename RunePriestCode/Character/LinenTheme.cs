using Godot;

namespace RunePriest.RunePriestCode.Character;

/// <summary>
/// The Rune Priest's linen colour theme: palette, the card frame / energy orb material, and a helper that
/// recolours borrowed vanilla VFX.
/// </summary>
public static class LinenTheme
{
    public static readonly Color Linen = new("EDE3D1");
    public static readonly Color Umber = new("6B5A44");
    public static readonly Color DeepUmber = new("4A3F33");

    // Vanilla frames/orbs are saturated colour; this re-tints only the saturated (chroma) parts to linen and
    // leaves near-neutral areas (e.g. the card text panel) dark, which the stock HSV shader can't do.
    // `h` is unused but kept because the deck view copies it onto its sort buttons.
    private const string ShaderCode = """
        shader_type canvas_item;

        uniform float h = 0.12;
        uniform vec3 tint : source_color = vec3(0.929, 0.890, 0.820);
        uniform float base = 0.92;
        uniform float slope = 1.6;
        uniform vec3 warm = vec3(1.04, 1.0, 0.94);

        varying vec4 modulate_color;

        void vertex() {
            modulate_color = COLOR;
        }

        void fragment() {
            vec4 col = texture(TEXTURE, UV);
            float y = dot(col.rgb, vec3(0.2989, 0.5870, 0.1140));
            float chroma = max(col.r, max(col.g, col.b)) - min(col.r, min(col.g, col.b));
            float m = smoothstep(0.10, 0.28, chroma);
            vec3 linen = tint * clamp(base + (y - 0.30) * slope, 0.0, 1.15);
            col.rgb = mix(vec3(y) * warm, linen, m);
            COLOR = col * modulate_color;
        }
        """;

    private static Shader? _shader;
    private static ShaderMaterial? _cardFrame;
    private static ShaderMaterial? _energyOrb;

    public static ShaderMaterial CardFrameMaterial => Valid(_cardFrame) ?? (_cardFrame = CreateMaterial(0.92f, 1.6f));

    // Orb centres are much brighter than frame borders, so flatten the ramp to keep the energy label readable.
    public static ShaderMaterial EnergyOrbMaterial => Valid(_energyOrb) ?? (_energyOrb = CreateMaterial(0.80f, 0.9f));

    private static ShaderMaterial? Valid(ShaderMaterial? material) =>
        material != null && GodotObject.IsInstanceValid(material) ? material : null;

    private static ShaderMaterial CreateMaterial(float baseLevel, float slope)
    {
        if (_shader == null || !GodotObject.IsInstanceValid(_shader)) _shader = new Shader { Code = ShaderCode };
        var material = new ShaderMaterial { Shader = _shader };
        material.SetShaderParameter("tint", Linen);
        material.SetShaderParameter("base", baseLevel);
        material.SetShaderParameter("slope", slope);
        return material;
    }

    /// <summary>Linen with the brightness (max channel, HDR-safe) and alpha of <paramref name="color"/>.</summary>
    public static Color Tint(Color color)
    {
        var value = Mathf.Max(color.R, Mathf.Max(color.G, color.B));
        return new Color(Linen.R * value, Linen.G * value, Linen.B * value, color.A);
    }

    /// <summary>
    /// Recolours every modulate, line and particle colour under <paramref name="root"/> to linen. Resources are
    /// duplicated before editing because vanilla scenes share them with other characters' instances.
    /// </summary>
    public static void Recolor(Node root)
    {
        if (root is CanvasItem item)
        {
            item.Modulate = Tint(item.Modulate);
            item.SelfModulate = Tint(item.SelfModulate);
        }

        switch (root)
        {
            case Line2D line:
                line.DefaultColor = Tint(line.DefaultColor);
                line.Gradient = Tint(line.Gradient);
                break;
            case CpuParticles2D cpu:
                cpu.Color = Tint(cpu.Color);
                cpu.ColorRamp = Tint(cpu.ColorRamp);
                cpu.ColorInitialRamp = Tint(cpu.ColorInitialRamp);
                break;
            case GpuParticles2D { ProcessMaterial: ParticleProcessMaterial process } gpu:
                var copy = (ParticleProcessMaterial)process.Duplicate();
                copy.Color = Tint(copy.Color);
                copy.ColorRamp = Tint(copy.ColorRamp);
                copy.ColorInitialRamp = Tint(copy.ColorInitialRamp);
                gpu.ProcessMaterial = copy;
                break;
        }

        foreach (var child in root.GetChildren()) Recolor(child);
    }

    private static Gradient? Tint(Gradient? gradient)
    {
        if (gradient == null) return null;
        var copy = (Gradient)gradient.Duplicate();
        var colors = copy.Colors;
        for (var i = 0; i < colors.Length; i++) colors[i] = Tint(colors[i]);
        copy.Colors = colors;
        return copy;
    }

    private static Texture2D? Tint(Texture2D? texture)
    {
        if (texture is not GradientTexture1D ramp) return texture;
        var copy = (GradientTexture1D)ramp.Duplicate();
        copy.Gradient = Tint(copy.Gradient);
        return copy;
    }
}
