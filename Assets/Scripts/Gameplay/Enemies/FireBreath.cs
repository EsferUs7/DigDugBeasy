using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FireBreath : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage();
            }
        }
    }
}