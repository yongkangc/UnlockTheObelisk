using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using ATOUnlocker.Tui;
using Spectre.Console;

namespace ATOUnlocker;

using Console = System.Console;

public static class Program
{
    private static string AtoPath { get; set; } = "";

    public static void Main(string[] args)
    {
        Console.Title = "Unlock The Obelisk";

        if (args.Length == 0)
        {
            ShowUsage();
            return;
        }

        if (args[0] == "help" || args[0] == "--help" || args[0] == "-h")
        {
            ShowHelp();
            return;
        }

        // Load game assemblies
        try
        {
            Assembly.Load("Assembly-CSharp");
        }
        catch (Exception)
        {
            AnsiConsole.MarkupLine(
                "[red]Failed to load game assemblies. Make sure you update the csproj before building and change the gamePath to point at the `Managed` directory of your ATO installation.[/]");
            return;
        }

        AtoPath = args[0];

        if (!File.Exists(AtoPath))
        {
            AnsiConsole.MarkupLine($"[red]Couldn't find the file at path: {AtoPath}[/]");
            AnsiConsole.MarkupLine("[yellow]Make sure the path exists. You should be pointing at the player.ato file.[/]");
            return;
        }

        // Determine mode: TUI (default) or CLI (if additional args provided)
        var additionalArgs = args.Skip(1).ToArray();

        // Check for debug-runs flag
        if (additionalArgs.Contains("debug-runs"))
        {
            DebugRuns();
            return;
        }

        // Check for test-create-run flag
        if (additionalArgs.Contains("test-create-run"))
        {
            TestCreateRun();
            return;
        }

        // Check for debug-gamedata flag
        if (additionalArgs.Contains("debug-gamedata"))
        {
            DebugGameData();
            return;
        }

        // Check for CLI mode flags
        bool cliMode = additionalArgs.Any(a => a == "perks" || a == "heroes" || a == "town" || a == "--cli");

        if (cliMode)
        {
            RunCliMode(additionalArgs);
        }
        else
        {
            RunTuiMode();
        }
    }

    private static void RunTuiMode()
    {
        try
        {
            var editor = new SaveEditor(AtoPath);
            editor.Run();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }
    }

