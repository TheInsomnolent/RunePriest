using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace RunePriest.RunePriestCode.Nodes;

/// <summary>
/// Rune Priest death: the body staggers, a void core opens at its centre and gravitationally lenses the body (and
/// everything near it) into itself while Void-rune motes spiral in, then the core collapses with a small pop.
/// Parented to the creature visuals so it follows them onto the game-over screen layer.
/// </summary>
public partial class NVoidSwallow : Node2D
{
    private const float StaggerTime = 0.35f;
    private const float PullTime = 1.0f;
    private const float CollapseTime = 0.55f;
    private const float FlashTime = 0.35f;
    public const float Duration = StaggerTime + PullTime + CollapseTime + FlashTime;
    private const float SpawnStartTime = StaggerTime * 0.5f;
    private const float SpawnStopTime = StaggerTime + PullTime + CollapseTime * 0.5f;

    private const float CoverPadding = 1.1f;
    private const float CoreFraction = 0.3f;
    private const float LensScale = 1.6f;
    // At this gamma everything inside the body's radius is squeezed inside the core.
    private const float MinGamma = 0.15f;
    private const float MaxTwist = 5f;
    private const float StaggerLean = 0.14f;
    private const float StaggerShake = 12f;

    private const float SpawnInterval = 0.006f;
    private const int MaxMotes = 260;
    private const float FadeInTime = 0.2f;
    private const float MaxMoteAge = 4f;

    private static readonly Color CoreColor = new(0.02f, 0.02f, 0.03f);
    private static readonly Color HaloColor = new(1f, 1f, 1f, 0.3f);
    private static readonly Color GlowColor = new(1f, 1f, 1f, 0.08f);

    // Screen-space gravitational lens; derives the local->screen-UV mapping from derivatives so zoom/scale/flip just work.
    private const string LensShaderCode = """
        shader_type canvas_item;
        render_mode unshaded;

        uniform sampler2D screen_tex : hint_screen_texture, filter_linear;
        uniform float radius = 100.0;
        uniform float gamma = 1.0;
        uniform float twist = 0.0;

        varying vec2 local_pos;

        void vertex() {
            local_pos = VERTEX;
        }

        void fragment() {
            mat2 uv_d = mat2(dFdx(SCREEN_UV), dFdy(SCREEN_UV));
            mat2 local_d = mat2(dFdx(local_pos), dFdy(local_pos));
            float r = length(local_pos);
            if (r >= radius) {
                discard;
            }
            float n = r / radius;
            float a = -twist * (1.0 - n) * (1.0 - n);
            vec2 dir = local_pos / max(r, 0.0001);
            vec2 rotated = vec2(dir.x * cos(a) - dir.y * sin(a), dir.x * sin(a) + dir.y * cos(a));
            vec2 src = rotated * radius * pow(max(n, 0.0001), gamma);
            COLOR = texture(screen_tex, SCREEN_UV + uv_d * inverse(local_d) * (src - local_pos));
        }
        """;

    private static Shader? _lensShader;

