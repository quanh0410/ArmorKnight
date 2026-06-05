using UnityEngine;
using TMPro;
using System.Collections;

public class AreaUIManager : MonoBehaviour
{
    public static AreaUIManager instance;

    [Header("--- UI Elements ---")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;

    [Header("--- Settings ---")]
    public float fadeInDuration = 1f;
    public float stayDuration = 2.5f;
    public float fadeOutDuration = 1f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        // Biến nó thành Singleton giống AudioManager
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }

        canvasGroup.alpha = 0f; // Luôn ẩn khi mới vào game
    }

    public void ShowAreaAnnouncement(string areaName, string subtitle = "")
    {
        titleText.text = areaName;

        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
            subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
        }

        // Nếu đang chạy hiệu ứng cũ thì dập tắt ngay để chạy cái mới
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        // 1. Sáng dần (Fade In)
        float t = 0;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeInDuration);
            yield return null;
        }

        // 2. Đứng hình cho người chơi đọc (Stay)
        yield return new WaitForSeconds(stayDuration);

        // 3. Tối dần (Fade Out)
        t = 0;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}