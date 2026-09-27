using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;

public sealed class Quickening() : RuneCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar("Kindle", 1), new CardsVar("Insight", 1), new HpLossVar("Blood", 3m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new KindleRune(Var("Kindle")), new SoulRune(Var("Insight")), new BloodRune(Var("Blood")))];

    protected override void OnUpgrade() => DynamicVars["Blood"].UpgradeValueBy(-1m);
}
