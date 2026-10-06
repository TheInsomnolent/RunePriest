# Rune Priest events

Events are BaseLib `CustomEventModel`s placed in acts via `Acts` (BaseLib's `AddActContent` adds them to those acts'
event pools). All Rune Priest events extend `RunePriestCode/Events/RuneEvent.cs`.

## Who gets an event (and its options)

Events in StS2 are per-player (`IsShared` false): in co-op every player runs their own copy and sees their own options.

- **`Qualifies(Player)`** — each event says whether it has something for a player (e.g. Pulsing Pedestal: a rune card
  that can still be upgraded). `RuneEvent.UsesRunes` is the general "can use runes" test: a Rune Priest, *or* anyone
  holding a card that Inscribes (`RuneCard`), so a Regent who picked up rune cards counts.
- **Pool**: `IsAllowed` = every player qualifies. The game re-checks `IsAllowed` each time it pulls the next event
  (`RoomSet.EnsureNextEventIsValid`) and skips ones that aren't allowed, so an event never appears for a party where
  someone would have no valid option. This is stricter than "all players are Rune Priests" only where it matters.
- **Options**: rune-specific options use `Choice(...)` when `Qualifies(Owner)`, else `Locked("OPTION")`
  (`OPTION_LOCKED`, greyed out with a reason). Other conditions (Gold) lock the same way.
- **Forced choice**: just omit the Leave option (Enchanted Forge); `IsAllowed` must then guarantee at least one option
  is available to everyone.

## Events

| Event | Act | Qualifies | Options |
|---|---|---|---|
| Pulsing Pedestal (`PulsingPedestal`) | 1 (Overgrowth, Underdocks) | Has an upgradable, removable rune card | **Insert a Rune**: choose one; 80% Upgrade it, 20% remove it (event `Rng`). **Leave**. |
| Enchanted Forge (`EnchantedForge`) | 2 (Hive) | Has 2+ removable, non-X-cost Common rune cards | Forced. **Forge**: merge 2 into a `ForgedRune` + add Clumsy. **Dissolve**: lose 50 Gold, remove a card (locked under 50 Gold). |
| Suspicious Tailor (`SuspiciousTailor`) | 3 (Glory) | Uses runes and doesn't own the robe | **Trade**: obtain Lightweight Cloth Robe. **Decline**: add a Blood Rune card to your deck. |

**Forged Rune** (`Cards/Token/ForgedRune.cs`, Event rarity, token pool): Inscribes both cards' runes in order (Imbued
runes first, upgrades baked in), costs the higher of the two, is an Attack if either was, and can't be upgraded. Saved
as `ForgedRunes` (`GlyphCodec`), `ForgedCost`, `ForgedAttack`. Only the runes carry over (a card's draw/Exhaust etc. is lost).

## Localization (`events.json`)

`<ID>.title`, `<ID>.pages.INITIAL.description`, `<ID>.pages.INITIAL.options.<OPTION>.title/.description` (and
`<OPTION>_LOCKED`), and one `<ID>.pages.<RESULT>.description` per `Finish("RESULT")`. Event vars (`CanonicalVars`)
are available in option text (`{Gold}`, `{Cards}`, `{UpgradeChance}`). The analyzer doesn't check event keys.
Card-selection prompts live in `card_selection.json` (`RUNEPRIEST-TO_INSERT`, `RUNEPRIEST-TO_FORGE`).

## Art

Placeholder only: `RuneEvent.CustomInitialPortraitPath` uses `RunePriest/images/events/<id>.png` if it exists, else a
vanilla portrait (`PlaceholderPortrait`). Drop the real art in that folder and it is picked up automatically.
