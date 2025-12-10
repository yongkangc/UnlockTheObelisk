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
