# Architecture

This document explains how the Across the Obelisk save editor works, for engineers who want to understand or extend it.

## Game Architecture Overview

Across the Obelisk is a Unity game written in C#. Key technical details:

- **Engine**: Unity with C# (.NET runtime)
- **Save Format**: .NET `BinaryFormatter` serialization
- **Encryption**: DES (Data Encryption Standard) with hardcoded key/IV
- **Platform**: Cross-platform (Windows, Mac, Linux)

## Why Reverse Engineering Works

Unity games compile C# source code to IL (Intermediate Language), not native machine code. This makes reverse engineering straightforward:

1. **IL is Decompilable**: Tools like [ILSpy](https://github.com/icsharpcode/ILSpy) or [dnSpy](https://github.com/dnSpy/dnSpy) can decompile the game's DLLs back to readable C# code

2. **Game DLLs Location**:
   - Mac: `~/Library/Application Support/Steam/steamapps/common/Across the Obelisk/Contents/Resources/Data/Managed/Assembly-CSharp.dll`
   - Windows: `C:\Program Files (x86)\Steam\steamapps\common\Across the Obelisk\AcrossTheObelisk_Data\Managed\Assembly-CSharp.dll`

3. **Save Format Matches Code**: The save files are serialized C# objects. By decompiling `PlayerData`, `PlayerRun`, etc., we know exactly what fields exist and their types

4. **Encryption Key in Code**: The DES encryption key and IV are hardcoded in the game's `SaveManager` class, easily extracted via decompilation

## Save File Structure

Save files are located at:
- **Mac**: `~/Library/Application Support/Dreamsite Games/AcrossTheObelisk/<SteamID>/`
- **Windows**: `%APPDATA%/../LocalLow/Dreamsite Games/AcrossTheObelisk/<SteamID>/`

### Files

| File | Contents | Class |
|------|----------|-------|
| `player.ato` | Main progression data | `PlayerData` |
| `runs.ato` | Completed runs / reward chests | `List<PlayerRun>` |
| `perks.ato` | Perk configuration | Perk data |

All files use the same DES encryption with identical key/IV.

### PlayerData Fields (partial list)

```csharp
// Currencies
int supplyActual;        // Supply currency (spendable)
int goldGained;          // Lifetime gold earned
int dustGained;          // Lifetime shards earned
int playerRankProgress;  // Perk points

// Progression
bool ngUnlocked;         // New Game+ unlocked
int ngLevel;             // NG+ level
int obeliskMadnessLevel;
int maxAdventureMadnessLevel;
int singularityMadnessLevel;

// Unlocks
List<string> unlockedHeroes;
List<string> unlockedCards;
List<string> unlockedNodes;
List<string> supplyBought;  // Town upgrades

// Progress
Dictionary<string, int> heroProgress;
Dictionary<string, List<string>> heroPerks;
```

### PlayerRun Fields (reward chests)

```csharp
string Id;           // Unique run identifier
int GoldGained;      // Gold in reward chest
int DustGained;      // Shards in reward chest
string Char0, Char1, Char2, Char3;  // Heroes used
// ... many more stats fields
```

## Technical Flow

```
┌─────────────┐     ┌─────────────┐     ┌────────────────────┐     ┌──────────────┐
│  Save File  │────▶│ DES Decrypt │────▶│ BinaryFormatter    │────▶│ PlayerData   │
│  (.ato)     │     │             │     │ Deserialize        │     │ Object       │
└─────────────┘     └─────────────┘     └────────────────────┘     └──────────────┘

┌──────────────┐     ┌────────────────────┐     ┌─────────────┐     ┌─────────────┐
│ PlayerData   │────▶│ BinaryFormatter    │────▶│ DES Encrypt │────▶│  Save File  │
│ Object       │     │ Serialize          │     │             │     │  (.ato)     │
└──────────────┘     └────────────────────┘     └─────────────┘     └─────────────┘
```

### Code Example

```csharp
// Decryption (simplified)
using var des = new DESCryptoServiceProvider { Key = key, IV = iv };
using var decryptor = des.CreateDecryptor();
using var cryptoStream = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read);
var formatter = new BinaryFormatter();
var playerData = (PlayerData)formatter.Deserialize(cryptoStream);

// Encryption (simplified)
using var encryptor = des.CreateEncryptor();
using var cryptoStream = new CryptoStream(fileStream, encryptor, CryptoStreamMode.Write);
formatter.Serialize(cryptoStream, playerData);
```

## What Can Be Modified

### Editable via Save File
- All currencies (supply, gold, dust, perk points)
- Hero unlocks and progress
- Card unlocks
- Town upgrades
- Madness levels
- NG+ status and level
- Reward chests (in runs.ato)

### NOT Editable via Save File
- **DLC Ownership**: The game checks Steam/Paradox API at runtime for DLC purchases. Save file edits cannot bypass this - you must own the DLC through the store.

## Decompiling Commands

Using `ilspycmd` (requires .NET SDK):

```bash
# Install ilspycmd
dotnet tool install --global ilspycmd --version 7.2.1.6856

# List all classes
DOTNET_ROOT="/path/to/dotnet" ilspycmd -l c "path/to/Assembly-CSharp.dll"

# Decompile a specific class
DOTNET_ROOT="/path/to/dotnet" ilspycmd -t PlayerData "path/to/Assembly-CSharp.dll"
```

## Project Structure

```
ATOUnlocker/
├── Program.cs           # CLI entry point and legacy unlocker
├── Cryptography.cs      # DES key and IV constants
├── Reference.cs         # Hero IDs, card IDs, constants
└── Tui/
    ├── SaveEditor.cs    # Interactive TUI implementation
    └── SaveManager.cs   # Load/save with encryption
```

## Security Notes

- This tool modifies local save files for personal use
- Does not interact with game servers
- Does not bypass DLC ownership (Steam API verification)
- Uses deprecated APIs (`BinaryFormatter`, `DESCryptoServiceProvider`) because the game uses them
