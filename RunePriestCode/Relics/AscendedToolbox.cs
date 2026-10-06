using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Cards;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>
/// Upgraded starter, only from Orobas' Touch of Orobas, which replaces <see cref="BlessedToolbox"/> with it: whenever
/// you play a card with "Rune" in its name (<see cref="RunePriestCard.HasRuneInName"/>), draw a card. It replaces the
/// Blessed Toolbox, so any Blessed Toolbox still held on pickup is removed. Starter rarity, like the game's upgraded
/// starters (Black Blood…).
/// </summary>
public sealed class AscendedToolbox : RunePriestRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    public override async Task AfterObtained()
    {
        foreach (var leftover in Owner.Relics.OfType<BlessedToolbox>().ToList())
            await RelicCmd.Remove(leftover);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!RunePriestCard.HasRuneInName(cardPlay.Card) || cardPlay.Card.Owner != Owner) return;
        Flash();
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }
}
