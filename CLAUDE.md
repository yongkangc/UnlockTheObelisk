# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

UnlockTheObelisk is a save editor for the game "Across The Obelisk". It provides both an interactive TUI (Terminal User Interface) and CLI modes to modify player save files:
- `player.ato` - Main progression, heroes, town upgrades, currencies
- `runs.ato` - Reward chests (completed runs)
- `gamedata_*.ato` - Active in-game runs (gold/shards during a run)

## Build & Run Commands

```bash
# Build (requires .NET 6 and game installed for DLL references)
make build

# Run interactive TUI editor
make run          # or: make tui

# CLI quick commands (auto-backups before running)
make cli-all      # Unlock everything
make cli-heroes   # Unlock all heroes
make cli-town     # Unlock all town upgrades
make cli-perks    # Max perk points

# Save management
make backup       # Backup player.ato only
make backup-full  # Backup ALL save files (player, runs, perks, gamedata)
make restore      # Restore player.ato from latest backup
make restore-full # Restore ALL save files from latest full backup
make find-save    # Show detected save path
make clean        # Clean build artifacts

# DLC Patcher (bypasses Steam DLC ownership checks)
make dlc-patch    # Apply DLC bypass patch to game
make dlc-restore  # Restore original game DLL
make dlc-status   # Check if game is patched
```

On Mac, the dotnet command is at `/opt/homebrew/opt/dotnet@6/bin/dotnet`.

## Architecture

### Save File Handling
- **Cryptography.cs** - DES encryption key/IV used by the game for save files
- **SaveManager.cs** - Load/save operations using DESCryptoServiceProvider + BinaryFormatter
  - Handles PlayerData, GameData, and runs (as JSON strings)
- **PlayerData** - Game's class for main progression (from Assembly-CSharp.dll)
- **GameData** - Game's class for active runs (from Assembly-CSharp.dll)
- **PlayerRun** - Game's class for completed runs (from Assembly-CSharp.dll)

### Application Modes
- **Program.cs** - Entry point; routes to TUI (default) or CLI mode based on arguments
- **SaveEditor.cs** - Interactive TUI built with Spectre.Console; menu-driven save editing
- **Reference.cs** - Static data (hero list, max values)

### Dependencies
The project references DLLs from the game's `Managed/` folder. The `gamePath` property in ATOUnlocker.csproj must point to:
- Mac: `~/Library/Application Support/Steam/steamapps/common/Across the Obelisk/Contents/Resources/Data/Managed/`
- Windows: `C:\Program Files (x86)\Steam\steamapps\common\Across the Obelisk\AcrossTheObelisk_Data\Managed\`

Save files are located at:
- Mac: `~/Library/Application Support/Dreamsite Games/AcrossTheObelisk/<SteamID>/`
- Windows: `%APPDATA%/../LocalLow/Dreamsite Games/AcrossTheObelisk/<SteamID>/`

## Key Implementation Details

- Save files are DES-encrypted with hardcoded key/IV matching the game's implementation
- Town upgrades follow naming convention: `townUpgrade_{tier}_{slot}` where tier and slot are 1-6
- Hero IDs are lowercase strings: `archer`, `assassin`, `berserker`, etc.
- The Makefile auto-detects Steam ID from the save directory to locate player.ato

## runs.ato Format (CRITICAL)

The game stores reward chests/completed runs in `runs.ato` using a **specific format**:

```
File format: BinaryFormatter-serialized List<String>
Each string: JSON-serialized PlayerRun object
```

**WRONG** (causes game to crash - UI won't load):
```csharp
List<PlayerRun> runs;  // BinaryFormatter serializes PlayerRun objects directly
```

**CORRECT** (game loads properly):
```csharp
List<String> runs;     // Each string is a JSON-serialized PlayerRun
```

Example JSON string in the list:
```json
{"Id":"ZY2QEFK_866_1850","Version":"1.7.0","GoldGained":491,"DustGained":668,...}
```

The SaveManager.cs handles this conversion:
- `LoadRuns()` - Loads `List<String>`, deserializes each JSON to PlayerRun
- `SaveRuns()` - Serializes each PlayerRun to JSON, saves as `List<String>`

## DLC Patcher

The DLC patcher modifies `Assembly-CSharp.dll` to bypass Steam DLC ownership checks.

```bash
make dlc-patch    # Apply patch (creates .backup automatically)
make dlc-restore  # Restore original DLL from .backup
make dlc-status   # Check current patch status
```

**How it works:**
- Uses Mono.Cecil to modify `SteamManager.PlayerHaveDLC()` method
- Replaces original logic with `return true;` (2 IL instructions: `ldc.i4.1` + `ret`)
- Original DLL backed up to `Assembly-CSharp.dll.backup`

**Note:** Steam may restore original files during game updates or file verification. Re-run `make dlc-patch` after updates.
