# Ancient boons — how the Rune Priest hooks into the game's ancients

Ancient options in StS2 are **relics** offered by an `AncientEventModel` (Neow, Orobas, Darv, Vakuu, Tezcatara, …).
Choosing one obtains the relic; its `AfterObtained` does the work. The Rune Priest never defines a new ancient — it
adds boons to the vanilla ones. The full list and card details live in
[rune-system-design.md §15](rune-system-design.md#15-ancients-and-ascended-cards).

## BaseLib hooks (preferred)

| Vanilla relic | What it does by default | Rune Priest hook |
|---|---|---|
| **Touch of Orobas** | Replaces the starter relic with its upgraded form (`RefinementUpgrades`) | `BlessedToolbox.GetUpgradeReplacement()` → `HallowedToolbox` (BaseLib `CustomRelicModel`) |
| **Archaic Tooth** | Transforms the first deck card that has a "transcendence" form | Each Common "… Rune" card implements BaseLib `ITranscendenceCard` → its Ascended card |
| **Dusty Tome** | Adds a random non-transcendence Ancient card of your character | `Mirrororrim` implements BaseLib `ITomeCard` |

Orobas only offers Touch of Orobas / Archaic Tooth when `SetupForPlayer` finds a starter relic / transcendable card, so
a Rune Priest who removed the starting rune card simply isn't offered the Tooth.

## Harmony patches (everything else)

`Patches/AncientOptionPatches.cs` postfixes `GenerateInitialOptions` on Darv, Vakuu and Tezcatara: for a Rune Priest who
doesn't own the boon yet, it replaces one generated option with probability roughly equal to one more relic in that
slot's pool, rolling on the event's own `Rng` so every co-op client and reload sees the same offer. The
`AllPossibleOptions` getters are patched too so the relics are grouped under their ancient in the relic collection and
can be forced with the dev console's `ancient` command.

Option text falls back to the relic's `title` and `eventDescription` (or `description`) in `relics.json`, so no
`ancients.json` keys are needed.

## Adding a new boon

1. Create the relic in `RunePriestCode/Relics/` (`RunePriestRelic`, `RelicRarity.Ancient`, `HasUponPickupEffect` if
   `AfterObtained` changes the deck), with `title`/`description`/`flavor` in `relics.json`.
2. Prefer a BaseLib hook if one fits; otherwise add a postfix pair to `AncientOptionPatches` (options + all options).
3. Character-lock via `Owner.Character is Character.RunePriest`; use `__instance.Rng` for any randomness.
4. Document it in §15 of the design doc.
