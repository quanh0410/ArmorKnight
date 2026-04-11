using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Settings")]
    public float attackRange = 1.2f;
    public LayerMask enemyLayer;
    public LayerMask enviromentLayer;

    [Header("Hit Stop Settings")]
    public float hitStopDuration = 0.05f;

    [Header("Visual Effects Settings")]
    public GameObject hitEffectPrefab;
    public Vector2 effectOffset = Vector2.zero;

    [Header("References")]
    [SerializeField] private Transform attackPoint;

    private PlayerController playerController;
    private PlayerAnimator playerAnimator;
    private PlayerEnergy playerEnergy; // Khai báo biến mới

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        playerAnimator = GetComponent<PlayerAnimator>();
        playerEnergy = GetComponent<PlayerEnergy>();
    }

    public void Attack(int comboStep)
    {
        playerAnimator.PlayAttackAnimation(comboStep);

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        Collider2D[] hitEnviroments = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enviromentLayer);

        bool hasHit = false;
        bool hasGainedEnergy = false; // Biến đánh dấu xem nhát chém này đã hút năng lượng chưa

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();

            if (playerEnergy != null && !hasGainedEnergy)
            {
                playerEnergy.AddEnergy();
                hasGainedEnergy = true; // Khóa lại, các con quái khác bị trúng nhát này sẽ không cho thêm năng lượng nữa
            }

            if (health != null)
            {
                health.TakeDamage(10, transform);
                CinemachineShake.Instance.ShakeCamera(0.05f); // Rung nhẹ
                Vector2 spawnPos = new Vector2(enemy.transform.position.x + effectOffset.x, enemy.transform.position.y + effectOffset.y);
                ObjectPoolManager.Instance.Spawn(hitEffectPrefab, spawnPos, Quaternion.identity);

                hasHit = true;
            }

            playerController.HandleAttackRecoil();
        }

        foreach (Collider2D env in hitEnviroments)
        {
            EGoblinBomb bomb = env.GetComponent<EGoblinBomb>();
            if (bomb != null)
            {
                bomb.Deflect(transform);
                CinemachineShake.Instance.ShakeCamera(0.05f);
                //Vector2 spawnPos = new Vector2(enemy.transform.position.x + effectOffset.x, enemy.transform.position.y + effectOffset.y);
                //if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, spawnPos, Quaternion.identity);

                hasHit = true;
            }
        }

        if (hasHit)
        {
            StartCoroutine(HitStop());
        }
    }

    private IEnumerator HitStop()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}