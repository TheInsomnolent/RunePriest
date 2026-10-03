using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using RunePriest.RunePriestCode.Powers;

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

    public override async Task Apply(Player player)
    {
        // Apply TouchOfOrobasPower to the player's creature
        var ctx = new MegaCrit.Sts2.Core.BlockingPlayerChoiceContext();
        await PowerCmd.Apply<TouchOfOrobasPower>(ctx, player.Creature, 1, player.Creature, null);
        MainFile.Logger.Info("[Rune] Touch of Orobas ancient applied");
    }

    public override bool CanOffer(Player player)
    {
        // Only offer to Orobas (character-specific)
        // For now, offer to RunePriest; specify Orobas when character system is available
        return base.CanOffer(player);
    }
}
