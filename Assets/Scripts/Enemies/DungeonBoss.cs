using UnityEngine;
using System.Collections;

public class DungeonBoss : EnemyBase
{
    #region ENUMS & FSM LAYERS
    public enum Phase { Phase1_Normal, Phase2_Enraged }
    public enum MovementState { Idle, Chase, CombatWalk }
    public enum CombatState { None, Meleeing, Teleporting, Casting }
    public enum ReactionState { Normal, HitStunned }
    #endregion

    #region INSPECTOR VARIABLES
    [Header("--- CORE & PHASES ---")]
    public bool isAwake = false;
    [Range(0f, 1f)] public float phase2Threshold = 0.3f;

    [Header("--- ATTACK 1: MELEE ---")]
    public Transform attackPoint;
    public float attackRadius = 1f;
    public int meleeDamage = 15;
    public LayerMask playerLayer;

    [Header("--- ATTACK 2: TELEPORT SLASH ---")]
    public float teleportOffset = 1.5f;
    public GameObject teleportEffectPrefab;

    [Header("--- ATTACK 3: BLACK HOLE ---")]
    public GameObject blackHolePrefab;
    public float spellSpawnHeight = 3f;
    public float tripleSpellOffset = 3.5f;

    [Header("--- AI RANGES & TUNING ---")]
    public float meleeAttackRange = 1.5f;
    public float baseAttackCooldown = 1.5f;
    public float decisionInertiaTime = 0.2f;

    [Header("--- AGGRESSIVE UPGRADES ---")]
    public float hitStunCooldown = 3f;
    [Range(0f, 100f)] public float comboChance = 30f;
    #endregion

    #region INTERNAL STATE
    private Phase currentPhase = Phase.Phase1_Normal;
    private MovementState moveState = MovementState.Idle;
    private CombatState combatState = CombatState.None;
    private CombatState lastCombatState = CombatState.None;
    private ReactionState reactionState = ReactionState.Normal;

    private float cooldownTimer = 0f;
    private float decisionLockTimer = 0f;
    private float failsafeTimer = 0f;
    private float lastHitStunTime = -10f;

    private Coroutine activeAttackCoroutine;
    private EnemyHealth bossHealth;
    private Collider2D arenaBounds;

    // BIẾN MỚI: Theo dõi máu để biết khi nào bị đánh trúng
    private int lastHealth;

    private readonly int hashIsWalking = Animator.StringToHash("isWalking");
    private readonly int hashAttack = Animator.StringToHash("Attack");
    private readonly int hashTeleOut = Animator.StringToHash("TeleOut");
    private readonly int hashTeleIn = Animator.StringToHash("TeleIn");
    private readonly int hashCast = Animator.StringToHash("Cast");
    // Đã xóa hashHitState vì không thèm dùng Animator để check lỗi nữa
    #endregion

    protected override void Awake()
    {
        base.Awake();
        bossHealth = GetComponent<EnemyHealth>();
    }

    protected virtual void Start()
    {
        if (bossHealth != null)
        {
            lastHealth = bossHealth.maxHealth;
        }
    }

    protected override void ExecuteAI()
    {
        if (!isAwake || player == null || bossHealth == null) return;

        // 1. ĐỒNG BỘ GIÁP SIÊU VIỆT
        bossHealth.isUnstoppable = (Time.time <= lastHitStunTime + hitStunCooldown);

        // 2. PHÁT HIỆN BỊ ĐÁNH CHÍNH XÁC QUA MÁU TỤT
        if (bossHealth.currentHealth < lastHealth)
        {
            lastHealth = bossHealth.currentHealth;

            // Nếu không có giáp siêu việt -> Chắc chắn bị khựng -> Cắt ngay mọi đòn đánh
            if (!bossHealth.isUnstoppable)
            {
                InterruptAttacks();
                reactionState = ReactionState.HitStunned;
                lastHitStunTime = Time.time;
            }
        }
        else if (bossHealth.currentHealth > lastHealth) // Cập nhật lại nếu quái hồi máu
        {
            lastHealth = bossHealth.currentHealth;
        }

        UpdatePhaseSystem();

        // 3. XỬ LÝ KHI ĐANG BỊ KHỰNG VẬT LÝ
        if (HandleReactionState()) return;

        if (combatState != CombatState.None)
        {
            HandleCombatFailsafe();
            return;
        }

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            HandleCooldownMovement();
            return;
        }

