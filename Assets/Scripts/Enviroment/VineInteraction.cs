using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class VineInteraction : MonoBehaviour
{
    [Header("Wiggle Settings (Rung lắc)")]
    public float wiggleDuration = 0.3f; // Thời gian rung
    public float wiggleAngle = 15f;     // Góc nghiêng tối đa
    public float wiggleSpeed = 25f;     // Tốc độ lắc

    [Header("Destroy Settings (Bị cắt)")]
    public GameObject cutEffectPrefab;  // Hiệu ứng lá rớt ra khi bị chém đứt

    private bool isWiggling = false;
    private Quaternion originalRotation;

    void Start()
    {
        originalRotation = transform.localRotation;
    }

    // 1. HÀM XỬ LÝ KHI BỊ ĐÁNH THƯỜNG (Rung lên)
    public void TakeMeleeHit()
    {
        if (!isWiggling)
        {
            StartCoroutine(WiggleRoutine());
        }
    }

    // 2. HÀM XỬ LÝ KHI BỊ KIẾM KHÍ CHÉM (Đứt luôn)
    public void TakeRangedHit()
    {
        // Sinh ra hiệu ứng lá rơi (nếu có)
        if (cutEffectPrefab != null)
        {
            ObjectPoolManager.Instance.Spawn(cutEffectPrefab, transform.position, Quaternion.identity);
        }

        // Tắt dây leo đi (Bạn có thể dùng Destroy, nhưng SetActive(false) an toàn hơn nếu muốn nó mọc lại khi chuyển map)
        Destroy(gameObject);
        //gameObject.SetActive(false);
    }

    // Tiến trình toán học để lắc dây leo qua lại mượt mà
    private IEnumerator WiggleRoutine()
    {
        isWiggling = true;
        float elapsed = 0f;

        while (elapsed < wiggleDuration)
        {
            elapsed += Time.deltaTime;

            // Dùng hàm Sin để tạo dao động giảm dần theo thời gian
            float dampener = 1f - (elapsed / wiggleDuration);
            float angle = Mathf.Sin(elapsed * wiggleSpeed) * wiggleAngle * dampener;

            transform.localRotation = originalRotation * Quaternion.Euler(0, 0, angle);
            yield return null;
        }

        transform.localRotation = originalRotation;
        isWiggling = false;
    }
}