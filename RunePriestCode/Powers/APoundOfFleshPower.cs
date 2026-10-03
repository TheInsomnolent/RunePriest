using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>At the start of your turn, gain 1 Energy and Inscribe Blood {Amount}.</summary>
public sealed class APoundOfFleshPower : RunePriestPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        Flash();
        await PlayerCmd.GainEnergy(1, player);
        await RuneCmd.Inscribe(choiceContext, player, [Glyph.Of(new BloodRune(Amount))], null);
    }
}
