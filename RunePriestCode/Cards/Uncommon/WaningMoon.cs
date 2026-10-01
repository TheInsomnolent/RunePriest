using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>
/// A looped crescent that wanes into a Reflection: Strike, Defend, a persistent Void, then the mirror image back.
/// Upgraded: Inscribe Amplify first.
/// </summary>
public sealed class WaningMoon() : RuneCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [RunePriestKeywords.Persist];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Amplify", 2m), new IntVar("Loop", 1m),
        new DamageVar("Strike", 4m, ValueProp.Move), new BlockVar("Defend", 4m, ValueProp.Move)
    ];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor)
    {
        if (IsUpgraded) yield return Glyph.Of(new AmplifyRune(Var("Amplify")));
        yield return Glyph.Of(new LoopRune(Var("Loop")));
        yield return Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor);
        yield return Glyph.Of(new DefendRune(Var("Defend")));
        yield return Glyph.Of(new VoidRune()).Persist();
        yield return Glyph.Of(new DefendRune(Var("Defend")));
        yield return Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor);
        yield return Glyph.Of(new ReflectionRune());
    }
}
