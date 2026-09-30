using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>
/// A whole doomed program: Nova, Twin, a closed Loop of Strikes, and a trailing Void that fizzles harmlessly —
/// until the upgrade removes the End Loop and pulls the Void (and anything inscribed after) into the loop.
/// </summary>
public sealed class Nebula() : RuneCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("Twin", 2m), new IntVar("Loop", 1m), new DamageVar("Strike", 5m, ValueProp.Move)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor)
    {
        yield return Glyph.Of(new TargetRune(TargetMode.Nova));
        yield return Glyph.Of(new TwinRune(Var("Twin")));
        yield return Glyph.Of(new LoopRune(Var("Loop")));
        yield return Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor);
        if (!IsUpgraded) yield return Glyph.Of(new EndLoopRune());
        yield return Glyph.Of(new VoidRune());
    }
}
