using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Powers;

/// <summary>
/// This turn, each rune inscribed this turn is inscribed with + Mend 3.
/// Gone at end of turn.
/// </summary>
public sealed class HealerPower : RunePriestPower, IRuneListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    IReadOnlyList<Glyph> IRuneListener.ModifyInscription(Player player, IReadOnlyList<Glyph> glyphs)
        => ModifyInscription(player, glyphs);

    public IReadOnlyList<Glyph> ModifyInscription(Player player, IReadOnlyList<Glyph> glyphs)
    {
        if (player != Owner.Player) return glyphs;
        
        // Only payload glyphs can hold another payload rune; anything else would become malformed and fizzle.
        return glyphs.Select(g => g.Kind == RuneKind.Payload ? g.WithRunes([..g.Runes, new MendRune(3)]) : g).ToList();
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner)) 
            await PowerCmd.Remove(this);
    }
}
