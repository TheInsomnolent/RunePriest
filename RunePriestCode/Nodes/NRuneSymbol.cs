using Godot;
using MegaCrit.Sts2.Core.Helpers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Nodes;

/// <summary>
/// One rune: a coloured script character that bobs around its home position, with value-scaled particles.
/// <see cref="Rune.Radiant"/> runes (inscribed by an Ascended card) cycle through the rainbow instead, with rainbow
/// sparks, an orbiting sparkle ring and a spinning RGB halo.
/// </summary>
public partial class NRuneSymbol : Node2D
{
    private const float BaseFontSize = 38f;
    private const float LabelBox = 96f;
    private const float PentagonRadius = 30f;
    // Stays within the pentagon's inradius (30 * cos 36° ≈ 24.3) at every rotation.
    private const float HeptagonRadius = 23f;
    private const float PentagonSpin = 0.45f;
    private const float HeptagonSpin = -0.7f;
    private const float MandalaAlpha = 0.55f;
    private const float MandalaFadeSpeed = 4f;

    // Hue cycles per second; the X-position term turns the colour cycle into a wave along the Incantation.
    private const float HueSpeed = 0.35f;
    private const float HueWave = 0.0025f;
    private const float GhostSaturation = 0.35f;
    private const int HaloSegments = 24;
    private const float HaloRadius = 25f;
    private const float HaloSpin = 1.3f;
    private const float SparkleRadius = 22f;

    private readonly Vector2[] _pentagon = new Vector2[6];
    private readonly Vector2[] _heptagon = new Vector2[8];
    private Color _mandalaColor;
    private float _mandala;

    private Vector2 _home;
    private Vector2 _amplitude;
    private Vector2 _frequency;
    private Vector2 _phase;
    private double _time;
    private CpuParticles2D? _particles;
    private NVoidVortex? _vortex;
    private Rune? _rune;
    private Label? _value;
    private Label? _symbol;
    private bool _radiant;
    private float _saturation = 1f;
    private CpuParticles2D? _sparkles;

    /// <param name="ghost">A drag-preview rune: desaturated, with sparser particles.</param>
    public static NRuneSymbol Create(Rune rune, bool ghost = false)
    {
        var node = new NRuneSymbol();
        node.Build(rune, ghost);
        return node;
    }

