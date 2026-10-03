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
/// <summary>Inscribe a Loop, then put Scatter at the very start of the Incantation. Upgraded: Imbue first.</summary>
public sealed class FrenziedIncant() : RuneCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Imbue", 1m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [..new TargetRune(TargetMode.Scatter).HoverTips, ..IsUpgraded ? ImbueHoverTips : []];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new LoopRune())];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded) await Imbue(choiceContext, cardPlay, DynamicVars["Imbue"].IntValue);
        await base.OnPlay(choiceContext, cardPlay);
        await RuneCmd.Prepend(choiceContext, Owner, [Glyph.Of(new TargetRune(TargetMode.Scatter))], this);
    }

    public override void PreviewIncantation(IncantationDraft draft, Creature? anchor)
    {
        if (IsUpgraded) PreviewImbue(draft, anchor, DynamicVars["Imbue"].IntValue);
        base.PreviewIncantation(draft, anchor);
        draft.Prepend([Glyph.Of(new TargetRune(TargetMode.Scatter))], this);
    }
}
