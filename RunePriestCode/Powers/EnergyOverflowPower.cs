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

/// <summary>Whenever a rune fizzles, deal Amount damage to ALL enemies.</summary>
public sealed class EnergyOverflowPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterFizzle(RuneContext ctx, Glyph glyph)
    {
        var enemies = Owner.CombatState?.HittableEnemies.ToList();
        if (enemies == null || enemies.Count == 0) return;
        Flash();
        await CreatureCmd.Damage(ctx.ChoiceContext, enemies, Amount, ValueProp.Unpowered, Owner);
    }
}
