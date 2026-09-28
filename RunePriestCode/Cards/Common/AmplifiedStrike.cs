using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;
public sealed class AmplifiedStrike() : RuneCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar("Strike", 4m, ValueProp.Move), new IntVar("Amplify", 1m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor), Glyph.Of(new AmplifyRune(Var("Amplify")))];

    protected override void OnUpgrade()
    {
        DynamicVars["Strike"].UpgradeValueBy(1m);
        DynamicVars["Amplify"].UpgradeValueBy(1m);
    }
}
