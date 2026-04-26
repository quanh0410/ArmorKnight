using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

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
    public float attackCooldown = 0.3f; // Thời gian không được chém tiếp (để animation kịp chạy)

    [Header("Combo Settings")]
    public float comboWindow = 0.8f; // Thời gian tối đa cho phép để nối Combo
    private int comboStep = 0; // Biến lưu lại đang chém nhát thứ mấy

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
    public bool isWallClimbing; // Đang trong quá trình leo

    private Rigidbody2D rb;
    private PlayerCombat playerCombat;
    private float defaultGravity; // Lưu lại trọng lực gốc (ví dụ: 3)

    [HideInInspector] public bool isInputLocked = false; // --- THÊM BIẾN NÀY ---
    [HideInInspector] public bool isResting = false;

    [Header("Rest Settings")]
    public GameObject restEffectPrefab; // Kéo thả Prefab Effect lúc ngồi vào đây
    private GameObject currentRestEffect; // Biến để theo dõi và tắt Effect khi đứng dậy
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCombat = GetComponent<PlayerCombat>();
        defaultGravity = rb.gravityScale;
        wasGrounded = IsGrounded();
    }

    void Update()
    {
        // THÊM ĐOẠN NÀY ĐỂ ĐỨNG DẬY
        if (isResting)
        {
            // Bấm Phím Nhảy hoặc Phím Lên để đứng dậy
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                isResting = false;
                isInputLocked = false;
                GetComponent<PlayerAnimator>().SetRestingAnimation(false);
                GetComponent<Animator>().Play("Idle", 0, 0f);
                // --- THÊM LOGIC TẮT EFFECT Ở ĐÂY ---
                if (currentRestEffect != null)
                {
                    Destroy(currentRestEffect);
                }
                // -----------------------------------
                // Nếu túi đồ đang mở thì đóng lại luôn
                if (InventoryUIManager.instance.mainInventoryPanel.activeSelf)
                    InventoryUIManager.instance.ToggleInventory();
            }
            return; // Chặn các thao tác khác (lướt, chém...) khi đang ngồi
        }
        if (isInputLocked) return;
        // 1. Leo tường luôn được ưu tiên (trừ khi đang lướt)
        if (!isDashing && !isWallClimbing)
        {
            CheckWallClimb();
        }

        // 2. Chạm tường là phải bám (Không bị khóa bởi AttackLocked)
        if (!isDashing && !isWallClimbing)
        {
            PlayerWallSlide();
            PlayerWallJump();
        }

        // 3. Ưu tiên xử lý lệnh Tấn công (Chỉ bị chặn khi đang lướt hoặc leo tường)
        if (!isDashing && !isWallClimbing)
        {
            HandleAttackInput();
        }

        // 4. Xử lý Di chuyển và Lướt (SẼ BỊ KHÓA THÊM NẾU ĐANG BỊ GIẬT LÙI isAttackLocked)
        if (!isDashing && !isWallClimbing && !isAttackLocked)
        {
            if (!isWallJumping)
            {
                PlayerMovement();
                PlayerJump();
            }

            // --- ĐÃ TRẢ LẠI TÍNH NĂNG DASH VÀO ĐÂY ---
            if (Input.GetKeyDown(KeyCode.K) && canDash)
            {
                StartCoroutine(PlayerDash());
            }
        }

        // 5. Hiệu ứng chạm đất (Giữ nguyên)
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

        // THÊM: && rb.linearVelocity.y < -0.1f 
        // Thay vì <= 0, ta bắt nó phải thực sự đang có gia tốc rơi rõ rệt. 
        // Khi vừa chạm đất, hệ thống vật lý thường giật vận tốc về gần 0 (ví dụ -0.001f).
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
        while (elapsedTime < wallClimbDuration) // Chạy đúng 0.4 giây
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
                // Báo lỗi nếu không có vũ khí (bạn có thể thay bằng âm thanh hoặc thông báo UI)
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

    public void PlayerMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

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
            Vector2 spawnPos = new Vector2(groundCheckPoint.transform.position.x, groundCheckPoint.transform.position.y );
            if(jumpEffectPrefab!=null) ObjectPoolManager.Instance.Spawn(jumpEffectPrefab, spawnPos, Quaternion.identity);
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

    // --- HÀM NÀY ĐỂ HỆ THỐNG MÁU GỌI KHI BỊ ĐÁNH TRÚNG ---
    public void InterruptDashAndActions()
    {
        // 1. Dừng ngay lập tức quá trình lướt, leo tường, giật lùi đang chạy ngầm
        StopAllCoroutines();

        // 2. Khôi phục trọng lực nếu đang bị tắt do lướt hoặc leo tường
        rb.gravityScale = defaultGravity;

        // 3. Reset toàn bộ cờ trạng thái về mặc định
        isDashing = false;
        canDash = true;
        isAttackLocked = false;
        isWallClimbing = false;
        isWallSliding = false;
        isWallJumping = false;

        // 4. Khôi phục vật lý nếu đang bị Kinematic do leo tường
        rb.bodyType = RigidbodyType2D.Dynamic;

        // 5. Ép Animator tắt ngay lập tức animation lướt/leo tường
        GetComponent<PlayerAnimator>().SetWallClimbAnimation(false);
        GetComponent<Animator>().SetBool("IsDashing", false);
    }

    public IEnumerator WalkToBenchAndRest(Transform benchTransform, string benchID)
    {
        isInputLocked = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        // Tạm thời tắt PlayerAnimator để nó không đè thông số Speed về 0
        GetComponent<PlayerAnimator>().enabled = false;

        // Gọi thẳng vào Animator gốc của Unity (ĐÃ FIX LỖI CS1061)
        Animator anim = GetComponent<Animator>();

        float targetX = benchTransform.position.x;
        float distance = Mathf.Abs(transform.position.x - targetX);

        // 1. TỰ ĐỘNG ĐI BỘ ĐẾN GIỮA GHẾ
        while (distance > 0.05f)
        {
            float direction = Mathf.Sign(targetX - transform.position.x);
            rb.linearVelocity = new Vector2(direction * moveSpeed * 0.5f, rb.linearVelocity.y);

            transform.localScale = new Vector3(direction, 1, 1);

            // Dùng anim thay vì GetComponent<PlayerAnimator>()
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));

            yield return null;
            distance = Mathf.Abs(transform.position.x - targetX);
        }

        // 2. TỚI NƠI -> ĐỨNG LẠI & NGỒI XUỐNG
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        transform.position = new Vector2(targetX, transform.position.y);

        // Ép tốc độ về 0
        anim.SetFloat("Speed", 0f);

        // Bật lại PlayerAnimator để nó tiếp quản hiệu ứng Nghỉ ngơi
        GetComponent<PlayerAnimator>().enabled = true;
        isResting = true;
        GetComponent<PlayerAnimator>().SetRestingAnimation(true);

        // --- THÊM LOGIC BẬT EFFECT Ở ĐÂY ---
        if (restEffectPrefab != null)
        {
            // Sinh ra Effect từ Object Pool
            currentRestEffect = ObjectPoolManager.Instance.Spawn(restEffectPrefab, transform.position, Quaternion.identity);

            // Ép Effect dính chặt vào Player
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

        // 3. LƯU GAME VÀ HỒI MÁU
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

    // HÀM MỚI: Ép nhân vật ngồi ngay lập tức khi hồi sinh
    public void SnapToRest()
    {
        isInputLocked = true;
        isResting = true;
        rb.linearVelocity = Vector2.zero;

        Animator anim = GetComponent<Animator>();
        anim.SetFloat("Speed", 0f);

        GetComponent<PlayerAnimator>().enabled = true;
        GetComponent<PlayerAnimator>().SetRestingAnimation(true);

        // Ép chạy ngay Animation ngồi để tránh bị giật hình (Thay "Rest" bằng tên state ngồi của bạn nếu khác)
        anim.Play("Rest", 0, 0f);

        // Bật Effect vòng sáng dưới chân
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