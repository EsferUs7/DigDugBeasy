using UnityEngine;
using System.Collections.Generic;

public static class LevelGenerator
{
    public static LevelData GenerateEndlessLevel(int difficultyLevel)
    {
        return GenerateSidewinderLevel(15, 15, difficultyLevel);
    }

    public static LevelData GenerateSidewinderLevel(int width, int height, int difficulty)
    {
        LevelData data = new LevelData();
        data.levelId = "auto_" + System.DateTime.Now.Ticks;
        data.levelName = "Sidewinder Area";

        char[,] mapGrid = new char[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    mapGrid[x, y] = '#';
                else
                    mapGrid[x, y] = '1';
            }
        }

        int startX = 1;
        int endX = width - 2;
        int startY = 1;
        int endY = height - 2;

        for (int y = startY; y <= endY; y += 2)
        {
            int runStart = startX;

            for (int x = startX; x <= endX; x += 2)
            {
                bool atEasternBoundary = (x + 2 > endX);
                bool atNorthernBoundary = (y - 2 < startY);

                bool shouldCarveEast = !atEasternBoundary && (Random.value < 0.6f);

                if (shouldCarveEast)
                {
                    Carve(mapGrid, x, y);
                    Carve(mapGrid, x + 1, y);
                    Carve(mapGrid, x + 2, y);
                }
                else
                {
                    Carve(mapGrid, x, y);

                    if (!atNorthernBoundary)
                    {
                        int cellCount = (x - runStart) / 2 + 1;
                        int randomCellIndex = Random.Range(0, cellCount);
                        int randomX = runStart + (randomCellIndex * 2);

                        Carve(mapGrid, randomX, y - 1);
                        Carve(mapGrid, randomX, y - 2);
                    }

                    runStart = x + 2;
                }
            }
        }

        int playerX = width / 2;
        int playerY = height / 2;

        if (mapGrid[playerX, playerY] != '0')
        {
            mapGrid[playerX, playerY] = '0';
        }

        data.playerStartPosition = new Vector2Int(playerX, height - 1 - playerY);

        List<EnemySpawnData> enemies = new List<EnemySpawnData>();
        int enemyCount = 3 + difficulty;
        int attempts = 0;

        while (enemies.Count < enemyCount && attempts < 200)
        {
            attempts++;
            int ex = Random.Range(1, width - 1);
            int ey = Random.Range(1, height - 1);

            if (mapGrid[ex, ey] == '0' && Vector2Int.Distance(new Vector2Int(ex, ey), new Vector2Int(playerX, playerY)) > 4)
            {
                if (!enemies.Exists(e => e.position.x == ex && e.position.y == (height - 1 - ey)))
                {
                    string type = Random.value > 0.5f ? "P" : "F";
                    enemies.Add(new EnemySpawnData { enemyType = type, position = new Vector2Int(ex, height - 1 - ey) });
                }
            }
        }
        data.enemies = enemies.ToArray();

        int rockCount = 2 + (difficulty / 2);
        int placedRocks = 0;
        attempts = 0;

        while (placedRocks < rockCount && attempts < 200)
        {
            attempts++;
            int rx = Random.Range(1, width - 1);
            int ry = Random.Range(1, height - 2);

            if (mapGrid[rx, ry] == '1')
            {
                mapGrid[rx, ry] = 'R';
                placedRocks++;
            }
        }

        data.map = new string[height];
        for (int y = 0; y < height; y++)
        {
            char[] row = new char[width];
            for (int x = 0; x < width; x++) row[x] = mapGrid[x, y];
            data.map[y] = new string(row);
        }

        return data;
    }

    private static void Carve(char[,] grid, int x, int y)
    {
        if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
        {
            if (grid[x, y] != '#')
                grid[x, y] = '0';
        }
    }
}