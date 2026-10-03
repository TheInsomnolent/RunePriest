using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Nodes;

/// <summary>
/// Floating row of glyphs above a player's head, mirroring their <see cref="RuneBuffer"/>.
/// Attached to every player <see cref="NCreature"/> by <c>NCreatureRuneBufferPatch</c>; invisible while empty.
/// While the player holds a card that changes the Incantation (<see cref="RuneDragPreview"/>), desaturated ghost glyphs
/// show the result: new glyphs, glyphs merged into the last one, and glyphs that would leave.
/// </summary>
public partial class NRuneBuffer : Node2D
{
    /// <summary>First glyph (spoken first) sits on the right when true.</summary>
    public const bool ReadRightToLeft = false;

    private const float HeightAboveHead = 70f;
    private const float GlyphSpacing = 58f;
    // Longer Incantations wrap like text: the first rune is top-left and the newest row sits just above the head.
    private const int GlyphsPerRow = 9;
    // Between rows, on top of however high the row below's compound glyphs stack.
    private const float RowSpacing = 64f;
    // Co-op: runes of whichever player isn't in focus fade back so long rows don't clutter each other.
    private const float UnfocusedAlpha = 0.3f;
    private const float FadeSpeed = 8f;

    private static readonly List<NRuneBuffer> Instances = [];

    private readonly List<NGlyph> _glyphs = [];
    private readonly List<NGlyph> _ghosts = [];
    // Created since the last layout; they appear in their slot rather than sliding in.
    private readonly List<NGlyph> _fresh = [];
    private Vector2[] _slots = [];
    // Drag preview slots; a merge ghost shares its slot with the glyph it replaces.
    private readonly List<(NGlyph Node, int Slot)> _previewLayout = [];
    private IncantationDraft? _draft;
    private int _previewVersion = -1;
    private int _shownSlots;
    private NCreature _creatureNode = null!;
    private RuneBuffer? _buffer;
    private int? _capacity;
    private bool _keepsIncantation;

