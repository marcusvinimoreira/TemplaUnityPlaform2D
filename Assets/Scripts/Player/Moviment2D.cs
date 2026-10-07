using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moviment2D : MonoBehaviour
{

    [Header("Components")]
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sprite;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [Header("Movement variables")]
    [SerializeField] private float movementAcceleration = 50f;
    [SerializeField] private float maxMoveSpeed = 10f;
    [SerializeField] private float groundlinearDrag = 7f;
    private float horizontalDirection;
    private float verticalDirection;
    private bool chagingDirection => (rb.linearVelocity.x > 0f && horizontalDirection < 0f) || (rb.linearVelocity.x < 0f && horizontalDirection > 0f);
    private bool facingRight = true;
    private bool canMove => !wallGrab;


    [Header("Ground Collision variables")]
    [SerializeField] private float groundRaycastLength;
    private bool onGround;


    [Header("Jump variables")]
    [SerializeField] private float jumpForce = 25f;
    [SerializeField] private float airLinearDrag = 2.5f;
    [SerializeField] private float fallMultiplier = 3f;
    [SerializeField] private float lowJumpFallMultiplier = 2f;
    [SerializeField] private int extraJumps = 1;
    [SerializeField] private float hangTime = 0.1f;
    [SerializeField] private float jumpBufferLength = .3f;
    private int extraJumpsValue;
    private float hangTimeCounter;
    private float jumpBufferCounter;
    private bool canJump => jumpBufferCounter > 0f && (hangTimeCounter > 0f || extraJumpsValue >= 1 || onWall);
    private bool isJumping = false;

    [Header("Wall Collision variables")]
    [SerializeField] private float wallRaycastLegth;
    [SerializeField] private float wallSlideModifier = 0.5f;
    [SerializeField] private float wallJumpXVelocityHaltDelay = 0.2f;
    private bool onWall;
    private bool onRightWall;
    private bool wallGrab => onWall && !onGround && Input.GetButton("WallGrab");
    private bool wallSlide => onWall && !onGround && !Input.GetButtonDown("WallGrab") && rb.linearVelocityY < 0f;

    [Header("Dash Collision Variables")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashLength = 0.3f;
    [SerializeField] private float dashBufferLength = 0.1f;
    private float dashBufferCounter;
    private bool isDashing;
    private bool hasDashed;
    private bool canDash => dashBufferCounter > 0f && !hasDashed;


    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        AnimatorPlayer();
        CheckFlip();
        horizontalDirection = GetInput().x;
        verticalDirection = GetInput().y;

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferLength;

        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
        if (Input.GetButtonDown("Fire1"))
        {
            dashBufferCounter = dashBufferLength;
            Debug.Log("Dash Buffer Counter: " + dashBufferCounter);
        }
        else
        {
            dashBufferCounter -= Time.deltaTime;
        }

        if (rb.linearVelocityY < 0f)
        {
            //Animation
            anim.SetBool("isJumping", false);
            anim.SetBool("isFalling", true);
        }
    }

    private void FixedUpdate()
    {
        CheckCollision();
        if (canDash)
        {
            StartCoroutine(Dash(horizontalDirection, verticalDirection));
            Debug.Log("Coroutine Dash");

        }
        if (!isDashing)
        {
            if (canMove)
            {
                MoveCharacter();
            }
            else
            {
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, (new Vector2(horizontalDirection * maxMoveSpeed, rb.linearVelocityY)), 0.5f * Time.deltaTime);
            }
            //ApplyLinearDrag();

            if (onGround)
            {
                ApplyGroundLinearDrag();
                extraJumpsValue = extraJumps;
                hangTimeCounter = hangTime;
                hasDashed = false;
                //Animation
                anim.SetBool("isJumping", false);
                anim.SetBool("isFalling", false);
            }
            else
            {
                ApplyAirLinearDrag();
                FallMultiplier();
                hangTimeCounter -= Time.fixedDeltaTime;
                if (!onWall || rb.linearVelocityY < 0f)
                {
                    isJumping = false;
                }
            }
            if (canJump)
            {
                if (onWall && !onGround)
                {
                    if (onRightWall && horizontalDirection > 0f || !onRightWall && horizontalDirection < 0f)
                    {
                        StartCoroutine(NeutralWallJump());
                    }
                    else
                    {
                        WallJump();
                    }

                    //Flip();
                }
                else if ((!wallSlide && rb.linearVelocity.y <= 0))
                {
                    Jump(Vector2.up);
                }


            }
            if (wallGrab)
            {
                WallGrab();
            }
            if (wallSlide)
            {
                WallSlide();
            }
        }

    }

    private Vector2 GetInput()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    private void MoveCharacter()
    {
        rb.AddForce(new Vector2(horizontalDirection, 0f) * movementAcceleration);

        if (Mathf.Abs(rb.linearVelocity.x) > maxMoveSpeed)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxMoveSpeed, rb.linearVelocity.y);
        }
    }

    private void ApplyGroundLinearDrag()
    {
        if (Mathf.Abs(horizontalDirection) < 0.4f)
        {
            rb.linearDamping = groundlinearDrag;
        }
        else
        {

            rb.linearDamping = 0f;
        }
    }

    private void ApplyAirLinearDrag()
    {
        rb.linearDamping = airLinearDrag;

    }

    void StickToWall()
    {
        if (onRightWall && horizontalDirection >= 0f)
        {
            rb.linearVelocity = new Vector2(1f, rb.linearVelocityY);
        }
        if (!onRightWall && horizontalDirection <= 0f)
        {
            rb.linearVelocity = new Vector2(-1f, rb.linearVelocityY);
        }
        if (onWall && !facingRight)
        {
            Flip();
        }
        if (!onWall && facingRight)
        {
            Flip();
        }
    }

    private void Jump(Vector2 direction)
    {
        if (!onGround && !onWall)
        {
            extraJumpsValue--;
        }
        ApplyAirLinearDrag();
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        hangTimeCounter = 0f;
        jumpBufferCounter = 0f;

        //Animation
        anim.SetBool("isJumping", true);
        anim.SetBool("isFalling", false);
    }

    private void WallJump()
    {
        Vector2 jumpDirection;

        if (onRightWall)
        {
            jumpDirection = Vector2.left;
        }
        else
        {
            jumpDirection = Vector2.right;
        }
        if (!isJumping)
        {
            if (wallGrab)
            {
                WallGrab();
            }
            if (wallSlide)
            {
                WallSlide();
            }
            if (onWall)
            {
                StickToWall();
            }
        }
    }

    IEnumerator NeutralWallJump()
    {
        Vector2 jumpDirection;

        if (onRightWall)
        {
            jumpDirection = Vector2.left;
        }
        else
        {
            jumpDirection = Vector2.right;
        }
        Jump(Vector2.up + jumpDirection);
        yield return new WaitForSeconds(wallJumpXVelocityHaltDelay);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocityY);
    }

    private void FallMultiplier()
    {

        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = fallMultiplier;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.gravityScale = lowJumpFallMultiplier;
        }
        else
        {
            rb.gravityScale = 1f;
        }

    }

    private void CheckCollision()
    {
        onGround = Physics2D.Raycast(transform.position * groundRaycastLength, Vector2.down, groundRaycastLength, groundLayer);
        onWall = Physics2D.Raycast(transform.position, Vector2.right, wallRaycastLegth, wallLayer) || Physics2D.Raycast(transform.position, Vector2.left, wallRaycastLegth, wallLayer);
        onRightWall = Physics2D.Raycast(transform.position, Vector2.right, wallRaycastLegth, wallLayer);

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundRaycastLength);
        //Wall
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * wallRaycastLegth);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * wallRaycastLegth);

    }

    void WallGrab()
    {
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocityX, 0f);
        // StickToWall();
    }

    void WallSlide()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocityX, -maxMoveSpeed * wallSlideModifier);
        StickToWall();
    }

    IEnumerator Dash(float x, float y)
    {
        float dashStartTime = Time.time;
        hasDashed = true;
        isDashing = true;
        isJumping = false;
        anim.SetBool("isJumping", false);
        anim.SetBool("isFalling", false);
        anim.SetTrigger("dash");

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;

        Vector2 dir;
        if (x != 0f || y != 0f)
        {
            dir = new Vector2(x, y);
        }
        else
        {
            if (facingRight)
            {
                dir = new Vector2(1f, 0f);
            }
            else
            {
                dir = new Vector2(-1f, 0f);
            }
        }

        while (Time.time < dashStartTime + dashLength)
        {
            rb.linearVelocity = dir.normalized * dashSpeed;
            yield return null;
        }
        isDashing = false;
    }

    void AnimatorPlayer()
    {
        anim.SetBool("isGround", onGround);
        anim.SetFloat("horizontalDireciton", Mathf.Abs(horizontalDirection));
        anim.SetBool("wallGrab", wallGrab || wallSlide);
        // anim.SetBool("isDashing", isDashing);
    }

    void Flip()
    {

        facingRight = !facingRight;
        //  transform.Rotate(0f, 180f, 0f);
        sprite.flipX = !sprite.flipX;

    }

    void CheckFlip()
    {
        if (horizontalDirection < 0f && facingRight)
        {
            Flip();
        }
        if (horizontalDirection > 0f && !facingRight)
        {
            {
                Flip();
            }
        }
    }
}

