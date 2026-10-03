# Build variants

The mod builds as one of three variants, chosen with the `ModVariant` MSBuild property. Each is a **separate mod**, so
any combination can be installed and enabled at the same time without overwriting each other's files, content or saves.

| `ModVariant` | Built by | Mod ID / folder | Shown as | Model ID / loc prefix | Assets |
|---|---|---|---|---|---|
| `Local` (default) | `dotnet build` on a dev machine | `RunePriestLocal` | RunePriest (Local) | `RUNEPRIESTLOCAL-` | `res://RunePriestLocal/` |
| `Nightly` | CI, pushes to `main` and PRs | `RunePriestNightly` | RunePriest (Nightly) | `RUNEPRIESTNIGHTLY-` | `res://RunePriestNightly/` |
| `Release` | CI, `v*` tags | `RunePriest` | RunePriest | `RUNEPRIEST-` | `res://RunePriest/` |

Non-release variants also put a **LOCAL** / **NIGHTLY** banner on the Rune Priest's character select tile.

## Why everything is renamed

The game and BaseLib key almost everything off the mod ID or root namespace, so two builds sharing them collide:

- **Mod ID**: the game loads `<id>.dll` / `<id>.pck` and treats equal IDs from the mods folder and the Workshop as
  duplicates (one gets disabled).
- **Assembly name**: all mods load into one `AssemblyLoadContext`, which can't hold two assemblies with the same name.
- **Root namespace**: BaseLib prefixes model IDs, custom enums and keyword keys with it (`RUNEPRIEST-STRIKE_RUNE_CARD`).
  Saves, run history and multiplayer sync store these IDs.
- **`res://` root**: every PCK is mounted into one virtual filesystem, so equal paths overwrite each other. Mod
  localization is read from `res://<id>/localization/`.
- **Godot script paths**: `[ScriptPath]` keys must be unique across assemblies (Godot throws on duplicates).
- **Harmony ID / logger name**: both come from `MainFile.ModId`.

## How the build does it

Source files keep `namespace RunePriest.RunePriestCode…`, and localization keeps `RUNEPRIEST-…` keys. For a variant build
[RunePriest.csproj](../RunePriest.csproj):

1. Sets `AssemblyName`/`RootNamespace` to the mod ID and defines `MOD_VARIANT_LOCAL|NIGHTLY|RELEASE`, which
   [ModVariant.cs](../RunePriestCode/ModVariant.cs) turns into `MainFile.ModId`, `MainFile.ModPrefix`, etc.
2. **Local/Nightly only:** `StageModVariantSources` compiles rewritten copies of `RunePriestCode/**` from
   `.godot/mono/temp/obj/<cfg>/variant/<ModId>/` (`RunePriest.RunePriestCode` → `<ModId>.RunePriestCode`). Each copy
   starts with a `#line` directive, so errors, warnings and the debugger still point at the original files. Edit the
   originals; the copies are regenerated on every build.
3. `StageModVariantAssets` copies `RunePriest/**` (without Godot `.import` files) to the same staging folder, renaming
   `RUNEPRIEST-`/`RUNEPRIEST_` keys and `res://RunePriest/` paths in `.json` files. The analyzer checks the staged
   localization, so STS001 still catches missing keys.
4. `PackModPck` packs the staged assets into `<ModId>.pck` under `res://<ModId>/` with BSchneppe.StS2.PckPacker (no
   Godot/MegaDot needed).
5. `AssembleModPackage` writes `<ModId>.json` from [RunePriest.json](../RunePriest.json) (with `id`/`name` replaced)
   and puts dll + pck + json in `.godot/mono/temp/bin/<cfg>/package/<ModId>/`. Local builds copy that folder (plus the
   pdb) to the game's `mods/<ModId>/`.

## Rules for code

- Never hard-code the mod ID, `res://RunePriest` or `RUNEPRIEST-`. Use `MainFile.ModId`, `MainFile.ResPath`,
  `MainFile.ModPrefix` (`RUNEPRIEST-`) or `ModVariant.IdUpper` (`RUNEPRIEST`, e.g. for `OPTION_RUNEPRIEST_ETCH`).
- Refer to the root namespace only as `RunePriest.RunePriestCode` (the rewrite only matches that exact form).
- Shared Godot meta keys / node names that another variant might also touch should include `MainFile.ModId`.
- Patches should act only on this variant's own types (`is Character.RunePriest`, `is RuneCard`), since other loaded
  variants patch the same game methods.

## Building a specific variant

```sh
dotnet build RunePriest.csproj                                   # Local -> mods/RunePriestLocal/
dotnet build RunePriest.csproj -p:ModVariant=Release             # installs mods/RunePriest/ (beware: shadows a Workshop release)
dotnet build RunePriest.csproj -c Release -p:UseSts2RefAssemblies=true -p:ModVariant=Nightly   # what CI does (no install)
```

Switching variants makes existing saves/runs from another variant unreadable to it: their model IDs differ.
