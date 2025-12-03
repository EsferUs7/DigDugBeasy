using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureManagersExist();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void EnsureManagersExist()
    {
        if (GameManager.Instance == null)
        {
            GameObject gameManager = new GameObject("GameManager");
            gameManager.AddComponent<GameManager>();
            DontDestroyOnLoad(gameManager);
        }

        if (SaveManager.Instance == null)
        {
            GameObject saveManager = new GameObject("SaveManager");
            saveManager.AddComponent<SaveManager>();
            DontDestroyOnLoad(saveManager);
        }
    }

    public void LoadScene(string sceneName)
    {
        EnsureManagersExist();
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
