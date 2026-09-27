using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace RunePriest.RunePriestCode.Cards.Common;

/// <summary>Block, plus a bonus if the Incantation is already long.</summary>
public sealed class EtchedGuard() : RunePriestCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private const int Threshold = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(6m, ValueProp.Move), new BlockVar("Bonus", 4m, ValueProp.Move), new IntVar("Threshold", Threshold)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (IncantationSize >= Threshold)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["Bonus"].BaseValue, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}
