using System;
using UnityEngine;

[System.Serializable]
public class LevelData
{
    public string levelId;
    public string levelName;
    public string[] map;
    public Vector2Int playerStartPosition;
    public EnemySpawnData[] enemies;
}

[System.Serializable]
public class EnemySpawnData
{
    public string enemyType; // "P" for Pooka, "F" for Fygar
    public Vector2Int position; // Block position on the grid
}

public enum BlockType
{
    Empty,
    Ground,
    Indestructible
}