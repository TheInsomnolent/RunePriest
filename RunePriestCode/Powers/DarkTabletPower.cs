using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// Power granted by Dark Tablet ancient. Blood/Mend runes damage/heal enemies instead of the player.
/// Co-op safe: only applies to the Darv player who has the ancient selected.
/// </summary>
public sealed class DarkTabletPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>
    /// Replace Blood/Mend runes with their flipped variants.
    /// BloodRune targets Self (player), so we wrap it in a ModifierRune-like effect that flips targeting.
    /// MendRune targets Ally, so we flip it to target Enemy.
    /// </summary>
    public PayloadRune ReplacePayload(RuneContext ctx, PayloadRune rune)
    {
        // Blood rune: instead of damaging self, damage an enemy
        if (rune is BloodRune blood)
        {
            return new FlippedBloodRune(blood.Value);
        }

        // Mend rune: instead of healing ally, heal an enemy (damage them instead, but as healing)
        if (rune is MendRune mend)
        {
            return new FlippedMendRune(mend.Value);
        }

        return rune;
    }

    /// <summary>Custom rune: Blood damage to enemy instead of self.</summary>
    private sealed class FlippedBloodRune(int value) : PayloadRune(value)
    {
        public override string Key => "BLOOD";
        public override Rune WithValue(int value) => new FlippedBloodRune(value);
        public override RuneTargeting Targeting => RuneTargeting.Enemy;

        public override async Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets)
        {
            foreach (var target in targets)
                await CreatureCmd.Damage(ctx.ChoiceContext, target, value, ValueProp.Unblockable | ValueProp.Unpowered, glyph.Source, null);
        }
    }

    /// <summary>Custom rune: Mend heal to enemy instead of ally.</summary>
    private sealed class FlippedMendRune(int value) : PayloadRune(value)
    {
        public override string Key => "MEND";
        public override Rune WithValue(int value) => new FlippedMendRune(value);
        public override RuneTargeting Targeting => RuneTargeting.Enemy;

        public override async Task Resolve(RuneContext ctx, Glyph glyph, int value, IReadOnlyList<Creature> targets)
        {
            foreach (var target in targets)
                await CreatureCmd.Heal(target, value);
        }
    }
}
