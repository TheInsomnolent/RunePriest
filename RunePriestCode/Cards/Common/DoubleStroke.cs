using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;

public sealed class DoubleStroke() : RuneCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar("Strike", 5m, ValueProp.Move)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
    [
        Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor),
        Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor)
    ];

    protected override void OnUpgrade() => DynamicVars["Strike"].UpgradeValueBy(2m);
}
