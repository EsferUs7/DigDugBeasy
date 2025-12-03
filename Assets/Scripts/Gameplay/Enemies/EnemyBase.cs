using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState
{
    Normal,
    Ghost,
    Inflated,
    Dying
}

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Collider2D))]
public class EnemyBase : MonoBehaviour
{
    [Header("Base Settings")]
    public float moveTime = 0.25f;
    public float ghostSpeed = 2f;
    public float ghostTriggerTime = 4f;
    public float minGhostDuration = 2f;

    protected List<Vector2Int> currentPath;
    protected Vector3 wanderTarget;
    protected float wanderRadius = 3f;
    protected float wanderJitter = 1f;

    [Header("Inflation Settings")]
    public int pumpsToKill = 4;
    public float deflateRate = 0.5f;

    [Header("Visuals")]
    public Sprite normalSprite;
    public Sprite ghostSprite;
    public Color ghostColor = new Color(1, 1, 1, 0.7f);

    [Header("References")]
    public GridSystem gridSystem;
    public LayerMask obstacleLayer;

    protected EnemyState currentState = EnemyState.Normal;
    protected Vector2Int currentBlockPosition;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Collider2D col;

    protected bool isMoving = false;
    protected int currentInflation = 0;
    protected Coroutine deflateCoroutine;
    protected Vector3 originalScale;

    protected float timeSinceLastMove = 0f;
    protected float timeSpentInGhost = 0f;

    public event System.Action<EnemyBase> OnEnemyDefeated;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (normalSprite == null && spriteRenderer != null)
            normalSprite = spriteRenderer.sprite;

