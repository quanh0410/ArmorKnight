using UnityEngine;

public class EnemyGoblin : EnemyBase
{
    private enum GoblinState { Idle, Patrol, Chase, Attack }
    private GoblinState currentState;
    private PlayerHealth targetHealth;

    [Header("Patrol Settings")]
    public Transform pointA;
    public Transform pointB;
    private Transform currentPatrolTarget;
    public float idleTimeAtPoint = 1.5f;
    private float idleTimer;

    [Header("Chase & Vision Settings")]
    public float chaseSpeed = 3f;
    public float detectionRange = 5f;
    public float loseInterestRange = 7f;
    public LayerMask obstacleLayer; // MỚI: Layer của tường/đất để cản tầm nhìn

    // --- MỚI: THÔNG SỐ ĐÀ DI CHUYỂN ---
    [Header("Movement Dynamics")]
    public float acceleration = 15f; // Gia tốc tăng tốc (Độ bốc)
    public float deceleration = 10f; // Quán tính trượt khi phanh

    [Header("Attack Settings")]
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    private float nextAttackTime;

    [Header("Stab Effect (Đâm tới)")]
    public float stabDashForce = 5f;
    public int attackDamage = 1;
    public Transform attackPoint;
    public Vector2 attackBoxSize = new Vector2(1f, 0.5f);
    public LayerMask playerLayer;
    public GameObject stabEffectPrefab;

    protected override void Awake()
    {
        base.Awake(); //

        if (player != null)
        {
            targetHealth = player.GetComponent<PlayerHealth>(); //
        }

        if (pointA != null) pointA.parent = null;
        if (pointB != null) pointB.parent = null;

        currentPatrolTarget = pointB;
        currentState = GoblinState.Patrol;
    }

    protected override void Update()
    {
        if (health != null && health.isKnockedBack) //
        {
            if (currentState == GoblinState.Attack)
            {
                currentState = GoblinState.Chase;
                nextAttackTime = Time.time + attackCooldown;
            }
        }

        base.Update();
    }

    protected override void ExecuteAI() //
    {
        float distanceToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : 999f;

        if (targetHealth != null && targetHealth.currentHealth <= 0) //
        {
            distanceToPlayer = 999f;
            if (currentState == GoblinState.Attack || currentState == GoblinState.Chase)
            {
                currentState = GoblinState.Patrol;
            }
        }

        // --- MỚI: KIỂM TRA TẦM NHÌN (LINECAST) ---
        bool canSeePlayer = false;
        if (distanceToPlayer <= detectionRange)
        {
            // Bắn tia từ bụng Goblin ra bụng Player. Nếu trúng chướng ngại vật -> Không thấy
            RaycastHit2D hit = Physics2D.Linecast(transform.position, player.position, obstacleLayer);
            canSeePlayer = (hit.collider == null);
        }

        // Cập nhật State Machine kết hợp với tầm nhìn
        if (currentState != GoblinState.Attack)
        {
            if (distanceToPlayer <= attackRange && canSeePlayer && Time.time >= nextAttackTime)
            {
                StartAttack();
            }
            else if (distanceToPlayer <= detectionRange && canSeePlayer)
            {
                currentState = GoblinState.Chase;
            }
            // Mất dấu do đi quá xa HOẶC bị núp sau tường
            else if ((distanceToPlayer > loseInterestRange || !canSeePlayer) && currentState == GoblinState.Chase)
            {
                currentState = GoblinState.Patrol;
            }
        }

        switch (currentState)
        {
            case GoblinState.Idle:
                HandleIdle();
                break;
            case GoblinState.Patrol:
                HandlePatrol();
                break;
            case GoblinState.Chase:
                HandleChase(distanceToPlayer);
                break;
            case GoblinState.Attack:
                SmoothStop(); // Khi vung kiếm đâm, sẽ có cảm giác trượt nhẹ tới trước rất "có lực"
                break;
        }
    }

