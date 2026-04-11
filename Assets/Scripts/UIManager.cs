using UnityEngine;
using UnityEngine.UI;
using TMPro; //
using System.Collections; // BẮT BUỘC: Thêm thư viện này để dùng Coroutine

public class UIManager : MonoBehaviour
{
    [Header("Health UI Settings")]
    public Image[] heartImages; //
    public Color fullHeartColor = Color.white; //
    public Color emptyHeartColor = new Color(0.2f, 0.2f, 0.2f, 1f); //

    [Header("Energy UI Settings")]
    public Image energyFillImage; //

    // --- CẬP NHẬT: THÔNG SỐ HIỆU ỨNG ẨN/HIỆN COIN ---
    [Header("Coin UI Settings")]
    public CanvasGroup coinUIGroup; // Nhóm chứa cả Icon và Số lượng xu
    public TextMeshProUGUI coinText; //
    public float coinDisplayDuration = 2.5f; // Thời gian hiển thị trước khi ẩn đi
    public float fadeSpeed = 4f; // Tốc độ mờ dần

    private Coroutine coinFadeCoroutine; // Biến lưu trữ tiến trình mờ UI

    private void OnEnable()
    {
        PlayerHealth.OnHealthChanged += UpdateHealth; //
        PlayerEnergy.OnEnergyChanged += UpdateEnergy; //
        CoinManager.OnCoinCollected += UpdateCoinDisplay; //
    }

    private void OnDisable()
    {
        PlayerHealth.OnHealthChanged -= UpdateHealth; //
        PlayerEnergy.OnEnergyChanged -= UpdateEnergy; //
        CoinManager.OnCoinCollected -= UpdateCoinDisplay; //
    }

    private void Start()
    {
        // 1. Cài đặt số 0 mặc định
        if (coinText != null) coinText.text = "0";

        // 2. Ẩn hoàn toàn bảng tiền lúc mới vào game
        if (coinUIGroup != null) coinUIGroup.alpha = 0f;
    }

    // Các hàm UpdateHealth và UpdateEnergy giữ nguyên của bạn
    private void UpdateHealth(int currentHealth, int maxHealth) //
    {
        for (int i = 0; i < heartImages.Length; i++) //
        {
            if (i < maxHealth) //
            {
                heartImages[i].enabled = true; //

                if (i < currentHealth) //
                {
                    heartImages[i].color = fullHeartColor; //
                }
                else //
                {
                    heartImages[i].color = emptyHeartColor; //
                }
            }
            else //
            {
                heartImages[i].enabled = false; //
            }
        }
    }

    private void UpdateEnergy(int currentEnergy, int maxEnergy) //
    {
        if (energyFillImage != null && maxEnergy > 0) //
        {
            energyFillImage.fillAmount = (float)currentEnergy / maxEnergy; //
        }
    }

    // --- HÀM CẬP NHẬT GIAO DIỆN COIN ĐƯỢC NÂNG CẤP ---
    private void UpdateCoinDisplay(int newTotal)
    {
        if (coinText != null)
        {
            coinText.text = newTotal.ToString(); //
        }

        if (coinUIGroup != null)
        {
            // Nếu UI đang trong quá trình mờ đi mà Player lại ăn xu mới -> Ngắt lệnh mờ đi
            if (coinFadeCoroutine != null)
            {
                StopCoroutine(coinFadeCoroutine);
            }

            // Khởi động lại hiệu ứng Hiện -> Chờ -> Ẩn
            coinFadeCoroutine = StartCoroutine(ShowAndHideCoinUIRoutine());
        }
    }

    // Tiến trình xử lý hoạt ảnh Fade In / Fade Out
    private IEnumerator ShowAndHideCoinUIRoutine()
    {
        // 1. FADE IN (Sáng dần lên)
        while (coinUIGroup.alpha < 1f)
        {
            coinUIGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null; // Chờ khung hình tiếp theo
        }
        coinUIGroup.alpha = 1f;

        // 2. CHỜ (Giữ nguyên trên màn hình)
        yield return new WaitForSeconds(coinDisplayDuration);

        // 3. FADE OUT (Mờ dần đi)
        while (coinUIGroup.alpha > 0f)
        {
            coinUIGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        coinUIGroup.alpha = 0f;
    }
}