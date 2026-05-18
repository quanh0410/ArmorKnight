using UnityEngine;

public class HorizontalBomb : MonoBehaviour
{
    [Header("Cài ??t")]
    public float speed = 15f;
    public int damage = 1;
    public float lifeTime = 3f;
    public float hitRadius = 0.5f;

    [Header("Layer va ch?m")]
    public LayerMask playerLayer;
    public LayerMask groundLayer;

    private float moveDirection;
    private float timer;
    private bool hasExploded = false;

    private Animator anim;
    private Rigidbody2D rb;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    // T??ng t? RangedSlash, g?i hàm này khi xu?t x??ng ??n
    public void Setup(float direction)
    {
        moveDirection = Mathf.Sign(direction);
        hasExploded = false;
        timer = 0f;

        transform.localScale = new Vector3(moveDirection * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    private void Update()
    {
        if (hasExploded) return;

        // Bay th?ng t?i tr??c
        transform.Translate(Vector2.right * moveDirection * speed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            Explode();
            return;
        }

        CheckCollision();
    }

    private void CheckCollision()
    {
        // 1. Ch?m Player
        Collider2D playerHit = Physics2D.OverlapCircle(transform.position, hitRadius, playerLayer);
        if (playerHit != null)
        {
            PlayerHealth hp = playerHit.GetComponent<PlayerHealth>();
            if (hp != null) hp.TakeDamage(damage, transform);
            Explode();
            return;
        }

        // 2. Ch?m ??t/T??ng
        Collider2D groundHit = Physics2D.OverlapCircle(transform.position, hitRadius, groundLayer);
        if (groundHit != null)
        {
            Explode();
        }
    }

    private void Explode()
    {
        hasExploded = true;
        if (anim != null) anim.SetTrigger("Explode"); // Ch?y Anim n? gi?ng EGoblinBomb
    }

    // G?n hàm này vào khung hình cu?i c?a Animation N? (Animation Event)
    public void DestroyBomb()
    {
        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        else
            Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}