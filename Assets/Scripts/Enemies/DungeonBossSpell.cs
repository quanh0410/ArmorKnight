using UnityEngine;
using System.Collections;

public class DungeonBossSpell : MonoBehaviour
{
    public float delayBeforeAction = 1f;
    public GameObject batEnemyPrefab;
    private Animator anim;
    private PlayerController player;

    void Start()
    {
        anim = GetComponent<Animator>();
        // Tìm player thông qua Tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.GetComponent<PlayerController>();

        StartCoroutine(ExecuteSpellRoutine());
    }

    private IEnumerator ExecuteSpellRoutine()
    {
        yield return new WaitForSeconds(delayBeforeAction);

        bool isPlayerOnGround = (player != null) ? player.IsGrounded() : true;

        if (isPlayerOnGround)
        {
            // Trên mặt đất: 50% Dơi, 50% Bàn tay
            if (Random.Range(0, 100) < 30) SummonBat();
            else SummonHand();
        }
        else
        {
            // Player đang nhảy: CHỈ triệu hồi Dơi
            SummonBat();
        }
    }

    private void SummonHand()
    {
        anim.SetTrigger("Slam");
        StartCoroutine(CleanupAfterSlam());
    }

    private void SummonBat()
    {
        if (batEnemyPrefab != null) Instantiate(batEnemyPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject, 0.5f);
    }

    private IEnumerator CleanupAfterSlam()
    {
        yield return new WaitForSeconds(1.5f); // Chờ anim bàn tay chạy xong
        Destroy(gameObject);
    }
}