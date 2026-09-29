using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// Choral Evocation: this turn, runes you Inscribe are also Inscribed for every other living player
/// (<see cref="RuneCmd.Share"/>). Gone at end of turn.
/// </summary>
public abstract class ChoralPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [..base.ExtraHoverTips, RuneTips.Inscribe];

    /// <summary>The part of an inscribed glyph that is shared, or null to share nothing.</summary>
    protected abstract Glyph? Shared(Glyph glyph);

    /// <summary>False while a stronger Choral power already shares these runes, so nothing is shared twice.</summary>
    protected virtual bool Active => true;

    public async Task AfterInscribed(PlayerChoiceContext choiceContext, Player player, IReadOnlyList<Glyph> glyphs)
    {
        if (player != Owner.Player || !Active || Owner.CombatState == null) return;

        var shared = glyphs.Select(Shared).OfType<Glyph>().ToList();
        if (shared.Count == 0) return;

        var others = Owner.CombatState.GetTeammatesOf(Owner)
            .Where(c => c != Owner && c.IsAlive && c.IsPlayer && c.Player != null)
            .Select(c => c.Player!)
            .ToList();
        if (others.Count == 0) return;

        Flash();
        foreach (var other in others)
            await RuneCmd.Share(choiceContext, other, shared, null);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner)) await PowerCmd.Remove(this);
    }
}

/// <summary>Shares only the Defend runes of each inscribed rune.</summary>
public sealed class ChoralEvocationPower : ChoralPower
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [..base.ExtraHoverTips, ..new DefendRune(0).HoverTips];

    protected override bool Active => Owner.GetPower<ChoralEvocationPlusPower>() == null;

    protected override Glyph? Shared(Glyph glyph) => glyph.Only(r => r is DefendRune);
}

/// <summary>Upgraded Choral Evocation: shares every inscribed rune.</summary>
public sealed class ChoralEvocationPlusPower : ChoralPower
{
    protected override Glyph? Shared(Glyph glyph) => glyph;
}
