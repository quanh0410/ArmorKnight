using UnityEngine;
using System.Collections;

public class CentipedeBoss : EnemyBase
{
    #region ENUMS & FSM LAYERS
    public enum Phase { Phase1_Normal, Phase2_Enraged }
    public enum MovementState { Idle, Chase }
    public enum CombatState { None, Smashing, Thrusting, Slithering }
    public enum ReactionState { Normal, HitStunned }
    #endregion

    #region INSPECTOR VARIABLES
    [Header("--- CORE & PHASES ---")]
    public bool spriteFacesRight = false;
    [Range(0f, 1f)] public float phase2HealthThreshold = 0.5f;
    public bool isAwake = false; // --- MỚI: Biến ngủ đông ---

    [Header("--- ATTACK 1: SMASH ---")]
    public Transform smashPoint;
    public Vector2 smashBox = new Vector2(3f, 2f);
    public int smashDamage = 3;
    public GameObject shockwavePrefab;

    [Header("--- ATTACK 2: THRUST ---")]
    public Transform thrustPoint;
    public Vector2 thrustBox = new Vector2(4f, 1.5f);
    public int thrustDamage = 2;

    [Header("--- ATTACK 3: SLITHER DASH ---")]
    public float slitherSpeedMultiplier = 4.5f;
    public float slitherDeceleration = 10f;

    [Header("--- AI RANGES & TUNING ---")]
    public float smashRange = 3f;
    public float thrustRange = 6f;
    public float baseAttackCooldown = 1.2f;
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

    private int slitherComboCount = 0;
    private int slitherVariant = 0;
    private bool isBraking = false;

    private readonly int hashIsWalking = Animator.StringToHash("isWalking");
    private readonly int hashAttack1 = Animator.StringToHash("Attack1");
    private readonly int hashAttack2 = Animator.StringToHash("Attack2");
    private readonly int hashAttack3 = Animator.StringToHash("Attack3");
    private readonly int hashHitState = Animator.StringToHash("BCentipedeHit");
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
            HandleBrakingInertia();
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
            baseAttackCooldown *= 0.6f;
            moveSpeed *= 1.3f;
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
        isBraking = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (anim != null) { anim.ResetTrigger(hashAttack1); anim.ResetTrigger(hashAttack2); anim.ResetTrigger(hashAttack3); }
    }

    private void EvaluateUtilityAndAct()
    {
        float distSqr = (player.position - transform.position).sqrMagnitude;
        FacePlayer();

        float smashScore = (distSqr <= smashRange * smashRange) ? 80f : 0f;
        float thrustScore = (distSqr <= thrustRange * thrustRange) ? 70f : 0f;
        float slitherScore = (distSqr > thrustRange * thrustRange) ? 90f : 0f;
        float chaseScore = (distSqr > smashRange * smashRange) ? 50f : 0f;

        if (lastCombatState == CombatState.Smashing) smashScore -= 50f;
        if (lastCombatState == CombatState.Thrusting) thrustScore -= 50f;
        if (lastCombatState == CombatState.Slithering) slitherScore -= 50f;

        smashScore += Random.Range(0, 15f); thrustScore += Random.Range(0, 15f); slitherScore += Random.Range(0, 15f); chaseScore += Random.Range(0, 15f);

        float highest = Mathf.Max(smashScore, thrustScore, slitherScore, chaseScore);

        if (highest == smashScore && smashScore > 0) ExecuteAttack(CombatState.Smashing, hashAttack1);
        else if (highest == thrustScore && thrustScore > 0) ExecuteAttack(CombatState.Thrusting, hashAttack2);
        else if (highest == slitherScore && slitherScore > 0)
        {
            slitherVariant = Random.value < 0.5f ? 0 : 1;
            slitherComboCount = (slitherVariant == 1) ? 2 : 1;
            ExecuteAttack(CombatState.Slithering, hashAttack3);
        }
        else ChangeMovementState(MovementState.Chase);

        decisionLockTimer = Time.time + decisionInertiaTime;
    }

    private void ExecuteAttack(CombatState state, int animHash)
    {
        ChangeMovementState(MovementState.Idle);
        combatState = state; lastCombatState = state; failsafeTimer = 8f; // Tăng failsafe vì dash khá dài
        isBraking = false;
        if (anim != null) anim.SetTrigger(animHash);
    }

    private void HandleCombatFailsafe()
    {
        failsafeTimer -= Time.deltaTime;
        if (failsafeTimer <= 0f) Event_EndAttack();
    }

    private void HandleBrakingInertia()
    {
        if (combatState == CombatState.Slithering && isBraking)
        {
            float newVelX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, slitherDeceleration * Time.deltaTime);
            rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);
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
    public void Event_SmashHit()
    {
        Collider2D hit = Physics2D.OverlapBox(smashPoint.position, smashBox, 0f, LayerMask.GetMask("Player"));
        if (hit != null) hit.GetComponent<PlayerHealth>()?.TakeDamage(smashDamage, transform);
        if (shockwavePrefab != null) ObjectPoolManager.Instance.Spawn(shockwavePrefab, smashPoint.position, Quaternion.identity);
        CinemachineShake.Instance.ShakeCamera(0.2f);
    }

    public void Event_ThrustHit()
    {
        Collider2D hit = Physics2D.OverlapBox(thrustPoint.position, thrustBox, 0f, LayerMask.GetMask("Player"));
        if (hit != null) hit.GetComponent<PlayerHealth>()?.TakeDamage(thrustDamage, transform);
    }

    public void Event_StartSlitherMove()
    {
        isBraking = false;
        float dirX = spriteFacesRight ? Mathf.Sign(transform.localScale.x) : -Mathf.Sign(transform.localScale.x);
        rb.linearVelocity = new Vector2(dirX * moveSpeed * slitherSpeedMultiplier, rb.linearVelocity.y);
    }

    public void Event_StopSlitherMove() { isBraking = true; }

    public void Event_CheckCombo() { if (activeAttackCoroutine != null) StopCoroutine(activeAttackCoroutine); activeAttackCoroutine = StartCoroutine(HandleDashEndRoutine()); }
    private IEnumerator HandleDashEndRoutine()
    {
        while (combatState == CombatState.Slithering && isBraking && Mathf.Abs(rb.linearVelocity.x) > 0.5f) yield return null;
        if (reactionState == ReactionState.HitStunned) yield break;

        isBraking = false; rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (slitherComboCount == 2)
        {
            slitherComboCount = 1; yield return new WaitForSeconds(0.1f);
            if (reactionState == ReactionState.HitStunned) yield break;
            FacePlayer(); ExecuteAttack(CombatState.Slithering, hashAttack3);
            if (anim != null) anim.Play("BCentipedeAttack3", -1, 0f);
        }
        else
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (slitherVariant == 0 && distance <= smashRange) ExecuteAttack(CombatState.Smashing, hashAttack1);
            else Event_EndAttack();
        }
    }

    public void Event_EndAttack() { InterruptAttacks(); cooldownTimer = baseAttackCooldown; }

    public void WakeUpBoss()
    {
        isAwake = true;
    }
}