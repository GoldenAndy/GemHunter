using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemigoContacto : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private int dano = 1;
    [SerializeField] private float fuerzaEmpuje = 6f;

    [Header("Filtros")]
    [SerializeField] private LayerMask capasObjetivo;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        IntentarDañar(
            collision.collider,
            collision.GetContact(0).point
        );
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        IntentarDañar(
            collision.collider,
            collision.GetContact(0).point
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Vector2 puntoImpacto = other.ClosestPoint(transform.position);

        IntentarDañar(other, puntoImpacto);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Vector2 puntoImpacto = other.ClosestPoint(transform.position);

        IntentarDañar(other, puntoImpacto);
    }

    private void IntentarDañar(
        Collider2D other,
        Vector2 puntoImpacto)
    {
        if (other == null)
            return;

        /*
        * Si el Collider pertenece a la espada o está dentro
        * de su jerarquía, el enemigo debe ignorarlo.
        */
        EspadaHitbox espada =
            other.GetComponentInParent<EspadaHitbox>();

        if (espada != null)
            return;

        if (!EstaEnCapaObjetivo(other.gameObject.layer))
            return;

        IDamageable damageable = BuscarDamageable(other);

        if (damageable == null)
            return;

        Vector2 centroObjetivo = other.bounds.center;
        Vector2 centroEnemigo = transform.position;

        Vector2 direccionEmpuje =
            (centroObjetivo - centroEnemigo).normalized;

        if (direccionEmpuje == Vector2.zero)
        {
            direccionEmpuje = Vector2.right;
        }

        DamageInfo damageInfo = new DamageInfo(
            dano,
            gameObject,
            puntoImpacto,
            direccionEmpuje,
            fuerzaEmpuje
        );

        damageable.RecibirDano(damageInfo);
    }

    private IDamageable BuscarDamageable(Collider2D other)
    {
        MonoBehaviour[] componentes =
            other.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour componente in componentes)
        {
            if (componente is IDamageable damageable)
            {
                return damageable;
            }
        }

        return null;
    }

    private bool EstaEnCapaObjetivo(int layer)
    {
        return (capasObjetivo.value & (1 << layer)) != 0;
    }
}