using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>Remove the most recently inscribed rune and draw cards. Upgraded: also Imbue a rune.</summary>
public sealed class Alchemize() : RunePriestCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new IntVar("Imbue", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe, RuneTips.Imbue];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await RuneCmd.Remove(choiceContext, Owner);
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
        if (IsUpgraded) await Imbue(choiceContext, DynamicVars["Imbue"].IntValue);
    }
}
