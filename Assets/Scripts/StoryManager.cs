using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class StoryFrame
{
    public Sprite image;

    // --- SỬA Ở ĐÂY: Chuyển thành Mảng để chứa nhiều dòng chữ trên cùng 1 ảnh ---
    [TextArea(2, 4)]
    public string[] textLines;
}

public class StoryManager : MonoBehaviour
{
    public static StoryManager instance;

    [Header("--- KẾT NỐI UI ---")]
    public GameObject storyCanvas;
    public CanvasGroup mainCanvasGroup; // Quản lý mờ/tỏ của TOÀN BỘ khung hình (gồm cả ảnh)
    public CanvasGroup textCanvasGroup; // --- MỚI: Chỉ quản lý mờ/tỏ của riêng dòng CHỮ ---
    public Image storyImage;
    public TextMeshProUGUI subtitleText;

    [Header("--- CÀI ĐẶT HIỆU ỨNG ---")]
    public float fadeDuration = 0.5f;
    [Tooltip("Tốc độ chữ mờ đi khi bấm qua dòng mới")]
    public float textFadeDuration = 0.2f;

    private bool isStoryActive = false;
    private bool isWaitingForInput = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (storyCanvas != null) storyCanvas.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
        if (textCanvasGroup != null) textCanvasGroup.alpha = 0f; // Mới: Chữ mặc định ẩn
    }

    public void PlayStory(List<StoryFrame> frames)
    {
        if (isStoryActive) return;
        StartCoroutine(StoryRoutine(frames));
    }

    private IEnumerator StoryRoutine(List<StoryFrame> frames)
    {
        isStoryActive = true;
        storyCanvas.SetActive(true);

        // Đóng băng thời gian game
        Time.timeScale = 0f;

        // VÒNG LẶP 1: Duyệt qua từng Khung hình (Ảnh)
        foreach (StoryFrame frame in frames)
        {
            // Thay ảnh nền mới
            storyImage.sprite = frame.image;

            // Đảm bảo lúc ảnh mới hiện lên thì chữ đang ở trạng thái ẩn để chuẩn bị hiệu ứng
            if (textCanvasGroup != null) textCanvasGroup.alpha = 0f;

            // Fade In TOÀN BỘ hệ thống (Ảnh bắt đầu hiện lên)
            yield return StartCoroutine(FadeRoutine(mainCanvasGroup, 0f, 1f, fadeDuration));

            // VÒNG LẶP 2: Duyệt qua từng dòng chữ của ẢNH HIỆN TẠI
            if (frame.textLines != null && frame.textLines.Length > 0)
            {
                for (int i = 0; i < frame.textLines.Length; i++)
                {
                    // 1. Gán chữ của dòng hiện tại
                    subtitleText.text = frame.textLines[i];

                    // 2. Chỉ Fade In riêng dòng chữ (Ảnh nền vẫn giữ nguyên)
                    if (textCanvasGroup != null)
                        yield return StartCoroutine(FadeRoutine(textCanvasGroup, 0f, 1f, textFadeDuration));
                    else
                        subtitleText.alpha = 1f; // Phòng hờ nếu quên kéo CanvasGroup chữ

                    // 3. Đợi người chơi bấm phím để qua dòng tiếp theo
                    isWaitingForInput = true;
                    while (isWaitingForInput)
                    {
                        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                        {
                            isWaitingForInput = false;
                        }
                        yield return null;
                    }

                    // 4. Nếu vẫn còn dòng chữ tiếp theo cho CÙNG 1 ẢNH này, Fade Out chữ cũ đi để chuẩn bị nạp chữ mới
                    if (i < frame.textLines.Length - 1 && textCanvasGroup != null)
                    {
                        yield return StartCoroutine(FadeRoutine(textCanvasGroup, 1f, 0f, textFadeDuration));
                    }
                }
            }

            // Hết toàn bộ chữ của ảnh này -> Fade Out toàn bộ để đổi sang ảnh tiếp theo
            yield return StartCoroutine(FadeRoutine(mainCanvasGroup, 1f, 0f, fadeDuration / 2f));
        }

        // Kết thúc kịch bản truyện
        Time.timeScale = 1f;
        storyCanvas.SetActive(false);
        isStoryActive = false;
    }

    // Hàm Fade dùng chung tối ưu, nhận vào CanvasGroup bất kỳ
    private IEnumerator FadeRoutine(CanvasGroup targetGroup, float startAlpha, float endAlpha, float duration)
    {
        if (targetGroup == null) yield break;

        float time = 0;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            targetGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            yield return null;
        }
        targetGroup.alpha = endAlpha;
    }
}