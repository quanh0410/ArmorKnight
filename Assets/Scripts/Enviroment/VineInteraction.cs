using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class VineInteraction : MonoBehaviour
{
    [Header("--- LƯU TRỮ ---")]
    public string vineID; // MỚI: ID lưu trạng thái bị đứt

    [Header("Wiggle Settings (Rung lắc)")]
    public float wiggleDuration = 0.3f;
    public float wiggleAngle = 15f;
    public float wiggleSpeed = 25f;

    [Header("Destroy Settings (Bị cắt)")]
    public GameObject cutEffectPrefab;

    private bool isWiggling = false;
    private Quaternion originalRotation;

    void Start()
    {
        // 1. KIỂM TRA LƯU TRỮ: Nếu dây leo đã bị chặt đứt từ trước thì ẩn luôn
        if (!string.IsNullOrEmpty(vineID) && SaveManager.instance != null)
        {
            if (SaveManager.instance.IsObjectInteracted(vineID))
            {
                gameObject.SetActive(false); // Dùng SetActive an toàn hơn Destroy lúc Start
                return;
            }
        }

        originalRotation = transform.localRotation;
    }

    public void TakeMeleeHit()
    {
        if (!isWiggling)
        {
            StartCoroutine(WiggleRoutine());
        }
    }

    public void TakeRangedHit()
    {
        if (cutEffectPrefab != null)
        {
            ObjectPoolManager.Instance.Spawn(cutEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. LƯU TRẠNG THÁI ĐÃ ĐỨT VÀO BỘ NHỚ
        if (!string.IsNullOrEmpty(vineID) && SaveManager.instance != null)
        {
            SaveManager.instance.SaveObjectState(vineID, true);
        }

        gameObject.SetActive(false);
    }

    private IEnumerator WiggleRoutine()
    {
        isWiggling = true;
        float elapsed = 0f;

        while (elapsed < wiggleDuration)
        {
            elapsed += Time.deltaTime;
            float dampener = 1f - (elapsed / wiggleDuration);
            float angle = Mathf.Sin(elapsed * wiggleSpeed) * wiggleAngle * dampener;
            transform.localRotation = originalRotation * Quaternion.Euler(0, 0, angle);
            yield return null;
        }

        transform.localRotation = originalRotation;
        isWiggling = false;
    }
}