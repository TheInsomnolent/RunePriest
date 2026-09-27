# The Rune Priest — a Slay the Spire 2 character mod

A spellwright inspired by **Noita**'s wand-building. Rune Priest cards **Inscribe** glyphs into an
**Incantation** that floats above your head. At the end of each turn the Incantation is **Spoken**: evaluated
left to right like a tiny program. Damage, block, loops, multipliers, targeting and self-inflicted costs all
combine, and the order you play your cards matters. If a spell doesn't make sense it simply fizzles.

> **Early development.** Card art is placeholder and the numbers still need balancing. Feedback is welcome!

---

## Download and install

### Requirements
- **Slay the Spire 2** (Steam), with mods enabled.
- **[BaseLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3737335127)**, the shared modding library.
  Subscribe to it on the Steam Workshop, or download it from [BaseLib releases](https://github.com/Alchyr/BaseLib-StS2/releases)
  and install it the same way as this mod.

### 1. Download
| Build | Link | Use it if… |
|---|---|---|
| **Latest release** | [**Releases → latest**](../../releases/latest) → `RunePriest.zip` | You want the most stable version. |
| **Nightly** | [**Releases → nightly**](../../releases/tag/nightly) → `RunePriest.zip` | You want the newest changes and don't mind bugs. |

### 2. Install
1. Open your Slay the Spire 2 install folder. In Steam: right-click the game → **Manage → Browse local files**.
2. Open the `mods` folder there, creating it if it doesn't exist:

   | OS | Mods folder |
   |---|---|
   | Windows | `…\steamapps\common\Slay the Spire 2\mods\` |
   | Linux / Steam Deck | `~/.local/share/Steam/steamapps/common/Slay the Spire 2/mods/` |
   | macOS | `…/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/` |

3. Extract `RunePriest.zip` into `mods`. You should end up with:
   ```
   mods/
   └── RunePriest/
       ├── RunePriest.dll
       ├── RunePriest.pck
       └── RunePriest.json
   ```
4. Launch the game and open **Settings → Mod Settings**. Make sure **BaseLib** and **RunePriest** are enabled,
   then restart if the game asks you to.
5. Start a run and pick **The Rune Priest**.

### Updating / uninstalling
- **Update:** delete `mods/RunePriest/` and extract the new zip in its place.
- **Uninstall:** delete `mods/RunePriest/`.

### Troubleshooting
- **The character doesn't appear:** check that BaseLib is installed and enabled, and that the three files sit directly
  inside `mods/RunePriest/`, not in a nested `RunePriest/RunePriest/` folder.
- **The game updated and the mod broke:** grab the latest nightly, or wait for a new release.
- **Something else:** the game log is at `%APPDATA%\SlayTheSpire2\logs\godot.log` (Windows) or
  `~/.local/share/SlayTheSpire2/logs/godot.log` (Linux). Lines starting with `[Rune]` trace every spell.
  Please include the log when reporting a bug.

---

## How to play (quick primer)
- **Inscribe:** cards add glyphs to your Incantation. If a glyph matches the one before it, they merge and their values add up.
- **Speak:** at end of turn the Incantation resolves left to right, then clears. **Invoke** and similar cards speak it mid-turn.
- **Effect runes:** Strike (a real attack), Block (block), Mend (heal), Blood (lose HP), Kindle (energy), Soul (draw), Weakening, Expose, Venom.
- **Modifiers** empower the *next* glyph, or a whole loop: Add +X, Multiply ×N, Echo, Sanctify.
- **Targeting runes** last until the next targeting rune: Anchor (default), Chaos, Nova, Execution, and the cursed Mirror.
- **Flow runes:** Loop N … End Loop, and Seal (stop here; the rest waits until next turn).
- Hover the runes over your head, or the Incantation buff, to see each rune's text and a **Forecast** of what will happen.

The full design is in [docs/rune-system-design.md](docs/rune-system-design.md).

---

## Building from source

### Prerequisites
- [.NET SDK 9+](https://dotnet.microsoft.com/download) (10 works).
- Slay the Spire 2 installed (the build finds it via Steam), for local builds that run in the game.
- [MegaDot](https://megadot.megacrit.com/), MegaCrit's Godot 4.5.1 fork, to export the `.pck`. Set `<GodotPath>` in a local,
  git-ignored `Directory.Build.props`:
  ```xml
  <Project><PropertyGroup><GodotPath>C:/path/to/MegaDot_v4.5.1-stable_mono_win64.exe</GodotPath></PropertyGroup></Project>
  ```

### Commands
```powershell
dotnet build RunePriest.csproj     # code only: builds the DLL and copies it to the game's mods folder
dotnet publish RunePriest.csproj   # code + text/images: also exports the .pck via MegaDot
```
Close the game before building; Windows locks the DLL while it's running.

### CI and releases
[.github/workflows/build.yml](.github/workflows/build.yml) builds on GitHub Actions without the game or Godot.
It compiles against reference-only stubs of the game assemblies
([BSchneppe.Sts2.ReferenceAssemblies](https://www.nuget.org/packages/BSchneppe.Sts2.ReferenceAssemblies)) and packs the
`.pck` with [BSchneppe.StS2.PckPacker](https://www.nuget.org/packages/BSchneppe.StS2.PckPacker).
Local builds are unaffected because they use your real game install.

| Trigger | Result |
|---|---|
| Pull request | Build check, plus a downloadable `RunePriest` artifact (kept 7 days). |
| Push to `main` | Replaces the **nightly** pre-release. |
| Push a tag `vX.Y.Z` | Publishes a **release** named after the tag, with the version stamped into `RunePriest.json`. |

To cut a release: `git tag v0.1.0 && git push origin v0.1.0`.

To keep CI cheap it runs on a single Linux job, caches NuGet packages, skips doc-only changes, cancels superseded
runs, and doesn't download Godot at all.

**When the game updates:** bump the `BSchneppe.Sts2.ReferenceAssemblies` version in `RunePriest.csproj` to match
`release_info.json` in the game folder.

To simulate CI locally: `dotnet build RunePriest.csproj -c Release -p:UseSts2RefAssemblies=true`.

---

## Credits
- Built on [BaseLib](https://github.com/Alchyr/BaseLib-StS2) and the [StS2 mod template](https://github.com/Alchyr/ModTemplate-StS2) by Alchyr.
- Inspired by Noita (Nolla Games). Slay the Spire 2 is by MegaCrit; this is an unofficial fan mod.
