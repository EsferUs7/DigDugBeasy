using UnityEngine;
using System.Collections;

public class PumpController : MonoBehaviour
{
    [Header("Налаштування")]
    public float range = 3f;
    public float shootSpeed = 15f;

    [Header("Колізії")]
    [Tooltip("Шари об'єктів, які зупиняють насос (Земля, Камені, Стіни)")]
    public LayerMask obstacleLayer;

    [Header("Візуалізація")]
    public GameObject pumpVisual;

    [Header("Референси")]
    public PlayerController playerController;
    public GridSystem gridSystem;

    private enum PumpState { Idle, Shooting, Attached, Retracting }
    private PumpState currentState = PumpState.Idle;

    private EnemyBase attachedEnemy;
    private Vector3 shootDirection;
    private Vector3 originalLocalScale;

    private void Start()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (gridSystem == null) gridSystem = FindFirstObjectByType<GridSystem>();

        if (pumpVisual != null)
        {
            pumpVisual.SetActive(false);
            originalLocalScale = pumpVisual.transform.localScale;
        }

        if (obstacleLayer == 0)
            obstacleLayer = LayerMask.GetMask("Default", "Rock", "Ground");
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;

        switch (currentState)
        {
            case PumpState.Idle:
                if (Input.GetKeyDown(KeyCode.Space) && playerController.CanUsePump())
                {
                    StartCoroutine(ShootRoutine());
                }
                break;

            case PumpState.Attached:
                HandlePumping();
                break;
        }
    }

    private void HandlePumping()
    {
        if (attachedEnemy == null || attachedEnemy.gameObject == null)
        {
            ForceRetractPump();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            attachedEnemy.PumpInflate();
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveY) > 0.1f)
        {
            StartCoroutine(RetractRoutine());
        }
    }

    private IEnumerator ShootRoutine()
    {
        currentState = PumpState.Shooting;

        Vector2Int dirInt = playerController.GetFacingDirection();
        shootDirection = new Vector3(dirInt.x, dirInt.y, 0);

        if (pumpVisual != null)
        {
            pumpVisual.SetActive(true);
            pumpVisual.transform.localPosition = Vector3.zero;

            float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
            pumpVisual.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        float traveledDistance = 0f;
        float maxDistanceWorld = range * (gridSystem ? gridSystem.blockSize : 1f);
        Vector3 startPos = transform.position;

        while (traveledDistance < maxDistanceWorld)
        {
            float step = shootSpeed * Time.deltaTime;
            traveledDistance += step;

            Vector3 currentTipPos = startPos + (shootDirection * traveledDistance);

            if (pumpVisual != null)
                pumpVisual.transform.position = currentTipPos;

            Collider2D enemyHit = CheckEnemy(currentTipPos);
            if (enemyHit != null)
            {
                EnemyBase enemy = enemyHit.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    AttachToEnemy(enemy);
                    yield break;
                }
            }

            if (CheckObstacle(currentTipPos))
            {
                break;
            }

            yield return null;
        }

        yield return StartCoroutine(RetractRoutine());
    }

    private void AttachToEnemy(EnemyBase enemy)
    {
        currentState = PumpState.Attached;
        attachedEnemy = enemy;

        enemy.StartInflation();

        if (pumpVisual != null)
        {
            pumpVisual.transform.position = enemy.transform.position;
        }
    }

    private IEnumerator RetractRoutine()
    {
        currentState = PumpState.Retracting;

        if (attachedEnemy != null)
        {
            attachedEnemy.StopInflation();
        }

        yield return new WaitForSeconds(0.05f);

        ResetPump();
    }

    private void ResetPump()
    {
        currentState = PumpState.Idle;
        attachedEnemy = null;

        if (pumpVisual != null)
        {
            pumpVisual.SetActive(false);
            pumpVisual.transform.localPosition = Vector3.zero;
            pumpVisual.transform.localScale = originalLocalScale;
        }
    }

    private Collider2D CheckEnemy(Vector3 checkPos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, 0.15f);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (pumpVisual != null && hit.gameObject == pumpVisual) continue;

            if (hit.GetComponent<EnemyBase>() != null) return hit;
        }
        return null;
    }

    private bool CheckObstacle(Vector3 checkPos)
    {
        return Physics2D.OverlapCircle(checkPos, 0.1f, obstacleLayer);
    }

    public bool IsPumpActive() => currentState != PumpState.Idle;

    public void ForceRetractPump()
    {
        StopAllCoroutines();
        if (attachedEnemy != null) attachedEnemy.StopInflation();
        ResetPump();
    }
}