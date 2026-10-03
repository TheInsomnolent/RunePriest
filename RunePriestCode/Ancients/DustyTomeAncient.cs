using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using RunePriest.RunePriestCode.Cards.Rare;

namespace RunePriest.RunePriestCode.Ancients;

/// <summary>
/// Dusty Tome Ancient (Darv, Special Card).
/// Effect: Add Mirrororrim card to your deck.
/// </summary>
public sealed class DustyTomeAncient : RunePriestAncientChoice
{
    public override string Id => "RUNEPRIEST-DUSTY_TOME";
    public override LocString Title => new("ancients", Id + ".title");
    public override LocString Description => new("ancients", Id + ".description");

    public override async Task Apply(Player player)
    {
        // Create Mirrororrim card and add to player deck
        var card = player.RunState.CreateCard(typeof(Mirrororrim), player);
        var ctx = new MegaCrit.Sts2.Core.BlockingPlayerChoiceContext();
        await CardPileCmd.Add(ctx, card, player, MegaCrit.Sts2.Core.Entities.Cards.PileType.Deck);
        MainFile.Logger.Info("[Rune] Dusty Tome ancient applied - added Mirrororrim to deck");
    }

    public override bool CanOffer(Player player)
    {
        // Only offer to Darv (character-specific)
        // For now, offer to RunePriest; specify Darv when character system is available
        return base.CanOffer(player);
    }
}
