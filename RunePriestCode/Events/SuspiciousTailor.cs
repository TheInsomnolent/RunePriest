using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using RunePriest.RunePriestCode.Cards.Event;
using RunePriest.RunePriestCode.Relics;

namespace RunePriest.RunePriestCode.Events;

/// <summary>
/// Act 3: a horned tailor offers the Lightweight Cloth Robe; declining leaves you with a Blood Rune card instead. Only
/// offered when every player uses runes and doesn't own the robe yet.
/// </summary>
public sealed class SuspiciousTailor : RuneEvent
{
    public override ActModel[] Acts => Act3;

    protected override string PlaceholderPortrait => "spirit_grafter";

    protected override bool Qualifies(Player player) =>
        UsesRunes(player) && player.Relics.All(r => r is not LightweightClothRobe);

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Qualifies(Owner!)
            ? Choice(Trade, "TRADE", HoverTipFactory.FromRelic<LightweightClothRobe>().ToArray())
            : Locked("TRADE"),
        Choice(Decline, "DECLINE", HoverTipFactory.FromCard<BloodRuneCard>())
    ];

    private async Task Trade()
    {
        await RelicCmd.Obtain(ModelDb.Relic<LightweightClothRobe>().ToMutable(), Owner!);
        Finish("TRADE");
    }

    private async Task Decline()
    {
        var card = Owner!.RunState.CreateCard<BloodRuneCard>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
        Finish("DECLINE");
    }
}
