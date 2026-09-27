using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace RunePriest.RunePriestCode.Runes;

/// <summary>Implemented by powers/relics on the speaking player to react to or reshape the Incantation.</summary>
public interface IRuneListener
{
    /// <summary>Adjusts a payload's base value before modifiers are applied. Must be side-effect free (used by preview).</summary>
    int ModifyRuneValue(RuneContext ctx, PayloadRune rune, int value) => value;

    /// <summary>Adjusts how many times a Loop repeats. Must be side-effect free.</summary>
    int ModifyLoopCount(RuneContext ctx, LoopRune loop, int count) => count;

    /// <summary>Glyph limit; null = unlimited. Combine with <see cref="RuneListeners.Tighten"/>.</summary>
    int? ModifyCapacity(int? capacity) => capacity;

    /// <summary>If any listener returns true, Speaking does not clear the Incantation.</summary>
    bool KeepsIncantation => false;

    Task AfterInscribed(PlayerChoiceContext choiceContext, Player player, IReadOnlyList<Glyph> glyphs) => Task.CompletedTask;

    Task AfterPayload(RuneContext ctx, PayloadRune rune, int value, IReadOnlyList<Creature> targets) => Task.CompletedTask;

    Task AfterFizzle(RuneContext ctx, Glyph glyph) => Task.CompletedTask;

    Task AfterSpeak(RuneContext ctx) => Task.CompletedTask;
}

public static class RuneListeners
{
    public static IReadOnlyList<IRuneListener> Of(Player player) =>
        player.Creature.Powers.OfType<IRuneListener>().Concat(player.Relics.OfType<IRuneListener>()).ToList();

    public static int? Capacity(Player player) => Of(player).Aggregate((int?)null, (cap, l) => l.ModifyCapacity(cap));

    public static int? Tighten(int? capacity, int limit) => capacity == null ? limit : Math.Min(capacity.Value, limit);
}
