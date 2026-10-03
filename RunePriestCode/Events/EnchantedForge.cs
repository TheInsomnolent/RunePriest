using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using RunePriest.RunePriestCode.Cards;
using RunePriest.RunePriestCode.Cards.Special;
using Clumsy = MegaCrit.Sts2.Core.Models.Cards.Clumsy;

namespace RunePriest.RunePriestCode.Events;

/// <summary>
/// Act 2, no way out: Forge two Common rune cards into one <see cref="ForgedRune"/> (and gain Clumsy), or pay Gold to
/// remove a card. Only offered when every player has two Common rune cards to forge, so nobody is stuck.
/// </summary>
public sealed class EnchantedForge : RuneEvent
{
    public const int DissolveCost = 50;
    private const int ForgeCount = 2;

    public override ActModel[] Acts => Act2;

    protected override string PlaceholderPortrait => "amalgamator";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(DissolveCost), new CardsVar(ForgeCount)];

    /// <summary>X-cost cards are left out: a forged card has a fixed cost.</summary>
    private static bool CanForge(CardModel card) =>
        card is RuneCard { Rarity: CardRarity.Common } rune && !card.EnergyCost.CostsX && card.IsRemovable &&
        rune.InscribedGlyphs.Count > 0;

    protected override bool Qualifies(Player player) => player.Deck.Cards.Count(CanForge) >= ForgeCount;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Qualifies(Owner!)
            ? Choice(Forge, "FORGE", HoverTipFactory.FromCard<ForgedRune>(), HoverTipFactory.FromCard<Clumsy>())
            : Locked("FORGE"),
        Owner!.Gold >= DissolveCost ? Choice(Dissolve, "DISSOLVE") : Locked("DISSOLVE")
    ];

    private async Task Forge()
    {
        var prefs = new CardSelectorPrefs(new LocString("card_selection", MainFile.ModPrefix + "TO_FORGE"), ForgeCount);
        var cards = (await CardSelectCmd.FromDeckForRemoval(Owner!, prefs, CanForge)).OfType<RuneCard>().ToList();
        if (cards.Count < ForgeCount)
        {
            Finish("NOTHING");
            return;
        }

        var forged = Owner!.RunState.CreateCard<ForgedRune>(Owner);
        forged.Forge(cards);
        await CardPileCmd.RemoveFromDeck(cards);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(forged, PileType.Deck), 2f);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(Owner.RunState.CreateCard<Clumsy>(Owner), PileType.Deck), 2f);
        Finish("FORGE");
    }

    private async Task Dissolve()
    {
        await PlayerCmd.LoseGold(DissolveCost, Owner!, GoldLossType.Spent);
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var cards = (await CardSelectCmd.FromDeckForRemoval(Owner!, prefs)).ToList();
        await CardPileCmd.RemoveFromDeck(cards);
        Finish("DISSOLVE");
    }
}
