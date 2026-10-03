using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using RunePriest.RunePriestCode.Powers;

namespace RunePriest.RunePriestCode.Ancients;

/// <summary>
/// Dark Tablet Ancient (Darv).
/// Effect: Blood/Mend runes now damage/heal the enemy instead of the player.
/// </summary>
public sealed class DarkTabletAncient : RunePriestAncientChoice
{
    public override string Id => "RUNEPRIEST-DARK_TABLET";
    public override LocString Title => new("ancients", Id + ".title");
    public override LocString Description => new("ancients", Id + ".description");

    public override async Task Apply(Player player)
    {
        // Apply DarkTabletPower to the player's creature
        var ctx = new MegaCrit.Sts2.Core.BlockingPlayerChoiceContext();
        await PowerCmd.Apply<DarkTabletPower>(ctx, player.Creature, 1, player.Creature, null);
        MainFile.Logger.Info("[Rune] Dark Tablet ancient applied");
    }

    public override bool CanOffer(Player player)
    {
        // Only offer to Darv (character-specific)
        // For now, offer to RunePriest; specify Darv when character system is available
        return base.CanOffer(player);
    }
}
