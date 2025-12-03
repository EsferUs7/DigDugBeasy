using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveTime = 0.18f;
    public LayerMask obstacleLayer;

    [Header("References")]
    public GridSystem gridSystem;
    public PumpController pumpController;
    public LevelManager myLevelManager;

    [Header("Player Health")]
    public int playerLives = 3;
    public bool isInvulnerable = false;
    public float invulnerabilityTime = 2f;

    public System.Action<int> OnLivesChanged;
    public System.Action OnPlayerDeath;

    public static PlayerController Instance { get; private set; }

    private Vector2Int currentBlockPosition = Vector2Int.zero;
    private Vector2Int nextMoveDirection = Vector2Int.zero;
    private Vector2Int lastFacingDirection = Vector2Int.right;
    private bool isMoving = false;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Coroutine moveCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(Instance.gameObject);
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (obstacleLayer == 0) obstacleLayer = LayerMask.GetMask("Default", "Rock");
    }

    private void Start()
    {
        if (pumpController == null) pumpController = GetComponent<PumpController>();
        if (gridSystem == null) gridSystem = FindFirstObjectByType<GridSystem>();
    }

    public void InitializePlayer(Vector2Int startBlock, LevelManager manager)
    {
        myLevelManager = manager;

        if (gridSystem == null && manager != null) gridSystem = manager.gridSystem;

        if (gridSystem == null) return;
        currentBlockPosition = startBlock;
        Vector3 world = gridSystem.BlockToWorldPosition(startBlock.x, startBlock.y);
        transform.position = world;
        if (rb != null) rb.MovePosition(world);
    }

    private void Update()
    {
        if (pumpController != null && pumpController.IsPumpActive()) return;

        int x = 0; int y = 0;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x = -1;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x = 1;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y = 1;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y = -1;

        if (x != 0) y = 0;
        Vector2Int inputDir = new Vector2Int(x, y);

        if (inputDir != Vector2Int.zero)
        {
            lastFacingDirection = inputDir;

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = false;
                transform.rotation = Quaternion.identity;

                if (inputDir.x > 0)
                {
                    // nothing
                }
                else if (inputDir.x < 0)
                {
                    spriteRenderer.flipX = true;
                }
                else if (inputDir.y > 0)
                {
                    transform.rotation = Quaternion.Euler(0, 0, 90);
                }
                else if (inputDir.y < 0)
                {
                    transform.rotation = Quaternion.Euler(0, 0, -90);
                }
            }
        }

        if (!isMoving && inputDir != Vector2Int.zero) TryStartMove(inputDir);
        else if (isMoving && inputDir != Vector2Int.zero) nextMoveDirection = inputDir;
    }

    private void TryStartMove(Vector2Int dir)
    {
        if (gridSystem == null) return;
        Vector2Int targetBlock = currentBlockPosition + dir;
        if (!gridSystem.IsValidBlockCoord(targetBlock.x, targetBlock.y)) return;
        if (!gridSystem.CanMoveToBlock(targetBlock.x, targetBlock.y)) return;

        Vector3 targetWorldPos = gridSystem.BlockToWorldPosition(targetBlock.x, targetBlock.y);
        Collider2D obstacle = Physics2D.OverlapCircle(targetWorldPos, 0.2f, obstacleLayer);
        if (obstacle != null && obstacle.GetComponent<Rock>() != null) return;

        moveCoroutine = StartCoroutine(MoveToBlockCoroutine(targetBlock, dir));
    }

    private IEnumerator MoveToBlockCoroutine(Vector2Int targetBlock, Vector2Int dir)
    {
        isMoving = true;
        Vector3 start = rb.position;
        Vector3 end = gridSystem.BlockToWorldPosition(targetBlock.x, targetBlock.y);
        float elapsed = 0f;
        while (elapsed < moveTime)
        {
            rb.MovePosition(Vector3.Lerp(start, end, elapsed / moveTime));
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        rb.MovePosition(end);
        currentBlockPosition = targetBlock;

        if (nextMoveDirection != Vector2Int.zero)
        {
            Vector2Int nextDir = nextMoveDirection;
            nextMoveDirection = Vector2Int.zero;
            isMoving = false;
            TryStartMove(nextDir);
            yield break;
        }
        isMoving = false;
        moveCoroutine = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isInvulnerable) return;
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null) TakeDamage();
    }

    public void TakeDamage()
    {
        if (isInvulnerable) return;
        playerLives--;
        OnLivesChanged?.Invoke(playerLives);
        if (playerLives <= 0) Die();
        else StartCoroutine(InvulnerabilityCoroutine());
    }

    private void Die()
    {
        Debug.Log("[PlayerController] DIE called.");
        if (pumpController != null) pumpController.ForceRetractPump();
        OnPlayerDeath?.Invoke();

        if (myLevelManager != null)
        {
            Debug.Log("[PlayerController] Calling EndLevel on stored reference.");
            myLevelManager.EndLevel(false);
        }
        else
        {
            Debug.LogWarning("[PlayerController] Direct LevelManager ref missing, trying Singleton.");
            if (LevelManager.Instance != null)
                LevelManager.Instance.EndLevel(false);
            else
                Debug.LogError("[CRITICAL] Both Direct Ref and Singleton are NULL!");
        }

        gameObject.SetActive(false);
    }

    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        while (elapsed < invulnerabilityTime)
        {
            if (sr) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        if (sr) sr.enabled = true;
        isInvulnerable = false;
    }

    public Vector2Int GetBlockPosition() => currentBlockPosition;
    public Vector2Int GetFacingDirection() => isMoving && nextMoveDirection != Vector2Int.zero ? nextMoveDirection : lastFacingDirection;
    public Vector2Int GetLastMoveDirection() => nextMoveDirection == Vector2Int.zero ? Vector2Int.right : nextMoveDirection;
    public bool IsMoving() => isMoving;
    public bool CanUsePump() => !isMoving;
    private void OnDestroy() { if (Instance == this) Instance = null; }
}