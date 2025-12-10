# How Decompiling Works (And Why We Can Edit The Code)

> ⚠️ **EDUCATIONAL CONTENT ONLY** - This document is for security research, CTF challenges, and understanding software internals. Do not use this knowledge for piracy.

## The Key Insight: .NET Is Not "Real" Machine Code

Most people assume compiled code = unreadable binary. But .NET/Unity is different:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    THE BIG PICTURE: TWO TYPES OF COMPILATION                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  NATIVE COMPILATION (C, C++, Rust):                                        │
│  ═══════════════════════════════════                                        │
│                                                                             │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────────┐              │
│  │   Source     │      │   Compiler   │      │   Machine    │              │
│  │   Code       │─────▶│   (gcc,      │─────▶│   Code       │              │
│  │              │      │    clang)    │      │   (.exe)     │              │
│  │  int main()  │      │              │      │  48 89 5C 24 │              │
│  │  { ... }     │      │              │      │  08 57 48 83 │              │
│  └──────────────┘      └──────────────┘      └──────────────┘              │
│                                                     │                       │
│                                                     ▼                       │
│                                              CPU RUNS DIRECTLY              │
│                                              Names gone forever             │
│                                              Logic = raw bytes              │
│                                                                             │
│  ───────────────────────────────────────────────────────────────────────── │
│                                                                             │
│  .NET COMPILATION (C#, Unity):                                             │
│  ══════════════════════════════                                             │
│                                                                             │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────────┐              │
│  │   Source     │      │   Compiler   │      │     IL       │              │
│  │   Code       │─────▶│   (Roslyn)   │─────▶│  Bytecode    │              │
│  │              │      │              │      │   (.dll)     │              │
│  │  class Foo   │      │              │      │              │              │
│  │  { ... }     │      │              │      │  READABLE!   │              │
│  └──────────────┘      └──────────────┘      └──────────────┘              │
│                                                     │                       │
│                                                     ▼                       │
│                                              ┌──────────────┐              │
│                                              │     JIT      │              │
│                                              │   Compiler   │              │
│                                              │  (Runtime)   │              │
│                                              └──────────────┘              │
│                                                     │                       │
│                                                     ▼                       │
│                                              CPU RUNS THIS                  │
│                                              (Generated on-the-fly)         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Why IL (Intermediate Language) Is Readable

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         IL PRESERVES EVERYTHING                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ORIGINAL C# CODE:                        COMPILED IL (in .dll):           │
│  ┌────────────────────────────┐          ┌─────────────────────────────────┐
│  │ namespace Game             │          │ .class public Game.SteamManager │
│  │ {                          │          │ {                               │
│  │   public class SteamManager│   ──▶    │   .method public instance bool  │
│  │   {                        │          │     PlayerHaveDLC(string sku)   │
│  │     public bool PlayerHave │          │   {                             │
│  │       DLC(string sku)      │          │     IL_0000: call GetInstance   │
│  │     {                      │          │     IL_0005: callvirt GetDev... │
│  │       if (GetDevMode())    │          │     IL_000a: brtrue.s IL_0018   │
│  │         return true;       │          │     ...                         │
│  │       ...                  │          │     IL_0018: ldc.i4.1           │
│  │     }                      │          │     IL_0019: ret                │
│  │   }                        │          │   }                             │
│  │ }                          │          │ }                               │
│  └────────────────────────────┘          └─────────────────────────────────┘
│                                                                             │
│  WHAT IL PRESERVES:                      WHAT IL LOSES:                    │
│  ✓ Class names                           ✗ Comments                        │
│  ✓ Method names                          ✗ Local variable names (sometimes)│
│  ✓ Field names                           ✗ Formatting/whitespace           │
│  ✓ Parameter names                       ✗ Original syntax sugar           │
│  ✓ Type information                                                        │
│  ✓ Method signatures                                                       │
│  ✓ Inheritance hierarchy                                                   │
│  ✓ Control flow logic                                                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## The Decompilation Process

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    HOW ILSPY/DNSPY DECOMPILES CODE                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  STEP 1: Read the DLL file                                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  Assembly-CSharp.dll                                                │   │
│  │  ┌───────────────────────────────────────────────────────────────┐ │   │
│  │  │  PE Header (Windows executable format)                        │ │   │
│  │  │  ┌─────────────────────────────────────────────────────────┐  │ │   │
│  │  │  │  .NET Metadata                                          │  │ │   │
│  │  │  │  - Type definitions (classes, structs, enums)           │  │ │   │
│  │  │  │  - Method definitions (names, parameters, return types) │  │ │   │
│  │  │  │  - Field definitions                                    │  │ │   │
│  │  │  │  - String literals                                      │  │ │   │
│  │  │  │  - Assembly references                                  │  │ │   │
│  │  │  └─────────────────────────────────────────────────────────┘  │ │   │
│  │  │  ┌─────────────────────────────────────────────────────────┐  │ │   │
│  │  │  │  IL Code (method bodies)                                │  │ │   │
│  │  │  │  - Stack-based bytecode instructions                    │  │ │   │
│  │  │  │  - Each method = sequence of IL opcodes                 │  │ │   │
│  │  │  └─────────────────────────────────────────────────────────┘  │ │   │
│  │  └───────────────────────────────────────────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                          │                                  │
│                                          ▼                                  │
│  STEP 2: Parse IL bytecode into control flow graph                         │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                                                                     │   │
│  │  IL_0000: call GetInstance()     ──▶  ┌─────────────────────┐      │   │
│  │  IL_0005: callvirt GetDevMode()       │   Basic Block 1     │      │   │
│  │  IL_000a: brtrue.s IL_0018      ──▶   │   (condition check) │      │   │
│  │                                       └──────────┬──────────┘      │   │
│  │                                            true/  \false           │   │
│  │                                               /    \               │   │
│  │  IL_0018: ldc.i4.1              ──▶  ┌──────┐      ┌──────┐       │   │
│  │  IL_0019: ret                        │ true │      │check │       │   │
│  │                                      │return│      │steam │       │   │
│  │                                      └──────┘      └──────┘       │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                          │                                  │
│                                          ▼                                  │
│  STEP 3: Pattern match to reconstruct C# syntax                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                                                                     │   │
│  │  "brtrue.s after callvirt" = if statement                          │   │
│  │  "ldc.i4.1 + ret" = return true                                    │   │
│  │  "call + callvirt" = method chain                                  │   │
│  │                                                                     │   │
│  │  Reconstructed:                                                    │   │
│  │  ┌───────────────────────────────────────────────────────────────┐ │   │
│  │  │  public bool PlayerHaveDLC(string _sku)                       │ │   │
│  │  │  {                                                            │ │   │
│  │  │      if (GameManager.Instance.GetDeveloperMode())             │ │   │
│  │  │          return true;                                         │ │   │
│  │  │      ...                                                      │ │   │
│  │  │  }                                                            │ │   │
│  │  └───────────────────────────────────────────────────────────────┘ │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Why Can We EDIT The Code?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         WHY EDITING WORKS                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  THE DLL IS JUST A FILE WITH A KNOWN FORMAT                                │
│  ═══════════════════════════════════════════                                │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                     Assembly-CSharp.dll                             │   │
│  │  ┌─────────────────────────────────────────────────────────────┐   │   │
│  │  │  Offset 0x1000: Method "PlayerHaveDLC"                      │   │   │
│  │  │  ┌───────────────────────────────────────────────────────┐  │   │   │
│  │  │  │  00 28 XX XX XX XX    ; call GameManager.get_Instance │  │   │   │
│  │  │  │  6F YY YY YY YY       ; callvirt GetDeveloperMode     │  │   │   │
│  │  │  │  2D 0C                ; brtrue.s +12                  │  │   │   │
│  │  │  │  ... 20 more bytes ...                                │  │   │   │
│  │  │  │  17                   ; ldc.i4.1 (push 1)             │  │   │   │
│  │  │  │  2A                   ; ret                           │  │   │   │
│  │  │  └───────────────────────────────────────────────────────┘  │   │   │
│  │  └─────────────────────────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                          │                                  │
│                                          ▼                                  │
│                              MONO.CECIL CAN:                               │
│                              1. Read this structure                        │
│                              2. Find any method                            │
│                              3. Replace the IL bytes                       │
│                              4. Write it back                              │
│                                          │                                  │
│                                          ▼                                  │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                   Assembly-CSharp.dll (PATCHED)                     │   │
│  │  ┌─────────────────────────────────────────────────────────────┐   │   │
│  │  │  Offset 0x1000: Method "PlayerHaveDLC"                      │   │   │
│  │  │  ┌───────────────────────────────────────────────────────┐  │   │   │
│  │  │  │  17                   ; ldc.i4.1 (push 1)    ◄── NEW! │  │   │   │
│  │  │  │  2A                   ; ret                  ◄── NEW! │  │   │   │
│  │  │  │  (rest is ignored, method body replaced)              │  │   │   │
│  │  │  └───────────────────────────────────────────────────────┘  │   │   │
│  │  └─────────────────────────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## The Patching Process (What Our Tool Does)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    OUR PATCHING PROCESS STEP BY STEP                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  STEP 1: Load DLL with Mono.Cecil                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  var assembly = AssemblyDefinition.ReadAssembly("Assembly-CSharp.dll")  │
│  │                                                                     │   │
│  │  Mono.Cecil parses the entire DLL into memory:                     │   │
│  │  - All types (classes, structs, enums)                             │   │
│  │  - All methods with their IL bodies                                │   │
│  │  - All metadata                                                    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                          │                                  │
│                                          ▼                                  │
│  STEP 2: Navigate to target method                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  var steamManager = assembly.MainModule.Types                       │   │
│  │      .FirstOrDefault(t => t.Name == "SteamManager");               │   │
│  │                                                                     │   │
│  │  var method = steamManager.Methods                                  │   │
│  │      .FirstOrDefault(m => m.Name == "PlayerHaveDLC");              │   │
│  │                                                                     │   │
│  │  Found: SteamManager.PlayerHaveDLC(string) : bool                  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                          │                                  │
│                                          ▼                                  │
│  STEP 3: Clear existing IL and write new IL                                │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  // Clear the old method body                                       │   │
│  │  method.Body.Instructions.Clear();                                  │   │
│  │  method.Body.ExceptionHandlers.Clear();                            │   │
│  │                                                                     │   │
│  │  // Get IL processor                                                │   │
│  │  var il = method.Body.GetILProcessor();                            │   │
│  │                                                                     │   │
│  │  // Write new instructions: return true;                           │   │
│  │  il.Append(il.Create(OpCodes.Ldc_I4_1));  // Push 1 (true)        │   │
│  │  il.Append(il.Create(OpCodes.Ret));       // Return                │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                          │                                  │
│                                          ▼                                  │
│  STEP 4: Write modified DLL back to disk                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  assembly.Write("Assembly-CSharp.dll");                             │   │
│  │                                                                     │   │
│  │  Mono.Cecil:                                                        │   │
│  │  - Recalculates all offsets                                        │   │
│  │  - Updates metadata tables                                         │   │
│  │  - Fixes all internal references                                   │   │
│  │  - Writes valid PE/.NET executable                                 │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                          │                                  │
│                                          ▼                                  │
│  RESULT: Game loads our modified DLL, runs our code!                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Why Doesn't The Game Detect This?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    WHY THE GAME DOESN'T DETECT TAMPERING                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ❌ NO CODE SIGNING                                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  The DLL is not cryptographically signed                           │   │
│  │  Game doesn't verify: "Is this the REAL Assembly-CSharp.dll?"      │   │
│  │  It just loads whatever file has that name                         │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ❌ NO HASH VERIFICATION                                                    │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  Game doesn't check: SHA256(Assembly-CSharp.dll) == expected?      │   │
│  │  Any modified DLL will be loaded without question                  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ❌ NO RUNTIME INTEGRITY CHECKS                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  Game doesn't scan memory for modifications                        │   │
│  │  No anti-cheat watching for code changes                           │   │
│  │  No periodic re-verification                                       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ❌ NO SERVER VALIDATION                                                    │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  DLC check is 100% client-side                                     │   │
│  │  No server asks "Does this player REALLY own the DLC?"             │   │
│  │  Client says "yes" and that's final                                │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  The game TRUSTS the DLL completely                                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## What WOULD Prevent This?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      PROTECTIONS THAT WOULD STOP US                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ✓ IL2CPP (Unity's ahead-of-time compilation)                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  C# ──▶ IL ──▶ C++ ──▶ Native Machine Code                         │   │
│  │                                                                     │   │
│  │  Instead of shipping IL bytecode, Unity converts to native code    │   │
│  │  No more readable IL, no easy Mono.Cecil editing                   │   │
│  │  Would require reverse engineering native assembly                 │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ✓ CODE SIGNING / STRONG NAMING                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  DLL signed with developer's private key                           │   │
│  │  .NET runtime verifies signature before loading                    │   │
│  │  Modified DLL = invalid signature = won't load                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ✓ ANTI-TAMPER (Denuvo, VMProtect)                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  Code encrypted/virtualized                                        │   │
│  │  Integrity checks throughout execution                             │   │
│  │  Modification = crash or detection                                 │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ✓ SERVER-SIDE VERIFICATION                                                │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  Server checks Steam ownership API directly                        │   │
│  │  DLC content streamed only to verified owners                      │   │
│  │  Client modification = server refuses service                      │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ✓ OBFUSCATION                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  Rename: PlayerHaveDLC ──▶ a0x7f3b2()                              │   │
│  │  Control flow mangling                                             │   │
│  │  String encryption                                                 │   │
│  │  Much harder to find target (but still possible)                   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  Across The Obelisk uses NONE of these                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Visual: The Full Attack Chain

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         COMPLETE ATTACK FLOW                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐  │
│  │ RECON   │───▶│DECOMPILE│───▶│ ANALYZE │───▶│  PATCH  │───▶│  PROFIT │  │
│  └─────────┘    └─────────┘    └─────────┘    └─────────┘    └─────────┘  │
│       │              │              │              │              │        │
│       ▼              ▼              ▼              ▼              ▼        │
│  ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐  │
│  │ Find    │    │ ILSpy   │    │ Find    │    │Mono.    │    │ All DLC │  │
│  │ game    │    │ dnSpy   │    │ DLC     │    │Cecil    │    │ content │  │
│  │ DLLs    │    │ shows   │    │ check   │    │ edits   │    │ unlocked│  │
│  │         │    │ source  │    │ method  │    │ IL code │    │         │  │
│  └─────────┘    └─────────┘    └─────────┘    └─────────┘    └─────────┘  │
│                                                                             │
│  Time: ~30 minutes for someone who knows what they're doing                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Summary: Why This "Shouldn't Work" But Does

| What You'd Expect | Reality |
|-------------------|---------|
| "Compiled code is unreadable" | .NET IL preserves everything |
| "You can't edit binaries" | IL is a documented format, tools exist |
| "Game would detect changes" | No integrity checking at all |
| "Steam protects DLC" | Steam only checks, game trusts local result |
| "DLC content is locked" | Content is on disk, just gated by boolean |

**The entire DLC protection is ONE function returning true/false, in readable code, with no verification.**

This is why security researchers say: **"Client-side checks are not security, they're a suggestion."**
