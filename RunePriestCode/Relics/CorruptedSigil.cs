using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Cards.Rare;
using SigilCard = RunePriest.RunePriestCode.Cards.Ancient.CorruptedSigil;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>
/// Ancient (offered by Vakuu, see <c>AncientOptionPatches</c>): upon pickup, remove every Eternal Sigil from your deck
/// and add the Corrupted Sigil card.
/// </summary>
public sealed class CorruptedSigil : RunePriestRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<SigilCard>(), HoverTipFactory.FromCard<EternalSigil>()];

    public override async Task AfterObtained()
    {
        var sigils = Owner.Deck.Cards.Where(c => c is EternalSigil).ToList();
        if (sigils.Count > 0) await CardPileCmd.RemoveFromDeck(sigils);

        var card = Owner.RunState.CreateCard<SigilCard>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
    }
}
