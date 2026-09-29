using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;
/// <summary>Imbue a rune. Upgraded: also draw a card.</summary>
public sealed class BlankRuneCard() : RunePriestCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Imbue", 1m), new CardsVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => ImbueHoverTips;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Imbue(choiceContext, cardPlay, DynamicVars["Imbue"].IntValue);
        if (IsUpgraded) await Draw(choiceContext, DynamicVars.Cards.BaseValue);
    }
}
