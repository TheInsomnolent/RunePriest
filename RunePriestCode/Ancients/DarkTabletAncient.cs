using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;

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

    public override Task Apply(Player player)
    {
        // TODO: Apply DarkTabletPower to the player's creature
        MainFile.Logger.Info("[Rune] Dark Tablet ancient applied (placeholder)");
        return Task.CompletedTask;
    }

    public override bool CanOffer(Player player) => base.CanOffer(player);
}
