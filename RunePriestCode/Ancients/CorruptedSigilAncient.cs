using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;

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

    public override Task Apply(Player player)
    {
        // TODO: Implement deck card removal and relic granting
        MainFile.Logger.Info("[Rune] Corrupted Sigil ancient applied (placeholder)");
        return Task.CompletedTask;
    }

    public override bool CanOffer(Player player) => base.CanOffer(player);
}