        if (Time.time < decisionLockTimer) return;
        EvaluateUtilityAndAct();
    }

    public void WakeUpBoss() { isAwake = true; }
    public void SetArenaBounds(Collider2D bounds) { arenaBounds = bounds; }

    private void UpdatePhaseSystem()
    {
        if (currentPhase == Phase.Phase1_Normal && bossHealth != null &&
            (float)bossHealth.currentHealth / bossHealth.maxHealth <= phase2Threshold)
        {
            currentPhase = Phase.Phase2_Enraged;
            baseAttackCooldown *= 0.5f;
            moveSpeed *= 1.3f;
            comboChance = 60f;
        }
    }

    private bool HandleReactionState()
    {
        // KIỂM TRA THEO BIẾN VẬT LÝ TỪ ENEMY HEALTH (Chính xác tuyệt đối)
        if (bossHealth.isKnockedBack || bossHealth.isStunned)
        {
            return true; // Boss đang bị đẩy lùi/choáng, dừng nghĩ AI
        }
        else if (reactionState == ReactionState.HitStunned)
        {
            // Vừa kết thúc đẩy lùi
            reactionState = ReactionState.Normal;
            cooldownTimer = 0.1f; // Phản đòn lập tức
        }
        return false;
    }

    private void HandleCooldownMovement()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer > meleeAttackRange)
        {
            ChangeMovementState(MovementState.Chase);
        }
        else
        {
            FacePlayer();
            ChangeMovementState(MovementState.CombatWalk);
        }
    }

    private void InterruptAttacks()
    {
        if (activeAttackCoroutine != null) { StopCoroutine(activeAttackCoroutine); activeAttackCoroutine = null; }

        // Trả AI về trạng thái sạch sẽ nhất
        combatState = CombatState.None;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (anim != null)
        {
            anim.ResetTrigger(hashAttack);
            anim.ResetTrigger(hashTeleOut);
            anim.ResetTrigger(hashTeleIn);
            anim.ResetTrigger(hashCast);
        }
    }

    private void EvaluateUtilityAndAct()
    {
        float distSqr = (player.position - transform.position).sqrMagnitude;
        FacePlayer();

        float meleeScore = (distSqr <= meleeAttackRange * meleeAttackRange) ? 100f : 0f;
        float teleportScore = (distSqr > meleeAttackRange * meleeAttackRange) ? 60f : 0f;
        float castScore = (distSqr > meleeAttackRange * meleeAttackRange) ? 40f : 0f;
        float chaseScore = (distSqr > meleeAttackRange * meleeAttackRange) ? 50f : 0f;

        if (currentPhase == Phase.Phase2_Enraged) castScore += 30f;

        if (lastCombatState == CombatState.Meleeing) meleeScore -= 30f;
        if (lastCombatState == CombatState.Teleporting) teleportScore -= 50f;
        if (lastCombatState == CombatState.Casting) castScore -= 50f;

        meleeScore += Random.Range(0, 15f); teleportScore += Random.Range(0, 15f); castScore += Random.Range(0, 15f); chaseScore += Random.Range(0, 15f);

        float highest = Mathf.Max(meleeScore, teleportScore, castScore, chaseScore);

        if (highest == meleeScore && meleeScore > 0) ExecuteAttack(CombatState.Meleeing, hashAttack);
        else if (highest == teleportScore && teleportScore > 0)
        {
            combatState = CombatState.Teleporting; lastCombatState = CombatState.Teleporting; failsafeTimer = 5f;
            if (activeAttackCoroutine != null) StopCoroutine(activeAttackCoroutine);
            activeAttackCoroutine = StartCoroutine(TeleportSlashRoutine());
        }
        else if (highest == castScore && castScore > 0)
        {
            combatState = CombatState.Casting; lastCombatState = CombatState.Casting; failsafeTimer = 5f;
            if (activeAttackCoroutine != null) StopCoroutine(activeAttackCoroutine);
            activeAttackCoroutine = StartCoroutine(CastSpellRoutine(currentPhase == Phase.Phase2_Enraged));
        }
        else ChangeMovementState(MovementState.Chase);

        decisionLockTimer = Time.time + decisionInertiaTime;
    }

    private void ExecuteAttack(CombatState state, int animHash)
    {
        ChangeMovementState(MovementState.Idle);
        combatState = state; lastCombatState = state; failsafeTimer = 5f;
        if (anim != null) anim.SetTrigger(animHash);
    }

    private void HandleCombatFailsafe()
    {
        failsafeTimer -= Time.deltaTime;
        if (failsafeTimer <= 0f) Event_EndAttack();
    }

    private void ChangeMovementState(MovementState newState)
    {
        moveState = newState;
        if (newState == MovementState.Idle)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (anim != null) anim.SetBool(hashIsWalking, false);
        }
        else if (newState == MovementState.Chase)
        {
            if (anim != null) anim.SetBool(hashIsWalking, true);
            float dirX = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(dirX * moveSpeed, rb.linearVelocity.y);
        }
        else if (newState == MovementState.CombatWalk)
        {
            if (anim != null) anim.SetBool(hashIsWalking, true);
            float dirX = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(dirX * (moveSpeed * 0.5f), rb.linearVelocity.y);
        }
    }

    private void FacePlayer()
    {
        if (player == null) return;
        float dirX = player.position.x - transform.position.x;
        if (Mathf.Abs(dirX) > 0.1f) { float direction = dirX > 0 ? -1f : 1f; Flip(direction); }
    }

    public void TriggerMeleeDamage()
    {
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);
        if (hitPlayer != null) hitPlayer.GetComponent<PlayerHealth>()?.TakeDamage(meleeDamage, transform);
    }

    private IEnumerator TeleportSlashRoutine()
    {
        ChangeMovementState(MovementState.Idle);
        anim.SetTrigger(hashTeleOut);
        yield return new WaitForSeconds(0.5f);
        if (reactionState == ReactionState.HitStunned) yield break;

        float bossSideRelativeToPlayer = Mathf.Sign(transform.position.x - player.position.x);
        float targetX = player.position.x - (bossSideRelativeToPlayer * teleportOffset);

        if (arenaBounds != null)
        {
            float minWallX = arenaBounds.bounds.min.x; float maxWallX = arenaBounds.bounds.max.x;
            targetX = Mathf.Clamp(targetX, minWallX + 1.5f, maxWallX - 1.5f);
        }

        transform.position = new Vector2(targetX, transform.position.y);
        FacePlayer();

        anim.SetTrigger(hashTeleIn);
        yield return new WaitForSeconds(0.5f);
        if (reactionState == ReactionState.HitStunned) yield break;

        anim.SetTrigger(hashAttack);
    }

    private IEnumerator CastSpellRoutine(bool isTriple)
    {
        ChangeMovementState(MovementState.Idle);
        anim.SetTrigger(hashCast);
        yield return new WaitForSeconds(0.5f);
        if (reactionState == ReactionState.HitStunned) yield break;

        if (blackHolePrefab != null)
        {
            Vector2 centralPos = new Vector2(player.position.x, player.position.y + spellSpawnHeight);
            Instantiate(blackHolePrefab, centralPos, Quaternion.identity);

            if (isTriple)
            {
                Instantiate(blackHolePrefab, centralPos + Vector2.left * tripleSpellOffset, Quaternion.identity);
                Instantiate(blackHolePrefab, centralPos + Vector2.right * tripleSpellOffset, Quaternion.identity);
            }
        }
        Event_EndAttack();
    }

    public void Event_EndAttack()
    {
        InterruptAttacks();

        if (Random.Range(0f, 100f) <= comboChance)
        {
            cooldownTimer = 0f;
        }
        else
        {
            cooldownTimer = baseAttackCooldown;
        }
    }
}