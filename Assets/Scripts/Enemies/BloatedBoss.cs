using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BloatedBoss : EnemyBase
{
    #region ENUMS & FSM LAYERS
    public enum Phase { Phase1_Normal, Phase2_Enraged }
    public enum MovementState { Idle, Chase, Reposition }
    public enum CombatState { None, Shooting, Summoning, Spiking, Thrusting }
    public enum ReactionState { Normal, HitStunned }
    #endregion

    #region INSPECTOR VARIABLES
    [Header("--- CORE & PHASES ---")]
    public bool spriteFacesRight = false;
    [Range(0f, 1f)] public float phase2HealthThreshold = 0.5f;
    public bool isAwake = false; // --- MỚI: Biến ngủ đông ---

    [Header("--- ATTACK 1: SHOOT ---")]
    public Transform shootPoint;
    public GameObject projectilePrefab;

    [Header("--- ATTACK 2: SUMMON ---")]
    public Transform summonPoint;
    public GameObject minionPrefab;
    public int maxMinions = 3;

    [Header("--- ATTACK 3: SPIKE CHASE ---")]
    public Transform groundSlamPoint;
    public GameObject spikePrefab;
    public float spikeSpacing = 1.2f;
    public float timeBetweenSpikes = 0.1f;
    public float largeSpikeScale = 2.5f;

    [Header("--- ATTACK 4: THRUST ---")]
    public Transform thrustPoint;
    public Vector2 thrustBox = new Vector2(3f, 2f);
    public int thrustDamage = 2;
    public float thrustDashForce = 12f;
    public float thrustStopDistance = 2f;

    [Header("--- AI RANGES & TUNING ---")]
    public float meleeRange = 3.5f;
    public float midRange = 7f;
    public float baseAttackCooldown = 0.8f;
    public float decisionInertiaTime = 0.2f;
    #endregion

    #region INTERNAL STATE & CACHING
    private Phase currentPhase = Phase.Phase1_Normal;
    private MovementState moveState = MovementState.Idle;
    private CombatState combatState = CombatState.None;
    private ReactionState reactionState = ReactionState.Normal;

    private CombatState lastCombatState = CombatState.None;

    private float cooldownTimer = 0f;
    private float decisionLockTimer = 0f;
    private float failsafeTimer = 0f;

    private Coroutine activeAttackCoroutine;
    private Coroutine spikeCoroutine;

    private HashSet<GameObject> activeMinions = new HashSet<GameObject>();
    private WaitForSeconds cachedSpikeDelay;
    private EnemyHealth bossHealth;

    private readonly int hashIsWalking = Animator.StringToHash("isWalking");
    private readonly int hashAttack1 = Animator.StringToHash("Attack1");
    private readonly int hashAttack2 = Animator.StringToHash("Attack2");
    private readonly int hashAttack3 = Animator.StringToHash("Attack3");
    private readonly int hashAttack4 = Animator.StringToHash("Attack4");
    private readonly int hashHitState = Animator.StringToHash("BBloatedHit");
    #endregion

    protected override void Awake()
    {
        base.Awake();
        bossHealth = GetComponent<EnemyHealth>();
        cachedSpikeDelay = new WaitForSeconds(timeBetweenSpikes);
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
            ChangeMovementState(MovementState.Idle);
            return;
        }

        if (Time.time < decisionLockTimer) return;

        EvaluateUtilityAndAct();
    }

    private void UpdatePhaseSystem()
    {
        if (currentPhase == Phase.Phase1_Normal && bossHealth != null)
        {
            float hpPercent = (float)bossHealth.currentHealth / bossHealth.maxHealth;
            if (hpPercent <= phase2HealthThreshold)
            {
                currentPhase = Phase.Phase2_Enraged;
                baseAttackCooldown *= 0.6f;
                moveSpeed *= 1.25f;
            }
        }
    }

    private bool HandleReactionState()
    {
        if (anim == null) return false;

        bool isHitAnimationPlaying = anim.GetCurrentAnimatorStateInfo(0).shortNameHash == hashHitState;

        if (isHitAnimationPlaying)
        {
            if (reactionState != ReactionState.HitStunned)
            {
                reactionState = ReactionState.HitStunned;

                if (spikeCoroutine != null)
                {
                    StopCoroutine(spikeCoroutine);
                    spikeCoroutine = null;
                }
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
        if (activeAttackCoroutine != null)
        {
            StopCoroutine(activeAttackCoroutine);
            activeAttackCoroutine = null;
        }
        combatState = CombatState.None;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (anim != null)
        {
            anim.ResetTrigger(hashAttack1);
            anim.ResetTrigger(hashAttack2);
            anim.ResetTrigger(hashAttack3);
            anim.ResetTrigger(hashAttack4);
        }
    }

    #region AI DECISION & ANTI-SPAM
    private void EvaluateUtilityAndAct()
    {
        float distSqr = (player.position - transform.position).sqrMagnitude;
        FacePlayer();

        float thrustScore = EvaluateThrustUtility(distSqr) + Random.Range(0f, 15f);
        float shootScore = EvaluateShootUtility(distSqr) + Random.Range(0f, 15f);
        float spikeScore = EvaluateSpikeUtility(distSqr) + Random.Range(0f, 15f);
        float summonScore = EvaluateSummonUtility() + Random.Range(0f, 15f);
        float chaseScore = EvaluateChaseUtility(distSqr) + Random.Range(0f, 15f);

        if (lastCombatState == CombatState.Thrusting) thrustScore -= 50f;
        if (lastCombatState == CombatState.Shooting) shootScore -= 50f;
        if (lastCombatState == CombatState.Spiking) spikeScore -= 50f;
        if (lastCombatState == CombatState.Summoning) summonScore -= 50f;

        float highestScore = Mathf.Max(thrustScore, shootScore, spikeScore, summonScore, chaseScore);

        if (highestScore == thrustScore && thrustScore > 0) ExecuteAttack(CombatState.Thrusting, hashAttack4);
        else if (highestScore == spikeScore && spikeScore > 0) ExecuteAttack(CombatState.Spiking, hashAttack3);
        else if (highestScore == summonScore && summonScore > 0) ExecuteAttack(CombatState.Summoning, hashAttack2);
        else if (highestScore == shootScore && shootScore > 0) ExecuteAttack(CombatState.Shooting, hashAttack1);
        else ChangeMovementState(MovementState.Chase);

        decisionLockTimer = Time.time + decisionInertiaTime;
    }

    private float EvaluateThrustUtility(float distSqr) => (distSqr <= meleeRange * meleeRange) ? 80f : 0f;
    private float EvaluateShootUtility(float distSqr) => (distSqr <= midRange * midRange) ? 60f : 0f;
    private float EvaluateSpikeUtility(float distSqr) => (distSqr > meleeRange * meleeRange) ? 80f : 0f;
    private float EvaluateSummonUtility() => (activeMinions.Count >= maxMinions) ? 0f : 90f - (activeMinions.Count * 30f);
    private float EvaluateChaseUtility(float distSqr) => (distSqr > meleeRange * meleeRange) ? 60f : 0f;
    #endregion

    private void ExecuteAttack(CombatState state, int animHash)
    {
        ChangeMovementState(MovementState.Idle);

        lastCombatState = state;
        combatState = state;
        failsafeTimer = 5f;

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
    }

    private void FacePlayer()
    {
        if (player == null) return;
        float dirX = player.position.x - transform.position.x;
        if (Mathf.Abs(dirX) > 0.1f)
        {
            float currentAbsScaleX = Mathf.Abs(transform.localScale.x);
            float newScaleX = spriteFacesRight ? (dirX > 0 ? currentAbsScaleX : -currentAbsScaleX) : (dirX > 0 ? -currentAbsScaleX : currentAbsScaleX);
            transform.localScale = new Vector3(newScaleX, transform.localScale.y, transform.localScale.z);
        }
    }

    public void RegisterMinion(GameObject minion) => activeMinions.Add(minion);
    public void UnregisterMinion(GameObject minion) => activeMinions.Remove(minion);

    #region ANIMATION EVENTS
    public void Event_Shoot()
    {
        if (projectilePrefab == null || shootPoint == null) return;
        float dir = spriteFacesRight ? Mathf.Sign(transform.localScale.x) : -Mathf.Sign(transform.localScale.x);

        GameObject bullet = ObjectPoolManager.Instance.Spawn(projectilePrefab, shootPoint.position, Quaternion.identity);

        // 1. SỬA LỖI ĐẠN: Dùng SendMessage chuẩn, KHÔNG dùng Interface nữa để khớp đạn của bạn
        bullet.SendMessage("Setup", dir, SendMessageOptions.DontRequireReceiver);
    }

    public void Event_Summon()
    {
        if (minionPrefab == null || summonPoint == null) return;
        GameObject minion = Instantiate(minionPrefab, summonPoint.position, Quaternion.identity);
        RegisterMinion(minion);
    }

    public void Event_SpikeLine()
    {
        if (player == null || groundSlamPoint == null) return;

        if (spikeCoroutine != null) StopCoroutine(spikeCoroutine);
        spikeCoroutine = StartCoroutine(SpikeStaticTargetRoutine());
    }

    // ==========================================
    // 2. SỬA LỖI GAI: KHÓA MỤC TIÊU CỐ ĐỊNH (STATIC TARGET)
    // ==========================================
    private IEnumerator SpikeStaticTargetRoutine()
    {
        Vector2 startPos = groundSlamPoint.position;
        float currentX = startPos.x;

        // Bắt chết tọa độ X của Player tại THỜI ĐIỂM Boss đập tay
        float targetX = player.position.x;

        float totalDistance = Mathf.Abs(targetX - currentX);
        float dirX = Mathf.Sign(targetX - currentX);

        // Nếu Player đứng quá gần (ngay dưới chân Boss)
        if (dirX == 0) dirX = spriteFacesRight ? Mathf.Sign(transform.localScale.x) : -Mathf.Sign(transform.localScale.x);

        // Tính toán trước tổng số lượng gai cần thiết để chạy tới tọa độ targetX
        int numberOfSpikes = Mathf.FloorToInt(totalDistance / spikeSpacing);
        if (numberOfSpikes < 1) numberOfSpikes = 1; // Ít nhất 1 cái gai dưới chân

        for (int i = 1; i <= numberOfSpikes; i++)
        {
            if (reactionState == ReactionState.HitStunned) yield break;

            currentX += dirX * spikeSpacing;
            Vector2 spawnPos = new Vector2(currentX, startPos.y);

            // Nếu là cái gai chốt hạ cuối cùng
            if (i == numberOfSpikes)
            {
                // Ép vị trí chốt hạ đúng ngay tọa độ targetX (phòng sai số chia)
                spawnPos.x = targetX;

                GameObject finalSpike = ObjectPoolManager.Instance.Spawn(spikePrefab, spawnPos, Quaternion.identity);
                finalSpike.transform.localScale = new Vector3(dirX * largeSpikeScale, largeSpikeScale, 1f);
                CinemachineShake.Instance.ShakeCamera(0.3f);
            }
            else // Các gai nhỏ trên đường đi
            {
                GameObject spike = ObjectPoolManager.Instance.Spawn(spikePrefab, spawnPos, Quaternion.identity);
                spike.transform.localScale = new Vector3(dirX * 1f, 1f, 1f);
                CinemachineShake.Instance.ShakeCamera(0.05f);
            }

            yield return cachedSpikeDelay;
        }
    }

    public void Event_StartThrustDash()
    {
        if (player == null) return;
        if (activeAttackCoroutine != null) StopCoroutine(activeAttackCoroutine);
        activeAttackCoroutine = StartCoroutine(ThrustDashRoutine());
    }

    private IEnumerator ThrustDashRoutine()
    {
        float dirX = spriteFacesRight ? Mathf.Sign(transform.localScale.x) : -Mathf.Sign(transform.localScale.x);
        rb.linearVelocity = new Vector2(dirX * thrustDashForce, rb.linearVelocity.y);

        while (combatState == CombatState.Thrusting)
        {
            if (reactionState == ReactionState.HitStunned) yield break;

            float distToPlayer = Mathf.Abs(player.position.x - transform.position.x);
            if (distToPlayer <= thrustStopDistance)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
            }
            yield return null;
        }
    }

    public void Event_ThrustHit()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        Collider2D hit = Physics2D.OverlapBox(thrustPoint.position, thrustBox, 0f, LayerMask.GetMask("Player"));
        if (hit != null) hit.GetComponent<PlayerHealth>()?.TakeDamage(thrustDamage, transform);

        CinemachineShake.Instance.ShakeCamera(0.15f);
    }

    public void Event_EndAttack()
    {
        InterruptAttacks();
        cooldownTimer = baseAttackCooldown;
    }

    public void WakeUpBoss()
    {
        isAwake = true;
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, midRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
        if (thrustPoint != null) { Gizmos.color = Color.red; Gizmos.DrawWireCube(thrustPoint.position, thrustBox); }
    }
}