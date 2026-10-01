using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Nodes;

/// <summary>
/// Floating row of glyphs above a player's head, mirroring their <see cref="RuneBuffer"/>.
/// Attached to every player <see cref="NCreature"/> by <c>NCreatureRuneBufferPatch</c>; invisible while empty.
/// </summary>
public partial class NRuneBuffer : Node2D
{
    /// <summary>First glyph (spoken first) sits on the right when true.</summary>
    public const bool ReadRightToLeft = false;

    private const float HeightAboveHead = 70f;
    private const float GlyphSpacing = 58f;
    private const float MinGlyphSpacing = 34f;
    private const int GlyphsBeforeCompressing = 9;
    // Co-op: runes of whichever player isn't in focus fade back so long rows don't clutter each other.
    private const float UnfocusedAlpha = 0.3f;
    private const float FadeSpeed = 8f;

    private static readonly List<NRuneBuffer> Instances = [];

    private readonly List<NGlyph> _glyphs = [];
    private NCreature _creatureNode = null!;
    private RuneBuffer? _buffer;
    private int? _capacity;

    public static NRuneBuffer Create(NCreature creatureNode) => new() { _creatureNode = creatureNode };

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(_creatureNode)) return;

        GlobalPosition = _creatureNode.GetTopOfHitbox() + Vector2.Up * HeightAboveHead;

        var inCombat = _creatureNode.Entity.CombatState != null;
        var buffer = inCombat ? RuneCmd.GetBuffer(_creatureNode.Entity) : null;
        if (buffer != _buffer) Bind(buffer);

        var player = _creatureNode.Entity.Player;
        var capacity = inCombat && player != null ? RuneListeners.Capacity(player) : null;
        if (capacity != _capacity)
        {
            _capacity = capacity;
            Relayout();
        }

        var alpha = IsInFocus() ? 1f : UnfocusedAlpha;
        Modulate = new Color(Modulate, Mathf.MoveToward(Modulate.A, alpha, (float)delta * FadeSpeed));
    }

    /// <summary>Mine, unless a remote player is hovered; a remote player's only while they're hovered.</summary>
    private bool IsInFocus()
    {
        if (!LocalContext.IsMe(_creatureNode.Entity)) return _creatureNode.IsFocused;
        return !Instances.Any(b => b != this && IsInstanceValid(b._creatureNode) && b._creatureNode.IsFocused &&
                                   !LocalContext.IsMe(b._creatureNode.Entity));
    }

    public override void _Draw()
    {
        if (_capacity is not { } capacity) return;
        var slots = Math.Max(capacity, _glyphs.Count);
        for (var i = _glyphs.Count; i < capacity; i++)
            DrawArc(SlotPosition(i, slots), 18f, 0f, Mathf.Tau, 24, new Color(1f, 1f, 1f, 0.25f), 2f);
    }

    public override void _EnterTree() => Instances.Add(this);

    public override void _ExitTree()
    {
        Instances.Remove(this);
        Unsubscribe();
    }

    private void Bind(RuneBuffer? buffer)
    {
        Unsubscribe();
        _buffer = buffer;

        if (_buffer != null)
        {
            _buffer.Changed += Sync;
            _buffer.GlyphActivated += OnActivated;
            _buffer.GlyphFizzled += OnFizzled;
            _buffer.GlyphReplaced += OnReplaced;
            _buffer.SpeakEnded += Sync;
        }

        Sync();
    }

    private void Unsubscribe()
    {
        if (_buffer == null) return;
        _buffer.Changed -= Sync;
        _buffer.GlyphActivated -= OnActivated;
        _buffer.GlyphFizzled -= OnFizzled;
        _buffer.GlyphReplaced -= OnReplaced;
        _buffer.SpeakEnded -= Sync;
    }

    private void Sync()
    {
        var current = _buffer?.Glyphs ?? [];
        var speaking = _buffer?.IsSpeaking ?? false;

        // While speaking, the buffer is emptied up front; keep showing those glyphs until the Speak ends.
        if (!speaking)
        {
            var stale = _glyphs.Where(n => !current.Contains(n.Glyph)).ToList();
            for (var i = 0; i < stale.Count; i++)
            {
                stale[i].Dissolve(i * 0.06f);
                _glyphs.Remove(stale[i]);
            }
        }

        foreach (var glyph in current.Where(g => _glyphs.All(n => n.Glyph != g)))
        {
            var node = NGlyph.Create(glyph, SlotPosition(_glyphs.Count, _glyphs.Count + 1));
            _glyphs.Add(node);
            AddChild(node);
        }

        if (!speaking)
        {
            _glyphs.Sort((a, b) => IndexIn(current, a.Glyph).CompareTo(IndexIn(current, b.Glyph)));
            foreach (var node in _glyphs) node.ResetSpent();
        }

        Relayout();
    }

    private void Relayout()
    {
        var slots = Math.Max(_glyphs.Count, _capacity ?? 0);
        for (var i = 0; i < _glyphs.Count; i++)
            _glyphs[i].TargetPosition = SlotPosition(i, slots);
        QueueRedraw();
    }

    private Vector2 SlotPosition(int index, int count)
    {
        var spacing = count <= GlyphsBeforeCompressing
            ? GlyphSpacing
            : Mathf.Max(MinGlyphSpacing, GlyphSpacing * GlyphsBeforeCompressing / count);
        var slot = ReadRightToLeft ? count - 1 - index : index;
        return new Vector2((slot - (count - 1) / 2f) * spacing, 0f);
    }

    private static int IndexIn(IReadOnlyList<Glyph> glyphs, Glyph glyph)
    {
        for (var i = 0; i < glyphs.Count; i++)
            if (glyphs[i] == glyph) return i;
        return int.MaxValue;
    }

    private void OnActivated(Glyph glyph) => _glyphs.FirstOrDefault(n => n.Glyph == glyph)?.Activate();

    private void OnFizzled(Glyph glyph) => _glyphs.FirstOrDefault(n => n.Glyph == glyph)?.Fizzle();

    private void OnReplaced(Glyph old, Glyph merged) => _glyphs.FirstOrDefault(n => n.Glyph == old)?.Rebuild(merged);
}
