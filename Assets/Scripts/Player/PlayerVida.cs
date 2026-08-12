using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Sonidos")]
    [SerializeField] private AudioClip sonidoDano;
    [SerializeField] private AudioClip sonidoMuerte;

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

        playerStats.TakeDamage(
            damageInfo.dano
        );

        Debug.Log(
            $"{gameObject.name} recibió {damageInfo.dano} de daño. " +
            $"Vida actual: {playerStats.Health}/{playerStats.MaxHealth}"
        );

        // =====================================================
        // MUERTE
        // =====================================================

        if (playerStats.Health <= 0f)
        {
            if (AudioManager.Instance != null &&
                sonidoMuerte != null)
            {
                AudioManager.Instance.ReproducirSFX(
                    sonidoMuerte
                );
            }

            Morir();
            return;
        }

        // =====================================================
        // DAÑO NORMAL
        // =====================================================

        if (AudioManager.Instance != null &&
            sonidoDano != null)
        {
            AudioManager.Instance.ReproducirSFX(
                sonidoDano
            );
        }

        if (movimiento != null)
        {
            movimiento.RecibirImpacto(
                damageInfo.direccion,
                damageInfo.fuerzaEmpuje,
                duracionBloqueoMovimiento
            );
        }

        if (coroutineInvulnerabilidad != null)
        {
            StopCoroutine(
                coroutineInvulnerabilidad
            );
        }

        coroutineInvulnerabilidad =
            StartCoroutine(
                ActivarInvulnerabilidad()
            );
    }

    private IEnumerator ActivarInvulnerabilidad()
    {
        esInvulnerable = true;

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido <
               duracionInvulnerabilidad)
        {
            spriteRenderer.enabled =
                !spriteRenderer.enabled;

            yield return new WaitForSeconds(
                intervaloParpadeo
            );

            tiempoTranscurrido +=
                intervaloParpadeo;
        }

        spriteRenderer.enabled = true;
        esInvulnerable = false;

        coroutineInvulnerabilidad = null;
    }

    private void Morir()
    {
        if (estaMuerto)
            return;

        estaMuerto = true;
        esInvulnerable = true;

        if (coroutineInvulnerabilidad != null)
        {
            StopCoroutine(
                coroutineInvulnerabilidad
            );

            coroutineInvulnerabilidad =
                null;
        }

        spriteRenderer.enabled = true;

        if (movimiento != null)
        {
            movimiento.ReproducirMuerte();
        }

        Debug.Log(
            "El jugador se quedó sin vida."
        );

        SceneManager.LoadScene(
            "MenuPerder"
        );
    }

    private void OnDisable()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }
}