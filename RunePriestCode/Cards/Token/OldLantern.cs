using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Special;
/// <summary>X cost: Inscribe Kindle X. Upgraded: loses Exhaust.</summary>
[Pool(typeof(TokenCardPool))]
public sealed class OldLantern() : RunePriestCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe, ..new KindleRune(1).HoverTips];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var x = ResolveEnergyXValue();
        if (x <= 0) return;
        await RuneCmd.Inscribe(choiceContext, Owner, [Glyph.Of(new KindleRune(x))], this);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
