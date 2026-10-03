using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;

namespace RunePriest.RunePriestCode.Ancients;

/// <summary>
/// Touch of Orobas Ancient (Orobas).
/// Effect: Whenever you play a rune card, draw a card.
/// </summary>
public sealed class TouchOfOrobasAncient : RunePriestAncientChoice
{
    public override string Id => "RUNEPRIEST-TOUCH_OF_OROBAS";
    public override LocString Title => new("ancients", Id + ".title");
    public override LocString Description => new("ancients", Id + ".description");

    public override Task Apply(Player player)
    {
        // TODO: Apply TouchOfOrobasPower to the player's creature
        MainFile.Logger.Info("[Rune] Touch of Orobas ancient applied (placeholder)");
        return Task.CompletedTask;
    }

    public override bool CanOffer(Player player) => base.CanOffer(player);
}
