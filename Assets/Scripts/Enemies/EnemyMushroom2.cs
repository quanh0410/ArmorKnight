using UnityEngine;
using System.Collections;

public class EnemyMushroom2 : EnemyBase
{
    private enum MushroomState { Sleeping, WakingUp, Patrolling, PreparingCharge, Charging, Braking }
    private MushroomState currentState;

    [Header("--- CÀI ĐẶT SÁT THƯƠNG ---")]
    public EnemyDamage bodyHitbox;
    public int normalDamage = 1;
    public int chargeDamage = 2;

    [Header("--- CÀI ĐẶT HƯỚNG ---")]
    public bool spriteFacesRight = false;

    [Header("--- CÀI ĐẶT THỨC DẬY ---")]
    public float wakeUpRadius = 5f;

    [Header("--- CÀI ĐẶT ĐIỂM TUẦN TRA ---")]
    public Transform pointA;
    public Transform pointB;
    private Transform currentTarget;

    [Header("--- CÀI ĐẶT TUẦN TRA (Patrol) ---")]
    public float patrolAcceleration = 5f;

    [Header("--- 1. CÀI ĐẶT CHẠY LẤY ĐÀ ---")]
    public float prepareRunSpeed = 6f;
    public float prepareDuration = 0.4f;

    [Header("--- 2. CÀI ĐẶT CÚ NHẢY HÚC ---")]
    public float chargeForceX = 12f;
    public float chargeJumpForceY = 6f;
    public float chargeDeceleration = 10f;

    private float prepareTimer = 0f;
    private bool hasLaunchedAttack = false;

    protected override void Awake()
    {
        base.Awake();

        if (pointA != null) pointA.SetParent(null);
        if (pointB != null) pointB.SetParent(null);

        currentState = MushroomState.Sleeping;
        currentTarget = pointB; // Mục tiêu ban đầu luôn là B

        rb.linearVelocity = Vector2.zero;
        hasLaunchedAttack = false;
    }

    protected override void ExecuteAI()
    {
        if (player == null) return;

        switch (currentState)
        {
            case MushroomState.Sleeping:
                HandleSleeping();
                break;
            case MushroomState.WakingUp:
                break;
            case MushroomState.Patrolling:
                HandlePatrolling();
                break;
            case MushroomState.PreparingCharge:
                HandlePreparingCharge();
                break;
            case MushroomState.Charging:
                HandleCharging();
                break;
            case MushroomState.Braking:
                HandleBraking();
                break;
        }

        UpdateAnimations();
    }

    private void HandleSleeping()
    {
        StopMovement();

        if (Vector2.Distance(transform.position, player.position) <= wakeUpRadius)
        {
            StartCoroutine(WakeUpRoutine());
        }
    }

    private IEnumerator WakeUpRoutine()
    {
        currentState = MushroomState.WakingUp;
        if (anim != null) anim.SetTrigger("WakeUp");
        yield return new WaitForSeconds(0.1f);

        if (anim != null)
        {
            float animLength = anim.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(animLength);
        }

        currentState = MushroomState.Patrolling;
    }

    private bool IsPlayerInPatrolZone()
    {
        if (player == null || pointA == null || pointB == null) return false;

        float minX = Mathf.Min(pointA.position.x, pointB.position.x);
        float maxX = Mathf.Max(pointA.position.x, pointB.position.x);

        bool isXInRange = player.position.x >= minX && player.position.x <= maxX;
        bool isYInRange = Mathf.Abs(player.position.y - transform.position.y) < 2f;

        return isXInRange && isYInRange;
    }

