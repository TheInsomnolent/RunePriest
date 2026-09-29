using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;
/// <summary>Inscribe Blood, then Kindle.</summary>
public sealed class FlintAndSteel() : RuneCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Blood", 5m), new IntVar("Kindle", 1m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new BloodRune(Var("Blood"))), Glyph.Of(new KindleRune(Var("Kindle")))];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
