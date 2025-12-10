using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace ATOUnlocker.Tui;

// Note: We use the game's PlayerRun class from Assembly-CSharp.dll
// This ensures BinaryFormatter serialization is compatible with the game

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

    public static GameData? LoadGameData(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var fileStream = new FileStream(path, FileMode.Open);
        if (fileStream.Length == 0L)
        {
            return null;
        }

        var cryptoServiceProvider = new DESCryptoServiceProvider();
        var cryptoStream = new CryptoStream(
            fileStream,
            cryptoServiceProvider.CreateDecryptor(Cryptography.Key, Cryptography.IV),
            CryptoStreamMode.Read);

        var binaryFormatter = new BinaryFormatter();
        #pragma warning disable SYSLIB0011
        var gameData = binaryFormatter.Deserialize(cryptoStream) as GameData;
        #pragma warning restore SYSLIB0011

        return gameData;
    }

    public static void SaveGameData(string path, GameData gameData)
    {
        var cryptoServiceProvider = new DESCryptoServiceProvider();
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var cryptoStream = new CryptoStream(
            fileStream,
            cryptoServiceProvider.CreateEncryptor(Cryptography.Key, Cryptography.IV),
            CryptoStreamMode.Write);

        #pragma warning disable SYSLIB0011
        new BinaryFormatter().Serialize(cryptoStream, gameData);
        #pragma warning restore SYSLIB0011
    }
}
