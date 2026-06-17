using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    public int coinValue = 1;

    private Animator anim;
    private Rigidbody2D rb; 
    private bool isCollected = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>(); 
    }

    private void OnEnable()
    {
        isCollected = false;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; 
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag("Player"))
        {
            isCollected = true;
            AudioManager.instance.PlaySFX("CoinPickup");

            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.AddCoins(coinValue);
            }

            if (anim != null) anim.SetTrigger("PickUp");

            if (rb != null) rb.bodyType = RigidbodyType2D.Static;
        }
    }

 
    private void Collected()
    {
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