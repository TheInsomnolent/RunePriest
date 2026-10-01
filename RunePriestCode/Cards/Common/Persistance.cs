using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;
/// <summary>Inscribe Growth 1. Upgraded: also draw a card.</summary>
public sealed class Persistance() : RuneCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Growth", 1m), new CardsVar(1)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new GrowthRune(Var("Growth")))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.OnPlay(choiceContext, cardPlay);
        if (IsUpgraded) await Draw(choiceContext, DynamicVars.Cards.BaseValue);
    }
}
