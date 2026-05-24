using UnityEngine;
using System.Collections;

public class KnightEnemy : EnemyBase
{
    #region ENUMS & FSM LAYERS
    public enum MovementState { Idle, Chase }
    public enum CombatState { None, Attack1, Attack2, ThrustAttack, RunAttack, Defending, Jumping, Dodging }
    public enum ReactionState { Normal, HitStunned }
    #endregion

    #region INSPECTOR VARIABLES
    [Header("--- VŨ KHÍ & SÁT THƯƠNG ---")]
    public Transform attackPoint;
    public Vector2 meleeBox = new Vector2(2f, 1.5f);
    public Vector2 thrustBox = new Vector2(3.5f, 1f);
    public Vector2 runAttackBox = new Vector2(2.5f, 2f);
    public int attackDamage = 15;

    [Header("--- THÔNG SỐ KỸ NĂNG & NÉ TRÁNH ---")]
    public float dashSpeed = 12f;
    public float jumpForceX = 7f;
    public float jumpForceY = 10f;
    public float dodgeForceX = 8f;
    public float defendDuration = 1.0f;

    [Header("--- TẦM NHÌN AI & COOLDOWN ---")]
    public float meleeRange = 2.2f;
    public float midRange = 5.5f;
    public float jumpRange = 9f;
    public float baseAttackCooldown = 1.2f;
    public float decisionInertiaTime = 0.2f;

    [Header("--- TÍNH NĂNG CHUYỂN MAP (SAU KHI THU PHỤC) ---")]
    public string sceneToLoad = "Castle_Scene";
    public int spawnPointID = 0;

    private bool isDoorMode = false;
    private bool isPlayerInRange = false;

    public bool isAwake = false;
    #endregion

    #region INTERNAL STATE
    private MovementState moveState = MovementState.Idle;
    public CombatState combatState = CombatState.None;
    private CombatState lastCombatState = CombatState.None;
    private ReactionState reactionState = ReactionState.Normal;

    private float cooldownTimer = 0f;
    private float decisionLockTimer = 0f;
    private float failsafeTimer = 0f;
    private Coroutine activeActionCoroutine;

    private Animator playerAnim;

    private readonly int hashIsRunning = Animator.StringToHash("isRunning");
    private readonly int hashAttack1 = Animator.StringToHash("Attack1");
    private readonly int hashAttack2 = Animator.StringToHash("Attack2");
    private readonly int hashAttack3 = Animator.StringToHash("Attack3");
    private readonly int hashRunAttack = Animator.StringToHash("RunAttack");
    private readonly int hashDefend = Animator.StringToHash("Defend");
    private readonly int hashJump = Animator.StringToHash("Jump");
    private readonly int hashHitState = Animator.StringToHash("EKnightHit");
    #endregion

    protected override void Awake()
    {
        base.Awake();
        if (player != null) playerAnim = player.GetComponent<Animator>();
    }

    protected override void Update()
    {
        base.Update(); // CỰC KỲ QUAN TRỌNG: Gọi Update của EnemyBase để nó xử lý máu, AI, chết...

        // Nếu đã bị thu phục thành cửa thì chạy logic này:
        if (isDoorMode && player != null)
        {
            float distanceToBoss = Vector2.Distance(player.position, transform.position);
            bool isCloseToBoss = distanceToBoss <= 3f;

            if (isCloseToBoss && !isPlayerInRange)
            {
                isPlayerInRange = true;
                if (InteractionUI.instance != null) InteractionUI.instance.Show(transform, "[S] Vào lâu đài");
            }
            else if (!isCloseToBoss && isPlayerInRange)
            {
                isPlayerInRange = false;
                if (InteractionUI.instance != null) InteractionUI.instance.Hide();
            }

            if (isPlayerInRange && Input.GetKeyDown(KeyCode.S))
            {
                if (InteractionUI.instance != null) InteractionUI.instance.Hide();
                if (FadeManager.instance != null)
                {
                    FadeManager.instance.StartTransition(sceneToLoad, gameObject.scene.name, spawnPointID, player.gameObject);
                }
            }
        }
    }

    protected override void ExecuteAI()
    {
        if (!isAwake || player == null) return;
        if (HandleReactionState()) return;

        if (combatState != CombatState.None)
        {
            HandleCombatFailsafe();
            return;
        }

        bool isPlayerAttacking = IsPlayerAttacking();

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;

            if (isPlayerAttacking && Time.time >= decisionLockTimer)
            {
                EvaluateDefenseOverride();
            }
            else
            {
                // FIX: Chỉ tiếp cận nếu ở xa. Đủ gần thì đứng lườm
                float distSqr = (player.position - transform.position).sqrMagnitude;
                if (distSqr > meleeRange * meleeRange) ChangeMovementState(MovementState.Chase);
                else { ChangeMovementState(MovementState.Idle); FacePlayer(); }
            }
            return;
        }

