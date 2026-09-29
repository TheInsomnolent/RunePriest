# Steam Workshop publishing

The `workshop` job in [.github/workflows/build.yml](../.github/workflows/build.yml) uploads the mod with SteamCMD:

| Trigger | Workshop item | Repo variable | Visibility |
|---|---|---|---|
| push to `main` | RunePriest (Nightly) — `3810157016` | `WORKSHOP_NIGHTLY_ID` | forced friends-only by CI |
| tag `v*` | RunePriest (release) — not created yet | `WORKSHOP_RELEASE_ID` | managed on the Workshop page |

Each upload is skipped until its repo variable is set. CI never creates items; they are created once by hand (below).

## 1. Local SteamCMD login

Run in your own terminal (you'll type your password and approve Steam Guard):

```powershell
New-Item -ItemType Directory C:\steamcmd -Force | Out-Null
Invoke-WebRequest https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip -OutFile C:\steamcmd\steamcmd.zip
Expand-Archive C:\steamcmd\steamcmd.zip C:\steamcmd -Force
C:\steamcmd\steamcmd.exe +login <STEAM_USERNAME> +quit
```

This caches a login token in `C:\steamcmd\config\config.vdf`; later commands log in without a password.

## 2. Create a Workshop item

1. Put the mod files in the content folder so the layout is `C:\steamcmd\content\RunePriest\RunePriest.{dll,pck,json}`:
   ```powershell
   New-Item -ItemType Directory C:\steamcmd\content -Force | Out-Null
   Invoke-WebRequest https://github.com/TheInsomnolent/RunePriest/releases/download/nightly/RunePriest.zip -OutFile $env:TEMP\RunePriest.zip
   Expand-Archive $env:TEMP\RunePriest.zip C:\steamcmd\content -Force
   ```
   For a release item, download that release's `RunePriest.zip` instead.
2. Create a VDF, e.g. `C:\steamcmd\release.vdf` (use forward slashes in paths):
   ```
   "workshopitem"
   {
     "appid" "2868840"
     "publishedfileid" "0"
     "contentfolder" "C:/steamcmd/content"
     "previewfile" "D:/Code/sts2CharacterMod/RunePriest/mod_image.png"
     "visibility" "2"
     "title" "RunePriest"
     "description" "..."
     "changenote" "Initial upload"
   }
   ```
   Visibility: `0` public, `1` friends-only, `2` hidden, `3` unlisted. Start the release item hidden and make it public on the Workshop page when ready.
3. Upload:
   ```powershell
   C:\steamcmd\steamcmd.exe +login <STEAM_USERNAME> +workshop_build_item C:\steamcmd\release.vdf +quit
   ```
   The new ID is printed (`Create new workshop item ( PublishFileID ... )`) and written back into the VDF.
   `ERROR! Build for workshop item has no content` means `contentfolder` is missing or empty; the item was still created, so fix the folder and rerun the same VDF.
4. On the item's Workshop page: accept the Workshop legal agreement if prompted, and under **Add/Remove Required Items** add **BaseLib** (`3737335127`).

## 3. GitHub configuration

Repo **Settings**:

- **Environments → `steam-workshop`**
  - Deployment branches and tags: **Selected** → branch `main`, tag `v*`.
  - Environment secrets:
    - `STEAM_USERNAME` — Steam login name.
    - `STEAM_CONFIG_VDF` — base64 of the cached login token:
      ```powershell
      [Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\steamcmd\config\config.vdf")) | Set-Clipboard
      ```
- **Secrets and variables → Actions → Variables** (repository variables, not environment — the job-level `if` reads them before entering the environment):
  - `WORKSHOP_NIGHTLY_ID` = `3810157016`
  - `WORKSHOP_RELEASE_ID` = the release item ID, once created.

## Public release checklist

1. Create the release item (section 2) and add BaseLib as a required item.
2. Set the `WORKSHOP_RELEASE_ID` repository variable.
3. Tag and push: `git tag v1.0.0; git push origin v1.0.0`. CI stamps the version into `RunePriest.json`, creates the GitHub release, and uploads to the release item.
4. Switch the item to **Public** on its Workshop page.

## Maintenance

- **Login fails in CI** (token expired, password changed, devices deauthorized): repeat section 1, then update `STEAM_CONFIG_VDF`.
- **Revoke CI access**: Steam Guard → *Deauthorize all other devices*, then delete the secret.
- `STEAM_CONFIG_VDF` is a login token for the main Steam account — treat it like a password.

## Tester notes

- Nightly is friends-only: testers friend the uploading Steam account, then subscribe.
- Subscribe to **either** the nightly or the release item, never both — same mod ID `RunePriest` gives a duplicate-ID error.
- A copy in the local `mods/` folder with an equal or higher version overrides the Workshop copy; remove it.
- Co-op partners need the same build.
