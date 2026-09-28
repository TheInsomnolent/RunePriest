using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using RunePriest.RunePriestCode.Cards;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>Cards that Inscribe runes have Retain.</summary>
public sealed class RunicSphere : RunePriestRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe, HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner == Owner && card is RuneCard) CardCmd.ApplySingleTurnRetain(card);
        return Task.CompletedTask;
    }

    // Cards kept from last turn need the flag again, since it is cleared at end of turn.
    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return Task.CompletedTask;
        foreach (var card in PileType.Hand.GetPile(Owner).Cards.OfType<RuneCard>())
            CardCmd.ApplySingleTurnRetain(card);
        return Task.CompletedTask;
    }
}
