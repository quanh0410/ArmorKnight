using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private Transform pointC;
    [SerializeField] private float speed = 2f;
    private Vector3 targetPosition;

    private PlayerController playerController;

    private bool isSwitchActivated = false;
    private bool reachedC = false;
    private bool isStoppedAtC = false;

    private void Start()
    {
        targetPosition = pointA.position;
    }

    private void Update()
    {
        if (isStoppedAtC)
        {
            if (playerController != null) playerController.platformVelocity = Vector2.zero;
            return;
        }

        if (isSwitchActivated && !reachedC)
        {
            targetPosition = pointC.position;
        }

        // 1. Tính toán khoảng cách (delta) và vị trí mới
        Vector3 deltaPos = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime) - transform.position;
        Vector3 newPos = transform.position + deltaPos;

        // 2. Tính vận tốc (cho trục X)
        Vector2 currentVelocity = deltaPos / Time.deltaTime;

        // 3. Di chuyển Platform
        transform.position = newPos;

        // 4. TRUYỀN VẬN TỐC & KHẮC PHỤC LỖI RƠI XUỐNG
        if (playerController != null)
        {
            // Trục X: Vẫn truyền vận tốc để Player đi bộ mượt mà
            playerController.platformVelocity = currentVelocity;

            // Trục Y: NẾU PLATFORM ĐI XUỐNG, ép Player kéo xuống theo bằng đúng khoảng cách đó
            // Điều này xóa bỏ hoàn toàn khe hở gây ra do trọng lực chậm chạp
            if (deltaPos.y < 0)
            {
                playerController.transform.position += new Vector3(0, deltaPos.y, 0);
            }
        }

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
                targetPosition = (targetPosition == pointA.position) ? pointB.position : pointA.position;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerController = collision.gameObject.GetComponent<PlayerController>();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (playerController != null)
            {
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

        targetPosition = (Vector3.Distance(transform.position, pointA.position) < Vector3.Distance(transform.position, pointB.position))
            ? pointB.position : pointA.position;
    }
}