using UnityEngine;

public class EnemyFlying : EnemyBase
{
    private enum FlyState { Idle, Chase, Telegraph, Dive, Recover }
    private FlyState currentState;

    [Header("Detection & Vision")]
    public float detectionRange = 8f;
    public float loseInterestRange = 12f;
    public LayerMask obstacleLayer; // MỚI: Tường/Đất cản tầm nhìn

    // --- MỚI: THÔNG SỐ ĐÀ BAY ---
    [Header("Flight Dynamics")]
    public float acceleration = 10f; // Độ bốc khi lượn tới
    public float deceleration = 6f;  // Quán tính trượt khi phanh lơ lửng
    public float hoverHeight = 3.5f;
    public float positionTolerance = 0.5f;

    [Header("Dive Attack Settings")]
    public float diveSpeed = 15f;
    public float telegraphDuration = 0.6f;
    private float stateTimer;

    [Header("Collision Settings")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;

    private PlayerHealth targetHealth;

    protected override void Awake()
    {
        base.Awake(); 

        rb.gravityScale = 0f; 

        if (player != null)
        {
            targetHealth = player.GetComponent<PlayerHealth>(); 
        }

        currentState = FlyState.Idle; 
    }

    protected override void Update()
    {
        if (health != null && health.isKnockedBack) 
        {
            if (currentState == FlyState.Telegraph || currentState == FlyState.Dive || currentState == FlyState.Recover) 
            {
                currentState = FlyState.Chase; 
            }
        }
        base.Update(); 
    }

    protected override void ExecuteAI()
    {
        float distanceToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : 999f; 

        if (targetHealth != null && targetHealth.currentHealth <= 0) 
        {
            distanceToPlayer = 999f; 
            if (currentState != FlyState.Idle) currentState = FlyState.Idle; 
        }

        // --- MỚI: KIỂM TRA TẦM NHÌN ---
        bool canSeePlayer = false;
        if (distanceToPlayer <= detectionRange)
        {
            RaycastHit2D hit = Physics2D.Linecast(transform.position, player.position, obstacleLayer);
            canSeePlayer = (hit.collider == null);
        }

        if (currentState == FlyState.Idle || currentState == FlyState.Chase)
        {
            if (distanceToPlayer <= detectionRange && canSeePlayer)
            {
                currentState = FlyState.Chase;
            }
            else if (distanceToPlayer > loseInterestRange || !canSeePlayer)
            {
                currentState = FlyState.Idle;
            }
        }

        switch (currentState) 
        {
            case FlyState.Idle: HandleIdle(); break;
            case FlyState.Chase: HandleChase(); break;
            case FlyState.Telegraph: HandleTelegraph(); break; 
            case FlyState.Dive: HandleDive(); break; 
            case FlyState.Recover: HandleRecover(); break; 
        }
    }

    private void HandleIdle()
    {
        anim.SetBool("isFlying", false); 

        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, deceleration * Time.deltaTime);
    }

    private void HandleChase()
    {
        anim.SetBool("isFlying", true); 

        Vector2 targetPos = new Vector2(player.position.x, player.position.y + hoverHeight); 
        Vector2 direction = (targetPos - (Vector2)transform.position).normalized; 

        // --- NÂNG CẤP: GIA TỐC BAY LƯỢN ---
        Vector2 targetVelocity = direction * moveSpeed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, acceleration * Time.deltaTime);

        // Tránh giật lật mặt (Flip) liên tục khi đang lượn chính xác trên đỉnh đầu
        if (Mathf.Abs(player.position.x - transform.position.x) > 0.1f)
        {
            Flip(player.position.x - transform.position.x);
        }

        if (Vector2.Distance(transform.position, targetPos) <= positionTolerance) 
        {
            StartTelegraph(); 
        }
    }

    // --- CÁC TRẠNG THÁI TẤN CÔNG GIỮ NGUYÊN VIỆC "KHỰNG LẠI" (Tạo cảm giác lực) ---
    private void StartTelegraph()
    {
        currentState = FlyState.Telegraph; 
        rb.linearVelocity = Vector2.zero;  // Đang lượn mượt mà đột ngột khựng lại để gồng -> Rất ngầu! //
        anim.SetTrigger("AttackStart"); 
        stateTimer = telegraphDuration; 
    }

    private void HandleTelegraph()
    {
        stateTimer -= Time.deltaTime; 
        if (stateTimer <= 0) StartDive(); 
    }

    private void StartDive()
    {
        currentState = FlyState.Dive; 
        anim.SetTrigger("Attack"); 
        rb.linearVelocity = Vector2.down * diveSpeed; 
    }

    private void HandleDive()
    {
        bool isHittingGround = false; 
        if (groundCheck != null) 
        {
            isHittingGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer); 
        }

        if (isHittingGround) StartRecover(); 
    }

    private void StartRecover()
    {
        currentState = FlyState.Recover; 
        rb.linearVelocity = Vector2.zero; // Găm xuống đất đứng im 
        anim.SetTrigger("AttackEnd"); 
        CinemachineShake.Instance.ShakeCamera(0.1f); 
    }

    private void HandleRecover()
    {
        rb.linearVelocity = Vector2.zero; 
    }

    public void FinishRecovery()
    {
        if (currentState == FlyState.Recover && !health.isDead) 
        {
            currentState = FlyState.Chase; 
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; 
        Gizmos.DrawWireSphere(transform.position, detectionRange); 

        Gizmos.color = Color.blue; 
        Gizmos.DrawWireSphere(transform.position, loseInterestRange); 

        if (groundCheck != null) 
        {
            Gizmos.color = Color.green; 
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius); 
        }

        // Vẽ tia Linecast
        if (player != null)
        {
            bool isBlocked = Physics2D.Linecast(transform.position, player.position, obstacleLayer);
            Gizmos.color = isBlocked ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}