using UnityEngine;
using System.Collections.Generic;

public class StoryTrigger : MonoBehaviour
{
    [Header("--- LƯU TRỮ ---")]
    public string storyID;
    public bool playOnce = true;

    [Header("--- CÁCH KÍCH HOẠT ---")]
    [Tooltip("Bật cái này nếu muốn đi ngang qua là phát truyện (Dùng Collider2D)")]
    public bool triggerOnEnter = false;

    [Header("--- KỊCH BẢN KỂ CHUYỆN ---")]
    public List<StoryFrame> storyFrames;

    private void Start()
    {
        if (playOnce && SaveManager.instance != null && !string.IsNullOrEmpty(storyID))
        {
            if (SaveManager.instance.IsObjectInteracted(storyID))
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggerOnEnter && collision.CompareTag("Player"))
        {
            TriggerStoryEvent();
        }
    }

    // ==========================================
    // MỚI: HÀM PUBLIC ĐỂ GỌI TỪ BÊN NGOÀI (LÚC CHẾT)
    // ==========================================
    public void TriggerStoryEvent()
    {
        if (StoryManager.instance != null)
        {
            StoryManager.instance.PlayStory(storyFrames);
        }

        if (playOnce && SaveManager.instance != null && !string.IsNullOrEmpty(storyID))
        {
            SaveManager.instance.SaveObjectState(storyID, true);
        }

        // Kể xong thì tự hủy cục Trigger này luôn
        Destroy(gameObject);
    }
}