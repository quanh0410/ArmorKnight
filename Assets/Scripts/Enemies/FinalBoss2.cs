using UnityEngine;
using System.Collections;

public class FinalBoss2 : EnemyBase
{
    public enum BossState { Idle, Chase, Airborne, Attacking, Stunned }

    [Header("--- TRẠNG THÁI KÍCH HOẠT ---")]
    [Tooltip("Boss có đang tỉnh táo không? Đạo diễn sẽ bật cái này khi hết hội thoại.")]
    public bool isAwake = false; // --- MỚI: Quản lý việc ngủ/thức của Boss 2 ---

    [Header("--- CƠ CHẾ DI CHUYỂN & AI ---")]
    public float chaseSpeed = 5f;
    public float jumpForce = 12f;
    [Tooltip("Khoảng cách tối thiểu để bắt đầu vung vũ khí")]
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;

    [Header("--- CẢM BIẾN MÔI TRƯỜNG (HÌNH CHỮ NHẬT) ---")]
    public Transform groundCheck;
    public Vector2 groundBoxSize = new Vector2(0.5f, 0.1f);

    public Transform wallCheck;
    public Vector2 wallBoxSize = new Vector2(0.1f, 0.8f);

    public Transform edgeCheck;
    public Vector2 edgeBoxSize = new Vector2(0.2f, 0.2f);

    public LayerMask groundLayer;

    [Header("--- VỊ TRÍ 3 HITBOX RIÊNG BIỆT ---")]
    public Transform attackPointAtk1;
    public Vector2 hitboxAtk1 = new Vector2(2f, 2f);
    public int damageAtk1 = 15;
    [Space(5)]
    public Transform attackPointAtk2;
    public Vector2 hitboxAtk2 = new Vector2(3f, 1.5f);
    public int damageAtk2 = 20;
    [Space(5)]
    public Transform attackPointAtk3;
    [Tooltip("Bán kính hình tròn cho đòn đánh số 3")]
    public float hitboxRadiusAtk3 = 2.5f;
    public int damageAtk3 = 30;

    [Header("--- LIÊN KẾT BOSS 1 (CƠ CHẾ BẤT TỬ) ---")]
    [Tooltip("Kéo script máu của Final Boss 1 vào đây")]
    public EnemyHealth boss1Health;
    [Tooltip("Kéo GameObject hiệu ứng vòng bảo vệ (Shield) vào đây")]
    public GameObject shieldEffect;

    private EnemyHealth myHealth;

    [Header("--- TÍNH TOÁN UTILITY AI ---")]
    public float decisionInertiaTime = 0.2f;

    private BossState currentState = BossState.Idle;
    private float cooldownTimer = 0f;
    private bool isGrounded = true;

    private float decisionLockTimer = 0f;
    private int lastAttack = 0;

    // --- CÁC HASH PARAMETER ---
    private readonly int hashIsRunning = Animator.StringToHash("isRunning");
    private readonly int hashIsGrounded = Animator.StringToHash("isGrounded");
    private readonly int hashVerticalSpeed = Animator.StringToHash("verticalSpeed");
    private readonly int hashAtk1Trigger = Animator.StringToHash("attack1");
    private readonly int hashAtk2Trigger = Animator.StringToHash("attack2");
    private readonly int hashAtk3Trigger = Animator.StringToHash("attack3");
    private readonly int hashHitState = Animator.StringToHash("BFinal2Hit");

    protected override void Awake()
    {
        base.Awake();
        myHealth = GetComponent<EnemyHealth>();

        if (shieldEffect != null) shieldEffect.SetActive(true);
    }

    protected override void Update()
    {
        base.Update();

        if (myHealth != null)
        {
            if (boss1Health != null && boss1Health.currentHealth > 0)
            {
                myHealth.isInvincible = true;
                if (shieldEffect != null && !shieldEffect.activeSelf)
                    shieldEffect.SetActive(true);
            }
            else
            {
                myHealth.isInvincible = false;
                if (shieldEffect != null && shieldEffect.activeSelf)
                    shieldEffect.SetActive(false);
            }
        }
    }

