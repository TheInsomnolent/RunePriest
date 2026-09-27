using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Nodes;

/// <summary>One slot of the Incantation. Compound glyphs stack their runes vertically (they resolve together).</summary>
public partial class NGlyph : Node2D
{
    public const float StackSpacing = 46f;

    private const float HitboxSize = 52f;
    private const float FollowSpeed = 10f;
    private const float SpentAlpha = 0.55f;

    private readonly List<NRuneSymbol> _symbols = [];
    private Control? _hitbox;
    private float _pulse;
    private float _fizzle;
    private float _appear;
    private bool _spent;
    private bool _dissolving;
    private float _dissolveDelay;

    public Glyph Glyph { get; private set; } = null!;

    public Vector2 TargetPosition { get; set; }

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
    }

    private void HideTips()
    {
        if (_hitbox != null) NHoverTipSet.Remove(_hitbox);
    }

    public override void _ExitTree() => HideTips();

    public void Activate()
    {
        _pulse = 1f;
        _spent = true;
        foreach (var symbol in _symbols) symbol.Burst();
    }

    public void Fizzle()
    {
        _fizzle = 1f;
        _spent = true;
    }

    /// <summary>Glyph survived a Speak (Seal / Eternal Script); show it as fresh again.</summary>
    public void ResetSpent() => _spent = false;

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

        var alpha = _spent ? Mathf.Lerp(SpentAlpha, 1f, _pulse) : 1f;
        var tint = Colors.White.Lerp(new Color(0.5f, 0.5f, 0.5f), _fizzle);
        var glow = 1f + _pulse * 0.8f;

        var appear = Mathf.Clamp(_appear, 0f, 1f);
        Scale = Vector2.One * (Mathf.Lerp(0.2f, 1f, appear) * (1f + _pulse * 0.35f));
        Modulate = new Color(tint.R * glow, tint.G * glow, tint.B * glow, alpha * appear);
    }
}
