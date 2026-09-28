using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace RunePriest.RunePriestCode.Runes;

/// <summary>
/// One slot in the Incantation: what a single card inscribes. Either 1+ payload runes, or exactly one
/// modifier/target/flow rune. Any other combination is malformed and fizzles when spoken.
/// </summary>
public sealed class Glyph
{
    private Glyph(IReadOnlyList<Rune> runes, Creature? anchor, CardModel? source, int imbued = 0)
    {
        Runes = runes;
        Anchor = anchor;
        Source = source;
        Imbued = imbued;
    }

    public IReadOnlyList<Rune> Runes { get; }

    /// <summary>The creature targeted when the glyph's card was played, if any.</summary>
    public Creature? Anchor { get; }

    public CardModel? Source { get; }

    /// <summary>How many times this glyph has been Imbued (its values already include the bonus).</summary>
    public int Imbued { get; }

    public RuneKind? Kind
    {
        get
        {
            if (Runes.Count == 0) return null;
            if (Runes.All(r => r.Kind == RuneKind.Payload)) return RuneKind.Payload;
            return Runes.Count == 1 ? Runes[0].Kind : null;
        }
    }

    /// <summary>Imbue only empowers payload glyphs that carry a scalable value.</summary>
    public bool CanImbue => Kind == RuneKind.Payload && Runes.Any(r => r is PayloadRune { Scalable: true });

    public string Label => string.Join(", ", Runes.Select(r => r.Label));

    public static Glyph Of(params Rune[] runes) => new(runes, null, null);

    public Glyph AnchoredTo(Creature? anchor) => new(Runes, anchor, Source, Imbued);

    public Glyph WithSource(CardModel? source) => new(Runes, Anchor, source, Imbued);

    /// <summary>A new, distinct glyph with the same runes, anchor and source.</summary>
    public Glyph Copy() => new(Runes, Anchor, Source, Imbued);

    /// <summary>Adds <paramref name="bonus"/> to every scalable payload rune and marks the glyph as Imbued.</summary>
    public Glyph Imbue(int bonus) => new(Empowered(bonus), Anchor, Source, Imbued + 1);

    /// <summary>Adds <paramref name="bonus"/> to every scalable payload rune without counting as an Imbue.</summary>
    public Glyph Empower(int bonus) => new(Empowered(bonus), Anchor, Source, Imbued);

    private List<Rune> Empowered(int bonus) =>
        Runes.Select(r => r is PayloadRune { Scalable: true } ? r.WithValue(r.Value + bonus) ?? r : r).ToList();

    /// <summary>Multiplies every mergeable rune's value (Strike, Loop, Echo, Amplify…). Valueless runes are unchanged.</summary>
    public Glyph Scaled(int factor) =>
        new(Runes.Select(r => r.WithValue(r.Value * factor) ?? r).ToList(), Anchor, Source, Imbued);

    /// <summary>Replaces every payload rune with a single Mend worth their combined value.</summary>
    public Glyph AsMend()
    {
        if (Kind != RuneKind.Payload) return this;
        return new Glyph([new MendRune(Runes.Sum(r => r.Value))], Anchor, Source, Imbued);
    }

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
        return new Glyph(merged, Anchor, Source, Imbued + next.Imbued);
    }

    public override string ToString() => $"[{string.Join(" + ", Runes)}]";
}
