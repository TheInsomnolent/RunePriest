using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
public sealed class PaladinSigil() : RunePriestCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PaladinSigilPower>(1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Imbue, HoverTipFactory.FromPower<PaladinSigilPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PaladinSigilPower>(choiceContext, Owner.Creature,
            DynamicVars["PaladinSigilPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
