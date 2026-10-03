using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;

namespace RunePriest.RunePriestCode.Ancients;

/// <summary>
/// Base pattern for Rune Priest–specific ancient choices.
/// Ancient choices are endgame upgrades offered during ascension. Each choice modifies the player's
/// deck, relics, or powers for the remainder of the run.
///
/// Extend this class to create new character-unique ancients that integrate with rune mechanics.
///
/// Example: Orobas ancient upgrades the starting rune to an Ascended variant.
/// </summary>
public abstract class RunePriestAncientChoice
{
    /// <summary>
    /// Unique identifier for the ancient (e.g., "RUNEPRIEST-OROBAS").
    /// Used for localization, logging, and deduplication.
    /// </summary>
    public abstract string Id { get; }

    /// <summary>
    /// Localized title. Typically: <c>new LocString("ancients", Id + ".title")</c>
    /// </summary>
    public abstract LocString Title { get; }

    /// <summary>
    /// Localized description. Typically: <c>new LocString("ancients", Id + ".description")</c>
    /// </summary>
    public abstract LocString Description { get; }

    /// <summary>
    /// Apply the ancient's effect when selected. Execute any modifications to the player's state.
    /// Called once when the ancient choice is accepted.
    /// Note: Implement specific method signature based on game integration (Neow API, event system, etc.).
    /// </summary>
    public abstract Task Apply(Player player);

    /// <summary>
    /// Whether this ancient can be offered to this player.
    /// Override to restrict to specific characters or run modifiers.
    /// </summary>
    public virtual bool CanOffer(Player player)
    {
        return player.Character is Character.RunePriest;
    }
}
