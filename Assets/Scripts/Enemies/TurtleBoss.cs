using UnityEngine;
using System.Collections;

public class TurtleBoss : EnemyBase
{
    #region ENUMS & FSM LAYERS
    public enum Phase { Phase1_Normal, Phase2_Enraged }
    public enum MovementState { Idle, Chase }
    public enum CombatState { None, ShootingWalk, Biting, ShootingArc, ShootingIdle }
    public enum ReactionState { Normal, HitStunned }
    #endregion

    #region INSPECTOR VARIABLES
    [Header("--- CORE & PHASES ---")]
    public bool spriteFacesRight = false;
    [Range(0f, 1f)] public float phase2HealthThreshold = 0.5f;
    public bool isAwake = false; // --- MỚI: Biến ngủ đông ---

    [Header("--- ATTACK 1 & 4: HORIZONTAL BOMB ---")]
    public Transform[] horizontalFirePoints;
    public GameObject horizontalBombPrefab;
    public float horizontalShotDelay = 0.15f;

    [Header("--- ATTACK 3: ARC BOMB ---")]
    public Transform[] arcFirePoints;
    public GameObject arcBombPrefab;
    public float arcShotDelay = 0.2f;

    [Header("--- ATTACK 2: BITE ---")]
    public Transform bitePoint;
    public Vector2 biteBoxSize = new Vector2(2.5f, 2f);
    public int biteDamage = 2;

    [Header("--- AI RANGES & TUNING ---")]
    public float biteRange = 2.5f;
    public float midRange = 5f;
    public float baseAttackCooldown = 1.5f;
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