        if (Time.time < decisionLockTimer) return;
        EvaluateUtilityAndAct(isPlayerAttacking);
    }

    private bool IsPlayerAttacking()
    {
        if (playerAnim == null) return false;
        AnimatorClipInfo[] clipInfo = playerAnim.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0)
        {
            string clipName = clipInfo[0].clip.name.ToLower();
            if (clipName.Contains("attack") || clipName.Contains("slash") || clipName.Contains("combo")) return true;
        }
        return false;
    }

    #region REACTION & INTERRUPT
    private bool HandleReactionState()
    {
        if (anim == null) return false;

        bool isHitAnimationPlaying = anim.GetCurrentAnimatorStateInfo(0).shortNameHash == hashHitState;

        if (isHitAnimationPlaying)
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
            cooldownTimer = 0.1f;
        }
        return false;
    }

    private void InterruptAttacks()
    {
        if (activeActionCoroutine != null) { StopCoroutine(activeActionCoroutine); activeActionCoroutine = null; }
        combatState = CombatState.None;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (anim != null)
        {
            anim.ResetTrigger(hashAttack1); anim.ResetTrigger(hashAttack2); anim.ResetTrigger(hashAttack3);
            anim.ResetTrigger(hashRunAttack); anim.ResetTrigger(hashDefend); anim.ResetTrigger(hashJump);
        }
    }
    #endregion

    #region AI QUYẾT ĐỊNH (UTILITY AI)
    private void EvaluateDefenseOverride()
    {
        float distSqr = (player.position - transform.position).sqrMagnitude;
        if (distSqr > midRange * midRange) return;

        FacePlayer();
        float rand = Random.value;

        if (rand < 0.7f) ExecuteAction(CombatState.Defending, hashDefend);
        else ExecuteAction(CombatState.Dodging, hashJump);
    }

    private void EvaluateUtilityAndAct(bool isPlayerAttacking)
    {
        float distSqr = (player.position - transform.position).sqrMagnitude;
        FacePlayer();

        float attack1Score = (distSqr <= meleeRange * meleeRange) ? 80f : 0f;
        float attack2Score = (distSqr <= meleeRange * meleeRange) ? 75f : 0f;
        float thrustScore = (distSqr > meleeRange * meleeRange && distSqr <= midRange * midRange) ? 85f : 0f;
        float runAttackScore = (distSqr > midRange * midRange && distSqr <= jumpRange * jumpRange) ? 75f : 0f;

        // FIX 1: Giới hạn tầm nhảy, nếu xa hơn jumpRange thì điểm nhảy = 0
        float jumpScore = (distSqr > midRange * midRange && distSqr <= jumpRange * jumpRange) ? 90f : 0f;

        // FIX 2: Thêm điểm chạy bộ (Chase) khi ở quá xa
        float chaseScore = (distSqr > jumpRange * jumpRange) ? 100f : 50f;

        float defendScore = 0f;
        float dodgeScore = 0f;

        if (isPlayerAttacking && distSqr <= midRange * midRange)
        {
            defendScore = 95f;
            dodgeScore = 80f;
        }

        if (lastCombatState == CombatState.Attack1) attack1Score -= 60f;
        if (lastCombatState == CombatState.Attack2) attack2Score -= 60f;
        if (lastCombatState == CombatState.ThrustAttack) thrustScore -= 60f;
        if (lastCombatState == CombatState.RunAttack) runAttackScore -= 60f;
        if (lastCombatState == CombatState.Jumping) jumpScore -= 60f;
        if (lastCombatState == CombatState.Defending) defendScore -= 80f;
        if (lastCombatState == CombatState.Dodging) dodgeScore -= 80f;

        attack1Score += Random.Range(0, 15f); attack2Score += Random.Range(0, 15f);
        thrustScore += Random.Range(0, 15f); runAttackScore += Random.Range(0, 15f);
        jumpScore += Random.Range(0, 15f); defendScore += Random.Range(0, 10f); dodgeScore += Random.Range(0, 10f);

        // FIX 3: Đưa chaseScore vào thuật toán so sánh lớn nhất
        float highest = Mathf.Max(attack1Score, attack2Score, thrustScore, runAttackScore, jumpScore, defendScore, dodgeScore, chaseScore);

        if (highest == defendScore && defendScore > 0) ExecuteAction(CombatState.Defending, hashDefend);
        else if (highest == dodgeScore && dodgeScore > 0) ExecuteAction(CombatState.Dodging, hashJump);
        else if (highest == jumpScore && jumpScore > 0) ExecuteAction(CombatState.Jumping, hashJump);
        else if (highest == thrustScore && thrustScore > 0) ExecuteAction(CombatState.ThrustAttack, hashAttack3);
        else if (highest == runAttackScore && runAttackScore > 0) ExecuteAction(CombatState.RunAttack, hashRunAttack);
        else if (highest == attack1Score && attack1Score > 0) ExecuteAction(CombatState.Attack1, hashAttack1);
        else if (highest == attack2Score && attack2Score > 0) ExecuteAction(CombatState.Attack2, hashAttack2);
        else ChangeMovementState(MovementState.Chase);

        decisionLockTimer = Time.time + decisionInertiaTime;
    }

    private void ExecuteAction(CombatState state, int animHash)
    {
        ChangeMovementState(MovementState.Idle);
        combatState = state; lastCombatState = state;
        failsafeTimer = 1.5f; // FIX 4: Rút ngắn thời gian chống kẹt xuống 1.5s để AI cơ động hơn

        if (anim != null)
        {
            anim.ResetTrigger(animHash);

            // FIX 5: Bắt buộc chạy hoạt ảnh Thủ và Nhảy để vượt qua mọi lỗi của Animator
            if (state == CombatState.Defending) anim.Play("EKnightDefend", -1, 0f);
            else if (state == CombatState.Jumping || state == CombatState.Dodging) anim.Play("EKnightJump", -1, 0f);
            else anim.SetTrigger(animHash);
        }

        // Tự động hóa các hành động đặc biệt
        if (state == CombatState.Defending)
        {
            if (activeActionCoroutine != null) StopCoroutine(activeActionCoroutine);
            activeActionCoroutine = StartCoroutine(DefendRoutine());
        }
        else if (state == CombatState.Jumping || state == CombatState.Dodging)
        {
            if (activeActionCoroutine != null) StopCoroutine(activeActionCoroutine);
            activeActionCoroutine = StartCoroutine(JumpRoutine(state));
        }
    }

    private void HandleCombatFailsafe()
    {
        failsafeTimer -= Time.deltaTime;
        if (failsafeTimer <= 0f) Event_EndAttack();
    }
    #endregion

    #region MOVEMENT & UTILS
    private void ChangeMovementState(MovementState newState)
    {
        moveState = newState;
        if (newState == MovementState.Idle)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (anim != null) anim.SetBool(hashIsRunning, false);
        }
        else if (newState == MovementState.Chase)
        {
            FacePlayer();
            if (anim != null) anim.SetBool(hashIsRunning, true);
            float dirX = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(dirX * moveSpeed, rb.linearVelocity.y);
        }
    }

    private void FacePlayer()
    {
        if (player == null) return;
        float dirX = player.position.x - transform.position.x;
        if (Mathf.Abs(dirX) > 0.1f) Flip(Mathf.Sign(dirX));
    }
    #endregion

    #region ANIMATION EVENTS & COROUTINES
    public void Event_MeleeHit() { DamageCheck(meleeBox); }
    public void Event_ThrustHit() { DamageCheck(thrustBox); }
    public void Event_RunAttackHit() { DamageCheck(runAttackBox); }

    public void Event_StartDash()
    {
        float dirX = Mathf.Sign(transform.localScale.x);
        rb.linearVelocity = new Vector2(dirX * dashSpeed, rb.linearVelocity.y);
    }
    public void Event_StopDash() { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); }

    // --- FIX 6: TỰ ĐỘNG HÓA HOÀN TOÀN NHẢY VÀ THỦ KHÔNG CẦN ANIMATION EVENT ---
    private IEnumerator JumpRoutine(CombatState state)
    {
        // Chờ 0.1s cho chân nhún xuống rồi mới nảy lên
        yield return new WaitForSeconds(0.1f);

        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        FacePlayer();

        if (state == CombatState.Dodging) rb.linearVelocity = new Vector2(-dirX * dodgeForceX, jumpForceY);
        else rb.linearVelocity = new Vector2(dirX * jumpForceX, jumpForceY);

        // Chờ hoạt ảnh nhảy xong (khoảng 0.8s) rồi tự kết thúc, không sợ bị đơ!
        yield return new WaitForSeconds(0.8f);
        if (combatState == state) Event_EndAttack();
    }

    private IEnumerator DefendRoutine()
    {
        yield return new WaitForSeconds(defendDuration);
        if (combatState == CombatState.Defending) Event_EndAttack();
    }

    private void DamageCheck(Vector2 hitBox)
    {
        Collider2D hit = Physics2D.OverlapBox(attackPoint.position, hitBox, 0f, LayerMask.GetMask("Player"));
        if (hit != null)
        {
            hit.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage, transform);
            CinemachineShake.Instance?.ShakeCamera(0.15f);
        }
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

    public void PacifyBoss()
    {
        isAwake = false; // Tắt bộ não AI
        InterruptAttacks(); // Hủy mọi đòn đang chém dở
        ChangeMovementState(MovementState.Idle); // Đứng im

        // Tắt va chạm gây sát thương (nếu có) để an toàn tuyệt đối
        rb.linearVelocity = Vector2.zero;
    }

    // MỚI: Đạo diễn gọi hàm này để biến Boss thành cửa
    public void TransformIntoDoor()
    {
        PacifyBoss(); // Đứng im
        isDoorMode = true; // Kích hoạt Update tìm phím S
    }
}
#endregion