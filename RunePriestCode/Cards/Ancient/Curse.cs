using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Ascended Skill (Hex Rune's Ascended form): Inscribe a Persistent Hex, which stays in the Incantation after being
/// Spoken.
/// </summary>
public sealed class Curse() : RuneCard(1, CardType.Skill, CardRarity.Ancient, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(RunePriestKeywords.Persist)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Hex", 1m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new HexRune(Var("Hex"))).AnchoredTo(anchor).Persist()];

    /// <summary>Ascended cards never upgrade.</summary>
    public override int MaxUpgradeLevel => 0;

    protected override void OnUpgrade()
    {
    }
}
