using UnityEngine;
using System.Collections;

public class DungeonBossSpell : MonoBehaviour
{
    [Header("--- THIẾT LẬP KỸ NĂNG ---")]
    public float delayBeforeAction = 1f;
    public GameObject batEnemyPrefab;
    
    [Header("--- GIỚI HẠN TRIỆU HỒI ---")]
    public int maxBats = 3; // Số lượng dơi tối đa trên màn hình
    public string batTag = "Bat"; // Tên Tag để nhận diện con dơi

    [Header("--- VÙNG SÁT THƯƠNG ---")]
    public GameObject damagePoint; 

    private Animator anim;
    private PlayerController player;

    void Start()
    {
        anim = GetComponent<Animator>();

        if (damagePoint != null)
        {
            damagePoint.SetActive(false);
        }

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
            if (Random.Range(0, 100) < 30) SummonBat();
            else SummonHand();
        }
        else
        {
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
        // BƯỚC 1: Tìm tất cả các con dơi đang bay trong cảnh (Scene)
        GameObject[] existingBats = GameObject.FindGameObjectsWithTag(batTag);

        // BƯỚC 2: Kiểm tra giới hạn
        if (existingBats.Length < maxBats)
        {
            // Nếu chưa đủ 3 con, tiếp tục gọi dơi
            if (batEnemyPrefab != null) Instantiate(batEnemyPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject, 0.5f);
        }
        else
        {
            // Nếu đã có từ 3 con trở lên, chuyển hướng sang tấn công bằng Bàn tay
            SummonHand();
        }
    }

    private IEnumerator CleanupAfterSlam()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }

    // ==========================================
    // SỰ KIỆN ANIMATION (ANIMATION EVENTS)
    // ==========================================

    public void EnableDamagePoint()
    {
        if (damagePoint != null) damagePoint.SetActive(true);
    }

    public void DisableDamagePoint()
    {
        if (damagePoint != null) damagePoint.SetActive(false);
    }
}