        originalScale = transform.localScale;
        if (obstacleLayer == 0) obstacleLayer = LayerMask.GetMask("Default", "Rock");
    }

    public virtual void InitializeEnemy(Vector2Int blockPos, GridSystem grid)
    {
        gridSystem = grid;
        currentBlockPosition = blockPos;
        transform.position = (gridSystem != null) ? gridSystem.BlockToWorldPosition(blockPos.x, blockPos.y) : transform.position;
        StartCoroutine(BehaviorLoop());
    }

    protected virtual IEnumerator BehaviorLoop()
    {
        yield return new WaitForSeconds(Random.Range(0f, 0.5f));

        while (true)
        {
            switch (currentState)
            {
                case EnemyState.Normal:
                    yield return NormalStateRoutine();
                    break;
                case EnemyState.Ghost:
                    yield return GhostStateRoutine();
                    break;
                case EnemyState.Inflated:
                    yield return null;
                    break;
                case EnemyState.Dying:
                    yield break;
            }
            yield return null;
        }
    }

    protected virtual IEnumerator NormalStateRoutine()
    {
        timeSinceLastMove += Time.deltaTime;

        bool isStuck = timeSinceLastMove > ghostTriggerTime;
        bool randomAggression = Random.value < 0.002f;

        if (isStuck || randomAggression)
        {
            EnterGhostMode();
            yield break;
        }

        if (!isMoving)
        {
            Vector2Int dir = GetNextMoveDirection();
            if (dir != Vector2Int.zero)
            {
                if (dir.x != 0) spriteRenderer.flipX = dir.x < 0;
                Vector2Int targetBlock = currentBlockPosition + dir;

                if (CanMoveTo(targetBlock))
                {
                    yield return MoveToBlock(targetBlock);
                    timeSinceLastMove = 0f;
                }
            }
        }
    }

    protected bool CanMoveTo(Vector2Int targetBlock)
    {
        if (gridSystem == null) return false;
        if (!gridSystem.IsValidBlockCoord(targetBlock.x, targetBlock.y)) return false;
        if (!gridSystem.IsPositionWalkable(targetBlock)) return false;

        Vector3 targetWorld = gridSystem.BlockToWorldPosition(targetBlock.x, targetBlock.y);
        Collider2D hit = Physics2D.OverlapCircle(targetWorld, 0.2f, obstacleLayer);
        if (hit != null && hit.GetComponent<Rock>() != null) return false;

        return true;
    }

    protected Vector2Int GetNextMoveDirection()
    {
        if (PlayerController.Instance == null) return GetRandomDirection();

        Vector2Int playerPos = PlayerController.Instance.GetBlockPosition();

        currentPath = Pathfinding.FindPath(gridSystem, currentBlockPosition, playerPos);

        if (currentPath != null && currentPath.Count > 0)
        {
            Vector2Int nextStep = currentPath[0];
            return nextStep - currentBlockPosition;
        }

        return GetRandomDirection();
    }

    private Vector2Int GetRandomDirection()
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        for (int i = 0; i < dirs.Length; i++)
        {
            Vector2Int temp = dirs[i];
            int r = Random.Range(i, dirs.Length);
            dirs[i] = dirs[r];
            dirs[r] = temp;
        }

        foreach (var dir in dirs)
        {
            if (CanMoveTo(currentBlockPosition + dir)) return dir;
        }
        return Vector2Int.zero;
    }

    protected IEnumerator MoveToBlock(Vector2Int targetBlock)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        Vector3 endPos = gridSystem.BlockToWorldPosition(targetBlock.x, targetBlock.y);

        float elapsed = 0f;
        while (elapsed < moveTime)
        {
            if (currentState != EnemyState.Normal) { isMoving = false; yield break; }
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / moveTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        currentBlockPosition = targetBlock;
        isMoving = false;
    }

    public virtual void EnterGhostMode()
    {
        if (currentState == EnemyState.Dying || currentState == EnemyState.Inflated) return;

        currentState = EnemyState.Ghost;
        timeSpentInGhost = 0f;

        StopAllCoroutines();
        isMoving = false;

        if (ghostSprite) spriteRenderer.sprite = ghostSprite;
        spriteRenderer.color = ghostColor;

        col.isTrigger = true;

        StartCoroutine(BehaviorLoop());
    }

    public void ExitGhostMode()
    {
        currentState = EnemyState.Normal;

        if (normalSprite) spriteRenderer.sprite = normalSprite;
        spriteRenderer.color = Color.white;

        col.isTrigger = false;

        currentBlockPosition = gridSystem.WorldToBlockPosition(transform.position);
        transform.position = gridSystem.BlockToWorldPosition(currentBlockPosition.x, currentBlockPosition.y);

        timeSinceLastMove = 0f;
    }

    protected IEnumerator GhostStateRoutine()
    {
        timeSpentInGhost += Time.deltaTime;
        Vector3 steeringForce = Vector3.zero;

        if (PlayerController.Instance != null)
        {
            Vector3 targetPos = PlayerController.Instance.transform.position;
            steeringForce += Seek(targetPos);
        }
        else
        {
            steeringForce += Wander();
        }

        Vector3 velocity = steeringForce.normalized * ghostSpeed;
        transform.position += velocity * Time.deltaTime;

        if (velocity.x != 0) spriteRenderer.flipX = velocity.x < 0;

        if (timeSpentInGhost > minGhostDuration)
        {
            Vector2Int gridPos = gridSystem.WorldToBlockPosition(transform.position);
            Vector3 centerPos = gridSystem.BlockToWorldPosition(gridPos.x, gridPos.y);

            if (Vector3.Distance(transform.position, centerPos) < 0.2f &&
                gridSystem.IsPositionWalkable(gridPos))
            {
                ExitGhostMode();
            }
        }

        yield return null;
    }

    private Vector3 Seek(Vector3 target)
    {
        Vector3 desiredVelocity = (target - transform.position).normalized * ghostSpeed;
        return desiredVelocity;
    }

    private Vector3 Wander()
    {
        wanderTarget += new Vector3(Random.Range(-1f, 1f) * wanderJitter, Random.Range(-1f, 1f) * wanderJitter, 0);
        wanderTarget.Normalize();
        wanderTarget *= wanderRadius;

        Vector3 targetLocal = wanderTarget + transform.position;
        return Seek(targetLocal);
    }

    public virtual void StartInflation()
    {
        if (currentState == EnemyState.Dying) return;

        StopAllCoroutines();
        if (deflateCoroutine != null) StopCoroutine(deflateCoroutine);

        if (currentState == EnemyState.Ghost)
        {
            if (normalSprite) spriteRenderer.sprite = normalSprite;
            spriteRenderer.color = Color.white;
            col.isTrigger = false;
        }

        currentState = EnemyState.Inflated;
        isMoving = false;
    }

    public void PumpInflate()
    {
        currentInflation++;
        transform.localScale += Vector3.one * 0.3f;
        if (currentInflation >= pumpsToKill) Die();
    }

    public void StopInflation()
    {
        if (currentState == EnemyState.Dying) return;
        if (deflateCoroutine != null) StopCoroutine(deflateCoroutine);
        deflateCoroutine = StartCoroutine(DeflateRoutine());
    }

    protected IEnumerator DeflateRoutine()
    {
        while (currentInflation > 0)
        {
            yield return new WaitForSeconds(deflateRate);
            currentInflation--;
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale + (Vector3.one * 0.3f * currentInflation), 0.5f);
        }

        transform.localScale = originalScale;
        currentState = EnemyState.Normal;
        StartCoroutine(BehaviorLoop());
    }

    public virtual void Die()
    {
        currentState = EnemyState.Dying;
        StopAllCoroutines();
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(ScoreType.EnemyDefeat, gameObject.name);
        OnEnemyDefeated?.Invoke(this);
        Destroy(gameObject);
    }
}