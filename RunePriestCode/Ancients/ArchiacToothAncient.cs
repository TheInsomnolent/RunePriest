using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;

namespace RunePriest.RunePriestCode.Ancients;

/// <summary>
/// Archaic Tooth Ancient (Orobas, Starter card upgrade).
/// Effect: Your starting rune gains an ascended form.
/// Integration point with Ascended framework - upgrades Blessed Toolbox relic's starting rune.
/// </summary>
public sealed class ArchiacToothAncient : RunePriestAncientChoice
{
    public override string Id => "RUNEPRIEST-ARCHIAC_TOOTH";
    public override LocString Title => new("ancients", Id + ".title");
    public override LocString Description => new("ancients", Id + ".description");

    public override Task Apply(Player player)
    {
        // TODO: Integrate with Ascended framework
        // This should upgrade the starting rune in Blessed Toolbox to its ascended variant
        // Placeholder - awaiting Ascended framework integration
        MainFile.Logger.Info("[Rune] Archaic Tooth ancient applied (placeholder)");
        return Task.CompletedTask;
    }

    public override bool CanOffer(Player player)
    {
        // Only offer to Orobas (character-specific)
        // For now, offer to RunePriest; specify Orobas when character system is available
        return base.CanOffer(player);
    }
}
