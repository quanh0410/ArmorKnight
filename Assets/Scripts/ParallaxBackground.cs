using UnityEngine;
using System.Collections; // BẮT BUỘC THÊM ĐỂ DÙNG COROUTINE

// ĐÃ XÓA dòng RequireComponent để không bị xung đột với Tilemap nữa

public class ParallaxBackground : MonoBehaviour
{
    [Header("--- CÀI ĐẶT CUỘN CẢNH (PARALLAX) ---")]
    [Tooltip("Tỷ lệ dịch chuyển (0 đến 1). Ví dụ: Mây = 0.1, Núi = 0.5, Cây xa = 0.8")]
    public float parallaxMultiplierX = 0.5f;
    public float parallaxMultiplierY = 0f;

    [Header("--- CÀI ĐẶT LẶP ẢNH ---")]
    public bool isInfiniteLoop = true;

    private Transform cameraTransform;
    private Vector3 cameraStartPos;
    private float startPosX;
    private float startPosY;
    private float spriteLength;
    private float boundOffset = 0f;

    // --- MỚI: Cờ kiểm tra xem Camera đã ổn định chưa ---
    private bool isReady = false;

    private IEnumerator Start()
    {
        cameraTransform = Camera.main.transform;

        yield return null;
        yield return null;

        cameraStartPos = cameraTransform.position;
        startPosX = transform.position.x;
        startPosY = transform.position.y;

        // ĐÃ NÂNG CẤP: Dùng Renderer tổng quát để hỗ trợ cả SpriteRenderer và TilemapRenderer
        Renderer myRenderer = GetComponent<Renderer>();
        if (myRenderer != null)
        {
            spriteLength = myRenderer.bounds.size.x;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy công cụ hiển thị (Renderer) nào trên đối tượng này!");
        }

        isReady = true;
    }

    void LateUpdate()
    {
        if (!isReady || cameraTransform == null) return;

        float travelX = cameraTransform.position.x - cameraStartPos.x;
        float travelY = cameraTransform.position.y - cameraStartPos.y;

        float distanceX = travelX * parallaxMultiplierX;
        float distanceY = travelY * parallaxMultiplierY;

        transform.position = new Vector3(startPosX + distanceX, startPosY + distanceY, transform.position.z);

        if (isInfiniteLoop)
        {
            float temp = travelX * (1 - parallaxMultiplierX);

            if (temp > boundOffset + spriteLength)
            {
                startPosX += spriteLength;
                boundOffset += spriteLength;
            }
            else if (temp < boundOffset - spriteLength)
            {
                startPosX -= spriteLength;
                boundOffset -= spriteLength;
            }
        }
    }
}