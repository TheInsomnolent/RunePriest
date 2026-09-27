using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;

public sealed class AmplifyingRune() : RuneCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amplify", 4m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new AddRune(Var("Amplify")))];

    protected override void OnUpgrade() => DynamicVars["Amplify"].UpgradeValueBy(2m);
}
