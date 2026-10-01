using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace RunePriest.RunePriestCode.Nodes;

/// <summary>
/// Rune Priest death: a void disc swells over the body, the body vanishes, then the disc collapses to a point while a
/// dense swarm of Void-rune motes spirals in. Parented to the creature visuals so it follows them onto the game-over
/// screen layer.
/// </summary>
public partial class NVoidSwallow : Node2D
{
    private const float GrowTime = 0.45f;
    private const float HoldTime = 0.3f;
    private const float CollapseTime = 0.8f;
    private const float FlashTime = 0.35f;
    public const float Duration = GrowTime + HoldTime + CollapseTime + FlashTime;
    private const float SpawnStopTime = GrowTime + HoldTime + CollapseTime * 0.6f;

    private const float CoverPadding = 1.1f;
    private const float SpawnInterval = 0.006f;
    private const int MaxMotes = 260;
    private const float FadeInTime = 0.2f;
    private const float MaxMoteAge = 4f;

    private static readonly Color CoreColor = new(0.02f, 0.02f, 0.03f);
    private static readonly Color HaloColor = new(1f, 1f, 1f, 0.3f);
    private static readonly Color GlowColor = new(1f, 1f, 1f, 0.08f);

    private readonly List<Mote> _motes = [];
    private Node2D? _body;
    private float _maxRadius;
    private float _age;
    private float _spawnTimer;

    private sealed class Mote
    {
        public float Radius;
        public float Angle;
        public float Age;
        public float Size;
    }

    public static NVoidSwallow Create(NCreatureVisuals visuals)
    {
        var bounds = visuals.Bounds.GetGlobalRect();
        var scale = Mathf.Max(Mathf.Abs(visuals.GlobalScale.X), 0.01f);
        var node = new NVoidSwallow
        {
            _body = visuals.Body,
            _maxRadius = bounds.Size.Length() * 0.5f * CoverPadding / scale
        };
        visuals.AddChild(node);
        node.GlobalPosition = bounds.GetCenter();
        return node;
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _age += dt;

        if (_age >= GrowTime && _body != null)
        {
            if (IsInstanceValid(_body)) _body.Visible = false;
            _body = null;
        }

        if (_age < SpawnStopTime)
        {
            _spawnTimer -= dt;
            while (_spawnTimer <= 0f)
            {
                _spawnTimer += SpawnInterval;
                if (_motes.Count < MaxMotes) Spawn();
            }
        }

        var disc = DiscRadius();
        for (var i = _motes.Count - 1; i >= 0; i--)
        {
            var mote = _motes[i];
            mote.Age += dt;

            // Pull scales with the disc size so big and small bodies collapse in the same time.
            var d = mote.Radius / _maxRadius;
            var radial = _maxRadius * (0.6f + 4f * mote.Age) * (1f + 0.5f / Mathf.Max(d, 0.05f));
            var tangential = radial * (0.5f + 0.6f / Mathf.Max(d, 0.05f));
            mote.Angle += tangential / Mathf.Max(mote.Radius, 1f) * dt;
            mote.Radius -= radial * dt;

            if (mote.Radius <= Mathf.Max(disc * 0.85f, 2f) || mote.Age > MaxMoteAge) _motes.RemoveAt(i);
        }

        if (_age >= Duration && _motes.Count == 0)
        {
            QueueFree();
            return;
        }

        QueueRedraw();
    }

    private void Spawn()
    {
        _motes.Add(new Mote
        {
            Radius = _maxRadius * (1.3f + GD.Randf() * 2.2f),
            Angle = GD.Randf() * Mathf.Tau,
            Size = (1.4f + GD.Randf() * 1.8f) * Mathf.Clamp(_maxRadius / 150f, 1f, 2f)
        });
    }

    private float DiscRadius()
    {
        if (_age < GrowTime)
        {
            // Back-out ease: overshoots slightly so the disc "gulps" the body.
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            var p = _age / GrowTime - 1f;
            return _maxRadius * (1f + c3 * p * p * p + c1 * p * p);
        }

        var t = _age - GrowTime;
        if (t < HoldTime) return _maxRadius * (1f + 0.03f * Mathf.Sin(t * 30f));

        t -= HoldTime;
        if (t < CollapseTime)
        {
            var p = t / CollapseTime;
            return _maxRadius * (1f - p * p * p);
        }

        return 0f;
    }

    public override void _Draw()
    {
        var disc = DiscRadius();
        if (disc > 0.5f)
        {
            DrawCircle(Vector2.Zero, disc + 6f, GlowColor);
            DrawCircle(Vector2.Zero, disc, CoreColor);
            DrawArc(Vector2.Zero, disc + 2f, 0f, Mathf.Tau, 64, HaloColor, 2.5f, true);
        }

        foreach (var mote in _motes)
        {
            var approach = 1f - Mathf.Clamp((mote.Radius / _maxRadius - 1f) / 2.5f, 0f, 1f);
            var fadeIn = Mathf.Clamp(mote.Age / FadeInTime, 0f, 1f);
            var swallow = Mathf.SmoothStep(disc * 0.85f, disc * 1.3f + 4f, mote.Radius);
            var alpha = Mathf.Lerp(0.15f, 0.95f, approach) * fadeIn * swallow;
            if (alpha <= 0.005f) continue;

            var position = Vector2.FromAngle(mote.Angle) * mote.Radius;
            DrawCircle(position, mote.Size * Mathf.Lerp(0.6f, 1f, swallow), new Color(1f, 1f, 1f, alpha));
        }

        var flashT = _age - GrowTime - HoldTime - CollapseTime;
        if (flashT is > 0f and < FlashTime)
        {
            var p = flashT / FlashTime;
            var fade = 1f - p;
            DrawArc(Vector2.Zero, _maxRadius * 0.6f * Mathf.Sqrt(p), 0f, Mathf.Tau, 64,
                new Color(1f, 1f, 1f, 0.8f * fade), 3f * fade + 0.5f, true);
            DrawCircle(Vector2.Zero, 4f * fade, new Color(1f, 1f, 1f, fade));
        }
    }
}