    private readonly List<Mote> _motes = [];
    private Node2D? _body;
    private Vector2 _bodyPosition;
    private Vector2 _bodyScale;
    private float _bodyRotation;
    private bool _bodyHidden;
    private Node2D _lens = null!;
    private ShaderMaterial _lensMaterial = null!;
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
        var body = visuals.Body;
        var node = new NVoidSwallow
        {
            _body = body,
            _bodyPosition = body.Position,
            _bodyScale = body.Scale,
            _bodyRotation = body.Rotation,
            _maxRadius = bounds.Size.Length() * 0.5f * CoverPadding / scale
        };
        node.BuildLens();
        visuals.AddChild(node);
        node.GlobalPosition = bounds.GetCenter();
        return node;
    }

    /// <summary>Stops the effect and puts the body back, e.g. on revive.</summary>
    public void Abort()
    {
        if (_body != null && IsInstanceValid(_body))
        {
            RestoreBodyTransform();
            _body.Visible = true;
        }

        QueueFree();
    }

    private void BuildLens()
    {
        var lensRadius = _maxRadius * LensScale;
        _lensMaterial = new ShaderMaterial { Shader = _lensShader ??= new Shader { Code = LensShaderCode } };
        _lensMaterial.SetShaderParameter("radius", lensRadius);
        _lens = new Node2D { ShowBehindParent = true, Material = _lensMaterial, Visible = false };
        _lens.Draw += () => _lens.DrawRect(new Rect2(-lensRadius, -lensRadius, lensRadius * 2f, lensRadius * 2f), Colors.White);

        // Forces a fresh screen copy so the lens always sees the body drawn this frame.
        AddChild(new BackBufferCopy { CopyMode = BackBufferCopy.CopyModeEnum.Viewport, ShowBehindParent = true });
        AddChild(_lens);
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _age += dt;

        UpdateBody();
        UpdateLens();

        if (_age is >= SpawnStartTime and < SpawnStopTime)
        {
            _spawnTimer -= dt;
            while (_spawnTimer <= 0f)
            {
                _spawnTimer += SpawnInterval;
                if (_motes.Count < MaxMotes) Spawn();
            }
        }

        var disc = CoreRadius();
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

    private void UpdateBody()
    {
        if (_body == null || _bodyHidden || !IsInstanceValid(_body)) return;

        if (_age < StaggerTime)
        {
            // Recoil away from the void (the Rune Priest faces right, so "back" is counter-clockwise).
            var p = _age / StaggerTime;
            var wave = Mathf.Sin(p * Mathf.Pi);
            _body.Rotation = _bodyRotation - StaggerLean * wave;
            _body.Scale = new Vector2(_bodyScale.X * (1f + 0.03f * wave), _bodyScale.Y * (1f - 0.05f * wave));
            _body.Position = _bodyPosition + new Vector2(GD.Randf() - 0.5f, GD.Randf() - 0.5f) * StaggerShake * (1f - p);
            return;
        }

        RestoreBodyTransform();
        // By the end of the pull the lens has squeezed the body inside the core, so hiding it is invisible.
        if (_age >= StaggerTime + PullTime)
        {
            _body.Visible = false;
            _bodyHidden = true;
        }
    }

    private void RestoreBodyTransform()
    {
        _body!.Position = _bodyPosition;
        _body.Scale = _bodyScale;
        _body.Rotation = _bodyRotation;
    }

    private void UpdateLens()
    {
        var t = _age - StaggerTime;
        if (t <= 0f || t >= PullTime + CollapseTime)
        {
            _lens.Visible = false;
            return;
        }

        float strength, twist;
        if (t < PullTime)
        {
            var p = t / PullTime;
            strength = Mathf.SmoothStep(0f, 1f, p);
            twist = MaxTwist * p * p;
        }
        else
        {
            var p = (t - PullTime) / CollapseTime;
            strength = 1f - p;
            twist = MaxTwist * (1f - p);
        }

        _lens.Visible = true;
        _lensMaterial.SetShaderParameter("gamma", Mathf.Lerp(1f, MinGamma, strength));
        _lensMaterial.SetShaderParameter("twist", twist);
    }

    private float CoreRadius()
    {
        var t = _age - StaggerTime;
        if (t <= 0f) return 0f;

        var core = _maxRadius * CoreFraction;
        if (t < PullTime)
        {
            var p = 1f - Mathf.Min(t / (PullTime * 0.35f), 1f);
            return core * (1f - p * p * p) * (1f + 0.04f * Mathf.Sin(_age * 30f));
        }

        t -= PullTime;
        if (t < CollapseTime)
        {
            var p = t / CollapseTime;
            return core * (1f - p * p * p);
        }

        return 0f;
    }

    public override void _Draw()
    {
        var disc = CoreRadius();
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

        var flashT = _age - StaggerTime - PullTime - CollapseTime;
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
