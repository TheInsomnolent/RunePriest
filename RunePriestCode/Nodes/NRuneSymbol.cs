using Godot;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Nodes;

/// <summary>One rune: a coloured script character that bobs around its home position, with value-scaled particles.</summary>
public partial class NRuneSymbol : Node2D
{
    private const float BaseFontSize = 38f;
    private const float LabelBox = 96f;
    private const float PentagonRadius = 30f;
    // Stays within the pentagon's inradius (30 * cos 36° ≈ 24.3) at every rotation.
    private const float HeptagonRadius = 23f;
    private const float PentagonSpin = 0.45f;
    private const float HeptagonSpin = -0.7f;

    private readonly Vector2[] _pentagon = new Vector2[6];
    private readonly Vector2[] _heptagon = new Vector2[8];
    private Color? _persistColor;

    private Vector2 _home;
    private Vector2 _amplitude;
    private Vector2 _frequency;
    private Vector2 _phase;
    private double _time;
    private CpuParticles2D? _particles;
    private NVoidVortex? _vortex;

    /// <param name="persistent">Frame the rune in a spinning pentagon/heptagon mandala (it stays between turns).</param>
    public static NRuneSymbol Create(Rune rune, bool persistent = false)
    {
        var node = new NRuneSymbol();
        if (persistent) node._persistColor = new Color(RuneVisuals.ColorOf(rune), 0.8f);
        node.Build(rune);
        return node;
    }

    public Vector2 Home
    {
        get => _home;
        set
        {
            _home = value;
            Position = value;
        }
    }

    /// <summary>Briefly boosts the particles, e.g. when the glyph is spoken.</summary>
    public void Burst()
    {
        _vortex?.Burst();
        if (_particles == null) return;
        _particles.SpeedScale = 3f;
        GetTree().CreateTimer(0.35).Timeout += () =>
        {
            if (IsInstanceValid(_particles)) _particles.SpeedScale = 1f;
        };
    }

    public override void _Process(double delta)
    {
        _time += delta;
        var t = (float)_time;
        Position = _home + new Vector2(
            Mathf.Sin(t * _frequency.X + _phase.X) * _amplitude.X,
            Mathf.Sin(t * _frequency.Y + _phase.Y) * _amplitude.Y);
        if (_persistColor != null) QueueRedraw();
    }

    public override void _Draw()
    {
        if (_persistColor is not { } color) return;
        var t = (float)_time;
        DrawPolyline(Polygon(_pentagon, PentagonRadius, _phase.X + t * PentagonSpin), color, 2f, antialiased: true);
        DrawPolyline(Polygon(_heptagon, HeptagonRadius, _phase.Y + t * HeptagonSpin), color, 2f, antialiased: true);
    }

    /// <summary>Fills <paramref name="points"/> with a closed regular polygon (last point repeats the first).</summary>
    private static Vector2[] Polygon(Vector2[] points, float radius, float rotation)
    {
        var sides = points.Length - 1;
        for (var i = 0; i < sides; i++)
            points[i] = Vector2.FromAngle(rotation + i * Mathf.Tau / sides) * radius;
        points[sides] = points[0];
        return points;
    }

    private void Build(Rune rune)
    {
        var color = RuneVisuals.ColorOf(rune);
        var intensity = RuneVisuals.IntensityOf(rune);

        // Purely cosmetic randomness, so GD.Randf (not the seeded run RNG) is fine here.
        _amplitude = new Vector2(2f + GD.Randf() * 3f, 3f + GD.Randf() * 4f);
        _frequency = new Vector2(0.8f + GD.Randf() * 0.9f, 1.0f + GD.Randf() * 1.1f);
        _phase = new Vector2(GD.Randf() * Mathf.Tau, GD.Randf() * Mathf.Tau);

        if (rune is VoidRune)
        {
            _vortex = new NVoidVortex();
            AddChild(_vortex);
            return;
        }

        _particles = CreateParticles(color, intensity);
        AddChild(_particles);

        var label = new Label
        {
            Text = RuneVisuals.SymbolOf(rune),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new Vector2(LabelBox, LabelBox),
            Position = new Vector2(-LabelBox / 2f, -LabelBox / 2f),
            LabelSettings = new LabelSettings
            {
                Font = RuneFont.Font,
                FontSize = (int)(BaseFontSize + intensity * 12f),
                FontColor = color.Lightened(0.25f),
                OutlineSize = 8,
                OutlineColor = new Color(color.Darkened(0.8f), 0.95f),
                ShadowSize = 14,
                ShadowColor = new Color(color, 0.45f),
                ShadowOffset = Vector2.Zero
            }
        };
        AddChild(label);

        if (!rune.ShowsValue) return;
        var value = new Label
        {
            Text = rune.ValueLabel,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = new Vector2(12f, 6f),
            LabelSettings = new LabelSettings
            {
                Font = RuneFont.Font,
                FontSize = 16,
                FontColor = Colors.White,
                OutlineSize = 5,
                OutlineColor = new Color(0f, 0f, 0f, 0.9f)
            }
        };
        AddChild(value);
    }

    private static CpuParticles2D CreateParticles(Color color, float intensity)
    {
        var ramp = new Gradient();
        ramp.SetColor(0, new Color(color.Lightened(0.4f), 0.9f));
        ramp.SetColor(1, new Color(color, 0f));

        return new CpuParticles2D
        {
            Amount = (int)Mathf.Lerp(4f, 48f, intensity),
            Lifetime = 1.2,
            Preprocess = 1.2,
            LocalCoords = false,
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 14f + intensity * 10f,
            Direction = Vector2.Up,
            Spread = 70f,
            Gravity = new Vector2(0f, -25f),
            InitialVelocityMin = 4f,
            InitialVelocityMax = 12f + intensity * 30f,
            ScaleAmountMin = 1.5f,
            ScaleAmountMax = 2.5f + intensity * 2.5f,
            Color = color,
            ColorRamp = ramp,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
            ShowBehindParent = true
        };
    }
}
