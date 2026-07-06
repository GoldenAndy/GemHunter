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

    [Header("Detección de suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.08f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private Vector2 cuerpoColliderOffsetOriginal;

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
            cuerpoColliderOffsetOriginal = cuerpoCollider.offset;
        }

        rb.gravityScale = normalGravity;
        rb.freezeRotation = true;

        DesactivarEspadaHitbox();
        ActualizarDireccionHitbox();
    }

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

        DetectarSuelo();
        ActualizarTiempoHitbox();
        ActualizarAnimator();
    }

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

    private void ActualizarBloqueoPorDano()
    {
        if (!recibiendoDano)
            return;

        bloqueoMovimientoPorDano -= Time.deltaTime;

        bool estaEnHurt =
            animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Hurt");

        bool estaEntrandoEnHurt =
            animator.IsInTransition(0) &&
            animator.GetNextAnimatorStateInfo(0).IsName("Player_Hurt");

        /*
        * El control vuelve únicamente cuando:
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
            animator.SetBool("CanInterruptAttack", true);
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
        * Durante la animación de daño bloqueamos las transiciones
        * de Any State hacia Jump y Fall.
        */
        canInterruptAttack = false;
        attackInterruptCounter = 0f;

        // Cancela cualquier ataque pendiente.
        animator.ResetTrigger("Attack");

        // Actualizamos el parámetro inmediatamente.
        animator.SetBool("CanInterruptAttack", false);

        // La espada deja de hacer daño al recibir el golpe.
        DesactivarEspadaHitbox();

        float direccionHorizontal = direccion.x;

        if (Mathf.Abs(direccionHorizontal) < 0.01f)
        {
            direccionHorizontal = mirandoDerecha ? -1f : 1f;
        }
        else
        {
            direccionHorizontal = Mathf.Sign(direccionHorizontal);
        }

        float fuerzaVertical = fuerzaEmpuje * 0.45f;

        // Aplica el retroceso.
        rb.velocity = new Vector2(
            direccionHorizontal * fuerzaEmpuje,
            fuerzaVertical
        );

        // Activa finalmente la animación de daño.
        animator.SetTrigger("Hurt");

        Debug.Log(
            $"Hurt activado en {gameObject.name}. " +
            $"Velocidad: {rb.velocity}. " +
            $"Animator: {animator.runtimeAnimatorController.name}"
        );
    }


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

        // La espada deja de estar activa al morir.
        DesactivarEspadaHitbox();

        // Cancelamos animaciones pendientes.
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Hurt");

        // Bloqueamos transiciones normales desde Any State.
        animator.SetBool("CanInterruptAttack", false);

        // Detenemos el movimiento horizontal.
        rb.velocity = new Vector2(0f, rb.velocity.y);

    // Activamos la animación final.
    animator.SetTrigger("Death");

    Debug.Log("Animación de muerte activada.");
}


    private void LeerInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        bool isMoving = Mathf.Abs(horizontalInput) > 0.01f;

        isRunning = isMoving &&
                    (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        if (Input.GetKeyDown(KeyCode.X))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void DetectarSuelo()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        if (groundIgnoreCounter > 0f)
        {
            groundIgnoreCounter -= Time.deltaTime;
            isGrounded = false;
            coyoteTimeCounter -= Time.deltaTime;
            return;
        }

        RaycastHit2D hit = Physics2D.BoxCast(
            groundCheck.position,
            new Vector2(0.25f, 0.05f),
            0f,
            Vector2.down,
            groundCheckRadius,
            groundLayer
        );

        bool touchingGround =
            hit.collider != null &&
            !hit.transform.IsChildOf(transform);

        isGrounded = touchingGround && rb.velocity.y <= 0.05f;

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void ControlarSalto()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);

            isGrounded = false;
            groundIgnoreCounter = groundIgnoreAfterJumpTime;

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        if (Input.GetKeyUp(KeyCode.X) && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(
                rb.velocity.x,
                rb.velocity.y * jumpCutMultiplier
            );
        }
    }

    private void ControlarAtaque()
    {
        if (Input.GetKeyDown(attackKey))
        {
            animator.SetTrigger("Attack");

            canInterruptAttack = false;
            attackInterruptCounter = minimumAttackTime;
        }

        if (!canInterruptAttack)
        {
            attackInterruptCounter -= Time.deltaTime;

            if (attackInterruptCounter <= 0f)
            {
                canInterruptAttack = true;
            }
        }
    }

    private void ActualizarTiempoHitbox()
    {
        if (!isSwordHitboxActive) return;

        hitboxActiveCounter -= Time.deltaTime;

        if (hitboxActiveCounter <= 0f)
        {
            DesactivarEspadaHitbox();
        }
    }

    private void MoverPersonaje()
    {
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        float targetSpeed = horizontalInput * currentSpeed;

        float speedDifference = targetSpeed - rb.velocity.x;

        float movementRate = Mathf.Abs(targetSpeed) > 0.01f
            ? acceleration
            : deceleration;

        float movement = speedDifference * movementRate;

        rb.AddForce(Vector2.right * movement);

        if (Mathf.Abs(rb.velocity.x) > currentSpeed)
        {
            rb.velocity = new Vector2(
                Mathf.Sign(rb.velocity.x) * currentSpeed,
                rb.velocity.y
            );
        }
    }

    private void AplicarGravedadMejorada()
    {
        if (rb.velocity.y < 0f)
        {
            rb.gravityScale = normalGravity * fallGravityMultiplier;
        }
        else
        {
            rb.gravityScale = normalGravity;
        }

        if (rb.velocity.y < -maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
        }
    }

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
            hitboxPivot.localScale = mirandoDerecha
                ? new Vector3(1f, 1f, 1f)
                : new Vector3(-1f, 1f, 1f);
        }

        if (cuerpoCollider != null)
        {
            cuerpoCollider.offset = mirandoDerecha
                ? cuerpoColliderOffsetOriginal
                : new Vector2(-cuerpoColliderOffsetOriginal.x, cuerpoColliderOffsetOriginal.y);
        }
    }

    private void ActualizarAnimator()
    {
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        animator.SetBool("IsRunning", isRunning);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VerticalSpeed", rb.velocity.y);
        animator.SetBool("CanInterruptAttack", canInterruptAttack);
    }

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
            espadaHitboxCollider.enabled = true;
        }

        isSwordHitboxActive = true;
        hitboxActiveCounter = hitboxMaxActiveTime;
    }

    public void DesactivarEspadaHitbox()
    {
        if (espadaHitboxCollider != null)
        {
            espadaHitboxCollider.enabled = false;
        }

        isSwordHitboxActive = false;
        hitboxActiveCounter = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.DrawWireCube(
            groundCheck.position + Vector3.down * groundCheckRadius / 2f,
            new Vector3(0.25f, groundCheckRadius, 0f));
    }
    
}