using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace RunePriest.RunePriestCode.Runes;

public enum DraftChange
{
    Unchanged,
    /// <summary>A new glyph stacked onto this one (values added up).</summary>
    Merged,
    Added,
    /// <summary>Taken out without being Spoken (Remove, Imbue…).</summary>
    Removed,
    /// <summary>Pushed out past the capacity: Spoken alone, immediately.</summary>
    Overflowed
}

/// <summary>One slot of an <see cref="IncantationDraft"/>, in display order.</summary>
public sealed class DraftEntry
{
    internal DraftEntry(Glyph glyph, Glyph? inscribed, DraftChange change)
    {
        Glyph = glyph;
        Inscribed = inscribed;
        Change = change;
    }

    /// <summary>The glyph as it would stand after the card is played.</summary>
    public Glyph Glyph { get; internal set; }

    /// <summary>The glyph currently in the Incantation (null for an added one).</summary>
    public Glyph? Inscribed { get; }

    public DraftChange Change { get; internal set; }

    public bool IsLive => Change is not (DraftChange.Removed or DraftChange.Overflowed);
}

/// <summary>
/// A side-effect-free copy of a player's Incantation that a card's preview plays out (drag preview): mirrors
/// <see cref="RuneCmd.Inscribe"/>, <see cref="RuneCmd.Prepend"/>, <see cref="RuneCmd.Remove"/> and
/// <see cref="RuneCmd.TakeForImbue"/>, including merging, inscription listeners (in preview mode) and Overflow.
/// Removed and overflowed glyphs keep their slot so the UI can show them leaving.
/// </summary>
public sealed class IncantationDraft
{
    private readonly List<DraftEntry> _entries;
    // Whether the Incantation power exists: Prepend needs it, Inscribe creates it.
    private bool _exists;

    private IncantationDraft(Player player, RuneBuffer? buffer)
    {
        Player = player;
        _exists = buffer != null;
        _entries = (buffer?.Glyphs ?? []).Select(g => new DraftEntry(g, g, DraftChange.Unchanged)).ToList();
    }

    public static IncantationDraft For(Player player) => new(player, RuneCmd.GetBuffer(player.Creature));

    public Player Player { get; }

    public IReadOnlyList<DraftEntry> Entries => _entries;

    public bool HasChanges => _entries.Any(e => e.Change != DraftChange.Unchanged);

    /// <summary>The Incantation after the card is played.</summary>
    public IReadOnlyList<Glyph> Result => Live.Select(e => e.Glyph).ToList();

    private List<DraftEntry> Live => _entries.Where(e => e.IsLive).ToList();

    public void Inscribe(IEnumerable<Glyph> glyphs, CardModel? source)
    {
        IReadOnlyList<Glyph> list = glyphs.Select(g => g.InscribedBy(source)).ToList();
        if (list.Count == 0) return;
        _exists = true;
        foreach (var listener in RuneListeners.Of(Player))
            list = listener.ModifyInscription(Player, list, preview: true);
        if (list.Count == 0) return;
        list = Glyph.HeavierInCast(list);
        GrowHeavy(list);

        for (var i = 0; i < list.Count; i++)
        {
            var live = Live;
            var last = live.Count > 0 ? live[^1] : null;
            if (i == 0 && last != null && !IsGrowing(live, live.Count - 1) && last.Glyph.MergeWith(list[i]) is { } merged)
            {
                last.Glyph = merged;
                if (last.Change == DraftChange.Unchanged) last.Change = DraftChange.Merged;
            }
            else
            {
                _entries.Add(new DraftEntry(list[i], null, DraftChange.Added));
            }
            Overflow();
        }
    }

    public void Prepend(IEnumerable<Glyph> glyphs, CardModel? source)
    {
        var list = glyphs.Select(g => g.InscribedBy(source))
            .Select(g => new DraftEntry(g, null, DraftChange.Added)).ToList();
        if (list.Count == 0 || !_exists) return;
        GrowHeavy(list.Select(e => e.Glyph));
        _entries.InsertRange(0, list);
        Overflow();
    }

    /// <summary>Index -1 removes the most recently inscribed glyph.</summary>
    public void Remove(int index = -1)
    {
        var live = Live;
        var i = index < 0 ? live.Count - 1 : index;
        if (i >= 0 && i < live.Count) Drop(live[i]);
    }

    public void RemoveWhere(Func<Glyph, bool> match)
    {
        foreach (var entry in Live.Where(e => match(e.Glyph))) Drop(entry);
    }

    /// <summary>Up to <paramref name="count"/> of the most recent glyphs matching <paramref name="filter"/>.</summary>
    public void TakeForImbue(int count, Func<Glyph, bool>? filter = null)
    {
        if (count <= 0) return;
        foreach (var entry in Enumerable.Reverse(Live).Where(e => filter?.Invoke(e.Glyph) ?? true).Take(count).ToList())
            Drop(entry);
    }

    private void Drop(DraftEntry entry)
    {
        if (entry.Change == DraftChange.Added) _entries.Remove(entry);
        else entry.Change = DraftChange.Removed;
    }

    /// <summary>Mirrors <see cref="RuneBuffer.CountInscribed"/>: heavy Strikes already inscribed grow for every new rune.</summary>
    private void GrowHeavy(IEnumerable<Glyph> glyphs)
    {
        var runes = glyphs.Sum(g => g.Runes.Count);
        foreach (var entry in Live.Where(e => e.Glyph.IsHeavy))
        {
            entry.Glyph = entry.Glyph.Heavier(runes);
            if (entry.Change == DraftChange.Unchanged) entry.Change = DraftChange.Merged;
        }
    }

    private void Overflow()
    {
        var capacity = RuneListeners.Capacity(Player);
        if (capacity == null) return;
        var live = Live;
        for (var i = 0; i < live.Count - capacity.Value; i++)
            live[i].Change = DraftChange.Overflowed;
    }

    /// <summary>Same rule as <see cref="RuneBuffer.IsGrowing"/>.</summary>
    private static bool IsGrowing(List<DraftEntry> live, int index) =>
        index > 0 && index < live.Count && live[index - 1].Glyph is { Kind: RuneKind.Modifier } previous &&
        previous.Runes[0] is GrowingRune;
}
