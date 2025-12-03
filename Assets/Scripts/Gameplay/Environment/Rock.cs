using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class Rock : MonoBehaviour
{
    [Header("Rock Settings")]
    public float wobbleDuration = 1.2f;
    public float fallSpeed = 4f;
    public LayerMask crushLayerMask;

    [Header("References")]
    public GridSystem gridSystem;

    private Vector2Int currentBlockPos;
    private bool isFalling = false;
    private bool isWobbling = false;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider.isTrigger = false;
    }

    public void Initialize(Vector2Int startPos, GridSystem grid)
    {
        this.gridSystem = grid;
        this.currentBlockPos = startPos;

        transform.position = grid.BlockToWorldPosition(startPos.x, startPos.y);
    }

    private void Update()
    {
        if (isFalling || isWobbling || gridSystem == null) return;

        if (ShouldFall())
        {
            StartCoroutine(FallRoutine());
        }
    }

    private bool ShouldFall()
    {
        int checkX = currentBlockPos.x;
        int checkY = currentBlockPos.y - 1;

        if (checkY < 0) return false;

        bool isSpaceBelow = gridSystem.IsPositionWalkable(checkX, checkY);

        if (isSpaceBelow)
        {
            if (PlayerController.Instance != null)
            {
                Vector2Int playerPos = PlayerController.Instance.GetBlockPosition();
                if (playerPos.x == checkX && playerPos.y == checkY)
                {
                    return false;
                }
            }
            return true;
        }

        return false;
    }

    private IEnumerator FallRoutine()
    {
        isWobbling = true;

        float timer = 0f;
        Vector3 initialPos = transform.position;

        while (timer < wobbleDuration)
        {
            float xOffset = Random.Range(-0.05f, 0.05f);
            transform.position = initialPos + new Vector3(xOffset, 0, 0);

            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = initialPos;
        isWobbling = false;
        isFalling = true;

        boxCollider.isTrigger = true;

        while (true)
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            Vector2Int newBlockPos = gridSystem.WorldToBlockPosition(transform.position);

            if (newBlockPos.y != currentBlockPos.y)
            {
                currentBlockPos = newBlockPos;

                int nextY = currentBlockPos.y - 1;

                if (nextY < 0 || !gridSystem.IsPositionWalkable(currentBlockPos.x, nextY))
                {
                    break;
                }
            }

            yield return null;
        }

        Vector3 finalPos = gridSystem.BlockToWorldPosition(currentBlockPos.x, currentBlockPos.y);
        transform.position = finalPos;

        Debug.Log("Rock crumbled!");

        yield return new WaitForSeconds(0.2f);

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isFalling) return;

        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(ScoreType.RockCrush, "EnemyCrushed");

            enemy.Die();
        }

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage();
        }
    }
}