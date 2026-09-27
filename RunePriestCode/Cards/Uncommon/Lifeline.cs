using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;

public sealed class Lifeline() : RuneCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HealVar("Mend", 2m), new BlockVar("Ward", 5m, ValueProp.Move)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new MendRune(Var("Mend")), new BlockRune(Var("Ward"))).AnchoredTo(anchor)];

    protected override void OnUpgrade()
    {
        DynamicVars["Mend"].UpgradeValueBy(1m);
        DynamicVars["Ward"].UpgradeValueBy(2m);
    }
}
