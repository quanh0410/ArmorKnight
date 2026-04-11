using UnityEngine;

public class Trap4 : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 2f;
    private Vector3 targetPosition;

    private void Start()
    {
        // --- THÊM 2 DÒNG NÀY ĐỂ TRÁNH LỖI ĐIỂM NEO BỊ TRÔI ---
        if (pointA != null) pointA.parent = null;
        if (pointB != null) pointB.parent = null;

        // Bắt đầu di chuyển đến điểm A
        targetPosition = pointA.position;
    }

    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            // Đổi hướng di chuyển khi đến điểm A hoặc B
            if (targetPosition == pointA.position)
            {
                targetPosition = pointB.position;
            }
            else
            {
                targetPosition = pointA.position;
            }
        }
    }
}