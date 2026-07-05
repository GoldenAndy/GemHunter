using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerVida : MonoBehaviour, IDamageable
{
    [Header("Invulnerabilidad")]
    [SerializeField] private float duracionInvulnerabilidad = 2f;
    [SerializeField] private float intervaloParpadeo = 0.1f;

    [Header("Reacción al daño")]
    [Tooltip("Tiempo durante el cual el jugador no podrá controlar el movimiento.")]
    [SerializeField] private float duracionBloqueoMovimiento = 0.25f;

    private bool esInvulnerable;
    private bool estaMuerto;

    private SpriteRenderer spriteRenderer;
    private PlayerMovementTest movimiento;
    private PlayerStats playerStats;

    private Coroutine coroutineInvulnerabilidad;

    public bool EsInvulnerable => esInvulnerable;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        movimiento = GetComponent<PlayerMovementTest>();
        playerStats = GetComponent<PlayerStats>();

        if (playerStats == null)
        {
            playerStats = PlayerStats.Instance;
        }

        if (playerStats == null)
        {
            Debug.LogError(
                "No se encontró PlayerStats. El jugador no podrá recibir daño.",
                this
            );
        }
    }

    public void RecibirDano(DamageInfo damageInfo)
    {
        if (estaMuerto)
            return;

        if (esInvulnerable)
            return;

        if (damageInfo.dano <= 0)
            return;

        if (playerStats == null)
            return;

        /*
         * PlayerStats descuenta la vida y avisa automáticamente
         * al HealthBarController para actualizar los corazones.
         */
        playerStats.TakeDamage(damageInfo.dano);

        Debug.Log(
            $"{gameObject.name} recibió {damageInfo.dano} de daño. " +
            $"Vida actual: {playerStats.Health}/{playerStats.MaxHealth}"
        );

        if (movimiento != null)
        {
            movimiento.RecibirImpacto(
                damageInfo.direccion,
                damageInfo.fuerzaEmpuje,
                duracionBloqueoMovimiento
            );
        }

        if (playerStats.Health <= 0f)
        {
            Morir();
            return;
        }

        if (coroutineInvulnerabilidad != null)
        {
            StopCoroutine(coroutineInvulnerabilidad);
        }

        coroutineInvulnerabilidad = StartCoroutine(
            ActivarInvulnerabilidad()
        );
    }

    private IEnumerator ActivarInvulnerabilidad()
    {
        esInvulnerable = true;

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionInvulnerabilidad)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return new WaitForSeconds(intervaloParpadeo);

            tiempoTranscurrido += intervaloParpadeo;
        }

        spriteRenderer.enabled = true;
        esInvulnerable = false;
        coroutineInvulnerabilidad = null;
    }

    private void Morir()
    {
        estaMuerto = true;
        esInvulnerable = true;

        spriteRenderer.enabled = true;

        Debug.Log("El jugador se quedó sin vida.");

        // Posteriormente:
        // - Animación de muerte.
        // - Pantalla de derrota.
        // - Reinicio del nivel.
        // - Desactivación de controles.
    }

    private void OnDisable()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }
}