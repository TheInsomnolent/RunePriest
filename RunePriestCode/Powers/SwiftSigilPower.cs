using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>Whenever a rune leaves your Incantation without being Spoken (removed or fizzled), draw Amount cards.</summary>
public sealed class SwiftSigilPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterRemoved(PlayerChoiceContext choiceContext, Player player, Glyph glyph)
    {
        if (player != Owner.Player) return;
        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, player);
    }

    public async Task AfterFizzle(RuneContext ctx, Glyph glyph)
    {
        Flash();
        if (ctx.Timing == SpeakTiming.Invoked)
            await CardPileCmd.Draw(ctx.ChoiceContext, Amount, ctx.Owner);
        else
            await PowerCmd.Apply<DrawCardsNextTurnPower>(ctx.ChoiceContext, Owner, Amount, Owner, null);
    }
}
