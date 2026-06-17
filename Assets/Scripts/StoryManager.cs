using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class StoryFrame
{
    public Sprite image;

    [TextArea(2, 4)]
    public string[] textLines;
}

public class StoryManager : MonoBehaviour
{
    public static StoryManager instance;

    [Header("--- KẾT NỐI UI ---")]
    public GameObject storyCanvas;
    public CanvasGroup mainCanvasGroup; 
    public CanvasGroup textCanvasGroup; 
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
        if (textCanvasGroup != null) textCanvasGroup.alpha = 0f; 
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
            storyImage.sprite = frame.image;

            if (textCanvasGroup != null) textCanvasGroup.alpha = 0f;

            yield return StartCoroutine(FadeRoutine(mainCanvasGroup, 0f, 1f, fadeDuration));

            if (frame.textLines != null && frame.textLines.Length > 0)
            {
                for (int i = 0; i < frame.textLines.Length; i++)
                {
                    subtitleText.text = frame.textLines[i];

                    if (textCanvasGroup != null)
                        yield return StartCoroutine(FadeRoutine(textCanvasGroup, 0f, 1f, textFadeDuration));
                    else
                        subtitleText.alpha = 1f; 

                    isWaitingForInput = true;
                    while (isWaitingForInput)
                    {
                        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                        {
                            isWaitingForInput = false;
                        }
                        yield return null;
                    }

                    if (i < frame.textLines.Length - 1 && textCanvasGroup != null)
                    {
                        yield return StartCoroutine(FadeRoutine(textCanvasGroup, 1f, 0f, textFadeDuration));
                    }
                }
            }

            yield return StartCoroutine(FadeRoutine(mainCanvasGroup, 1f, 0f, fadeDuration / 2f));
        }

        Time.timeScale = 1f;
        storyCanvas.SetActive(false);
        isStoryActive = false;
    }

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