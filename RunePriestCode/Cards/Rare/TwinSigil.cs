using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;

public sealed class TwinSigil() : RuneCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Twin", 2m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new MultiplyRune(Var("Twin")))];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
