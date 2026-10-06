using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// Mirrororrim: at the end of each turn, Inscribe [Reflection] so it is part of this turn's Speak.
/// Uses <see cref="BeforeSideTurnEndEarly"/>: <see cref="IncantationPower"/> Speaks in <c>BeforeSideTurnEnd</c>, and
/// inscribing in the same hook could land after the Speak (the rune would then sit there into the next turn).
/// </summary>
public sealed class MirrororrimPower : RunePriestPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner.Player == null || !participants.Contains(Owner)) return;

        Flash();
        await RuneCmd.Inscribe(choiceContext, Owner.Player, [Glyph.Of(new ReflectionRune())], null);
    }
}
