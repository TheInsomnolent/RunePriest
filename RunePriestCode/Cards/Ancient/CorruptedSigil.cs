using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Powers;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Ancient Power (from Vakuu's Corrupted Sigil relic): cards with "Rune" in their name are Ethereal, Exhaust and cost 0;
/// runes remain between turns. Upgrade: rune cards are no longer Ethereal.
/// </summary>
public sealed class CorruptedSigil() : RunePriestCard(0, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<CorruptedSigilPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var power = await PowerCmd.Apply<CorruptedSigilPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        if (IsUpgraded) power?.RemoveEthereal();
    }

    protected override void OnUpgrade()
    {
    }
}
