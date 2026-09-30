using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;
/// <summary>A closed Loop incubating a Growth Strike: it doubles for six turns before it finally lands.</summary>
public sealed class OrobasStrike() : RuneCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("Loop", 1m), new IntVar("Growth", 6m), new DamageVar("Strike", 3m, ValueProp.Move)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
    [
        Glyph.Of(new LoopRune(Var("Loop"))),
        Glyph.Of(new GrowthRune(Var("Growth"))),
        Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor),
        Glyph.Of(new EndLoopRune())
    ];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
