using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace RunePriest.RunePriestCode.Runes;

public enum RuneTargeting
{
    /// <summary>Picks from enemies using the current target mode.</summary>
    Enemy,
    /// <summary>Picks from allies (anchor ally, else the caster) using the current target mode.</summary>
    Ally,
    /// <summary>Always the caster; ignores target mode.</summary>
    Self
}

public abstract class PayloadRune(int value) : Rune(value)
{
    public override RuneKind Kind => RuneKind.Payload;

    /// <summary>Whether Amplify/Twin affect this rune. Non-scalable runes still repeat in loops.</summary>
    public virtual bool Scalable => true;

    public abstract RuneTargeting Targeting { get; }

    public abstract Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets);
}

public sealed class StrikeRune(int value) : PayloadRune(value)
{
    public override string Key => "STRIKE";
    public override Rune WithValue(int value) => new StrikeRune(value);
    public override RuneTargeting Targeting => RuneTargeting.Enemy;

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

public sealed class WardRune(int value) : PayloadRune(value)
{
    public override string Key => "WARD";
    public override Rune WithValue(int value) => new WardRune(value);
    public override RuneTargeting Targeting => RuneTargeting.Ally;

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
    public override bool Scalable => false;
    public override RuneTargeting Targeting => RuneTargeting.Self;

    public override async Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets)
    {
        if (ctx.Timing == SpeakTiming.Invoked)
            await PlayerCmd.GainEnergy(value, ctx.Owner);
        else
            await PowerCmd.Apply<EnergyNextTurnPower>(ctx.ChoiceContext, ctx.Creature, value, ctx.Creature, glyph.Source);
    }
}

public sealed class InsightRune(int value) : PayloadRune(value)
{
    public override string Key => "INSIGHT";
    public override Rune WithValue(int value) => new InsightRune(value);
    public override bool Scalable => false;
    public override RuneTargeting Targeting => RuneTargeting.Self;

    public override async Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets)
    {
        if (ctx.Timing == SpeakTiming.Invoked)
            await CardPileCmd.Draw(ctx.ChoiceContext, value, ctx.Owner);
        else
            await PowerCmd.Apply<DrawCardsNextTurnPower>(ctx.ChoiceContext, ctx.Creature, value, ctx.Creature, glyph.Source);
    }
}

public sealed class HexRune(int value) : PayloadRune(value)
{
    public override string Key => "HEX";
    public override Rune WithValue(int value) => new HexRune(value);
    public override RuneTargeting Targeting => RuneTargeting.Enemy;
    public override IEnumerable<IHoverTip> HoverTips => [..base.HoverTips, HoverTipFactory.FromPower<WeakPower>()];

    public override Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets) =>
        PowerCmd.Apply<WeakPower>(ctx.ChoiceContext, targets, value, ctx.Creature, glyph.Source);
}

public sealed class ExposeRune(int value) : PayloadRune(value)
{
    public override string Key => "EXPOSE";
    public override Rune WithValue(int value) => new ExposeRune(value);
    public override RuneTargeting Targeting => RuneTargeting.Enemy;
    public override IEnumerable<IHoverTip> HoverTips => [..base.HoverTips, HoverTipFactory.FromPower<VulnerablePower>()];

    public override Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets) =>
        PowerCmd.Apply<VulnerablePower>(ctx.ChoiceContext, targets, value, ctx.Creature, glyph.Source);
}

public sealed class VenomRune(int value) : PayloadRune(value)
{
    public override string Key => "VENOM";
    public override Rune WithValue(int value) => new VenomRune(value);
    public override RuneTargeting Targeting => RuneTargeting.Enemy;
    public override IEnumerable<IHoverTip> HoverTips => [..base.HoverTips, HoverTipFactory.FromPower<PoisonPower>()];

    public override Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets) =>
        PowerCmd.Apply<PoisonPower>(ctx.ChoiceContext, targets, value, ctx.Creature, glyph.Source);
}
