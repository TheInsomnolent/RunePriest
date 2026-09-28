using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;
public sealed class Meditate() : RuneCard(2, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HealVar("Mend", 5m), new BlockVar("Defend", 5m, ValueProp.Move)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new MendRune(Var("Mend"))).AnchoredTo(anchor), Glyph.Of(new DefendRune(Var("Defend"))).AnchoredTo(anchor)];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
