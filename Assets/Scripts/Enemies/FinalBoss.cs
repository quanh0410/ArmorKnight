using UnityEngine;
using System.Collections;

public class FinalBoss : EnemyBase
{
    public enum BossState { Idle, Chase, Melee, Casting }

    [Header("--- TRẠNG THÁI KÍCH HOẠT ---")]
    [Tooltip("Boss có đang tỉnh táo không? Đạo diễn sẽ bật cái này khi hết hội thoại.")]
    public bool isAwake = false; // --- MỚI: Quản lý việc ngủ/thức của Boss 1 ---

    [Header("--- THÔNG SỐ TRÙM CUỐI ---")]
    public GameObject spellPrefab;

    [Header("--- CƠ CHẾ GỌI BÙA (KHÔNG DÙNG ANIMATION) ---")]
    public int minSpells = 3;
    public int maxSpells = 5;
    public float spawnHeight = 5f;
    public float spawnSpreadX = 6f;
    public float castCooldown = 4f;

    [Header("--- THỜI GIAN GIẢ LẬP ANIMATION ---")]
    [Tooltip("Thời gian Boss đứng gồng trước khi bùa hiện ra")]
    public float castWindupTime = 1.0f;
    [Tooltip("Thời gian Boss đứng nghỉ sau khi gọi bùa")]
    public float castRecoveryTime = 0.5f;
    [Tooltip("Thời gian gồng trước khi sát thương cận chiến nổ")]
    public float meleeWindupTime = 0.3f;
    [Tooltip("Thời gian nghỉ sau khi nổ cận chiến")]
    public float meleeRecoveryTime = 0.5f;

    [Header("--- CẬN CHIẾN (PHÒNG THÂN) ---")]
    public float meleeRange = 2.5f;
    public int meleeDamage = 20;
    public Vector2 meleeBox = new Vector2(3f, 2f);
    public Transform attackPoint;

    private BossState currentState = BossState.Idle;
    private float cooldownTimer = 2f;

    protected override void ExecuteAI()
    {
        // --- SỬA Ở ĐÂY: Nếu chưa được gọi dậy (isAwake = false), Boss sẽ đóng băng hoàn toàn AI ---
        if (!isAwake || player == null) return;

        // Bỏ qua AI nếu đang bận giả lập chém hoặc gọi bùa
        if (currentState == BossState.Casting || currentState == BossState.Melee) return;

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;

            float distSqr = (player.position - transform.position).sqrMagnitude;
            if (distSqr > meleeRange * meleeRange)
            {
                ChangeState(BossState.Chase);
            }
            else
            {
                ChangeState(BossState.Idle);
                FacePlayer();
            }
            return;
        }

        // QUYẾT ĐỊNH HÀNH ĐỘNG
        float distanceToPlayer = Vector2.Distance(player.position, transform.position);

        if (distanceToPlayer <= meleeRange)
        {
            ExecuteMelee();
        }
        else
        {
            ExecuteCastSpell();
        }
    }

    #region HÀNH ĐỘNG & TRẠNG THÁI
    private void ChangeState(BossState newState)
    {
        currentState = newState;
        if (newState == BossState.Idle || newState == BossState.Casting || newState == BossState.Melee)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else if (newState == BossState.Chase)
        {
            FacePlayer();
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

    #region COROUTINE GIẢ LẬP ĐÒN ĐÁNH
    private void ExecuteMelee()
    {
        ChangeState(BossState.Melee);
        FacePlayer();
        StartCoroutine(MeleeRoutine());
    }

    private IEnumerator MeleeRoutine()
    {
        yield return new WaitForSeconds(meleeWindupTime);

        if (attackPoint != null)
        {
            Collider2D hit = Physics2D.OverlapBox(attackPoint.position, meleeBox, 0f, LayerMask.GetMask("Player"));
            if (hit != null)
            {
                hit.GetComponent<PlayerHealth>()?.TakeDamage(meleeDamage, transform);
                CinemachineShake.Instance?.ShakeCamera(0.2f);
            }
        }

        yield return new WaitForSeconds(meleeRecoveryTime);

        ChangeState(BossState.Idle);
        cooldownTimer = castCooldown;
    }

    private void ExecuteCastSpell()
    {
        ChangeState(BossState.Casting);
        FacePlayer();
        StartCoroutine(CastSpellRoutine());
    }

    private IEnumerator CastSpellRoutine()
    {
        yield return new WaitForSeconds(castWindupTime);

        SpawnSpellsLogic();

        yield return new WaitForSeconds(castRecoveryTime);

        ChangeState(BossState.Idle);
        cooldownTimer = castCooldown;
    }

    private void SpawnSpellsLogic()
    {
        if (player == null || spellPrefab == null) return;

        int phaseBonus = 0;
        if (health != null && health.currentHealth <= health.maxHealth / 2f)
        {
            phaseBonus = 3;
        }

        int spellCount = Random.Range(minSpells, maxSpells + 1) + phaseBonus;

        for (int i = 0; i < spellCount; i++)
        {
            float randomX = player.position.x + Random.Range(-spawnSpreadX, spawnSpreadX);
            float targetY = player.position.y + spawnHeight + Random.Range(-1f, 1f);
            Vector3 spawnPos = new Vector3(randomX, targetY, 0f);

            GameObject spell = Instantiate(spellPrefab, spawnPos, Quaternion.identity);
            VerticalBomb bombScript = spell.GetComponent<VerticalBomb>();
            if (bombScript != null)
            {
                bombScript.Setup();
            }
        }

        CinemachineShake.Instance?.ShakeCamera(0.1f);
    }
    #endregion

    // =======================================================================
    // 🌟 MỚI: HÀM ĐƯỢC GỌI TỪ BOSSARENAMANAGER ĐỂ ĐÁNH THỨC BOSS
    // =======================================================================
    public void WakeUpBoss()
    {
        isAwake = true;
        Debug.Log($"<color=orange>{gameObject.name} đã thức tỉnh và sẵn sàng chiến đấu!</color>");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        if (attackPoint != null)
        {
            Gizmos.DrawWireCube(attackPoint.position, meleeBox);
        }

        Gizmos.color = Color.magenta;
        Vector3 topCenter = transform.position + Vector3.up * spawnHeight;
        Gizmos.DrawLine(topCenter - Vector3.right * spawnSpreadX, topCenter + Vector3.right * spawnSpreadX);
    }
}