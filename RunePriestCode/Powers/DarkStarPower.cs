using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>While a Void rune is in your Incantation, draw Amount cards at the start of each turn.</summary>
public sealed class DarkStarPower : RunePriestPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        var buffer = RuneCmd.GetBuffer(Owner);
        if (buffer == null || !buffer.Glyphs.Any(g => g.Runes.Any(r => r is VoidRune))) return;
        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, player);
    }
}
