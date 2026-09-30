using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// Whenever you Imbue a rune, Inscribe a copy of it for a random other player (<see cref="RuneCmd.Share"/>, so the
/// copy belongs to them and raises no inscription listeners).
/// </summary>
public sealed class ConjurePortalPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public async Task AfterImbued(PlayerChoiceContext choiceContext, Player player, Glyph glyph)
    {
        if (player != Owner.Player || Owner.CombatState == null) return;

        var others = Owner.CombatState.GetTeammatesOf(Owner)
            .Where(c => c != Owner && c.IsAlive && c.IsPlayer && c.Player != null)
            .Select(c => c.Player!)
            .ToList();
        if (others.Count == 0) return;

        Flash();
        var ally = player.RunState.Rng.CombatTargets.NextItem(others)!;
        await RuneCmd.Share(choiceContext, ally, [glyph.Copy()], null);
    }
}
