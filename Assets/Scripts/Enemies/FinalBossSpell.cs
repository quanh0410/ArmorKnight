using UnityEngine;
using System.Collections;

public class FinalBossSpell : MonoBehaviour
{
    [Header("--- CÀI ĐẶT TRIỆU HỒI ---")]
    [Tooltip("Thời gian vòng tròn ma thuật nhấp nháy cảnh báo trước khi cầu lửa rơi")]
    public float delayBeforeAction = 1f;

    [Tooltip("Kéo Prefab Cầu lửa (có gắn script VerticalBomb) vào đây")]
    public GameObject fireballPrefab;

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();

        // Bắt đầu đếm ngược thời gian cảnh báo ngay khi Boss vừa gọi bùa ra
        StartCoroutine(ExecuteSpellRoutine());
    }

    private IEnumerator ExecuteSpellRoutine()
    {
        // 1. Chờ thời gian cảnh báo (Hoạt ảnh vòng tròn ma thuật sẽ chạy trong lúc này)
        yield return new WaitForSeconds(delayBeforeAction);

        // 2. Triệu hồi Cầu lửa
        SummonFireball();

        // 3. Xử lý dọn dẹp sau khi gọi xong
        if (anim != null)
        {
            // Bật hoạt ảnh vòng tròn mờ dần/nổ tung (nếu bạn có làm clip tắt cho nó)
            anim.SetTrigger("End");
        }

        // Tự hủy vòng tròn ma thuật (cho nó sống thêm 0.2s để kịp chạy hết frame mờ dần)
        Destroy(gameObject, 0.2f);
    }

    private void SummonFireball()
    {
        if (fireballPrefab != null)
        {
            AudioManager.instance.PlaySFX("FinalBossSpell"); // Phát 1 lần duy nhất tại đây

            // Sinh ra cầu lửa ngay tại vị trí của vòng tròn ma thuật
            GameObject fireball = Instantiate(fireballPrefab, transform.position, Quaternion.identity);

            // Tìm script đạn rơi thẳng đứng và khởi động nó
            VerticalBomb bombScript = fireball.GetComponent<VerticalBomb>();
            if (bombScript != null)
            {
                bombScript.Setup();
            }
        }
        else
        {
            Debug.LogWarning("Chưa gắn Prefab Cầu lửa vào FinalBossSpell!");
        }
    }
}