    protected override void ExecuteAI()
    {
        // --- MỚI: Nếu chưa được gọi dậy (isAwake = false), Boss sẽ đứng im chờ lệnh ---
        if (!isAwake || player == null) return;

        if (HandleReactionState()) return;

        // 1. CẬP NHẬT TRẠNG THÁI MÔI TRƯỜNG
        CheckEnvironment();

        // 2. LỰC ÉP NGANG KHI NHẢY (AIR-CONTROL)
        if (currentState == BossState.Airborne)
        {
            float dirX = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(dirX * chaseSpeed, rb.linearVelocity.y);
            return;
        }

        // 3. CHẶN AI NẾU ĐANG BỊ CHOÁNG HOẶC ĐANG TRONG HOẠT ẢNH ĐÁNH
        if (currentState == BossState.Stunned || currentState == BossState.Attacking) return;

        // 4. XỬ LÝ HỒI CHIÊU (COOLDOWN)
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            ChangeState(BossState.Idle);
            FacePlayer();
            return;
        }

        if (Time.time < decisionLockTimer) return;

        // 5. QUYẾT ĐỊNH HÀNH ĐỘNG 
        EvaluateUtilityAndAct();
    }

    // =======================================================================
    // 🌟 MỚI: HÀM ĐƯỢC GỌI TỪ BOSSARENAMANAGER ĐỂ ĐÁNH THỨC BOSS
    // =======================================================================
    public void WakeUpBoss()
    {
        isAwake = true;
        Debug.Log($"<color=red><b>{gameObject.name} đã thức tỉnh và bắt đầu truy sát Player!</b></color>");
    }

    #region CẢM BIẾN & DI CHUYỂN (PARKOUR BOX)
    private void CheckEnvironment()
    {
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundBoxSize, 0f, groundLayer);

        if (anim != null)
        {
            anim.SetBool(hashIsGrounded, isGrounded);
            anim.SetFloat(hashVerticalSpeed, rb.linearVelocity.y);
        }

        if (!isGrounded)
        {
            currentState = BossState.Airborne;
        }
        else if (currentState == BossState.Airborne && rb.linearVelocity.y <= 0.01f)
        {
            ChangeState(BossState.Idle);
        }
    }

    private void ChasePlayer()
    {
        if (!isGrounded) return;

        ChangeState(BossState.Chase);
        FacePlayer();

        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dirX * chaseSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private void FacePlayer()
    {
        float dirX = player.position.x - transform.position.x;
        if (Mathf.Abs(dirX) > 0.1f) Flip(Mathf.Sign(dirX));
    }
    #endregion

    #region THUẬT TOÁN TÍNH ĐIỂM (UTILITY AI & COMBAT)
    private void ChangeState(BossState newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        if (anim == null) return;

        if (newState == BossState.Idle)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            anim.SetBool(hashIsRunning, false);
        }
        else if (newState == BossState.Chase)
        {
            anim.SetBool(hashIsRunning, true);
        }
    }

    private void EvaluateUtilityAndAct()
    {
        float distSqr = (player.position - transform.position).sqrMagnitude;
        FacePlayer();

        float scoreAtk1 = (distSqr <= attackRange * attackRange) ? 80f : 0f;
        float scoreAtk2 = (distSqr <= attackRange * attackRange) ? 75f : 0f;
        float scoreAtk3 = (distSqr <= attackRange * attackRange) ? 85f : 0f;

        bool isWallAhead = Physics2D.OverlapBox(wallCheck.position, wallBoxSize, 0f, groundLayer);
        bool isEdgeAhead = !Physics2D.OverlapBox(edgeCheck.position, edgeBoxSize, 0f, groundLayer);
        bool needsParkour = isWallAhead || isEdgeAhead || (player.position.y > transform.position.y + 1.5f);

        float scoreJump = (needsParkour && isGrounded) ? 100f : 0f;
        float scoreChase = (!needsParkour && distSqr > attackRange * attackRange) ? 90f : 0f;

        if (lastAttack == 1) scoreAtk1 -= 60f;
        if (lastAttack == 2) scoreAtk2 -= 60f;
        if (lastAttack == 3) scoreAtk3 -= 60f;

        if (distSqr <= 1.0f) scoreAtk3 += 30f;

        if (scoreAtk1 > 0) scoreAtk1 += Random.Range(0f, 20f);
        if (scoreAtk2 > 0) scoreAtk2 += Random.Range(0f, 20f);
        if (scoreAtk3 > 0) scoreAtk3 += Random.Range(0f, 20f);

        float highestScore = Mathf.Max(scoreAtk1, scoreAtk2, scoreAtk3, scoreJump, scoreChase);

        if (highestScore == scoreJump && scoreJump > 0)
        {
            Jump();
        }
        else if (highestScore == scoreAtk1 && scoreAtk1 > 0)
        {
            ExecuteSpecificAttack(1, hashAtk1Trigger);
        }
        else if (highestScore == scoreAtk2 && scoreAtk2 > 0)
        {
            ExecuteSpecificAttack(2, hashAtk2Trigger);
        }
        else if (highestScore == scoreAtk3 && scoreAtk3 > 0)
        {
            ExecuteSpecificAttack(3, hashAtk3Trigger);
        }
        else
        {
            ChasePlayer();
        }

        decisionLockTimer = Time.time + decisionInertiaTime;
    }

    private void ExecuteSpecificAttack(int attackIndex, int triggerHash)
    {
        currentState = BossState.Attacking;
        lastAttack = attackIndex;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (anim != null) anim.SetBool(hashIsRunning, false);
        FacePlayer();

        if (anim != null) anim.SetTrigger(triggerHash);
    }

    #endregion

    #region ANIMATION EVENTS & PHẢN XẠ BỊ ĐÁNH
    public void Event_Atk1Hit()
    {
        if (attackPointAtk1 == null) return;
        Collider2D hit = Physics2D.OverlapBox(attackPointAtk1.position, hitboxAtk1, 0f, LayerMask.GetMask("Player"));
        ApplyDamage(hit, damageAtk1);
    }

    public void Event_Atk2Hit()
    {
        if (attackPointAtk2 == null) return;
        Collider2D hit = Physics2D.OverlapBox(attackPointAtk2.position, hitboxAtk2, 0f, LayerMask.GetMask("Player"));
        ApplyDamage(hit, damageAtk2);
    }

    public void Event_Atk3Hit()
    {
        if (attackPointAtk3 == null) return;
        Collider2D hit = Physics2D.OverlapCircle(attackPointAtk3.position, hitboxRadiusAtk3, LayerMask.GetMask("Player"));
        ApplyDamage(hit, damageAtk3);
    }

    private void ApplyDamage(Collider2D hit, int dmg)
    {
        if (hit != null)
        {
            hit.GetComponent<PlayerHealth>()?.TakeDamage(dmg, transform);
            CinemachineShake.Instance?.ShakeCamera(0.2f);
        }
    }

    public void Event_EndAttack()
    {
        ChangeState(BossState.Idle);
        cooldownTimer = attackCooldown;
    }

    private bool HandleReactionState()
    {
        if (anim == null) return false;

        bool isHitAnimationPlaying = anim.GetCurrentAnimatorStateInfo(0).shortNameHash == hashHitState;

        if (isHitAnimationPlaying)
        {
            if (currentState != BossState.Stunned)
            {
                currentState = BossState.Stunned;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                anim.SetBool(hashIsRunning, false);
            }
            return true;
        }
        else if (currentState == BossState.Stunned)
        {
            ChangeState(BossState.Idle);
            cooldownTimer = 0.1f;
        }

        return false;
    }
    #endregion

    #region GIZMOS GỠ LỖI
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        if (groundCheck != null) Gizmos.DrawWireCube(groundCheck.position, groundBoxSize);
        if (wallCheck != null) Gizmos.DrawWireCube(wallCheck.position, wallBoxSize);
        Gizmos.color = Color.blue;
        if (edgeCheck != null) Gizmos.DrawWireCube(edgeCheck.position, edgeBoxSize);

        Gizmos.color = Color.red;
        if (attackPointAtk1 != null) Gizmos.DrawWireCube(attackPointAtk1.position, hitboxAtk1);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        if (attackPointAtk2 != null) Gizmos.DrawWireCube(attackPointAtk2.position, hitboxAtk2);

        Gizmos.color = Color.magenta;
        if (attackPointAtk3 != null) Gizmos.DrawWireSphere(attackPointAtk3.position, hitboxRadiusAtk3);
    }
    #endregion
}