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

    public IReadOnlyList<Glyph> ModifyInscription(Player player, IReadOnlyList<Glyph> glyphs, bool preview)
    {
        if (player != Owner.Player) return glyphs;
        
        // Only payload glyphs can hold another payload rune; anything else would become malformed and fizzle.
        // The Mend comes from Healer (an Ascended card), so it is radiant even when the rest of the glyph isn't.
        return glyphs.Select(g => g.Kind == RuneKind.Payload ? g.WithRunes([..g.Runes, new MendRune(3).AsRadiant()]) : g).ToList();
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner)) 
            await PowerCmd.Remove(this);
    }
}
