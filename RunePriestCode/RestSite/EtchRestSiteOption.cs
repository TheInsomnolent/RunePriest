using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using RunePriest.RunePriestCode.Cards;
using RunePriest.RunePriestCode.Cards.Ancient;
using RunePriest.RunePriestCode.Extensions;

namespace RunePriest.RunePriestCode.RestSite;

/// <summary>
/// Rest site option from <see cref="Relics.EternalCandle"/>: remove a rune card from your deck and etch its runes onto
/// your Eternal Scroll (which costs 1 more for each etching).
/// </summary>
public sealed class EtchRestSiteOption(Player owner) : CustomRestSiteOption(owner)
{
    public override string OptionId => ModVariant.IdUpper + "_ETCH";

    public override string CustomIconPath => "eternal_candle.png".RelicImagePath();

    public override LocString Description => IsEnabled
        ? base.Description
        : new LocString("rest_site_ui", "OPTION_" + OptionId + ".descriptionDisabled");

    public override bool IsEnabled => Scroll != null && Owner.Deck.Cards.Any(CanEtch);

    private EternalScroll? Scroll => Owner.Deck.Cards.OfType<EternalScroll>().FirstOrDefault();

    private static bool CanEtch(CardModel card) => card is RuneCard rune && card is not EternalScroll && rune.InscribedGlyphs.Count > 0;

    public override async Task<bool> OnSelect()
    {
        var scroll = Scroll;
        if (scroll == null) return false;

        var prefs = new CardSelectorPrefs(new LocString("card_selection", MainFile.ModPrefix + "TO_ETCH"), 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        var card = (await CardSelectCmd.FromDeckGeneric(Owner, prefs, CanEtch)).FirstOrDefault();
        if (card is not RuneCard rune) return false;

        var glyphs = rune.InscribedGlyphs;
        await CardPileCmd.RemoveFromDeck(card);
        scroll.Etch(glyphs);
        return true;
    }
}
