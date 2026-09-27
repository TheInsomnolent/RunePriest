using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Status;

/// <summary>Curse: when drawn, Inscribe Blood. Added by the Cursed Quill relic.</summary>
[Pool(typeof(CurseCardPool))]
public sealed class StrayRune() : RunePriestCard(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HpLossVar("Blood", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe, ..new BloodRune(0).HoverTips];

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        await RuneCmd.Inscribe(choiceContext, Owner, [Glyph.Of(new BloodRune(DynamicVars["Blood"].IntValue))], this);
    }
}
