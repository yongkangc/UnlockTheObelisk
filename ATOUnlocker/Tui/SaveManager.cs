using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.Json;

namespace ATOUnlocker.Tui;

// Note: The game stores runs as List<String> where each string is a JSON-serialized PlayerRun.
// We convert between List<String> (file format) and List<PlayerRun> (in-memory) for editing.

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

    /// <summary>
    /// Load runs from runs.ato file. The file contains List&lt;String&gt; where each string is JSON.
    /// </summary>
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
        var jsonStrings = binaryFormatter.Deserialize(cryptoStream) as List<string>;
        #pragma warning restore SYSLIB0011

        if (jsonStrings == null)
        {
            return new List<PlayerRun>();
        }

        // Convert JSON strings to PlayerRun objects
        var runs = new List<PlayerRun>();
        foreach (var json in jsonStrings)
        {
            var playerRun = DeserializePlayerRunFromJson(json);
            if (playerRun != null)
            {
                runs.Add(playerRun);
            }
        }

        return runs;
    }

    /// <summary>
    /// Save runs to runs.ato file. Converts PlayerRun objects to JSON strings and saves as List&lt;String&gt;.
    /// </summary>
    public static void SaveRuns(string path, List<PlayerRun> runs)
    {
        // Convert PlayerRun objects to JSON strings
        var jsonStrings = new List<string>();
        foreach (var run in runs)
        {
            var json = SerializePlayerRunToJson(run);
            jsonStrings.Add(json);
        }

        var cryptoServiceProvider = new DESCryptoServiceProvider();
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var cryptoStream = new CryptoStream(
            fileStream,
            cryptoServiceProvider.CreateEncryptor(Cryptography.Key, Cryptography.IV),
            CryptoStreamMode.Write);

        #pragma warning disable SYSLIB0011
        new BinaryFormatter().Serialize(cryptoStream, jsonStrings);
        #pragma warning restore SYSLIB0011
    }

    /// <summary>
    /// Serialize a PlayerRun to JSON string matching the game's format.
    /// </summary>
    private static string SerializePlayerRunToJson(PlayerRun run)
    {
        var dict = new Dictionary<string, object?>();
        var type = run.GetType();

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = field.GetValue(run);
            dict[field.Name] = value;
        }

        return JsonSerializer.Serialize(dict);
    }

    /// <summary>
    /// Deserialize a PlayerRun from JSON string.
    /// </summary>
    private static PlayerRun? DeserializePlayerRunFromJson(string json)
    {
        try
        {
            var run = new PlayerRun();
            var type = run.GetType();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            foreach (var prop in root.EnumerateObject())
            {
                var field = type.GetField(prop.Name, BindingFlags.Public | BindingFlags.Instance);
                if (field == null) continue;

                var value = ConvertJsonElement(prop.Value, field.FieldType);
                if (value != null)
                {
                    field.SetValue(run, value);
                }
            }

            return run;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Convert a JsonElement to the target type.
    /// </summary>
    private static object? ConvertJsonElement(JsonElement element, Type targetType)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (targetType == typeof(int) || targetType == typeof(Int32))
                    return element.GetInt32();
                if (targetType == typeof(float) || targetType == typeof(Single))
                    return element.GetSingle();
                if (targetType == typeof(double))
                    return element.GetDouble();
                return element.GetInt32();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Array:
                if (targetType == typeof(int[]))
                {
                    var list = new List<int>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(item.GetInt32());
                    }
                    return list.ToArray();
                }
                if (targetType == typeof(List<string>))
                {
                    var list = new List<string>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(item.GetString() ?? "");
                    }
                    return list;
                }
                return null;
            default:
                return null;
        }
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
