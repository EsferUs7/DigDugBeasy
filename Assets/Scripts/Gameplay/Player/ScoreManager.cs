using UnityEngine;
using TMPro;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Values")]
    public int digScore = 1;
    public int enemyDefeatScore = 100;
    public int rockCrushScore = 50;
    public int fruitCollectScore = 200;
    public int levelCompleteScore = 1000;

    [Header("UI (optional)")]
    public TextMeshProUGUI scoreText;

    public event Action<int> OnScoreChanged;

    private int _currentScore = 0;
    public int CurrentScore => _currentScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("ScoreManager initialized");
    }

    public void AddScore(ScoreType scoreType, string source = "")
    {
        int points = GetPointsForType(scoreType);
        _currentScore += points;

        Debug.Log($"Score: +{points} for {scoreType} ({source}) | Total: {_currentScore}");
        OnScoreChanged?.Invoke(_currentScore);
        UpdateScoreUI();
    }

    private int GetPointsForType(ScoreType scoreType)
    {
        return scoreType switch
        {
            ScoreType.Dig => digScore,
            ScoreType.EnemyDefeat => enemyDefeatScore,
            ScoreType.RockCrush => rockCrushScore,
            ScoreType.FruitCollect => fruitCollectScore,
            ScoreType.LevelComplete => levelCompleteScore,
            _ => 0
        };
    }

    public void ResetScore()
    {
        _currentScore = 0;
        OnScoreChanged?.Invoke(_currentScore);
        UpdateScoreUI();
        Debug.Log("Score reset to 0");
    }

    public void SetScore(int newScore)
    {
        _currentScore = newScore;
        OnScoreChanged?.Invoke(_currentScore);
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = _currentScore.ToString();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

public enum ScoreType
{
    Dig,
    EnemyDefeat,
    RockCrush,
    FruitCollect,
    LevelComplete
}