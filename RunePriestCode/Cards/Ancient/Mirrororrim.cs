using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Ancient Power (Darv's Dusty Tome card for the Rune Priest, via <see cref="ITomeCard"/>): at the end of each turn,
/// before the Incantation is Spoken, Inscribe [End Loop][Reflection]. Ethereal; upgrading removes Ethereal.
/// </summary>
public sealed class Mirrororrim() : RunePriestCard(2, CardType.Power, CardRarity.Ancient, TargetType.Self), ITomeCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MirrororrimPower>(), RuneTips.Inscribe,
        ..new EndLoopRune().HoverTips, ..new ReflectionRune().HoverTips
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<MirrororrimPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Ethereal);
}
