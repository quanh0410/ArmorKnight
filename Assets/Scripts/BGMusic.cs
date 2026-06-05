using UnityEngine;

public class BGMusic : MonoBehaviour
{
    [Header("--- CÀI ĐẶT ÂM THANH BẢN ĐỒ ---")]
    [Tooltip("Để trống: Giữ nhạc cũ. \nGõ 'NONE': Tắt nhạc kênh này. \nGõ tên: Bật nhạc mới.")]
    public string mapMusicName;
    public string mapMusicName2;

    private void Start()
    {
        if (AudioManager.instance == null) return;

        // =====================================
        // XỬ LÝ KÊNH 1 (NHẠC CHÍNH)
        // =====================================
        if (!string.IsNullOrEmpty(mapMusicName))
        {
            if (mapMusicName.Trim().ToUpper() == "NONE")
            {
                AudioManager.instance.StopMusic(1);
            }
            else
            {
                AudioManager.instance.PlayMusicWithFade(mapMusicName, 1.5f, 1);
            }
        }

        // =====================================
        // XỬ LÝ KÊNH 2 (NHẠC PHỤ/MÔI TRƯỜNG)
        // =====================================
        if (!string.IsNullOrEmpty(mapMusicName2))
        {
            if (mapMusicName2.Trim().ToUpper() == "NONE")
            {
                AudioManager.instance.StopMusic(2);
            }
            else
            {
                AudioManager.instance.PlayMusicWithFade(mapMusicName2, 1.5f, 2);
            }
        }

        // Dọn dẹp các âm thanh hiệu ứng (SFX) từ Map cũ tránh kẹt tiếng
        AudioManager.instance.StopAllSFX();
    }
}