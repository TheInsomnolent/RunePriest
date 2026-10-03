using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;

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

    public override Task Apply(Player player)
    {
        // TODO: Add Mirrororrim card to player deck
        MainFile.Logger.Info("[Rune] Dusty Tome ancient applied (placeholder)");
        return Task.CompletedTask;
    }

    public override bool CanOffer(Player player) => base.CanOffer(player);
}
