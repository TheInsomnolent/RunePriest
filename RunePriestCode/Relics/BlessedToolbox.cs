using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using RunePriest.RunePriestCode.Cards;
using RunePriest.RunePriestCode.Character;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>
/// Starter: when obtained, add a random Common card with "Rune" in its name to your deck. The first time you play a
/// rune card each combat, draw a card.
/// </summary>
public sealed class BlessedToolbox : RunePriestRelic
{
    private bool _usedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool HasUponPickupEffect => true;

    /// <summary>Set once the bonus card has been added, so it is only granted once per run.</summary>
    [SavedProperty]
    public bool CardGranted { get; set; }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe];

    public override async Task AfterObtained()
    {
        if (CardGranted) return;
        var options = ModelDb.CardPool<RunePriestCardPool>().AllCards
            .Where(c => c.Rarity == CardRarity.Common && HasRuneInName(c)).ToList();
        var canonical = Owner.RunState.Rng.Niche.NextItem(options);
        if (canonical == null) return;

        CardGranted = true;
        var card = Owner.RunState.CreateCard(canonical, Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
    }

    /// <summary>
    /// Whether the card is named "… Rune" (class <c>XRuneCard</c>, ID <c>…_RUNE_CARD</c>). Uses the model ID rather
    /// than the localized title so every player in co-op gets the same options.
    /// </summary>
    private static bool HasRuneInName(CardModel card) => card.Id.Entry.RemovePrefix().Split('_').Contains("RUNE");

    public override Task BeforeCombatStart()
    {
        _usedThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_usedThisCombat || cardPlay.Card is not RuneCard || cardPlay.Card.Owner != Owner) return;
        _usedThisCombat = true;
        Flash();
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }
}
