using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(Animator), typeof(Collider2D))]
public class SwitchController : MonoBehaviour
{
    [Header("--- LƯU TRỮ ---")]
    public string switchID; // MỚI: ID để lưu trạng thái vĩnh viễn

    [Header("--- TRẠNG THÁI ---")]
    public bool isOn = false;
    public bool isOneTimeUse = true;
    private bool hasBeenActivated = false;

    public float cooldownTime = 0.5f;
    private float lastHitTime = 0f;

    [Header("--- KÍCH HOẠT CƠ QUAN ---")]
    public UnityEvent onSwitchTurnedOn;

    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        GetComponent<Collider2D>().isTrigger = true;
    }

    private IEnumerator Start()
    {
        // Đợi 1 khung hình để đảm bảo Animator và các cơ quan (Cửa, bệ đỡ) đã khởi động xong hoàn toàn
        yield return null;

        // 1. KIỂM TRA LƯU TRỮ: Nếu đã bật từ trước, ép trạng thái thành ON
        if (!string.IsNullOrEmpty(switchID) && SaveManager.instance != null)
        {
            if (SaveManager.instance.IsObjectInteracted(switchID))
            {
                isOn = true;
                hasBeenActivated = true;
            }
        }

        // 2. KÍCH HOẠT LẠI NẾU ĐÃ LƯU LÀ ON
        if (isOn)
        {
            // Bật hình ảnh công tắc
            if (anim != null) anim.SetTrigger("On");

            // --- QUAN TRỌNG: Bắn sự kiện để mở luôn Cửa/Cơ quan đang nối với công tắc này ---
            onSwitchTurnedOn?.Invoke();
        }
    }

    public void HitSwitch()
    {
        if (isOneTimeUse && hasBeenActivated) return;
        if (Time.time - lastHitTime < cooldownTime) return;

        lastHitTime = Time.time;
        hasBeenActivated = true;
        isOn = true;

        if (anim != null) anim.SetTrigger("On");
        Debug.Log("Công tắc đã BẬT!");

        // 2. LƯU TRẠNG THÁI VÀO BỘ NHỚ
        if (!string.IsNullOrEmpty(switchID) && SaveManager.instance != null)
        {
            SaveManager.instance.SaveObjectState(switchID, true);
        }

        onSwitchTurnedOn?.Invoke();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerAttack"))
        {
            HitSwitch();
        }
    }
}