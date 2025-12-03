using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProfileController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nameInputField;
    public TextMeshProUGUI currentScoreText;
    public TextMeshProUGUI statusText;

    [Header("Buttons")]
    public Button saveButton;
    public Button backButton;

    private void OnEnable()
    {
        LoadCurrentProfileData();
        if (statusText) statusText.text = "";
    }

    private void Start()
    {
        saveButton.onClick.AddListener(SaveProfile);
        backButton.onClick.AddListener(ClosePanel);
    }

    private void LoadCurrentProfileData()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentProfile != null)
        {
            nameInputField.text = GameManager.Instance.currentProfile.playerName;
            if (currentScoreText != null)
                currentScoreText.text = $"Total Score: {GameManager.Instance.currentProfile.totalScore}";
        }
    }

    private void SaveProfile()
    {
        string newName = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(newName))
        {
            ShowStatus("Name cannot be empty!", true);
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdatePlayerName(newName);
            ShowStatus("Saved!", false);
        }
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);

        var menu = FindFirstObjectByType<MainMenuController>();
        if (menu != null) menu.ShowMainPanel();
    }

    private void ShowStatus(string message, bool isError)
    {
        if (statusText == null) return;
        statusText.text = message;
        statusText.color = isError ? Color.red : Color.green;
        CancelInvoke(nameof(HideStatus));
        Invoke(nameof(HideStatus), 2f);
    }

    private void HideStatus() { if (statusText) statusText.text = ""; }
}