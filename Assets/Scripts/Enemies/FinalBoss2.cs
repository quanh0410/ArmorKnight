using UnityEngine;
using System.Collections;

public class FinalBoss2 : EnemyBase
{
    public enum BossState { Idle, Chase, Airborne, Attacking, Stunned, Dead }

    [Header("--- TRẠNG THÁI KÍCH HOẠT ---")]
    public bool isAwake = false;

    [Header("--- CƠ CHẾ DI CHUYỂN & AI ---")]
    public float chaseSpeed = 5f;
    public float jumpForce = 12f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;

    [Header("--- AGGRESSIVE UPGRADES ---")]
    public float hitStunCooldown = 3f;
    [Range(0f, 100f)] public float comboChance = 30f;

    [Header("--- CẢM BIẾN MÔI TRƯỜNG ---")]
    public Transform groundCheck;
    public Vector2 groundBoxSize = new Vector2(0.5f, 0.1f);
    public Transform wallCheck;
    public Vector2 wallBoxSize = new Vector2(0.1f, 0.8f);
    public Transform edgeCheck;
    public Vector2 edgeBoxSize = new Vector2(0.2f, 0.2f);
    public LayerMask groundLayer;

    [Header("--- VỊ TRÍ 3 HITBOX ---")]
    public Transform attackPointAtk1;
    public Vector2 hitboxAtk1 = new Vector2(2f, 2f);
    public int damageAtk1 = 15;
    [Space(5)]
    public Transform attackPointAtk2;
    public Vector2 hitboxAtk2 = new Vector2(3f, 1.5f);
    public int damageAtk2 = 20;
    [Space(5)]
    public Transform attackPointAtk3;
    public float hitboxRadiusAtk3 = 2.5f;
    public int damageAtk3 = 30;

    [Header("--- LIÊN KẾT BOSS 1 (KHIÊN BẤT TỬ) ---")]
    public EnemyHealth boss1Health;
    public GameObject shieldEffect;

    [Header("--- RƠI VẬT PHẨM (VƯƠNG MIỆN) ---")]
    public GameObject crownPrefab;

    private EnemyHealth myHealth;
    private BossState currentState = BossState.Idle;
    private float cooldownTimer = 0f;
    private bool isGrounded = true;
    private float decisionLockTimer = 0f;
    private int lastAttack = 0;
    public float decisionInertiaTime = 0.2f;

    private float failsafeTimer = 0f;
    private float lastHitStunTime = -10f;
    private int lastHealth;

    private readonly int hashIsRunning = Animator.StringToHash("isRunning");
    private readonly int hashIsGrounded = Animator.StringToHash("isGrounded");
    private readonly int hashVerticalSpeed = Animator.StringToHash("verticalSpeed");
    private readonly int hashAtk1Trigger = Animator.StringToHash("attack1");
    private readonly int hashAtk2Trigger = Animator.StringToHash("attack2");
    private readonly int hashAtk3Trigger = Animator.StringToHash("attack3");

    protected override void Awake()
    {
        base.Awake();
        myHealth = GetComponent<EnemyHealth>();
        UpdateShieldStatus();
    }

    protected virtual void Start()
    {
        if (myHealth != null) lastHealth = myHealth.maxHealth;
    }

    protected override void Update()
    {
        if (currentState == BossState.Dead) return;
        base.Update();
        UpdateShieldStatus();

        if (myHealth != null && myHealth.isDead && currentState != BossState.Dead)
        {
            DieAndDropCrown();
        }
    }

    private void UpdateShieldStatus()
    {
        if (myHealth == null || boss1Health == null) return;
        bool boss1IsAlive = !boss1Health.isDead && boss1Health.currentHealth > 0;

        if (boss1IsAlive)
        {
            myHealth.isInvincible = true;
            if (shieldEffect != null && !shieldEffect.activeSelf) shieldEffect.SetActive(true);
        }
        else
        {
            myHealth.isInvincible = false;
            if (shieldEffect != null && shieldEffect.activeSelf) shieldEffect.SetActive(false);
        }
    }

