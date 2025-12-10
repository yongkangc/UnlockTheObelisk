# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

UnlockTheObelisk is a save editor for the game "Across The Obelisk". It provides both an interactive TUI (Terminal User Interface) and CLI modes to modify player save files (player.ato).

## Build & Run Commands

```bash
# Build (requires .NET 6 and game installed for DLL references)
make build

# Run interactive TUI editor
make run

# CLI quick commands (auto-backups before running)
make cli-all      # Unlock everything
make cli-heroes   # Unlock all heroes
make cli-town     # Unlock all town upgrades
make cli-perks    # Max perk points

# Save management
make backup       # Backup save file
make restore      # Restore from latest backup
make find-save    # Show detected save path
make clean        # Clean build artifacts
```

On Mac, the dotnet command is at `/opt/homebrew/opt/dotnet@6/bin/dotnet`.

## Architecture

### Save File Handling
- **Cryptography.cs** - DES encryption key/IV used by the game for save files
- **SaveManager.cs** - Load/save operations using DESCryptoServiceProvider + BinaryFormatter
- **PlayerData** - Game's serialized class from Assembly-CSharp.dll (referenced, not defined here)

### Application Modes
- **Program.cs** - Entry point; routes to TUI (default) or CLI mode based on arguments
- **SaveEditor.cs** - Interactive TUI built with Spectre.Console; menu-driven save editing
- **Reference.cs** - Static data (hero list, max values)

### Dependencies
The project references DLLs from the game's `Managed/` folder. The `gamePath` property in ATOUnlocker.csproj must point to:
- Mac: `~/Library/Application Support/Steam/steamapps/common/Across the Obelisk/Contents/Resources/Data/Managed/`
- Windows: `D:\SteamLibrary\steamapps\common\Across the Obelisk\AcrossTheObelisk_Data\Managed\`

## Key Implementation Details

- Save files are DES-encrypted with hardcoded key/IV matching the game's implementation
- Town upgrades follow naming convention: `townUpgrade_{tier}_{slot}` where tier and slot are 1-6
- Hero IDs are lowercase strings: `archer`, `assassin`, `berserker`, etc.
- The Makefile auto-detects Steam ID from the save directory to locate player.ato
