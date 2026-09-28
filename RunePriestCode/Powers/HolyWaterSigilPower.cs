using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>Whenever you heal, deal that much damage to a random enemy (Amount 1) or ALL enemies (Amount 2+).</summary>
public sealed class HolyWaterSigilPower : RunePriestPower
{
    public const int RandomEnemy = 1;
    public const int AllEnemies = 2;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner || delta <= 0 || Owner.CombatState == null) return;
        var enemies = Owner.CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0) return;

        Flash();
        var targets = Amount >= AllEnemies ? enemies : [Owner.Player!.RunState.Rng.CombatTargets.NextItem(enemies)!];
        await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), targets, delta, ValueProp.Unpowered, Owner);
    }
}
