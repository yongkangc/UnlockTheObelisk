<img width="1151" height="959" alt="image" src="https://github.com/user-attachments/assets/f556fcf6-31f3-4747-b8d8-ea78b5ad2c67" /># UnlockTheObelisk (Mac Fork)

Fork of the original UnlockTheObelisk with Mac support and an interactive TUI editor.

Simple command line tool for unlocking various things in the game Across The Obelisk.

> **For developers**: See [ARCHITECTURE.md](ARCHITECTURE.md) for technical details on how save file editing works.

Run at your own risk. I'm not responsible if it ruins your game data, corrupts your hard drive, destroys your marriage, or anything else.

## Quick Start (Mac)

```bash
# Install .NET 6
brew install dotnet@6

# Build and run TUI
make run

# Or quick unlock everything
make cli-all
```

## Features

### Interactive TUI Mode (Default)
Launch the editor with just the save file path to get an interactive menu:

![TUI Main Menu](images/tui-main-menu.png)
- **Heroes** - Select which heroes to unlock
<img width="247" height="305" alt="image" src="https://github.com/user-attachments/assets/82adda79-287d-44f7-ab0c-215c2dbc02fa" />
<img width="1655" height="984" alt="image" src="https://github.com/user-attachments/assets/c179664b-a087-4224-b5f4-ac7effe27390" />


- **Town Upgrades** - Unlock by tier (1-6) or all
- **Cards** - View and manage unlocked cards
- **Currencies & Resources** - Set Supply, Perk Points, Hero Progress
<img width="501" height="214" alt="image" src="https://github.com/user-attachments/assets/b71cf6f1-6aa8-425c-bcb0-312b4344c530" />
<img width="1151" height="959" alt="image" src="https://github.com/user-attachments/assets/2b6d7a0b-c74a-4fa1-9b61-79cfc23a8f47" />

- **Progression** - Toggle NG+ and set level
- **Madness Levels** - Set Obelisk/Adventure/Singularity madness
- **Unlock All** - Quick option to max everything
- **Reward Chests** - Create/edit reward chests claimable in town
<img width="478" height="172" alt="image" src="https://github.com/user-attachments/assets/9b6cd8b3-eaee-44c0-8747-e6f32cdd1132" />

### Important Notes

> ⚠️ **Do not edit saves during an active run.** Always return to town or exit to main menu before editing. The game saves separately for runs in progress and changes may not apply or could cause issues.

**Starting Gold/Shards for New Runs:**
The "Set Gold/Shards" options in the Resources menu edit *lifetime statistics* (total ever earned), not your starting resources. To get gold and shards at the start of a new adventure:
1. Select **Reward Chests** from the main menu
2. Create a new chest with your desired gold/shards amounts
3. Save & Exit
4. In-game, go to Town and click the **chest icons** (top-right) to claim before starting a new run

### CLI Mode
Pass arguments directly for quick unlocks without the interactive menu.

---

## Mac Setup & Usage

### Prerequisites
Install .NET 6 SDK:
```bash
brew install dotnet@6
```

### Build
The `.csproj` is pre-configured for Mac Steam installations. Just build:
```bash
/opt/homebrew/opt/dotnet@6/bin/dotnet build
```

If your game is installed elsewhere, edit `ATOUnlocker/ATOUnlocker.csproj` and update the `gamePath` property to point to your game's `Managed` folder:
```
~/Library/Application Support/Steam/steamapps/common/Across the Obelisk/Contents/Resources/Data/Managed/
```

### Backup Your Save
**BACK UP YOUR SAVE DATA BEFORE RUNNING.**

Your save data is at:
```
~/Library/Application Support/Dreamsite Games/AcrossTheObelisk/STEAM_ID/player.ato
```

Create a backup:
```bash
cp ~/Library/Application\ Support/Dreamsite\ Games/AcrossTheObelisk/YOUR_STEAM_ID/player.ato ~/Library/Application\ Support/Dreamsite\ Games/AcrossTheObelisk/YOUR_STEAM_ID/player_backup.ato
```

### Run

**Interactive TUI Mode (default):**
```bash
/opt/homebrew/opt/dotnet@6/bin/dotnet ATOUnlocker/bin/Debug/net6.0/ATOUnlocker.dll "$HOME/Library/Application Support/Dreamsite Games/AcrossTheObelisk/YOUR_STEAM_ID/player.ato"
```

**CLI Mode (legacy):**
```bash
# Unlock everything
/opt/homebrew/opt/dotnet@6/bin/dotnet ATOUnlocker/bin/Debug/net6.0/ATOUnlocker.dll "$HOME/Library/Application Support/Dreamsite Games/AcrossTheObelisk/YOUR_STEAM_ID/player.ato" town perks heroes

# Just unlock town upgrades
/opt/homebrew/opt/dotnet@6/bin/dotnet ATOUnlocker/bin/Debug/net6.0/ATOUnlocker.dll "$HOME/Library/Application Support/Dreamsite Games/AcrossTheObelisk/YOUR_STEAM_ID/player.ato" town
```

**CLI Arguments:**
- `perks` - max out all perk points
- `heroes` - unlock all heroes
- `town` - unlock all town upgrades

### Makefile Commands

The Makefile auto-detects your Steam ID and save file:

```bash
make help        # Show all commands
make build       # Build the project
make run         # Launch interactive TUI
make cli-all     # Unlock everything (auto-backups first)
make cli-heroes  # Unlock all heroes
make cli-town    # Unlock all town upgrades
make cli-perks   # Max out perk points
make backup      # Backup your save
make restore     # Restore from latest backup
make find-save   # Show detected save path
make clean       # Clean build artifacts
```

---

## Windows Setup & Usage

### Build
Edit the `.csproj` and update `gamePath` to your Windows installation:
```
D:\SteamLibrary\steamapps\common\Across the Obelisk\AcrossTheObelisk_Data\Managed\
```

Then build with Visual Studio, Rider, or `dotnet build`.

### Backup Your Save
Save data is at:
```
C:\Users\USER_NAME\AppData\LocalLow\Dreamsite Games\AcrossTheObelisk\STEAM_ID\player.ato
```

### Run

**Interactive TUI Mode (default):**
```
.\ATOUnlocker.exe "PATH_TO_player.ato"
```

**CLI Mode:**
```
.\ATOUnlocker.exe "PATH_TO_player.ato" town perks heroes
```
