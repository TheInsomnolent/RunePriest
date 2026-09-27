using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;

public sealed class ScatterMarks() : RuneCard(1, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar("Strike", 4m, ValueProp.Move)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
    [
        Glyph.Of(new TargetRune(TargetMode.Seek)),
        Glyph.Of(new StrikeRune(Var("Strike"))),
        Glyph.Of(new StrikeRune(Var("Strike")))
    ];

    protected override void OnUpgrade() => DynamicVars["Strike"].UpgradeValueBy(2m);
}
