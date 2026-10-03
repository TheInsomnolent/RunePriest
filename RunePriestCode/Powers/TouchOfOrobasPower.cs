using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Cards;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// Power granted by Touch of Orobas ancient. Whenever this player plays a rune card, draw a card.
/// Co-op safe: only applies to the Orobas player who has the ancient selected.
/// </summary>
public sealed class TouchOfOrobasPower : RunePriestPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    /// <summary>
    /// Hook into rune card play events. Draws a card whenever a rune card is played.
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Only trigger for this player's cards
        if (cardPlay.Card.Owner != Owner.Player) return;
        
        // Only trigger for rune cards
        if (cardPlay.Card is not RuneCard) return;
        
        Flash();
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner.Player);
    }
}
