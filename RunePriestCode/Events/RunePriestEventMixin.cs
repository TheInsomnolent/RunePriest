using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Players;

namespace RunePriest.RunePriestCode.Events;

/// <summary>
/// Utility mixin for handling RunePriest-specific event filtering and character checks.
/// </summary>
public static class RunePriestEventMixin
{
    /// <summary>
    /// Prefix for RunePriest-exclusive event IDs.
    /// Example: "RUNEPRIEST_FORBIDDEN_CODEX"
    /// </summary>
    private const string RunePriestPrefix = "RUNEPRIEST_";

    /// <summary>
    /// Check if an event ID indicates a RunePriest-exclusive event.
    /// </summary>
    public static bool IsRunePriestOnly(string eventId)
    {
        return eventId.StartsWith(RunePriestPrefix);
    }

    /// <summary>
    /// Check if a player can experience this event based on their character.
    /// RunePriest-only events are restricted to RunePriest; generic events are available to all.
    /// </summary>
    public static bool PlayerCanExperience(string eventId, Player player)
    {
        if (IsRunePriestOnly(eventId))
            return player.Character is Character.RunePriest;
        return true; // Generic events available to all characters
    }

    /// <summary>
    /// Filter an event pool by character availability.
    /// Removes RunePriest events if player is not RunePriest; keeps others.
    /// </summary>
    public static List<T> FilterByCharacter<T>(List<T> events, Player player, Func<T, string> getEventId)
    {
        return events
            .Where(e => PlayerCanExperience(getEventId(e), player))
            .ToList();
    }

    /// <summary>
    /// Log an event trigger with RunePriest context.
    /// </summary>
    public static void LogEventTrigger(string eventId)
    {
        MainFile.Logger.Info($"[Rune] Event triggered: {eventId}");
    }
}