    /// <summary>Frames the rune in a spinning pentagon/heptagon mandala: it stays past the end of this turn.</summary>
    public bool Persisting { get; set; }

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
        foreach (var particles in new[] { _particles, _sparkles })
        {
            if (particles == null) continue;
            particles.SpeedScale = 3f;
            GetTree().CreateTimer(0.35).Timeout += () =>
            {
                if (IsInstanceValid(particles)) particles.SpeedScale = 1f;
            };
        }
    }

    public override void _Process(double delta)
    {
        _time += delta;
        var t = (float)_time;
        Position = _home + new Vector2(
            Mathf.Sin(t * _frequency.X + _phase.X) * _amplitude.X,
            Mathf.Sin(t * _frequency.Y + _phase.Y) * _amplitude.Y);
        var mandala = Mathf.MoveToward(_mandala, Persisting ? 1f : 0f, (float)delta * MandalaFadeSpeed);
        if (mandala > 0f || _mandala > 0f || _radiant) QueueRedraw();
        _mandala = mandala;
        if (_radiant) CycleSymbolColors();
    }

    public override void _Draw()
    {
        var t = (float)_time;
        if (_radiant) DrawHalo(t);
        if (_mandala <= 0f) return;
        var color = new Color(_radiant ? Rainbow() : _mandalaColor, MandalaAlpha * _mandala);
        DrawPolyline(Polygon(_pentagon, PentagonRadius, _phase.X + t * PentagonSpin), color, 2f, antialiased: true);
        DrawPolyline(Polygon(_heptagon, HeptagonRadius, _phase.Y + t * HeptagonSpin), color, 2f, antialiased: true);
    }

    /// <summary>A ring of rainbow arcs (hue around the circle, rotating) over a faint, pulsing glow.</summary>
    private void DrawHalo(float t)
    {
        var step = Mathf.Tau / HaloSegments;
        var spin = _phase.X + t * HaloSpin;
        var radius = HaloRadius + Mathf.Sin(t * 2.4f + _phase.Y) * 1.5f;
        for (var i = 0; i < HaloSegments; i++)
        {
            var from = spin + i * step;
            var color = Rainbow((float)i / HaloSegments);
            DrawArc(Vector2.Zero, radius, from, from + step * 1.05f, 4, new Color(color, 0.18f), 8f, antialiased: true);
            DrawArc(Vector2.Zero, radius, from, from + step * 1.05f, 4, new Color(color, 0.7f), 2f, antialiased: true);
        }
    }

    /// <summary>The symbol's fill, outline and glow follow the rainbow, each a little apart in hue.</summary>
    private void CycleSymbolColors()
    {
        if (_symbol?.LabelSettings is not { } settings) return;
        settings.FontColor = Rainbow().Lightened(0.35f);
        settings.OutlineColor = new Color(Rainbow(0.5f).Darkened(0.75f), 0.95f);
        settings.ShadowColor = new Color(Rainbow(0.15f), 0.6f);
    }

    /// <summary>The current rainbow colour, <paramref name="offset"/> turns of the hue wheel ahead.</summary>
    private Color Rainbow(float offset = 0f)
    {
        var x = IsInsideTree() ? GlobalPosition.X : Position.X;
        var hue = (float)(Time.GetTicksMsec() / 1000.0) * HueSpeed + x * HueWave + offset;
        return Color.FromHsv(Mathf.PosMod(hue, 1f), _saturation * 0.85f, 1f);
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

    private void Build(Rune rune, bool ghost)
    {
        var color = RuneVisuals.ColorOf(rune);
        if (ghost) color = RuneVisuals.Desaturate(color);
        _mandalaColor = color;
        var intensity = RuneVisuals.IntensityOf(rune);

        // Purely cosmetic randomness, so GD.Randf (not the seeded run RNG) is fine here.
        _amplitude = new Vector2(2f + GD.Randf() * 3f, 3f + GD.Randf() * 4f);
        _frequency = new Vector2(0.8f + GD.Randf() * 0.9f, 1.0f + GD.Randf() * 1.1f);
        _phase = new Vector2(GD.Randf() * Mathf.Tau, GD.Randf() * Mathf.Tau);

        _radiant = rune.Radiant;
        if (ghost) _saturation = GhostSaturation;
        if (_radiant)
        {
            _sparkles = CreateSparkles(_saturation, ghost ? 0.4f : 1f);
            AddChild(_sparkles);
        }

        if (rune is VoidRune)
        {
            _vortex = new NVoidVortex();
            if (ghost) _vortex.Modulate = new Color(0.75f, 0.75f, 0.75f);
            AddChild(_vortex);
            return;
        }

        _particles = _radiant
            ? CreateRainbowParticles(_saturation, intensity * (ghost ? 0.4f : 1f))
            : CreateParticles(color, intensity * (ghost ? 0.4f : 1f));
        AddChild(_particles);

        var label = _symbol = new Label
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
                ShadowSize = _radiant ? 18 : 14,
                ShadowColor = new Color(color, 0.45f),
                ShadowOffset = Vector2.Zero
            }
        };
        AddChild(label);
        if (_radiant) CycleSymbolColors();

        if (!rune.ShowsValue) return;
        _rune = rune;
        _value = new Label
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
        AddChild(_value);
    }

    /// <summary>Shows the value after game effects (Strength, Weak, Frail…): green if raised, red if lowered.</summary>
    public void ShowModifiedValue(int? modified)
    {
        if (_value == null || _rune == null) return;
        var shown = modified ?? _rune.Value;
        _value.Text = shown == _rune.Value ? _rune.ValueLabel : shown.ToString();
        _value.LabelSettings.FontColor = shown > _rune.Value ? StsColors.green
            : shown < _rune.Value ? StsColors.red : Colors.White;
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

    /// <summary>The usual value-scaled sparks, denser and in every colour: each spark starts at a random hue and runs the rainbow.</summary>
    private static CpuParticles2D CreateRainbowParticles(float saturation, float intensity)
    {
        var particles = CreateParticles(Colors.White, intensity);
        particles.Amount = (int)Mathf.Lerp(12f, 64f, intensity);
        particles.EmissionSphereRadius += 4f;
        particles.InitialVelocityMax += 10f;
        particles.ColorRamp = RainbowRamp(saturation);
        particles.HueVariationMin = -1f;
        particles.HueVariationMax = 1f;
        return particles;
    }

    /// <summary>Rainbow motes orbiting the symbol. Local coordinates, so the ring follows the bobbing symbol.</summary>
    private static CpuParticles2D CreateSparkles(float saturation, float density) => new()
    {
        Amount = (int)Mathf.Max(6f, 20f * density),
        Lifetime = 1.1,
        Preprocess = 1.1,
        LocalCoords = true,
        EmissionShape = CpuParticles2D.EmissionShapeEnum.SphereSurface,
        EmissionSphereRadius = SparkleRadius,
        Gravity = Vector2.Zero,
        InitialVelocityMin = 0f,
        InitialVelocityMax = 4f,
        OrbitVelocityMin = 0.25f,
        OrbitVelocityMax = 0.55f,
        ScaleAmountMin = 1.5f,
        ScaleAmountMax = 3.5f,
        Color = Colors.White,
        ColorRamp = RainbowRamp(saturation),
        HueVariationMin = -1f,
        HueVariationMax = 1f,
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
        ShowBehindParent = true
    };

    /// <summary>One full turn of the hue wheel over a particle's lifetime, fading out at the end.</summary>
    private static Gradient RainbowRamp(float saturation)
    {
        const int stops = 7;
        var offsets = new float[stops];
        var colors = new Color[stops];
        for (var i = 0; i < stops; i++)
        {
            var at = i / (float)(stops - 1);
            offsets[i] = at;
            colors[i] = Color.FromHsv(at, saturation * 0.9f, 1f, Mathf.Lerp(0.95f, 0f, at * at));
        }
        return new Gradient { Offsets = offsets, Colors = colors };
    }
}
