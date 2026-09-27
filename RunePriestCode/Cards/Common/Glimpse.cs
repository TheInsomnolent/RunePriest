using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;

public sealed class Glimpse() : RuneCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Insight", 2)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new SoulRune(Var("Insight")))];

    protected override void OnUpgrade() => DynamicVars["Insight"].UpgradeValueBy(1m);
}
