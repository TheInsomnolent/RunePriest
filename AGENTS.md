# AGENTS.md — RunePriest (Slay the Spire 2 character mod)

A StS2 character mod built on Alchyr's BaseLib template. The Rune Priest is inspired by **Noita's wand-building**:
cards **inscribe runes** into a buffer floating over the character's head, and at end of turn the buffer is
**evaluated like a tiny block-coding language** (damage, block, loops, multipliers, targeting…). Invalid programs
must **fizzle gracefully**, never crash.

## Must-read docs
- [docs/rune-system-design.md](docs/rune-system-design.md) — rune language spec, base rune set, architecture, roadmap. **Source of truth for the mechanic.**
- [docs/sts2-modding-reference.md](docs/sts2-modding-reference.md) — verified game/BaseLib API cheat sheet, file paths, character minimums.

## Build
- `dotnet build RunePriest.csproj` — compiles DLL and copies to the StS2 `mods/` folder. .NET SDK 9+ (10 works), targets `net9.0`.
- **Publish** is required for any non-code change (localization, images, scenes) — it exports the `.pck` via MegaDot (`GodotPath` in `Directory.Build.props`, gitignored/per-machine). Works from CLI: `dotnet publish RunePriest.csproj` (or Rider "Publish → Local folder").
- Runtime logs: rune evaluation logs every step with a `[Rune]` prefix via `MainFile.Logger`.
- **CI** (`.github/workflows/build.yml`): without a local game, the csproj sets `UseSts2RefAssemblies=true` and compiles against `BSchneppe.Sts2.ReferenceAssemblies` (pinned to the game version in `release_info.json`), then packs the `.pck` with `BSchneppe.StS2.PckPacker` (no Godot). Simulate locally: `dotnet build RunePriest.csproj -c Release -p:UseSts2RefAssemblies=true` (needs .NET 9 runtime or `DOTNET_ROLL_FORWARD=Major`). Stubs may omit non-public members, so avoid relying on publicized private game API.
- **Steam Workshop** (app `2868840`): the `workshop` job in `build.yml` uploads via SteamCMD — `main` → friends-only nightly item (`vars.WORKSHOP_NIGHTLY_ID`), `v*` tags → release item (`vars.WORKSHOP_RELEASE_ID`). Credentials (`STEAM_USERNAME`, base64 `STEAM_CONFIG_VDF`) live in the `steam-workshop` environment. Setup/re-auth/release steps: [docs/steam-workshop.md](docs/steam-workshop.md).
- The Roslyn analyzer `Alchyr.Sts2.ModAnalyzers` fails the build (STS001) when a model is missing localization keys. Fix by adding keys to `RunePriest/localization/eng/*.json`, not by suppressing.

## Layout
- `RunePriestCode/` — all C# (namespace root `RunePriest.RunePriestCode`). `MainFile.cs` is the mod initializer (Harmony `PatchAll`).
- `RunePriest/` — Godot assets: `images/`, `localization/eng/*.json`. Excluded from compilation.
- `.decompiled/` — **gitignored** decompiled `sts2.dll` and `BaseLib.dll` for API lookup. Search it (use `includeIgnoredFiles`) before guessing any game API. Regenerate:
  ```powershell
  dotnet tool install -g ilspycmd
  $data = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64"
  ilspycmd -p -o .decompiled\sts2 "$data\sts2.dll"
  ilspycmd -p -o .decompiled\BaseLib (Get-ChildItem "$env:USERPROFILE\.nuget\packages\alchyr.sts2.baselib" -Recurse -Filter BaseLib.dll | Select-Object -First 1).FullName
  New-Item .decompiled\.gdignore -Force
  ```
  Never copy decompiled code verbatim into the repo; reference it.

## Conventions
- Model IDs are `<MODID>-<SLUGIFIED_CLASS_NAME>` in upper snake case, e.g. class `RunePriest` → `RUNEPRIEST-RUNE_PRIEST`. Localization keys use that ID: `RUNEPRIEST-STRIKE_RUNE_CARD.title`.
- Player-facing text says **rune**, never "glyph" (`Glyph` is code-only). Card text rules: [.github/instructions/localization.instructions.md](.github/instructions/localization.instructions.md).
- Card descriptions use SmartFormat vars: `"Deal {Damage:diff()} damage."` (not `[[Damage]]`).
- Cards extend `RunePriestCard` (auto-registered to `RunePriestCardPool` via `[Pool]`). Rune-casting cards extend `RuneCard` and implement `Glyphs(Creature? anchor)`. Folders by rarity: `Cards/Basic|Common|Uncommon|Rare`; generated-only cards go in `Cards/Special` with `CardRarity.Token` and `[Pool(typeof(TokenCardPool))]`.
- New runes need hand-written `RUNEPRIEST-RUNE_<KEY>.title/.description` in `static_hover_tips.json` (the analyzer doesn't check these).
- Art: **use placeholder assets only**; an artist will supply art later. Missing images fall back to `card.png`/`power.png`/`relic.png` via `Extensions/StringExtensions.cs`.
- Randomness in combat must use `Owner.RunState.Rng.CombatTargets` (or another `RunRngSet` stream) — never `System.Random` (breaks co-op determinism / replays).
- Co-op: runes only affect the player who inscribed them (ally payloads always hit the caster; target modes only pick enemies). Giving runes to other players goes through `RuneCmd.Share` (Choral Evocation), which raises no inscription listeners.
- All effects go through game commands (`DamageCmd`, `CreatureCmd`, `PowerCmd`, `PlayerCmd`, `CardPileCmd`) and are `await`ed with the `PlayerChoiceContext`.
- Keep the rune interpreter free of Godot/UI dependencies; UI observes buffer change events.
- Custom Godot nodes (`RunePriestCode/Nodes/`) must be `partial` and are created in code (no scenes); `MainFile` calls `ScriptManagerBridge.LookupScriptsInAssembly` so their callbacks run.

## External references
- Template wiki: https://github.com/Alchyr/ModTemplate-StS2/wiki (Setup, Adding Cards, Modding Basics, Decompiling)
- BaseLib docs: https://alchyr.github.io/BaseLib-Wiki/ · source: https://github.com/Alchyr/BaseLib-StS2
- Analyzer required-loc table: https://github.com/Alchyr/StS2ModAnalyzers/blob/master/ModAnalyzers/ModAnalyzers/LocalizationAnalyzer.cs
- Godot BBCode (loc text): https://docs.godotengine.org/en/4.5/tutorials/ui/bbcode_in_richtextlabel.html
