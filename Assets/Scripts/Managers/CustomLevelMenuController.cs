using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class CustomLevelSelectController : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentContainer;
    public GameObject levelItemPrefab;
    public Button createNewButton;
    public Button backButton;

    private void Start()
    {
        createNewButton.onClick.AddListener(OnCreateNewClicked);
        backButton.onClick.AddListener(BackToMain);
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

                Button btn = item.GetComponent<Button>();
                btn.onClick.AddListener(() => OnEditLevel(data, file));
            }
            catch (System.Exception e) { Debug.LogWarning($"Error: {e.Message}"); }
        }
    }

    private void OnCreateNewClicked()
    {
        LevelData newLevel = new LevelData();
        newLevel.levelName = "New Level";
        newLevel.levelId = "";
        newLevel.map = new string[15];
        for (int i = 0; i < 15; i++)
        {
            if (i == 0 || i == 14) newLevel.map[i] = "###############";
            else newLevel.map[i] = "#0000000000000#";
        }
        newLevel.enemies = new EnemySpawnData[0];
        newLevel.playerStartPosition = new Vector2Int(7, 7);

        LaunchEditor(newLevel);
    }

    private void OnEditLevel(LevelData data, string filePath)
    {
        data.levelId = Path.GetFileNameWithoutExtension(filePath);
        LaunchEditor(data);
    }

    private void LaunchEditor(LevelData data)
    {
        LevelEditorManager.levelDataToEdit = data;
        SceneLoader.Instance.LoadScene("LevelEditor");
    }

    private void BackToMain()
    {
        gameObject.SetActive(false);
        var menu = FindFirstObjectByType<MainMenuController>();
        if (menu) menu.ShowMainPanel();
    }
}