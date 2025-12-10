# Why This Game Is Vulnerable - Technical Deep Dive

> ⚠️ **EDUCATIONAL CONTENT ONLY** - This document explains software protection mechanisms for security research and CTF challenges. Do not use this knowledge to pirate software.

## Table of Contents
1. [How PlayerHaveDLC Works](#how-playerhavedlc-works)
2. [Why This Game Is Vulnerable](#why-this-game-is-vulnerable)
3. [Why Other Games Are NOT Vulnerable](#why-other-games-are-not-vulnerable)
4. [Protection Methods Games Use](#protection-methods-games-use)
5. [The Vulnerability Spectrum](#the-vulnerability-spectrum)

---

## How PlayerHaveDLC Works

### The Original Code Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DLC CHECK FLOW                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Game wants to show DLC content (e.g., DLC hero "Amelia")                  │
│                           │                                                 │
│                           ▼                                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  if (SteamManager.Instance.PlayerHaveDLC("2666340"))                │   │
│  │  {                                                                   │   │
│  │      ShowAmeliaHero();  // Player owns DLC                          │   │
│  │  }                                                                   │   │
│  │  else                                                                │   │
│  │  {                                                                   │   │
│  │      ShowBuyDLCButton(); // Player doesn't own DLC                  │   │
│  │  }                                                                   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                           │                                                 │
│                           ▼                                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  PlayerHaveDLC(string _sku)                                         │   │
│  │  {                                                                   │   │
│  │      // Bypass for developers                                       │   │
│  │      if (GetDeveloperMode() || CheatMode)  ──────────┐              │   │
│  │          return true;                                 │              │   │
│  │                                                       │              │   │
│  │      // Actual Steam API call                         │              │   │
│  │      uint appId = uint.Parse(_sku);                   │              │   │
│  │                                                       ▼              │   │
│  │      if (SteamApps.IsSubscribedToApp(appId))    ┌──────────┐        │   │
│  │          return true;  ◄────────────────────────│  Steam   │        │   │
│  │                                                 │  Client  │        │   │
│  │      return false;                              └──────────┘        │   │
│  │  }                                                                   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### What We Changed

```
BEFORE (23 IL instructions):                AFTER (2 IL instructions):
┌────────────────────────────────┐         ┌────────────────────────────────┐
│ IL_0000: call GetInstance()    │         │ IL_0000: ldc.i4.1              │
│ IL_0005: callvirt GetDevMode() │         │ IL_0001: ret                   │
│ IL_000a: brtrue.s IL_0018      │         └────────────────────────────────┘
│ IL_000c: call GetInstance()    │                      │
│ IL_0011: callvirt get_Cheat()  │                      │
│ IL_0016: brfalse.s IL_001a     │                      ▼
│ IL_0018: ldc.i4.1              │         Just pushes "1" (true) and returns
│ IL_0019: ret                   │
│ IL_001a: ldarg.1               │         All Steam checks BYPASSED
│ IL_001b: call UInt32.Parse()   │
│ IL_0020: stloc.0               │
│ IL_0021: ldloc.0               │
│ IL_0022: call IsSubscribedTo() │◄─── This Steam API call is REMOVED
│ IL_0027: brfalse.s IL_0035     │
│ ... more instructions ...      │
│ IL_0037: ret                   │
└────────────────────────────────┘
```

### The IL Instructions Explained

```
ldc.i4.1   = "Load Constant Int32 value 1"
           = Push the number 1 onto the evaluation stack
           = In boolean context, 1 = true

ret        = "Return"
           = Pop value from stack and return it to caller
           = Returns true to whoever called PlayerHaveDLC()
```

---

## Why This Game Is Vulnerable

### Vulnerability Factor 1: Unity + C# = Readable Code

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    COMPILATION COMPARISON                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  UNITY/C# GAME (Across The Obelisk):                                       │
│  ┌────────────────┐    ┌────────────────┐    ┌────────────────┐            │
│  │   C# Source    │───▶│   IL Bytecode  │───▶│  JIT Compile   │            │
│  │                │    │  (Readable!)   │    │  (At Runtime)  │            │
│  │ class Steam {  │    │                │    │                │            │
│  │   bool Check() │    │ .method public │    │  Native x64    │            │
│  │   { ... }      │    │   instance     │    │  Machine Code  │            │
│  │ }              │    │   bool Check() │    │                │            │
│  └────────────────┘    └────────────────┘    └────────────────┘            │
│                              │                                              │
│                              ▼                                              │
│                    CAN BE DECOMPILED BACK TO C#!                           │
│                    Names, types, logic ALL preserved                        │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  C/C++ GAME (e.g., native game):                                           │
│  ┌────────────────┐    ┌────────────────┐                                  │
│  │   C++ Source   │───▶│  Native x64    │    No intermediate step!         │
│  │                │    │  Machine Code  │                                  │
│  │ class Steam {  │    │                │    Names LOST                    │
│  │   bool Check() │    │ 48 89 5C 24 08 │    Types LOST                    │
│  │   { ... }      │    │ 57 48 83 EC 20 │    Logic OBFUSCATED              │
│  │ }              │    │ 48 8B F9 E8 ...│                                  │
│  └────────────────┘    └────────────────┘                                  │
│                              │                                              │
│                              ▼                                              │
│                    VERY HARD to reverse engineer                           │
│                    Must analyze raw assembly                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Vulnerability Factor 2: Client-Side Trust

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CLIENT-SIDE vs SERVER-SIDE VALIDATION                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ACROSS THE OBELISK (Vulnerable - Client Trust):                           │
│                                                                             │
│  ┌──────────────┐         ┌──────────────┐                                 │
│  │    Client    │         │    Steam     │                                 │
│  │              │◄───────▶│    Client    │                                 │
│  │  Game Logic  │  "Own   │              │                                 │
│  │  ──────────  │  DLC?"  │  Ownership   │                                 │
│  │  if(own_dlc) │         │  Database    │                                 │
│  │    show();   │         │              │                                 │
│  │  ──────────  │         │              │                                 │
│  │              │         │              │                                 │
│  │  DLC CONTENT │         │              │     NO SERVER VALIDATION!       │
│  │  ALREADY ON  │         │              │     Game trusts local check     │
│  │  DISK!       │         │              │                                 │
│  └──────────────┘         └──────────────┘                                 │
│        │                                                                    │
│        ▼                                                                    │
│  Patch the check = Access content already on disk                          │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  SECURE GAME (Server-Side Validation):                                     │
│                                                                             │
│  ┌──────────────┐         ┌──────────────┐        ┌──────────────┐        │
│  │    Client    │         │    Steam     │        │  Game Server │        │
│  │              │◄───────▶│    Client    │◄──────▶│              │        │
│  │  Game Logic  │         │              │        │  Validates   │        │
│  │              │         │  Auth Token  │        │  ownership   │        │
│  │  Requests    │─────────│─────────────▶│───────▶│  server-side │        │
│  │  DLC content │         │              │        │              │        │
│  │              │◄────────│──────────────│───────▶│  Sends DLC   │        │
│  │              │   Only if verified     │        │  if valid    │        │
│  └──────────────┘         └──────────────┘        └──────────────┘        │
│        │                                                                    │
│        ▼                                                                    │
│  Can't patch client - server won't send content without valid ownership    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Vulnerability Factor 3: DLC Content Pre-Downloaded

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DLC CONTENT LOCATION                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  $ ls "Across the Obelisk/DLC/"                                            │
│                                                                             │
│  dlc_amelia/          ◄── All DLC files already on disk!                   │
│  dlc_asian/                                                                 │
│  dlc_bernard/              Game downloads ALL DLC content                   │
│  dlc_necropolis/           regardless of ownership                          │
│  dlc_nenukil/                                                              │
│  dlc_obsidian/             Only the CHECK prevents access                  │
│  dlc_sahti/                                                                │
│  dlc_sigrun/               Remove check = Access content                   │
│  dlc_spooky/                                                               │
│  dlc_sunken/                                                               │
│  dlc_tulah/                                                                │
│  dlc_ulminin/                                                              │
│  dlc_wolfwars/                                                             │
│                                                                             │
│  Total: ~500MB of DLC content sitting on disk, protected by ONE boolean    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Vulnerability Factor 4: Single Point of Failure

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SINGLE vs DISTRIBUTED CHECKS                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ACROSS THE OBELISK (Single Point - Vulnerable):                           │
│                                                                             │
│      ShowHero() ─────────┐                                                 │
│      LoadCards() ────────┤                                                 │
│      EnableSkin() ───────┼────▶  PlayerHaveDLC()  ◄── PATCH HERE          │
│      UnlockMap() ────────┤            │                                    │
│      ShowContent() ──────┘            ▼                                    │
│                              Returns true/false                            │
│                                                                             │
│      ONE function controls ALL DLC access                                  │
│      Patch ONE method = Unlock EVERYTHING                                  │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  BETTER DESIGN (Distributed Checks):                                       │
│                                                                             │
│      ShowHero() ─────▶ CheckHeroDLC() ─────▶ Steam + Decrypt Hero Data    │
│      LoadCards() ────▶ CheckCardsDLC() ────▶ Steam + Decrypt Card Data    │
│      EnableSkin() ───▶ CheckSkinDLC() ─────▶ Steam + Validate Asset       │
│      UnlockMap() ────▶ CheckMapDLC() ──────▶ Steam + Server Verify        │
│                                                                             │
│      Multiple independent checks                                           │
│      Each with its own validation                                          │
│      Patching one doesn't unlock others                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Why Other Games Are NOT Vulnerable

### Protection Method 1: Native Code (C/C++)

**Examples:** Most AAA games, Unreal Engine games

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         NATIVE CODE PROTECTION                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Original C++ code:           Compiled binary:                              │
│  ┌──────────────────────┐     ┌──────────────────────────────────────────┐ │
│  │ bool CheckDLC() {    │     │ 55                    ; push rbp         │ │
│  │   if (steam->owns()) │ ──▶ │ 48 89 E5              ; mov rbp, rsp     │ │
│  │     return true;     │     │ 48 83 EC 10           ; sub rsp, 0x10    │ │
│  │   return false;      │     │ 48 8B 05 XX XX XX XX  ; mov rax, [rip+X] │ │
│  │ }                    │     │ 48 8B 00              ; mov rax, [rax]   │ │
│  └──────────────────────┘     │ FF 50 48              ; call [rax+0x48]  │ │
│                               │ 84 C0                 ; test al, al      │ │
│  Function name: LOST          │ 74 07                 ; jz 0x...         │ │
│  Variable names: LOST         │ B8 01 00 00 00        ; mov eax, 1       │ │
│  Logic: OBFUSCATED           │ EB 05                 ; jmp 0x...        │ │
│                               │ B8 00 00 00 00        ; mov eax, 0       │ │
│                               │ C9                    ; leave            │ │
│                               │ C3                    ; ret              │ │
│                               └──────────────────────────────────────────┘ │
│                                                                             │
│  To patch this you need to:                                                │
│  1. Reverse engineer the entire binary                                     │
│  2. Find the DLC check among millions of functions                         │
│  3. Understand x64 assembly                                                │
│  4. Locate the correct bytes to patch                                      │
│  5. Deal with anti-tamper if present                                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Protection Method 2: Server-Side Validation

**Examples:** MMOs, Live Service Games, Online-Only Games

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      SERVER-SIDE DLC VALIDATION                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Player requests DLC content:                                              │
│                                                                             │
│  ┌────────────┐                              ┌────────────────────────────┐│
│  │   Client   │  "I want DLC hero Amelia"    │      Game Server          ││
│  │            │ ────────────────────────────▶│                            ││
│  │            │                              │  1. Check Steam API        ││
│  │            │                              │  2. Verify purchase        ││
│  │            │                              │  3. Check license          ││
│  │            │                              │                            ││
│  │            │  IF NOT OWNED:               │                            ││
│  │            │ ◀────────────────────────────│  "Access Denied"           ││
│  │            │                              │                            ││
│  │            │  IF OWNED:                   │                            ││
│  │            │ ◀────────────────────────────│  [Encrypted DLC Data]      ││
│  └────────────┘                              └────────────────────────────┘│
│                                                                             │
│  Client has NO DLC content locally                                         │
│  Server sends data ONLY after verification                                 │
│  Can't bypass - server won't cooperate                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Protection Method 3: Encrypted/Streamed Content

**Examples:** Modern AAA games

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       ENCRYPTED DLC CONTENT                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  DLC files on disk:                                                        │
│  ┌────────────────────────────────────────────────────────────────────────┐│
│  │  dlc_hero.pak.encrypted                                                ││
│  │  ┌────────────────────────────────────────────────────────────────┐   ││
│  │  │  A7 F3 2B 8C 91 D4 E5 6A 3F 88 C2 1D 9E 74 B5 ...              │   ││
│  │  │  (Encrypted with key derived from Steam license)               │   ││
│  │  └────────────────────────────────────────────────────────────────┘   ││
│  └────────────────────────────────────────────────────────────────────────┘│
│                                                                             │
│  Decryption flow:                                                          │
│                                                                             │
│  ┌────────────┐    ┌────────────┐    ┌────────────┐    ┌────────────┐     │
│  │   Steam    │───▶│  License   │───▶│ Decryption │───▶│  Usable    │     │
│  │  Account   │    │   Token    │    │    Key     │    │  Content   │     │
│  └────────────┘    └────────────┘    └────────────┘    └────────────┘     │
│                                                                             │
│  Even if you bypass the CHECK, content is encrypted                        │
│  Need valid Steam ownership to get decryption key                          │
│  Patching the game doesn't give you the key                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Protection Method 4: Anti-Tamper (Denuvo, VMProtect, etc.)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         ANTI-TAMPER PROTECTION                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  DENUVO (Common in AAA games):                                             │
│                                                                             │
│  Original code:                Protected code:                              │
│  ┌─────────────────┐           ┌─────────────────────────────────────────┐ │
│  │ bool CheckDLC() │           │ jmp encrypted_vm_handler                │ │
│  │ {               │   ──▶     │ ... (code virtualized)                  │ │
│  │   return steam  │           │ ... (integrity checks)                  │ │
│  │     ->owns();   │           │ ... (anti-debug)                        │ │
│  │ }               │           │ ... (online activation)                 │ │
│  └─────────────────┘           └─────────────────────────────────────────┘ │
│                                                                             │
│  Anti-tamper adds:                                                         │
│  - Code virtualization (custom VM interprets code)                         │
│  - Integrity checking (detects modified files)                             │
│  - Anti-debugging (crashes if debugger attached)                           │
│  - Online activation (phone home to verify)                                │
│  - Periodic re-verification                                                │
│                                                                             │
│  Modifying ANY byte triggers crash or ban                                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Protection Methods Games Use

### Summary Table

| Protection | How It Works | Bypass Difficulty | Used By |
|------------|--------------|-------------------|---------|
| **Native Code** | Compiles to machine code, no symbols | Hard | Unreal, C++ games |
| **Server Validation** | Server checks ownership | Very Hard | MMOs, Online games |
| **Encrypted Content** | DLC encrypted, key from license | Very Hard | AAA games |
| **Anti-Tamper** | Denuvo, VMProtect, etc. | Expert level | AAA releases |
| **Code Obfuscation** | Mangles IL/bytecode | Medium | Some Unity games |
| **Integrity Checks** | Hash verification | Medium | Various |
| **IL2CPP** | Unity IL → C++ → Native | Hard | Modern Unity games |

### Why Across The Obelisk Uses NONE Of These

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    ACROSS THE OBELISK VULNERABILITY SUMMARY                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ❌ Uses Mono (IL bytecode) instead of IL2CPP (native)                     │
│  ❌ No code obfuscation                                                     │
│  ❌ No anti-tamper                                                          │
│  ❌ Client-side only DLC checks                                             │
│  ❌ DLC content pre-downloaded and unencrypted                              │
│  ❌ Single point of failure (one function controls all DLC)                 │
│  ❌ No integrity verification of game files                                 │
│                                                                             │
│  Result: 2-line IL patch unlocks all DLC                                   │
│                                                                             │
│  This is common for:                                                       │
│  - Indie games (limited budget for DRM)                                    │
│  - Single-player focused games                                             │
│  - Games that prioritize user experience over protection                   │
│  - Developers who accept some piracy as unavoidable                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## The Vulnerability Spectrum

```
EASY TO CRACK ◄─────────────────────────────────────────────► HARD TO CRACK

┌─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┐
│  Unity  │  Unity  │  Unity  │ Native  │ Native  │ Native  │ Online  │
│  Mono   │  Mono   │  IL2CPP │  C++    │  C++    │  +Anti  │  Only   │
│  Plain  │  Obfusc │         │  Plain  │  +Integ │  Tamper │  Server │
├─────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
│         │         │         │         │         │         │         │
│  ATO    │  Some   │  Modern │  Many   │  Steam  │  AAA    │  MMOs   │
│  HERE   │  Unity  │  Unity  │  Games  │  Games  │  Games  │         │
│    ▲    │  Games  │  Games  │         │         │         │         │
│    │    │         │         │         │         │         │         │
│ 5 min   │ Hours   │  Days   │  Days   │  Weeks  │ Months  │  N/A    │
│         │         │         │         │         │         │         │
└─────────┴─────────┴─────────┴─────────┴─────────┴─────────┴─────────┘

Across The Obelisk sits at the EASIEST end of the spectrum
```

---

## Key Takeaways

1. **Unity Mono = Readable**: IL bytecode preserves all names and logic, trivial to decompile
2. **Client Trust = Exploitable**: If the client makes the decision, it can be patched
3. **Pre-downloaded DLC = Already Yours**: Content on disk just needs the gate removed
4. **Single Check = Single Patch**: One function controlling access = one patch needed
5. **No Protection = No Effort**: Indie games often skip expensive DRM solutions

The game developers likely made a conscious choice: implementing robust DRM costs development time and money, and determined pirates will crack it anyway. They focused on making a good game instead of fighting an unwinnable battle against piracy.

---

## What Are IL Instructions?

IL (Intermediate Language) is the "assembly language" of .NET. When you compile C#, it becomes IL bytecode - a set of simple stack-based instructions.

### IL Is A Stack Machine

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    HOW IL EXECUTES CODE                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  IL uses an "evaluation stack" - values are pushed on, operated on,        │
│  and popped off. Like a stack of plates.                                   │
│                                                                             │
│  Example: return 1 + 2;                                                    │
│                                                                             │
│  Step 1: ldc.i4.1          Step 2: ldc.i4.2          Step 3: add           │
│  (load constant 1)         (load constant 2)         (add top two)         │
│                                                                             │
│  ┌─────────────┐           ┌─────────────┐           ┌─────────────┐       │
│  │             │           │      2      │           │             │       │
│  │      1      │           │      1      │           │      3      │       │
│  └─────────────┘           └─────────────┘           └─────────────┘       │
│     Stack: [1]              Stack: [1, 2]             Stack: [3]           │
│                                                                             │
│  Step 4: ret                                                               │
│  (return top of stack)                                                     │
│                                                                             │
│  Returns 3 to caller!                                                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Common IL Instructions

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    IL INSTRUCTION REFERENCE                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  LOADING VALUES (push onto stack):                                         │
│  ─────────────────────────────────                                          │
│  ldc.i4.0      Load constant 0 (int)       │  Used for: false, zero        │
│  ldc.i4.1      Load constant 1 (int)       │  Used for: true, one          │
│  ldc.i4.s X    Load constant X (int)       │  Small constants (-128 to 127)│
│  ldc.i4 X      Load constant X (int)       │  Any 32-bit integer           │
│  ldstr "..."   Load string literal         │  String constants             │
│  ldarg.0       Load argument 0 (this)      │  Instance method's 'this'     │
│  ldarg.1       Load argument 1             │  First parameter              │
│  ldloc.0       Load local variable 0       │  First local variable         │
│                                                                             │
│  STORING VALUES (pop from stack):                                          │
│  ────────────────────────────────                                           │
│  stloc.0       Store to local variable 0   │  Save result temporarily      │
│  starg.1       Store to argument 1         │  Modify parameter             │
│                                                                             │
│  CALLING METHODS:                                                          │
│  ────────────────                                                           │
│  call          Call static/known method    │  Direct call, compile-time    │
│  callvirt      Call virtual method         │  Through vtable, polymorphism │
│                                                                             │
│  CONTROL FLOW:                                                             │
│  ─────────────                                                              │
│  br.s X        Branch (jump) to X          │  Unconditional goto           │
│  brtrue.s X    Branch if true              │  if (condition) goto X        │
│  brfalse.s X   Branch if false             │  if (!condition) goto X       │
│  beq.s X       Branch if equal             │  if (a == b) goto X           │
│                                                                             │
│  RETURN:                                                                   │
│  ───────                                                                    │
│  ret           Return from method          │  Pops value and returns it    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Our Patch Explained Step By Step

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    PATCHED METHOD: return true;                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  C# Code:                                                                  │
│  ┌───────────────────────────────────┐                                     │
│  │  public bool PlayerHaveDLC(...)   │                                     │
│  │  {                                │                                     │
│  │      return true;                 │                                     │
│  │  }                                │                                     │
│  └───────────────────────────────────┘                                     │
│                                                                             │
│  IL Bytecode:                                                              │
│  ┌───────────────────────────────────┐                                     │
│  │  IL_0000: ldc.i4.1                │  ◄── Push integer 1 onto stack     │
│  │  IL_0001: ret                     │  ◄── Return top of stack           │
│  └───────────────────────────────────┘                                     │
│                                                                             │
│  Execution:                                                                │
│                                                                             │
│  1. ldc.i4.1 executes:                2. ret executes:                     │
│     ┌─────────────┐                      ┌─────────────┐                   │
│     │      1      │ ◄── pushed           │             │ ◄── popped        │
│     └─────────────┘                      └─────────────┘                   │
│        Stack: [1]                         Return value: 1 (true)           │
│                                                                             │
│  Why this works:                                                           │
│  • In .NET, bool is just an int (0 = false, non-zero = true)              │
│  • ldc.i4.1 pushes 1 (true) onto the stack                                │
│  • ret pops it and returns to caller                                      │
│  • Caller receives true, thinks player owns DLC!                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### IL vs x86 Assembly Comparison

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    IL vs NATIVE ASSEMBLY                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Task: return true;                                                        │
│                                                                             │
│  .NET IL (what we patch):           x86-64 Assembly (native code):         │
│  ┌──────────────────────┐           ┌──────────────────────────────────┐   │
│  │ ldc.i4.1             │           │ mov eax, 1    ; put 1 in eax     │   │
│  │ ret                  │           │ ret           ; return eax       │   │
│  └──────────────────────┘           └──────────────────────────────────┘   │
│                                                                             │
│  Hex bytes:                         Hex bytes:                             │
│  17 2A                              B8 01 00 00 00 C3                      │
│  (2 bytes!)                         (6 bytes)                              │
│                                                                             │
│  READABILITY:                                                              │
│  ┌──────────────────────┐           ┌──────────────────────────────────┐   │
│  │ IL: Fully named      │           │ x86: Just bytes                  │   │
│  │ "ldc.i4.1" = clear   │           │ "B8 01 00 00 00" = ???           │   │
│  │ Has type info        │           │ No type info                     │   │
│  │ Stack-based (simple) │           │ Register-based (complex)         │   │
│  └──────────────────────┘           └──────────────────────────────────┘   │
│                                                                             │
│  This is why IL is so much easier to reverse engineer!                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### How Mono.Cecil Modifies IL

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    MONO.CECIL PATCHING PROCESS                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. LOAD: Read DLL into memory                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  AssemblyDefinition.ReadAssembly("Assembly-CSharp.dll")             │   │
│  │                                                                     │   │
│  │  Creates object tree:                                               │   │
│  │  Assembly                                                           │   │
│  │    └── Module                                                       │   │
│  │          └── Types[] ─────────────────────────┐                     │   │
│  │                └── SteamManager               │                     │   │
│  │                      └── Methods[] ───────────┼─────┐               │   │
│  │                            └── PlayerHaveDLC  │     │               │   │
│  │                                  └── Body ────┼─────┼──┐            │   │
│  │                                      └── Instructions  │            │   │
│  │                                          [0]: call     │            │   │
│  │                                          [1]: callvirt ◄── WE EDIT  │   │
│  │                                          [2]: brtrue       THESE    │   │
│  │                                          ...                        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  2. MODIFY: Replace instructions                                           │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  var il = method.Body.GetILProcessor();                             │   │
│  │                                                                     │   │
│  │  // Clear old instructions                                          │   │
│  │  method.Body.Instructions.Clear();                                  │   │
│  │                                                                     │   │
│  │  // Add new ones                                                    │   │
│  │  il.Append(il.Create(OpCodes.Ldc_I4_1));  // Push 1                │   │
│  │  il.Append(il.Create(OpCodes.Ret));       // Return                │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  3. SAVE: Write modified DLL                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  assembly.Write("Assembly-CSharp.dll");                             │   │
│  │                                                                     │   │
│  │  Mono.Cecil:                                                        │   │
│  │  • Recalculates all byte offsets                                   │   │
│  │  • Updates metadata tables                                         │   │
│  │  • Fixes method body sizes                                         │   │
│  │  • Writes valid PE executable                                      │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  Game now loads OUR code instead of the original!                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Visual Summary

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    THE COMPLETE PICTURE                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                          DEVELOPER                                          │
│                              │                                              │
│                              ▼                                              │
│                      ┌──────────────┐                                       │
│                      │   C# Code    │                                       │
│                      │  if(steam    │                                       │
│                      │    .owns())  │                                       │
│                      └──────────────┘                                       │
│                              │                                              │
│                         Compile                                             │
│                              │                                              │
│                              ▼                                              │
│                      ┌──────────────┐                                       │
│                      │  IL Bytecode │◄──── READABLE! Names preserved       │
│                      │  .method     │                                       │
│                      │  PlayerHave  │                                       │
│                      │  DLC(...)    │                                       │
│                      └──────────────┘                                       │
│                              │                                              │
│              ┌───────────────┴───────────────┐                              │
│              │                               │                              │
│              ▼                               ▼                              │
│       NORMAL PATH                      ATTACKER PATH                        │
│              │                               │                              │
│              ▼                               ▼                              │
│      ┌──────────────┐                ┌──────────────┐                       │
│      │     JIT      │                │   ILSpy/     │◄── Decompile          │
│      │   Compile    │                │   dnSpy      │                       │
│      └──────────────┘                └──────────────┘                       │
│              │                               │                              │
│              ▼                               ▼                              │
│      ┌──────────────┐                ┌──────────────┐                       │
│      │   Execute    │                │  See source  │◄── Find target        │
│      │   as-is      │                │  code!       │                       │
│      └──────────────┘                └──────────────┘                       │
│              │                               │                              │
│              │                               ▼                              │
│              │                       ┌──────────────┐                       │
│              │                       │  Mono.Cecil  │◄── Modify IL          │
│              │                       │  edit IL     │                       │
│              │                       └──────────────┘                       │
│              │                               │                              │
│              │                               ▼                              │
│              │                       ┌──────────────┐                       │
│              │                       │  Patched     │◄── Save               │
│              │                       │  DLL         │                       │
│              │                       └──────────────┘                       │
│              │                               │                              │
│              └───────────────┬───────────────┘                              │
│                              │                                              │
│                              ▼                                              │
│                       ┌──────────────┐                                      │
│                       │    GAME      │                                      │
│                       │   RUNS       │                                      │
│                       │  (with our   │                                      │
│                       │   code!)     │                                      │
│                       └──────────────┘                                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```
