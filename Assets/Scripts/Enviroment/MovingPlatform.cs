using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private Transform pointC;
    [SerializeField] private float speed = 2f;
    private Vector3 targetPosition;

    // --- ĐÃ NÂNG CẤP: Dùng trực tiếp PlayerController thay vì Transform ---
    private PlayerController playerController;

    private bool isSwitchActivated = false;
    private bool reachedC = false;
    private bool isStoppedAtC = false;

    private void Start()
    {
        // Bắt đầu di chuyển đến điểm A
        targetPosition = pointA.position;
    }

    private void Update()
    {
        if (isStoppedAtC)
        {
            // Reset vận tốc nếu bệ đỡ dừng lại
            if (playerController != null) playerController.platformVelocity = Vector2.zero;
            return;
        }

        // Khi switch được kích hoạt, chuyển hướng đến C (một lần duy nhất)
        if (isSwitchActivated && !reachedC)
        {
            targetPosition = pointC.position;
        }

        // 1. Tính toán vị trí mới
        Vector3 newPos = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // 2. Tính ra vận tốc của bệ đỡ trong Frame này
        Vector2 currentVelocity = (newPos - transform.position) / Time.deltaTime;

        // 3. Di chuyển bệ đỡ
        transform.position = newPos;

        // 4. TRUYỀN VẬN TỐC SANG CHO NHÂN VẬT
        if (playerController != null)
        {
            playerController.platformVelocity = currentVelocity;
        }

        // Đảo chiều di chuyển hoặc dừng lại khi tới đích
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            if (isSwitchActivated)
            {
                if (!reachedC && targetPosition == pointC.position)
                {
                    reachedC = true;
                    isStoppedAtC = true;
                    if (playerController != null) playerController.platformVelocity = Vector2.zero;
                    return;
                }
            }
            else
            {
                // Di chuyển qua lại giữa A và B
                targetPosition = (targetPosition == pointA.position) ? pointB.position : pointA.position;
            }
        }
    }

    // ==========================================
    // NHẬN DIỆN NGƯỜI CHƠI
    // ==========================================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Kết nối với Script thay vì dùng SetParent
            playerController = collision.gameObject.GetComponent<PlayerController>();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (playerController != null)
            {
                // XÓA sạch vận tốc của bệ đỡ trên người Player khi họ nhảy ra ngoài
                playerController.platformVelocity = Vector2.zero;
                playerController = null;
            }
        }
    }

    public void ActivateSwitch()
    {
        isSwitchActivated = true;
        reachedC = false;
        isStoppedAtC = false;
    }

    public void DeactivateSwitch()
    {
        isSwitchActivated = false;
        reachedC = false;
        isStoppedAtC = false;

        // Tiếp tục di chuyển về phía A hoặc B tùy vị trí hiện tại
        targetPosition = (Vector3.Distance(transform.position, pointA.position) < Vector3.Distance(transform.position, pointB.position))
            ? pointB.position : pointA.position;
    }
}