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
/// Amount = spirits. Like damage orbs: at the end of your turn each spirit deals <see cref="SpiritDamage"/> damage
/// (unpowered, so Strength doesn't apply) to a random enemy with Black Mark. Without a marked enemy, spirits wait.
/// Gain a spirit at the start of each of your turns.
/// </summary>
public sealed class CursedSpiritsPower : RunePriestPower
{
    public const int SpiritDamage = 5;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [..base.ExtraHoverTips, HoverTipFactory.FromPower<BlackMarkPower>()];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        await PowerCmd.ModifyAmount(choiceContext, this, 1m, Owner, null);
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner.Player == null || !participants.Contains(Owner)) return;
        for (var i = 0; i < Amount; i++)
        {
            var marked = Owner.CombatState?.HittableEnemies.Where(e => e.Powers.OfType<BlackMarkPower>().Any()).ToList();
            if (marked == null || marked.Count == 0) return;
            var target = Owner.Player.RunState.Rng.CombatTargets.NextItem(marked);
            if (target == null) return;
            if (i == 0) Flash();
            await CreatureCmd.Damage(choiceContext, target, SpiritDamage, ValueProp.Unpowered, Owner);
        }
    }
}
