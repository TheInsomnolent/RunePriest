using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Ascended Attack: Set up a loop with Amplify, then inscribe two Strikes inside, closing the loop for double-amplified output.
/// </summary>
public sealed class Disintegrate() : RuneCard(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar("Strike", 2m, ValueProp.Move),
        new IntVar("Amplify", 10m)
    ];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
    [
        Glyph.Of(new AmplifyRune(Var("Amplify"))),
        Glyph.Of(new LoopRune()),
        Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor),
        Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor),
        Glyph.Of(new EndLoopRune())
    ];

    /// <summary>Ascended cards never upgrade.</summary>
    public override int MaxUpgradeLevel => 0;

    protected override void OnUpgrade()
    {
    }
}
