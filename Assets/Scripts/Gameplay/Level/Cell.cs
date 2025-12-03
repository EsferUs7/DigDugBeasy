using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Cell : MonoBehaviour
{
    [HideInInspector] public int blockX;
    [HideInInspector] public int blockY;
    [HideInInspector] public int cellX;
    [HideInInspector] public int cellY;

    [HideInInspector] public GridSystem gridSystem;

    public string playerTag = "Player";

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        bool isPlayer = false;
        if (!string.IsNullOrEmpty(playerTag))
            isPlayer = other.CompareTag(playerTag);

        if (!isPlayer)
        {
            if (other.GetComponent<PlayerController>() != null) isPlayer = true;
        }

        if (!isPlayer) return;

        if (gridSystem == null)
        {
            Debug.LogWarning("Cell.OnTriggerEnter2D: gridSystem is null for cell " + name);
            return;
        }

        gridSystem.RequestDig(blockX, blockY, cellX, cellY, gameObject);
    }
}
