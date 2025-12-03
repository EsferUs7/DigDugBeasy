using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections;

public class LevelEditorManager : MonoBehaviour
{
    public static LevelData levelDataToEdit;

    [Header("Settings")]
    public int minSize = 5;
    public int maxSize = 30;
    public int maxNameLength = 20;

    public Color selectedToolColor = Color.green;
    public Color normalToolColor = Color.white;

    [Header("Grid Visualization")]
    public RectTransform gridContainerRect;
    public GameObject editorCellPrefab;
    public float baseCellSize = 40f;

    [Header("UI Feedback")]
    public TextMeshProUGUI statusText;
    public float messageDuration = 3f;

    [Header("Tool Sprites")]
    public Sprite groundSprite;
    public Sprite wallSprite;
    public Sprite rockSprite;
    public Sprite pookaSprite;
    public Sprite fygarSprite;
    public Sprite playerSprite;
    public Sprite emptySprite;
    public Sprite fruitSprite;

    [Header("UI Controls")]
    public TMP_InputField levelNameInput;
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;
    public Button generateButton;
    public Button saveButton;
    public Button backButton;

    [Header("Tool Buttons")]
    public Button btnGround, btnWall, btnRock, btnPooka, btnFygar, btnPlayer, btnFruit, btnErase;

    private char[,] mapGrid;
    private int width;
    private int height;
    private Vector2Int playerPos;
    private List<EnemySpawnData> enemies = new List<EnemySpawnData>();
    private string currentFileId;

    private enum EditorTool { None, Ground, Wall, Rock, Fruit, Pooka, Fygar, Player, Erase }
    private EditorTool currentTool = EditorTool.Ground;
    private Image[,] visualCells;

    void Start()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        if (statusText != null) statusText.text = "";

        if (levelDataToEdit == null) CreateDefaultData();
        else ParseLevelData(levelDataToEdit);

        levelNameInput.text = levelDataToEdit != null ? levelDataToEdit.levelName : "New Level";
        levelNameInput.characterLimit = maxNameLength;

        widthInput.text = width.ToString();
        heightInput.text = height.ToString();

        widthInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        heightInput.contentType = TMP_InputField.ContentType.IntegerNumber;

