using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// At the start of your turn, Inscribe Scatter, Twin ×2 and Loop 1. While this power is active, Scatter picks its
/// target from every creature — players included (<see cref="IRuneListener.ScatterTargetsAnyone"/>).
/// </summary>
public sealed class ChaosFallsPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public bool ScatterTargetsAnyone => true;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        Flash();
        await RuneCmd.Inscribe(choiceContext, player,
            [Glyph.Of(new TargetRune(TargetMode.Scatter)), Glyph.Of(new TwinRune()), Glyph.Of(new LoopRune(1))], null);
    }
}
