using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    public GridSystem gridSystem;
    public float padding = 1f;

    public void FitCamera()
    {
        if (gridSystem == null) gridSystem = FindFirstObjectByType<GridSystem>();

        if (gridSystem == null || gridSystem.currentLevelData == null)
        {
            Debug.LogWarning("CameraFit: GridSystem or LevelData is missing during fit.");
            return;
        }

        Camera cam = GetComponent<Camera>();
        float blockSize = gridSystem.blockSize;

        string[] map = gridSystem.currentLevelData.map;
        int height = map.Length;
        int width = 0;
        foreach (var row in map) if (row != null && row.Length > width) width = row.Length;

        float worldHeight = height * blockSize;
        float worldWidth = width * blockSize;

        transform.position = new Vector3(0, 0, -10f);

        float screenRatio = (float)Screen.width / (float)Screen.height;
        float targetRatio = worldWidth / worldHeight;

        if (screenRatio >= targetRatio)
        {
            cam.orthographicSize = (worldHeight / 2f) + padding;
        }
        else
        {
            float differenceInSize = targetRatio / screenRatio;
            cam.orthographicSize = (worldHeight / 2f * differenceInSize) + padding;
        }

        Debug.Log("Camera fitted to level.");
    }
}