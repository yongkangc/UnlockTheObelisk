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