using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(EnemyHealth))] //
public abstract class EnemyBase : MonoBehaviour //
{
    [Header("Base Settings")]
    public float moveSpeed; //
    protected Rigidbody2D rb; //
    protected Animator anim; //
    protected EnemyHealth health; //
    protected Transform player; //
    protected float initialScaleX; //

    // --- THÊM SETTINGS CHO COIN LỘT XÁC ---
    [Header("Loot Settings")]
    public GameObject coinPrefab;     // Kéo Prefab đồng xu vào đây
    public int minCoins = 1;          // Số xu rớt tối thiểu
    public int maxCoins = 3;          // Số xu rớt tối đa
    public float burstForceX = 3f;    // Lực văng sang 2 bên
    public float burstForceY = 6f;    // Lực nảy bốc lên trời

    private bool hasDroppedLoot = false; // Khóa an toàn tránh rớt tiền liên tục

    protected virtual void Awake() //
    {
        rb = GetComponent<Rigidbody2D>(); //
        anim = GetComponent<Animator>(); //
        health = GetComponent<EnemyHealth>(); //

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player"); //
        if (playerObj != null) player = playerObj.transform; //

        initialScaleX = Mathf.Abs(transform.localScale.x); //
    }

    protected virtual void Update() //
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;

            // Nếu vẫn chưa thấy (do player đang tàng hình lúc chuyển map) thì bỏ qua không làm gì cả
            if (player == null) return;
        }
        if (health != null && health.isDead) //
        {
            // --- KÍCH HOẠT RỚT TIỀN 1 LẦN DUY NHẤT ---
            if (!hasDroppedLoot)
            {
                SpawnCoins();
                hasDroppedLoot = true;
            }

            StopMovement(); //
            return; //
        }

        if (health != null && health.isKnockedBack) return; //

        // ==========================================
        // DÒNG MỚI ĐƯỢC CẬP NHẬT Ở ĐÂY
        // ==========================================
        // Nếu quái đang bị đẩy lùi HOẶC bị choáng, dừng ngay mọi suy nghĩ (AI) lại!
        if (health != null && health.isStunned)
        {
            StopMovement(); // Ép nó dừng lại (bổ sung thêm để triệt tiêu đà trượt)
            return;
        }

        ExecuteAI(); //
    }

    protected abstract void ExecuteAI(); //

    protected void StopMovement() //
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); //
    }

    protected void Flip(float direction) //
    {
        float newX = (direction > 0) ? initialScaleX : -initialScaleX; //
        transform.localScale = new Vector3(newX, transform.localScale.y, transform.localScale.z); //
    }

    // --- HÀM TẠO HIỆU ỨNG BUNG TIỀN ---
    // --- HÀM TẠO HIỆU ỨNG BUNG TIỀN (ĐÃ NÂNG CẤP X2) ---
    protected virtual void SpawnCoins()
    {
        // Nếu không có prefab đồng xu thì bỏ qua (dành cho quái không rớt tiền)
        if (coinPrefab == null) return;

        // 1. Tính toán số tiền cơ bản ban đầu
        int baseCoinCount = Random.Range(minCoins, maxCoins + 1);
        int multiplier = 1;

        // 2. KIỂM TRA ITEM: Xem người chơi có đang trang bị đồ x2 Vàng không?
        if (EquipmentManager.instance != null && EquipmentManager.instance.HasMechanic("DoubleGold"))
        {
            multiplier = 2; // Kích hoạt nhân đôi!
            // Bạn có thể đổi số 2 thành bất kỳ hệ số nào (ví dụ 3, 4) nếu muốn x3, x4
        }

        // 3. Tính tổng số xu cuối cùng sẽ rớt ra
        int finalCoinCount = baseCoinCount * multiplier;

        for (int i = 0; i < finalCoinCount; i++)
        {
            // Sinh ra đồng xu ngay tại bụng con quái
            GameObject coin = ObjectPoolManager.Instance.Spawn(coinPrefab, transform.position, Quaternion.identity);

            // Lấy Rigidbody2D của đồng xu để tác dụng lực
            Rigidbody2D coinRb = coin.GetComponent<Rigidbody2D>();
            if (coinRb != null)
            {
                // Tạo lực đẩy random: X văng sang trái/phải, Y luôn bốc lên trên
                float randomX = Random.Range(-burstForceX, burstForceX);
                float randomY = Random.Range(burstForceY * 0.5f, burstForceY);

                // Gắn thẳng lực văng vào đồng xu
                coinRb.linearVelocity = new Vector2(randomX, randomY);
            }
        }
    }
}