using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyDamage : MonoBehaviour
{
    [Header("Cài đặt Sát thương")]
    public int damageAmount = 1;

    [Tooltip("Kéo transform của quái vật vào đây để Player biết bị ai đẩy lùi. Để trống sẽ tự lấy vị trí của Hitbox.")]
    public Transform ownerTransform;

    private void Start()
    {
        // Đảm bảo Collider này luôn là Trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        if (ownerTransform == null) ownerTransform = transform;
    }

    // Dùng OnTriggerStay2D để nếu Player đứng lỳ trong vùng sát thương thì vẫn bị mất máu liên tục (sau khi hết I-Frame)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth pHealth = collision.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                pHealth.TakeDamage(damageAmount, ownerTransform);
            }
        }
    }
}