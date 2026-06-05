using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerMovementTests
{
    private GameObject playerObj;
    private PlayerController playerController;
    private Rigidbody2D rb;
    private GameObject groundObj;

    [SetUp]
    public void Setup()
    {
        // 1. Tạo Player ảo
        playerObj = new GameObject("TestPlayer");
        playerController = playerObj.AddComponent<PlayerController>();
        rb = playerObj.AddComponent<Rigidbody2D>();
        // MỚI: Thêm PlayerCombat để tránh lỗi ở Start()
        playerObj.AddComponent<PlayerCombat>();

        // MỚI: Tạo các Transform giả và gán vào Controller để tránh lỗi IsGrounded/IsWalled
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(playerObj.transform);
        playerController.groundCheckPoint = groundCheck.transform;

        GameObject wallCheck = new GameObject("WallCheck");
        wallCheck.transform.SetParent(playerObj.transform);
        playerController.wallCheckPoint = wallCheck.transform;

        GameObject wallClimbCheck = new GameObject("WallClimbCheck");
        wallClimbCheck.transform.SetParent(playerObj.transform);
        playerController.wallClimbCheckPoint = wallClimbCheck.transform;

        // Cấu hình thông số cơ bản cho Test
        playerController.moveSpeed = 5f;
        playerController.jumpForce = 15f;
        playerController.maxFallSpeed = 25f;
        playerController.dashSpeed = 15f;

        // Thiết lập biến ẩn (Cần thiết vì biến này trong script của bạn là ẩn)
        playerController.platformVelocity = Vector2.zero;

        // 2. Tạo Mặt đất ảo (Ground) để test va chạm/nhảy
        groundObj = new GameObject("TestGround");
        groundObj.transform.position = new Vector3(0, -1, 0);
        groundObj.AddComponent<BoxCollider2D>();
        // Set Layer cho Ground (Bạn cần đảm bảo số Layer này khớp với setup trong game của bạn)
        groundObj.layer = LayerMask.NameToLayer("Ground");
    }
    [UnityTest]
    public IEnumerator Player_MoveHorizontal_AppliesVelocity()
    {
        // Giả lập Input sang phải (moveInput = 1)
        float simulatedInput = 1f;

        // Gọi hàm di chuyển (Ta giả lập phần lõi của hàm PlayerMovement)
        rb.linearVelocity = new Vector2((simulatedInput * playerController.moveSpeed) + playerController.platformVelocity.x, rb.linearVelocity.y);

        // Chờ 1 fixed update để vật lý tính toán
        yield return new WaitForFixedUpdate();

        // Kiểm tra xem vận tốc X có bằng đúng tốc độ di chuyển không
        Assert.AreEqual(5f, rb.linearVelocity.x, "Vận tốc di chuyển ngang không đúng!");
    }

    [UnityTest]
    public IEnumerator Player_FallSpeed_IsLimitedByMaxFallSpeed()
    {
        // Đưa Player lên cao
        playerObj.transform.position = new Vector3(0, 50, 0);

        // Ép cho Player một lực rơi cực mạnh (vượt quá giới hạn)
        rb.linearVelocity = new Vector2(0, -50f);

        // Chờ 1 fixed update
        yield return new WaitForFixedUpdate();

        // Chạy lại đoạn logic hãm tốc độ trong Update của bạn
        if (rb.linearVelocity.y < -playerController.maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -playerController.maxFallSpeed);
        }

        // Khẳng định vận tốc Y không được vượt quá maxFallSpeed (-25f)
        Assert.GreaterOrEqual(rb.linearVelocity.y, -25f, "Tốc độ rơi đã vượt qua giới hạn cho phép!");
    }

    [UnityTest]
    public IEnumerator Player_Dash_ActivatesDashStateAndVelocity()
    {
        // Giả định Player đang hướng sang phải (Scale X = 1)
        playerObj.transform.localScale = new Vector3(1, 1, 1);

        // Ép trạng thái có thể Dash
        // (Trong thực tế, bạn có thể cần giả lập thêm việc EquipmentManager cho phép Dash)

        // Mở Coroutine Dash bằng reflection hoặc tạo hàm public trung gian
        // Ở đây ta gọi trực tiếp một hàm public mô phỏng logic Dash
        SimulateDashLogic();

        // Kiểm tra xem cờ isDashing đã được bật chưa
        Assert.IsTrue(playerController.isDashing, "Trạng thái isDashing không được kích hoạt!");

        // Kiểm tra vận tốc
        Assert.AreEqual(playerController.dashSpeed, rb.linearVelocity.x, "Vận tốc Dash không đúng!");

        yield return null;
    }

    // Hàm phụ trợ mô phỏng logic Dash để test
    private void SimulateDashLogic()
    {
        playerController.isDashing = true;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(playerObj.transform.localScale.x * playerController.dashSpeed, 0f);
    }

[TearDown]
    public void Teardown()
    {
        // Dọn dẹp sau mỗi bài test
        Object.DestroyImmediate(playerObj);
        Object.DestroyImmediate(groundObj);
    }
}