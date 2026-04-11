using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    private PlayerController playerController;

    // --- 1. KHAI BÁO HASH ID (Tối ưu hiệu suất, tránh dùng String) ---
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int YVelocityParam = Animator.StringToHash("yVelocity");
    private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
    private static readonly int IsDashingParam = Animator.StringToHash("IsDashing");
    private static readonly int IsWallSlidingParam = Animator.StringToHash("IsWallSliding");
    private static readonly int Attack1Trigger = Animator.StringToHash("Attack1");
    private static readonly int Attack2Trigger = Animator.StringToHash("Attack2");
    private static readonly int IsWallClimbingParam = Animator.StringToHash("IsWallClimbing");
    private static readonly int HitTrigger = Animator.StringToHash("Hit");
    private static readonly int DieTrigger = Animator.StringToHash("Die");
    private static readonly int IsHealingParam = Animator.StringToHash("IsHealing");
    private static readonly int IsAttackingParam = Animator.StringToHash("IsAttacking");
    private static readonly int TurnAroundTrigger = Animator.StringToHash("TurnAround");


    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        // Tách riêng phần cập nhật các trạng thái liên tục cho dễ nhìn
        UpdateMovementStates();
    }

    // --- 2. CẬP NHẬT TRẠNG THÁI LIÊN TỤC (Chạy mỗi frame) ---
    private void UpdateMovementStates()
    {
        if (rb == null || playerController == null) return;

        // Chỉ cần chặn khi đang bị khóa đòn chém (isAttackLocked - nếu bạn có thêm vào)
        if (!playerController.enabled) return;

        float moveInput = Input.GetAxisRaw("Horizontal");
        anim.SetFloat(SpeedParam, Mathf.Abs(moveInput));

        anim.SetFloat(YVelocityParam, rb.linearVelocity.y);
        anim.SetBool(IsGroundedParam, playerController.IsGrounded());
        anim.SetBool(IsDashingParam, playerController.isDashing);
        anim.SetBool(IsWallSlidingParam, playerController.isWallSliding);
    }

    public void PlayAttackAnimation(int comboStep)
    {
        // Bật khiên chắn: Cấm Any State chuyển sang Jump/Fall/Run
        anim.SetBool(IsAttackingParam, true);

        if (comboStep == 1) anim.SetTrigger(Attack1Trigger);
        else if (comboStep == 2) anim.SetTrigger(Attack2Trigger);

        // Đặt đồng hồ hẹn giờ tắt khiên. 
        // StopCoroutine để lỡ bạn bấm chém liên tục thì nó sẽ đếm lại từ đầu
        StopCoroutine("ResetAttackBool");
        StartCoroutine("ResetAttackBool");
    }

    private IEnumerator ResetAttackBool()
    {
        // Chờ 0.3 giây (Bằng đúng attackCooldown trong PlayerController của bạn)
        yield return new WaitForSeconds(0.3f);

        // Hạ khiên xuống: Cho phép Any State hoạt động lại bình thường
        anim.SetBool(IsAttackingParam, false);
    }

    public void SetWallClimbAnimation(bool isClimbing)
    {
        anim.SetBool(IsWallClimbingParam, isClimbing);
    }

    public void PlayHitAnimation()
    {
        anim.SetTrigger(HitTrigger);
    }

    public void PlayDieAnimation()
    {
        anim.SetBool(IsGroundedParam, true);
        anim.SetFloat(YVelocityParam, 0f);
        anim.SetFloat(SpeedParam, 0f);

        // 2. Kích hoạt lệnh chết một cách dứt khoát
        anim.SetTrigger(DieTrigger);
    }

    public void SetHealingAnimation(bool isHealing)
    {
        anim.SetBool(IsHealingParam, isHealing);
    }

    public void PlayTurnAnimation()
    {
        anim.SetTrigger(TurnAroundTrigger);
    }
}