using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;

public sealed class HexingMark() : RuneCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Hex", 1m), new IntVar("Expose", 1m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new WeakeningRune(Var("Hex")), new ExposeRune(Var("Expose"))).AnchoredTo(anchor)];

    protected override void OnUpgrade()
    {
        DynamicVars["Hex"].UpgradeValueBy(1m);
        DynamicVars["Expose"].UpgradeValueBy(1m);
    }
}
