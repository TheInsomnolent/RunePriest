using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Saves.Runs;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Ancient Skill (from Tezcatara's Eternal Candle): Inscribes the runes etched onto it. With the candle, rest sites offer
/// <see cref="RestSite.EtchRestSiteOption"/>: remove a rune card from your deck and etch its runes here, for +1 cost.
/// Upgrades lower the cost by 1 and can be repeated without limit.
/// </summary>
public sealed class EternalScroll() : RuneCard(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    private string? _decodedRunes;
    private IReadOnlyList<Glyph> _decodedGlyphs = [];

    /// <summary>Etched runes as <see cref="GlyphCodec"/> text. Saved with the deck card.</summary>
    [SavedProperty]
    public string EtchedRunes { get; set; } = "";

    /// <summary>How many times runes were etched; each etching raises the cost by 1.</summary>
    [SavedProperty]
    public int Etchings { get; set; }

    public override int MaxUpgradeLevel => 99;

    public IReadOnlyList<Glyph> EtchedGlyphs
    {
        get
        {
            if (_decodedRunes != EtchedRunes)
            {
                _decodedGlyphs = GlyphCodec.Decode(EtchedRunes);
                _decodedRunes = EtchedRunes;
            }
            return _decodedGlyphs;
        }
    }

    /// <summary>Asks for an enemy to anchor to while it holds enemy-targeting runes (like an Imbued card).</summary>
    public override TargetType TargetType =>
        EtchedGlyphs.Any(g => g.Runes.Any(r => r is PayloadRune { Targeting: RuneTargeting.Enemy }))
            ? TargetType.AnyEnemy
            : base.TargetType;

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => EtchedGlyphs.Select(g => g.AnchoredTo(anchor));

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [RuneTips.Etch];

    /// <summary>Etches <paramref name="glyphs"/> after the runes already etched, raising the cost by 1.</summary>
    public void Etch(IEnumerable<Glyph> glyphs)
    {
        EtchedRunes = GlyphCodec.Encode(EtchedGlyphs.Concat(glyphs));
        Etchings++;
        EnergyCost.SetCustomBaseCost(Cost(CurrentUpgradeLevel));
        MainFile.Logger.Info($"[Rune] Eternal Scroll etched: {EtchedRunes} (cost {Cost(CurrentUpgradeLevel)})");
    }

    /// <summary>Cost is a pure function of etchings and upgrades, so it survives save/load whatever their order.</summary>
    private int Cost(int upgradeLevel) => Math.Max(0, EnergyCost.Canonical + Etchings - upgradeLevel);

    // Saved properties are filled before upgrades are replayed, so start from the un-upgraded cost.
    protected override void AfterDeserialized()
    {
        base.AfterDeserialized();
        EnergyCost.SetCustomBaseCost(Cost(0));
    }

    // A downgrade (Dampen, Reflections…) resets the cost to the canonical 0; add the etchings back.
    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        EnergyCost.SetCustomBaseCost(Cost(0));
    }

    protected override void OnUpgrade() =>
        EnergyCost.UpgradeBy(Cost(CurrentUpgradeLevel) - Cost(CurrentUpgradeLevel - 1));

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("EtchedCount", (decimal)EtchedGlyphs.Count);
        description.Add("EtchedRunes", RuneTips.InscribeText(EtchedGlyphs));
    }
}
