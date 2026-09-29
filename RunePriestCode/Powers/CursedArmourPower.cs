using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// Until your next turn you can't gain Block. At the start of your next turn, Inscribe a Mend equal to the HP you
/// lost meanwhile, then this power ends. Shows the HP lost so far.
/// </summary>
public sealed class CursedArmourPower : RunePriestPower
{
    private int _hpLost;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override int DisplayAmount => _hpLost;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [..base.ExtraHoverTips, RuneTips.Inscribe, ..new MendRune(0).HoverTips];

    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay) =>
        target == Owner ? 0m : 1m;

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        await base.AfterCurrentHpChanged(creature, delta);
        if (creature != Owner || delta >= 0) return;
        _hpLost += (int)Math.Ceiling(-delta);
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (_hpLost > 0)
        {
            Flash();
            await RuneCmd.Inscribe(choiceContext, player, [Glyph.Of(new MendRune(_hpLost))], null);
        }
        await PowerCmd.Remove(this);
    }
}
