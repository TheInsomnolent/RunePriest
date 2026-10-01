using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Powers;

namespace RunePriest.RunePriestCode.Runes;

public enum RuneTargeting
{
    /// <summary>Picks from enemies using the current target mode.</summary>
    Enemy,
    /// <summary>Supportive: always the caster (runes never affect other players), but Mirror turns it on an enemy.</summary>
    Ally,
    /// <summary>Always the caster; ignores target mode.</summary>
    Self
}

public abstract class PayloadRune(int value) : Rune(value)
{
    public override RuneKind Kind => RuneKind.Payload;

    /// <summary>Payloads are amplifiable unless they opt out. Non-amplifiable runes still repeat in loops.</summary>
    public override bool Amplifiable => true;

    public abstract RuneTargeting Targeting { get; }

    public abstract Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets);

    /// <summary>
    /// <paramref name="value"/> after game effects (Strength, Weak, Vulnerable, Frail…) as it would resolve against
    /// <paramref name="target"/> (null: not a single known creature). Must stay side-effect free.
    /// </summary>
    public virtual int Modified(Player owner, Glyph glyph, Creature? target, int value) => value;

    /// <summary>Mirrors the damage a Strike deals when it resolves.</summary>
    protected static int ModifiedAttack(Player owner, Glyph glyph, Creature? target, int value) =>
        owner.Creature.CombatState is { } combat
            ? (int)Hook.ModifyDamage(owner.RunState, combat, target, owner.Creature, value, ValueProp.Move, glyph.Source,
                null, ModifyDamageHookType.All, CardPreviewMode.None, out _)
            : value;
}

public sealed class StrikeRune(int value) : PayloadRune(value)
{
    public override string Key => "STRIKE";
    public override Rune WithValue(int value) => new StrikeRune(value);
    public override RuneTargeting Targeting => RuneTargeting.Enemy;

    public override int Modified(Player owner, Glyph glyph, Creature? target, int value) =>
        ModifiedAttack(owner, glyph, target, value);

    public override async Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets)
    {
        // A real attack needs a source card; relic/power-inscribed strikes fall back to powered damage.
        if (glyph.Source == null)
        {
            await CreatureCmd.Damage(ctx.ChoiceContext, targets, value, ValueProp.Move, ctx.Creature, null, null);
            return;
        }

        var attack = DamageCmd.Attack(value).FromCard(glyph.Source, null).WithHitFx("vfx/vfx_attack_slash");
        if (targets.Count > 1 && targets.All(t => t.IsEnemy))
        {
            await attack.TargetingAllOpponents(ctx.CombatState).Execute(ctx.ChoiceContext);
            return;
        }

        // Mirrored Nova can hit several allies; attack each one separately.
        await attack.Targeting(targets[0]).Execute(ctx.ChoiceContext);
        foreach (var target in targets.Skip(1))
            await DamageCmd.Attack(value).FromCard(glyph.Source, null).Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash").Execute(ctx.ChoiceContext);
    }
}

public sealed class DefendRune(int value) : PayloadRune(value)
{
    public override string Key => "DEFEND";
    public override Rune WithValue(int value) => new DefendRune(value);
    public override RuneTargeting Targeting => RuneTargeting.Ally;

    public override int Modified(Player owner, Glyph glyph, Creature? target, int value) =>
        owner.Creature.CombatState is { } combat
            ? (int)Hook.ModifyBlock(combat, target ?? owner.Creature, value, ValueProp.Move, null, null, out _)
            : value;

    public override async Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets)
    {
        foreach (var target in targets)
            await CreatureCmd.GainBlock(target, value, ValueProp.Move, null);
    }
}

public sealed class MendRune(int value) : PayloadRune(value)
{
    public override string Key => "MEND";
    public override Rune WithValue(int value) => new MendRune(value);
    public override RuneTargeting Targeting => RuneTargeting.Ally;

