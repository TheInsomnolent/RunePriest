using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using RunePriest.RunePriestCode.Relics;

namespace RunePriest.RunePriestCode.Ancients;

/// <summary>
/// Corrupted Sigil Ancient (Vakku Pool 2).
/// Effect: Remove all Eternal Sigil cards from your deck. Gain Corrupted Sigil relic.
/// </summary>
public sealed class CorruptedSigilAncient : RunePriestAncientChoice
{
    public override string Id => "RUNEPRIEST-CORRUPTED_SIGIL";
    public override LocString Title => new("ancients", Id + ".title");
    public override LocString Description => new("ancients", Id + ".description");

    public override async Task Apply(Player player)
    {
        // Find and remove all Eternal Sigil cards from the deck
        var deckPile = PileType.Deck.GetPile(player);
        var cardsToRemove = deckPile.Cards
            .Where(card => card.CanonicalModel.Id.Entry == "RUNEPRIEST-ETERNAL_SIGIL")
            .ToList();

        foreach (var card in cardsToRemove)
        {
            deckPile.Remove(card);
            MainFile.Logger.Info($"[Rune] Corrupted Sigil: removed {card.CanonicalModel.Id.Entry}");
        }

        // Add Corrupted Sigil relic
        var relic = new CorruptedSigil();
        var ctx = new MegaCrit.Sts2.Core.BlockingPlayerChoiceContext();
        await RelicCmd.Obtain(ctx, relic, player);
        MainFile.Logger.Info("[Rune] Corrupted Sigil ancient applied");
    }

    public override bool CanOffer(Player player)
    {
        // Only offer to Vakku (character-specific)
        // For now, offer to RunePriest; specify Vakku when character system is available
        return base.CanOffer(player);
    }
}
