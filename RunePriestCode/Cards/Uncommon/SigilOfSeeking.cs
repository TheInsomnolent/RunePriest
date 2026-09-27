using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;

public sealed class SigilOfSeeking() : RuneCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar("Strike", 3m, ValueProp.Move), new IntVar("Loop", 3m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
    [
        Glyph.Of(new TargetRune(TargetMode.Seek)),
        Glyph.Of(new LoopRune(Var("Loop"))),
        Glyph.Of(new StrikeRune(Var("Strike"))),
        Glyph.Of(new EndLoopRune())
    ];

    protected override void OnUpgrade() => DynamicVars["Strike"].UpgradeValueBy(1m);
}
