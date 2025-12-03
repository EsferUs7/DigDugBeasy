using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public bool soundEnabled = true;
    public PlayerProfile currentProfile;

    public int scoreAtLevelStart = 0;
    public bool isRetry = false;

    [Header("Game State")]
    public GameState currentGameState = GameState.Menu;

    public System.Action<int> OnScoreUpdated;
    public System.Action<GameState> OnGameStateChanged;

    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver,
        LevelComplete
    }

    [Header("Campaign Configuration")]
    public string[] campaignLevelFiles = new string[]
    {
        "campaign_level_1",
        "campaign_level_2",
        "campaign_level_3",
        "campaign_level_4",
        "campaign_level_5"
    };

    public string levelToLoad;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeGame()
    {
        EnsureSaveManagerExists();
        LoadPlayerProfile();

        soundEnabled = PlayerPrefs.GetInt("SoundEnabled", soundEnabled ? 1 : 0) == 1;
        AudioListener.volume = soundEnabled ? 1f : 0f;

        SetupScoreSync();
        ResetSessionScore();

        Debug.Log("GameManager initialized successfully");
    }

    private void SetupScoreSync()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += SyncScoreWithGameManager;
        }
        else
        {
            Debug.LogWarning("ScoreManager not found - score synchronization disabled");
        }
    }

    private void SyncScoreWithGameManager(int newScore)
    {
        OnScoreUpdated?.Invoke(newScore);

        if (currentProfile != null)
        {
            currentProfile.totalScore = newScore;
        }
    }

    private void EnsureSaveManagerExists()
    {
        if (SaveManager.Instance == null)
        {
            GameObject saveManagerObj = new GameObject("SaveManager");
            saveManagerObj.AddComponent<SaveManager>();
            DontDestroyOnLoad(saveManagerObj);
        }
    }

    private void LoadPlayerProfile()
    {
        if (SaveManager.Instance != null)
        {
            PlayerProfile savedProfile = SaveManager.Instance.LoadPlayerProfile();
            if (savedProfile != null)
            {
                currentProfile = savedProfile;
                Debug.Log("Player profile loaded successfully");

                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.SetScore(currentProfile.totalScore);
                }
            }
            else
            {
                CreateAndSaveNewProfile();
            }
        }
        else
        {
            Debug.LogError("SaveManager not available! Creating default profile in-memory.");
            CreateDefaultProfile();
        }
    }

    private void CreateAndSaveNewProfile()
    {
        currentProfile = CreateDefaultProfile();
        SavePlayerProfile();
        Debug.Log("New player profile created and saved");
    }

    private PlayerProfile CreateDefaultProfile()
    {
        return new PlayerProfile()
        {
            playerName = "Player",
            totalScore = 0,
            campaignProgress = new CampaignProgress[]
            {
                new CampaignProgress() { levelId = "campaign_level_1", completed = false, bestScore = 0, bestTime = 0 },
                new CampaignProgress() { levelId = "campaign_level_2", completed = false, bestScore = 0, bestTime = 0 },
                new CampaignProgress() { levelId = "campaign_level_3", completed = false, bestScore = 0, bestTime = 0 },
                new CampaignProgress() { levelId = "campaign_level_4", completed = false, bestScore = 0, bestTime = 0 },
                new CampaignProgress() { levelId = "campaign_level_5", completed = false, bestScore = 0, bestTime = 0 }
            },
            customLevels = new CustomLevel[0]
        };
    }

    public void SetGameState(GameState newState)
    {
        if (currentGameState == newState) return;

        currentGameState = newState;
        OnGameStateChanged?.Invoke(newState);

        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                EndGameSession(false);
                break;
            case GameState.LevelComplete:
                EndGameSession(true);
                break;
        }

        Debug.Log("GameState changed to: " + newState);
    }

    public void ToggleSound()
    {
        soundEnabled = !soundEnabled;
        AudioListener.volume = soundEnabled ? 1f : 0f;

        PlayerPrefs.SetInt("SoundEnabled", soundEnabled ? 1 : 0);
        PlayerPrefs.Save();
        SavePlayerProfile();
    }

    public void SavePlayerProfile()
    {
        if (SaveManager.Instance != null && currentProfile != null)
        {
            if (ScoreManager.Instance != null)
            {
                currentProfile.totalScore = ScoreManager.Instance.CurrentScore;
            }

            SaveManager.Instance.SavePlayerProfile(currentProfile);
            Debug.Log("Player profile saved");
        }
    }

    public void EndGameSession(bool won = false)
    {
        if (currentProfile != null)
        {
            if (ScoreManager.Instance != null)
            {
                currentProfile.totalScore = ScoreManager.Instance.CurrentScore;
            }

            SavePlayerProfile();
        }
        else
        {
            Debug.LogWarning("EndGameSession called but currentProfile is null.");
        }

        Debug.Log("Game session ended. Won: " + won + ". Final score: " + GetCurrentSessionScore());

        if (!won)
        {
            SetGameState(GameState.GameOver);
        }
        else
        {
            SetGameState(GameState.LevelComplete);
        }
    }

    public int GetCurrentSessionScore()
    {
        if (ScoreManager.Instance != null)
        {
            return ScoreManager.Instance.CurrentScore;
        }
        return 0;
    }

    public void UpdatePlayerName(string newName)
    {
        if (!string.IsNullOrEmpty(newName) && currentProfile != null)
        {
            currentProfile.playerName = newName;
            SavePlayerProfile();
        }
    }

    public void ResetSessionScore()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }
        OnScoreUpdated?.Invoke(0);
    }

    public void StartNewGame()
    {
        scoreAtLevelStart = 0;
        isRetry = false;
        ResetSessionScore();
        SetGameState(GameState.Playing);
    }

    public void PrepareForNextLevel()
    {
        if (ScoreManager.Instance != null)
            scoreAtLevelStart = ScoreManager.Instance.CurrentScore;

        isRetry = false;
    }

    public void PrepareForRetry()
    {
        isRetry = true;
    }

    public void ResetScoreForCampaign()
    {
        scoreAtLevelStart = 0;
        isRetry = false;
    }

    public void TogglePause()
    {
        if (currentGameState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
        }
        else if (currentGameState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        currentGameState = GameState.Paused;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        currentGameState = GameState.Playing;
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= SyncScoreWithGameManager;
        }
    }

    public bool IsLevelUnlocked(string levelFileId)
    {
        if (levelFileId == campaignLevelFiles[0]) return true;

        int index = System.Array.IndexOf(campaignLevelFiles, levelFileId);
        if (index <= 0) return true;

        string prevLevelId = campaignLevelFiles[index - 1];

        if (currentProfile != null && currentProfile.campaignProgress != null)
        {
            foreach (var progress in currentProfile.campaignProgress)
            {
                if (progress.levelId == prevLevelId && progress.completed)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void CompleteLevel(string levelFileId, int score)
    {
        Debug.Log($"Saving Progress for Level: {levelFileId}");

        if (currentProfile == null) return;

        if (currentProfile.campaignProgress == null)
            currentProfile.campaignProgress = new CampaignProgress[0];

        CampaignProgress prog = System.Array.Find(currentProfile.campaignProgress, p => p.levelId == levelFileId);

        if (prog == null)
        {
            Debug.Log("New level completion record created.");
            var list = new System.Collections.Generic.List<CampaignProgress>(currentProfile.campaignProgress);
            prog = new CampaignProgress { levelId = levelFileId, completed = true, bestScore = score };
            list.Add(prog);
            currentProfile.campaignProgress = list.ToArray();
        }
        else
        {
            Debug.Log("Updating existing level record.");
            prog.completed = true;
            if (score > prog.bestScore) prog.bestScore = score;
        }

        SavePlayerProfile();
    }
}