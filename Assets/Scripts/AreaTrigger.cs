using UnityEngine;

public class AreaTrigger : MonoBehaviour
{
    [Header("--- Thông tin Khu vực ---")]
    public string areaName;
    public string subtitle;

    // Tránh việc người chơi đi lùi lại vạch kích hoạt làm chữ hiện lên liên tục
    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasTriggered && collision.CompareTag("Player"))
        {
            hasTriggered = true;

            // Gọi UI Manager hiện tên
            if (AreaUIManager.instance != null)
            {
                AreaUIManager.instance.ShowAreaAnnouncement(areaName, subtitle);
            }
        }
    }
}