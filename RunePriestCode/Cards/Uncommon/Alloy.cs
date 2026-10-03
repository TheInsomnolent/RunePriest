using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Uncommon;
/// <summary>
/// Remove the most recently inscribed rune; only if one was removed, draw cards. Upgraded: also Imbue a rune
/// (an already-Imbued Alloy Inscribes its bound runes first, so there is always one to remove).
/// </summary>
public sealed class Alloy() : RunePriestCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new IntVar("Imbue", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [RuneTips.Inscribe, ..ImbueHoverTips];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded && IsImbued) await Imbue(choiceContext, cardPlay, DynamicVars["Imbue"].IntValue);
        var removed = await RuneCmd.Remove(choiceContext, Owner);
        if (removed != null) await Draw(choiceContext, DynamicVars.Cards.BaseValue);
        if (IsUpgraded && !IsImbued) await Imbue(choiceContext, cardPlay, DynamicVars["Imbue"].IntValue);
    }

    public override void PreviewIncantation(IncantationDraft draft, Creature? anchor)
    {
        if (IsUpgraded && IsImbued) PreviewImbue(draft, anchor, DynamicVars["Imbue"].IntValue);
        draft.Remove();
        if (IsUpgraded && !IsImbued) PreviewImbue(draft, anchor, DynamicVars["Imbue"].IntValue);
    }
}
