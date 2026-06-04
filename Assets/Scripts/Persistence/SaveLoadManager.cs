using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Static JSON persistence utility for loading and saving GameSaveData
/// under Unity's persistent data path.
/// </summary>
public static class SaveLoadManager
{
    private const string SaveFileName = "gameSaveData.json";

    public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static void Save(GameSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError("Cannot save null game save data.");
            return;
        }

        try
        {
            saveData.EnsureProfiles();
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to save game data. Exception: {exception.Message}");
        }
    }

    public static GameSaveData Load()
    {
        return TryLoad(out GameSaveData saveData)
            ? saveData
            : new GameSaveData();
    }

    public static bool TryLoad(out GameSaveData saveData)
    {
        if (!File.Exists(SavePath))
        {
            saveData = null;
            return false;
        }

        try
        {
            string json = File.ReadAllText(SavePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("Save file was empty.");
                saveData = null;
                return false;
            }

            saveData = JsonUtility.FromJson<GameSaveData>(json);

            if (saveData == null)
            {
                Debug.LogWarning("Save data was invalid.");
                return false;
            }

            saveData.EnsureProfiles();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to load game data. Exception: {exception.Message}");
            saveData = null;
            return false;
        }
    }

    public static bool DeleteSave()
    {
        if (!File.Exists(SavePath))
        {
            return false;
        }

        try
        {
            File.Delete(SavePath);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to delete game data. Exception: {exception.Message}");
            return false;
        }
    }
}
