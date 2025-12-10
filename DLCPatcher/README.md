# DLC Patcher for Across The Obelisk

A command-line tool that patches the game to bypass Steam DLC ownership verification. Uses IL (Intermediate Language) patching via Mono.Cecil to modify the game's .NET assembly.

## Quick Start

```bash
# From the UnlockTheObelisk root directory:

# Check if game is patched
make dlc-status

# Apply DLC bypass patch
make dlc-patch

# Restore original game files
make dlc-restore

# Show technical details
make dlc-help
```

## How It Works

### Why Unity Games Can Be Decompiled

Unlike C/C++ games that compile to unreadable machine code, Unity/.NET games compile to **IL (Intermediate Language)** which preserves all names and logic:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    NATIVE vs .NET COMPILATION                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  C/C++ (HARD TO REVERSE):              Unity/C# (EASY TO REVERSE):     │
│                                                                         │
│  Source     Compiler    Binary         Source    Compiler     IL DLL   │
│  ┌─────┐    ┌─────┐    ┌─────┐        ┌─────┐    ┌─────┐    ┌─────┐   │
│  │ C++ │───▶│ gcc │───▶│ x64 │        │ C#  │───▶│Roslyn│───▶│ IL  │   │
│  │code │    │     │    │code │        │code │    │     │    │code │   │
│  └─────┘    └─────┘    └─────┘        └─────┘    └─────┘    └─────┘   │
│                             │                                   │       │
│                             ▼                                   ▼       │
│                     48 89 5C 24 08              .method public bool    │
│                     57 48 83 EC 20                PlayerHaveDLC()      │
│                     (unreadable)                  (readable!)          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### The Attack Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         COMPLETE ATTACK CHAIN                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐        │
│   │  FIND    │    │DECOMPILE │    │  PATCH   │    │  PROFIT  │        │
│   │  DLL     │───▶│  WITH    │───▶│  WITH    │───▶│  ALL DLC │        │
│   │          │    │  ILSPY   │    │MONO.CECIL│    │ UNLOCKED │        │
│   └──────────┘    └──────────┘    └──────────┘    └──────────┘        │
│        │               │               │               │               │
│        ▼               ▼               ▼               ▼               │
│   Assembly-       See full       Replace IL      Game loads           │
│   CSharp.dll      C# source      bytecode        patched DLL          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### The Problem (Original Code)

Across The Obelisk checks Steam for DLC ownership via:
```csharp
public bool PlayerHaveDLC(string _sku)
{
    if (GameManager.Instance.GetDeveloperMode() || GameManager.Instance.CheatMode)
        return true;

    if (SteamApps.IsSubscribedToApp(sku) && LauncherEnabledDLC(sku))
        return true;

    return false;
}
```

### The Solution (Patched Code)

We patch the method to always return `true`:
```csharp
public bool PlayerHaveDLC(string _sku)
{
    return true;
}
```

### What The Patch Does

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    BEFORE AND AFTER PATCHING                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ORIGINAL (23 IL instructions):       PATCHED (2 IL instructions):     │
│  ┌─────────────────────────────┐     ┌─────────────────────────────┐   │
│  │ call GetInstance()          │     │ ldc.i4.1    // push "true"  │   │
│  │ callvirt GetDeveloperMode() │     │ ret         // return it    │   │
│  │ brtrue.s return_true        │     └─────────────────────────────┘   │
│  │ call GetInstance()          │                                       │
│  │ callvirt get_CheatMode()    │      Method now ALWAYS returns true   │
│  │ brfalse.s check_steam       │      Steam check COMPLETELY REMOVED   │
│  │ ldc.i4.1                    │                                       │
│  │ ret                         │                                       │
│  │ ldarg.1                     │                                       │
│  │ call UInt32.Parse()         │                                       │
│  │ call IsSubscribedToApp() ◄──│── This Steam API call is GONE        │
│  │ brfalse.s return_false      │                                       │
│  │ ... more checks ...         │                                       │
│  │ ret                         │                                       │
│  └─────────────────────────────┘                                       │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Why The Game Doesn't Detect This

