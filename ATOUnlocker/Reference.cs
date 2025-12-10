namespace ATOUnlocker;

public static class Reference
{
    public static IReadOnlyList<string> Heroes = new List<string>
    {
        "archer",
        "assassin",
        "berserker",
        "cleric",
        "elementalist",
        "loremaster",
        "mercenary",
        "minstrel",
        "priest",
        "prophet",
        "pyromancer",
        "ranger",
        "sentinel",
        "voodoowitch",
        "warden",
        "warlock"
    };

    // Hero ID to display name mapping (subclass → character name)
    public static readonly Dictionary<string, string> HeroNames = new()
    {
        // Base Game - Scouts
        {"archer", "Andrin"},
        {"assassin", "Thuls"},
        {"mercenary", "Sylvie"},
        {"minstrel", "Gustav"},

        // Base Game - Warriors
        {"berserker", "Magnus"},
        {"ranger", "Heiner"},
        {"prophet", "Grukli"},
        {"warden", "Bree"},
        {"sentinel", "Yogger"},

        // Base Game - Mages
        {"elementalist", "Evelyn"},
        {"pyromancer", "Cornelius"},
        {"loremaster", "Wilbur"},
        {"warlock", "Zek"},

        // Base Game - Healers
        {"cleric", "Reginald"},
        {"priest", "Ottis"},
        {"voodoowitch", "Malukah"},
        {"seer", "Nezglekt"},

        // DLC Heroes
        {"engineer", "Nenukil"},
        {"queen", "Amelia"},
        {"shaman", "Tulah"},
        {"alchemist", "Bernard"},
        {"valkyrie", "Sigrun"},
        {"bloodmage", "Velarion"},
        {"deathknight", "Nevermoor"},
        // Ulminin DLC
        {"diviner", "Navalea"},
        {"fallen", "Laia"},
    };

    /// <summary>
    /// Gets a display name for a hero ID, e.g., "Andrin (archer)"
    /// Falls back to just the ID if not in the mapping
    /// </summary>
    public static string GetHeroDisplayName(string heroId) =>
        HeroNames.TryGetValue(heroId, out var name) ? $"{name} ({heroId})" : heroId;

    // Known pet card IDs (pets are cards with CardType = Pet)
    public static IReadOnlyList<string> Pets = new List<string>
    {
        // Base Game Pets
        "asmody",
        "batsy",
        "betty",
        "bunny",
        "champy",
        "chompy",
        "chumpy",
        "cubydark",
        "cubyholy",
        "daley",
        "fenny",
        "flamy",
        "lianta",
        "matey",
        "mimy",
        "mozzy",
        "oculy",
        "orby",
        "rocky",
        "sharpy",
        "slimy",
        "stormy",
        // DLC Pets
        "floaty",   // Nenukil
        "inky",     // Bernard
        "jelly",    // Sigrun
        "wolfy",    // Wolf Wars DLC
        "lychee",   // Necropolis DLC
    };

    // Cards list - this is a subset, cards are dynamically loaded from save
    public static IReadOnlyList<string> Cards = new List<string>
    {
        // Cards are read from the player's save file
        // This list can be extended with known card IDs if needed
    };

    public const int MaxMadnessLevel = 20;
    public const int MaxHeroProgress = 1000;
    public const int MaxPerkPoints = 100000;
}