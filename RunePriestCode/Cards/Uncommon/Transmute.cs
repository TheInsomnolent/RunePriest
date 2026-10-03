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
public sealed class Transmute() : RuneCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Imbue", 1m), new CardsVar("Swift", 3)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => ImbueHoverTips;

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new SwiftRune(Var("Swift")))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Imbue(choiceContext, cardPlay, DynamicVars["Imbue"].IntValue);
        await base.OnPlay(choiceContext, cardPlay);
    }

    public override void PreviewIncantation(IncantationDraft draft, Creature? anchor)
    {
        PreviewImbue(draft, anchor, DynamicVars["Imbue"].IntValue);
        base.PreviewIncantation(draft, anchor);
    }

    protected override void OnUpgrade() => DynamicVars["Imbue"].UpgradeValueBy(1m);
}
