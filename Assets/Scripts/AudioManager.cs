using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;

    [Tooltip("Kéo nhóm Music hoặc SFX từ Audio Mixer vào đây")]
    public AudioMixerGroup mixerGroup;

    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 1.5f)] public float pitch = 1f;
    public bool loop = true; // Nhạc nền nên để mặc định là true
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("--- BỘ TRỘN TỔNG (AUDIO MIXER) ---")]
    public AudioMixer mainMixer;

    [Header("--- DANH SÁCH ÂM THANH ---")]
    public Sound[] musicSounds;
    public Sound[] sfxSounds;

    [Header("--- KÊNH PHÁT (CHANNELS) ---")]
    public AudioSource musicSource1; // Kênh 1: Nhạc chính
    public AudioSource musicSource2; // Kênh 2: Nhạc môi trường/Phụ
    public AudioSource sfxSource;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- PHÁT NHẠC CHẾ ĐỘ THƯỜNG ---
    public void PlayMusic(string soundName, int channel = 1)
    {
        Sound s = Array.Find(musicSounds, x => x.name == soundName);
        if (s == null) return;

        AudioSource targetSource = (channel == 2) ? musicSource2 : musicSource1;

        targetSource.clip = s.clip;
        targetSource.outputAudioMixerGroup = s.mixerGroup;
        targetSource.volume = s.volume;
        targetSource.pitch = s.pitch;
        targetSource.loop = s.loop;
        targetSource.Play();
    }

    // --- PHÁT NHẠC CHẾ ĐỘ CHUYỂN MƯỢT (FADE) ---
    private Coroutine fadeRoutine1;
    private Coroutine fadeRoutine2;

    public void PlayMusicWithFade(string soundName, float transitionDuration = 1.5f, int channel = 1)
    {
        Sound s = Array.Find(musicSounds, x => x.name == soundName);
        if (s == null) return;

        AudioSource targetSource = (channel == 2) ? musicSource2 : musicSource1;

        if (channel == 1)
        {
            if (fadeRoutine1 != null) StopCoroutine(fadeRoutine1);
            fadeRoutine1 = StartCoroutine(FadeMusicRoutine(s, transitionDuration, targetSource));
        }
        else
        {
            if (fadeRoutine2 != null) StopCoroutine(fadeRoutine2);
            fadeRoutine2 = StartCoroutine(FadeMusicRoutine(s, transitionDuration, targetSource));
        }
    }

    private IEnumerator FadeMusicRoutine(Sound newSound, float duration, AudioSource targetSource)
    {
        float halfDuration = duration / 2f;
        float startVolume = targetSource.volume;

        // 1. Tối dần âm lượng bài nhạc đang phát (nếu có)
        if (targetSource.isPlaying)
        {
            for (float t = 0; t < halfDuration; t += Time.unscaledDeltaTime)
            {
                targetSource.volume = Mathf.Lerp(startVolume, 0f, t / halfDuration);
                yield return null;
            }
        }

        // 2. Đổi sang bài nhạc mới
        targetSource.clip = newSound.clip;
        targetSource.outputAudioMixerGroup = newSound.mixerGroup;
        targetSource.pitch = newSound.pitch;
        targetSource.loop = newSound.loop;
        targetSource.Play();

        // 3. Sáng dần âm lượng bài nhạc mới lên mức chuẩn
        for (float t = 0; t < halfDuration; t += Time.unscaledDeltaTime)
        {
            targetSource.volume = Mathf.Lerp(0f, newSound.volume, t / halfDuration);
            yield return null;
        }

        targetSource.volume = newSound.volume;
    }

    // --- CÁC HÀM TIỆN ÍCH KHÁC ---
    public void PlaySFX(string soundName)
    {
        Sound s = Array.Find(sfxSounds, x => x.name == soundName);
        if (s == null) return;

        sfxSource.outputAudioMixerGroup = s.mixerGroup;
        sfxSource.PlayOneShot(s.clip, s.volume);
    }

    public void StopMusic(int channel = 1)
    {
        AudioSource targetSource = (channel == 2) ? musicSource2 : musicSource1;
        if (targetSource != null) targetSource.Stop();
    }

    public void StopAllSFX()
    {
        if (sfxSource != null) sfxSource.Stop();
    }
}