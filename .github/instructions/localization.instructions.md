---
applyTo: "RunePriest/localization/**/*.json"
description: "Card/power/relic text conventions for RunePriest localization files"
---
# RunePriest localization style

## Terminology
- Say **rune**, never "glyph", in player-facing text (the `Glyph` class is an internal detail).
- The rune buffer is the **Incantation**. Adding runes is **Inscribe** (`[gold]Inscribe[/gold]`); resolving it is **Speak** (`[gold]Speak[/gold]`, past tense `[gold]Spoken[/gold]`); binding the most recent runes to a card's Imbue slot is **Imbue** (`[gold]Imbue[/gold]`; use `ImbueHoverTips` on the card so the bound runes are listed once it is Imbued).
- Rune names are blue: `[blue]Strike[/blue]`, `[blue]Defend[/blue]`, `[blue]Loop[/blue]`. Game keywords/powers are gold: `[gold]Block[/gold]`, `[gold]Weak[/gold]`.
  - The block rune is **Defend** (`[blue]Defend[/blue]`); `[gold]Block[/gold]` always means actually gaining block now.
- Current rune names: Strike, Defend, Mend, Hex, Cleanse, Kindle, Swift, Blood, Diminish · Amplify, Twin, Echo, Sanctify, Void, Growth, Reflection, Friendship, Clone · Anchor, Scatter, Nova, Execution, Mirror · Loop, End Loop, Seal. (Sanctify, Seal, Mirror are engine-only: no card inscribes them.)
- A rune that is Spoken but does nothing **Fizzles** — a gold keyword with a hover tip (`RuneTips.Fizzle`): "Whenever a rune [gold]Fizzles[/gold], ...". Add `RuneTips.Fizzle` to the model's hover tips when its text mentions it.
- **Concentration** is a custom keyword (`card_keywords.json`, currently unused). Cards get it via `CanonicalKeywords` (auto-prefixed, don't write it in the card text); Concentration powers start their description with `[gold]Concentration[/gold].\n`.

## Inscribe lines
- **Sequential runes** (separate `Glyph.Of(...)` entries): one `Inscribe` line each, separated by `\n`.
  ```
  [gold]Inscribe[/gold] [blue]Execution[/blue].\n[gold]Inscribe[/gold] [blue]Twin[/blue] ×{Twin}.\n[gold]Inscribe[/gold] [blue]Strike[/blue] {Strike:diff()}.
  ```
- **Simultaneous runes** (one `Glyph.Of(a, b, ...)`): a single line, runes separated by commas.
  ```
  [gold]Inscribe[/gold] [blue]Defend[/blue] {Defend:diff()}, [blue]Blood[/blue] {Blood:inverseDiff()}.
  ```
- Identical consecutive runes may be collapsed: `[gold]Inscribe[/gold] [blue]Strike[/blue] {Strike:diff()} twice.` / `{Hits} times.`
- Don't use "then" or "+" to join runes.
- Non-Inscribe effects go on their own line before or after the Inscribe lines, matching the order they happen in `OnPlay`. Imbue lines: `{ImbuedCount:choose(0):[gold]Imbue[/gold] {Imbue}.|{ImbuedRunes}}` — once the card is Imbued, the line is replaced by its bound runes (`ImbuedCount` / `ImbuedRunes` come from `RunePriestCard.AddExtraArgsToDescription`).
- Text that only applies when upgraded uses the game's formatter: `{IfUpgraded:show:upgraded text|normal text}` (either side may be empty, nested vars like `{Cards}` work inside). Used for upgrades that add an effect (Loop Rune "Draw 1", Quick Scribe's Loop).

## Values and formatting
- Use SmartFormat vars, not `[[Var]]`: `{Strike:diff()}` for upgradeable values; `{Blood:inverseDiff()}` for drawbacks that go down on upgrade; plain `{Loop}` for fixed values.
- Value prefixes follow the in-game rune label: `[blue]Amplify[/blue] +{Amplify}`, `[blue]Twin[/blue] ×{Twin}`.
- End each line with a period.

## Keys
- Keys are `RUNEPRIEST-<UPPER_SNAKE_CLASS_NAME>.<field>` (e.g. `RUNEPRIEST-STRIKE_RUNE_CARD.title`). Renaming a class renames its key. Cards titled "X Rune" use class `XRuneCard` (the title stays "X Rune") so they don't clash with the rune classes.
- Rune hover tips live in `static_hover_tips.json` as `RUNEPRIEST-RUNE_<KEY>.title/.description`, where `<KEY>` is the rune's `Key` (target runes: the `TargetMode` name upper-cased). The analyzer doesn't check these — add them by hand.
- Missing card/power/relic/potion keys fail the build (STS001); fix by adding keys, not suppressing.
- Localization changes need `dotnet publish RunePriest.csproj` to reach the game.
