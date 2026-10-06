using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using RunePriest.RunePriestCode.Cards;

namespace RunePriest.RunePriestCode.Runes;

/// <summary>
/// One slot in the Incantation: what a single card inscribes. Either 1+ payload runes, or exactly one
/// modifier/target/flow rune. Any other combination is malformed and fizzles when spoken.
/// </summary>
public sealed class Glyph
{
    private Glyph(IReadOnlyList<Rune> runes, Creature? anchor, CardModel? source, bool persistent = false)
    {
        Runes = runes;
        Anchor = anchor;
        Source = source;
        Persistent = persistent;
    }

    public IReadOnlyList<Rune> Runes { get; }

    /// <summary>The creature targeted when the glyph's card was played, if any.</summary>
    public Creature? Anchor { get; }

    public CardModel? Source { get; }

    /// <summary>Whether this glyph stays in the Incantation after being Spoken (the Persist keyword).</summary>
    public bool Persistent { get; }

    public RuneKind? Kind
    {
        get
        {
            if (Runes.Count == 0) return null;
            if (Runes.All(r => r.Kind == RuneKind.Payload)) return RuneKind.Payload;
            return Runes.Count == 1 ? Runes[0].Kind : null;
        }
    }

    /// <summary>Empower only affects payload glyphs that carry an amplifiable value.</summary>
    public bool CanEmpower => Kind == RuneKind.Payload && Runes.Any(r => r is PayloadRune { Amplifiable: true });

    public string Label => string.Join(", ", Runes.Select(r => r.Label));

    public static Glyph Of(params Rune[] runes) => new(runes, null, null);

    public Glyph AnchoredTo(Creature? anchor) => new(Runes, anchor, Source, Persistent);

    public Glyph WithSource(CardModel? source) => new(Runes, Anchor, source, Persistent);

    /// <summary>Every rune made <see cref="Rune.Radiant"/> (inscribed by an Ascended card).</summary>
    public Glyph Radiant() => Runes.All(r => r.Radiant) ? this : WithRunes(Runes.Select(r => r.AsRadiant()).ToArray());

    /// <summary>
    /// Attributes an unsourced glyph to the card inscribing it; runes an Ascended card creates turn
    /// <see cref="Rune.Radiant"/>. Glyphs that already have a source (copies of existing runes) are unchanged.
    /// </summary>
    public Glyph InscribedBy(CardModel? source)
    {
        if (Source != null) return this;
        var glyph = WithSource(source);
        return source is RunePriestCard { IsAscended: true } ? glyph.Radiant() : glyph;
    }

    /// <summary>The same glyph, marked to stay in the Incantation after it is Spoken (the Persist keyword).</summary>
    public Glyph Persist() => new(Runes, Anchor, Source, persistent: true);

    /// <summary>Same anchor, source and persistence with different runes.</summary>
    public Glyph WithRunes(params Rune[] runes) => new(runes, Anchor, Source, Persistent);

    /// <summary>A new, distinct glyph with the same runes, anchor and source.</summary>
    public Glyph Copy() => new(Runes, Anchor, Source, Persistent);

    /// <summary>Keeps only the runes matching <paramref name="keep"/> (same anchor and source); null if none match.</summary>
    public Glyph? Only(Func<Rune, bool> keep)
    {
        var runes = Runes.Where(keep).ToList();
        if (runes.Count == 0) return null;
        return runes.Count == Runes.Count ? this : new Glyph(runes, Anchor, Source, Persistent);
    }

    /// <summary>Adds <paramref name="bonus"/> to every amplifiable payload rune.</summary>
    public Glyph Empower(int bonus) =>
        new(Runes.Select(r => r is PayloadRune { Amplifiable: true } ? r.WithValue(r.Value + bonus) ?? r : r).ToList(), Anchor, Source, Persistent);

    /// <summary>Whether this glyph holds a heavy Strike (<see cref="StrikeRune.BonusPerRune"/>).</summary>
    public bool IsHeavy => Runes.Any(r => r is StrikeRune { IsHeavy: true });

    /// <summary>Every heavy Strike grown by its bonus for each of <paramref name="runes"/> runes inscribed after it.</summary>
    public Glyph Heavier(int runes)
    {
        if (runes <= 0 || !IsHeavy) return this;
        return new Glyph(Runes.Select(r => r is StrikeRune { IsHeavy: true } strike
            ? strike.WithValue(strike.Value + strike.BonusPerRune * runes)! : r).ToList(), Anchor, Source, Persistent);
    }

    /// <summary>
    /// A cast's heavy Strikes also grow for the other runes inscribed with them (a Runic Form Loop, other Imbued runes…).
    /// </summary>
    public static IReadOnlyList<Glyph> HeavierInCast(IReadOnlyList<Glyph> cast)
    {
        if (!cast.Any(g => g.IsHeavy)) return cast;
        var total = cast.Sum(g => g.Runes.Count);
        return cast.Select(g => g.Heavier(total - g.Runes.Count)).ToList();
    }

    /// <summary>Multiplies every mergeable rune's value (Strike, Echo, Amplify…). Valueless runes are unchanged.</summary>
    public Glyph Scaled(int factor) =>
        new(Runes.Select(r => r.WithValue(r.Value * factor) ?? r).ToList(), Anchor, Source, Persistent);

    /// <summary>Replaces every Defend rune with a Mend of the same value. Returns this glyph if it has no Defend.</summary>
    public Glyph DefendAsMend()
    {
        if (!Runes.Any(r => r is DefendRune)) return this;
        return new Glyph(Runes.Select(r => r is DefendRune ? new MendRune(r.Value).RadiantIf(r.Radiant) : r).ToList(), Anchor, Source, Persistent);
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
            if (!Runes[i].CanMergeWith(next.Runes[i])) return null;
            var rune = Runes[i].WithValue(Runes[i].Value + next.Runes[i].Value);
            if (rune == null) return null;
            merged[i] = rune.RadiantIf(next.Runes[i].Radiant);
        }
        return new Glyph(merged, Anchor, Source, Persistent || next.Persistent);
    }

    public override string ToString() => $"[{string.Join(" + ", Runes)}]";
}
