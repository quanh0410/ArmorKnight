using UnityEngine;
using System.Collections;

public class DungeonBoss : EnemyBase
{
    [Header("Giai đoạn (Phases)")]
    public float phase2Threshold = 0.3f; // 30% máu

    [Header("Tấn công thường")]
    public float meleeAttackRange = 1.5f;
    public float attackCooldown = 2f;
    public int meleeDamage = 15;
    public Transform attackPoint;
    public float attackRadius = 1f;
    public LayerMask playerLayer;

    [Header("Chiêu Hố Đen")]
    public GameObject blackHolePrefab;
    public float spellSpawnHeight = 3f;
    public float tripleSpellOffset = 3.5f; // Khoảng cách giữa 3 hố đen

    [Header("Dịch chuyển")]
    public float teleportOffset = 1.5f;
    public GameObject teleportEffectPrefab;

    private bool isAttacking = false;
    private float cooldownTimer = 0f;
    private PlayerController playerController;

    [Header("Trạng thái Boss")]
    public bool isAwake = false; // Mới vào phòng sẽ đứng im chờ Cutscene
    private Collider2D arenaBounds; // Lưu ranh giới phòng

    protected override void Awake()
    {
        base.Awake();
        if (player != null) playerController = player.GetComponent<PlayerController>();
    }

    protected override void ExecuteAI()
    {
        if (!isAwake) return;

        if (isAttacking || (health != null && health.isKnockedBack)) return;

        cooldownTimer -= Time.deltaTime;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (cooldownTimer <= 0f)
        {
            // Kiểm tra phần trăm máu hiện tại
            float healthPercent = (float)health.currentHealth / health.maxHealth;
            int randomChoice = Random.Range(0, 100);

            if (healthPercent > phase2Threshold)
            {
                // --- PHASE 1 (Máu > 30%) ---
                if (distanceToPlayer <= meleeAttackRange) StartCoroutine(MeleeAttackRoutine());
                else if (randomChoice < 50) ChasePlayer(); // 50% Truy đuổi
                else if (randomChoice < 85) StartCoroutine(TeleportSlashRoutine()); // 35% Dịch chuyển
                else StartCoroutine(CastSpellRoutine(false)); // 15% Hố đen
            }
            else
            {
                // --- PHASE 2 (Máu <= 30%) ---
                if (distanceToPlayer <= meleeAttackRange) StartCoroutine(MeleeAttackRoutine());
                else if (randomChoice < 30) ChasePlayer(); // 30% Truy đuổi
                else if (randomChoice < 60) StartCoroutine(TeleportSlashRoutine()); // 30% Dịch chuyển
                else StartCoroutine(CastSpellRoutine(true)); // 40% Hố đen (Triple)
            }
        }
        else if (distanceToPlayer > meleeAttackRange)
        {
            ChasePlayer();
        }
    }

    private void ChasePlayer()
    {
        anim.SetBool("isWalking", true);
        float direction = player.position.x > transform.position.x ? 1f : -1f;
        Flip(-direction);
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    private IEnumerator MeleeAttackRoutine()
    {
        isAttacking = true;
        StopMovement();
        anim.SetBool("isWalking", false);
        anim.SetTrigger("Attack");
        yield return new WaitForSeconds(1f);
        cooldownTimer = attackCooldown;
        isAttacking = false;
    }

    // ==========================================
    // KỸ NĂNG 3: DỊCH CHUYỂN ĐÂM LÉN (TELEPORT SLASH)
    // ==========================================
    private IEnumerator TeleportSlashRoutine()
    {
        isAttacking = true;
        StopMovement();
        anim.SetBool("isWalking", false);

        anim.SetTrigger("TeleOut");
        yield return new WaitForSeconds(0.5f);

        // Tính vị trí định nhảy tới
        float bossSideRelativeToPlayer = Mathf.Sign(transform.position.x - player.position.x);
        float targetX = player.position.x - (bossSideRelativeToPlayer * teleportOffset);

        // --- MỚI: ÉP TỌA ĐỘ KHÔNG ĐƯỢC VƯỢT QUÁ RANH GIỚI PHÒNG ---
        if (arenaBounds != null)
        {
            // Lấy tọa độ vách tường trái (min) và phải (max)
            float minWallX = arenaBounds.bounds.min.x;
            float maxWallX = arenaBounds.bounds.max.x;

            // Dùng Mathf.Clamp để nhốt targetX vào giữa 2 vách tường.
            // (Cộng/trừ thêm 1.5f để chừa chỗ cho bụng con Boss, tránh việc bị ghim sát quá vào tường)
            targetX = Mathf.Clamp(targetX, minWallX + 1.5f, maxWallX - 1.5f);
        }

        transform.position = new Vector2(targetX, transform.position.y);

        float directionToPlayer = player.position.x > transform.position.x ? 1f : -1f;
        Flip(-directionToPlayer);

        anim.SetTrigger("TeleIn");
        yield return new WaitForSeconds(0.5f);

        anim.SetTrigger("Attack");
        yield return new WaitForSeconds(1f);

        cooldownTimer = attackCooldown;
        isAttacking = false;
    }

    private IEnumerator CastSpellRoutine(bool isTriple)
    {
        isAttacking = true;
        StopMovement();
        anim.SetBool("isWalking", false);
        anim.SetTrigger("Cast");
        yield return new WaitForSeconds(0.5f);

        if (blackHolePrefab != null)
        {
            Vector2 centralPos = new Vector2(player.position.x, player.position.y + spellSpawnHeight);
            Instantiate(blackHolePrefab, centralPos, Quaternion.identity);

            if (isTriple)
            {
                // Triệu hồi thêm 2 hố đen phía trước và phía sau
                Instantiate(blackHolePrefab, centralPos + Vector2.left * tripleSpellOffset, Quaternion.identity);
                Instantiate(blackHolePrefab, centralPos + Vector2.right * tripleSpellOffset, Quaternion.identity);
            }
        }

        yield return new WaitForSeconds(0.5f);
        cooldownTimer = attackCooldown;
        isAttacking = false;
    }

    public void TriggerMeleeDamage()
    {
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);
        if (hitPlayer != null)
        {
            PlayerHealth pHealth = hitPlayer.GetComponent<PlayerHealth>();
            if (pHealth != null) pHealth.TakeDamage(meleeDamage, transform); // Gây 1 sát thương cho player
        }
    }

    // Hàm này sẽ được BossArenaManager gọi khi cửa đã đóng và rung màn hình xong
    public void WakeUpBoss()
    {
        isAwake = true;
    }

    // Hàm để BossArenaManager bơm dữ liệu vào
    public void SetArenaBounds(Collider2D bounds)
    {
        arenaBounds = bounds;
    }
}