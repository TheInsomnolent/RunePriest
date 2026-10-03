# Custom Ancient Choices — Design & Implementation Guide

## Overview
**Ancient Choices** are endgame upgrades offered during ascension runs. Custom Ancients allow the Rune Priest to have character-specific choices that integrate deeply with rune mechanics.

The primary example is **Orobas** — a choice that upgrades the starting rune (via **Blessed Toolbox**) to an Ascended version, granting enhanced visuals and mechanics.

## BaseLib Integration

**STATUS**: Investigation required (see below)

The Rune Priest mod currently uses Harmony patches for character-specific mechanics (see `NeowAetherQuillPatch` as pattern). We will likely follow the same approach for custom ancients:

1. Patch the ancient choice generation system
2. Check if the player is RunePriest
3. Add or filter ancient options accordingly

### To Investigate
- [ ] Does BaseLib provide `CustomAncientModel`?
- [ ] Is there an ancient registration pool (like `CustomCardPoolModel`)?
- [ ] Which hook in the game's Ancient system is best to patch?
  - Search: `MegaCrit.Sts2.Core.Models.Ancients` or similar
  - Look for: ancient choice generation, ancient reward logic, Neow integration

---

## Architecture Pattern

### Base Class: RunePriestAncientChoice
```csharp
// RunePriestCode/Ancients/RunePriestAncientChoice.cs

using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace RunePriest.RunePriestCode.Ancients;

/// <summary>
/// Base pattern for Rune Priest–specific ancient choices.
/// Extend this to create new character-unique ancients.
/// </summary>
public abstract class RunePriestAncientChoice
{
    public abstract string Id { get; }
    public abstract LocString Title { get; }
    public abstract LocString Description { get; }

    /// <summary>Apply the ancient's effect when selected (co-op safe).</summary>
    public abstract Task Apply(Player player, PlayerChoiceContext ctx);

    /// <summary>Whether this ancient can be offered to this player.</summary>
    public virtual bool CanOffer(Player player) => player.Character is Character.RunePriest;

    /// <summary>
    /// Helper: upgrade a card in the starting deck or hand.
    /// </summary>
    protected void UpgradeCardInDeck<T>(Player player) where T : CardModel
    {
        // Implementation: find card by type, increment upgrade level
    }

    /// <summary>
    /// Helper: grant an ascended rune to the Blessed Toolbox relic.
    /// </summary>
    protected void GrantAscendedStartingRune(Player player, Rune ascendedRune)
    {
        var toolbox = player.Relics.FirstOrDefault(r => r is BlessedToolbox);
        if (toolbox is BlessedToolbox blessed)
        {
            // Implementation: modify the relic's starting rune
        }
    }
}
```

### Example: OrobasChoice

```csharp
// RunePriestCode/Ancients/OrobasChoice.cs

public class OrobasChoice : RunePriestAncientChoice
{
    public override string Id => "RUNEPRIEST-OROBAS";
    public override LocString Title => new("ancients", Id + ".title");
    public override LocString Description => new("ancients", Id + ".description");

    public override async Task Apply(Player player, PlayerChoiceContext ctx)
    {
        // Upgrade the Blessed Toolbox starting rune to Ascended Strike
        GrantAscendedStartingRune(player, new AscendedStrike(5));
        
        // Bonus: +1 upgrade to all Ascended cards
        foreach (var card in player.Hand.Concat(player.Deck.DrawPile).Concat(player.Deck.ExhaustPile))
        {
            if (card.CanBeUpgraded)
            {
                card.Upgrade(); // May need async version
            }
        }
        
        MainFile.Logger.Info("[Rune] Orobas ancient applied: starting rune ascended");
    }
}
```

### Localization

Add to `RunePriest/localization/eng/ancients.json`:

```json
{
  "RUNEPRIEST-OROBAS.title": "Orobas",
  "RUNEPRIEST-OROBAS.description": "[gold]Ascend[/gold] your starting rune, granting divine power. Upgrade all Ascended cards."
}
```

---

## Implementation Steps

### Step 1: Define RunePriestAncientChoice Base
- Create file: `RunePriestCode/Ancients/RunePriestAncientChoice.cs`
- Define abstract methods: `Id`, `Title`, `Description`, `Apply`, `CanOffer`
- Add helpers: `UpgradeCardInDeck`, `GrantAscendedStartingRune`

### Step 2: Create OrobasChoice
- File: `RunePriestCode/Ancients/OrobasChoice.cs`
- Implement `Apply`: upgrade Blessed Toolbox rune → Ascended Strike
- Implement `Apply`: grant +1 upgrade to Ascended cards

### Step 3: Integrate with Ancient System
- **Option A**: BaseLib support (if it exists)
  - Register `OrobasChoice` via attribute or pool
- **Option B**: Harmony patch
  - Hook ancient choice generation
  - Filter by character; inject `OrobasChoice` if RunePriest

### Step 4: Localization
- Add `RUNEPRIEST-OROBAS.*` keys to `ancients.json`
- Analyzer should validate automatically

---

## Testing Checklist

- [ ] OrobasChoice compiles
- [ ] Ancient can be offered in a RunePriest run
- [ ] Ancient does NOT appear in non-RunePriest runs
- [ ] Selecting ancient upgrades Blessed Toolbox rune
- [ ] Ascended rune displays with correct styling
- [ ] Ascended cards receive +1 upgrade
- [ ] Co-op: both players receive the bonus (if party play)
- [ ] Save/load: ascended rune persists

---

## Future Ancients

Examples for planning:
- **Seraph** — All runes gain +2 damage, visible glow in buffer
- **Plague** — Inscribed runes apply 1 Poison to enemies automatically
- **Stillness** — Loops cost 1 less slot (capacity benefit)
- **Tempest** — Swift and Kindle grant +1 (improved card draw/energy)

---

## Notes
- Ancients are **end-of-run**, not combat-wide — effects persist for the remainder of the run
- Character-locking is critical: ancients should only be offered to RunePriest
- Effects should integrate with existing rune mechanics (not require new rune types)
- Co-op: use `Player` owner hooks for per-player state (relics, deck)
