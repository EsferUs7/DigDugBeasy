using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Buttons")]
    public Button endlessModeButton;
    public Button campaignModeButton;
    public Button customModeButton;
    public Button levelEditorButton;
    public Button scoresButton;
    public Button profileButton;
    public Button exitButton;

    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject levelSelectPanel;
    public GameObject customPlayPanel;
    public GameObject editorMenuPanel;
    public GameObject highscoresPanel;
    public GameObject profilePanel;

    [Header("Other")]
    public Button soundToggleButton;
    public TextMeshProUGUI soundButtonText;

    void Start()
    {
        endlessModeButton.onClick.AddListener(StartEndlessMode);
        campaignModeButton.onClick.AddListener(OpenCampaignMenu);

        customModeButton.onClick.AddListener(OpenCustomPlayMenu);
        if (levelEditorButton) levelEditorButton.onClick.AddListener(OpenEditorMenu);
        if (scoresButton) scoresButton.onClick.AddListener(OpenHighscores);
        if (profileButton) profileButton.onClick.AddListener(OpenProfile);

        exitButton.onClick.AddListener(Application.Quit);
        soundToggleButton.onClick.AddListener(ToggleSound);
        UpdateSoundButtonText();

        ShowMainPanel();
    }

    private void OnEnable()
    {
        UpdateSoundButtonText();
    }

    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
        if (editorMenuPanel) editorMenuPanel.SetActive(false);
        if (customPlayPanel) customPlayPanel.SetActive(false);
        if (highscoresPanel) highscoresPanel.SetActive(false);
        if (profilePanel) profilePanel.SetActive(false);
    }

    public void OpenCampaignMenu()
    {
        if (levelSelectPanel == null)
        {
            Debug.LogError("CRITICAL ERROR: 'Level Select Panel' is NOT assigned in MainMenuController Inspector!");
            return;
        }

        Debug.Log("[MainMenu] Switching to Campaign Menu...");
        mainPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
    }

    public void OpenEditorMenu()
    {
        mainPanel.SetActive(false);
        if (editorMenuPanel) editorMenuPanel.SetActive(true);
    }

    public void OpenCustomPlayMenu()
    {
        mainPanel.SetActive(false);
        if (customPlayPanel) customPlayPanel.SetActive(true);
    }

    public void StartEndlessMode()
    {
        PlayerPrefs.SetInt("EndlessLevelNum", 1);
        LevelData newLevel = LevelGenerator.GenerateEndlessLevel(1);
        if (SaveManager.Instance != null) SaveManager.Instance.SaveLevelData(newLevel, "endless_level.json");
        PlayerPrefs.SetInt("GameMode", (int)LevelManager.GameMode.Endless);
        PlayerPrefs.Save();
        SceneLoader.Instance.LoadScene("Gameplay");
    }

    void ToggleSound()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToggleSound();
            UpdateSoundButtonText();
        }
    }

    void UpdateSoundButtonText()
    {
        bool isOn = GameManager.Instance != null ? GameManager.Instance.soundEnabled : true;
        if (soundButtonText != null)
            soundButtonText.text = isOn ? "SOUND: ON" : "SOUND: OFF";
    }

    public void OpenProfile()
    {
        mainPanel.SetActive(false);
        if (profilePanel) profilePanel.SetActive(true);
    }

    public void OpenHighscores()
    {
        mainPanel.SetActive(false);
        if (highscoresPanel) highscoresPanel.SetActive(true);
    }
}