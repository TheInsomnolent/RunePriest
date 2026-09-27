using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;

/// <summary>Speak only the first glyph of the Incantation, right now.</summary>
public sealed class Utter() : RunePriestCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Speak];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        RuneCmd.SpeakAt(choiceContext, Owner, 0);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
