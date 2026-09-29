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
/// <summary>Inscribe a Loop, then put Scatter at the very start of the Incantation.</summary>
public sealed class FrenziedIncant() : RuneCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Loop", 1m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new TargetRune(TargetMode.Scatter).HoverTips;

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new LoopRune(Var("Loop")))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.OnPlay(choiceContext, cardPlay);
        await RuneCmd.Prepend(choiceContext, Owner, [Glyph.Of(new TargetRune(TargetMode.Scatter))], this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