    // Caching Hashes
    private readonly int hashIsWalking = Animator.StringToHash("isWalking");
    private readonly int hashAttack1 = Animator.StringToHash("Attack1");
    private readonly int hashAttack2 = Animator.StringToHash("Attack2");
    private readonly int hashAttack3 = Animator.StringToHash("Attack3");
    private readonly int hashAttack4 = Animator.StringToHash("Attack4");
    private readonly int hashHitState = Animator.StringToHash("BTurtleHit");
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
            HandleCombatMovement(); // Dành riêng cho chiêu vừa đi vừa bắn
            return;
        }

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            ChangeMovementState(MovementState.Idle);
            return;
        }

        if (Time.time < decisionLockTimer) return;
        EvaluateUtilityAndAct();
    }

    private void UpdatePhaseSystem()
    {
        if (currentPhase == Phase.Phase1_Normal && bossHealth != null &&
            (float)bossHealth.currentHealth / bossHealth.maxHealth <= phase2HealthThreshold)
        {
            currentPhase = Phase.Phase2_Enraged;
            baseAttackCooldown *= 0.7f;
            moveSpeed *= 1.2f;
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
        if (anim != null) { anim.ResetTrigger(hashAttack1); anim.ResetTrigger(hashAttack2); anim.ResetTrigger(hashAttack3); anim.ResetTrigger(hashAttack4); }
    }

    private void EvaluateUtilityAndAct()
    {
        float distSqr = (player.position - transform.position).sqrMagnitude;
        FacePlayer();

        float biteScore = (distSqr <= biteRange * biteRange) ? 90f : 0f;
        float walkShootScore = (distSqr <= midRange * midRange && distSqr > biteRange * biteRange) ? 70f : 0f;
        float idleShootScore = (distSqr > midRange * midRange) ? 60f : 0f;
        float arcShootScore = (distSqr > midRange * midRange) ? 70f : 0f;
        float chaseScore = (distSqr > biteRange * biteRange) ? 50f : 0f;

        // Anti-Spam
        if (lastCombatState == CombatState.Biting) biteScore -= 50f;
        if (lastCombatState == CombatState.ShootingWalk) walkShootScore -= 50f;
        if (lastCombatState == CombatState.ShootingIdle) idleShootScore -= 50f;
        if (lastCombatState == CombatState.ShootingArc) arcShootScore -= 50f;

        // Random Noise
        biteScore += Random.Range(0, 15f); walkShootScore += Random.Range(0, 15f);
        idleShootScore += Random.Range(0, 15f); arcShootScore += Random.Range(0, 15f); chaseScore += Random.Range(0, 15f);

        float highest = Mathf.Max(biteScore, walkShootScore, idleShootScore, arcShootScore, chaseScore);

        if (highest == biteScore && biteScore > 0) ExecuteAttack(CombatState.Biting, hashAttack2);
        else if (highest == walkShootScore && walkShootScore > 0) ExecuteAttack(CombatState.ShootingWalk, hashAttack1);
        else if (highest == arcShootScore && arcShootScore > 0) ExecuteAttack(CombatState.ShootingArc, hashAttack3);
        else if (highest == idleShootScore && idleShootScore > 0) ExecuteAttack(CombatState.ShootingIdle, hashAttack4);
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

    private void HandleCombatMovement()
    {
        // Vận tốc cho chiêu vừa đi vừa bắn
        if (combatState == CombatState.ShootingWalk)
        {
            FacePlayer();
            float dirX = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(dirX * moveSpeed * 0.6f, rb.linearVelocity.y);
        }
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
        if (Mathf.Abs(dirX) > 0.1f)
        {
            float newScaleX = spriteFacesRight ? (dirX > 0 ? Mathf.Abs(transform.localScale.x) : -Mathf.Abs(transform.localScale.x)) : (dirX > 0 ? -Mathf.Abs(transform.localScale.x) : Mathf.Abs(transform.localScale.x));
            transform.localScale = new Vector3(newScaleX, transform.localScale.y, transform.localScale.z);
        }
    }

    // ANIMATION EVENTS
    public void Event_BiteHit()
    {
        Collider2D hit = Physics2D.OverlapBox(bitePoint.position, biteBoxSize, 0f, LayerMask.GetMask("Player"));
        if (hit != null) hit.GetComponent<PlayerHealth>()?.TakeDamage(biteDamage, transform);
    }

    public void Event_FireHorizontal() { if (activeAttackCoroutine != null) StopCoroutine(activeAttackCoroutine); activeAttackCoroutine = StartCoroutine(FireHorizontalRoutine()); }
    private IEnumerator FireHorizontalRoutine()
    {
        float dir = spriteFacesRight ? Mathf.Sign(transform.localScale.x) : -Mathf.Sign(transform.localScale.x);
        foreach (Transform fp in horizontalFirePoints)
        {
            if (fp == null) continue;
            GameObject bomb = ObjectPoolManager.Instance.Spawn(horizontalBombPrefab, fp.position, Quaternion.identity);
            bomb.SendMessage("Setup", dir, SendMessageOptions.DontRequireReceiver);
            yield return new WaitForSeconds(horizontalShotDelay);
        }
    }

    public void Event_FireArc() { if (activeAttackCoroutine != null) StopCoroutine(activeAttackCoroutine); activeAttackCoroutine = StartCoroutine(FireArcRoutine()); }
    private IEnumerator FireArcRoutine()
    {
        foreach (Transform fp in arcFirePoints)
        {
            if (fp == null) continue;
            ShootParabola(fp.position);
            yield return new WaitForSeconds(arcShotDelay);
        }
    }

    private void ShootParabola(Vector2 startPos)
    {
        if (player == null || arcBombPrefab == null) return;
        Rigidbody2D prefabRb = arcBombPrefab.GetComponent<Rigidbody2D>();
        float gravity = Mathf.Abs(Physics2D.gravity.y * (prefabRb ? prefabRb.gravityScale : 1f));
        float dx = player.position.x - startPos.x; float dy = player.position.y - startPos.y;
        float angleRad = 45f * Mathf.Deg2Rad; float cos = Mathf.Cos(angleRad);
        float denominator = 2 * (Mathf.Abs(dx) * Mathf.Tan(angleRad) - dy) * cos * cos;

        Vector2 vel = (denominator > 0f) ? new Vector2(Mathf.Sqrt((gravity * dx * dx) / denominator) * cos * Mathf.Sign(dx), Mathf.Sqrt((gravity * dx * dx) / denominator) * Mathf.Sin(angleRad)) : new Vector2(6f * Mathf.Sign(dx), 12f);
        GameObject bomb = ObjectPoolManager.Instance.Spawn(arcBombPrefab, startPos, Quaternion.identity);
        if (bomb.GetComponent<Rigidbody2D>()) bomb.GetComponent<Rigidbody2D>().linearVelocity = vel;
    }

    public void Event_EndAttack() { InterruptAttacks(); cooldownTimer = baseAttackCooldown; }

    public void WakeUpBoss()
    {
        isAwake = true;
    }
}