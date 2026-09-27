using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;

public sealed class HexCircle() : RuneCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Hex", 1m), new IntVar("Expose", 1m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
    [
        Glyph.Of(new TargetRune(TargetMode.Nova)),
        Glyph.Of(new HexRune(Var("Hex")), new ExposeRune(Var("Expose")))
    ];

    protected override void OnUpgrade()
    {
        DynamicVars["Hex"].UpgradeValueBy(1m);
        DynamicVars["Expose"].UpgradeValueBy(1m);
    }
}
