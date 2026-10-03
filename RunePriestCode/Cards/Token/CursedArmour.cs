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
/// <summary>Cursed weapon (Unforgiveable Curse): no Block until next turn, then Mend what you lost. Upgraded: also reflect.</summary>
[Pool(typeof(TokenCardPool))]
public sealed class CursedArmour() : RunePriestCard(1, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CursedArmourPower>(),
        ..(IsUpgraded ? [HoverTipFactory.FromPower<CursedReflectPower>()] : Array.Empty<IHoverTip>()),
        RuneTips.Inscribe,
        ..new MendRune(0).HoverTips
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CursedArmourPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        if (IsUpgraded)
            await PowerCmd.Apply<CursedReflectPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
}
