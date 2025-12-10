# UnlockTheObelisk (Mac Fork)

Fork of [the original UnlockTheObelisk](https://github.com/original-author/UnlockTheObelisk) with Mac support.

Simple command line tool for unlocking various things in the game Across The Obelisk.

Run at your own risk. I'm not responsible if it ruins your game data, corrupts your hard drive, destroys your marriage, or anything else.
If it does not work, you are welcome to modify the source yourself but I do not intend to provide support for this project.

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
```bash
/opt/homebrew/opt/dotnet@6/bin/dotnet ATOUnlocker/bin/Debug/net6.0/ATOUnlocker.dll "PATH_TO_player.ato" [args]
```

**Arguments:**
- `perks` - max out all perk points
- `heroes` - unlock all heroes
- `town` - unlock all town upgrades

**Examples:**
```bash
# Unlock everything
/opt/homebrew/opt/dotnet@6/bin/dotnet ATOUnlocker/bin/Debug/net6.0/ATOUnlocker.dll "$HOME/Library/Application Support/Dreamsite Games/AcrossTheObelisk/YOUR_STEAM_ID/player.ato" town perks heroes

# Just unlock town upgrades
/opt/homebrew/opt/dotnet@6/bin/dotnet ATOUnlocker/bin/Debug/net6.0/ATOUnlocker.dll "$HOME/Library/Application Support/Dreamsite Games/AcrossTheObelisk/YOUR_STEAM_ID/player.ato" town
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
```
.\ATOUnlocker.exe "PATH_TO_ATO_FILE" [args]
```

Arguments are the same as Mac: `perks`, `heroes`, `town`.
