using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;
/// <summary>A big Defend paid for in Blood. Upgraded: more Defend.</summary>
public sealed class UnstableWard() : RuneCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar("Defend", 8m, ValueProp.Move), new IntVar("Blood", 3m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new DefendRune(Var("Defend"))).AnchoredTo(anchor), Glyph.Of(new BloodRune(Var("Blood")))];

    protected override void OnUpgrade() => DynamicVars["Defend"].UpgradeValueBy(2m);
}
