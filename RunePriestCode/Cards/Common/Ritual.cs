using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;
/// <summary>Pay Blood, then Speak the Incantation right now; its runes (the Blood too) remain afterwards.</summary>
public sealed class Ritual() : RuneCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Blood", 5m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [RuneTips.Speak];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new BloodRune(Var("Blood")))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.OnPlay(choiceContext, cardPlay);
        await RuneCmd.Speak(choiceContext, Owner, SpeakTiming.Invoked, keep: true);
    }

    protected override void OnUpgrade() => DynamicVars["Blood"].UpgradeValueBy(-2m);
}
