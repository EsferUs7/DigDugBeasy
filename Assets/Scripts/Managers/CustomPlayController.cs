using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class CustomPlayController : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentContainer;
    public GameObject levelItemPrefab;
    public Button backButton;

    private void Start()
    {
        if (backButton) backButton.onClick.AddListener(BackToMain);
    }

    private void OnEnable()
    {
        RefreshList();
    }

    private void RefreshList()
    {
        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        if (SaveManager.Instance == null) return;

        string[] files = SaveManager.Instance.GetCustomLevelFiles();

        foreach (string file in files)
        {
            try
            {
                string json = File.ReadAllText(file);
                LevelData data = JsonUtility.FromJson<LevelData>(json);

                GameObject item = Instantiate(levelItemPrefab, contentContainer);

                TextMeshProUGUI text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text) text.text = string.IsNullOrEmpty(data.levelName) ? "Untitled" : data.levelName;

                Button playBtn = item.GetComponent<Button>();
                string id = Path.GetFileNameWithoutExtension(file);

                playBtn.onClick.AddListener(() => OnPlayLevel(id));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error: {e.Message}");
            }
        }
    }

    private void OnPlayLevel(string levelId)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.levelToLoad = levelId;
        }

        PlayerPrefs.SetInt("GameMode", (int)LevelManager.GameMode.Custom);
        SceneLoader.Instance.LoadScene("Gameplay");
    }

    private void BackToMain()
    {
        gameObject.SetActive(false);
        var menu = FindFirstObjectByType<MainMenuController>();
        if (menu) menu.ShowMainPanel();
    }
}