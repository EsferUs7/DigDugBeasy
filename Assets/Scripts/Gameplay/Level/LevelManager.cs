using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Зв'язки")]
    public GridSystem gridSystem;
    public CameraFit cameraFit;
    public GameUI gameUI;
    public GameObject playerPrefab;

    [Header("Дані")]
    public GameMode currentGameMode = GameMode.Campaign;
    [HideInInspector] public LevelData currentLevelData;

    public static bool forceNewEndlessGeneration = true;

    private List<EnemyBase> activeEnemies = new List<EnemyBase>();
    private int enemiesRemaining;

    private bool isLevelEnded = false;
    private bool scoreSavedInThisSession = false;

    public enum GameMode { Endless = 0, Campaign = 1, Custom = 2 }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[LevelManager] Duplicate detected! Destroying new instance on {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log($"[LevelManager] Initialized. Instance ID: {GetInstanceID()}");
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Debug.Log($"[LevelManager] Destroying Instance ID: {GetInstanceID()}");
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(InitializeRoutine());
    }

    private IEnumerator InitializeRoutine()
    {
        isLevelEnded = false;
        scoreSavedInThisSession = false;

        if (GameManager.Instance != null && ScoreManager.Instance != null)
        {
            if (currentGameMode == GameMode.Endless)
            {
                int levelNum = PlayerPrefs.GetInt("EndlessLevelNum", 1);

                if (levelNum == 1 && !GameManager.Instance.isRetry)
                {
                    GameManager.Instance.ResetSessionScore();
                    ScoreManager.Instance.ResetScore();
                }
                else
                {
                    ScoreManager.Instance.SetScore(GameManager.Instance.scoreAtLevelStart);
                }
            }
            else
            {
                GameManager.Instance.ResetSessionScore();
                ScoreManager.Instance.ResetScore();
            }

            GameManager.Instance.ResumeGame();
            Time.timeScale = 1f;
        }

        yield return null;

        if (!gameUI) gameUI = FindFirstObjectByType<GameUI>();
        if (!gridSystem) gridSystem = FindFirstObjectByType<GridSystem>();
        if (!cameraFit) cameraFit = FindFirstObjectByType<CameraFit>();

        LoadLevelData();

        if (gridSystem != null)
        {
            while (gridSystem.transform.childCount > 0)
                DestroyImmediate(gridSystem.transform.GetChild(0).gameObject);

            if (currentLevelData != null)
                gridSystem.InitializeGrid(currentLevelData);
        }

        yield return null;

        if (cameraFit != null && gridSystem != null)
        {
            cameraFit.gridSystem = gridSystem;
            cameraFit.FitCamera();
        }

        if (gameUI != null)
        {
            if (currentLevelData != null)
                gameUI.SetLevelName(currentLevelData.levelName);

            gameUI.UpdateHighScoreDisplay();
        }

        PlayerController pc = SpawnPlayer();
        SpawnEnemies();

        if (gameUI != null)
        {
            string lvlName = currentLevelData != null ? currentLevelData.levelName : "Unknown";
            gameUI.InitializeHUD(currentGameMode, pc, lvlName);
        }

        if (currentGameMode == GameMode.Endless && forceNewEndlessGeneration)
            forceNewEndlessGeneration = false;

        Debug.Log($"[LevelManager] Level Initialized. Mode: {currentGameMode}");
    }

    public void RestartLevel()
    {
        Debug.Log("[LevelManager] Restarting Level...");
        Time.timeScale = 1f;

        if (GameManager.Instance) GameManager.Instance.PrepareForRetry();

        forceNewEndlessGeneration = false;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextEndlessLevel()
    {
        if (currentGameMode != GameMode.Endless) currentGameMode = GameMode.Endless;

        Debug.Log("[LevelManager] Generating Next Endless Level...");

        int levelNum = PlayerPrefs.GetInt("EndlessLevelNum", 1);
        PlayerPrefs.SetInt("EndlessLevelNum", levelNum + 1);

        if (GameManager.Instance) GameManager.Instance.PrepareForNextLevel();

        forceNewEndlessGeneration = true;
        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadNextCampaignLevel()
    {
        if (currentLevelData == null || GameManager.Instance == null)
        {
            Debug.LogError("Cannot load next level: LevelData or GameManager missing.");
            GoToMainMenu();
            return;
        }

        string currentId = currentLevelData.levelId;
        string[] allLevels = GameManager.Instance.campaignLevelFiles;
        int currentIndex = System.Array.IndexOf(allLevels, currentId);

        if (currentIndex >= 0 && currentIndex < allLevels.Length - 1)
        {
            string nextLevelId = allLevels[currentIndex + 1];
            Debug.Log($"[LevelManager] Loading Next Campaign Level: {nextLevelId}");

            GameManager.Instance.levelToLoad = nextLevelId;

            GameManager.Instance.ResetScoreForCampaign();

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            Debug.Log("[LevelManager] Campaign Finished. Returning to Menu.");
            GoToMainMenu();
        }
    }

    public void GoToMainMenu()
    {
        if (!isLevelEnded && currentGameMode == GameMode.Endless) TrySaveEndlessScore();

        Time.timeScale = 1f;
        forceNewEndlessGeneration = true;
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();

        SceneManager.LoadScene("MainMenu");
    }

    public void EndLevel(bool won)
    {
        if (isLevelEnded) return;
        isLevelEnded = true;

        Time.timeScale = 0f;

        if (won)
        {
            if (currentGameMode == GameMode.Campaign && GameManager.Instance != null)
            {
                int score = ScoreManager.Instance ? ScoreManager.Instance.CurrentScore : 0;
                GameManager.Instance.CompleteLevel(currentLevelData.levelId, score);
            }

            if (gameUI != null) gameUI.ShowWinPanel();
        }
        else
        {
            TrySaveEndlessScore();
            if (gameUI != null) gameUI.ShowLosePanel();
        }

        if (GameManager.Instance) GameManager.Instance.EndGameSession(won);
    }

    private void TrySaveEndlessScore()
    {
        if (scoreSavedInThisSession) return;

        if (currentGameMode != GameMode.Endless) return;

        if (SaveManager.Instance != null && GameManager.Instance != null && ScoreManager.Instance != null)
        {
            int currentScore = ScoreManager.Instance.CurrentScore;
            if (currentScore > 0)
            {
                string playerName = GameManager.Instance.currentProfile.playerName;
                SaveManager.Instance.AddHighscore(playerName, currentScore);

                scoreSavedInThisSession = true;
                Debug.Log($"Endless Highscore Saved for {playerName}: {currentScore}");
            }
        }
    }

    private void LoadLevelData()
    {
        currentGameMode = (GameMode)PlayerPrefs.GetInt("GameMode", 0);
        string fileName = "campaign_level_1";
        bool isBuiltIn = true;

        if (currentGameMode == GameMode.Endless)
        {
            isBuiltIn = false;
            fileName = "endless_level.json";
            if (forceNewEndlessGeneration)
            {
                int levelNum = PlayerPrefs.GetInt("EndlessLevelNum", 1);
                LevelData newData = LevelGenerator.GenerateSidewinderLevel(15, 15, levelNum);
                if (SaveManager.Instance != null) SaveManager.Instance.SaveLevelData(newData, fileName);
                currentLevelData = newData;
                return;
            }
        }
        else if (currentGameMode == GameMode.Custom)
        {
            if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.levelToLoad))
                fileName = GameManager.Instance.levelToLoad;
            else
                fileName = "custom_none";
            isBuiltIn = false;
        }
        else
        {
            if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.levelToLoad))
                fileName = GameManager.Instance.levelToLoad;
            isBuiltIn = true;
        }

        currentLevelData = LoadJson(fileName, isBuiltIn);
        if (currentLevelData == null) currentLevelData = CreateDefaultLevel();
    }

    private LevelData LoadJson(string fileName, bool isBuiltIn)
    {
        try
        {
            string path = "";
            if (isBuiltIn)
                path = Path.Combine(Application.streamingAssetsPath, "Levels", fileName + ".json");
            else if (SaveManager.Instance != null)
            {
                if (currentGameMode == GameMode.Endless)
                    path = SaveManager.Instance.GetLevelFilePath(fileName);
                else
                    path = Path.Combine(SaveManager.Instance.GetCustomLevelsPath(), fileName + ".json");
            }

            if (File.Exists(path)) return JsonUtility.FromJson<LevelData>(File.ReadAllText(path));
        }
        catch { }
        return null;
    }

    private PlayerController SpawnPlayer()
    {
        if (!playerPrefab || !gridSystem) return null;
        Vector2Int start = gridSystem.FindPlayerStartPosition();
        Vector3 pos = gridSystem.BlockToWorldPosition(start.x, start.y);
        pos.z = 0;
        GameObject p = Instantiate(playerPrefab, pos, Quaternion.identity);
        var pc = p.GetComponent<PlayerController>();
        if (pc) pc.InitializePlayer(start, this);
        return pc;
    }

    private void SpawnEnemies()
    {
        if (!gridSystem) return;
        activeEnemies.Clear();
        var enemies = gridSystem.SpawnEnemies(currentLevelData);
        foreach (var e in enemies)
        {
            e.OnEnemyDefeated += (en) => { activeEnemies.Remove(en); enemiesRemaining--; if (enemiesRemaining <= 0) EndLevel(true); };
            activeEnemies.Add(e);
        }
        enemiesRemaining = activeEnemies.Count;
    }

    private LevelData CreateDefaultLevel()
    {
        return new LevelData() { levelId = "def", levelName = "Default", map = new string[] { "#######", "#1*001#", "#######" }, enemies = new EnemySpawnData[0] };
    }
}