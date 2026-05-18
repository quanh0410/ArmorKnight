using UnityEngine;
using System.Collections;

public class DungeonBoss : EnemyBase
{
    #region ENUMS & FSM LAYERS
    public enum Phase { Phase1_Normal, Phase2_Enraged }
    public enum MovementState { Idle, Chase }
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
    public float baseAttackCooldown = 2f;
    public float decisionInertiaTime = 0.2f;
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
    private Coroutine activeAttackCoroutine;
    private EnemyHealth bossHealth;
    private Collider2D arenaBounds;

    private readonly int hashIsWalking = Animator.StringToHash("isWalking");
    private readonly int hashAttack = Animator.StringToHash("Attack");
    private readonly int hashTeleOut = Animator.StringToHash("TeleOut");
    private readonly int hashTeleIn = Animator.StringToHash("TeleIn");
    private readonly int hashCast = Animator.StringToHash("Cast");
    private readonly int hashHitState = Animator.StringToHash("Hit"); // Thay tên theo Anim Hit của bạn
    #endregion

    protected override void Awake()
    {
        base.Awake();
        bossHealth = GetComponent<EnemyHealth>();
    }

    protected override void ExecuteAI()
    {
        if (!isAwake || player == null) return;

        UpdatePhaseSystem();
        if (HandleReactionState()) return;

        if (combatState != CombatState.None)
        {
            HandleCombatFailsafe();
            return;
        }

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;

            // --- CẢI TIẾN: Cho phép lội bộ rượt theo dù đang chờ hồi chiêu ---
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer > meleeAttackRange)
            {
                ChangeMovementState(MovementState.Chase);
            }
            else
            {
                ChangeMovementState(MovementState.Idle);
                FacePlayer(); // Ở sát mặt thì đứng lườm tạo áp lực
            }
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
            baseAttackCooldown *= 0.5f; // Đánh nhanh gấp đôi khi máu < 30%
            moveSpeed *= 1.5f;
        }
    }

    private bool HandleReactionState()
    {
        if (anim == null) return false;
        if (anim.GetCurrentAnimatorStateInfo(0).shortNameHash == hashHitState)
        {
            if (reactionState != ReactionState.HitStunned)
            {
                reactionState = ReactionState.HitStunned;
                InterruptAttacks();
            }
            return true;
        }
        else if (reactionState == ReactionState.HitStunned)
        {
            reactionState = ReactionState.Normal;
            cooldownTimer = 0.2f;
        }
        return false;
    }

    private void InterruptAttacks()
    {
        if (activeAttackCoroutine != null) { StopCoroutine(activeAttackCoroutine); activeAttackCoroutine = null; }
        combatState = CombatState.None;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (anim != null) { anim.ResetTrigger(hashAttack); anim.ResetTrigger(hashTeleOut); anim.ResetTrigger(hashTeleIn); anim.ResetTrigger(hashCast); }
    }

    private void EvaluateUtilityAndAct()
    {
        float distSqr = (player.position - transform.position).sqrMagnitude;
        FacePlayer();

        float meleeScore = (distSqr <= meleeAttackRange * meleeAttackRange) ? 100f : 0f;
        float teleportScore = (distSqr > meleeAttackRange * meleeAttackRange) ? 60f : 0f;
        float castScore = (distSqr > meleeAttackRange * meleeAttackRange) ? 40f : 0f;
        float chaseScore = (distSqr > meleeAttackRange * meleeAttackRange) ? 50f : 0f;

        if (currentPhase == Phase.Phase2_Enraged) castScore += 30f; // Tăng tỉ lệ xài lỗ đen ở Phase 2

        if (lastCombatState == CombatState.Meleeing) meleeScore -= 50f;
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
        if (newState == MovementState.Idle) { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); if (anim != null) anim.SetBool(hashIsWalking, false); }
        else if (newState == MovementState.Chase)
        {
            if (anim != null) anim.SetBool(hashIsWalking, true);
            float dirX = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(dirX * moveSpeed, rb.linearVelocity.y);
        }
    }

    private void FacePlayer()
    {
        if (player == null) return;
        float dirX = player.position.x - transform.position.x;
        if (Mathf.Abs(dirX) > 0.1f) { float direction = dirX > 0 ? -1f : 1f; Flip(direction); }
    }

    // ANIMATION EVENTS (Melee)
    public void TriggerMeleeDamage()
    {
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);
        if (hitPlayer != null) hitPlayer.GetComponent<PlayerHealth>()?.TakeDamage(meleeDamage, transform);
    }

    // COROUTINES (Teleport & Spell)
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

        anim.SetTrigger(hashAttack); // Quét sát thương dùng chung hàm TriggerMeleeDamage
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

    public void Event_EndAttack() { InterruptAttacks(); cooldownTimer = baseAttackCooldown; }
}