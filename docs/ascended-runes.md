# Ascended Runes — Design & Implementation Guide

## Overview
> **Current state (10/3/2026):** Ascended *cards* are `CardRarity.Ancient` cards with `MaxUpgradeLevel => 0`, obtained
> through Orobas' Archaic Tooth (see [rune-system-design.md §15](rune-system-design.md#15-ancients-and-ascended-cards)).
> The only Ascended *rune* so far is Overgrowth (`OvergrowthRune`, a `GrowingRune` that triples). The examples below
> (`AscendedStrike`, `AncientCard`, `BlessedToolboxAscended`) are design sketches, not existing code.

**Ascended Runes** are enhanced versions of standard runes inscribed by Ascended cards (Ancient-rarity cards that grant special powers). They are mechanically identical to their base counterparts but visually and textually distinct, signaling their elevated power.

## Key Concepts

### Visual Hierarchy
- **Ascended runes** should appear noticeably different from standard runes
- Examples: glowing auras, particle effects, shader tints, distinct icons
- The rune UI should display them with enhanced styling (glow, animation, color shifts)

### Mechanical vs. Visual
- Ascended runes **preserve base behavior** (damage, block, etc.)
- They do NOT inherently do more damage/block — that's the card's responsibility
- If a card grants "Ascended Strike 10", it's up to the card to make that 10 higher than standard Strikes
- Future iteration may add mechanics (e.g., Ascended status effect, ascended-specific keywords), but initially it's **visual/textual only**

### Localization Pattern
Each payload rune has two sets of locale strings:

```json
"RUNEPRIEST-RUNE_STRIKE.title": "Strike",
"RUNEPRIEST-RUNE_STRIKE.description": "Attack the target for {Value} damage.",

"RUNEPRIEST-RUNE_STRIKE_ASCENDED.title": "Ascended Strike",
"RUNEPRIEST-RUNE_STRIKE_ASCENDED.description": "Attack the target for {Value} damage with [gold]divine radiance[/gold]."
```

If an Ascended variant isn't localized, it falls back to the base rune text (see `Rune.LocKey` property).

---

## Creating an Ascended Variant

### Example: AscendedStrike

```csharp
using RunePriest.RunePriestCode.Runes;

namespace RunePriest.RunePriestCode.Runes.Ascended;

public class AscendedStrike : AscendedRune
{
    private readonly PayloadRune _baseStrike;

    public AscendedStrike(int value) : base(value)
    {
        _baseStrike = new Strike(value);
    }

    public override Rune BaseRune => _baseStrike;

    protected override Rune ToAscended(Rune merged)
        => new AscendedStrike(merged.Value);
}
```

### File Organization
Create ascended variants in `RunePriestCode/Runes/Ascended/`:
- `AscendedStrike.cs`
- `AscendedDefend.cs`
- `AscendedMend.cs`
- etc.

---

## Inscribing Ascended Runes

### From a Card
```csharp
public class OrobasInvasion : AncientCard
{
    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
    [
        Glyph.Of(new AscendedStrike(Var("Damage"))).AnchoredTo(anchor)
    ];
}
```

### From a Relic (e.g., Blessed Toolbox upgrade)
```csharp
public class BlessedToolboxAscended : RunePriestRelic
{
    public override void OnCombatStart(PlayerChoiceContext ctx, Player owner)
    {
        // Replace the starting rune with an Ascended version
        var buffer = owner.RuneBuffer;
        var rune = buffer.Buffer.FirstOrDefault();
        if (rune is Strike strike && !strike.IsAscended)
        {
            buffer.Buffer.RemoveAt(0);
            buffer.Buffer.Insert(0, new AscendedStrike(strike.Value));
        }
    }
}
```

---

## UI Considerations

### Rune Buffer Display
- The rune UI node should check `rune.IsAscended` and apply a glow/tint shader
- Particle effects: consider adding small sparkles or radiance around ascended runes
- Animation: ascended runes could have a subtle pulsing or floating animation

### Hover Tips
- The hover tip already pulls from the ascended localization, so no changes needed there
- Colors: consider using a gold/white glow for contrast

### Forecast (Prediction UI)
- Ascended runes in the forecast should appear distinctly
- Same visuals as in the buffer (consistent visual language)

---

## Testing Checklist

- [ ] Ascended rune created and compiles
- [ ] `IsAscended` property returns `true`
- [ ] Localization key lookup works (title/description pull from `_ASCENDED` variant)
- [ ] Merging: two Ascended Strikes (5 + 3) = Ascended Strike (8) ✓
- [ ] Merging: Ascended Strike (5) + Strike (3) = Ascended Strike (8) ✓
- [ ] Merging: Strike (5) + Ascended Strike (3) = Ascended Strike (8) ✓
- [ ] UI renders with ascended styling (when UI is implemented)
- [ ] No gameplay breakage (ascended runes resolve as their base type)

---

## Future Extensions

- **Ascended Keyword**: A card keyword that adds visual polish and potentially bonus effects
- **Ascended Status**: An enemy power that persists after Ascended card effects
- **Theme**: Ascended cards invite darker, more divine visuals (contrast with standard runes)
- **Rarity**: Ascended cards are likely Rare/Ancient and should feel epic

---

## Notes

- Ascended runes are **not** new rune types; they're decorated versions of existing runes
- The `AscendedRune` abstract base handles the boilerplate; each variant is a trivial subclass
- No modifications to the rune interpreter (`RuneInterpreter`) are needed; ascended runes execute identically to their base
- Co-op: Ascended status travels with the rune value, so it's deterministic and saveable