    // ==========================================
    // SỬA LỖI ĐẢO HƯỚNG BẮT ĐẦU TỪ ĐÂY
    // ==========================================
    private void HandlePatrolling()
    {
        // Nếu chạm đích -> ĐỔI MỤC TIÊU LẬP TỨC
        if (Mathf.Abs(transform.position.x - currentTarget.position.x) < 0.5f)
        {
            currentTarget = (currentTarget == pointA) ? pointB : pointA;

            if (IsPlayerInPatrolZone())
            {
                currentState = MushroomState.PreparingCharge;
                prepareTimer = prepareDuration;
                hasLaunchedAttack = false;
            }
        }
        else
        {
            float dirX = Mathf.Sign(currentTarget.position.x - transform.position.x);
            Flip(dirX, spriteFacesRight);

            float targetVelocityX = dirX * moveSpeed;
            float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetVelocityX, patrolAcceleration * Time.deltaTime);
            rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
        }
    }

    private void HandlePreparingCharge()
    {
        // Luôn luôn hướng mặt và chạy về phía currentTarget
        float dirX = Mathf.Sign(currentTarget.position.x - transform.position.x);
        Flip(dirX, spriteFacesRight);

        rb.linearVelocity = new Vector2(dirX * prepareRunSpeed, rb.linearVelocity.y);

        prepareTimer -= Time.deltaTime;
        if (prepareTimer <= 0f)
        {
            currentState = MushroomState.Charging;
            LaunchJumpAttack();
        }
    }

    private void LaunchJumpAttack()
    {
        if (hasLaunchedAttack) return;
        hasLaunchedAttack = true;

        float dirX = Mathf.Sign(currentTarget.position.x - transform.position.x);
        Flip(dirX, spriteFacesRight);

        rb.linearVelocity = new Vector2(dirX * chargeForceX, chargeJumpForceY);
    }

    private void HandleCharging()
    {
        // 1. Tới đích thành công
        if (Mathf.Abs(transform.position.x - currentTarget.position.x) < 0.5f)
        {
            currentState = MushroomState.Braking;
        }
        // 2. Bị ngắt đòn (Bị chém Knockback hoặc tông trúng tường)
        else if (Mathf.Abs(rb.linearVelocity.x) < 0.2f)
        {
            currentState = MushroomState.Braking;
            // Lúc này bị ngắt, nhưng currentTarget CHƯA HỀ BỊ ĐỔI. 
            // Nó vẫn sẽ ghi nhớ đích đến.
        }
    }

    private void HandleBraking()
    {
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, chargeDeceleration * Time.deltaTime);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);

        // Khi đã dừng hẳn
        if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            // NẾU THỰC SỰ ĐÃ TỚI ĐÍCH -> Mới cho phép quay đầu
            if (Mathf.Abs(transform.position.x - currentTarget.position.x) < 0.5f)
            {
                currentTarget = (currentTarget == pointA) ? pointB : pointA;
            }

            // Quyết định hành động tiếp theo
            if (IsPlayerInPatrolZone())
            {
                currentState = MushroomState.PreparingCharge;
                prepareTimer = prepareDuration;
                hasLaunchedAttack = false;
            }
            else
            {
                currentState = MushroomState.Patrolling;
            }
        }
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;

        if (currentState == MushroomState.Charging)
        {
            anim.SetBool("isRunning", false);
            anim.SetBool("isAttacking", true);
            if (bodyHitbox != null) bodyHitbox.damageAmount = chargeDamage;
        }
        else if (currentState == MushroomState.PreparingCharge || currentState == MushroomState.Braking)
        {
            anim.SetBool("isAttacking", false);
            anim.SetBool("isRunning", true);
            if (bodyHitbox != null) bodyHitbox.damageAmount = normalDamage;
        }
        else if (currentState == MushroomState.Patrolling)
        {
            anim.SetBool("isAttacking", false);
            anim.SetBool("isRunning", Mathf.Abs(rb.linearVelocity.x) > 0.1f);
            if (bodyHitbox != null) bodyHitbox.damageAmount = normalDamage;
        }
        else
        {
            anim.SetBool("isRunning", false);
            anim.SetBool("isAttacking", false);
        }
    }

    protected void Flip(float direction, bool defaultFacesRight)
    {
        if (direction == 0) return;
        float newScaleX;

        if (defaultFacesRight) newScaleX = (direction > 0) ? initialScaleX : -initialScaleX;
        else newScaleX = (direction > 0) ? -initialScaleX : initialScaleX;

        transform.localScale = new Vector3(newScaleX, transform.localScale.y, transform.localScale.z);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, wakeUpRadius);

        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pointA.position, pointB.position);

            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            float minX = Mathf.Min(pointA.position.x, pointB.position.x);
            float maxX = Mathf.Max(pointA.position.x, pointB.position.x);
            float midX = (minX + maxX) / 2f;
            float sizeX = maxX - minX;
            Gizmos.DrawCube(new Vector3(midX, transform.position.y, 0), new Vector3(sizeX, 4f, 0f));
        }
    }
}