    public static NRuneBuffer Create(NCreature creatureNode) => new() { _creatureNode = creatureNode };

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(_creatureNode)) return;

        // HeightAboveHead is tuned for a full-size model; shrink it to match downscaled visuals (e.g. the Architect rig).
        var visualScale = Mathf.Abs(_creatureNode.Visuals.Scale.Y);
        GlobalPosition = _creatureNode.GetTopOfHitbox() + Vector2.Up * HeightAboveHead * visualScale;

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

        var keeps = inCombat && player != null && RuneListeners.Of(player).Any(l => l.KeepsIncantation);
        if (keeps != _keepsIncantation)
        {
            _keepsIncantation = keeps;
            UpdateForecast();
        }

        RuneDragPreview.Poll();
        if (RuneDragPreview.Version != _previewVersion)
        {
            _previewVersion = RuneDragPreview.Version;
            UpdatePreview();
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
        if (_capacity == null) return;
        for (var i = _shownSlots; i < _slots.Length; i++)
            DrawArc(_slots[i], 18f, 0f, Mathf.Tau, 24, new Color(1f, 1f, 1f, 0.25f), 2f);
    }

    public override void _EnterTree()
    {
        Instances.Add(this);
        CombatManager.Instance.StateTracker.CombatStateChanged += OnCombatStateChanged;
    }

    public override void _ExitTree()
    {
        Instances.Remove(this);
        CombatManager.Instance.StateTracker.CombatStateChanged -= OnCombatStateChanged;
        Unsubscribe();
    }

    // Powers changed (Weak, Frail, Strength…): rune values and outcomes may have too, and so may a held card's runes.
    private void OnCombatStateChanged(CombatState _) => UpdatePreview();

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
            var node = NGlyph.Create(glyph, Vector2.Zero);
            _fresh.Add(node);
            _glyphs.Add(node);
            AddChild(node);
        }

        if (!speaking)
        {
            _glyphs.Sort((a, b) => IndexIn(current, a.Glyph).CompareTo(IndexIn(current, b.Glyph)));
            foreach (var node in _glyphs) node.ResetSpent();
        }

        BuildPreview();
        if (!speaking)
        {
            UpdateForecast();
            UpdateOtherForecasts();
        }

        Relayout();
    }

    private void UpdatePreview()
    {
        BuildPreview();
        Relayout();
        UpdateForecast();
    }

    /// <summary>
    /// Lays out the held card's <see cref="IncantationDraft"/>: ghosts for new and merged glyphs (reusing identical ghosts
    /// so they don't flicker as the preview refreshes), the glyphs they merge into hidden, and leaving glyphs greyed.
    /// </summary>
    private void BuildPreview()
    {
        _draft = _buffer?.IsSpeaking != true && IsInstanceValid(_creatureNode)
            ? RuneDragPreview.DraftFor(_creatureNode.Entity)
            : null;

        foreach (var node in _glyphs) node.Preview = GlyphPreview.None;
        var pool = _ghosts.ToList();
        _ghosts.Clear();
        _previewLayout.Clear();

        if (_draft != null)
        {
            var entries = _draft.Entries;
            for (var slot = 0; slot < entries.Count; slot++)
            {
                var entry = entries[slot];
                var inscribed = entry.Inscribed == null ? null : _glyphs.FirstOrDefault(n => n.Glyph == entry.Inscribed);
                if (inscribed != null)
                {
                    inscribed.Preview = entry.Change switch
                    {
                        DraftChange.Merged => GlyphPreview.Superseded,
                        DraftChange.Removed or DraftChange.Overflowed => GlyphPreview.Leaving,
                        _ => GlyphPreview.None
                    };
                    _previewLayout.Add((inscribed, slot));
                }
                if (entry.Change != DraftChange.Merged && entry.Inscribed != null) continue;

                var ghost = TakeGhost(pool, entry.Glyph);
                if (ghost == null)
                {
                    // A merge ghost grows out of the glyph it replaces.
                    ghost = NGlyph.Create(entry.Glyph, inscribed?.Position ?? Vector2.Zero, ghost: true);
                    if (inscribed == null) _fresh.Add(ghost);
                    AddChild(ghost);
                }
                ghost.Preview = entry.IsLive ? GlyphPreview.None : GlyphPreview.Leaving;
                _ghosts.Add(ghost);
                _previewLayout.Add((ghost, slot));
            }
        }

        foreach (var stale in pool) stale.Dissolve();
    }

    private static NGlyph? TakeGhost(List<NGlyph> pool, Glyph glyph)
    {
        var index = pool.FindIndex(n => LooksSame(n.Glyph, glyph));
        if (index < 0) return null;
        var ghost = pool[index];
        pool.RemoveAt(index);
        ghost.Rebind(glyph);
        return ghost;
    }

    private static bool LooksSame(Glyph a, Glyph b) =>
        a.Persistent == b.Persistent && a.Runes.Count == b.Runes.Count &&
        a.Runes.Zip(b.Runes).All(p => p.First.Key == p.Second.Key && p.First.Value == p.Second.Value);

    /// <summary>Dry-runs the Incantation to frame glyphs that outlast the Speak and show rune values after game effects.</summary>
    private void UpdateForecast()
    {
        if (_buffer?.IsSpeaking == true || !IsInstanceValid(_creatureNode) ||
            _creatureNode.Entity.Player is not { } player) return;
        // While a card is held, forecast the Incantation as it would be after playing it.
        var glyphs = _draft?.Result ?? _buffer?.Glyphs ?? [];
        var forecast = glyphs.Count > 0 ? RuneCmd.Forecast(player, glyphs) : null;
        var keepsAll = RuneListeners.Of(player).Any(l => l.KeepsIncantation);
        foreach (var node in _glyphs.Concat(_ghosts))
        {
            node.Persisting = keepsAll || forecast?.Persisting.Contains(node.Glyph) == true;
            node.ShowModifiedValues(forecast);
            node.ShowTargets(forecast?.TargetsOf(node.Glyph));
        }
    }

    /// <summary>
    /// Co-op: teammates Speak in combat order, so what one player inscribes (a Hex…) changes the values previewed
    /// over the players who Speak after them.
    /// </summary>
    private void UpdateOtherForecasts()
    {
        foreach (var other in Instances.Where(b => b != this && IsInstanceValid(b)))
            other.UpdateForecast();
    }

    private void Relayout()
    {
        _shownSlots = _draft?.Entries.Count ?? _glyphs.Count;
        var stacks = new int[Math.Max(_shownSlots, _capacity ?? 0)];
        Array.Fill(stacks, 1);
        if (_draft == null)
        {
            for (var i = 0; i < _glyphs.Count; i++) stacks[i] = _glyphs[i].Glyph.Runes.Count;
        }
        else
        {
            foreach (var (node, slot) in _previewLayout) stacks[slot] = Math.Max(stacks[slot], node.Glyph.Runes.Count);
        }

        _slots = SlotPositions(stacks);
        if (_draft == null)
        {
            for (var i = 0; i < _glyphs.Count; i++) _glyphs[i].TargetPosition = _slots[i];
        }
        else
        {
            foreach (var (node, slot) in _previewLayout) node.TargetPosition = _slots[slot];
        }

        foreach (var node in _fresh) node.Position = node.TargetPosition;
        _fresh.Clear();
        QueueRedraw();
    }

    /// <summary>
    /// One row, centred, up to <see cref="GlyphsPerRow"/> glyphs; beyond that a grid read like text (left to right, top
    /// to bottom) whose last row sits just above the head, so earlier rows climb as the Incantation grows.
    /// </summary>
    /// <param name="stacks">Runes in each slot's glyph: compound glyphs stack upward, so the row above must clear them.</param>
    private static Vector2[] SlotPositions(IReadOnlyList<int> stacks)
    {
        var count = stacks.Count;
        var positions = new Vector2[count];
        var columns = Math.Min(count, GlyphsPerRow);
        var rows = (count + GlyphsPerRow - 1) / GlyphsPerRow;
        var y = 0f;
        for (var row = rows - 1; row >= 0; row--)
        {
            var start = row * GlyphsPerRow;
            var end = Math.Min(start + GlyphsPerRow, count);
            for (var i = start; i < end; i++)
            {
                var column = ReadRightToLeft ? columns - 1 - (i - start) : i - start;
                positions[i] = new Vector2((column - (columns - 1) / 2f) * GlyphSpacing, y);
            }
            var tallest = Enumerable.Range(start, end - start).Max(i => stacks[i]);
            y -= RowSpacing + (tallest - 1) * NGlyph.StackSpacing;
        }
        return positions;
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
