using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("--- THÔNG TIN CƠ BẢN ---")]
    public int spawnPointID;

    [Header("--- CƠ CHẾ ĐẨY (TÙY CHỌN) ---")]
    [Tooltip("Bật cờ này nếu bạn muốn nhân vật bị văng đi khi xuất hiện ở đây")]
    public bool isPushSpawn = false;

    [Tooltip("Lực văng: Trục X (Dương = Phải, Âm = Trái) | Trục Y (Độ cao)")]
    public Vector2 pushForce = new Vector2(10f, 15f);

    // Vẽ mũi tên chỉ hướng bay ngay trên Unity Editor (Không hiện trong game)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        if (isPushSpawn)
        {
            Gizmos.color = Color.cyan;
            Vector3 endPoint = transform.position + (Vector3)pushForce * 0.1f; // Nhân 0.1 để đường kẻ không quá dài
            Gizmos.DrawLine(transform.position, endPoint);
            Gizmos.DrawSphere(endPoint, 0.1f);
        }
    }
}