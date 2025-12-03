using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class Fruit : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(ScoreType.FruitCollect, "Banana");
        }

        Destroy(gameObject);
    }
}