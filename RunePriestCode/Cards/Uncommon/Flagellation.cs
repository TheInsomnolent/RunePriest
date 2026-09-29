using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>Whenever you lose HP during your turn, Inscribe Defend.</summary>
public sealed class Flagellation() : RunePriestCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FlagellationPower>(4m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FlagellationPower>(), RuneTips.Inscribe, ..new DefendRune(0).HoverTips];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FlagellationPower>(choiceContext, Owner.Creature,
            DynamicVars["FlagellationPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
