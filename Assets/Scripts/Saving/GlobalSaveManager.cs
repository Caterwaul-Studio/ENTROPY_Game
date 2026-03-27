using UnityEngine;
using System.IO;
using System;
using System.Linq;

// this contains info about the game, such as the current level and its state
// this is the file the will handle saving and loading
public static class GlobalSaveManager
{
    public static bool LoadFromSave = false;
    public static bool SavedWithTerminal = false;
    public static void SaveGame(bool permanent)
    {
        ISaveable[] saveables = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>().ToArray();
        foreach (var saveable in saveables)
        {
            // permanent save file
            if (permanent) saveable.CreateSaveFile(GenerateFileName(saveable, true));
            // temporary save file - always make one of these
            saveable.CreateSaveFile(GenerateFileName(saveable, false));
        }
    }
    //public static void LoadGame(bool permanent)
    //{
    //    ISaveable[] saveables = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>().ToArray();
    //    foreach (var saveable in saveables)
    //    {
    //        LoadSavable(saveable, permanent);
    //    }
    //}
    public static void LoadSavable(ISaveable saveable, bool permanent)
    {
        // permanent save file
        if (permanent)
        {
            saveable.LoadSaveFile(GenerateFileName(saveable, true));
        }
        // temporary save file
        else
        {
            saveable.LoadSaveFile(GenerateFileName(saveable, false));
        }
    }
    private static string GenerateFileName(ISaveable saveable, bool permanent)
    {
        string typeName = saveable.GetType().Name;
        if (permanent) return $"{typeName}_Save.json";
        return $"{typeName}_Temp.json";
    }
    public static void SaveTextToFile(string path, string fileName, string data)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        path = Path.Join(path, fileName);

        // Debug.Log(path);

        try
        {
            File.WriteAllText(path, data);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
    public static string LoadTextFromFile(string path, string fileName)
    {
        string data = null;
        path = Path.Join(path, fileName);

        if (!File.Exists(path)) return null;
        try
        {
            data = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
        return data;
    }
    // Deletes the temp files
    public static void DeleteTempFiles()
    {
        // Delete all of the temp save files
        // The path where our save files are stored
        string path = Application.persistentDataPath;
        // Find all of the temporary save files
        string[] files = Directory.GetFiles(path, "*Temp*", SearchOption.AllDirectories);
        int deletedCount = 0;
        foreach (string file in files)
        {
            try
            {
                File.Delete(file);
                deletedCount++;
            }
            catch (IOException e)
            {
                Debug.LogWarning($"Failed to delete file: {file}\n{e.Message}");
            }
        }
    }
    // This function overwrites the temp files with the permanent save files
    public static void OverwriteTempFiles()
    {
        // Copy any permanent save files to temp save file paths
        // The path where our save files are stored
        string path = Application.persistentDataPath;
        // Find all of the permanent save files
        string[] files = Directory.GetFiles(path, "*Save*", SearchOption.AllDirectories);
        int copiedCount = 0;
        foreach (string sourceFilePath in files)
        {
            try
            {
                // Copy permanent save files to the temp save file paths
                string directory = Path.GetDirectoryName(sourceFilePath);
                string filename = Path.GetFileName(sourceFilePath);
                string newFilename = filename.Replace("Save", "Temp");
                string destinationFilePath = Path.Combine(directory, newFilename);
                File.Copy(sourceFilePath, destinationFilePath, true);
                copiedCount++;
            }
            catch (IOException e)
            {
                Debug.LogWarning($"Failed to copy file: {sourceFilePath}\n{e.Message}");
            }
        }
    }
    public static bool SaveDataExists()
    {
        // The path where our save files are stored
        string path = Application.persistentDataPath;
        // Find all of the temporary save files
        string[] files = Directory.GetFiles(path, "*Save*", SearchOption.AllDirectories);
        if (files.Length > 0) return true;
        return false;
    }
    public static bool TempDataExists()
    {
        // The path where our save files are stored
        string path = Application.persistentDataPath;
        // Find all of the temporary save files
        string[] files = Directory.GetFiles(path, "*Temp*", SearchOption.AllDirectories);
        if (files.Length > 0) return true;
        return false;
    }
    public static bool SaveFileExists(string fileName)
    {
        return File.Exists(Path.Join(Application.persistentDataPath, fileName));
    }
}
