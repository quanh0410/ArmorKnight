using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animator), typeof(Collider2D))]
public class SwitchController : MonoBehaviour
{
    // ĐÃ XÓA DÒNG NÀY: public static SwitchController Instance { get; private set; }

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
        // ĐÃ XÓA TOÀN BỘ LOGIC KIỂM TRA SINGLETON VÀ TỰ HỦY
        anim = GetComponent<Animator>();
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        if (isOn && anim != null) anim.SetTrigger("On");
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