using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LevelSelectController : MonoBehaviour
{
    [Header("References")]
    public GameObject buttonPrefab;
    public Transform buttonsContainer;
    public Button backButton;

    private void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(BackToMain);
        }
    }

    private void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(GenerateButtonsRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator GenerateButtonsRoutine()
    {
        for (int i = buttonsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(buttonsContainer.GetChild(i).gameObject);
        }

        yield return null;

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager is missing!");
            yield break;
        }

        string[] levels = GameManager.Instance.campaignLevelFiles;
        Debug.Log($"[UI] Generating {levels.Length} level buttons.");

        for (int i = 0; i < levels.Length; i++)
        {
            string fileId = levels[i];
            int levelNum = i + 1;

            bool isUnlocked = GameManager.Instance.IsLevelUnlocked(fileId);

            GameObject btnObj = Instantiate(buttonPrefab, buttonsContainer);

            btnObj.transform.localScale = Vector3.one;
            Vector3 localPos = btnObj.transform.localPosition;
            localPos.z = 0;
            btnObj.transform.localPosition = localPos;

            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (txt != null) txt.text = levelNum.ToString();

            btn.interactable = isUnlocked;

            Image btnImg = btn.GetComponent<Image>();
            if (btnImg != null)
            {
                btnImg.color = isUnlocked ? Color.white : Color.gray;
            }

            string idToLoad = fileId;
            btn.onClick.AddListener(() => OnLevelSelected(idToLoad));
        }

        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsContainer as RectTransform);
    }

    private void OnLevelSelected(string levelId)
    {
        Debug.Log($"Selected Level: {levelId}");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.levelToLoad = levelId;
        }

        PlayerPrefs.SetInt("GameMode", (int)LevelManager.GameMode.Campaign);
        SceneLoader.Instance.LoadScene("Gameplay");
    }

    public void BackToMain()
    {
        MainMenuController menu = FindFirstObjectByType<MainMenuController>();
        if (menu != null)
        {
            menu.ShowMainPanel();
        }
        else
        {
            gameObject.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}