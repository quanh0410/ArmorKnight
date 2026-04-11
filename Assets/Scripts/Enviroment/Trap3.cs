using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D), typeof(Animator))]
public class Trap3 : MonoBehaviour
{
    [Header("Trap Settings")]
    public int damage = 1; // Sát thương chuẩn (thường bẫy Hollow Knight trừ 1 máu)
    public float loopDelay = 2f; // Thời gian lặp lại cú đâm/chém

    private SpriteRenderer spriteRenderer;
    private PolygonCollider2D polyCollider;
    private Animator animator;
    private Sprite lastSprite;

    // --- TỐI ƯU HIỆU SUẤT: Dùng Hash ID thay cho String ---
    private static readonly int ActTrigger = Animator.StringToHash("act");

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        polyCollider = GetComponent<PolygonCollider2D>();
        animator = GetComponent<Animator>();

        // Ép buộc trở thành Trigger để không cản đường bay/vật lý của Player
        polyCollider.isTrigger = true;

        // Bắt đầu vòng lặp Animation
        InvokeRepeating(nameof(PlayAnimation), 0f, loopDelay);
        UpdateCollider();
    }

    void Update()
    {
        // Liên tục bóp méo Hitbox bám sát theo từng frame ảnh thay đổi
        if (spriteRenderer.sprite != lastSprite)
        {
            UpdateCollider();
        }
    }

    void UpdateCollider()
    {
        lastSprite = spriteRenderer.sprite;
        polyCollider.pathCount = spriteRenderer.sprite.GetPhysicsShapeCount();
        List<Vector2> path = new List<Vector2>();

        for (int i = 0; i < polyCollider.pathCount; i++)
        {
            path.Clear();
            spriteRenderer.sprite.GetPhysicsShape(i, path);
            polyCollider.SetPath(i, path);
        }
    }

    void PlayAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(ActTrigger);
        }
    }

    // --- CƠ CHẾ GÂY SÁT THƯƠNG ĐÃ ĐỒNG BỘ VỚI PLAYER ---
    private void OnTriggerStay2D(Collider2D collision)
    {
        // Nhớ gắn Tag "Player" cho nhân vật của bạn nhé
        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Gọi thẳng vào hệ thống dịch chuyển an toàn của Player
                playerHealth.TakeTrapDamage(damage);
            }
        }
    }
}