    public override async Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets)
    {
        foreach (var target in targets)
            await CreatureCmd.Heal(target, value);
    }
}

/// <summary>Removes every debuff from you (the caster, regardless of target mode). Has no value; never merges.</summary>
public sealed class CleanseRune() : PayloadRune(1)
{
    public override string Key => "CLEANSE";
    public override bool ShowsValue => false;
    public override bool Amplifiable => false;
    public override RuneTargeting Targeting => RuneTargeting.Self;

    public override async Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets)
    {
        foreach (var target in targets)
        foreach (var debuff in target.Powers.Where(p => p.Type == PowerType.Debuff).ToList())
            await PowerCmd.Remove(debuff);
    }
}

public sealed class BloodRune(int value) : PayloadRune(value)
{
    public override string Key => "BLOOD";
    public override Rune WithValue(int value) => new BloodRune(value);
    public override RuneTargeting Targeting => RuneTargeting.Self;

    public override Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets) =>
        CreatureCmd.Damage(ctx.ChoiceContext, ctx.Creature, value, ValueProp.Unblockable | ValueProp.Unpowered, glyph.Source, null);
}

public sealed class KindleRune(int value) : PayloadRune(value)
{
    public override string Key => "KINDLE";
    public override Rune WithValue(int value) => new KindleRune(value);
    public override bool Amplifiable => false;
    public override RuneTargeting Targeting => RuneTargeting.Self;

    public override async Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets)
    {
        if (ctx.Timing == SpeakTiming.Invoked)
            await PlayerCmd.GainEnergy(value, ctx.Owner);
        else
            await PowerCmd.Apply<EnergyNextTurnPower>(ctx.ChoiceContext, ctx.Creature, value, ctx.Creature, glyph.Source);
    }
}

public sealed class SwiftRune(int value) : PayloadRune(value)
{
    public override string Key => "SWIFT";
    public override Rune WithValue(int value) => new SwiftRune(value);
    public override bool Amplifiable => false;
    public override RuneTargeting Targeting => RuneTargeting.Self;

    public override async Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets)
    {
        if (ctx.Timing == SpeakTiming.Invoked)
            await CardPileCmd.Draw(ctx.ChoiceContext, value, ctx.Owner);
        else
            await PowerCmd.Apply<DrawCardsNextTurnPower>(ctx.ChoiceContext, ctx.Creature, value, ctx.Creature, glyph.Source);
    }
}

/// <summary>Applies Weak and Vulnerable equal to its value.</summary>
public sealed class HexRune(int value = 1) : PayloadRune(value)
{
    public override string Key => "HEX";
    public override Rune WithValue(int value) => new HexRune(value);
    public override RuneTargeting Targeting => RuneTargeting.Enemy;

    public override IEnumerable<IHoverTip> HoverTips =>
        [..base.HoverTips, HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<VulnerablePower>()];

    public override async Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<WeakPower>(ctx.ChoiceContext, targets, value, ctx.Creature, glyph.Source);
        await PowerCmd.Apply<VulnerablePower>(ctx.ChoiceContext, targets, value, ctx.Creature, glyph.Source);
    }
}

/// <summary>
/// A lingering Strike: attacks like Strike, then persists into next turn with its value halved. Once the halved
/// value would drop below <see cref="FizzleThreshold"/>, it fizzles away instead. Halving and persistence are
/// handled by the interpreter.
/// </summary>
public sealed class DiminishRune(int value) : PayloadRune(value)
{
    public const int FizzleThreshold = 5;

    public override string Key => "DIMINISH";
    public override Rune WithValue(int value) => new DiminishRune(value);
    public override RuneTargeting Targeting => RuneTargeting.Enemy;

    public override int Modified(Player owner, Glyph glyph, Creature? target, int value) =>
        ModifiedAttack(owner, glyph, target, value);

    public override Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets) =>
        new StrikeRune(Value).Resolve(ctx, glyph, value, targets);
}
