using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Cards.Common;
/// <summary>Remove the last rune, then Inscribe Void. Upgraded: also draw a card.</summary>
public sealed class CleanSlate() : RuneCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) => [Glyph.Of(new VoidRune())];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await RuneCmd.Remove(choiceContext, Owner);
        await base.OnPlay(choiceContext, cardPlay);
        if (IsUpgraded) await Draw(choiceContext, DynamicVars.Cards.BaseValue);
    }

    public override void PreviewIncantation(IncantationDraft draft, Creature? anchor)
    {
        draft.Remove();
        base.PreviewIncantation(draft, anchor);
    }
}
