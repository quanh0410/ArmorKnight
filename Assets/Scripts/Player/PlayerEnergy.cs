using UnityEngine;
using System.Collections;
using System; // Bắt buộc phải thêm thư viện này để dùng Action

public class PlayerEnergy : MonoBehaviour
{
    [Header("Energy Settings")]
    public int maxEnergy = 100;
    public int currentEnergy { get; private set; }

    // --- 1. TẠO SỰ KIỆN (Trạm phát thanh Năng lượng) ---
    public static event Action<int, int> OnEnergyChanged;

    [Header("Gain Settings")]
    public int energyPerHit = 11;

    [Header("Focus (Heal) Settings")]
    public int energyToHeal = 33;
    public float healChannelTime = 1f;
    public bool isHealing { get; private set; }

    [Header("Heal VFX")]
    public GameObject healEffectPrefab;
    private GameObject currentHealEffect;

    private PlayerHealth playerHealth;
    private PlayerController playerController;
    private PlayerAnimator playerAnimator;
    private Rigidbody2D rb;
    private float defaultGravity;

    void Start()
    {
        currentEnergy = 0;
        playerHealth = GetComponent<PlayerHealth>();
        playerController = GetComponent<PlayerController>();
        playerAnimator = GetComponent<PlayerAnimator>();
        rb = GetComponent<Rigidbody2D>();

        defaultGravity = rb.gravityScale;

        // --- 2. PHÁT TÍN HIỆU LÚC BẮT ĐẦU GAME ---
        // (Để thanh năng lượng bắt đầu ở mức 0 chuẩn xác)
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L) && !isHealing)
        {
            StartCoroutine(HealRoutine());
        }
    }

    public void AddEnergy()
    {
        currentEnergy += energyPerHit;
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;

        Debug.Log("Hút Năng Lượng! Hiện tại: " + currentEnergy + "/" + maxEnergy);

        // --- 3. PHÁT TÍN HIỆU KHI CHÉM TRÚNG QUÁI ---
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    private IEnumerator HealRoutine()
    {
        if (playerHealth.currentHealth >= playerHealth.maxHealth)
        {
            Debug.Log("Máu đã đầy, không cần hồi!");
            yield break;
        }
        if (currentEnergy < energyToHeal)
        {
            Debug.Log("Không đủ năng lượng!");
            yield break;
        }

        isHealing = true;
        playerController.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        if (playerAnimator != null) playerAnimator.SetHealingAnimation(true);

        if (healEffectPrefab != null)
        {
            currentHealEffect = ObjectPoolManager.Instance.Spawn(healEffectPrefab, transform.position, Quaternion.identity);
            StickyEffect2D sticky = currentHealEffect.GetComponent<StickyEffect2D>();
            if (sticky != null) sticky.SetTarget(transform);
            else currentHealEffect.transform.SetParent(transform, true);
        }

        float timer = 0f;
        while (timer < healChannelTime && isHealing)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (isHealing)
        {
            currentEnergy -= energyToHeal;

            // --- 4. PHÁT TÍN HIỆU KHI GỒNG MÁU THÀNH CÔNG (TRỪ NĂNG LƯỢNG) ---
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

            playerHealth.Heal(3);

            isHealing = false;
            playerController.enabled = true;
            rb.gravityScale = defaultGravity;
            Debug.Log("Hồi máu thành công!");

            if (playerAnimator != null) playerAnimator.SetHealingAnimation(false);
            if (currentHealEffect != null) Destroy(currentHealEffect);
        }
    }

    public void CancelHeal(bool canceledByDamage)
    {
        if (!isHealing) return;

        isHealing = false;
        rb.gravityScale = defaultGravity;

        if (playerAnimator != null) playerAnimator.SetHealingAnimation(false);
        if (currentHealEffect != null) Destroy(currentHealEffect);

        if (canceledByDamage)
        {
            currentEnergy = 0;
            Debug.Log("⚠️ Bị đánh gãy niệm chú! MẤT SẠCH TOÀN BỘ NĂNG LƯỢNG! ⚠️");

            // --- 5. PHÁT TÍN HIỆU KHI MẤT NĂNG LƯỢNG DO BỊ ĐÁNH ---
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }
    }
}