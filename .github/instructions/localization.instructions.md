---
applyTo: "RunePriest/localization/**/*.json"
description: "Card/power/relic text conventions for RunePriest localization files"
---
# RunePriest localization style

## Terminology
- Say **rune**, never "glyph", in player-facing text (the `Glyph` class is an internal detail).
- The rune buffer is the **Incantation**. Adding runes is **Inscribe** (`[gold]Inscribe[/gold]`); resolving it is **Speak** (`[gold]Speak[/gold]`, past tense `[gold]Spoken[/gold]`).
- Rune names are blue: `[blue]Strike[/blue]`, `[blue]Block[/blue]`, `[blue]Loop[/blue]`. Game keywords/powers are gold: `[gold]Block[/gold]`, `[gold]Weak[/gold]`.
  - `[blue]Block[/blue]` = the Block *rune*; `[gold]Block[/gold]` = actually gaining block now.
- Current rune names: Strike, Block, Mend, Blood, Kindle, Soul, Weakening, Expose, Venom · Add, Multiply, Echo, Sanctify · Anchor, Chaos, Nova, Execution, Mirror · Loop, End Loop, Seal.
- **Concentration** is a custom keyword (`card_keywords.json`). Cards get it via `CanonicalKeywords` (auto-prefixed, don't write it in the card text); Concentration powers start their description with `[gold]Concentration[/gold].\n`.

## Inscribe lines
- **Sequential runes** (separate `Glyph.Of(...)` entries): one `Inscribe` line each, separated by `\n`.
  ```
  [gold]Inscribe[/gold] [blue]Execution[/blue].\n[gold]Inscribe[/gold] [blue]Multiply[/blue] ×{Twin}.\n[gold]Inscribe[/gold] [blue]Strike[/blue] {Strike:diff()}.
  ```
- **Simultaneous runes** (one `Glyph.Of(a, b, ...)`): a single line, runes separated by commas.
  ```
  [gold]Inscribe[/gold] [blue]Block[/blue] {Ward:diff()}, [blue]Blood[/blue] {Blood:inverseDiff()}.
  ```
- Identical consecutive runes may be collapsed: `[gold]Inscribe[/gold] [blue]Strike[/blue] {Strike:diff()} twice.` / `{Hits} times.`
- Don't use "then" or "+" to join runes.
- Non-Inscribe effects go on their own line before or after the Inscribe lines, matching the order they happen in `OnPlay`.

## Values and formatting
- Use SmartFormat vars, not `[[Var]]`: `{Strike:diff()}` for upgradeable values; `{Blood:inverseDiff()}` for drawbacks that go down on upgrade; plain `{Loop}` for fixed values.
- Value prefixes follow the in-game rune label: `[blue]Add[/blue] +{Amplify}`, `[blue]Multiply[/blue] ×{Twin}`.
- End each line with a period.

## Keys
- Keys are `RUNEPRIEST-<UPPER_SNAKE_CLASS_NAME>.<field>` (e.g. `RUNEPRIEST-STRIKE_RUNE_CARD.title`). Renaming a class renames its key.
- Rune hover tips live in `static_hover_tips.json` as `RUNEPRIEST-RUNE_<KEY>.title/.description`, where `<KEY>` is the rune's `Key` (target runes: the `TargetMode` name upper-cased). The analyzer doesn't check these — add them by hand.
- Missing card/power/relic/potion keys fail the build (STS001); fix by adding keys, not suppressing.
- Localization changes need `dotnet publish RunePriest.csproj` to reach the game.
