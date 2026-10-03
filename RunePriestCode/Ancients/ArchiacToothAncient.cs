using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;

namespace RunePriest.RunePriestCode.Ancients;

/// <summary>
/// Archaic Tooth Ancient (Orobas, Starter card upgrade).
/// Effect: Your starting rune gains an ascended form.
/// </summary>
public sealed class ArchiacToothAncient : RunePriestAncientChoice
{
    public override string Id => "RUNEPRIEST-ARCHIAC_TOOTH";
    public override LocString Title => new("ancients", Id + ".title");
    public override LocString Description => new("ancients", Id + ".description");

    public override Task Apply(Player player)
    {
        // TODO: Upgrade starting rune in Blessed Toolbox to ascended form
        MainFile.Logger.Info("[Rune] Archiac Tooth ancient applied (placeholder)");
        return Task.CompletedTask;
    }

    public override bool CanOffer(Player player) => base.CanOffer(player);
}
