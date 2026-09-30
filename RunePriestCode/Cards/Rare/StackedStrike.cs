using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;
/// <summary>Inscribe three separate Strikes (a card's own glyph sequence never merges).</summary>
public sealed class StackedStrike() : RuneCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar("Strike", 7m, ValueProp.Move), new IntVar("Hits", 3m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        Enumerable.Range(0, Var("Hits")).Select(_ => Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor));

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
