using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Ancient;

/// <summary>
/// Ascended Skill - Cost 0
/// Apply Imbue effect (grant +1 imbuement). Draw 3 cards.
/// </summary>
public sealed class Strategi() : RunePriestCard(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => ImbueHoverTips;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Apply one Imbue effect
        await Imbue(choiceContext, cardPlay, 1);
        
        // Draw 3 cards
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
    }

    /// <summary>Ascended cards never upgrade.</summary>
    public override int MaxUpgradeLevel => 0;

    protected override void OnUpgrade()
    {
    }
}