```
┌─────────────────────────────────────────────────────────────────────────┐
│                 NO PROTECTION = EASY TARGET                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ❌ No code signing      - DLL not cryptographically signed            │
│  ❌ No hash verification - Game doesn't check if DLL was modified      │
│  ❌ No anti-tamper       - No Denuvo/VMProtect                         │
│  ❌ No server validation - DLC check is 100% client-side               │
│  ❌ No IL2CPP            - Uses Mono (readable IL), not native code    │
│                                                                         │
│  The game TRUSTS the DLL completely. Modify it = game runs our code.  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Platform Support

### macOS
✅ **Fully Supported**

```bash
# Requires .NET 6 SDK
brew install dotnet@6

# Game DLL auto-detected at:
~/Library/Application Support/Steam/steamapps/common/Across the Obelisk/Contents/Resources/Data/Managed/Assembly-CSharp.dll
```

### Windows
✅ **Fully Supported**

```bash
# Requires .NET 6 SDK (download from Microsoft)

# Game DLL auto-detected at:
C:\Program Files (x86)\Steam\steamapps\common\Across the Obelisk\AcrossTheObelisk_Data\Managed\Assembly-CSharp.dll
```

Or specify custom path:
```bash
dotnet run -- "D:\Games\Across the Obelisk\AcrossTheObelisk_Data\Managed\Assembly-CSharp.dll"
```

### Linux
✅ **Should Work** (untested)

```bash
# Install .NET 6 SDK via package manager

# Typical Steam path:
~/.steam/steam/steamapps/common/Across the Obelisk/.../Assembly-CSharp.dll
```

## Makefile Commands

| Command | Description |
|---------|-------------|
| `make dlc-patch` | Apply the DLC bypass patch |
| `make dlc-restore` | Restore original DLL from backup |
| `make dlc-status` | Check if game is patched or original |
| `make dlc-help` | Show technical details and DLC list |
| `make dlc-build` | Build the patcher (auto-run by other commands) |

## Standalone Usage

You can also run the patcher directly:

```bash
# Build
dotnet build -c Release

# Run with auto-detection
dotnet bin/Release/net6.0/DLCPatcher.dll

# Run with custom path
dotnet bin/Release/net6.0/DLCPatcher.dll /path/to/Assembly-CSharp.dll

# Check status
dotnet bin/Release/net6.0/DLCPatcher.dll --status

# Restore original
dotnet bin/Release/net6.0/DLCPatcher.dll --restore
```

## DLCs Unlocked

| App ID | DLC Name |
|--------|----------|
| 2666340 | Amelia the Queen |
| 2168960 | Spooky Nights in Senenthia |
| 2511580 | Sands of Ulminin |
| 2325780 | Wolf Wars |
| 2879690 | The Obsidian Uprising |
| 2879680 | Nenukil the Engineer |
| 3875470 | Necropolis |
| 4013420 | Asian Skins |

## Files

```
DLCPatcher/
├── README.md                   # This file
├── DLC.md                      # Full reverse engineering writeup
├── TECHNICAL.md                # Why Unity games are vulnerable
├── HOW_DECOMPILING_WORKS.md    # Deep dive into IL patching
├── DLCPatcher.csproj           # .NET project file
└── Program.cs                  # Patcher source code
```

## Backup & Restore

The patcher automatically creates a backup before patching:
- Backup: `Assembly-CSharp.dll.backup`
- Original is preserved and can be restored anytime

To restore manually:
```bash
# macOS
cp "~/Library/.../Managed/Assembly-CSharp.dll.backup" "~/Library/.../Managed/Assembly-CSharp.dll"

# Windows
copy "...\Managed\Assembly-CSharp.dll.backup" "...\Managed\Assembly-CSharp.dll"
```

## Notes

- **Steam Updates**: Steam may restore original files during game updates or "Verify Integrity". Re-run `make dlc-patch` after updates.
- **Multiplayer**: Patched DLLs may affect multiplayer compatibility.
- **Leaderboards**: Some leaderboard features may not work correctly.

## Technical Details

See [DLC.md](DLC.md) for the full reverse engineering writeup including:
- Steam API verification flow
- Decompilation process
- All patching methods considered
- Alternative approaches (Steam emulators, memory patching)

## Dependencies

- [Mono.Cecil](https://github.com/jbevain/cecil) - .NET assembly manipulation library
- .NET 6.0 SDK