    private void HandleIdle()
    {
        SmoothStop(); // MỚI: Phanh có quán tính thay vì khựng lại ngay lập tức
        anim.SetBool("isWalking", false);

        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0)
        {
            currentPatrolTarget = (currentPatrolTarget == pointA) ? pointB : pointA;
            currentState = GoblinState.Patrol;
        }
    }

    private void HandlePatrol()
    {
        anim.SetBool("isWalking", true);

        if (currentPatrolTarget == null) return;

        float distanceToTarget = Mathf.Abs(transform.position.x - currentPatrolTarget.position.x);
        if (distanceToTarget < 0.2f)
        {
            idleTimer = idleTimeAtPoint;
            currentState = GoblinState.Idle;
            return;
        }

        MoveTowards(currentPatrolTarget.position, moveSpeed); // Dùng moveSpeed của EnemyBase
    }

    private void HandleChase(float distanceToPlayer)
    {
        anim.SetBool("isWalking", true);

        if (distanceToPlayer > attackRange * 0.8f)
        {
            MoveTowards(player.position, chaseSpeed);
        }
        else
        {
            SmoothStop(); // MỚI: Trượt nhẹ lại khi áp sát mục tiêu
            anim.SetBool("isWalking", false);
        }
    }

    // --- MỚI: DI CHUYỂN CÓ ĐÀ CHỈ TRÊN TRỤC X ---
    private void MoveTowards(Vector2 targetPos, float targetSpeed)
    {
        float direction = targetPos.x - transform.position.x;

        // Tránh lật mặt liên tục khi đang đứng quá gần mục tiêu
        if (Mathf.Abs(direction) > 0.1f)
        {
            Flip(direction); //
        }

        // Tốc độ mong muốn đạt được
        float targetVelocityX = Mathf.Sign(direction) * targetSpeed;

        // Tăng tốc dần đều trục X, giữ nguyên trục Y cho trọng lực
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetVelocityX, acceleration * Time.deltaTime);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
    }

    // --- MỚI: PHANH CÓ QUÁN TÍNH CHỈ TRÊN TRỤC X ---
    private void SmoothStop()
    {
        // Kéo vận tốc trục X về 0 từ từ
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, deceleration * Time.deltaTime);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
    }

    // --- CÁC HÀM XỬ LÝ TẤN CÔNG (Giữ nguyên) ---
    private void StartAttack()
    {
        currentState = GoblinState.Attack;
        anim.SetBool("isWalking", false);
        anim.SetTrigger("Attack");
    }

    public void PerformStabAttack()
    {
        if (player == null || health.isDead) return;

        float faceDirection = Mathf.Sign(transform.localScale.x);
        rb.linearVelocity = new Vector2(faceDirection * stabDashForce, rb.linearVelocity.y);

        if (stabEffectPrefab != null && attackPoint != null)
        {
            float angleZ = -45f;
            float angleY = faceDirection > 0 ? 0f : 180f;
            Quaternion finalRotation = Quaternion.Euler(0, angleY, angleZ);
            Instantiate(stabEffectPrefab, attackPoint.position, finalRotation);
        }

        if (attackPoint != null)
        {
            Collider2D[] hitPlayers = Physics2D.OverlapBoxAll(attackPoint.position, attackBoxSize, 0f, playerLayer);
            foreach (Collider2D p in hitPlayers)
            {
                PlayerHealth pHealth = p.GetComponent<PlayerHealth>(); //
                if (pHealth != null) pHealth.TakeDamage(attackDamage, transform); //
            }
        }
    }

    public void FinishAttack()
    {
        nextAttackTime = Time.time + attackCooldown;
        currentState = GoblinState.Idle;
        idleTimer = 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (player != null)
        {
            // Vẽ đường tầm nhìn
            bool isBlocked = Physics2D.Linecast(transform.position, player.position, obstacleLayer);
            Gizmos.color = isBlocked ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(attackPoint.position, new Vector3(attackBoxSize.x, attackBoxSize.y, 1f));
        }
    }
}