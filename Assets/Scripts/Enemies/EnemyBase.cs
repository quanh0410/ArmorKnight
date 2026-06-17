using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(EnemyHealth))] 
public abstract class EnemyBase : MonoBehaviour 
{
    [Header("Base Settings")]
    public float moveSpeed; 
    protected Rigidbody2D rb; 
    protected Animator anim;
    protected EnemyHealth health; 
    protected Transform player; 
    protected float initialScaleX; 

    [Header("Loot Settings")]
    public GameObject coinPrefab;     
    public int minCoins = 1;          
    public int maxCoins = 3;         
    public float burstForceX = 3f;    
    public float burstForceY = 6f;   

    private bool hasDroppedLoot = false; 

    protected virtual void Awake() 
    {
        rb = GetComponent<Rigidbody2D>(); 
        anim = GetComponent<Animator>(); 
        health = GetComponent<EnemyHealth>(); 

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player"); 
        if (playerObj != null) player = playerObj.transform; 

        initialScaleX = Mathf.Abs(transform.localScale.x); 
    }

    protected virtual void Update() 
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;

            if (player == null) return;
        }
        if (health != null && health.isDead) 
        {
            if (!hasDroppedLoot)
            {
                SpawnCoins();
                hasDroppedLoot = true;
            }

            StopMovement(); 
            return; 
        }

        if (health != null && health.isKnockedBack) return; 


        if (health != null && health.isStunned)
        {
            StopMovement(); 
            return;
        }

        ExecuteAI(); 
    }

    protected abstract void ExecuteAI(); 

    protected void StopMovement() 
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
    }

    protected void Flip(float direction) 
    {
        float newX = (direction > 0) ? initialScaleX : -initialScaleX; 
        transform.localScale = new Vector3(newX, transform.localScale.y, transform.localScale.z); 
    }

    protected virtual void SpawnCoins()
    {
        if (coinPrefab == null) return;

        int baseCoinCount = Random.Range(minCoins, maxCoins + 1);
        int multiplier = 1;

        if (EquipmentManager.instance != null && EquipmentManager.instance.HasMechanic("DoubleGold"))
        {
            multiplier = 2; 
        }

        int finalCoinCount = baseCoinCount * multiplier;

        for (int i = 0; i < finalCoinCount; i++)
        {
            GameObject coin = ObjectPoolManager.Instance.Spawn(coinPrefab, transform.position, Quaternion.identity);

            Rigidbody2D coinRb = coin.GetComponent<Rigidbody2D>();
            if (coinRb != null)
            {
                float randomX = Random.Range(-burstForceX, burstForceX);
                float randomY = Random.Range(burstForceY * 0.5f, burstForceY);

                coinRb.linearVelocity = new Vector2(randomX, randomY);
            }
        }
    }
}