using UnityEngine;
using System.Collections;

public class Fygar : EnemyBase
{
    [Header("Fygar Settings")]
    public GameObject fireBreathPrefab;
    public float attackRange = 3f;
    public float fireLengthBlocks = 3f;
    public float chargeTime = 1.0f;
    public float fireDuration = 0.5f;
    public float attackCooldown = 3.0f;

    private bool isAttacking = false;
    private float lastAttackTime = -999f;

    public override void EnterGhostMode()
    {
        isAttacking = false;
        spriteRenderer.color = Color.white;

        base.EnterGhostMode();
    }

    public override void StartInflation()
    {
        isAttacking = false;
        spriteRenderer.color = Color.white;
        base.StartInflation();
    }

    protected override IEnumerator NormalStateRoutine()
    {
        if (isAttacking)
        {
            yield return null;
            yield break;
        }

        timeSinceLastMove += Time.deltaTime;

        bool isStuck = timeSinceLastMove > ghostTriggerTime;
        bool randomAggression = Random.value < 0.002f;

        if (isStuck || randomAggression)
        {
            EnterGhostMode();
            yield break;
        }

        if (Time.time > lastAttackTime + attackCooldown && CanSeePlayer())
        {
            yield return StartCoroutine(AttackRoutine());
        }
        else
        {
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
    }

    private bool CanSeePlayer()
    {
        if (PlayerController.Instance == null) return false;
        Vector2Int pPos = PlayerController.Instance.GetBlockPosition();

        bool sameRow = pPos.y == currentBlockPosition.y;
        if (!sameRow) return false;

        float dist = Vector2Int.Distance(pPos, currentBlockPosition);
        if (dist > attackRange) return false;

        return true;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        isMoving = false;

        Vector2Int pPos = PlayerController.Instance.GetBlockPosition();
        Vector2Int dirInt = (pPos.x > currentBlockPosition.x) ? Vector2Int.right : Vector2Int.left;
        spriteRenderer.flipX = dirInt.x < 0;

        float timer = 0f;
        Color original = Color.white;

        while (timer < chargeTime)
        {
            if (currentState != EnemyState.Normal) { isAttacking = false; yield break; }

            spriteRenderer.color = (timer % 0.2f < 0.1f) ? Color.red : original;
            timer += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.color = original;

        if (currentState == EnemyState.Normal && fireBreathPrefab != null)
        {
            lastAttackTime = Time.time;
            int actualLength = 0;
            for (int i = 1; i <= fireLengthBlocks; i++)
            {
                Vector2Int checkPos = currentBlockPosition + (dirInt * i);
                if (gridSystem.IsPositionWalkable(checkPos) && !CheckRock(checkPos))
                {
                    actualLength++;
                }
                else break;
            }

            if (actualLength > 0)
            {
                SpawnFire(dirInt, actualLength);
            }
        }

        yield return new WaitForSeconds(fireDuration);
        isAttacking = false;
    }

    private bool CheckRock(Vector2Int targetBlock)
    {
        Vector3 wPos = gridSystem.BlockToWorldPosition(targetBlock.x, targetBlock.y);
        return Physics2D.OverlapCircle(wPos, 0.2f, obstacleLayer);
    }

    private void SpawnFire(Vector2Int dir, int length)
    {
        Vector3 spawnPos = transform.position + (Vector3)(Vector2)dir * (gridSystem.blockSize * 0.5f);
        GameObject fire = Instantiate(fireBreathPrefab, transform.position, Quaternion.identity);

        float angle = dir.x > 0 ? 0 : 180;
        fire.transform.rotation = Quaternion.Euler(0, 0, angle);

        float totalLengthWorld = length * gridSystem.blockSize;
        Vector3 offset = (Vector3)(Vector2)dir * (totalLengthWorld * 0.5f);
        fire.transform.position = transform.position + offset;

        Vector3 scale = fire.transform.localScale;
        scale.x = length;
        fire.transform.localScale = scale;

        Destroy(fire, fireDuration);
    }
}