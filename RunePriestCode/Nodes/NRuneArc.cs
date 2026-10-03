using Godot;

namespace RunePriest.RunePriestCode.Nodes;

/// <summary>
/// A streaming arc of rune-coloured motes from a hovered glyph to one of its targets. Created in code, drawn in global
/// coordinates (<see cref="CanvasItem.TopLevel"/>) so it can span the room; the source and target are re-read every frame
/// so the arc follows bobbing glyphs and moving creatures. Fades in, then fades out and frees itself on <see cref="Release"/>.
/// </summary>
public partial class NRuneArc : Node2D
{
    private const int CurveSegments = 32;
    private const float MoteSpeed = 0.9f;
    private const float FadeSpeed = 6f;

    private readonly List<(float T, float Speed, float Size, float Wobble)> _motes = [];
    private readonly Vector2[] _curve = new Vector2[CurveSegments + 1];
    private Func<Vector2> _from = () => Vector2.Zero;
    private Func<Vector2> _to = () => Vector2.Zero;
    private Color _color;
    private bool _random;
    private float _spawn;
    private float _alpha;
    private float _time;
    private bool _released;

    /// <param name="random">The target is one of several candidates (Scatter): drawn fainter, with sparser motes.</param>
    public static NRuneArc Create(Func<Vector2> from, Func<Vector2> to, Color color, bool random) => new()
    {
        _from = from,
        _to = to,
        _color = color,
        _random = random,
        TopLevel = true,
        ZIndex = 100,
        ZAsRelative = false,
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
    };

    /// <summary>Fade out, then free.</summary>
    public void Release() => _released = true;

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _time += dt;
        _alpha = Mathf.MoveToward(_alpha, _released ? 0f : 1f, dt * FadeSpeed);
        if (_released && _alpha <= 0f)
        {
            QueueFree();
            return;
        }

        if (!_released)
        {
            // Purely cosmetic randomness, so GD.Randf (not the seeded run RNG) is fine here.
            _spawn += dt * (_random ? 14f : 34f);
            for (; _spawn >= 1f; _spawn--)
                _motes.Add((0f, MoteSpeed * (0.75f + GD.Randf() * 0.5f), 2.5f + GD.Randf() * 3.5f, GD.Randf() * Mathf.Tau));
        }

        for (var i = _motes.Count - 1; i >= 0; i--)
        {
            var mote = _motes[i];
            mote.T += mote.Speed * dt;
            if (mote.T >= 1f) _motes.RemoveAt(i);
            else _motes[i] = mote;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        var from = _from();
        var to = _to();
        // Arc upwards: the higher the further it travels, like a thrown spell.
        var control = (from + to) / 2f + Vector2.Up * (60f + from.DistanceTo(to) * 0.35f);
        for (var i = 0; i <= CurveSegments; i++)
            _curve[i] = Bezier(from, control, to, i / (float)CurveSegments);

        var strength = _alpha * (_random ? 0.45f : 1f);
        // The line stays faint so the motes carry the effect.
        DrawPolyline(_curve, new Color(_color, 0.1f * strength), 10f, antialiased: true);
        DrawPolyline(_curve, new Color(_color.Lightened(0.4f), 0.2f * strength), 3f, antialiased: true);

        foreach (var (t, _, size, wobble) in _motes)
        {
            var point = Bezier(from, control, to, t);
            var tangent = (Bezier(from, control, to, Mathf.Min(t + 0.01f, 1f)) - point).Normalized();
            point += tangent.Orthogonal() * Mathf.Sin(_time * 9f + wobble) * 6f;
            // Motes swell mid-flight and fade at both ends.
            var envelope = Mathf.Sin(t * Mathf.Pi);
            DrawCircle(point, size * (0.6f + envelope), new Color(_color, 0.35f * envelope * strength));
            DrawCircle(point, size * 0.45f * (0.6f + envelope), new Color(_color.Lightened(0.6f), 0.8f * envelope * strength));
        }

        // Impact glow at the target.
        var pulse = 0.75f + Mathf.Sin(_time * 6f) * 0.25f;
        DrawCircle(to, 22f * pulse, new Color(_color, 0.2f * strength));
        DrawCircle(to, 9f * pulse, new Color(_color.Lightened(0.5f), 0.45f * strength));
    }

    private static Vector2 Bezier(Vector2 a, Vector2 control, Vector2 b, float t)
    {
        var u = 1f - t;
        return u * u * a + 2f * u * t * control + t * t * b;
    }
}
