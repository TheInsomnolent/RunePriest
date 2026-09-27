using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;

public sealed class KindlingMark() : RuneCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar("Kindle", 1), new HpLossVar("Blood", 2m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new KindleRune(Var("Kindle")), new BloodRune(Var("Blood")))];

    protected override void OnUpgrade() => DynamicVars["Blood"].UpgradeValueBy(-1m);
}
