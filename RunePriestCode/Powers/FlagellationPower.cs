using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// Whenever you lose HP during your turn (Blood runes included), Inscribe Defend Amount. Runes inscribed while the
/// Incantation is being Spoken are added after it, so they wait for the next Speak.
/// </summary>
public sealed class FlagellationPower : RunePriestPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [..base.ExtraHoverTips, RuneTips.Inscribe, ..new DefendRune(0).HoverTips];

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0 || Owner.Player == null) return;
        if (Owner.CombatState?.CurrentSide != CombatSide.Player) return;

        Flash();
        await RuneCmd.Inscribe(choiceContext, Owner.Player, [Glyph.Of(new DefendRune(Amount))], null);
    }
}
