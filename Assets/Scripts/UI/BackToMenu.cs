using UnityEngine;
using UnityEngine.UI;

public class BackToMenu : MonoBehaviour
{
    public Button returnButton;

    void Start()
    {
        returnButton.onClick.AddListener(() => {
            FindFirstObjectByType<SceneLoader>().LoadScene("MainMenu");
        });
    }
}