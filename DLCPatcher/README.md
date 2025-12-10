# Unity IL Patching Tutorial

> **Learn how .NET IL bytecode works by modifying a real Unity game**

A hands-on tutorial exploring IL (Intermediate Language) patching techniques in Unity/.NET applications. Uses "Across The Obelisk" as a practical case study to understand:

- How C# compiles to IL bytecode
- How to decompile and analyze .NET assemblies
- How to modify IL instructions using Mono.Cecil
- Why client-side verification is inherently insecure

---

## Responsible Use Policy

### ✅ Permitted Uses
- Security research and education
- CTF (Capture The Flag) challenges and competitions
- Academic study of game protection mechanisms
- Understanding IL/bytecode manipulation techniques
- Developing better protection for your own games
- Authorized penetration testing

### ❌ Prohibited Uses
- Accessing content you haven't legally purchased
- Piracy or theft of services
- Commercial exploitation
- Distributing patched game files
- Any activity that violates applicable laws

### Legal Notice

This project is provided for **educational purposes only** under fair use principles for security research. No copyrighted game code or assets are distributed. Users are solely responsible for ensuring their use complies with applicable laws and terms of service.

**If you are a rights holder** with concerns about this research, please open an issue. I will promptly address legitimate concerns.

---

## What You'll Learn

### Why Unity/Mono Games Are Easy to Analyze

Unity games compiled with **Mono** (instead of IL2CPP) retain full metadata:

```
C# Source Code → IL Bytecode (with names!) → JIT Compiled at Runtime
                      ↑
              We can read and modify this!
```

This allows:
- Near-perfect decompilation back to C#
- Easy identification of any function
- Simple bytecode patching

### The Security Lesson

**Client-side trust is broken by design.** Any verification running on the user's machine can be bypassed.

```
┌─────────────────────────────────────────────────────────────────┐
│                    THE FUNDAMENTAL PROBLEM                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   "Should I show DLC content?"                                  │
│                                                                 │
│   ┌─────────────┐         ┌─────────────┐                      │
│   │   CLIENT    │ ◄─────► │   STEAM     │                      │
│   │  (Game.exe) │  Query  │   CLIENT    │                      │
│   │             │         │             │                      │
│   │  Decision   │         │  Ownership  │                      │
│   │  made HERE  │◄────────│  Info       │                      │
│   └─────────────┘         └─────────────┘                      │
│         │                                                       │
│         ▼                                                       │
│   VULNERABLE: Attacker controls the decision point             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### The Patch (Proof of Concept)

```csharp
// ORIGINAL: 59 bytes, checks Steam API
public bool PlayerHaveDLC(string _sku) {
    if (GetDeveloperMode() || CheatMode) return true;
    if (SteamApps.IsSubscribedToApp(sku)) return true;
    return false;
}

// PATCHED: 2 bytes, always returns true
public bool PlayerHaveDLC(string _sku) {
    return true;
}
```

**Raw IL change:**
```
Before: 28 XX XX XX XX 6F XX XX ... (59 bytes)
After:  17 2A                       (2 bytes: ldc.i4.1, ret)
```

---

## Documentation

| Document | Description |
|----------|-------------|
| [TECHNICAL.md](TECHNICAL.md) | Why Unity/Mono games are vulnerable vs protected games |
| [DLC.md](DLC.md) | Complete reverse engineering methodology |
| `Program.cs` | Proof-of-concept patcher using Mono.Cecil |

---

## Security Recommendations for Developers

If you're building a Unity game, here's how to protect against this class of attack:

### 1. Use IL2CPP (Not Mono)
```
Unity → Build Settings → Player → Scripting Backend → IL2CPP
```
Compiles C# → C++ → Native. Much harder to reverse.

### 2. Server-Side Validation
```csharp
// ❌ Vulnerable: Client decides
if (SteamApps.IsSubscribedToApp(dlcId))
    ShowContent();

// ✅ Secure: Server decides
var response = await GameServer.ValidateDLC(steamTicket, dlcId);
if (response.Valid)
    LoadEncryptedContent(response.DecryptionKey);
```

### 3. Encrypt DLC Content
- Don't ship plaintext DLC assets
- Derive decryption keys from valid license tokens
- Bypassing the check is useless without the key

### 4. Code Obfuscation
- [Beebyte Obfuscator](https://assetstore.unity.com/packages/tools/utilities/obfuscator-48919)
- [ConfuserEx](https://github.com/mkaring/ConfuserEx)

Makes analysis harder (not impossible, but raises the bar).

### 5. Integrity Verification
- Hash critical assemblies at startup
- Detect tampering and refuse to run
- Phone home for validation (with offline grace period)

---

## Running the Research Tool

### Prerequisites
- .NET 6 SDK
- The target game installed (for case study)

### Via Makefile
```bash
make dlc-status   # Check current state
make dlc-patch    # Apply research patch
make dlc-restore  # Restore original
make dlc-help     # Technical details
```

### Standalone
```bash
dotnet build -c Release
dotnet run -- --status              # Check status
dotnet run -- [path-to-dll]         # Apply patch
dotnet run -- --restore             # Restore
```

### Platform Support

| Platform | Status | Auto-detected Path |
|----------|--------|-------------------|
| macOS | ✅ | `~/Library/Application Support/Steam/steamapps/common/...` |
| Windows | ✅ | `C:\Program Files (x86)\Steam\steamapps\common\...` |
| Linux | ✅ | `~/.steam/steam/steamapps/common/...` |

---

## Project Structure

```
DLCPatcher/
├── README.md           # This file
├── TECHNICAL.md        # Vulnerability analysis
├── DLC.md              # Reverse engineering writeup
├── DLCPatcher.csproj   # .NET project
└── Program.cs          # PoC patcher (Mono.Cecil)
```

---

## References

- [Mono.Cecil](https://github.com/jbevain/cecil) - .NET assembly manipulation
- [ILSpy](https://github.com/icsharpcode/ILSpy) - .NET decompiler
- [dnSpy](https://github.com/dnSpy/dnSpy) - .NET debugger/editor
- [Unity IL2CPP](https://docs.unity3d.com/Manual/IL2CPP.html) - Secure compilation
- [OWASP Mobile Top 10](https://owasp.org/www-project-mobile-top-10/) - Client-side security

---

## License

MIT License - Applies to research code only, not to any third-party software or game assets.

---

*This research demonstrates a known class of vulnerability. The goal is education and helping developers build more secure software.*
