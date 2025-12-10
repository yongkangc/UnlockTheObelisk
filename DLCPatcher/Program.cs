using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DLCPatcher;

/// <summary>
/// DLC Bypass Patcher for Across The Obelisk
///
/// This tool patches the game's Assembly-CSharp.dll to bypass Steam DLC ownership checks.
/// It modifies SteamManager.PlayerHaveDLC() to always return true.
///
/// Usage:
///   DLCPatcher [path-to-Assembly-CSharp.dll] [--restore|--status]
///
/// Commands:
///   (no args)   - Auto-detect game path and apply patch
///   --restore   - Restore original DLL from backup
///   --status    - Check if DLL is patched or original
///   --help      - Show this help
/// </summary>
class Program
{
    static string GetDefaultDllPath()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // macOS path
        string macPath = Path.Combine(home,
            "Library/Application Support/Steam/steamapps/common/Across the Obelisk/Contents/Resources/Data/Managed/Assembly-CSharp.dll");
        if (File.Exists(macPath)) return macPath;

        // Windows common paths
        string[] windowsPaths = {
            @"C:\Program Files (x86)\Steam\steamapps\common\Across the Obelisk\AcrossTheObelisk_Data\Managed\Assembly-CSharp.dll",
            @"D:\SteamLibrary\steamapps\common\Across the Obelisk\AcrossTheObelisk_Data\Managed\Assembly-CSharp.dll",
            @"E:\SteamLibrary\steamapps\common\Across the Obelisk\AcrossTheObelisk_Data\Managed\Assembly-CSharp.dll",
        };

        foreach (var path in windowsPaths)
        {
            if (File.Exists(path)) return path;
        }

