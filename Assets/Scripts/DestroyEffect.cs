using UnityEngine;

public class DestroyEffect : MonoBehaviour
{
    // Hàm này vẫn được gọi bởi Animation Event ở cuối Frame của hiệu ứng
    public void DestroySelf()
    {
        // Kiểm tra xem có PoolManager không, nếu có thì trả về, không thì tự hủy (phòng hờ)
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}