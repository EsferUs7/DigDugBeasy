using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HighscoresController : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentContainer;
    public GameObject rowPrefab;
    public Button backButton;
    public TextMeshProUGUI emptyText;

    private void Start()
    {
        backButton.onClick.AddListener(ClosePanel);
    }

    private void OnEnable()
    {
        LoadScores();
    }

    private void LoadScores()
    {
        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        if (SaveManager.Instance == null) return;

        HighscoreTable table = SaveManager.Instance.LoadHighscores();

        if (table.entries.Count == 0)
        {
            if (emptyText) emptyText.gameObject.SetActive(true);
            return;
        }

        if (emptyText) emptyText.gameObject.SetActive(false);

        for (int i = 0; i < table.entries.Count; i++)
        {
            HighscoreEntry entry = table.entries[i];
            GameObject row = Instantiate(rowPrefab, contentContainer);

            TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length > 0) texts[0].text = (i + 1).ToString();
            if (texts.Length > 1) texts[1].text = entry.playerName;
            if (texts.Length > 2) texts[2].text = entry.score.ToString();
            if (texts.Length > 3) texts[3].text = entry.date;

            Button deleteBtn = row.GetComponentInChildren<Button>();
            if (deleteBtn != null)
            {
                int indexToRemove = i;
                deleteBtn.onClick.AddListener(() =>
                {
                    SaveManager.Instance.DeleteHighscore(indexToRemove);
                    LoadScores();
                });
            }
        }
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
        var menu = FindFirstObjectByType<MainMenuController>();
        if (menu) menu.ShowMainPanel();
    }
}