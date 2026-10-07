using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    private Rigidbody2D rb;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [Header("Movement")]
    [SerializeField] private float movementAcceleration = 50f;
    [SerializeField] private float maxMoveSpeed = 10f;
    [SerializeField] private float groundLinearDamping = 7f;
    [SerializeField] private float airLinearDamping = 2.5f;

    private float horizontalDirection;

    private bool changingDirection =>
        (rb.linearVelocity.x > 0f && horizontalDirection < 0f) ||
        (rb.linearVelocity.x < 0f && horizontalDirection > 0f);

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);

    private bool onGround;

    [Header("Wall Detection")]
    [SerializeField] private float wallCheckDistance = 0.2f;

    private bool onWall;
    private bool onRightWall;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 25f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    [Header("Jump Gravity")]
    [SerializeField] private float fallMultiplier = 8f;
    [SerializeField] private float lowJumpFallMultiplier = 5f;
    [SerializeField] private float normalGravity = 1f;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpForce = 25f;
    [SerializeField] private float wallJumpHorizontalForce = 12f;

    [Header("Dash")]
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private int maxDashes = 1;

    private int dashesRemaining;
    private float dashTimer;

    private bool isDashing;

    private Vector2 dashDirection;


    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        normalGravity = rb.gravityScale;

        dashesRemaining = maxDashes;
    }


    private void Update()
    {
        GetInput();

        HandleJumpInput();
        HandleDashInput();

        UpdateCoyoteTime();
    }


    private void FixedUpdate()
    {
        CheckCollision();

        if (isDashing)
        {
            HandleDash();
            return;
        }

        MoveCharacter();

        HandleJump();

        if (onGround)
        {
            ApplyGroundLinearDamping();

            dashesRemaining = maxDashes;
        }
        else
        {
            ApplyAirLinearDamping();
            ApplyFallGravity();
        }
    }


    private void GetInput()
    {
        horizontalDirection = Input.GetAxisRaw("Horizontal");
    }


    #region Movement

    private void MoveCharacter()
    {
        rb.AddForce(
            Vector2.right * horizontalDirection * movementAcceleration
        );

        if (Mathf.Abs(rb.linearVelocity.x) > maxMoveSpeed)
        {
            rb.linearVelocity = new Vector2(
                Mathf.Sign(rb.linearVelocity.x) * maxMoveSpeed,
                rb.linearVelocity.y
            );
        }
    }


    private void ApplyGroundLinearDamping()
    {
        if (Mathf.Abs(horizontalDirection) < 0.4f || changingDirection)
        {
            rb.linearDamping = groundLinearDamping;
        }
        else
        {
            rb.linearDamping = 0f;
        }
    }


    private void ApplyAirLinearDamping()
    {
        rb.linearDamping = airLinearDamping;
    }

    #endregion


    #region Jump

    private void HandleJumpInput()
    {
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }


    private void UpdateCoyoteTime()
    {
        if (onGround)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }


    private void HandleJump()
    {
        if (jumpBufferCounter <= 0f)
            return;

        // Normal jump usando Coyote Time
        if (coyoteTimeCounter > 0f)
        {
            Jump();
            return;
        }

        // Wall Jump
        if (onWall && !onGround)
        {
            WallJump();
        }
    }


    private void Jump()
    {
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            0f
        );

        rb.AddForce(
            Vector2.up * jumpForce,
            ForceMode2D.Impulse
        );

        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
    }


    private void WallJump()
    {
        float direction = onRightWall ? -1f : 1f;

        rb.linearVelocity = Vector2.zero;

        rb.AddForce(
            new Vector2(
                direction * wallJumpHorizontalForce,
                wallJumpForce
            ),
            ForceMode2D.Impulse
        );

        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
    }


    private void ApplyFallGravity()
    {
        if (rb.linearVelocity.y < 0f)
        {
            rb.gravityScale = fallMultiplier;
        }
        else if (
            rb.linearVelocity.y > 0f &&
            !Input.GetButton("Jump")
        )
        {
            rb.gravityScale = lowJumpFallMultiplier;
        }
        else
        {
            rb.gravityScale = normalGravity;
        }
    }

    #endregion


    #region Dash

    private void HandleDashInput()
    {
        if (!Input.GetButtonDown("Dash"))
            return;

        if (dashesRemaining <= 0 || isDashing)
            return;

        StartDash();
    }


    private void StartDash()
    {
        isDashing = true;

        dashesRemaining--;

        dashTimer = dashDuration;

        float verticalDirection = Input.GetAxisRaw("Vertical");

        dashDirection = new Vector2(
            horizontalDirection,
            verticalDirection
        );

        // Se não houver direção, dash horizontal
        if (dashDirection == Vector2.zero)
        {
            dashDirection = transform.localScale.x >= 0
                ? Vector2.right
                : Vector2.left;
        }

        dashDirection.Normalize();

        rb.linearVelocity = Vector2.zero;

        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
    }


    private void HandleDash()
    {
        rb.linearVelocity = dashDirection * dashForce;

        dashTimer -= Time.fixedDeltaTime;

        if (dashTimer <= 0f)
        {
            EndDash();
        }
    }


    private void EndDash()
    {
        isDashing = false;

        rb.gravityScale = normalGravity;
    }

    #endregion


    #region Collision

    private void CheckCollision()
    {
        onGround = Physics2D.BoxCast(
            transform.position,
            groundCheckSize,
            0f,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        RaycastHit2D rightWall = Physics2D.Raycast(
            transform.position,
            Vector2.right,
            wallCheckDistance,
            wallLayer
        );

        RaycastHit2D leftWall = Physics2D.Raycast(
            transform.position,
            Vector2.left,
            wallCheckDistance,
            wallLayer
        );

        onWall = rightWall || leftWall;

        onRightWall = rightWall;
    }

    #endregion


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireCube(
            transform.position + Vector3.down * groundCheckDistance,
            groundCheckSize
        );

        Gizmos.DrawLine(
            transform.position,
            transform.position + Vector3.right * wallCheckDistance
        );

        Gizmos.DrawLine(
            transform.position,
            transform.position + Vector3.left * wallCheckDistance
        );
    }
}