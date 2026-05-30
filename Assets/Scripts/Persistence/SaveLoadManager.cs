using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Static JSON persistence utility for loading and saving PlayerData
/// under Unity's persistent data path.
/// </summary>
public static class SaveLoadManager
{
    private const string SaveFileName = "playerData.json";

    public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static void Save(PlayerData playerData)
    {
        if (playerData == null)
        {
            Debug.LogError("Cannot save null player data.");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(playerData, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"Player data saved to: {SavePath}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to save player data. Exception: {exception.Message}");
        }
    }

    public static PlayerData Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("No save file found. Returning default player data.");
            return new PlayerData();
        }

        try
        {
            string json = File.ReadAllText(SavePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("Save file was empty. Returning default player data.");
                return new PlayerData();
            }

            PlayerData playerData = JsonUtility.FromJson<PlayerData>(json);

            if (playerData == null)
            {
                Debug.LogWarning("Save data was invalid. Returning default player data.");
                return new PlayerData();
            }

            return playerData;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to load player data. Returning default player data. Exception: {exception.Message}");
            return new PlayerData();
        }
    }
}
