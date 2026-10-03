using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Rare;

public sealed class APoundOfFlesh() : RunePriestCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Blood", 4m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<APoundOfFleshPower>(), RuneTips.Inscribe, ..new BloodRune(1).HoverTips];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<APoundOfFleshPower>(choiceContext, Owner.Creature, DynamicVars["Blood"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Blood"].UpgradeValueBy(-2m);
}
