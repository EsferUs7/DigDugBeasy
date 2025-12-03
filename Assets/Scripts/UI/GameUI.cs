using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("HUD Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI levelNameText;
    public Button hudPauseButton;

    [Header("Lives Display")]
    public TextMeshProUGUI livesText;

    [Header("Menus")]
    public GameObject pauseMenuPanel;
    public GameObject levelCompletePanel;
    public GameObject gameOverPanel;

    [Header("End Game Score Texts")]
    public TextMeshProUGUI winScoreText;
    public TextMeshProUGUI loseScoreText;

    [Header("Menu Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button menuButton;

    [Header("Game Over / Win Buttons")]
    public Button winRestartButton;
    public Button winMenuButton;
    public Button nextLevelButton;
    public Button failRestartButton;
    public Button failMenuButton;

    [Header("Sound Buttons")]
    public Button[] soundToggleButtons;
    public TextMeshProUGUI[] soundButtonTexts;

    private bool isPaused = false;
    private bool isLevelLoading = false;

    private void Start()
    {
        SetupButtons();
        EnsureEventSystem();
        SetupSoundButtons();
        UpdateSoundUI();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScore;
            UpdateScore(ScoreManager.Instance.CurrentScore);
        }

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnLivesChanged += UpdateLives;
            UpdateLives(PlayerController.Instance.playerLives);
        }

        if (LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null)
        {
            if (highScoreText != null)
            {
                bool showHighScore = (LevelManager.Instance.currentGameMode == LevelManager.GameMode.Endless);
                highScoreText.gameObject.SetActive(showHighScore);
            }
            SetLevelName(LevelManager.Instance.currentLevelData.levelName);
        }
        else
        {
            SetLevelName("Loading...");
        }

        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (levelCompletePanel) levelCompletePanel.SetActive(false);
    }

    public void InitializeHUD(LevelManager.GameMode mode, PlayerController player, string levelName)
    {
        if (highScoreText != null)
        {
            bool showHighScore = (mode == LevelManager.GameMode.Endless);
            highScoreText.gameObject.SetActive(showHighScore);
            if (showHighScore) UpdateHighScoreDisplay();
        }

        SetLevelName(levelName);

        if (player != null)
        {
            player.OnLivesChanged -= UpdateLives;
            player.OnLivesChanged += UpdateLives;
            UpdateLives(player.playerLives);
        }
        else
        {
            Debug.LogError("[GameUI] Player reference passed is NULL!");
        }
    }

    public void UpdateHighScoreDisplay()
    {
        if (highScoreText != null && GameManager.Instance != null && SaveManager.Instance != null)
        {
            string playerName = GameManager.Instance.currentProfile.playerName;
            int fileHighScore = SaveManager.Instance.GetHighScoreForPlayer(playerName);
            int currentSessionScore = 0;
            if (ScoreManager.Instance != null)
                currentSessionScore = ScoreManager.Instance.CurrentScore;

            int displayScore = Mathf.Max(fileHighScore, currentSessionScore);
            highScoreText.text = $"HIGH-SCORE: {displayScore}";
        }
    }

    private void SetupSoundButtons()
    {
        if (soundToggleButtons != null)
        {
            foreach (var btn in soundToggleButtons)
            {
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(OnSoundToggleClicked);
                }
            }
        }
    }

    private void OnSoundToggleClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToggleSound();
            UpdateSoundUI();
        }
    }

    private void UpdateSoundUI()
    {
        if (GameManager.Instance == null) return;

        bool isOn = GameManager.Instance.soundEnabled;
        string text = isOn ? "SOUND: ON" : "SOUND: OFF";

        if (soundButtonTexts != null)
        {
            foreach (var txt in soundButtonTexts)
            {
                if (txt != null) txt.text = text;
            }
        }
    }

    public void ShowWinPanel()
    {
        Debug.Log("[GameUI] Showing Win Panel");
        if (levelCompletePanel != null)
        {
            if (winScoreText != null && ScoreManager.Instance != null)
            {
                winScoreText.text = "SCORE: " + ScoreManager.Instance.CurrentScore;
            }

            levelCompletePanel.SetActive(true);
            levelCompletePanel.transform.SetAsLastSibling();
        }
        else Debug.LogError("[GameUI] Level Complete Panel is not assigned!");
    }

    public void ShowLosePanel()
    {
        Debug.Log("[GameUI] Showing Lose Panel");
        if (gameOverPanel != null)
        {
            if (loseScoreText != null && ScoreManager.Instance != null)
            {
                loseScoreText.text = "SCORE: " + ScoreManager.Instance.CurrentScore;
            }

            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }
        else Debug.LogError("[GameUI] Game Over Panel is not assigned!");
    }

    public void SetLevelName(string name)
    {
        if (levelNameText != null)
            levelNameText.text = name;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem").AddComponent<UnityEngine.EventSystems.EventSystem>()
               .AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private void SetupButtons()
    {
        if (hudPauseButton) hudPauseButton.onClick.AddListener(TogglePause);
        if (resumeButton) resumeButton.onClick.AddListener(TogglePause);
        if (restartButton) restartButton.onClick.AddListener(RestartLevel);
        if (menuButton) menuButton.onClick.AddListener(GoToMenu);

        if (winRestartButton) winRestartButton.onClick.AddListener(RestartLevel);
        if (winMenuButton) winMenuButton.onClick.AddListener(GoToMenu);
        if (nextLevelButton) nextLevelButton.onClick.AddListener(OnNextLevelClicked);

        if (failRestartButton) failRestartButton.onClick.AddListener(RestartLevel);
        if (failMenuButton) failMenuButton.onClick.AddListener(GoToMenu);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        if (LevelManager.Instance != null) LevelManager.Instance.RestartLevel();
        else UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void OnNextLevelClicked()
    {
        if (isLevelLoading) return;

        LevelManager mgr = LevelManager.Instance;
        if (mgr == null)
        {
            mgr = FindFirstObjectByType<LevelManager>();
            if (mgr != null) Debug.LogWarning("[GameUI] LevelManager.Instance was null, but found object in scene manually.");
        }

        if (mgr == null)
        {
            Debug.LogError("[GameUI] CRITICAL: LevelManager not found in scene! Cannot proceed.");
            return;
        }

        isLevelLoading = true;
        if (nextLevelButton != null) nextLevelButton.interactable = false;

        Time.timeScale = 1f;

        if (mgr.currentGameMode == LevelManager.GameMode.Endless)
            mgr.NextEndlessLevel();
        else if (mgr.currentGameMode == LevelManager.GameMode.Campaign)
            mgr.LoadNextCampaignLevel();
        else
            mgr.RestartLevel();
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        if (LevelManager.Instance != null) LevelManager.Instance.GoToMainMenu();
        else UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        if (gameOverPanel != null && gameOverPanel.activeSelf) return;
        if (levelCompletePanel != null && levelCompletePanel.activeSelf) return;

        isPaused = !isPaused;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    private void UpdateScore(int newScore)
    {
        if (scoreText != null) scoreText.text = $"SCORE: {newScore}";
        if (highScoreText != null)
        {
            int fileHighScore = 0;
            if (SaveManager.Instance != null && GameManager.Instance != null)
                fileHighScore = SaveManager.Instance.GetHighScoreForPlayer(GameManager.Instance.currentProfile.playerName);

            if (newScore > fileHighScore) highScoreText.text = $"HIGH-SCORE: {newScore}";
        }
    }

    private void UpdateLives(int currentLives)
    {
        if (livesText != null)
        {
            livesText.text = $"LIVES: {currentLives}";
        }
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null) ScoreManager.Instance.OnScoreChanged -= UpdateScore;
        if (PlayerController.Instance != null) PlayerController.Instance.OnLivesChanged -= UpdateLives;
    }
}