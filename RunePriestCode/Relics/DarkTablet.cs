using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Relics;

/// <summary>
/// Ancient (offered by Darv, see <c>AncientOptionPatches</c>): your [Blood] runes damage enemies and your [Mend] runes
/// heal enemies instead of you. Both follow the current target mode like a Strike.
/// </summary>
public sealed class DarkTablet : RunePriestRelic, IRuneListener
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [..new BloodRune(0).HoverTips, ..new MendRune(0).HoverTips];

    public PayloadRune ReplacePayload(RuneContext ctx, PayloadRune rune) => rune switch
    {
        BloodRune blood => new EnemyBloodRune(blood.Value),
        MendRune mend => new EnemyMendRune(mend.Value),
        _ => rune
    };

    /// <summary>Blood that costs an enemy the HP instead of the caster.</summary>
    private sealed class EnemyBloodRune(int value) : PayloadRune(value)
    {
        public override string Key => "BLOOD";
        protected override Rune Revalued(int value) => new EnemyBloodRune(value);
        public override RuneTargeting Targeting => RuneTargeting.Enemy;

        public override Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets) =>
            CreatureCmd.Damage(ctx.ChoiceContext, targets, value, ValueProp.Unblockable | ValueProp.Unpowered, ctx.Creature, glyph.Source, null);
    }

    /// <summary>Mend that heals an enemy instead of the caster.</summary>
    private sealed class EnemyMendRune(int value) : PayloadRune(value)
    {
        public override string Key => "MEND";
        protected override Rune Revalued(int value) => new EnemyMendRune(value);
        public override RuneTargeting Targeting => RuneTargeting.Enemy;

        public override async Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets)
        {
            foreach (var target in targets)
                await CreatureCmd.Heal(target, value);
        }
    }
}
