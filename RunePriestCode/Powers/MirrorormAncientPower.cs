using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// Mirrororrim (Ancient): At the end of each turn, inscribe EndLoop + Reflection.
/// EndLoop closes a loop block; Reflection reverses the Incantation.
/// This persistent power remains until removed (or across turns if upgraded).
/// Ethereal by default (removed at end of turn via CorruptedSigilPower or automatic removal).
/// </summary>
public sealed class MirrorormAncientPower : RunePriestPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Owner.Player == null) return;

        Flash();
        // Inscribe EndLoop (closes a Loop construct)
        await RuneCmd.Inscribe(choiceContext, Owner.Player, [Glyph.Of(new EndLoopRune())], null);

        // Inscribe Reflection (reverses the Incantation)
        await RuneCmd.Inscribe(choiceContext, Owner.Player, [Glyph.Of(new ReflectionRune())], null);

        MainFile.Logger.Info("[Rune] MirrorormAncient: inscribed EndLoop + Reflection at turn end");
    }
}