    private void DieAndDropCrown()
    {
        currentState = BossState.Dead;
        rb.linearVelocity = Vector2.zero;
        if (shieldEffect != null) shieldEffect.SetActive(false);

        if (crownPrefab != null)
        {
            Vector3 dropPosition = transform.position + new Vector3(0, 1.5f, 0);
            Instantiate(crownPrefab, dropPosition, Quaternion.identity);
        }
    }

    protected override void ExecuteAI()
    {
        if (!isAwake || player == null || currentState == BossState.Dead || myHealth == null) return;

        myHealth.isUnstoppable = (Time.time <= lastHitStunTime + hitStunCooldown);
        if (myHealth.currentHealth < lastHealth)
        {
            lastHealth = myHealth.currentHealth;
            if (!myHealth.isUnstoppable)
            {
                InterruptAttacks();
                ChangeState(BossState.Stunned);
                lastHitStunTime = Time.time;
            }
        }
        else if (myHealth.currentHealth > lastHealth) lastHealth = myHealth.currentHealth;

        if (HandleReactionState()) return;

        CheckEnvironment();

        if (currentState == BossState.Airborne)
        {
            float dirX = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(dirX * chaseSpeed, rb.linearVelocity.y);
            return;
        }

        if (currentState == BossState.Attacking)
        {
            failsafeTimer -= Time.deltaTime;
            if (failsafeTimer <= 0f) Event_EndAttack();
            return;
        }

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            HandleCooldownMovement();
            return;
        }

        // --- CƠ CHẾ MỚI: PHẢN XẠ NHANH ---
        // Nếu Boss đang chạy tới mà lọt vào tầm đánh, hủy bỏ thời gian chờ suy nghĩ và đánh luôn!
        if (currentState == BossState.Chase)
        {
            float currentDistSqr = (player.position - transform.position).sqrMagnitude;
            if (currentDistSqr <= attackRange * attackRange)
            {
                decisionLockTimer = 0f;
            }
        }

