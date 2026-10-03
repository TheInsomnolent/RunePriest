using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>The first Curse you draw each turn is Exhausted; then draw Amount cards and Inscribe a Void.</summary>
public sealed class ExorcismPower : RunePriestPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [..base.ExtraHoverTips, HoverTipFactory.FromKeyword(CardKeyword.Exhaust), RuneTips.Inscribe, ..new VoidRune().HoverTips];

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner.Creature != Owner || card.Type != CardType.Curse || Owner.Player is not { } player) return;

        // The draw history already includes this card, so it's the first Curse this turn when the count is 1.
        var cursesThisTurn = CombatManager.Instance.History.Entries.OfType<CardDrawnEntry>()
            .Count(e => e.HappenedThisTurn(CombatState) && e.Actor == Owner && e.Card.Type == CardType.Curse);
        if (cursesThisTurn > 1) return;

        Flash();
        await CardCmd.Exhaust(choiceContext, card);
        await CardPileCmd.Draw(choiceContext, Amount, player);
        await RuneCmd.Inscribe(choiceContext, player, [Glyph.Of(new VoidRune())], null);
    }
}