        saveButton.onClick.AddListener(TrySaveLevel);
        backButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene("MainMenu"));
        if (generateButton != null)
            generateButton.onClick.AddListener(OnAutoGenerateClicked);

        widthInput.onEndEdit.AddListener((s) => ValidateAndResize(true));
        heightInput.onEndEdit.AddListener((s) => ValidateAndResize(false));

        SetupToolButton(btnGround, EditorTool.Ground);
        SetupToolButton(btnWall, EditorTool.Wall);
        SetupToolButton(btnRock, EditorTool.Rock);
        SetupToolButton(btnFruit, EditorTool.Fruit);
        SetupToolButton(btnPooka, EditorTool.Pooka);
        SetupToolButton(btnFygar, EditorTool.Fygar);
        SetupToolButton(btnPlayer, EditorTool.Player);
        SetupToolButton(btnErase, EditorTool.Erase);

        BuildVisualGrid();

        StartCoroutine(UpdateGridLayoutNextFrame());

        SetTool(EditorTool.Ground, btnGround);
    }

    private void SetupToolButton(Button btn, EditorTool tool)
    {
        btn.onClick.AddListener(() => SetTool(tool, btn));
    }

    private void CreateDefaultData()
    {
        width = 15; height = 15;
        mapGrid = new char[width, height];
        currentFileId = "";
        for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) mapGrid[x, y] = IsBorder(x, y, width, height) ? '#' : '0';
        playerPos = new Vector2Int(width / 2, height / 2);
    }

    private void ParseLevelData(LevelData data)
    {
        currentFileId = data.levelId;
        height = data.map.Length;
        width = height > 0 ? data.map[0].Length : 15;
        mapGrid = new char[width, height];

        for (int y = 0; y < height; y++)
        {
            string row = data.map[y];
            for (int x = 0; x < width; x++) mapGrid[x, y] = (x < row.Length) ? row[x] : '0';
        }

        if (data.playerStartPosition.x >= 0)
        {
            playerPos = new Vector2Int(data.playerStartPosition.x, data.playerStartPosition.y);
        }
        else
        {
            playerPos = new Vector2Int(-1, -1);
        }

        if (data.enemies != null)
        {
            foreach (var e in data.enemies)
            {
                enemies.Add(new EnemySpawnData
                {
                    enemyType = e.enemyType,
                    position = new Vector2Int(e.position.x, e.position.y)
                });
            }
        }

        EnforceBorders();
    }

    public void OnCellClicked(int x, int y)
    {
        if (IsBorder(x, y, width, height))
        {
            ShowMessage("Cannot edit border blocks!", true);
            return;
        }

        if (playerPos.x == x && playerPos.y == y)
        {
            playerPos = new Vector2Int(-1, -1);
        }
        enemies.RemoveAll(e => e.position.x == x && e.position.y == y);

        char symbol = '0';

        switch (currentTool)
        {
            case EditorTool.Ground: symbol = '1'; break;
            case EditorTool.Wall: symbol = '#'; break;
            case EditorTool.Rock: symbol = 'R'; break;
            case EditorTool.Fruit: symbol = 'B'; break;
            case EditorTool.Erase: symbol = '0'; break;

            case EditorTool.Player:
                if (IsValid(playerPos.x, playerPos.y))
                {
                    Vector2Int oldPos = playerPos;
                    playerPos = new Vector2Int(-1, -1);
                    UpdateVisualCell(oldPos.x, oldPos.y);
                }

                playerPos = new Vector2Int(x, y);
                symbol = '0';
                break;

            case EditorTool.Pooka:
                enemies.Add(new EnemySpawnData { enemyType = "P", position = new Vector2Int(x, y) });
                symbol = '0'; break;
            case EditorTool.Fygar:
                enemies.Add(new EnemySpawnData { enemyType = "F", position = new Vector2Int(x, y) });
                symbol = '0'; break;
        }

        mapGrid[x, y] = symbol;
        UpdateVisualCell(x, y);
    }

    private void ValidateAndResize(bool isWidth)
    {
        string inputVal = isWidth ? widthInput.text : heightInput.text;

        if (int.TryParse(inputVal, out int val))
        {
            if (val < minSize || val > maxSize)
            {
                ShowMessage($"Size must be {minSize}-{maxSize}", true);
                if (isWidth) widthInput.text = width.ToString();
                else heightInput.text = height.ToString();
            }
            else
            {
                ResizeGrid(isWidth ? val : width, isWidth ? height : val);
            }
        }
        else
        {
            if (isWidth) widthInput.text = width.ToString();
            else heightInput.text = height.ToString();
        }
    }

    private void ResizeGrid(int newW, int newH)
    {
        if (newW == width && newH == height) return;

        char[,] newMap = new char[newW, newH];

        for (int x = 0; x < newW; x++)
        {
            for (int y = 0; y < newH; y++)
            {
                if (IsBorder(x, y, newW, newH)) newMap[x, y] = '#';
                else if (x < width && y < height)
                {
                    char old = mapGrid[x, y];
                    if (old == '#' && IsBorder(x, y, width, height)) newMap[x, y] = '0';
                    else newMap[x, y] = old;
                }
                else newMap[x, y] = '0';
            }
        }

        width = newW; height = newH; mapGrid = newMap;

        enemies.RemoveAll(e => e.position.x >= width || e.position.y >= height);
        if (playerPos.x >= width || playerPos.y >= height || IsBorder(playerPos.x, playerPos.y, width, height))
        {
            playerPos = new Vector2Int(width / 2, height / 2);
            ShowMessage("Player reset to center", false);
        }

        BuildVisualGrid();
        StartCoroutine(UpdateGridLayoutNextFrame());
    }

    private void OnAutoGenerateClicked()
    {
        int targetW = width;
        int targetH = height;

        if (int.TryParse(widthInput.text, out int w)) targetW = Mathf.Clamp(w, minSize, maxSize);
        if (int.TryParse(heightInput.text, out int h)) targetH = Mathf.Clamp(h, minSize, maxSize);

        widthInput.text = targetW.ToString();
        heightInput.text = targetH.ToString();

        LevelData genData = LevelGenerator.GenerateSidewinderLevel(targetW, targetH, 2);

        string oldId = currentFileId;

        ParseLevelData(genData);

        if (!string.IsNullOrEmpty(oldId)) currentFileId = oldId;

        BuildVisualGrid();
        StartCoroutine(UpdateGridLayoutNextFrame());

        ShowMessage("Level Generated (Sidewinder)!", false);
    }

    private void BuildVisualGrid()
    {
        foreach (Transform child in gridContainerRect) Destroy(child.gameObject);
        visualCells = new Image[width, height];

        GridLayoutGroup gridLayout = gridContainerRect.GetComponent<GridLayoutGroup>();
        if (gridLayout)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = width;
            gridLayout.cellSize = new Vector2(baseCellSize, baseCellSize);
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject cellObj = Instantiate(editorCellPrefab, gridContainerRect);
                visualCells[x, y] = cellObj.GetComponent<Image>();
                var clicker = cellObj.AddComponent<EditorCellClick>();
                clicker.x = x; clicker.y = y; clicker.manager = this;
                UpdateVisualCell(x, y);
            }
        }
    }

    private IEnumerator UpdateGridLayoutNextFrame()
    {
        yield return new WaitForEndOfFrame();
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridContainerRect);
        FitGridToScreen();
    }

    private void FitGridToScreen()
    {
        RectTransform parentRect = gridContainerRect.parent as RectTransform;
        if (parentRect == null) return;

        float availableWidth = parentRect.rect.width - 40;
        float availableHeight = parentRect.rect.height - 40;

        float contentWidth = width * baseCellSize;
        float contentHeight = height * baseCellSize;

        if (contentWidth <= 0 || contentHeight <= 0) return;

        float scaleX = availableWidth / contentWidth;
        float scaleY = availableHeight / contentHeight;
        float finalScale = Mathf.Min(scaleX, scaleY);

        finalScale = Mathf.Clamp(finalScale, 0.05f, 1.2f);

        gridContainerRect.localScale = new Vector3(finalScale, finalScale, 1f);
        gridContainerRect.anchoredPosition = Vector2.zero;
    }

    private void UpdateVisualCell(int x, int y)
    {
        if (!IsValid(x, y)) return;
        Image img = visualCells[x, y];
        char c = mapGrid[x, y];

        if (playerPos.x == x && playerPos.y == y) { img.sprite = playerSprite; return; }

        foreach (var e in enemies)
        {
            if (e.position.x == x && e.position.y == y)
            {
                img.sprite = (e.enemyType == "P") ? pookaSprite : fygarSprite; return;
            }
        }

        if (c == '1') img.sprite = groundSprite;
        else if (c == '#') img.sprite = wallSprite;
        else if (c == 'R') img.sprite = rockSprite;
        else if (c == 'B') img.sprite = fruitSprite;
        else img.sprite = emptySprite;
    }

    private void TrySaveLevel()
    {
        if (!IsValid(playerPos.x, playerPos.y) || IsBorder(playerPos.x, playerPos.y, width, height))
        {
            ShowMessage("Error: Missing Player start position!", true);
            return;
        }

        SaveLevel();
    }

    private void SaveLevel()
    {
        LevelData data = new LevelData();
        data.levelName = string.IsNullOrEmpty(levelNameInput.text) ? "Untitled Level" : levelNameInput.text;

        if (string.IsNullOrEmpty(currentFileId))
        {
            long timestamp = System.DateTime.Now.Ticks;
            currentFileId = $"custom_{timestamp}";
        }
        data.levelId = currentFileId;

        data.map = new string[height];
        for (int y = 0; y < height; y++)
        {
            char[] row = new char[width];
            for (int x = 0; x < width; x++) row[x] = mapGrid[x, y];
            data.map[y] = new string(row);
        }

        List<EnemySpawnData> gameEnemies = new List<EnemySpawnData>();
        foreach (var e in enemies)
        {
            gameEnemies.Add(new EnemySpawnData
            {
                enemyType = e.enemyType,
                position = new Vector2Int(e.position.x, e.position.y)
            });
        }
        data.enemies = gameEnemies.ToArray();

        data.playerStartPosition = new Vector2Int(playerPos.x, playerPos.y);

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveCustomLevel(data);
            ShowMessage("Level Saved Successfully!", false);
        }
        else
        {
            ShowMessage("Error: SaveManager not found!", true);
        }
    }

    private void ShowMessage(string msg, bool isError)
    {
        if (statusText == null) return;
        statusText.text = msg;
        statusText.color = isError ? Color.red : Color.green;
        CancelInvoke(nameof(HideMessage));
        Invoke(nameof(HideMessage), messageDuration);
    }

    private void HideMessage()
    {
        if (statusText != null) statusText.text = "";
    }

    private void SetTool(EditorTool tool, Button clickedButton)
    {
        currentTool = tool;
        ResetButtonColor(btnGround); ResetButtonColor(btnWall); ResetButtonColor(btnRock);
        ResetButtonColor(btnPooka); ResetButtonColor(btnFygar); ResetButtonColor(btnPlayer); ResetButtonColor(btnErase);

        if (clickedButton != null)
        {
            var colors = clickedButton.colors;
            colors.normalColor = selectedToolColor;
            colors.selectedColor = selectedToolColor;
            clickedButton.colors = colors;
        }
    }

    private void ResetButtonColor(Button btn)
    {
        if (btn == null) return;
        var colors = btn.colors;
        colors.normalColor = normalToolColor;
        colors.selectedColor = normalToolColor;
        btn.colors = colors;
    }

    private bool IsBorder(int x, int y, int w, int h) => x == 0 || x == w - 1 || y == 0 || y == h - 1;
    private void EnforceBorders() { for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) if (IsBorder(x, y, width, height)) mapGrid[x, y] = '#'; }
    private bool IsValid(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
}


public class EditorCellClick : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    public int x, y;
    public LevelEditorManager manager;

    public void OnPointerClick(PointerEventData eventData)
    {
        manager.OnCellClicked(x, y);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Input.GetMouseButton(0))
        {
            manager.OnCellClicked(x, y);
        }
    }
}