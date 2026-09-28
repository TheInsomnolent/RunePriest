using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
public sealed class HolyWaterSigil() : RunePriestCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<HolyWaterSigilPower>()];

    // Amount 1 = a random enemy; upgraded amount 2 = ALL enemies (see HolyWaterSigilPower).
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HolyWaterSigilPower>(choiceContext, Owner.Creature,
            IsUpgraded ? HolyWaterSigilPower.AllEnemies : HolyWaterSigilPower.RandomEnemy, Owner.Creature, this);
    }
}
