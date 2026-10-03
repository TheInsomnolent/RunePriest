using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Nodes;

/// <summary>One slot of the Incantation. Compound glyphs stack their runes vertically (they resolve together).</summary>
public partial class NGlyph : Node2D
{
    public const float StackSpacing = 46f;

    private const float HitboxSize = 52f;
    private const float FollowSpeed = 10f;
    private const float SpentAlpha = 0.55f;
    private const float FizzledAlpha = 0.25f;

    private readonly List<NRuneSymbol> _symbols = [];
    private readonly List<NRuneArc> _arcs = [];
    private readonly List<NCreature> _highlighted = [];
    private (IReadOnlyCollection<Creature> Creatures, bool Random)? _targets;
    private bool _hovered;
    private Control? _hitbox;
    private float _pulse;
    private float _fizzle;
    private float _appear;
    private bool _spent;
    private bool _fizzled;
    private bool _dissolving;
    private float _dissolveDelay;

    public Glyph Glyph { get; private set; } = null!;

    public Vector2 TargetPosition { get; set; }

    private bool _persisting;

    /// <summary>Shows the persistence mandala: this glyph (or its carry-over) outlasts the next Speak.</summary>
    public bool Persisting
    {
        get => _persisting;
        set
        {
            _persisting = value;
            foreach (var symbol in _symbols) symbol.Persisting = value;
        }
    }

    /// <summary>Rune values after game effects, from a dry run of the Incantation (null: show inscribed values).</summary>
    public void ShowModifiedValues(RunePreview? forecast)
    {
        for (var i = 0; i < _symbols.Count; i++)
            _symbols[i].ShowModifiedValue(forecast?.ShownValue(Glyph, i));
    }

    /// <summary>Who this glyph would affect, from the same dry run; highlighted with arcs while hovered.</summary>
    public void ShowTargets((IReadOnlyCollection<Creature> Creatures, bool Random)? targets)
    {
        var same = targets is { } next && _targets is { } current && next.Random == current.Random &&
                   next.Creatures.Count == current.Creatures.Count && next.Creatures.All(current.Creatures.Contains);
        _targets = targets;
        // Forecasts refresh often; only rebuild the arcs when the targets actually change, so they don't flicker.
        if (_hovered && !same) HighlightTargets();
    }

    public static NGlyph Create(Glyph glyph, Vector2 startPosition)
    {
        var node = new NGlyph { Position = startPosition, TargetPosition = startPosition };
        node.BuildSymbols(glyph);
        node.Scale = Vector2.One * 0.2f;
        node.Modulate = Colors.Transparent;
        return node;
    }

    /// <summary>Swap to a merged glyph in place, with a flourish.</summary>
    public void Rebuild(Glyph glyph)
    {
        foreach (var symbol in _symbols) symbol.QueueFree();
        _symbols.Clear();
        BuildSymbols(glyph);
        _pulse = 1f;
        foreach (var symbol in _symbols) symbol.Burst();
    }

    private void BuildSymbols(Glyph glyph)
    {
        Glyph = glyph;
        for (var i = 0; i < glyph.Runes.Count; i++)
        {
            var symbol = NRuneSymbol.Create(glyph.Runes[i]);
            symbol.Persisting = _persisting;
            symbol.Home = new Vector2(0f, -i * StackSpacing);
            _symbols.Add(symbol);
            AddChild(symbol);
        }
        BuildHitbox(glyph.Runes.Count);
    }

    private void BuildHitbox(int runeCount)
    {
        if (_hitbox == null)
        {
            _hitbox = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
            _hitbox.MouseEntered += ShowTips;
            _hitbox.MouseExited += HideTips;
            AddChild(_hitbox);
        }

        var height = HitboxSize + (runeCount - 1) * StackSpacing;
        _hitbox.Size = new Vector2(HitboxSize, height);
        _hitbox.Position = new Vector2(-HitboxSize / 2f, HitboxSize / 2f - height);
    }

    private void ShowTips()
    {
        if (_hitbox == null || _dissolving) return;
        var tips = Glyph.Runes.DistinctBy(r => r.Key).SelectMany(r => r.HoverTips).ToList();
        NHoverTipSet.CreateAndShow(_hitbox, tips, HoverTip.GetHoverTipAlignment(_hitbox))?.SetFollowOwner();
        _hovered = true;
        HighlightTargets();
    }

    private void HideTips()
    {
        if (_hitbox != null) NHoverTipSet.Remove(_hitbox);
        _hovered = false;
        ClearTargets();
    }

