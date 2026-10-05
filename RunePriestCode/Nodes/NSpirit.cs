using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using RunePriest.RunePriestCode.Powers;

namespace RunePriest.RunePriestCode.Nodes;

/// <summary>
/// One Cursed Spirit: a purple wisp with a swaying tail and its damage, bobbing around its slot in
/// <see cref="NCursedSpirits"/>. Rises out of its owner when summoned, flies at its target when it attacks and drifts
/// back, and fades away when it leaves. Hovering it shows the Cursed Spirits hover tip.
/// </summary>
public partial class NSpirit : Node2D
{
    private const float FollowSpeed = 6f;
    private const float AppearSpeed = 2.5f;
    private const float FlightSeconds = 0.22f;
    private const float HitboxSize = 40f;
    private const float BodyRadius = 11f;
    private const int TailSegments = 5;
    private const int FlightZIndex = 100;

    private static readonly Color Glow = new("6a2bd9");
    private static readonly Color Body = new("a070ff");
    private static readonly Color Core = new("f0e6ff");
    private static readonly Color Eyes = new("1a0033");

    private Func<PowerModel?> _power = () => null;
    private Control? _hitbox;
    private Vector2 _amplitude;
    private Vector2 _frequency;
    private Vector2 _phase;
    private float _time;
    private float _appear;
    private bool _vanishing;
    private Func<Vector2>? _strikeTarget;
    private Vector2 _strikeFrom;
    private float _strike;

    /// <summary>This spirit's slot, relative to <see cref="NCursedSpirits"/>.</summary>
    public Vector2 Home { get; set; }

    /// <param name="power">The owner's Cursed Spirits power, for the hover tip.</param>
    public static NSpirit Create(Func<PowerModel?> power)
    {
        var node = new NSpirit { _power = power, Scale = Vector2.One * 0.2f, Modulate = Colors.Transparent };
        node.Build();
        return node;
    }

    /// <summary>Fly at the target (re-read every frame, so it follows a moving creature), burst on it, then drift back.</summary>
    public void Strike(Func<Vector2> target)
    {
        _strikeTarget = target;
        _strikeFrom = GlobalPosition;
        _strike = 0f;
        // Above the enemies it flies past, until it's back in its slot.
        ZIndex = FlightZIndex;
    }

    /// <summary>Fade out, then free.</summary>
    public void Vanish()
    {
        _vanishing = true;
        HideTips();
        if (_hitbox != null) _hitbox.MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    private void Build()
    {
        // Purely cosmetic randomness, so GD.Randf (not the seeded run RNG) is fine here.
        _amplitude = new Vector2(3f + GD.Randf() * 3f, 4f + GD.Randf() * 4f);
        _frequency = new Vector2(0.7f + GD.Randf() * 0.8f, 1.1f + GD.Randf() * 0.9f);
        _phase = new Vector2(GD.Randf() * Mathf.Tau, GD.Randf() * Mathf.Tau);

        AddChild(CreateWisps());

        AddChild(new Label
        {
            Text = CursedSpiritsPower.SpiritDamage.ToString(),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = new Vector2(8f, 2f),
            LabelSettings = new LabelSettings
            {
                Font = RuneFont.Font,
                FontSize = 16,
                FontColor = Colors.White,
                OutlineSize = 5,
                OutlineColor = new Color(0f, 0f, 0f, 0.9f)
            }
        });

        _hitbox = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Stop,
            Size = new Vector2(HitboxSize, HitboxSize),
            Position = new Vector2(-HitboxSize / 2f, -HitboxSize / 2f)
        };
        _hitbox.MouseEntered += ShowTips;
        _hitbox.MouseExited += HideTips;
        AddChild(_hitbox);
    }

    private void ShowTips()
    {
        if (_hitbox == null || _vanishing || _power() is not { } power) return;
        NHoverTipSet.CreateAndShow(_hitbox, power.HoverTips, HoverTip.GetHoverTipAlignment(_hitbox))?.SetFollowOwner();
    }

    private void HideTips()
    {
        if (_hitbox != null) NHoverTipSet.Remove(_hitbox);
    }

    public override void _ExitTree() => HideTips();

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _time += dt;

        _appear = Mathf.MoveToward(_appear, _vanishing ? 0f : 1f, dt * AppearSpeed);
        if (_vanishing && _appear <= 0f)
        {
            QueueFree();
            return;
        }

