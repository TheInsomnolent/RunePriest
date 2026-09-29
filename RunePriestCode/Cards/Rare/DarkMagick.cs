using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Powers;
using RunePriest.RunePriestCode.Runes;
using RunePriest.RunePriestCode.Cards.Uncommon;

namespace RunePriest.RunePriestCode.Cards.Rare;
/// <summary>Inscribe Void (upgraded: also Kindle), then add upgraded Loop Runes to your hand.</summary>
public sealed class DarkMagick() : RuneCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Kindle", 1m), new CardsVar(2)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<LoopRuneCard>(true)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor)
    {
        yield return Glyph.Of(new VoidRune());
        if (IsUpgraded) yield return Glyph.Of(new KindleRune(Var("Kindle")));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.OnPlay(choiceContext, cardPlay);
        await AddToHand<LoopRuneCard>(DynamicVars.Cards.IntValue, upgraded: true);
    }
}