    /// <summary>Puts the targeting reticle on every creature this glyph would affect and streams an arc to each.</summary>
    private void HighlightTargets()
    {
        ClearTargets();
        if (_targets is not { } targets || NCombatRoom.Instance is not { } room) return;

        var rune = Glyph.Runes.FirstOrDefault(r => r is PayloadRune) ?? Glyph.Runes.FirstOrDefault();
        if (rune == null) return;
        var color = RuneVisuals.ColorOf(rune);
        foreach (var creature in targets.Creatures)
        {
            var node = room.GetCreatureNode(creature);
            if (node == null || !IsInstanceValid(node)) continue;
            node.ShowSingleSelectReticle();
            _highlighted.Add(node);

            var last = node.Hitbox.GetGlobalRect().GetCenter();
            var arc = NRuneArc.Create(() => GlobalPosition,
                () => IsInstanceValid(node) ? last = node.Hitbox.GetGlobalRect().GetCenter() : last, color, targets.Random);
            _arcs.Add(arc);
            AddChild(arc);
        }
    }

    private void ClearTargets()
    {
        foreach (var node in _highlighted.Where(IsInstanceValid)) node.HideSingleSelectReticle();
        _highlighted.Clear();
        foreach (var arc in _arcs.Where(IsInstanceValid)) arc.Release();
        _arcs.Clear();
    }

    public override void _ExitTree() => HideTips();

    public void Activate()
    {
        _pulse = 1f;
        _spent = true;
        foreach (var symbol in _symbols) symbol.Burst();
    }

    /// <summary>The glyph fizzles out: a grey puff, then it lingers faded until the Speak ends.</summary>
    public void Fizzle()
    {
        _fizzle = 1f;
        _spent = true;
        _fizzled = true;
        EmitFizzlePuff();
    }

    private void EmitFizzlePuff()
    {
        var puff = new CpuParticles2D
        {
            OneShot = true,
            Emitting = true,
            Amount = 14,
            Lifetime = 0.6f,
            Explosiveness = 0.9f,
            Direction = Vector2.Up,
            Spread = 70f,
            InitialVelocityMin = 30f,
            InitialVelocityMax = 90f,
            Gravity = new Vector2(0f, -40f),
            ScaleAmountMin = 2f,
            ScaleAmountMax = 5f,
            Color = new Color(0.6f, 0.6f, 0.6f, 0.8f)
        };
        puff.Finished += puff.QueueFree;
        AddChild(puff);
    }

    /// <summary>Glyph survived a Speak (Seal / Eternal Script); show it as fresh again.</summary>
    public void ResetSpent()
    {
        _spent = false;
        _fizzled = false;
    }

    public void Dissolve(float delay = 0f)
    {
        if (_dissolving) return;
        _dissolving = true;
        _dissolveDelay = delay;
        HideTips();
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _pulse = Mathf.MoveToward(_pulse, 0f, dt * 2.5f);
        _fizzle = Mathf.MoveToward(_fizzle, 0f, dt * 1.5f);

        if (_dissolving && (_dissolveDelay -= dt) <= 0f)
        {
            _appear -= dt * 3f;
            TargetPosition += Vector2.Up * 60f * dt;
            if (_appear <= 0f)
            {
                QueueFree();
                return;
            }
        }
        else
        {
            _appear = Mathf.MoveToward(_appear, 1f, dt * 3f);
        }

        Position = Position.Lerp(TargetPosition, 1f - Mathf.Exp(-FollowSpeed * dt));
        if (_fizzle > 0f) Position += new Vector2((GD.Randf() - 0.5f) * 6f * _fizzle, 0f);

        var alpha = _fizzled ? Mathf.Lerp(FizzledAlpha, 1f, _fizzle)
            : _spent ? Mathf.Lerp(SpentAlpha, 1f, _pulse) : 1f;
        var grey = _fizzled ? Mathf.Max(_fizzle, 0.7f) : _fizzle;
        var tint = Colors.White.Lerp(new Color(0.5f, 0.5f, 0.5f), grey);
        var glow = 1f + _pulse * 0.8f;

        var appear = Mathf.Clamp(_appear, 0f, 1f);
        Scale = Vector2.One * (Mathf.Lerp(0.2f, 1f, appear) * (1f + _pulse * 0.35f));
        Modulate = new Color(tint.R * glow, tint.G * glow, tint.B * glow, alpha * appear);
    }
}
