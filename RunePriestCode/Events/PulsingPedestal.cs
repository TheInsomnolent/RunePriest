using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using RunePriest.RunePriestCode.Cards;

namespace RunePriest.RunePriestCode.Events;

/// <summary>
/// Act 1: insert a rune card into the pedestal — usually it is upgraded, sometimes the pedestal keeps it (removed from
/// your deck). Only offered when every player has a rune card that can still be upgraded.
/// </summary>
public sealed class PulsingPedestal : RuneEvent
{
    public const int UpgradeChance = 80;

    public override ActModel[] Acts => Act1;

    protected override string PlaceholderPortrait => "wellspring";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("UpgradeChance", UpgradeChance), new DynamicVar("RemoveChance", 100 - UpgradeChance)];

    private static bool CanInsert(CardModel card) => card is RuneCard && card.IsUpgradable && card.IsRemovable;

    protected override bool Qualifies(Player player) => player.Deck.Cards.Any(CanInsert);

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Qualifies(Owner!) ? Choice(Insert, "INSERT") : Locked("INSERT"),
        Choice(Leave, "LEAVE")
    ];

    private async Task Insert()
    {
        var prefs = new CardSelectorPrefs(new LocString("card_selection", MainFile.ModPrefix + "TO_INSERT"), 1);
        var card = (await CardSelectCmd.FromDeckGeneric(Owner!, prefs, CanInsert)).FirstOrDefault();
        if (card == null)
        {
            Finish("LEAVE");
            return;
        }

        if (Rng.NextInt(100) < UpgradeChance)
        {
            CardCmd.Upgrade(card);
            Finish("UPGRADED");
        }
        else
        {
            await CardPileCmd.RemoveFromDeck(card);
            Finish("CONSUMED");
        }
    }

    private Task Leave()
    {
        Finish("LEAVE");
        return Task.CompletedTask;
    }
}
