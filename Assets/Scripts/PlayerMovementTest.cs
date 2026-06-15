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

    private float horizontalInput;
    private bool isRunning;
    private bool isGrounded;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float groundIgnoreCounter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        rb.gravityScale = normalGravity;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        LeerInput();
        DetectarSuelo();
        ControlarSalto();
        GirarPersonaje();
        ActualizarAnimator();
    }

    private void FixedUpdate()
    {
        MoverPersonaje();
        AplicarGravedadMejorada();
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

        bool touchingGround = false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            // Evita que el GroundCheck detecte el propio collider del Player.
            if (hit.transform.IsChildOf(transform)) continue;

            touchingGround = true;
            break;
        }

        // Si el jugador está subiendo, no debe contar como grounded.
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
            spriteRenderer.flipX = false;
        }
        else if (horizontalInput < -0.01f)
        {
            spriteRenderer.flipX = true;
        }
    }

    private void ActualizarAnimator()
    {
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        animator.SetBool("IsRunning", isRunning);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VerticalSpeed", rb.velocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}