using UnityEngine;

public class Coleccionable : MonoBehaviour
{
    [SerializeField] private float vidaQueRecupera = 1f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (PlayerStats.Instance.Health < PlayerStats.Instance.MaxHealth)
            {
                PlayerStats.Instance.Heal(vidaQueRecupera);
                Destroy(gameObject);
            }
        }
    }
}