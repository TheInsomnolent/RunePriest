# Custom Events — Design & Implementation Guide

## Overview
**Custom Events** are character-specific map events that can offer unique rewards or challenges. For the Rune Priest, events provide opportunities for rune-themed storytelling: encounters with lost rune practitioners, forbidden inscriptions, or ascended trials.

## Key Principles
- Events should feel **thematic** (runes, incantations, ancient magic)
- Some events are **RunePriest-only** (locked to character)
- Results can grant cards, relics, or rune-based effects (e.g., starting buffer pre-filled)
- Character-locking prevents generic events from feeling out-of-place

## BaseLib Integration

**STATUS**: Investigation required

The game's event system likely:
1. Stores events in a pool (like cards/relics)
2. Generates event choices during map transitions
3. Executes event results when selected

We need to determine:
- [ ] Does BaseLib provide `CustomEventModel`?
- [ ] Is there an event pool or registration system?
- [ ] Which hook filters/injects events?
- [ ] How do event results execute (async? hooks?)?

If BaseLib doesn't support custom events, we'll use Harmony to patch event generation (see `NeowAetherQuillPatch` pattern).

---

## Architecture Pattern

### Event Filtering Mixin
```csharp
// RunePriestCode/Events/RunePriestEventMixin.cs

using MegaCrit.Sts2.Core.Entities.Characters;

namespace RunePriest.RunePriestCode.Events;

public static class RunePriestEventMixin
{
    /// <summary>Check if an event is exclusive to the Rune Priest.</summary>
    public static bool IsRunePriestOnly(string eventId)
    {
        // List of RunePriest-exclusive events
        return eventId.StartsWith("RUNEPRIEST_");
    }

    /// <summary>Check if the player can experience this event.</summary>
    public static bool PlayerCanExperience(Event @event, Player player)
    {
        if (IsRunePriestOnly(@event.Id))
            return player.Character is Character.RunePriest;
        return true; // Generic events available to all
    }

    /// <summary>Filter an event pool by character.</summary>
    public static List<Event> FilterByCharacter(List<Event> events, Player player)
    {
        return events
            .Where(e => PlayerCanExperience(e, player))
            .ToList();
    }
}
```

### Base Event Class
```csharp
// RunePriestCode/Events/RunePriestEventBase.cs (template)

public abstract class RunePriestEventBase : Event
{
    public override string Id => "RUNEPRIEST_" + Key;
    protected abstract string Key { get; } // e.g., "FORBIDDEN_CODEX"

    public override LocString Title => new("events", Id + ".title");
    public override LocString Description => new("events", Id + ".description");

    /// <summary>Guaranteed to be RunePriest; safe to cast player to RunePriest type.</summary>
    public virtual Task OnEvent(Player player, PlayerChoiceContext ctx)
    {
        MainFile.Logger.Info($"[Rune] Event triggered: {Id}");
        return Task.CompletedTask;
    }
}
```

### Example: ForbiddenCodexEvent
```csharp
// RunePriestCode/Events/ForbiddenCodexEvent.cs

public class ForbiddenCodexEvent : RunePriestEventBase
{
    protected override string Key => "FORBIDDEN_CODEX";

    public override IReadOnlyList<EventOption> Options => 
    [
        new EventOption(
            new LocString("events", Id + ".option1"),
            OnLearnRunes
        ),
        new EventOption(
            new LocString("events", Id + ".option2"),
            OnIgnore
        )
    ];

    private async Task OnLearnRunes(Player player, PlayerChoiceContext ctx)
    {
        // Grant a rare rune card or power
        var card = ctx.CombatState.CreateCard<AscendedStrike>(player);
        await CardPileCmd.AddGeneratedCardToCombat(
            card, PileType.Hand, player, CardPilePosition.Random
        ).Execute(ctx);
        
        MainFile.Logger.Info("[Rune] Learned forbidden runes");
    }

    private async Task OnIgnore(Player player, PlayerChoiceContext ctx)
    {
        // Safe choice: nothing happens
        await Task.CompletedTask;
    }
}
```

