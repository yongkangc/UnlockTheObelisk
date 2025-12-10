using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace ATOUnlocker.Tui;

/// <summary>
/// Represents a completed game run with reward chest data.
/// Must match the game's PlayerRun class for BinaryFormatter compatibility.
/// </summary>
[Serializable]
public class PlayerRun
{
    public string Id = "";
    public string Version = "";
    public string GameUniqueId = "";
    public string GameSeed = "";
    public string gameDate = "";
    public bool Singularity;
    public bool ObeliskChallenge;
    public bool WeeklyChallenge;
    public int WeekChallenge;
    public float PlayedTime;
    public int FinalScore;
    public int ActTier;
    public int TotalPlayers;
    public string PlaceOfDeath = "";
    public int PlacesVisited;
    public int ExperienceGained;
    public int TotalGoldGained;
    public int GoldGained;        // Reward chest gold
    public int TotalDustGained;
    public int DustGained;        // Reward chest shards
    public int BossesKilled;
    public int MonstersKilled;
    public int[]? CombatStats0;
    public int[]? CombatStats1;
    public int[]? CombatStats2;
    public int[]? CombatStats3;
    public List<string> VisitedNodes = new();
    public List<string> VisitedNodesAction = new();
    public List<string> BossesKilledName = new();
    public bool SandboxEnabled;
    public string SandboxConfig = "";
    public int MadnessLevel;
    public string MadnessCorruptors = "";
    public int ObeliskMadness;
    public int SingularityMadness;
    public int CommonCorruptions;
    public int UncommonCorruptions;
    public int RareCorruptions;
    public int EpicCorruptions;
    public int TotalDeaths;
    public List<string> UnlockedCards = new();
    public string Char0 = "";
    public string Char0Skin = "";
    public int Char0Rank;
    public string Char0Owner = "";
    public List<string> Char0Cards = new();
    public List<string> Char0Items = new();
    public List<string> Char0Traits = new();
    public string Char1 = "";
    public string Char1Skin = "";
    public int Char1Rank;
    public string Char1Owner = "";
    public List<string> Char1Cards = new();
    public List<string> Char1Items = new();
    public List<string> Char1Traits = new();
    public string Char2 = "";
    public string Char2Skin = "";
    public int Char2Rank;
    public string Char2Owner = "";
    public List<string> Char2Cards = new();
    public List<string> Char2Items = new();
    public List<string> Char2Traits = new();
    public string Char3 = "";
    public string Char3Skin = "";
    public int Char3Rank;
    public string Char3Owner = "";
    public List<string> Char3Cards = new();
    public List<string> Char3Items = new();
    public List<string> Char3Traits = new();
}

public static class SaveManager
{
    public static PlayerData LoadPlayerData(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Save file not found: {path}");
        }

        using var fileStream = new FileStream(path, FileMode.Open);
        if (fileStream.Length == 0L)
        {
            throw new InvalidDataException("Save file is empty");
        }

        var cryptoServiceProvider = new DESCryptoServiceProvider();
        var cryptoStream = new CryptoStream(
            fileStream,
            cryptoServiceProvider.CreateDecryptor(Cryptography.Key, Cryptography.IV),
            CryptoStreamMode.Read);

        var binaryFormatter = new BinaryFormatter();
        #pragma warning disable SYSLIB0011
        var playerData = binaryFormatter.Deserialize(cryptoStream) as PlayerData;
        #pragma warning restore SYSLIB0011

        return playerData ?? throw new InvalidDataException("Failed to deserialize player data");
    }

    public static void SavePlayerData(string path, PlayerData playerData)
    {
        var cryptoServiceProvider = new DESCryptoServiceProvider();
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var cryptoStream = new CryptoStream(
            fileStream,
            cryptoServiceProvider.CreateEncryptor(Cryptography.Key, Cryptography.IV),
            CryptoStreamMode.Write);

        #pragma warning disable SYSLIB0011
        new BinaryFormatter().Serialize(cryptoStream, playerData);
        #pragma warning restore SYSLIB0011
    }

    public static List<PlayerRun> LoadRuns(string path)
    {
        if (!File.Exists(path))
        {
            return new List<PlayerRun>();
        }

        using var fileStream = new FileStream(path, FileMode.Open);
        if (fileStream.Length == 0L)
        {
            return new List<PlayerRun>();
        }

        var cryptoServiceProvider = new DESCryptoServiceProvider();
        var cryptoStream = new CryptoStream(
            fileStream,
            cryptoServiceProvider.CreateDecryptor(Cryptography.Key, Cryptography.IV),
            CryptoStreamMode.Read);

        var binaryFormatter = new BinaryFormatter();
        #pragma warning disable SYSLIB0011
        var runs = binaryFormatter.Deserialize(cryptoStream) as List<PlayerRun>;
        #pragma warning restore SYSLIB0011

        return runs ?? new List<PlayerRun>();
    }

    public static void SaveRuns(string path, List<PlayerRun> runs)
    {
        var cryptoServiceProvider = new DESCryptoServiceProvider();
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var cryptoStream = new CryptoStream(
            fileStream,
            cryptoServiceProvider.CreateEncryptor(Cryptography.Key, Cryptography.IV),
            CryptoStreamMode.Write);

        #pragma warning disable SYSLIB0011
        new BinaryFormatter().Serialize(cryptoStream, runs);
        #pragma warning restore SYSLIB0011
    }
}
