using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Settings")]
    public float attackRange = 1.2f;
    public LayerMask enemyLayer;
    public LayerMask enviromentLayer;

    [Header("Hit Stop Settings")]
    public float hitStopDuration = 0.05f;

    [Header("Visual Effects Settings")]
    public GameObject hitEffectPrefab;
    public GameObject stunEffectPrefab;
    public Vector2 effectOffset = Vector2.zero;

    [Header("Ranged Slash Settings")]
    public GameObject rangedSlashPrefab;
    public Vector2 rangedSlashOffset = new Vector2(0.5f, 0f); // --- MỚI: Vị trí sinh ra so với nhân vật ---
    public float rangedSlashSpeed = 15f;                    // --- MỚI: Tốc độ bay có thể chỉnh ở đây ---
    public float rangedSlashLifetime = 1f;                  // --- MỚI: Thời gian tồn tại ---
    public int rangedSlashDamage = 20;

    [Header("Dive Kick Hitbox")]
    public Transform diveKickHitPoint; // Vị trí dưới chân Player
    public float diveKickHitRadius = 0.8f;
    public int diveKickDamage = 15;

    private bool isInvincible = false;

    [Header("References")]
    [SerializeField] private Transform attackPoint;

    private PlayerController playerController;
    private PlayerAnimator playerAnimator;
    private PlayerEnergy playerEnergy;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        playerAnimator = GetComponent<PlayerAnimator>();
        playerEnergy = GetComponent<PlayerEnergy>();
    }

    void Update()
    {
        if (playerController.isDiveKicking && diveKickHitPoint != null)
        {
            // Quét Enemy dưới chân
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(diveKickHitPoint.position, diveKickHitRadius, enemyLayer);
            bool hasHit = false;

            foreach (Collider2D enemy in hitEnemies)
            {
                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    health.TakeDamage(diveKickDamage, transform);
                    hasHit = true;

                    // Sinh Effect nếu có
                    if (hitEffectPrefab != null)
                        ObjectPoolManager.Instance.Spawn(hitEffectPrefab, diveKickHitPoint.position, Quaternion.identity);
                }
            }

            // Quét thêm Environment (ví dụ: Chém công tắc, đạp bom)
            Collider2D[] hitEnvs = Physics2D.OverlapCircleAll(diveKickHitPoint.position, diveKickHitRadius, enviromentLayer);
            foreach (Collider2D env in hitEnvs)
            {
                EGoblinBomb bomb = env.GetComponent<EGoblinBomb>();
                if (bomb != null) { bomb.Deflect(transform); hasHit = true; }

                VineInteraction vine = env.GetComponent<VineInteraction>();
                if (vine != null) { vine.TakeMeleeHit(); hasHit = true; }

                SwitchController sw = env.GetComponent<SwitchController>();
                if (sw != null) { sw.HitSwitch(); hasHit = true; }
            }

            if (hasHit)
            {
                // Gọi hàm nảy lên ở PlayerController
                playerController.ExecuteDiveKickBounce();
                StartCoroutine(HitStop());
            }
        }
    }

    public void Attack(int comboStep)
    {
        playerAnimator.PlayAttackAnimation(comboStep);

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        Collider2D[] hitEnviroments = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enviromentLayer);

        bool hasHit = false;
        bool hasGainedEnergy = false;

        // ==========================================
        // CÁCH SỬA CHUẨN: LẤY MECHANIC TỪ EQUIPMENT DATA
        // ==========================================
        bool isStunWeapon = false;
        if (EquipmentManager.instance != null && EquipmentManager.instance.currentWeapon != null)
        {
            string currentWeaponID = EquipmentManager.instance.currentWeapon.itemID;

            // Lục tìm món vũ khí đó trong túi đồ để đọc thông số Mechanic
            if (InventoryManager.instance != null)
            {
                foreach (ItemData item in InventoryManager.instance.items)
                {
                    // Nếu là Trang bị VÀ có ID trùng với vũ khí đang cầm
                    if (item is EquipmentData equip && equip.itemID == currentWeaponID)
                    {
                        if (equip.mechanicToUnlock == "Stun")
                        {
                            isStunWeapon = true;
                        }
                        break; // Tìm thấy rồi thì thoát vòng lặp cho nhẹ máy
                    }
                }
            }
        }

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();

            if (playerEnergy != null && !hasGainedEnergy)
            {
                playerEnergy.AddEnergy();
                hasGainedEnergy = true;
            }

            if (health != null)
            {
                CinemachineShake.Instance.ShakeCamera(0.05f);
                Vector2 spawnPos = new Vector2(enemy.transform.position.x + effectOffset.x, enemy.transform.position.y + effectOffset.y);

                if (isStunWeapon)
                {
                    // SIÊU NGẮN GỌN: Chỉ việc chuyền cái Mẫu Effect (Prefab) sang cho quái tự lo liệu!
                    health.TakeStun(1.5f, transform, stunEffectPrefab);
                }
                else
                {
                    // Vũ khí Kiếm -> Gây sát thương bình thường
                    health.TakeDamage(10, transform);

                    // Chạy Effect Chém
                    if (hitEffectPrefab != null)
                        ObjectPoolManager.Instance.Spawn(hitEffectPrefab, spawnPos, Quaternion.identity);
                }

                hasHit = true;
            }

            playerController.HandleAttackRecoil();
        }

        foreach (Collider2D env in hitEnviroments)
        {
            EGoblinBomb bomb = env.GetComponent<EGoblinBomb>();
            if (bomb != null)
            {
                bomb.Deflect(transform);
                CinemachineShake.Instance.ShakeCamera(0.05f);
                hasHit = true;
            }

            // --- THÊM MỚI: Tương tác với Dây Leo ---
            VineInteraction vine = env.GetComponent<VineInteraction>();
            if (vine != null)
            {
                vine.TakeMeleeHit(); // Gọi hàm Rung lắc
                hasHit = true;       // Kích hoạt khựng hình (Hit Stop) cho có cảm giác chém trúng vật thể
            }

            SwitchController sw = env.GetComponent<SwitchController>();
            if (sw != null)
            {
                sw.HitSwitch();
                hasHit = true; // Kích hoạt HitStop cho cảm giác chém trúng vật thể thực
            }
        }

        if (hasHit)
        {
            StartCoroutine(HitStop());
        }
    }

    private IEnumerator HitStop()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }

        // Vẽ Hitbox dưới chân
        if (diveKickHitPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(diveKickHitPoint.position, diveKickHitRadius);
        }
    }

    public void CastRangedSlash()
    {
        playerAnimator.PlayAttackAnimation(2);

        if (rangedSlashPrefab != null && attackPoint != null)
        {
            // --- TÍNH TOÁN VỊ TRÍ SINH RA DỰA TRÊN OFFSET VÀ HƯỚNG MẶT ---
            float facingDir = transform.localScale.x;
            Vector3 spawnPos = new Vector3(
                transform.position.x + (rangedSlashOffset.x * facingDir),
                transform.position.y + rangedSlashOffset.y,
                0f
            );

            GameObject slash = ObjectPoolManager.Instance.Spawn(rangedSlashPrefab, spawnPos, Quaternion.identity);

            RangedSlash projectile = slash.GetComponent<RangedSlash>();
            if (projectile != null)
            {
                // CHUYỀN THÊM: Tốc độ và Thời gian tồn tại vào hàm Setup
                projectile.Setup(facingDir, rangedSlashSpeed, rangedSlashDamage, rangedSlashLifetime);
            }
        }
    }

    public void SetInvincible(bool state)
    {
        isInvincible = state;
        // Báo cho PlayerHealth biết để không nhận sát thương (Yêu cầu bạn phải vào script PlayerHealth chặn sát thương nếu biến này true)
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.isInvincible = state; // Giả định PlayerHealth của bạn có biến này
        }
    }
}