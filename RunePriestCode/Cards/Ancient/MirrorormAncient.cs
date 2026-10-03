using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Mirrororrim: Ancient Power, Cost 2
/// Applies MirrorormPower: at end of turn, inscribe two runes that create program loops:
/// - EndLoopRune: Closes a loop block
/// - ReflectionRune: Reverses the Incantation (earlier runes Spoken backward)
/// Ethereal: card is discarded at end of turn after power is applied.
/// Upgradable: Remove ethereal (power persists across turns).
/// </summary>
public sealed class MirrorormAncient() : RunePriestCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<MirrorormAncientPower>(), RuneTips.Inscribe];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<MirrorormAncientPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}
