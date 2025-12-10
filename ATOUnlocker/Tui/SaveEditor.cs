using Spectre.Console;
using SystemConsole = System.Console;

namespace ATOUnlocker.Tui;

public class SaveEditor
{
    private readonly string _atoPath;
    private readonly string _runsPath;
    private PlayerData _playerData;
    private List<PlayerRun> _runs;
    private bool _hasChanges;
    private bool _hasRunsChanges;

    public SaveEditor(string atoPath)
    {
        _atoPath = atoPath;
        _runsPath = Path.Combine(Path.GetDirectoryName(atoPath)!, "runs.ato");
        _playerData = SaveManager.LoadPlayerData(atoPath);
        _runs = SaveManager.LoadRuns(_runsPath);
        _hasChanges = false;
        _hasRunsChanges = false;
    }

    public void Run()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("ATO Editor").Color(Color.Gold1));
        AnsiConsole.MarkupLine($"[grey]Save: {_atoPath}[/]\n");

        while (true)
        {
            var hasAnyChanges = _hasChanges || _hasRunsChanges;
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Main Menu[/]")
                    .PageSize(12)
                    .AddChoices(new[]
                    {
                        "Heroes",
                        "Town Upgrades",
                        "Cards",
                        "Currencies & Resources",
                        "Progression",
                        "Madness Levels",
                        $"Reward Chests ({_runs.Count})",
                        "Unlock All",
                        hasAnyChanges ? "[green]Save & Exit[/]" : "Exit"
                    }));

            if (choice.StartsWith("Reward Chests"))
            {
                RewardChestMenu();
                continue;
            }

            switch (choice)
            {
                case "Heroes":
                    HeroesMenu();
                    break;
                case "Town Upgrades":
                    TownMenu();
                    break;
                case "Cards":
                    CardsMenu();
                    break;
                case "Currencies & Resources":
                    ResourcesMenu();
                    break;
                case "Progression":
                    ProgressionMenu();
                    break;
                case "Madness Levels":
                    MadnessMenu();
                    break;
                case "Unlock All":
                    UnlockAll();
                    break;
                default:
                    if (hasAnyChanges)
                    {
                        if (AnsiConsole.Confirm("Save changes before exiting?"))
                        {
                            SaveChanges();
                        }
                    }
                    return;
            }
        }
    }

    private void HeroesMenu()
    {
        var currentlyUnlocked = _playerData.UnlockedHeroes ?? new List<string>();

        var prompt = new MultiSelectionPrompt<string>()
            .Title("[yellow]Select heroes to unlock[/]")
            .PageSize(20)
            .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
            .UseConverter(heroId => Reference.GetHeroDisplayName(heroId))
            .AddChoices(Reference.Heroes);

        // Pre-select currently unlocked heroes
        foreach (var hero in currentlyUnlocked)
        {
            if (Reference.Heroes.Contains(hero))
            {
                prompt.Select(hero);
            }
        }

        var selected = AnsiConsole.Prompt(prompt);

        _playerData.UnlockedHeroes = selected.ToList();
        _hasChanges = true;
        AnsiConsole.MarkupLine($"[green]Selected {selected.Count} heroes[/]");
        Thread.Sleep(500);
    }

    private void TownMenu()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Town Upgrades[/]")
                .AddChoices(new[]
                {
                    "Unlock by Tier (1-6)",
                    "Unlock All Town Upgrades",
                    "Back"
                }));

        if (choice == "Back") return;

        if (choice == "Unlock All Town Upgrades")
        {
            _playerData.SupplyBought = GenerateTownUpgrades(6);
            _hasChanges = true;
            AnsiConsole.MarkupLine("[green]All town upgrades unlocked![/]");
        }
        else
        {
            var tier = AnsiConsole.Prompt(
                new TextPrompt<int>("[yellow]Enter tier level (1-6):[/]")
                    .DefaultValue(3)
                    .Validate(t => t >= 1 && t <= 6 ? ValidationResult.Success() : ValidationResult.Error("Must be 1-6")));

            _playerData.SupplyBought = GenerateTownUpgrades(tier);
            _hasChanges = true;
            AnsiConsole.MarkupLine($"[green]Town upgrades unlocked up to tier {tier}![/]");
        }
        Thread.Sleep(500);
    }

    private void CardsMenu()
    {
        var currentCards = _playerData.UnlockedCards ?? new List<string>();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[yellow]Cards Menu[/] [grey](Currently {currentCards.Count} cards unlocked)[/]")
                .AddChoices(new[]
                {
                    "View Current Cards",
                    "Unlock All Cards (from Reference)",
                    "Back"
                }));

        if (choice == "Back") return;

        if (choice == "View Current Cards")
        {
            if (currentCards.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey]No cards unlocked yet[/]");
            }
            else
            {
                var table = new Table();
                table.AddColumn("Unlocked Cards");
                foreach (var card in currentCards.Take(50))
                {
                    table.AddRow(card);
                }
                if (currentCards.Count > 50)
                {
                    table.AddRow($"[grey]... and {currentCards.Count - 50} more[/]");
                }
                AnsiConsole.Write(table);
            }
            AnsiConsole.Markup("[grey]Press any key to continue...[/]");
            SystemConsole.ReadKey(true);
        }
        else if (choice == "Unlock All Cards (from Reference)")
        {
            // Add all cards from reference that aren't already unlocked
            var allCards = new HashSet<string>(currentCards);
            foreach (var card in Reference.Cards)
            {
                allCards.Add(card);
            }
            _playerData.UnlockedCards = allCards.ToList();
            _hasChanges = true;
            AnsiConsole.MarkupLine($"[green]Cards updated! Now have {_playerData.UnlockedCards.Count} cards[/]");
            Thread.Sleep(500);
        }
    }

    private void ResourcesMenu()
    {
        while (true)
        {
            var currentSupply = _playerData.SupplyActual;
            var currentGold = _playerData.GoldGained;
            var currentDust = _playerData.DustGained;
            var currentPerkPoints = _playerData.PlayerRankProgress;

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Currencies & Resources[/]")
                    .AddChoices(new[]
                    {
                        $"Set Supply (Current: {currentSupply})",
                        $"Set Gold Gained [Stats] (Current: {currentGold})",
                        $"Set Shards Gained [Stats] (Current: {currentDust})",
                        $"Set Perk Points (Current: {currentPerkPoints})",
                        "Set Hero Progress",
                        "Back"
                    }));

            if (choice.StartsWith("Set Supply"))
            {
                var value = AnsiConsole.Prompt(
                    new TextPrompt<int>("[yellow]Enter supply amount:[/]")
                        .DefaultValue(currentSupply));
                _playerData.SupplyActual = value;
                _hasChanges = true;
                AnsiConsole.MarkupLine($"[green]Supply set to {value}[/]");
            }
            else if (choice.StartsWith("Set Gold Gained"))
            {
                var value = AnsiConsole.Prompt(
                    new TextPrompt<int>("[yellow]Enter gold amount:[/]")
                        .DefaultValue(currentGold)
                        .Validate(v => v >= 0 ? ValidationResult.Success() : ValidationResult.Error("Must be >= 0")));
                _playerData.GoldGained = value;
                _hasChanges = true;
                AnsiConsole.MarkupLine($"[green]Gold set to {value}[/]");
            }
            else if (choice.StartsWith("Set Shards Gained"))
            {
                var value = AnsiConsole.Prompt(
                    new TextPrompt<int>("[yellow]Enter shards/dust amount:[/]")
                        .DefaultValue(currentDust)
                        .Validate(v => v >= 0 ? ValidationResult.Success() : ValidationResult.Error("Must be >= 0")));
                _playerData.DustGained = value;
                _hasChanges = true;
                AnsiConsole.MarkupLine($"[green]Shards/Dust set to {value}[/]");
            }
            else if (choice.StartsWith("Set Perk Points"))
            {
                var value = AnsiConsole.Prompt(
                    new TextPrompt<int>("[yellow]Enter perk points:[/]")
                        .DefaultValue(currentPerkPoints)
                        .Validate(v => v >= 0 ? ValidationResult.Success() : ValidationResult.Error("Must be >= 0")));
                _playerData.PlayerRankProgress = value;
                _hasChanges = true;
                AnsiConsole.MarkupLine($"[green]Perk points set to {value}[/]");
            }
            else if (choice == "Set Hero Progress")
            {
                HeroProgressMenu();
            }
            else
            {
                return;
            }
            Thread.Sleep(300);
        }
    }

    private void HeroProgressMenu()
    {
        var heroes = _playerData.UnlockedHeroes ?? new List<string>();
        if (heroes.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No heroes unlocked. Unlock heroes first.[/]");
            Thread.Sleep(1000);
            return;
        }

        var heroProgress = _playerData.HeroProgress ?? new Dictionary<string, int>();

        var selectedHero = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Select hero to set progress[/]")
                .UseConverter(h => h == "Set All to Max" || h == "Back" ? h : Reference.GetHeroDisplayName(h))
                .AddChoices(heroes.Append("Set All to Max").Append("Back")));

        if (selectedHero == "Back") return;

        if (selectedHero == "Set All to Max")
        {
            foreach (var hero in heroes)
            {
                heroProgress[hero] = 1000;
            }
            _playerData.HeroProgress = heroProgress;
            _hasChanges = true;
            AnsiConsole.MarkupLine("[green]All hero progress set to max![/]");
        }
        else
        {
            var currentProgress = heroProgress.GetValueOrDefault(selectedHero, 0);
            var displayName = Reference.GetHeroDisplayName(selectedHero);
            var value = AnsiConsole.Prompt(
                new TextPrompt<int>($"[yellow]Enter progress for {displayName} (current: {currentProgress}):[/]")
                    .DefaultValue(1000));
            heroProgress[selectedHero] = value;
            _playerData.HeroProgress = heroProgress;
            _hasChanges = true;
            AnsiConsole.MarkupLine($"[green]{displayName} progress set to {value}[/]");
        }
        Thread.Sleep(500);
    }

    private void ProgressionMenu()
    {
        while (true)
        {
            var ngUnlocked = _playerData.NgUnlocked;
            var ngLevel = _playerData.NgLevel;

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Progression[/]")
                    .AddChoices(new[]
                    {
                        $"Toggle NG+ (Currently: {(ngUnlocked ? "Unlocked" : "Locked")})",
                        $"Set NG+ Level (Current: {ngLevel})",
                        "Back"
                    }));

            if (choice.StartsWith("Toggle NG+"))
            {
                _playerData.NgUnlocked = !ngUnlocked;
                _hasChanges = true;
                AnsiConsole.MarkupLine($"[green]NG+ is now {(_playerData.NgUnlocked ? "Unlocked" : "Locked")}[/]");
            }
            else if (choice.StartsWith("Set NG+ Level"))
            {
                var value = AnsiConsole.Prompt(
                    new TextPrompt<int>("[yellow]Enter NG+ level:[/]")
                        .DefaultValue(ngLevel)
                        .Validate(v => v >= 0 ? ValidationResult.Success() : ValidationResult.Error("Must be >= 0")));
                _playerData.NgLevel = value;
                _hasChanges = true;
                AnsiConsole.MarkupLine($"[green]NG+ level set to {value}[/]");
            }
            else
            {
                return;
            }
            Thread.Sleep(300);
        }
    }

    private void MadnessMenu()
    {
        while (true)
        {
            var obelisk = _playerData.ObeliskMadnessLevel;
            var adventure = _playerData.MaxAdventureMadnessLevel;
            var singularity = _playerData.SingularityMadnessLevel;

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Madness Levels[/]")
                    .AddChoices(new[]
                    {
                        $"Set Obelisk Madness (Current: {obelisk})",
                        $"Set Adventure Madness (Current: {adventure})",
                        $"Set Singularity Madness (Current: {singularity})",
                        "Max All Madness Levels",
                        "Back"
                    }));

            if (choice.StartsWith("Set Obelisk"))
            {
                var value = AnsiConsole.Prompt(
                    new TextPrompt<int>("[yellow]Enter Obelisk madness level:[/]")
                        .DefaultValue(obelisk));
                _playerData.ObeliskMadnessLevel = value;
                _hasChanges = true;
            }
            else if (choice.StartsWith("Set Adventure"))
            {
                var value = AnsiConsole.Prompt(
                    new TextPrompt<int>("[yellow]Enter Adventure madness level:[/]")
                        .DefaultValue(adventure));
                _playerData.MaxAdventureMadnessLevel = value;
                _hasChanges = true;
            }
            else if (choice.StartsWith("Set Singularity"))
            {
                var value = AnsiConsole.Prompt(
                    new TextPrompt<int>("[yellow]Enter Singularity madness level:[/]")
                        .DefaultValue(singularity));
                _playerData.SingularityMadnessLevel = value;
                _hasChanges = true;
            }
            else if (choice == "Max All Madness Levels")
            {
                _playerData.ObeliskMadnessLevel = 20;
                _playerData.MaxAdventureMadnessLevel = 20;
                _playerData.SingularityMadnessLevel = 20;
                _hasChanges = true;
                AnsiConsole.MarkupLine("[green]All madness levels set to 20![/]");
            }
            else
            {
                return;
            }
            Thread.Sleep(300);
        }
    }

    private void RewardChestMenu()
    {
        while (true)
        {
            if (_runs.Count == 0)
            {
                var createChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[yellow]Reward Chests[/] [grey](No reward chests found)[/]")
                        .AddChoices(new[] { "Create New Reward Chest", "Back" }));

                if (createChoice == "Back") return;

                CreateNewRewardChest();
                continue;
            }

            // Build choice list with run details
            var choices = new List<string>();
            for (int i = 0; i < _runs.Count; i++)
            {
                var run = _runs[i];
                var heroes = string.Join(", ", new[] { run.Char0, run.Char1, run.Char2, run.Char3 }
                    .Where(h => !string.IsNullOrEmpty(h))
                    .Select(h => Reference.GetHeroDisplayName(h).Split('(')[0].Trim()));
                // Escape brackets for Spectre.Console markup
                choices.Add($"[[{i}]] Gold: {run.GoldGained}, Shards: {run.DustGained} ({heroes})");
            }
            choices.Add("Create New Reward Chest");
            choices.Add("Back");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Reward Chests[/]")
                    .PageSize(15)
                    .AddChoices(choices));

            if (choice == "Back") return;
            if (choice == "Create New Reward Chest")
            {
                CreateNewRewardChest();
                continue;
            }

            // Parse index from choice (format: [[0]] Gold: ...)
            var indexStr = choice.Split(']')[0].TrimStart('[');
            if (int.TryParse(indexStr, out int index) && index >= 0 && index < _runs.Count)
            {
                EditRewardChest(index);
            }
        }
    }

    private void CreateNewRewardChest()
    {
        var gold = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Enter gold amount:[/]")
                .DefaultValue(10000)
                .Validate(v => v >= 0 ? ValidationResult.Success() : ValidationResult.Error("Must be >= 0")));

        var dust = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Enter shards/dust amount:[/]")
                .DefaultValue(1000)
                .Validate(v => v >= 0 ? ValidationResult.Success() : ValidationResult.Error("Must be >= 0")));

        var newRun = new PlayerRun
        {
            Id = Guid.NewGuid().ToString(),
            GoldGained = gold,
            DustGained = dust,
            TotalGoldGained = gold,
            TotalDustGained = dust,
            Version = "1.0",
            gameDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Char0 = "archer",  // Default hero
        };

        _runs.Add(newRun);
        _hasRunsChanges = true;
        AnsiConsole.MarkupLine($"[green]Created reward chest with {gold} gold and {dust} shards![/]");
        Thread.Sleep(500);
    }

    private void EditRewardChest(int index)
    {
        var run = _runs[index];

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[yellow]Edit Reward Chest {index}[/]")
                    .AddChoices(new[]
                    {
                        $"Set Gold (Current: {run.GoldGained})",
                        $"Set Shards/Dust (Current: {run.DustGained})",
                        "Delete This Chest",
                        "Back"
                    }));

            if (choice == "Back") return;

            if (choice.StartsWith("Set Gold"))
            {
                var value = AnsiConsole.Prompt(
                    new TextPrompt<int>("[yellow]Enter gold amount:[/]")
                        .DefaultValue(run.GoldGained)
                        .Validate(v => v >= 0 ? ValidationResult.Success() : ValidationResult.Error("Must be >= 0")));
                run.GoldGained = value;
                run.TotalGoldGained = Math.Max(run.TotalGoldGained, value);
                _hasRunsChanges = true;
                AnsiConsole.MarkupLine($"[green]Gold set to {value}[/]");
            }
            else if (choice.StartsWith("Set Shards"))
            {
                var value = AnsiConsole.Prompt(
                    new TextPrompt<int>("[yellow]Enter shards/dust amount:[/]")
                        .DefaultValue(run.DustGained)
                        .Validate(v => v >= 0 ? ValidationResult.Success() : ValidationResult.Error("Must be >= 0")));
                run.DustGained = value;
                run.TotalDustGained = Math.Max(run.TotalDustGained, value);
                _hasRunsChanges = true;
                AnsiConsole.MarkupLine($"[green]Shards/Dust set to {value}[/]");
            }
            else if (choice == "Delete This Chest")
            {
                if (AnsiConsole.Confirm("[red]Delete this reward chest?[/]"))
                {
                    _runs.RemoveAt(index);
                    _hasRunsChanges = true;
                    AnsiConsole.MarkupLine("[green]Reward chest deleted![/]");
                    return;
                }
            }
            Thread.Sleep(300);
        }
    }

    private void UnlockAll()
    {
        if (!AnsiConsole.Confirm("[yellow]This will max out everything. Continue?[/]"))
            return;

        // Unlock all heroes
        _playerData.UnlockedHeroes = Reference.Heroes.ToList();

        // Max hero progress
        var heroProgress = new Dictionary<string, int>();
        foreach (var hero in Reference.Heroes)
        {
            heroProgress[hero] = 1000;
        }
        _playerData.HeroProgress = heroProgress;

        // Max perk points
        _playerData.PlayerRankProgress = 100000;

        // All town upgrades
        _playerData.SupplyBought = GenerateTownUpgrades(6);

        // Unlock NG+
        _playerData.NgUnlocked = true;
        _playerData.NgLevel = 20;

        // Max madness
        _playerData.ObeliskMadnessLevel = 20;
        _playerData.MaxAdventureMadnessLevel = 20;
        _playerData.SingularityMadnessLevel = 20;

        // Max supply
        _playerData.SupplyActual = 99999;

        _hasChanges = true;
        AnsiConsole.MarkupLine("[green]Everything unlocked and maxed![/]");
        Thread.Sleep(1000);
    }

    private List<string> GenerateTownUpgrades(int maxTier)
    {
        var upgrades = new List<string>();
        for (int x = 1; x <= maxTier; x++)
        {
            for (int y = 1; y <= 6; y++)
            {
                upgrades.Add($"townUpgrade_{x}_{y}");
            }
        }
        return upgrades;
    }

    private void SaveChanges()
    {
        try
        {
            if (_hasChanges)
            {
                SaveManager.SavePlayerData(_atoPath, _playerData);
                _hasChanges = false;
            }
            if (_hasRunsChanges)
            {
                SaveManager.SaveRuns(_runsPath, _runs);
                _hasRunsChanges = false;
            }
            AnsiConsole.MarkupLine("[green]Save successful![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error saving: {ex.Message}[/]");
        }
        Thread.Sleep(1000);
    }
}
