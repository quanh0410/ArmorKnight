using UnityEngine;
using UnityEngine.Audio; // Khai báo thư viện Audio của Unity
using System;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;

    [Tooltip("Kéo nhóm Music hoặc SFX từ Audio Mixer vào đây")]
    public AudioMixerGroup mixerGroup;

    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 1.5f)] public float pitch = 1f;
    public bool loop = false;
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
    public AudioSource musicSource;
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

    public void PlayMusic(string soundName)
    {
        Sound s = Array.Find(musicSounds, x => x.name == soundName);
        if (s == null) return;

        musicSource.clip = s.clip;
        musicSource.outputAudioMixerGroup = s.mixerGroup; // Ép chạy qua luồng Mixer
        musicSource.volume = s.volume;
        musicSource.pitch = s.pitch;
        musicSource.loop = s.loop;
        musicSource.Play();
    }

    public void PlaySFX(string soundName)
    {
        Sound s = Array.Find(sfxSounds, x => x.name == soundName);
        if (s == null) return;

        sfxSource.outputAudioMixerGroup = s.mixerGroup; // Ép chạy qua luồng SFX
        sfxSource.PlayOneShot(s.clip, s.volume);
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }
}