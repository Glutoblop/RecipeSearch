# Recipe Search

![version](https://img.shields.io/badge/version-0.2.0-blue) ![valheim](https://img.shields.io/badge/Valheim-1.0.21-orange) ![client](https://img.shields.io/badge/client--side-only-green)

A BepInEx plugin for Valheim.

Scrolling through a workbench's recipe list gets old fast. Recipe Search adds a search box above the crafting list: start typing and the list filters instantly, with forgiving fuzzy matching.

## Features
- Search box above the recipe list at every crafting station.
- Fuzzy matching, best match first (or craftable recipes first, if you prefer).
- Optionally matches internal prefab names too (e.g. `SwordBronze`).
- Game hotkeys are suppressed while you type, so typing doesn't trigger game actions.
- Clears itself when the inventory closes (configurable).

## Configuration (`BepInEx/config/jordan.valheim.recipesearch.cfg`)
| Section | Key | Default | What it does |
|---|---|---|---|
| Search | ClearOnClose | true | Clear the search box when the inventory closes. |
| Search | MatchPrefabNames | true | Also match internal prefab names. |
| Search | CraftableFirst | false | List craftable recipes before uncraftable ones. |

## Install

### Easiest: Valheim Mod Installer
Run the **Valheim Mod Installer** (`Start-Installer.bat` on Windows, `start-installer.sh` on Linux/macOS), point it at your Valheim folder, tick **Recipe Search** and click **Install / Update**. It installs BepInEx for you if needed.

### Manual
1. Install [BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) (5.4.2360 or newer) into your Valheim folder.
2. Download [`dist/RecipeSearch.dll`](https://github.com/Glutoblop/RecipeSearch/raw/main/dist/RecipeSearch.dll).
3. Copy it to `<Valheim>/BepInEx/plugins/RecipeSearch/RecipeSearch.dll`.
4. Launch the game once; settings are written to `BepInEx/config/jordan.valheim.recipesearch.cfg`.

## Compatibility

| | |
|---|---|
| Plugin version | **0.2.0** |
| Last tested on Valheim | **1.0.21** (network version 41, Steam build 26249040) on 2026-11-03 |
| BepInExPack_Valheim | 5.4.2360 |
| Multiplayer | Client-side only: works on vanilla servers, other players don't need it |

Machine-readable versions live in [`plugin.json`](plugin.json). The installer reads it straight from `main` to tell users when an update is available, or when the mod hasn't been tested against their game version.

## Building

Requires the .NET SDK (8+) and a Valheim install with BepInEx.

```
dotnet build -c Release -p:GameDir="C:\Path\To\Valheim"
```

The build copies the DLL into `<GameDir>/BepInEx/plugins/RecipeSearch` for testing, and into `dist/` (which is committed, and is what the installer downloads).

## Releasing an update

1. Bump `Version` in `src/Plugin.cs` **and** `version` in `plugin.json` (keep them identical).
2. If you tested on a newer Valheim, update the `valheim` block in `plugin.json` (version and network version are printed in `BepInEx/LogOutput.log`; the Steam build id is in `steamapps/appmanifest_892970.acf`).
3. Add a line to `changelog` in `plugin.json`.
4. `dotnet build -c Release`, then commit `dist/RecipeSearch.dll` + `plugin.json` and push to `main`. Installer users see the update next time they open it.
