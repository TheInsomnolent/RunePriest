using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;

/// <summary>Speak the Incantation now without clearing it.</summary>
public sealed class Rehearse() : RunePriestCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Speak];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        RuneCmd.Speak(choiceContext, Owner, SpeakTiming.Invoked, keep: true);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
