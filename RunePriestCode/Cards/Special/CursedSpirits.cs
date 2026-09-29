using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Special;
/// <summary>Cursed weapon (Unforgiveable Curse): summon spirits that strike at end of turn, and add a Black Mark to your hand.</summary>
[Pool(typeof(TokenCardPool))]
public sealed class CursedSpirits() : RunePriestCard(1, CardType.Power, CardRarity.Token, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<CursedSpiritsPower>(3m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<CursedSpiritsPower>(), HoverTipFactory.FromCard<BlackMark>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CursedSpiritsPower>(choiceContext, Owner.Creature,
            DynamicVars["CursedSpiritsPower"].BaseValue, Owner.Creature, this);
        await AddToHand<BlackMark>(1);
    }

    protected override void OnUpgrade() => DynamicVars["CursedSpiritsPower"].UpgradeValueBy(2m);
}