    private static void DebugRuns()
    {
        var runsPath = Path.Combine(Path.GetDirectoryName(AtoPath)!, "runs.ato");
        AnsiConsole.MarkupLine($"[grey]Runs file: {runsPath}[/]");

        // List Run-related types in game assembly
        try
        {
            var asm = Assembly.Load("Assembly-CSharp");
            AnsiConsole.MarkupLine($"[grey]Game assembly types containing 'Run':[/]");
            foreach (var t in asm.GetTypes())
            {
                if (t.Name.ToLower().Contains("run") && !t.Name.Contains("Runtime") && !t.IsInterface)
                {
                    AnsiConsole.MarkupLine($"[grey]  {t.FullName}[/]");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error listing types: {ex.Message}[/]");
        }

        if (!File.Exists(runsPath))
        {
            AnsiConsole.MarkupLine("[yellow]runs.ato not found[/]");
            return;
        }

        try
        {
            var runs = ATOUnlocker.Tui.SaveManager.LoadRuns(runsPath);
            AnsiConsole.MarkupLine($"[green]Found {runs.Count} runs:[/]");
            for (int i = 0; i < runs.Count; i++)
            {
                var r = runs[i];
                var heroes = string.Join(", ", new[] { r.Char0, r.Char1, r.Char2, r.Char3 }
                    .Where(h => !string.IsNullOrEmpty(h)));
                AnsiConsole.MarkupLine($"  [[{i}]] Gold: {r.GoldGained}, Dust: {r.DustGained}, Heroes: {heroes}");
                AnsiConsole.MarkupLine($"       Id: {r.Id}, Date: {r.gameDate}");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error loading runs: {ex.Message}[/]");
            AnsiConsole.MarkupLine($"[grey]{ex.GetType().Name}[/]");
        }
    }

    private static void DebugGameData()
    {
        var saveDir = Path.GetDirectoryName(AtoPath)!;

        // List game data related types
        try
        {
            var asm = Assembly.Load("Assembly-CSharp");
            AnsiConsole.MarkupLine($"[yellow]Types containing 'Game' or 'Save' or 'Data':[/]");
            foreach (var t in asm.GetTypes())
            {
                var name = t.Name.ToLower();
                if ((name.Contains("game") || name.Contains("save") || name.Contains("data"))
                    && !name.Contains("runtime") && !t.IsInterface && t.IsSerializable)
                {
                    AnsiConsole.MarkupLine($"[grey]  {t.FullName}[/]");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error listing types: {ex.Message}[/]");
        }

        // Try to read gamedata files
        var gameDataFiles = Directory.GetFiles(saveDir, "gamedata_*.ato");
        AnsiConsole.MarkupLine($"\n[yellow]Found {gameDataFiles.Length} gamedata files:[/]");

        foreach (var file in gameDataFiles)
        {
            AnsiConsole.MarkupLine($"[grey]  {Path.GetFileName(file)} ({new FileInfo(file).Length} bytes)[/]");

            // Try to deserialize and identify the type
            try
            {
                using var fs = new FileStream(file, FileMode.Open);
                var des = new DESCryptoServiceProvider();
                #pragma warning disable SYSLIB0021
                var cs = new CryptoStream(fs, des.CreateDecryptor(Cryptography.Key, Cryptography.IV), CryptoStreamMode.Read);
                #pragma warning restore SYSLIB0021

                var bf = new BinaryFormatter();
                #pragma warning disable SYSLIB0011
                var obj = bf.Deserialize(cs);
                #pragma warning restore SYSLIB0011

                AnsiConsole.MarkupLine($"[green]    Type: {obj.GetType().FullName}[/]");

                // Try to find gold/shards properties
                var type = obj.GetType();
                var goldField = type.GetField("gold") ?? type.GetField("Gold") ?? type.GetField("currentGold");
                var dustField = type.GetField("dust") ?? type.GetField("Dust") ?? type.GetField("shards");

                if (goldField != null)
                    AnsiConsole.MarkupLine($"[green]    Gold: {goldField.GetValue(obj)}[/]");
                if (dustField != null)
                    AnsiConsole.MarkupLine($"[green]    Dust: {dustField.GetValue(obj)}[/]");

                // List ALL fields including private
                AnsiConsole.MarkupLine($"[grey]    All fields (including private):[/]");
                var allFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                AnsiConsole.MarkupLine($"[grey]    Found {allFields.Length} fields[/]");
                foreach (var field in allFields)
                {
                    try
                    {
                        var val = field.GetValue(obj);
                        var valStr = val?.ToString() ?? "null";
                        if (valStr.Length > 50) valStr = valStr.Substring(0, 47) + "...";
                        AnsiConsole.MarkupLine($"[grey]      {field.Name} ({field.FieldType.Name}) = {valStr}[/]");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]    Error: {ex.Message}[/]");
            }
        }
    }

    private static void TestCreateRun()
    {
        var runsPath = Path.Combine(Path.GetDirectoryName(AtoPath)!, "runs.ato");
        AnsiConsole.MarkupLine($"[grey]Creating test run in: {runsPath}[/]");

        try
        {
            var runs = ATOUnlocker.Tui.SaveManager.LoadRuns(runsPath);
            AnsiConsole.MarkupLine($"[grey]Existing runs: {runs.Count}[/]");

            var newRun = new PlayerRun
            {
                Id = Guid.NewGuid().ToString(),
                GoldGained = 50000,
                DustGained = 10000,
                TotalGoldGained = 50000,
                TotalDustGained = 10000,
                Version = "1.0",
                gameDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Char0 = "archer",
            };

            runs.Add(newRun);
            ATOUnlocker.Tui.SaveManager.SaveRuns(runsPath, runs);
            AnsiConsole.MarkupLine($"[green]Created test run with 50000 gold and 10000 shards![/]");
            AnsiConsole.MarkupLine($"[grey]Total runs now: {runs.Count}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            AnsiConsole.MarkupLine($"[grey]{ex}[/]");
        }
    }

    private static void RunCliMode(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;

        if (args.Length == 0 || (args.Length == 1 && args[0] == "--cli"))
        {
            Console.WriteLine("No unlock arguments provided. Use: perks, heroes, town");
            return;
        }

        if (args.Contains("perks"))
        {
            Console.WriteLine("Maxing out all perk points.");
            MaxAllHeroesPerks();
        }

        if (args.Contains("heroes"))
        {
            Console.WriteLine("Unlocking all heroes.");
            AddAllHeroes();
        }

        if (args.Contains("town"))
        {
            Console.WriteLine("Maxing out all town upgrades.");
            UpgradeTown();
        }

        Console.ResetColor();
    }

    private static void ShowUsage()
    {
        AnsiConsole.MarkupLine("[yellow]Usage:[/]");
        AnsiConsole.MarkupLine("  ATOUnlocker [grey]<path-to-player.ato>[/]              [green]# Launch TUI editor[/]");
        AnsiConsole.MarkupLine("  ATOUnlocker [grey]<path-to-player.ato>[/] [args]       [green]# CLI mode[/]");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[yellow]Example:[/]");
        AnsiConsole.MarkupLine("  ATOUnlocker \"$HOME/Library/Application Support/Dreamsite Games/AcrossTheObelisk/STEAM_ID/player.ato\"");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("Use [green]--help[/] for more information.");
    }

    private static void ShowHelp()
    {
        AnsiConsole.Write(new FigletText("ATO Editor").Color(Color.Gold1));
        AnsiConsole.MarkupLine("[grey]Across the Obelisk Save Editor[/]\n");

        AnsiConsole.MarkupLine("[yellow]Usage:[/]");
        AnsiConsole.MarkupLine("  ATOUnlocker [grey]<path>[/]                    Launch interactive TUI editor");
        AnsiConsole.MarkupLine("  ATOUnlocker [grey]<path>[/] [blue]perks[/]            Max out all perk points");
        AnsiConsole.MarkupLine("  ATOUnlocker [grey]<path>[/] [blue]heroes[/]           Unlock all heroes");
        AnsiConsole.MarkupLine("  ATOUnlocker [grey]<path>[/] [blue]town[/]             Unlock all town upgrades");
        AnsiConsole.MarkupLine("  ATOUnlocker [grey]<path>[/] [blue]town perks heroes[/] Unlock everything (CLI)");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[yellow]Save file locations:[/]");
        AnsiConsole.MarkupLine("  [green]Mac:[/]     ~/Library/Application Support/Dreamsite Games/AcrossTheObelisk/STEAM_ID/player.ato");
        AnsiConsole.MarkupLine("  [green]Windows:[/] C:\\Users\\NAME\\AppData\\LocalLow\\Dreamsite Games\\AcrossTheObelisk\\STEAM_ID\\player.ato");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[red]WARNING:[/] Back up your save file before making changes!");
    }

    private static void AddAllHeroes()
    {
        PlayerData test;
        test = LoadPlayerData();
        foreach (string subClass in Reference.Heroes.Where(subClass => !test.UnlockedHeroes.Contains(subClass)))
        {
            test.UnlockedHeroes.Add(subClass);
        }

        SavePlayerData(test);
    }

    private static void MaxAllHeroesPerks()
    {
        PlayerData test;

        test = LoadPlayerData();
        test.PlayerRankProgress = 100_000;
        foreach (string subClass in Reference.Heroes.Where(subClass => test.UnlockedHeroes.Contains(subClass)))
        {
            test.HeroProgress[subClass] = 1_000;
        }

        SavePlayerData(test);
    }

    private static void UpgradeTown()
    {
        PlayerData test;

        test = LoadPlayerData();
        test.SupplyBought = new List<string>();

        for (int x = 1; x < 7; x++)
        {
            for (int y = 1; y < 7; y++)
            {
                test.SupplyBought.Add($"townUpgrade_{x}_{y}");
            }
        }

        SavePlayerData(test);
    }

    private static void SavePlayerData(PlayerData playerData)
    {
        DESCryptoServiceProvider cryptoServiceProvider = new();
        using (FileStream fileStream = new(AtoPath, FileMode.Create, FileAccess.Write))
        {
            using (CryptoStream cryptoStream =
                   new(fileStream, cryptoServiceProvider.CreateEncryptor(Cryptography.Key, Cryptography.IV), CryptoStreamMode.Write))
            {
                #pragma warning disable SYSLIB0011
                new BinaryFormatter().Serialize(cryptoStream, playerData);
                #pragma warning restore SYSLIB0011
                cryptoStream.Close();
            }

            fileStream.Close();
        }
    }

    private static PlayerData LoadPlayerData()
    {
        if (File.Exists(AtoPath))
        {
            using (FileStream fileStream = new(AtoPath, FileMode.Open))
            {
                if (fileStream.Length == 0L)
                {
                    fileStream.Close();
                }
                else
                {
                    DESCryptoServiceProvider cryptoServiceProvider = new();
                    PlayerData playerData2;
                    try
                    {
                        CryptoStream cryptoStream = new(fileStream,
                            cryptoServiceProvider.CreateDecryptor(Cryptography.Key, Cryptography.IV), CryptoStreamMode.Read);
                        BinaryFormatter binaryFormatter = new();
                        try
                        {
                            #pragma warning disable SYSLIB0011
                            playerData2 = binaryFormatter.Deserialize(cryptoStream) as PlayerData;
                            #pragma warning restore SYSLIB0011
                        }
                        catch (Exception ex)
                        {
                            fileStream.Close();
                            Console.WriteLine(ex.Message);
                            return null!;
                        }
                    }
                    catch (SerializationException)
                    {
                        fileStream.Close();
                        return null!;
                    }
                    catch (DecoderFallbackException)
                    {
                        fileStream.Close();
                        return null!;
                    }

                    fileStream.Close();
                    return playerData2!;
                }
            }
        }

        throw new FileNotFoundException();
    }
}
