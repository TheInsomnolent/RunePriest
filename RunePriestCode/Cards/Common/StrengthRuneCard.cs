using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;
public sealed class StrengthRuneCard() : RuneCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Strength", 2m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new StrengthRune(Var("Strength"))).AnchoredTo(anchor)];

    protected override void OnUpgrade() => DynamicVars["Strength"].UpgradeValueBy(1m);
}
