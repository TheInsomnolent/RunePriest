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
/// <summary>Inscribe Echo then Nova, twice. Upgraded: Imbue first.</summary>
public sealed class FollysMirror() : RuneCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Echo", 1m), new IntVar("Imbue", 1m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => IsUpgraded ? ImbueHoverTips : [];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
    [
        Glyph.Of(new EchoRune(Var("Echo"))), Glyph.Of(new TargetRune(TargetMode.Nova)),
        Glyph.Of(new EchoRune(Var("Echo"))), Glyph.Of(new TargetRune(TargetMode.Nova))
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded) await Imbue(choiceContext, cardPlay, DynamicVars["Imbue"].IntValue);
        await base.OnPlay(choiceContext, cardPlay);
    }
}