        if (_strikeTarget != null)
        {
            _strike = Mathf.Min(1f, _strike + dt / FlightSeconds);
            var to = _strikeTarget();
            // Swoop up and over, accelerating into the hit.
            var control = (_strikeFrom + to) / 2f + Vector2.Up * (40f + _strikeFrom.DistanceTo(to) * 0.25f);
            var t = _strike * _strike;
            var u = 1f - t;
            GlobalPosition = u * u * _strikeFrom + 2f * u * t * control + t * t * to;
            if (_strike >= 1f)
            {
                _strikeTarget = null;
                Impact(to);
            }
        }
        else
        {
            var bob = new Vector2(
                Mathf.Sin(_time * _frequency.X + _phase.X) * _amplitude.X,
                Mathf.Sin(_time * _frequency.Y + _phase.Y) * _amplitude.Y);
            Position = Position.Lerp(Home + bob, 1f - Mathf.Exp(-FollowSpeed * dt));
            if (ZIndex != 0 && Position.DistanceTo(Home) < 20f) ZIndex = 0;
        }

        var appear = Mathf.Clamp(_appear, 0f, 1f);
        Scale = Vector2.One * Mathf.Lerp(0.2f, 1f, appear);
        Modulate = new Color(1f, 1f, 1f, appear);
        QueueRedraw();
    }

    public override void _Draw()
    {
        var flicker = 0.85f + Mathf.Sin(_time * 7f + _phase.X) * 0.15f;
        DrawCircle(Vector2.Zero, BodyRadius * 2f, new Color(Glow, 0.18f * flicker));

        // The tail trails below and sways; segments shrink and fade towards its tip.
        for (var k = TailSegments; k >= 1; k--)
        {
            var along = k / (float)(TailSegments + 1);
            var sway = Mathf.Sin(_time * 3.2f + _phase.Y - k * 0.7f) * 3.5f * along * 2f;
            var center = new Vector2(sway, k * 4.5f);
            DrawCircle(center, BodyRadius * (1f - along), new Color(Body, 0.75f * (1f - along)));
        }

        DrawCircle(Vector2.Zero, BodyRadius, new Color(Body, 0.9f));
        DrawCircle(new Vector2(0f, -2f), BodyRadius * 0.55f, new Color(Core, 0.55f * flicker));
        DrawCircle(new Vector2(-3.5f, -1f), 1.8f, Eyes);
        DrawCircle(new Vector2(3.5f, -1f), 1.8f, Eyes);
    }

    /// <summary>A one-shot purple burst where the spirit hits, drawn in global coordinates.</summary>
    private void Impact(Vector2 at)
    {
        var burst = new CpuParticles2D
        {
            TopLevel = true,
            GlobalPosition = at,
            OneShot = true,
            Emitting = true,
            Amount = 24,
            Lifetime = 0.5,
            Explosiveness = 0.95f,
            Spread = 180f,
            Gravity = Vector2.Zero,
            InitialVelocityMin = 60f,
            InitialVelocityMax = 160f,
            ScaleAmountMin = 2f,
            ScaleAmountMax = 5f,
            Color = Body,
            ColorRamp = Fade(Core, Glow),
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
        };
        burst.Finished += burst.QueueFree;
        AddChild(burst);
    }

    /// <summary>Faint motes shed by the wisp; global coordinates so they trail behind it as it moves.</summary>
    private static CpuParticles2D CreateWisps() => new()
    {
        Amount = 14,
        Lifetime = 0.9,
        Preprocess = 0.9,
        LocalCoords = false,
        EmissionShape = CpuParticles2D.EmissionShapeEnum.Sphere,
        EmissionSphereRadius = 9f,
        Direction = Vector2.Up,
        Spread = 60f,
        Gravity = new Vector2(0f, -20f),
        InitialVelocityMin = 4f,
        InitialVelocityMax = 14f,
        ScaleAmountMin = 1.5f,
        ScaleAmountMax = 3f,
        Color = Body,
        ColorRamp = Fade(Core, Body),
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
        ShowBehindParent = true
    };

    private static Gradient Fade(Color from, Color to)
    {
        var ramp = new Gradient();
        ramp.SetColor(0, new Color(from, 0.9f));
        ramp.SetColor(1, new Color(to, 0f));
        return ramp;
    }
}