### Localization
Add to `RunePriest/localization/eng/events.json`:

```json
{
  "RUNEPRIEST_FORBIDDEN_CODEX.title": "The Forbidden Codex",
  "RUNEPRIEST_FORBIDDEN_CODEX.description": "A tome bound in shadow. Its runes whisper ancient incantations...",
  "RUNEPRIEST_FORBIDDEN_CODEX.option1": "Learn the runes.",
  "RUNEPRIEST_FORBIDDEN_CODEX.option2": "Leave it be."
}
```

---

## Implementation Steps

### Step 1: Create Event Filtering Infrastructure
- File: `RunePriestCode/Events/RunePriestEventMixin.cs`
- Implement: `IsRunePriestOnly`, `PlayerCanExperience`, `FilterByCharacter`
- Test: verify mixin filters correctly

### Step 2: Investigate BaseLib Event Support
- Check if `CustomEventModel` exists
- If yes: follow registration pattern
- If no: create Harmony patch to inject/filter events

### Step 3: Create Base Event Template
- File: `RunePriestCode/Events/RunePriestEventBase.cs`
- Abstract methods: `Key`, `Options`, event resolution
- Localization lookup pattern

### Step 4: Create Example Event (ForbiddenCodexEvent)
- File: `RunePriestCode/Events/ForbiddenCodexEvent.cs`
- Two options: learn/ignore
- One option grants rune reward

### Step 5: Integrate with Event Generation
- **Option A**: BaseLib support
  - Register event via pool or attribute
- **Option B**: Harmony patch
  - Hook event pool generation
  - Inject RunePriest events; filter generic ones

### Step 6: Localization
- Create: `RunePriest/localization/eng/events.json`
- Add: `RUNEPRIEST_*` keys for all events

---

## Event Result Patterns

### Grant a Rune Card
```csharp
var card = ctx.CombatState.CreateCard<AscendedStrike>(player);
await CardPileCmd.AddGeneratedCardToCombat(
    card, PileType.Hand, player, CardPilePosition.Random
).Execute(ctx);
```

### Modify Starting Rune Buffer
```csharp
// Start combat with a pre-filled buffer
player.RuneBuffer.Buffer.Add(new AscendedStrike(5));
```

### Grant a Relic
```csharp
var relic = ModelDb.Relic<AetherInkwell>();
await RelicCmd.AddRelic(player, relic).Execute(ctx);
```

### Apply a Curse/Status
```csharp
var curse = ctx.CombatState.CreateCard<RunePriestCurse>(player);
await CardPileCmd.AddCurseToDeck(curse, player).Execute(ctx);
```

### Trigger a Power
```csharp
await PowerCmd.Apply<RunicFormPower>(
    ctx, player, 1, null, null, silent: false
).Execute(ctx);
```

---

## Testing Checklist

- [ ] Event filtering works (RunePriest vs. other chars)
- [ ] ForbiddenCodexEvent compiles and shows in map
- [ ] Event appears only in RunePriest runs
- [ ] Selecting option 1 grants rune card
- [ ] Selecting option 2 does nothing
- [ ] Localization keys are present and non-empty
- [ ] Co-op: both players see the same event
- [ ] No crashes or unhandled exceptions

---

## Future Events

Example ideas:
- **The Ascended Vault** — Choose 1 of 3 powerful Ascended cards
- **Echo of the Past** — Start combat with buffer: `[Loop][Strike 3][End]`
- **Cursed Inscription** — Gain 1 rare rune, lose max HP
- **The Cleansed Temple** — Restore all HP; all runes are Ascended this combat
- **Rune Practitioner** — Upgrade all Rune cards; grant a potion

---

## Notes
- Events are **non-combat** map encounters — they execute during travel
- Results should feel **rewarding** but not overpowered (balance with other characters)
- RunePriest-exclusive events can be more experimental/thematic
- Use `PlayerChoiceContext` for co-op determinism (RNG, card creation)