        return macPath; // Default to mac path for error message
    }

    static int Main(string[] args)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     DLC Patcher for Across The Obelisk                    ║");
        Console.WriteLine("║     Bypasses Steam DLC ownership verification             ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

        // Parse arguments
        string? dllPath = null;
        bool restore = false;
        bool status = false;
        bool help = false;

        foreach (var arg in args)
        {
            if (arg == "--restore" || arg == "-r") restore = true;
            else if (arg == "--status" || arg == "-s") status = true;
            else if (arg == "--help" || arg == "-h") help = true;
            else if (!arg.StartsWith("-")) dllPath = arg;
        }

        if (help)
        {
            ShowHelp();
            return 0;
        }

        dllPath ??= GetDefaultDllPath();

        if (!File.Exists(dllPath))
        {
            Console.WriteLine($"[ERROR] Assembly-CSharp.dll not found at:\n  {dllPath}");
            Console.WriteLine("\nProvide the path as an argument:");
            Console.WriteLine("  DLCPatcher /path/to/Assembly-CSharp.dll");
            return 1;
        }

        Console.WriteLine($"[*] Target: {dllPath}");
        string backupPath = dllPath + ".backup";

        if (status)
        {
            return CheckStatus(dllPath, backupPath);
        }

        if (restore)
        {
            return RestoreBackup(dllPath, backupPath);
        }

        return ApplyPatch(dllPath, backupPath);
    }

    static void ShowHelp()
    {
        Console.WriteLine("Usage: DLCPatcher [path] [options]\n");
        Console.WriteLine("Arguments:");
        Console.WriteLine("  path          Path to Assembly-CSharp.dll (auto-detected if omitted)\n");
        Console.WriteLine("Options:");
        Console.WriteLine("  --status, -s  Check if DLL is patched or original");
        Console.WriteLine("  --restore, -r Restore original DLL from backup");
        Console.WriteLine("  --help, -h    Show this help\n");
        Console.WriteLine("Examples:");
        Console.WriteLine("  DLCPatcher                    # Auto-detect and patch");
        Console.WriteLine("  DLCPatcher --status           # Check patch status");
        Console.WriteLine("  DLCPatcher --restore          # Restore original");
        Console.WriteLine("  DLCPatcher /path/to/dll       # Patch specific file");
    }

    static int CheckStatus(string dllPath, string backupPath)
    {
        Console.WriteLine("\n[*] Checking patch status...\n");

        bool isPatched = IsDllPatched(dllPath);
        bool hasBackup = File.Exists(backupPath);

        Console.WriteLine($"  DLL Status:    {(isPatched ? "PATCHED (DLC unlocked)" : "ORIGINAL (unmodified)")}");
        Console.WriteLine($"  Backup exists: {(hasBackup ? "Yes" : "No")}");

        if (isPatched)
        {
            Console.WriteLine("\n  All DLCs should be accessible in-game.");
            Console.WriteLine("  Run with --restore to revert to original.");
        }
        else
        {
            Console.WriteLine("\n  DLC checks are active (Steam verification).");
            Console.WriteLine("  Run without arguments to apply patch.");
        }

        return 0;
    }

    static bool IsDllPatched(string dllPath)
    {
        try
        {
            using var assembly = AssemblyDefinition.ReadAssembly(dllPath);
            var steamManager = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "SteamManager");
            if (steamManager == null) return false;

            var method = steamManager.Methods.FirstOrDefault(m => m.Name == "PlayerHaveDLC");
            if (method == null) return false;

            var instructions = method.Body.Instructions;

            // Patched version has exactly 2 instructions: ldc.i4.1, ret
            return instructions.Count == 2 &&
                   instructions[0].OpCode == OpCodes.Ldc_I4_1 &&
                   instructions[1].OpCode == OpCodes.Ret;
        }
        catch
        {
            return false;
        }
    }

    static int RestoreBackup(string dllPath, string backupPath)
    {
        Console.WriteLine("\n[*] Restoring original DLL...\n");

        if (!File.Exists(backupPath))
        {
            Console.WriteLine("[ERROR] No backup found at:");
            Console.WriteLine($"  {backupPath}");
            Console.WriteLine("\nCannot restore without backup.");
            return 1;
        }

        try
        {
            File.Copy(backupPath, dllPath, overwrite: true);
            Console.WriteLine("[SUCCESS] Original DLL restored!");
            Console.WriteLine("\nDLC checks are now active (Steam verification).");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to restore: {ex.Message}");
            return 1;
        }
    }

    static int ApplyPatch(string dllPath, string backupPath)
    {
        // Check if already patched
        if (IsDllPatched(dllPath))
        {
            Console.WriteLine("\n[!] DLL is already patched! Nothing to do.");
            Console.WriteLine("    Use --restore to revert to original.");
            return 0;
        }

        // Create backup
        if (!File.Exists(backupPath))
        {
            Console.WriteLine($"[*] Creating backup: {Path.GetFileName(backupPath)}");
            try
            {
                File.Copy(dllPath, backupPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to create backup: {ex.Message}");
                return 1;
            }
        }
        else
        {
            Console.WriteLine("[*] Backup already exists");
        }

        try
        {
            Console.WriteLine("[*] Loading assembly...");

            var readerParams = new ReaderParameters
            {
                ReadWrite = true,
                InMemory = true
            };

            using var assembly = AssemblyDefinition.ReadAssembly(dllPath, readerParams);
            var module = assembly.MainModule;

            // Find SteamManager class
            var steamManager = module.Types.FirstOrDefault(t => t.Name == "SteamManager");
            if (steamManager == null)
            {
                Console.WriteLine("[ERROR] SteamManager class not found!");
                return 1;
            }
            Console.WriteLine("[+] Found SteamManager class");

            // Find PlayerHaveDLC method
            var playerHaveDLC = steamManager.Methods.FirstOrDefault(m => m.Name == "PlayerHaveDLC");
            if (playerHaveDLC == null)
            {
                Console.WriteLine("[ERROR] PlayerHaveDLC method not found!");
                return 1;
            }
            Console.WriteLine("[+] Found PlayerHaveDLC method");

            // Show original method info
            var instructions = playerHaveDLC.Body.Instructions;
            Console.WriteLine($"\n[*] Original method: {instructions.Count} IL instructions");
            Console.WriteLine("[*] Original logic:");
            Console.WriteLine("    - Check DeveloperMode/CheatMode → return true");
            Console.WriteLine("    - Check SteamApps.IsSubscribedToApp() → return true/false");

            // Patch the method
            Console.WriteLine("\n[*] Patching method to always return true...");

            var il = playerHaveDLC.Body.GetILProcessor();
            playerHaveDLC.Body.Instructions.Clear();
            playerHaveDLC.Body.ExceptionHandlers.Clear();
            playerHaveDLC.Body.Variables.Clear();

            il.Append(il.Create(OpCodes.Ldc_I4_1));  // Push 1 (true)
            il.Append(il.Create(OpCodes.Ret));       // Return

            Console.WriteLine("[+] Patched IL:");
            Console.WriteLine("    IL_0000: ldc.i4.1   // Push true");
            Console.WriteLine("    IL_0001: ret        // Return");

            // Save
            Console.WriteLine("\n[*] Saving patched assembly...");
            assembly.Write(dllPath);

            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  [SUCCESS] DLC bypass patch applied!                      ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine("\nUnlocked DLCs:");
            Console.WriteLine("  ✓ Amelia the Queen");
            Console.WriteLine("  ✓ Spooky Nights in Senenthia");
            Console.WriteLine("  ✓ Sands of Ulminin");
            Console.WriteLine("  ✓ Wolf Wars");
            Console.WriteLine("  ✓ The Obsidian Uprising");
            Console.WriteLine("  ✓ Nenukil the Engineer");
            Console.WriteLine("  ✓ Necropolis");
            Console.WriteLine("  ✓ Asian Skins");
            Console.WriteLine("\nTo restore original: run with --restore flag");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERROR] Failed to patch: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}
