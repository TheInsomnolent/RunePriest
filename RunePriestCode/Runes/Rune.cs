using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace RunePriest.RunePriestCode.Runes;

public enum RuneKind
{
    Payload,
    Modifier,
    Target,
    Flow
}

/// <summary>
/// Atomic instruction in the Incantation language. Immutable; values are baked in when inscribed.
/// Text lives in static_hover_tips.json under <c>RUNEPRIEST-RUNE_{Key}.title/.description</c>.
/// </summary>
public abstract class Rune(int value = 0)
{
    public int Value { get; } = value;

    public abstract RuneKind Kind { get; }

    public abstract string Key { get; }

    public virtual bool ShowsValue => true;

    /// <summary>
    /// Whether additive/multiplicative effects (Amplify, Twin, Amplify Sigil, Empower) change this rune's value.
    /// Runes that aren't amplifiable keep their value even if they have one (Loop, Kindle, Swift…).
    /// </summary>
    public virtual bool Amplifiable => false;

    public virtual string ValueLabel => Value.ToString();

    /// <summary>
    /// Whether this rune is Ascended — mechanically or visually enhanced. Ascended runes may have improved
    /// visuals, different particle effects, or stronger mechanics depending on the card that inscribed them.
    /// For localization: looks up RUNEPRIEST-RUNE_{Key}_ASCENDED first; falls back to base if not found.
    /// </summary>
    public virtual bool IsAscended => false;

    public string LocKey
    {
        get
        {
            var baseKey = $"{RuneTips.Prefix}RUNE_{Key}";
            // If Ascended, try the ascended variant first; fallback to base if not localized.
            if (IsAscended)
            {
                var ascendedKey = baseKey + "_ASCENDED";
                if (LocString.Exists(RuneTips.Table, ascendedKey + ".title"))
                    return ascendedKey;
            }
            return baseKey;
        }
    }

    public LocString TitleLoc => new(RuneTips.Table, LocKey + ".title");

    public LocString DescriptionLoc
    {
        get
        {
            var loc = new LocString(RuneTips.Table, LocKey + ".description");
            // Value 0 is used for generic tooltips (e.g. on relics), so show a placeholder instead.
            loc.Add("Value", Value > 0 ? Value.ToString() : "X");
            return loc;
        }
    }

    public string Label => ShowsValue ? $"{TitleLoc.GetFormattedText()} {ValueLabel}" : TitleLoc.GetFormattedText();

    public virtual IEnumerable<IHoverTip> HoverTips => [new HoverTip(TitleLoc, DescriptionLoc)];

    /// <summary>Same rune with a new value, or null if this rune never merges (targets, Loop, End Loop, Seal, Sanctify).</summary>
    public virtual Rune? WithValue(int value) => null;

    public override string ToString() => ShowsValue ? $"{Key} {ValueLabel}" : Key;
}
