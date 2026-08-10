using System;
using UnityEngine;

public class EnemigoVida : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 3;
    [SerializeField] private bool destruirAlMorir = true;

    /*
     * Otros scripts pueden escuchar este evento.
     * Por ejemplo, MurcielagoEnemy lo utiliza
     * para retroceder al recibir un espadazo.
     */
    public event Action<DamageInfo> OnDanoRecibido;

    private int vidaActual;
    private Rigidbody2D rb;
    private bool estaMuerto;

    private void Awake()
    {
        vidaActual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();
    }

    public void RecibirDano(DamageInfo damageInfo)
    {
        if (estaMuerto)
            return;

        vidaActual -= damageInfo.dano;

        Debug.Log(
            $"{gameObject.name} recibió {damageInfo.dano} de daño. " +
            $"Vida actual: {vidaActual}"
        );

        /*
         * Avisamos al controlador especial del enemigo
         * de que acaba de recibir un golpe.
         */
        OnDanoRecibido?.Invoke(damageInfo);

        AplicarEmpuje(damageInfo);

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    private void AplicarEmpuje(DamageInfo damageInfo)
    {
        if (rb == null)
            return;

        if (damageInfo.fuerzaEmpuje <= 0f)
            return;

        rb.velocity = new Vector2(
            damageInfo.direccion.x * damageInfo.fuerzaEmpuje,
            rb.velocity.y
        );
    }

    private void Morir()
    {
        if (estaMuerto)
            return;

        estaMuerto = true;

        Debug.Log(
            $"{gameObject.name} fue derrotado."
        );

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