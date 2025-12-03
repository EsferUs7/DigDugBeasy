using UnityEngine;
using System.Collections.Generic;

public class GridSystem : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("World size of a single block (consists of 3x3 micro-cells)")]
    public float blockSize = 3f;

    [Header("Prefabs")]
    [Tooltip("Prefab used for visual micro-cell if no dedicated cellPrefab provided. May be null.")]
    public GameObject groundBlockPrefab;
    public GameObject indestructibleBlockPrefab;
    public GameObject rockPrefab;

    [Tooltip("Micro-cell prefab: should have SpriteRenderer + Collider2D (isTrigger=true is fine). If null, groundBlockPrefab will be used as fallback and component will be added.")]
    public GameObject cellPrefab;

    [Header("Enemies")]
    public GameObject pookaPrefab;
    public GameObject fygarPrefab;
    public Transform enemiesParent;

    [Header("Items")]
    public GameObject fruitPrefab;

    public LevelData currentLevelData;
    private GameObject[,] blockObjects;
    private GameObject[,,] cellObjects;
    private bool[,,] dugCells;

    private int gridWidth;
    private int gridHeight;

    public enum BlockType { Indestructible, Ground, Empty }

    public System.Action<int, int> OnBlockFullyDug;

    #region Initialization / Generation

    public void InitializeGrid(LevelData levelData)
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        currentLevelData = levelData;
        int maxWidth = 0;
        for (int i = 0; i < levelData.map.Length; i++)
            if (!string.IsNullOrEmpty(levelData.map[i]) && levelData.map[i].Length > maxWidth) maxWidth = levelData.map[i].Length;
        if (maxWidth == 0) maxWidth = 1;

        for (int i = 0; i < levelData.map.Length; i++)
        {
            string row = levelData.map[i] ?? "";
            row = row.Replace(' ', '0');
            if (row.Length < maxWidth) row = row.PadRight(maxWidth, '0');
            levelData.map[i] = row;
        }
        gridHeight = levelData.map.Length; gridWidth = maxWidth;
        blockObjects = new GameObject[gridWidth, gridHeight];
        cellObjects = new GameObject[gridWidth, gridHeight, 9];
        dugCells = new bool[gridWidth, gridHeight, 9];
        GenerateGridFromSymbols();
    }

    private void GenerateGridFromSymbols()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        for (int y = 0; y < gridHeight; y++)
        {
            int mapIndex = gridHeight - 1 - y;
            string row = currentLevelData.map[mapIndex];
            for (int x = 0; x < gridWidth; x++)
            {
                char symbol = (x < row.Length) ? row[x] : '0';
                CreateBlockFromSymbol(x, y, symbol);
            }
        }
    }

    private void CreateBlockFromSymbol(int x, int y, char symbol)
    {
        Vector3 worldPos = BlockToWorldPosition(x, y);
        GameObject parent = null;

        switch (symbol)
        {
            case '#':
                parent = CreateSingleBlock(worldPos, indestructibleBlockPrefab, $"Indestructible_{x}_{y}");
                break;
            case '1':
                parent = Create3x3Block(worldPos, groundBlockPrefab, $"Ground_{x}_{y}", x, y, isDestructible: true);
                break;
            case 'R':
                SpawnRock(x, y);
                break;
            case 'B':
                SpawnFruit(x, y);
                break;
    
            case '0':
            case '*':
                break;
            default:
                Debug.LogWarning($"GridSystem: Unknown symbol '{symbol}' at ({x},{y}) treated as empty");
                break;
        }

        if (parent != null)
            blockObjects[x, y] = parent;
    }

    private void SpawnFruit(int x, int y)
    {
        if (fruitPrefab == null)
        {
            Debug.LogError("GridSystem: Fruit Prefab not assigned!");
            return;
        }

        Transform itemsParent = transform.Find("Items");
        if (itemsParent == null)
        {
            GameObject ip = new GameObject("Items");
            ip.transform.parent = transform;
            itemsParent = ip.transform;
        }

        Vector3 pos = BlockToWorldPosition(x, y);
        GameObject fruitObj = Instantiate(fruitPrefab, pos, Quaternion.identity, itemsParent);
        fruitObj.name = $"Fruit_{x}_{y}";
    }

    private void SpawnRock(int x, int y)
    {
        if (rockPrefab == null)
        {
            Debug.LogError("GridSystem: Rock Prefab not assigned!");
            return;
        }

        Transform rocksParent = transform.Find("Rocks");
        if (rocksParent == null)
        {
            GameObject rp = new GameObject("Rocks");
            rp.transform.parent = transform;
            rocksParent = rp.transform;
        }

        GameObject rockObj = Instantiate(rockPrefab, rocksParent);
        rockObj.name = $"Rock_{x}_{y}";

        Rock rockScript = rockObj.GetComponent<Rock>();
        if (rockScript != null)
        {
            rockScript.Initialize(new Vector2Int(x, y), this);
        }
        else
        {
            Debug.LogError("GridSystem: Rock prefab missing Rock script!");
        }
    }

    private GameObject Create3x3Block(Vector3 centerPosition, GameObject visualPrefab, string name, int blockX, int blockY, bool isDestructible)
    {
        GameObject parent = new GameObject(name);
        parent.transform.position = centerPosition;
        parent.transform.parent = transform;

        float cellSize = blockSize / 3f;

        for (int cellY = 0; cellY < 3; cellY++)
        {
            for (int cellX = 0; cellX < 3; cellX++)
            {
                Vector3 localPos = new Vector3((cellX - 1) * cellSize, (cellY - 1) * cellSize, 0f);
                GameObject small;

                GameObject prefabToInstantiate = visualPrefab != null ? visualPrefab : cellPrefab;

                if (prefabToInstantiate != null)
                {
                    small = Instantiate(prefabToInstantiate, parent.transform);
                    small.transform.localPosition = localPos;
                }
                else
                {
                    small = new GameObject($"cellVis_{cellX}_{cellY}");
                    small.transform.parent = parent.transform;
                    small.transform.localPosition = localPos;
                    var sr = small.AddComponent<SpriteRenderer>();
                    sr.sortingLayerName = "Default";
                    sr.color = isDestructible ? Color.gray : Color.black;
                }

                small.name = $"cell_{cellX}_{cellY}";
                int index = cellY * 3 + cellX;

                if (isDestructible)
                {
                    Collider2D col = small.GetComponent<Collider2D>();
                    if (col == null)
                    {
                        var box = small.AddComponent<BoxCollider2D>();
                        box.size = new Vector2(cellSize, cellSize);
                        box.isTrigger = true;
                    }
                    else
                    {
                        col.isTrigger = true;
                    }

                    Cell cellComp = small.GetComponent<Cell>();
                    if (cellComp == null)
                        cellComp = small.AddComponent<Cell>();

                    cellComp.blockX = blockX;
                    cellComp.blockY = blockY;
                    cellComp.cellX = cellX;
                    cellComp.cellY = cellY;
                    cellComp.gridSystem = this;
                }
                else
                {
                    Collider2D col = small.GetComponent<Collider2D>();
                    if (col == null)
                    {
                        var box = small.AddComponent<BoxCollider2D>();
                        box.size = new Vector2(cellSize, cellSize);
                        box.isTrigger = false;
                    }
                    else
                    {
                        col.isTrigger = false;
                    }
                }

                cellObjects[blockX, blockY, index] = small;
            }
        }

        return parent;
    }

    #endregion

    #region Digging API (called by Cell)

    public void RequestDig(int blockX, int blockY, int cellX, int cellY, GameObject cellGameObject)
    {
        if (!IsValidBlockCoord(blockX, blockY)) return;
        if (cellX < 0 || cellX > 2 || cellY < 0 || cellY > 2) return;
        if (GetBlockType(blockX, blockY) != BlockType.Ground) return;

        int idx = cellY * 3 + cellX;
        if (dugCells[blockX, blockY, idx]) return;

        dugCells[blockX, blockY, idx] = true;

        if (cellGameObject != null) Destroy(cellGameObject);
        else
        {
            GameObject stored = cellObjects[blockX, blockY, idx];
            if (stored != null)
            {
                Destroy(stored);
                cellObjects[blockX, blockY, idx] = null;
            }
        }
        cellObjects[blockX, blockY, idx] = null;

        bool allDug = true;
        for (int i = 0; i < 9; i++)
        {
            if (!dugCells[blockX, blockY, i])
            {
                allDug = false;
                break;
            }
        }

        if (allDug)
        {
            GameObject parent = blockObjects[blockX, blockY];
            blockObjects[blockX, blockY] = null;
            SetMapCellToEmpty(blockX, blockY);

            if (parent != null) Destroy(parent);

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(ScoreType.Dig, $"block_clear_{blockX}_{blockY}");
            }

            OnBlockFullyDug?.Invoke(blockX, blockY);
        }
    }

    #endregion

    #region Helpers / Conversions / Queries

    public Vector3 BlockToWorldPosition(int blockX, int blockY)
    {
        float centerX = (gridWidth * blockSize) / 2f;
        float centerY = (gridHeight * blockSize) / 2f;

        return new Vector3(
            blockX * blockSize - centerX + blockSize / 2f,
            blockY * blockSize - centerY + blockSize / 2f,
            0f
        );
    }

    public Vector2Int WorldToBlockPosition(Vector3 worldPosition)
    {
        float centerX = (gridWidth * blockSize) / 2f;
        float centerY = (gridHeight * blockSize) / 2f;

        int blockX = Mathf.FloorToInt((worldPosition.x + centerX) / blockSize);
        int blockY = Mathf.FloorToInt((worldPosition.y + centerY) / blockSize);

        return new Vector2Int(blockX, blockY);
    }

    public bool IsValidBlockCoord(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    public BlockType GetBlockType(int blockX, int blockY)
    {
        if (!IsValidBlockCoord(blockX, blockY)) return BlockType.Indestructible;

        int mapY = gridHeight - 1 - blockY;
        if (mapY < 0 || mapY >= currentLevelData.map.Length) return BlockType.Indestructible;

        string row = currentLevelData.map[mapY];
        if (blockX >= row.Length) return BlockType.Empty;

        char c = row[blockX];
        switch (c)
        {
            case '#': return BlockType.Indestructible;
            case '1': return BlockType.Ground;
            case 'B': return BlockType.Empty;
            case 'R': return BlockType.Empty;
            case '0':
            case '*': return BlockType.Empty;
            default: return BlockType.Empty;
        }
    }

    public bool CanMoveToBlock(int blockX, int blockY)
    {
        BlockType t = GetBlockType(blockX, blockY);
        return t != BlockType.Indestructible;
    }

    public bool IsMicroCellDug(int blockX, int blockY, int cellX, int cellY)
    {
        if (!IsValidBlockCoord(blockX, blockY)) return false;
        if (cellX < 0 || cellX > 2 || cellY < 0 || cellY > 2) return false;
        int idx = cellY * 3 + cellX;
        return dugCells[blockX, blockY, idx];
    }

    #endregion

    #region Map update / helpers

    private void SetMapCellToEmpty(int blockX, int blockY)
    {
        if (currentLevelData == null) return;
        int mapY = gridHeight - 1 - blockY;
        if (mapY < 0 || mapY >= currentLevelData.map.Length) return;

        char[] chars = currentLevelData.map[mapY].ToCharArray();
        if (blockX >= 0 && blockX < chars.Length)
        {
            chars[blockX] = '0';
            currentLevelData.map[mapY] = new string(chars);
        }
    }

    #endregion

    #region Enemies / Player start

    public List<EnemyBase> SpawnEnemies(LevelData levelData)
    {
        List<EnemyBase> spawnedList = new List<EnemyBase>();

        if (levelData == null || levelData.enemies == null) return spawnedList;

        if (enemiesParent == null)
        {
            GameObject parent = new GameObject("Enemies");
            parent.transform.parent = transform;
            enemiesParent = parent.transform;
        }

        foreach (EnemySpawnData e in levelData.enemies)
        {
            GameObject prefab = GetEnemyPrefab(e.enemyType);
            if (prefab == null) continue;

            int unityY = gridHeight - 1 - e.position.y;
            Vector2Int spawnPos = new Vector2Int(e.position.x, unityY);

            Vector3 worldPos = BlockToWorldPosition(spawnPos.x, spawnPos.y);
            GameObject inst = Instantiate(prefab, worldPos, Quaternion.identity, enemiesParent);

            EnemyBase enemyScript = inst.GetComponent<EnemyBase>();
            if (enemyScript != null)
            {
                enemyScript.InitializeEnemy(spawnPos, this);
                spawnedList.Add(enemyScript);
            }
        }

        return spawnedList;
    }

    public bool IsPositionWalkable(Vector2Int gridPosition)
    {
        return IsPositionWalkable(gridPosition.x, gridPosition.y);
    }

    public bool IsPositionWalkable(int blockX, int blockY)
    {
        if (!IsValidBlockCoord(blockX, blockY)) return false;

        BlockType t = GetBlockType(blockX, blockY);

        if (t == BlockType.Empty) return true;
        if (t == BlockType.Indestructible) return false;

        if (dugCells == null) return false;

        for (int i = 0; i < 9; i++)
        {
            if (dugCells[blockX, blockY, i])
                return true;
        }

        return false;
    }

    private GameObject CreateSingleBlock(Vector3 centerPosition, GameObject prefab, string name)
    {
        GameObject block = Instantiate(prefab != null ? prefab : indestructibleBlockPrefab, transform);
        block.transform.position = centerPosition;
        block.name = name;

        Collider2D col = block.GetComponent<Collider2D>();
        if (col == null)
        {
            var box = block.AddComponent<BoxCollider2D>();
            box.size = new Vector2(blockSize, blockSize);
        }

        return block;
    }

    private GameObject GetEnemyPrefab(string type)
    {
        if (string.IsNullOrEmpty(type))
        {
            Debug.LogError("GridSystem: Enemy type is null or empty");
            return null;
        }

        switch (type.ToUpper())
        {
            case "P":
                if (pookaPrefab == null)
                    Debug.LogError("GridSystem: pookaPrefab is not assigned in inspector!");
                return pookaPrefab;
            case "F":
                if (fygarPrefab == null)
                    Debug.LogError("GridSystem: fygarPrefab is not assigned in inspector!");
                return fygarPrefab;
            default:
                Debug.LogError($"GridSystem: Unknown enemy type '{type}'");
                return null;
        }
    }

    public Vector2Int FindPlayerStartPosition()
    {
        if (currentLevelData != null && currentLevelData.playerStartPosition.x >= 0)
        {
            int unityY = gridHeight - 1 - currentLevelData.playerStartPosition.y;
            return new Vector2Int(currentLevelData.playerStartPosition.x, unityY);
        }

        if (currentLevelData != null && currentLevelData.map != null)
        {
            for (int mapY = 0; mapY < currentLevelData.map.Length; mapY++)
            {
                string row = currentLevelData.map[mapY];
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x] == '*')
                    {
                        int blockY = gridHeight - 1 - mapY;
                        return new Vector2Int(x, blockY);
                    }
                }
            }
        }

        Debug.LogWarning("GridSystem: No player start found, using default (1,1)");
        return new Vector2Int(1, 1);
    }

    #endregion
}