using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>Whenever a rune is removed from your Incantation or Imbued, draw Amount cards.</summary>
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

    public async Task AfterImbued(PlayerChoiceContext choiceContext, Player player, Glyph glyph)
    {
        if (player != Owner.Player) return;
        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, player);
    }
}