        if (Time.time < decisionLockTimer) return;
        EvaluateUtilityAndAct();
    }

    private void HandleCooldownMovement()
    {
        float distToPlayer = Vector2.Distance(transform.position, player.position);
        if (distToPlayer > attackRange)
        {
            ChasePlayer();
        }
        else
        {
            // --- SỬA ĐỔI: Khi đã ở trong tầm đánh thì đứng yên (Idle) nhìn người chơi, không tiến thêm nữa
            FacePlayer();
            ChangeState(BossState.Idle);
        }
    }

    private void InterruptAttacks()
    {
        if (anim != null)
        {
            anim.ResetTrigger(hashAtk1Trigger);
            anim.ResetTrigger(hashAtk2Trigger);
            anim.ResetTrigger(hashAtk3Trigger);
        }
    }

    private bool HandleReactionState()
    {
        if (myHealth.isKnockedBack || myHealth.isStunned) return true;
        else if (currentState == BossState.Stunned)
        {
            ChangeState(BossState.Idle);
            cooldownTimer = 0.1f;
        }
        return false;
    }

    public void WakeUpBoss()
    {
        isAwake = true;
    }

    #region CẢM BIẾN & DI CHUYỂN
    private void CheckEnvironment()
    {
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundBoxSize, 0f, groundLayer);

        if (anim != null)
        {
            anim.SetBool(hashIsGrounded, isGrounded);
            anim.SetFloat(hashVerticalSpeed, rb.linearVelocity.y);
        }

        if (!isGrounded) currentState = BossState.Airborne;
        else if (currentState == BossState.Airborne && rb.linearVelocity.y <= 0.01f) ChangeState(BossState.Idle);
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
        if (isGrounded) rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void FacePlayer()
    {
        float dirX = player.position.x - transform.position.x;
        if (Mathf.Abs(dirX) > 0.1f) Flip(Mathf.Sign(dirX));
    }
    #endregion

    #region THUẬT TOÁN TÍNH ĐIỂM (UTILITY AI)
    private void ChangeState(BossState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        if (anim == null) return;

        if (newState == BossState.Idle || newState == BossState.Stunned || newState == BossState.Attacking)
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

        bool isInAttackRange = distSqr <= (attackRange * attackRange);
        float scoreAtk1 = isInAttackRange ? 80f : 0f;
        float scoreAtk2 = isInAttackRange ? 75f : 0f;
        float scoreAtk3 = isInAttackRange ? 85f : 0f;

        bool isWallAhead = Physics2D.OverlapBox(wallCheck.position, wallBoxSize, 0f, groundLayer);
        bool isEdgeAhead = !Physics2D.OverlapBox(edgeCheck.position, edgeBoxSize, 0f, groundLayer);
        bool isPlayerHighUp = player.position.y > transform.position.y + 1.5f;

        float scoreJump = 0f;
        if (isGrounded)
        {
            if (isPlayerHighUp) scoreJump = 100f;
            else if (!isInAttackRange && (isWallAhead || isEdgeAhead)) scoreJump = 100f;
        }

        float scoreChase = (!isInAttackRange && !isWallAhead && !isEdgeAhead) ? 90f : 0f;

        if (lastAttack == 1) scoreAtk1 -= 60f;
        if (lastAttack == 2) scoreAtk2 -= 60f;
        if (lastAttack == 3) scoreAtk3 -= 60f;

        if (distSqr <= 1.0f) scoreAtk3 += 30f;

        if (scoreAtk1 > 0) scoreAtk1 += Random.Range(0f, 20f);
        if (scoreAtk2 > 0) scoreAtk2 += Random.Range(0f, 20f);
        if (scoreAtk3 > 0) scoreAtk3 += Random.Range(0f, 20f);

        float highestScore = Mathf.Max(scoreAtk1, scoreAtk2, scoreAtk3, scoreJump, scoreChase);

        if (highestScore == scoreJump && scoreJump > 0) Jump();
        else if (highestScore == scoreAtk1 && scoreAtk1 > 0) ExecuteSpecificAttack(1, hashAtk1Trigger);
        else if (highestScore == scoreAtk2 && scoreAtk2 > 0) ExecuteSpecificAttack(2, hashAtk2Trigger);
        else if (highestScore == scoreAtk3 && scoreAtk3 > 0) ExecuteSpecificAttack(3, hashAtk3Trigger);
        else ChasePlayer();

        decisionLockTimer = Time.time + decisionInertiaTime;
    }

    private void ExecuteSpecificAttack(int attackIndex, int triggerHash)
    {
        ChangeState(BossState.Attacking);
        lastAttack = attackIndex;
        failsafeTimer = 5f;
        FacePlayer();
        if (anim != null) anim.SetTrigger(triggerHash);
    }
    #endregion

    #region ANIMATION EVENTS
    public void Event_Atk1Hit()
    {
        if (attackPointAtk1 == null) return;
        Collider2D hit = Physics2D.OverlapBox(attackPointAtk1.position, hitboxAtk1, 0f, LayerMask.GetMask("Player"));
        ApplyDamage(hit, damageAtk1);
        AudioManager.instance?.PlaySFX("FinalBoss2");
        AudioManager.instance?.PlaySFX("FinalBoss2Atk1");
    }

    public void Event_Atk2Hit()
    {
        if (attackPointAtk2 == null) return;
        Collider2D hit = Physics2D.OverlapBox(attackPointAtk2.position, hitboxAtk2, 0f, LayerMask.GetMask("Player"));
        ApplyDamage(hit, damageAtk2);
        AudioManager.instance?.PlaySFX("FinalBoss2");
        AudioManager.instance?.PlaySFX("FinalBoss2Atk2");
    }

    public void Event_Atk3Hit()
    {
        if (attackPointAtk3 == null) return;
        Collider2D hit = Physics2D.OverlapCircle(attackPointAtk3.position, hitboxRadiusAtk3, LayerMask.GetMask("Player"));
        ApplyDamage(hit, damageAtk3);
        AudioManager.instance?.PlaySFX("FinalBoss2");
        AudioManager.instance?.PlaySFX("FinalBoss2Atk3");
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
        if (Random.Range(0f, 100f) <= comboChance) cooldownTimer = 0f;
        else cooldownTimer = attackCooldown;
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan; if (groundCheck != null) Gizmos.DrawWireCube(groundCheck.position, groundBoxSize);
        if (wallCheck != null) Gizmos.DrawWireCube(wallCheck.position, wallBoxSize);
        Gizmos.color = Color.blue; if (edgeCheck != null) Gizmos.DrawWireCube(edgeCheck.position, edgeBoxSize);
    }
}