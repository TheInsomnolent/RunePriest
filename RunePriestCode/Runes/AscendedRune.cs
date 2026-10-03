using MegaCrit.Sts2.Core.HoverTips;

namespace RunePriest.RunePriestCode.Runes;

/// <summary>
/// Wrapper around a base rune that marks it as Ascended — visually and/or mechanically enhanced.
/// Preserves all payload behavior of the wrapped rune; only changes visuals and labels.
///
/// Example: an Ascended Strike behaves identically to Strike, but displays with enhanced visuals
/// and may use different localized text (e.g., "Ascended Strike: Deal divine radiance.").
///
/// Ascended runes still respect merging: two Ascended Strikes merge into one with combined values.
/// </summary>
public abstract class AscendedRune : Rune
{
    public abstract Rune BaseRune { get; }

    public override RuneKind Kind => BaseRune.Kind;
    public override string Key => BaseRune.Key;
    public override bool ShowsValue => BaseRune.ShowsValue;
    public override bool Amplifiable => BaseRune.Amplifiable;
    public override string ValueLabel => BaseRune.ValueLabel;
    public override bool IsAscended => true;

    public override IEnumerable<IHoverTip> HoverTips => BaseRune.HoverTips;

    /// <summary>
    /// Ascended runes merge with their base counterparts: an Ascended Strike (5) + Strike (3) = Ascended Strike (8).
    /// Returns an Ascended variant of WithValue result, or null if the base rune doesn't merge.
    /// </summary>
    protected override Rune? Revalued(int value)
    {
        var merged = BaseRune.WithValue(value);
        return merged != null ? ToAscended(merged) : null;
    }

    /// <summary>
    /// Convert a merged rune back to its Ascended variant.
    /// Override in subclasses: return new AscendedStrike((PayloadRune)rune) etc.
    /// </summary>
    protected abstract Rune ToAscended(Rune merged);

    public override string ToString() => $"Ascended {BaseRune}";
}
