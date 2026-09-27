using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace RunePriest.RunePriestCode.Runes;

/// <summary>
/// One slot in the Incantation: what a single card inscribes. Either 1+ payload runes, or exactly one
/// modifier/target/flow rune. Any other combination is malformed and fizzles when spoken.
/// </summary>
public sealed class Glyph
{
    private Glyph(IReadOnlyList<Rune> runes, Creature? anchor, CardModel? source)
    {
        Runes = runes;
        Anchor = anchor;
        Source = source;
    }

    public IReadOnlyList<Rune> Runes { get; }

    /// <summary>The creature targeted when the glyph's card was played, if any.</summary>
    public Creature? Anchor { get; }

    public CardModel? Source { get; }

    public RuneKind? Kind
    {
        get
        {
            if (Runes.Count == 0) return null;
            if (Runes.All(r => r.Kind == RuneKind.Payload)) return RuneKind.Payload;
            return Runes.Count == 1 ? Runes[0].Kind : null;
        }
    }

    public string Label => string.Join(", ", Runes.Select(r => r.Label));

    public static Glyph Of(params Rune[] runes) => new(runes, null, null);

    public Glyph AnchoredTo(Creature? anchor) => new(Runes, anchor, Source);

    public Glyph WithSource(CardModel? source) => new(Runes, Anchor, source);

    /// <summary>A new, distinct glyph with the same runes, anchor and source.</summary>
    public Glyph Copy() => new(Runes, Anchor, Source);

    /// <summary>
    /// Merges a glyph inscribed right after this one: same kind, same runes in the same order, same anchor.
    /// Values add up. Returns null if they can't merge.
    /// </summary>
    public Glyph? MergeWith(Glyph next)
    {
        if (Kind == null || Kind != next.Kind || Anchor != next.Anchor || Runes.Count != next.Runes.Count) return null;

        var merged = new Rune[Runes.Count];
        for (var i = 0; i < Runes.Count; i++)
        {
            if (Runes[i].Key != next.Runes[i].Key) return null;
            var rune = Runes[i].WithValue(Runes[i].Value + next.Runes[i].Value);
            if (rune == null) return null;
            merged[i] = rune;
        }
        return new Glyph(merged, Anchor, Source);
    }

    public override string ToString() => $"[{string.Join(" + ", Runes)}]";
}
