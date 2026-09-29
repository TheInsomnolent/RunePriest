using Godot;

namespace RunePriest.RunePriestCode.Nodes;

/// <summary>
/// Black-hole visual for the Void rune: a dark hollow ring that slowly pulls faint white motes in from the edges of the
/// screen. Motes fade in at the screen border, spiral inward while accelerating, brighten as they approach and are
/// swallowed by the ring. Kept sparse and faint at the edges so the full-screen effect stays subtle.
/// </summary>
public partial class NVoidVortex : Node2D
{
    private const float RingRadius = 16f;
    private const float RingWidth = 5f;
    private const float SpawnInterval = 0.16f;
    private const int MaxMotes = 40;
    private const float Prewarm = 4f;
    private const float PrewarmStep = 1f / 30f;
    private const float MaxLifetime = 12f;

    private const float EdgeAlpha = 0.04f;
    private const float PeakAlpha = 0.9f;
    private const float FadeInTime = 0.8f;

    private const float InwardSpeed = 35f;
    private const float InwardAcceleration = 110f;

    private static readonly Color RingColor = new(0.06f, 0.06f, 0.08f);
    private static readonly Color HaloColor = new(1f, 1f, 1f, 0.22f);

    private readonly List<Mote> _motes = [];
    private float _spawnTimer;
    private float _timeScale = 1f;
    private bool _warmed;

    private sealed class Mote
    {
        public float Radius;
        public float StartRadius;
        public float Angle;
        public float Age;
        public float Size;
    }

    /// <summary>Briefly speeds up the pull, e.g. when the glyph is spoken.</summary>
    public void Burst()
    {
        _timeScale = 3f;
        GetTree().CreateTimer(0.35).Timeout += () =>
        {
            if (IsInstanceValid(this)) _timeScale = 1f;
        };
    }

    public override void _Process(double delta)
    {
        if (!_warmed)
        {
            _warmed = true;
            for (var t = 0f; t < Prewarm; t += PrewarmStep) Step(PrewarmStep);
        }

        Step((float)delta * _timeScale);
        QueueRedraw();
    }

    private void Step(float dt)
    {
        _spawnTimer -= dt;
        while (_spawnTimer <= 0f)
        {
            _spawnTimer += SpawnInterval * (0.6f + GD.Randf() * 0.8f);
            if (_motes.Count < MaxMotes) Spawn();
        }

        for (var i = _motes.Count - 1; i >= 0; i--)
        {
            var mote = _motes[i];
            mote.Age += dt;

            // Radial pull ramps up over time and near the core; the swirl tightens as the mote falls in.
            var radial = (InwardSpeed + InwardAcceleration * mote.Age) * (1f + 80f / mote.Radius);
            var tangential = radial * (0.5f + 70f / (mote.Radius + 20f));
            mote.Angle += tangential / mote.Radius * dt;
            mote.Radius -= radial * dt;

            if (mote.Radius <= RingRadius * 0.5f || mote.Age > MaxLifetime) _motes.RemoveAt(i);
        }
    }

    /// <summary>Picks a random point on the screen border (cosmetic, so <c>GD.Randf</c> rather than run RNG).</summary>
    private void Spawn()
    {
        var rect = GetViewportRect();
        var perimeter = 2f * (rect.Size.X + rect.Size.Y);
        if (perimeter <= 0f) return;

        var d = GD.Randf() * perimeter;
        Vector2 screenPoint;
        if (d < rect.Size.X) screenPoint = new Vector2(d, 0f);
        else if ((d -= rect.Size.X) < rect.Size.Y) screenPoint = new Vector2(rect.Size.X, d);
        else if ((d -= rect.Size.Y) < rect.Size.X) screenPoint = new Vector2(rect.Size.X - d, rect.Size.Y);
        else screenPoint = new Vector2(0f, rect.Size.Y - (d - rect.Size.X));

        var offset = GetCanvasTransform().AffineInverse() * (rect.Position + screenPoint) - GlobalPosition;
        var radius = offset.Length();
        if (radius <= RingRadius) return;

        _motes.Add(new Mote
        {
            Radius = radius,
            StartRadius = radius,
            Angle = offset.Angle(),
            Size = 1.2f + GD.Randf() * 1.3f
        });
    }

    public override void _Draw()
    {
        // Motes live in canvas space around the rune, so the glyph's scale pulses don't distort the spiral.
        DrawSetTransformMatrix(GetGlobalTransform().AffineInverse());
        var center = GlobalPosition;
        foreach (var mote in _motes)
        {
            var approach = Mathf.SmoothStep(0f, 1f, 1f - mote.Radius / mote.StartRadius);
            var fadeIn = Mathf.Clamp(mote.Age / FadeInTime, 0f, 1f);
            var swallow = Mathf.SmoothStep(RingRadius * 0.5f, RingRadius * 1.6f, mote.Radius);
            var alpha = Mathf.Lerp(EdgeAlpha, PeakAlpha, approach) * fadeIn * swallow;
            if (alpha <= 0.005f) continue;

            var position = center + Vector2.FromAngle(mote.Angle) * mote.Radius;
            DrawCircle(position, mote.Size * Mathf.Lerp(1f, 0.6f, 1f - swallow), new Color(1f, 1f, 1f, alpha));
        }

        DrawSetTransformMatrix(Transform2D.Identity);
        DrawArc(Vector2.Zero, RingRadius + RingWidth * 0.5f + 1.5f, 0f, Mathf.Tau, 48, HaloColor, 2f, true);
        DrawArc(Vector2.Zero, RingRadius, 0f, Mathf.Tau, 48, RingColor, RingWidth, true);
    }
}
