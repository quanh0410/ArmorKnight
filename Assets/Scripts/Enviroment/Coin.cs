using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    public int coinValue = 1;

    private Animator anim;
    private bool isCollected = false;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag("Player"))
        {
            isCollected = true;

            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.AddCoins(coinValue);
            }

            if (anim != null) anim.SetTrigger("PickUp");

            // Neo đồng xu lại trên không trong lúc phát animation bay màu
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        }
    }

    private void Collected()
    {
        Destroy(gameObject);
    }
}