using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;

/// <summary>
/// Multiplayer only. This turn, Defend runes you Inscribe are also Inscribed for all other players; upgraded, every
/// rune is.
/// </summary>
public sealed class ChoralEvocation() : RunePriestCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [RuneTips.Inscribe, ..new DefendRune(0).HoverTips];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded)
            await PowerCmd.Apply<ChoralEvocationPlusPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        else
            await PowerCmd.Apply<ChoralEvocationPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
}
