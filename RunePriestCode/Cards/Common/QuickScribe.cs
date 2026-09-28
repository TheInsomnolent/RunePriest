using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;

public sealed class QuickScribe() : RuneCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar("Strike", 3m, ValueProp.Move), new BlockVar("Defend", 3m, ValueProp.Move), new IntVar("Loop", 2m)];

    // Upgraded: the pair is wrapped in a closed Loop.
    protected override IEnumerable<Glyph> Glyphs(Creature? anchor)
    {
        if (IsUpgraded) yield return Glyph.Of(new LoopRune(Var("Loop")));
        yield return Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor);
        yield return Glyph.Of(new DefendRune(Var("Defend")));
        if (IsUpgraded) yield return Glyph.Of(new EndLoopRune());
    }
}
