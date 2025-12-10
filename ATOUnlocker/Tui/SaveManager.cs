using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace ATOUnlocker.Tui;

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
}
