using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Cards.Ancient;
using RunePriest.RunePriestCode.RestSite;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>
/// Ancient (offered by Tezcatara, see <c>AncientOptionPatches</c>): upon pickup, add an Eternal Scroll to your deck.
/// Rest sites offer <see cref="EtchRestSiteOption"/>.
/// </summary>
public sealed class EternalCandle : RunePriestRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<EternalScroll>(), RuneTips.Etch];

    public override async Task AfterObtained()
    {
        var card = Owner.RunState.CreateCard<EternalScroll>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;
        options.Add(new EtchRestSiteOption(player));
        return true;
    }
}
