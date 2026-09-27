using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>Whenever a Blood rune resolves, deal (value × Amount) damage to ALL enemies.</summary>
public sealed class InkCovenantPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterPayload(RuneContext ctx, PayloadRune rune, int value, IReadOnlyList<Creature> targets)
    {
        if (rune is not BloodRune) return;
        var enemies = ctx.CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0) return;
        Flash();
        await CreatureCmd.Damage(ctx.ChoiceContext, enemies, value * Amount, ValueProp.Unpowered, Owner);
    }
}
