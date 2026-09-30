using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;
/// <summary>Inscribe a Strike that grows with every rune already in the Incantation.</summary>
public sealed class HeavyRune() : RuneCard(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar("Strike", 9m, ValueProp.Move), new IntVar("Bonus", 3m)];

    // Glyphs(null) is only used for hover tips; the real value is read when played.
    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new StrikeRune(Var("Strike"))).AnchoredTo(anchor)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var amount = Var("Strike") + IncantationRuneCount * Var("Bonus");
        await RuneCmd.Inscribe(choiceContext, Owner, [Glyph.Of(new StrikeRune(amount)).AnchoredTo(cardPlay.Target)], this);
    }

    protected override void OnUpgrade() => DynamicVars["Bonus"].UpgradeValueBy(1m);
}
