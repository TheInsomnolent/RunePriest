using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;
/// <summary>Inscribe three simultaneous Strikes (one compound glyph: they resolve together).</summary>
public sealed class StackedStrike() : RuneCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar("Strike", 7m, ValueProp.Move)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new StrikeRune(Var("Strike")), new StrikeRune(Var("Strike")), new StrikeRune(Var("Strike"))).AnchoredTo(anchor)];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
