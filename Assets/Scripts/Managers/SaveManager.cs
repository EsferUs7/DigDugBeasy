using UnityEngine;
using System.IO;
using System;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string saveFolderPath;
    private string levelsFolderPath;
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDirectories();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDirectories()
    {
        if (isInitialized) return;

        saveFolderPath = Path.Combine(Application.persistentDataPath, "Saves");
        levelsFolderPath = Path.Combine(Application.persistentDataPath, "Levels");

        if (!Directory.Exists(saveFolderPath))
            Directory.CreateDirectory(saveFolderPath);

        if (!Directory.Exists(levelsFolderPath))
            Directory.CreateDirectory(levelsFolderPath);

        Directory.CreateDirectory(Path.Combine(levelsFolderPath, "Campaign"));
        Directory.CreateDirectory(Path.Combine(levelsFolderPath, "Custom"));

        isInitialized = true;
        Debug.Log("SaveManager initialized: " + saveFolderPath);
    }

    public void SavePlayerProfile(PlayerProfile profile)
    {
        try
        {
            if (!isInitialized)
                InitializeDirectories();

            if (profile == null)
            {
                Debug.LogError("Cannot save null profile");
                return;
            }

            profile.campaignProgress ??= new CampaignProgress[0];
            profile.customLevels ??= new CustomLevel[0];

            string filePath = Path.Combine(saveFolderPath, "playerprofile.json");
            string json = JsonUtility.ToJson(profile, true);

            File.WriteAllText(filePath, json);
            Debug.Log("Profile saved: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Save profile error: {e.Message}");
        }
    }

    public PlayerProfile LoadPlayerProfile()
    {
        try
        {
            if (!isInitialized)
                InitializeDirectories();

            string filePath = Path.Combine(saveFolderPath, "playerprofile.json");

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                PlayerProfile profile = JsonUtility.FromJson<PlayerProfile>(json);

                if (IsProfileValid(profile))
                {
                    Debug.Log("Profile loaded from file");
                    return profile;
                }
                else
                {
                    Debug.LogWarning("Profile file is invalid or corrupted");
                    return null;
                }
            }
            else
            {
                Debug.Log("No profile file found");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Load profile error: {e.Message}");
            return null;
        }
    }

    public void SaveLevelData(LevelData data, string fileName)
    {
        try
        {
            if (!isInitialized) InitializeDirectories();

            string path = Path.Combine(levelsFolderPath, fileName);
            string json = JsonUtility.ToJson(data, true);

            File.WriteAllText(path, json);
            Debug.Log($"Level generated and saved to: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving level data: {e.Message}");
        }
    }

    public string GetLevelFilePath(string fileName)
    {
        if (!isInitialized) InitializeDirectories();
        return Path.Combine(levelsFolderPath, fileName);
    }

    private bool IsProfileValid(PlayerProfile profile)
    {
        if (profile == null) return false;
        if (string.IsNullOrEmpty(profile.playerName)) return false;

        profile.campaignProgress ??= new CampaignProgress[0];
        profile.customLevels ??= new CustomLevel[0];

        return true;
    }

    public string GetCustomLevelsPath()
    {
        if (!isInitialized) InitializeDirectories();
        string path = Path.Combine(levelsFolderPath, "Custom");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }

    public string[] GetCustomLevelFiles()
    {
        string path = GetCustomLevelsPath();
        if (Directory.Exists(path))
        {
            return Directory.GetFiles(path, "*.json");
        }
        return new string[0];
    }

    public void SaveCustomLevel(LevelData data)
    {
        try
        {
            string folder = GetCustomLevelsPath();

            if (string.IsNullOrEmpty(data.levelId))
            {
                Debug.LogError("Cannot save level with empty ID");
                return;
            }

            string fileName = $"{data.levelId}.json";
            string fullPath = Path.Combine(folder, fileName);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(fullPath, json);
            Debug.Log($"Custom level saved: {fullPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving custom level: {e.Message}");
        }
    }

    public void DeleteCustomLevel(string filePath)
    {
        if (File.Exists(filePath)) File.Delete(filePath);
    }

    public void AddHighscore(string playerName, int score)
    {
        try
        {
            HighscoreTable table = LoadHighscores();
            if (table == null) table = new HighscoreTable();

            HighscoreEntry newEntry = new HighscoreEntry
            {
                playerName = playerName,
                score = score,
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            table.entries.Add(newEntry);

            table.entries.Sort((a, b) => b.score.CompareTo(a.score));

            if (table.entries.Count > 20)
            {
                table.entries.RemoveRange(20, table.entries.Count - 20);
            }

            string json = JsonUtility.ToJson(table, true);
            string path = Path.Combine(saveFolderPath, "highscores.json");
            File.WriteAllText(path, json);

            Debug.Log($"Highscore saved: {playerName} - {score}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving highscore: {e.Message}");
        }
    }

    public HighscoreTable LoadHighscores()
    {
        try
        {
            if (!isInitialized) InitializeDirectories();
            string path = Path.Combine(saveFolderPath, "highscores.json");

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<HighscoreTable>(json);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading highscores: {e.Message}");
        }
        return new HighscoreTable();
    }

    public int GetHighScoreForPlayer(string playerName)
    {
        HighscoreTable table = LoadHighscores();
        int maxScore = 0;

        if (table != null && table.entries != null)
        {
            foreach (var entry in table.entries)
            {
                if (entry.playerName == playerName && entry.score > maxScore)
                {
                    maxScore = entry.score;
                }
            }
        }
        return maxScore;
    }

    public void DeleteHighscore(int index)
    {
        try
        {
            HighscoreTable table = LoadHighscores();
            if (table != null && index >= 0 && index < table.entries.Count)
            {
                table.entries.RemoveAt(index);

                string json = JsonUtility.ToJson(table, true);
                string path = Path.Combine(saveFolderPath, "highscores.json");
                File.WriteAllText(path, json);

                Debug.Log($"Highscore at index {index} deleted.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deleting highscore: {e.Message}");
        }
    }
}