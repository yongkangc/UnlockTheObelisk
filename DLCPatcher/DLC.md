# Steam DLC Bypass - Across The Obelisk

A CTF-style writeup on bypassing DLC ownership checks in Unity games using IL patching.

## Target Game

- **Game**: Across The Obelisk
- **Engine**: Unity (C# / .NET)
- **Platform**: Steam
- **DLL Target**: `Assembly-CSharp.dll`

## How Steam DLC Verification Works

### The Standard Flow

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Game Code     │────▶│  Steam API      │────▶│  Steam Client   │
│                 │     │  (Facepunch)    │     │  (Ownership DB) │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        │                       │                       │
        │  PlayerHaveDLC()      │  IsSubscribedToApp()  │
        │◀──────────────────────│◀──────────────────────│
        │     true/false        │     true/false        │
```

1. Game calls internal DLC check function
2. Function queries Steam API (`SteamApps.IsSubscribedToApp()`)
3. Steam client checks ownership against account
4. Returns boolean result

### Why Unity Games Are Vulnerable

Unity compiles C# to IL (Intermediate Language), not native code:

```
C# Source → IL Bytecode → JIT Compiled at Runtime
```

IL preserves:
- Class names
- Method names
- Field names
- Type information

Tools like **ILSpy**, **dnSpy**, or **dotPeek** can decompile IL back to nearly-original C# code.

---

## Reverse Engineering Process

### Step 1: Locate Game DLLs

**Mac:**
```
~/Library/Application Support/Steam/steamapps/common/Across the Obelisk/Contents/Resources/Data/Managed/Assembly-CSharp.dll
```

**Windows:**
```
C:\Program Files (x86)\Steam\steamapps\common\Across the Obelisk\AcrossTheObelisk_Data\Managed\Assembly-CSharp.dll
```

### Step 2: Decompile and Search for DLC Code

Using `ilspycmd`:
```bash
# List all classes
ilspycmd -l c Assembly-CSharp.dll | grep -i "dlc\|steam\|owned"

# Found:
# - SteamManager
# - GameManager.DLCData
```

### Step 3: Decompile Target Classes

```bash
ilspycmd -t "SteamManager" Assembly-CSharp.dll
```

---

## The Vulnerable Code

### Primary DLC Check: `SteamManager.PlayerHaveDLC`

```csharp
public bool PlayerHaveDLC(string _sku)
{
    // BYPASS CONDITION 1: Developer mode
    // BYPASS CONDITION 2: Cheat mode
    if (GameManager.Instance.GetDeveloperMode() || GameManager.Instance.CheatMode)
    {
        return true;
    }

    uint num = uint.Parse(_sku);

    // Actual Steam ownership check
    if (SteamApps.IsSubscribedToApp(num) && LauncherEnabledDLC(num))
    {
        return true;
    }

    return false;
}
```

### DLC App ID Mapping: `LauncherEnabledDLC`

```csharp
private bool LauncherEnabledDLC(uint _appId)
{
    string item = "";
    switch (_appId)
    {
        case 2666340u: item = "amelia_the_queen"; break;
        case 2168960u: item = "spooky_nights_in_senenthia"; break;
        case 2511580u: item = "sands_of_ulminin"; break;
        case 2325780u: item = "wolf_wars"; break;
        case 2879690u: item = "the_obsidian_uprising"; break;
        case 2879680u: item = "nenukil_the_engineer"; break;
        case 3875470u: item = "necropolis"; break;
        case 4013420u: item = "asian_skins"; break;
    }

    if (GameManager.Instance.DisabledDLCs.Contains(item))
    {
        return false;
    }
    return true;
}
```

### Developer Mode Check: `GameManager.GetDeveloperMode`

```csharp
public bool GetDeveloperMode()
{
    return developerMode;  // Private serialized field
}
```

---

## DLC App IDs

| App ID | Internal Name | DLC Name |
|--------|---------------|----------|
| 2666340 | amelia_the_queen | Amelia the Queen |
| 2168960 | spooky_nights_in_senenthia | Spooky Nights in Senenthia |
| 2511580 | sands_of_ulminin | Sands of Ulminin |
| 2325780 | wolf_wars | Wolf Wars |
| 2879690 | the_obsidian_uprising | The Obsidian Uprising |
| 2879680 | nenukil_the_engineer | Nenukil the Engineer |
| 3875470 | necropolis | Necropolis |
| 4013420 | asian_skins | Asian Skins |

---

## Bypass Methods

### Method 1: Patch `PlayerHaveDLC` (RECOMMENDED)

**Why it's best:**
- Single point of failure - all DLC checks go through this method
- Minimal code change
- No side effects
- Easy to implement

**Original:**
```csharp
public bool PlayerHaveDLC(string _sku)
{
    if (GameManager.Instance.GetDeveloperMode() || GameManager.Instance.CheatMode)
    {
        return true;
    }
    uint num = uint.Parse(_sku);
    if (SteamApps.IsSubscribedToApp(num) && LauncherEnabledDLC(num))
    {
        return true;
    }
    return false;
}
```

**Patched:**
```csharp
public bool PlayerHaveDLC(string _sku)
{
    return true;
}
```

**IL Bytecode (patched):**
```
IL_0000: ldc.i4.1    // Push 1 (true) onto stack
IL_0001: ret         // Return
```

### Method 2: Patch `GetDeveloperMode`

**Why use this:**
- Enables developer mode globally
- May unlock additional debug features
- Single boolean change

**Original:**
```csharp
public bool GetDeveloperMode()
{
    return developerMode;
}
```

**Patched:**
```csharp
public bool GetDeveloperMode()
{
    return true;
}
```

**Note:** This also enables developer-only features and may affect gameplay/leaderboards.

### Method 3: Modify `developerMode` Field Default

Change the field initializer:
```csharp
[SerializeField]
private bool developerMode = true;  // Changed from false
```

### Method 4: Steam API Emulator (Alternative)

Use Goldberg Steam Emulator with DLC config:
```ini
; steam_settings/DLC.txt
2666340=Amelia the Queen
2168960=Spooky Nights in Senenthia
2511580=Sands of Ulminin
2325780=Wolf Wars
2879690=The Obsidian Uprising
2879680=Nenukil the Engineer
3875470=Necropolis
4013420=Asian Skins
```

---

## Step-by-Step: IL Patching with dnSpy

### Prerequisites
- Download [dnSpy](https://github.com/dnSpy/dnSpy/releases) (Windows) or [dnSpyEx](https://github.com/dnSpyEx/dnSpy) (cross-platform)
- Backup `Assembly-CSharp.dll`

### Steps

1. **Open the DLL**
   ```
   File → Open → Assembly-CSharp.dll
   ```

2. **Navigate to Target Method**
   ```
   Assembly-CSharp → {} (global namespace) → SteamManager → PlayerHaveDLC
   ```

3. **Edit the Method**
   - Right-click on `PlayerHaveDLC`
   - Select "Edit Method (C#)..."

4. **Replace Method Body**
   ```csharp
   public bool PlayerHaveDLC(string _sku)
   {
       return true;
   }
   ```

5. **Compile**
   - Click "Compile" button
   - Fix any errors if they appear

6. **Save**
   ```
   File → Save Module → Save
   ```

7. **Test**
   - Launch game
   - DLC content should now be accessible

---

## Verification

After patching, verify the bypass works:

1. Launch the game
2. Check if DLC heroes are available in character selection
3. Check if DLC content appears in menus

---

## Technical Notes

### Why This Works

The game uses a **client-side trust model** for DLC verification:
- DLC content is already downloaded (in the DLC folder)
- Only the ownership check prevents access
- No server-side validation for single-player content

### File Integrity

Steam may verify game files and restore the original DLL. To prevent this:
- Set game to not auto-update
- Use Steam's "Verify integrity" carefully
- Keep a backup of your patched DLL

### Multiplayer Considerations

- Patched DLLs may cause issues in multiplayer
- Other players may not see DLC content
- Leaderboards may be affected (developer mode detection)

---

## Additional Findings

### Shame List

The game maintains a "shame list" of Steam IDs in `SteamManager.Awake()`:
```csharp
shameList = new List<string>();
shameList.Add("76561197967061331");
// ... ~150 more Steam IDs
```

These users have their leaderboard entries hidden from others (likely cheaters/exploiters detected by the devs).

### Developer Steam IDs

Hardcoded developer detection:
```csharp
if (steamId.ToString() == "76561198229850604" ||
    steamId.ToString() == "76561198018931074" ||
    // ... more dev IDs
{
    GameManager.Instance.SetDeveloperMode(state: true);
}
```

---

## Summary

| Method | Difficulty | Side Effects | Recommendation |
|--------|------------|--------------|----------------|
| Patch `PlayerHaveDLC` | Easy | None | **BEST** |
| Patch `GetDeveloperMode` | Easy | Enables debug features | Good |
| Steam Emulator | Medium | Requires setup | Alternative |
| Memory Patching | Hard | Runtime only | Not recommended |

**Best Approach:** Patch `SteamManager.PlayerHaveDLC` to unconditionally return `true`. This is the cleanest solution with zero side effects on gameplay.

---

## Disclaimer

This writeup is for **educational purposes** and **CTF challenges only**. Bypassing DLC checks on games you don't own the DLC for violates Steam's Terms of Service and may be illegal in your jurisdiction. Support game developers by purchasing content legitimately.
