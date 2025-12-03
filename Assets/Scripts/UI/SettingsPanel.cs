using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    public Toggle soundToggle;
    public Button backButton;

    void Start()
    {
        MainMenuController mainMenuController = FindFirstObjectByType<MainMenuController>();

        soundToggle.isOn = GameManager.Instance.soundEnabled;
        soundToggle.onValueChanged.AddListener(ToggleSound);
    }

    void ToggleSound(bool isOn)
    {
        GameManager.Instance.ToggleSound();
    }
}