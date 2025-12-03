using UnityEngine;
using TMPro;

public class FPSDisplay : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    public float updateInterval = 0.5f;

    private float accum = 0;
    private int frames = 0;
    private float timeleft;

    void Start()
    {
        timeleft = updateInterval;
        if (fpsText == null) fpsText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        timeleft -= Time.unscaledDeltaTime;
        accum += Time.timeScale / Time.unscaledDeltaTime;
        frames++;

        if (timeleft <= 0.0)
        {
            float fps = accum / frames;
            string format = string.Format("{0:F0} FPS", fps);

            if (fpsText != null)
            {
                fpsText.text = format;

                if (fps < 30) fpsText.color = Color.red;
                else if (fps < 60) fpsText.color = Color.yellow;
                else fpsText.color = Color.green;
            }

            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }
}