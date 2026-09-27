using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;

public sealed class SanguineWard() : RuneCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar("Ward", 10m, ValueProp.Move), new HpLossVar("Blood", 2m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new BlockRune(Var("Ward")), new BloodRune(Var("Blood"))).AnchoredTo(anchor)];

    protected override void OnUpgrade() => DynamicVars["Ward"].UpgradeValueBy(3m);
}
