using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    [HideInInspector] public Vector2 platformVelocity; // --- BIẾN MỚI: Nhận vận tốc từ Platform ---

    [Header("Jump Settings")]
    public float jumpForce = 15f;
    [Range(0f, 1f)]
    public float jumpCutMultiplier = 0.5f;
    public GameObject jumpEffectPrefab;
    private bool wasGrounded;

    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.1f;
    public bool isDashing;
    private bool canDash = true;

    [Header("Combat Settings")]
    public float sideRecoilForce = 5f;
    public float attackCooldown = 0.3f;

    [Header("Combo Settings")]
    public float comboWindow = 0.8f;
    private int comboStep = 0;

    private bool isAttackLocked;
    private float lastAttackTime;

    [Header("Ground Check Settings")]
    public Transform groundCheckPoint;
    public Vector2 groundCheckSize = new Vector2(0.4f, 0.1f);
    public LayerMask groundLayer;

    [Header("Wall Slide Settings")]
    public Transform wallCheckPoint;
    public Vector2 wallCheckSize = new Vector2(0.2f, 1f);
    public LayerMask wallLayer;
    public float wallSlidingSpeed = 2f;
    public bool isWallSliding;

    [Header("Wall Jump Settings")]
    public Vector2 wallJumpPower = new Vector2(10f, 10f);
    public float wallJumpDuration = 0.2f;
    private bool isWallJumping;
    private float wallJumpDirection;

    [Header("Wall Climb Settings")]
    public Transform wallClimbCheckPoint;
    public Vector2 wallClimbCheckSize = new Vector2(0.2f, 0.2f);
    public float wallClimbDuration = 0.4f;
    public Vector2 wallClimbOffset = new Vector2(0.5f, 1.2f);
    public bool isWallClimbing;

    private Rigidbody2D rb;
    private PlayerCombat playerCombat;
    private float defaultGravity;

    [HideInInspector] public bool isInputLocked = false;
    [HideInInspector] public bool isResting = false;

    [Header("Rest Settings")]
    public GameObject restEffectPrefab;
    private GameObject currentRestEffect;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCombat = GetComponent<PlayerCombat>();
        defaultGravity = rb.gravityScale;
        wasGrounded = IsGrounded();
    }

    void Update()
    {
        if (isResting)
        {
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                isResting = false;
                isInputLocked = false;
                GetComponent<PlayerAnimator>().SetRestingAnimation(false);
                GetComponent<Animator>().Play("Idle", 0, 0f);
                if (currentRestEffect != null)
                {
                    Destroy(currentRestEffect);
                }
                if (InventoryUIManager.instance.mainInventoryPanel.activeSelf)
                    InventoryUIManager.instance.ToggleInventory();
            }
            return;
        }

        if (isInputLocked) return;

        if (!isDashing && !isWallClimbing)
        {
            CheckWallClimb();
        }

        if (!isDashing && !isWallClimbing)
        {
            PlayerWallSlide();
            PlayerWallJump();
        }

        if (!isDashing && !isWallClimbing)
        {
            HandleAttackInput();
            // --- MỚI: KỸ NĂNG CHÉM XA (Phím U) ---
            if (EquipmentManager.instance.HasMechanic("RangedSlash") && Input.GetKeyDown(KeyCode.U) && Time.time >= lastAttackTime + attackCooldown && (!IsWalled() || IsGrounded()))
            {
                // Kiểm tra và trừ 33 năng lượng
                if (GetComponent<PlayerEnergy>().SpendEnergy(0))
                {
                    lastAttackTime = Time.time;
                    GetComponent<PlayerCombat>().CastRangedSlash(); // Gọi hàm chém xa ở PlayerCombat
                }
                else
                {
                    Debug.Log("Không đủ 33 năng lượng để phóng kiếm khí!");
                }
            }
        }

        if (!isDashing && !isWallClimbing && !isAttackLocked)
        {
            if (!isWallJumping)
            {
                PlayerMovement();
                PlayerJump();
            }

            if (Input.GetKeyDown(KeyCode.K) && canDash)
            {
                StartCoroutine(PlayerDash());
            }
        }

        bool isGrounded = IsGrounded();
        if (!wasGrounded && isGrounded && rb.linearVelocity.y <= 0f)
        {
            Vector2 spawnPos = new Vector2(groundCheckPoint.position.x, groundCheckPoint.position.y);
            if (jumpEffectPrefab != null) ObjectPoolManager.Instance.Spawn(jumpEffectPrefab, spawnPos, Quaternion.identity);
        }
        wasGrounded = isGrounded;
    }

    private void CheckWallClimb()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        if (IsWalled() && !IsLedgeWalled() && !IsGrounded() && rb.linearVelocity.y < -0.1f && moveInput != 0)
        {
            StartCoroutine(PlayerWallClimb());
        }
    }

    private IEnumerator PlayerWallClimb()
    {
        isWallClimbing = true;
        isWallSliding = false;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        GetComponent<PlayerAnimator>().SetWallClimbAnimation(true);

        Vector2 startPosition = transform.position;
        float facingDirection = transform.localScale.x;
        Vector2 targetPosition = new Vector2(startPosition.x + (wallClimbOffset.x * facingDirection), startPosition.y + wallClimbOffset.y);

        float elapsedTime = 0f;
        while (elapsedTime < wallClimbDuration)
        {
            transform.position = Vector2.Lerp(startPosition, targetPosition, elapsedTime / wallClimbDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = defaultGravity;
        isWallClimbing = false;

        GetComponent<PlayerAnimator>().SetWallClimbAnimation(false);
    }

    private void HandleAttackInput()
    {
        if (Time.time > lastAttackTime + comboWindow)
        {
            comboStep = 0;
        }
        if (Input.GetKeyDown(KeyCode.J) && Time.time >= lastAttackTime + attackCooldown && (!IsWalled() || IsGrounded()))
        {
            if (EquipmentManager.instance != null && EquipmentManager.instance.currentWeapon != null)
            {
                lastAttackTime = Time.time;
                comboStep++;
                if (comboStep > 2)
                {
                    comboStep = 1;
                }
                playerCombat.Attack(comboStep);
            }
            else
            {
                Debug.Log("Không có vũ khí! Hãy mở túi đồ (E) và trang bị vũ khí trước khi tấn công.");
            }
        }
    }

    public void HandleAttackRecoil()
    {
        rb.linearVelocity = Vector2.zero;
        rb.linearVelocity = new Vector2(-transform.localScale.x * sideRecoilForce, rb.linearVelocity.y);
        StartCoroutine(LockMovementForRecoil());
    }

    private IEnumerator LockMovementForRecoil()
    {
        isAttackLocked = true;
        yield return new WaitForSeconds(0.1f);
        isAttackLocked = false;
    }

    // ==========================================
    // HÀM ĐÃ ĐƯỢC NÂNG CẤP (TRUYỀN VẬN TỐC)
    // ==========================================
    public void PlayerMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");

        // CỘNG DỒN VẬN TỐC: Lấy (Input * Tốc độ đi bộ) + Vận tốc của bệ đỡ (Nếu có)
        rb.linearVelocity = new Vector2((moveInput * moveSpeed) + platformVelocity.x, rb.linearVelocity.y);

        if (moveInput > 0 && transform.localScale.x < 0)
        {
            Flip(1f);
        }
        else if (moveInput < 0 && transform.localScale.x > 0)
        {
            Flip(-1f);
        }
    }

    private void Flip(float direction)
    {
        if (IsGrounded())
        {
            GetComponent<PlayerAnimator>().PlayTurnAnimation();
        }

        transform.localScale = new Vector3(direction, 1, 1);
    }

    public void PlayerJump()
    {
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            Vector2 spawnPos = new Vector2(groundCheckPoint.transform.position.x, groundCheckPoint.transform.position.y);
            if (jumpEffectPrefab != null) ObjectPoolManager.Instance.Spawn(jumpEffectPrefab, spawnPos, Quaternion.identity);
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    private void PlayerWallSlide()
    {
        if (EquipmentManager.instance.HasMechanic("WallSlide") && IsWalled() && !IsGrounded() && rb.linearVelocity.y <= 0f)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector2(0f, Mathf.Clamp(rb.linearVelocity.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void PlayerWallJump()
    {
        if (IsWalled() && !IsGrounded())
        {
            wallJumpDirection = -transform.localScale.x;
        }

        if (EquipmentManager.instance.HasMechanic("WallSlide") && Input.GetButtonDown("Jump") && IsWalled() && !IsGrounded())
        {
            isWallJumping = true;
            rb.linearVelocity = new Vector2(wallJumpDirection * wallJumpPower.x, wallJumpPower.y);
            transform.localScale = new Vector3(wallJumpDirection, 1, 1);
            CancelInvoke(nameof(StopWallJumping));
            Invoke(nameof(StopWallJumping), wallJumpDuration);
        }
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    private IEnumerator PlayerDash()
    {
        if (!IsWalled() && EquipmentManager.instance.HasMechanic("Dash"))
        {
            canDash = false;
            isDashing = true;
            float originalGravity = rb.gravityScale;
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(transform.localScale.x * dashSpeed, 0f);
            yield return new WaitForSeconds(dashDuration);
            rb.gravityScale = originalGravity;
            isDashing = false;
            yield return new WaitForSeconds(dashCooldown);
            canDash = true;
        }
    }

    public bool IsGrounded()
    {
        return Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0f, groundLayer);
    }

    public bool IsWalled()
    {
        return Physics2D.OverlapBox(wallCheckPoint.position, wallCheckSize, 0f, wallLayer);
    }
    public bool IsLedgeWalled()
    {
        return Physics2D.OverlapBox(wallClimbCheckPoint.position, wallClimbCheckSize, 0f, wallLayer);
    }

    public void InterruptDashAndActions()
    {
        StopAllCoroutines();
        rb.gravityScale = defaultGravity;
        isDashing = false;
        canDash = true;
        isAttackLocked = false;
        isWallClimbing = false;
        isWallSliding = false;
        isWallJumping = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        GetComponent<PlayerAnimator>().SetWallClimbAnimation(false);
        GetComponent<Animator>().SetBool("IsDashing", false);
    }

    public IEnumerator WalkToBenchAndRest(Transform benchTransform, string benchID)
    {
        isInputLocked = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        GetComponent<PlayerAnimator>().enabled = false;
        Animator anim = GetComponent<Animator>();
        float targetX = benchTransform.position.x;
        float distance = Mathf.Abs(transform.position.x - targetX);

        while (distance > 0.05f)
        {
            float direction = Mathf.Sign(targetX - transform.position.x);
            rb.linearVelocity = new Vector2(direction * moveSpeed * 0.5f, rb.linearVelocity.y);
            transform.localScale = new Vector3(direction, 1, 1);
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            yield return null;
            distance = Mathf.Abs(transform.position.x - targetX);
        }

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        transform.position = new Vector2(targetX, transform.position.y);
        anim.SetFloat("Speed", 0f);
        GetComponent<PlayerAnimator>().enabled = true;
        isResting = true;
        GetComponent<PlayerAnimator>().SetRestingAnimation(true);

        if (restEffectPrefab != null)
        {
            currentRestEffect = ObjectPoolManager.Instance.Spawn(restEffectPrefab, transform.position, Quaternion.identity);
            StickyEffect2D sticky = currentRestEffect.GetComponent<StickyEffect2D>();
            if (sticky != null)
            {
                sticky.SetTarget(transform);
            }
            else
            {
                currentRestEffect.transform.SetParent(transform, true);
            }
        }

        SaveManager.instance.UpdateCheckpoint(benchTransform.gameObject.scene.name, benchID);
        GetComponent<PlayerHealth>()?.FullHeal();
        Debug.Log("Đã ngồi vào ghế. Bây giờ có thể ấn E để mở túi đồ!");
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        }
        if (wallCheckPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(wallCheckPoint.position, wallCheckSize);
        }

        if (wallClimbCheckPoint != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireCube(wallClimbCheckPoint.position, wallClimbCheckSize); }
    }

    public void SnapToRest()
    {
        isInputLocked = true;
        isResting = true;
        rb.linearVelocity = Vector2.zero;
        Animator anim = GetComponent<Animator>();
        anim.SetFloat("Speed", 0f);
        GetComponent<PlayerAnimator>().enabled = true;
        GetComponent<PlayerAnimator>().SetRestingAnimation(true);
        anim.Play("Rest", 0, 0f);

        if (restEffectPrefab != null && currentRestEffect == null)
        {
            currentRestEffect = ObjectPoolManager.Instance.Spawn(restEffectPrefab, transform.position, Quaternion.identity);
            StickyEffect2D sticky = currentRestEffect.GetComponent<StickyEffect2D>();
            if (sticky != null)
            {
                sticky.SetTarget(transform);
            }
            else
            {
                currentRestEffect.transform.SetParent(transform, true);
            }
        }
    }
}