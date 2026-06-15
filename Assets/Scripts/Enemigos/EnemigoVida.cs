using UnityEngine;

public class EnemigoVida : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 3;
    [SerializeField] private bool destruirAlMorir = true;

    private int vidaActual;
    private Rigidbody2D rb;

    private void Awake()
    {
        vidaActual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();
    }

    public void RecibirDano(DamageInfo damageInfo)
    {
        vidaActual -= damageInfo.dano;

        Debug.Log($"{gameObject.name} recibió {damageInfo.dano} de daño. Vida actual: {vidaActual}");

        AplicarEmpuje(damageInfo);

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    private void AplicarEmpuje(DamageInfo damageInfo)
    {
        if (rb == null) return;
        if (damageInfo.fuerzaEmpuje <= 0f) return;

        rb.velocity = new Vector2(
            damageInfo.direccion.x * damageInfo.fuerzaEmpuje,
            rb.velocity.y
        );
    }

    private void Morir()
    {
        Debug.Log($"{gameObject.name} fue derrotado.");

        if (destruirAlMorir)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}