# ATOUnlocker - Save File Editor

The core save file editor for Across The Obelisk. Provides both an interactive TUI (Terminal User Interface) and CLI modes for modifying player save data.

## How It Works

Across The Obelisk saves are:
1. **Serialized** using .NET `BinaryFormatter`
2. **Encrypted** with DES (hardcoded key/IV)
3. **Stored** as `.ato` files

This tool reverses that process:

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  .ato file  │────▶│ DES Decrypt │────▶│ Deserialize │────▶│ PlayerData  │
│ (encrypted) │     │  (Key/IV)   │     │ (BinFormat) │     │  (object)   │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
                                                                   │
                                                            Modify fields
                                                                   │
                                                                   ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  .ato file  │◀────│ DES Encrypt │◀────│  Serialize  │◀────│ PlayerData  │
│   (saved)   │     │  (Key/IV)   │     │ (BinFormat) │     │ (modified)  │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
```

## Project Structure

```
ATOUnlocker/
├── Program.cs           # Entry point, CLI argument handling
├── Cryptography.cs      # DES key and IV constants
├── Reference.cs         # Hero IDs, max values, constants
├── ATOUnlocker.csproj   # Project file with game DLL references
└── Tui/
    ├── SaveEditor.cs    # Interactive TUI (Spectre.Console)
    └── SaveManager.cs   # Load/save with encryption
```

## Key Files Explained

### Cryptography.cs
Contains the DES encryption key and IV extracted from the game:
```csharp
public static byte[] Key = new byte[8] { 18, 54, 100, 160, 190, 148, 136, 3 };
public static byte[] IV = new byte[8] { 82, 242, 164, 132, 119, 197, 179, 20 };
```

### SaveManager.cs
Handles the encryption/decryption and serialization:
- `LoadPlayerData(path)` - Decrypt and deserialize player.ato
- `SavePlayerData(path, data)` - Serialize and encrypt back
- `LoadRuns(path)` - Load reward chests from runs.ato
- `SaveRuns(path, runs)` - Save reward chests
- `LoadGameData(path)` - Load active run data

### SaveEditor.cs
Interactive TUI built with [Spectre.Console](https://spectreconsole.net/):
- Menu-driven interface
- Hero selection with checkboxes
- Currency editing
- Town upgrade management
- Reward chest creation

### Reference.cs
Static data about game content:
```csharp
public static List<string> Heroes = new() {
    "mercenary", "sentinel", "berserker", "warden",
    "cleric", "priest", "voodoowitch", "prophet",
    // ... all hero IDs
};
```

## Dependencies

The project references DLLs directly from the game installation:
- `Assembly-CSharp.dll` - Game's compiled code (contains `PlayerData`, `PlayerRun`, etc.)
- `Facepunch.Steamworks.*.dll` - Steam integration
- Various Unity DLLs

This is required because `BinaryFormatter` embeds full type names. Using the game's actual types ensures compatibility.

## Save File Locations

| Platform | Path |
|----------|------|
| **macOS** | `~/Library/Application Support/Dreamsite Games/AcrossTheObelisk/<SteamID>/` |
| **Windows** | `%APPDATA%/../LocalLow/Dreamsite Games/AcrossTheObelisk/<SteamID>/` |

### Save Files

| File | Contents |
|------|----------|
| `player.ato` | Main progression (heroes, currencies, unlocks) |
| `runs.ato` | Completed runs / reward chests |
| `perks.ato` | Perk configuration |
| `gamedata_0.ato` | Active run slot 0 |
| `gamedata_1.ato` | Active run slot 1 |

## Building

```bash
# Requires .NET 6 SDK and the game installed
dotnet build

# Or via Makefile from repo root
make build
```

The `gamePath` in `.csproj` must point to your game's `Managed/` folder.

## Usage

```bash
# Interactive TUI (recommended)
dotnet run -- "/path/to/player.ato"

# CLI mode
dotnet run -- "/path/to/player.ato" heroes town perks
```

## What Can Be Modified

| Category | Fields |
|----------|--------|
| **Currencies** | Supply, Perk Points, Gold/Dust (statistics) |
| **Heroes** | Unlock any hero, set progress |
| **Town** | Unlock upgrades by tier (1-6) |
| **Progression** | NG+ status, madness levels |
| **Reward Chests** | Create chests with gold/shards |

## What CANNOT Be Modified

- **DLC Ownership** - Checked via Steam API at runtime, not stored in save
- **Achievements** - Stored on Steam servers
- **Multiplayer state** - Handled by Photon servers

## Technical Notes

### Why BinaryFormatter?

The game uses the deprecated `BinaryFormatter` for serialization. This:
- Embeds full type names in serialized data
- Requires exact type matching for deserialization
- Is why we reference game DLLs directly

### Why DES?

DES is outdated but the game uses it. The key/IV are hardcoded in the game's `SaveManager` class, easily extracted via decompilation.

### Editing During Active Runs

> ⚠️ Don't edit saves during active runs!

The game maintains separate state:
- Town/progression → `player.ato`
- Active run → `gamedata_*.ato`

Changes to `player.ato` won't affect an in-progress run.

## See Also

- [ARCHITECTURE.md](../ARCHITECTURE.md) - Full technical documentation
- [DLCPatcher/](../DLCPatcher/) - IL patching tutorial (different technique)
