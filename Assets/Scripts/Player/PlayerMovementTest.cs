using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerMovementTest : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 5.5f;
    [SerializeField] private float acceleration = 35f;
    [SerializeField] private float deceleration = 45f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float groundIgnoreAfterJumpTime = 0.15f;

    [Header("Ataque")]
    [SerializeField] private KeyCode attackKey = KeyCode.Z;
    [SerializeField] private float minimumAttackTime = 0.15f;

    [Header("Hitbox de espada")]
    [SerializeField] private Transform hitboxPivot;
    [SerializeField] private Collider2D espadaHitboxCollider;
    [SerializeField] private EspadaHitbox espadaHitbox;
    [SerializeField] private float hitboxMaxActiveTime = 0.25f;

    [Header("Collider del jugador")]
    [SerializeField] private Collider2D cuerpoCollider;

    [Header("Gravedad")]
    [SerializeField] private float normalGravity = 3f;
    [SerializeField] private float fallGravityMultiplier = 1.8f;
    [SerializeField] private float maxFallSpeed = 14f;

    // ============================================================
    // DETECCIÓN DE SUELO
    // ============================================================

    [Header("Detección de suelo")]

    [Tooltip("Layers que serán consideradas como suelo.")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip(
        "Cantidad mínima de componente vertical que debe tener " +
        "la normal del contacto para considerarlo suelo. " +
        "Un valor bajo permite reconocer mejor los bordes.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float normalMinimaSuelo = 0.15f;

    [Tooltip(
        "Pequeña tolerancia para considerar contactos cercanos " +
        "a la mitad inferior del collider como suelo.")]
    [SerializeField] private float toleranciaContactoSuelo = 0.03f;

    // ============================================================
    // COMPONENTES
    // ============================================================

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private Vector2 cuerpoColliderOffsetOriginal;

    // ============================================================
    // CONTACTOS FÍSICOS
    // ============================================================

    /*
     * Reutilizamos siempre el mismo array.
     *
     * Aquí Unity colocará los puntos donde el collider del jugador
     * está tocando otros colliders.
     */
    private readonly ContactPoint2D[] contactosSuelo =
        new ContactPoint2D[16];

    /*
     * El filtro garantiza que solamente nos interesen contactos
     * con objetos pertenecientes a Ground Layer.
     */
    private ContactFilter2D filtroSuelo;

    // ============================================================
    // ESTADO
    // ============================================================

    private float horizontalInput;
    private bool isRunning;
    private bool isGrounded;
    private bool mirandoDerecha = true;

    private bool canInterruptAttack = true;
    private float attackInterruptCounter;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float groundIgnoreCounter;

    private float hitboxActiveCounter;
    private bool isSwordHitboxActive;

    private float bloqueoMovimientoPorDano;
    private bool recibiendoDano;
    private bool estaMuerto;

    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (cuerpoCollider == null)
        {
            cuerpoCollider = GetComponent<Collider2D>();
        }

        if (cuerpoCollider != null)
        {
            cuerpoColliderOffsetOriginal =
                cuerpoCollider.offset;

            /*
             * El collider corporal NO debe ser Trigger.
             *
             * Necesitamos contactos físicos reales para detectar
             * correctamente el suelo.
             */
            if (cuerpoCollider.isTrigger)
            {
                Debug.LogWarning(
                    "El collider corporal tiene Is Trigger activado. " +
                    "Desactívalo para que la detección de suelo funcione.",
                    this
                );
            }
        }
        else
        {
            Debug.LogError(
                "No se encontró el collider corporal del jugador.",
                this
            );
        }

        /*
         * Configuramos el filtro que utilizaremos para consultar
         * únicamente contactos con Ground Layer.
         */
        filtroSuelo = new ContactFilter2D();
        filtroSuelo.SetLayerMask(groundLayer);
        filtroSuelo.useTriggers = false;

        rb.gravityScale = normalGravity;
        rb.freezeRotation = true;

        DesactivarEspadaHitbox();
        ActualizarDireccionHitbox();
    }

    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        if (estaMuerto)
            return;

        ActualizarBloqueoPorDano();

        if (!recibiendoDano)
        {
            LeerInput();
            ControlarSalto();
            ControlarAtaque();
            GirarPersonaje();
        }
        else
        {
            horizontalInput = 0f;
            isRunning = false;
        }

        /*
         * Ya NO usamos GroundCheck.
         *
         * Ahora consultamos directamente los contactos físicos
         * del collider del jugador.
         */
        DetectarSuelo();

        ActualizarTiempoHitbox();
        ActualizarAnimator();
    }

    // ============================================================
    // FIXED UPDATE
    // ============================================================

    private void FixedUpdate()
    {
        if (estaMuerto)
            return;

        if (!recibiendoDano)
        {
            MoverPersonaje();
        }

        AplicarGravedadMejorada();
    }

    // ============================================================
    // DAÑO
    // ============================================================

    private void ActualizarBloqueoPorDano()
    {
        if (!recibiendoDano)
            return;

        bloqueoMovimientoPorDano -= Time.deltaTime;

        bool estaEnHurt =
            animator.GetCurrentAnimatorStateInfo(0)
                .IsName("Player_Hurt");

        bool estaEntrandoEnHurt =
            animator.IsInTransition(0) &&
            animator.GetNextAnimatorStateInfo(0)
                .IsName("Player_Hurt");

        /*
         * El control vuelve únicamente cuando:
         *
         * 1. Terminó el tiempo mínimo de bloqueo.
         * 2. El Animator ya salió de Player_Hurt.
         */
        if (bloqueoMovimientoPorDano <= 0f &&
            !estaEnHurt &&
            !estaEntrandoEnHurt)
        {
            bloqueoMovimientoPorDano = 0f;
            recibiendoDano = false;

            canInterruptAttack = true;

            animator.SetBool(
                "CanInterruptAttack",
                true
            );
        }
    }

    public void RecibirImpacto(
        Vector2 direccion,
        float fuerzaEmpuje,
        float duracionBloqueo)
    {
        recibiendoDano = true;
        bloqueoMovimientoPorDano = duracionBloqueo;

        horizontalInput = 0f;
        isRunning = false;

        /*
         * Durante la animación de daño bloqueamos las
         * transiciones de Any State hacia Jump y Fall.
         */
        canInterruptAttack = false;
        attackInterruptCounter = 0f;

        animator.ResetTrigger("Attack");

        animator.SetBool(
            "CanInterruptAttack",
            false
        );

        DesactivarEspadaHitbox();

        float direccionHorizontal =
            direccion.x;

        if (Mathf.Abs(direccionHorizontal) < 0.01f)
        {
            direccionHorizontal =
                mirandoDerecha ? -1f : 1f;
        }
        else
        {
            direccionHorizontal =
                Mathf.Sign(direccionHorizontal);
        }

        float fuerzaVertical =
            fuerzaEmpuje * 0.45f;

        rb.velocity = new Vector2(
            direccionHorizontal * fuerzaEmpuje,
            fuerzaVertical
        );

        animator.SetTrigger("Hurt");

        Debug.Log(
            $"Hurt activado en {gameObject.name}. " +
            $"Velocidad: {rb.velocity}. " +
            $"Animator: {animator.runtimeAnimatorController.name}"
        );
    }

    // ============================================================
    // MUERTE
    // ============================================================

    public void ReproducirMuerte()
    {
        if (estaMuerto)
            return;

        estaMuerto = true;
        recibiendoDano = false;

        horizontalInput = 0f;
        isRunning = false;

        canInterruptAttack = false;
        attackInterruptCounter = 0f;

        DesactivarEspadaHitbox();

        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Hurt");

        animator.SetBool(
            "CanInterruptAttack",
            false
        );

        rb.velocity =
            new Vector2(0f, rb.velocity.y);

        animator.SetTrigger("Death");

        Debug.Log(
            "Animación de muerte activada."
        );
    }

    // ============================================================
    // INPUT
    // ============================================================

    private void LeerInput()
    {
        horizontalInput =
            Input.GetAxisRaw("Horizontal");

        bool isMoving =
            Mathf.Abs(horizontalInput) > 0.01f;

        isRunning =
            isMoving &&
            (
                Input.GetKey(KeyCode.LeftShift) ||
                Input.GetKey(KeyCode.RightShift)
            );

        if (Input.GetKeyDown(KeyCode.X))
        {
            jumpBufferCounter =
                jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -=
                Time.deltaTime;
        }
    }

    // ============================================================
    // DETECCIÓN DE SUELO POR CONTACTO REAL
    // ============================================================

    private void DetectarSuelo()
    {
        /*
         * Si por algún motivo no existe el collider,
         * no podemos detectar suelo.
         */
        if (cuerpoCollider == null)
        {
            isGrounded = false;
            return;
        }

        /*
         * Después de saltar ignoramos brevemente los contactos.
         *
         * Esto evita que durante los primeros frames del salto
         * Unity todavía considere el contacto anterior con el suelo.
         */
        if (groundIgnoreCounter > 0f)
        {
            groundIgnoreCounter -=
                Time.deltaTime;

            isGrounded = false;

            coyoteTimeCounter -=
                Time.deltaTime;

            return;
        }

        /*
         * Obtenemos los contactos FÍSICOS REALES del collider.
         *
         * El filtro ya limita los resultados a objetos
         * pertenecientes a Ground Layer.
         */
        int cantidadContactos =
            cuerpoCollider.GetContacts(
                filtroSuelo,
                contactosSuelo
            );

        bool touchingGround = false;

        /*
         * Centro del collider en coordenadas del mundo.
         *
         * Lo utilizamos para distinguir entre:
         *
         * - contacto debajo del personaje = posible suelo
         * - contacto encima = techo
         */
        float centroColliderY =
            cuerpoCollider.bounds.center.y;

        for (int i = 0; i < cantidadContactos; i++)
        {
            ContactPoint2D contacto =
                contactosSuelo[i];

            /*
             * El contacto tiene que ocurrir en la mitad inferior
             * del collider.
             *
             * Esto evita considerar el techo como suelo.
             */
            bool contactoEnParteInferior =
                contacto.point.y <=
                centroColliderY +
                toleranciaContactoSuelo;

            /*
             * Una pared completamente vertical tendría una normal
             * prácticamente horizontal:
             *
             * normal.y ≈ 0
             *
             * Por eso NO la consideramos suelo.
             *
             * Un borde o esquina puede producir una normal diagonal,
             * así que aceptamos valores pequeños de Y.
             */
            bool tieneComponenteVertical =
                Mathf.Abs(contacto.normal.y) >=
                normalMinimaSuelo;

            /*
             * Además, solo tiene sentido quedar Grounded si
             * no estamos desplazándonos claramente hacia arriba.
             */
            bool noEstaSubiendo =
                rb.velocity.y <= 0.05f;

            if (contactoEnParteInferior &&
                tieneComponenteVertical &&
                noEstaSubiendo)
            {
                touchingGround = true;
                break;
            }
        }

        isGrounded = touchingGround;

        if (isGrounded)
        {
            coyoteTimeCounter =
                coyoteTime;
        }
        else
        {
            coyoteTimeCounter -=
                Time.deltaTime;
        }
    }

    // ============================================================
    // SALTO
    // ============================================================

    private void ControlarSalto()
    {
        if (jumpBufferCounter > 0f &&
            coyoteTimeCounter > 0f)
        {
            rb.velocity =
                new Vector2(
                    rb.velocity.x,
                    jumpForce
                );

            isGrounded = false;

            groundIgnoreCounter =
                groundIgnoreAfterJumpTime;

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        if (Input.GetKeyUp(KeyCode.X) &&
            rb.velocity.y > 0f)
        {
            rb.velocity =
                new Vector2(
                    rb.velocity.x,
                    rb.velocity.y *
                    jumpCutMultiplier
                );
        }
    }

    // ============================================================
    // ATAQUE
    // ============================================================

    private void ControlarAtaque()
    {
        if (Input.GetKeyDown(attackKey))
        {
            animator.SetTrigger("Attack");

            canInterruptAttack = false;

            attackInterruptCounter =
                minimumAttackTime;
        }

        if (!canInterruptAttack)
        {
            attackInterruptCounter -=
                Time.deltaTime;

            if (attackInterruptCounter <= 0f)
            {
                canInterruptAttack = true;
            }
        }
    }

    private void ActualizarTiempoHitbox()
    {
        if (!isSwordHitboxActive)
            return;

        hitboxActiveCounter -=
            Time.deltaTime;

        if (hitboxActiveCounter <= 0f)
        {
            DesactivarEspadaHitbox();
        }
    }

    // ============================================================
    // MOVIMIENTO
    // ============================================================

    private void MoverPersonaje()
    {
        float currentSpeed =
            isRunning
                ? runSpeed
                : walkSpeed;

        float targetSpeed =
            horizontalInput *
            currentSpeed;

        float speedDifference =
            targetSpeed -
            rb.velocity.x;

        float movementRate =
            Mathf.Abs(targetSpeed) > 0.01f
                ? acceleration
                : deceleration;

        float movement =
            speedDifference *
            movementRate;

        rb.AddForce(
            Vector2.right *
            movement
        );

        if (Mathf.Abs(rb.velocity.x) >
            currentSpeed)
        {
            rb.velocity =
                new Vector2(
                    Mathf.Sign(rb.velocity.x) *
                    currentSpeed,
                    rb.velocity.y
                );
        }
    }

    // ============================================================
    // GRAVEDAD
    // ============================================================

    private void AplicarGravedadMejorada()
    {
        if (rb.velocity.y < 0f)
        {
            rb.gravityScale =
                normalGravity *
                fallGravityMultiplier;
        }
        else
        {
            rb.gravityScale =
                normalGravity;
        }

        if (rb.velocity.y <
            -maxFallSpeed)
        {
            rb.velocity =
                new Vector2(
                    rb.velocity.x,
                    -maxFallSpeed
                );
        }
    }

    // ============================================================
    // DIRECCIÓN
    // ============================================================

    private void GirarPersonaje()
    {
        if (horizontalInput > 0.01f)
        {
            mirandoDerecha = true;
            spriteRenderer.flipX = false;
        }
        else if (horizontalInput < -0.01f)
        {
            mirandoDerecha = false;
            spriteRenderer.flipX = true;
        }

        ActualizarDireccionHitbox();
    }

    private void ActualizarDireccionHitbox()
    {
        if (hitboxPivot != null)
        {
            hitboxPivot.localScale =
                mirandoDerecha
                    ? new Vector3(1f, 1f, 1f)
                    : new Vector3(-1f, 1f, 1f);
        }

        /*
         * El collider del cuerpo conserva exactamente
         * su forma y tamaño originales.
         *
         * Únicamente invertimos el Offset X al mirar
         * hacia la izquierda, tal como hacía el
         * script original.
         */
        if (cuerpoCollider != null)
        {
            cuerpoCollider.offset =
                mirandoDerecha
                    ? cuerpoColliderOffsetOriginal
                    : new Vector2(
                        -cuerpoColliderOffsetOriginal.x,
                        cuerpoColliderOffsetOriginal.y
                    );
        }
    }

    // ============================================================
    // ANIMATOR
    // ============================================================

    private void ActualizarAnimator()
    {
        animator.SetFloat(
            "Speed",
            Mathf.Abs(horizontalInput)
        );

        animator.SetBool(
            "IsRunning",
            isRunning
        );

        animator.SetBool(
            "IsGrounded",
            isGrounded
        );

        animator.SetFloat(
            "VerticalSpeed",
            rb.velocity.y
        );

        animator.SetBool(
            "CanInterruptAttack",
            canInterruptAttack
        );
    }

    // ============================================================
    // HITBOX DE ESPADA
    // ============================================================

    public void ActivarEspadaHitbox()
    {
        if (estaMuerto)
            return;

        if (espadaHitbox != null)
        {
            espadaHitbox.ReiniciarGolpes();
        }

        if (espadaHitboxCollider != null)
        {
            espadaHitboxCollider.enabled =
                true;
        }

        isSwordHitboxActive = true;

        hitboxActiveCounter =
            hitboxMaxActiveTime;
    }

    public void DesactivarEspadaHitbox()
    {
        if (espadaHitboxCollider != null)
        {
            espadaHitboxCollider.enabled =
                false;
        }

        isSwordHitboxActive = false;
        hitboxActiveCounter = 0f;
    }
}