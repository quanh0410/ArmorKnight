using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    // --- MỚI: Thêm Singleton để Shop gọi tới ---
    public static UIManager instance;

    [Header("Health UI Settings")]
    public Image[] heartImages;
    public Color fullHeartColor = Color.white;
    public Color emptyHeartColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Header("Energy UI Settings")]
    public Image energyFillImage;

    [Header("Coin UI Settings")]
    public CanvasGroup coinUIGroup;
    public TextMeshProUGUI coinText;
    public float coinDisplayDuration = 2.5f;
    public float fadeSpeed = 4f;

    private Coroutine coinFadeCoroutine;

    // --- MỚI: Công tắc giữ UI không bị mờ ---
    private bool keepCoinUIOpen = false;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void OnEnable()
    {
        PlayerHealth.OnHealthChanged += UpdateHealth;
        PlayerEnergy.OnEnergyChanged += UpdateEnergy;
        CoinManager.OnCoinCollected += UpdateCoinDisplay;
    }

    private void OnDisable()
    {
        PlayerHealth.OnHealthChanged -= UpdateHealth;
        PlayerEnergy.OnEnergyChanged -= UpdateEnergy;
        CoinManager.OnCoinCollected -= UpdateCoinDisplay;
    }

    private void Start()
    {
        if (coinText != null) coinText.text = "0";
        if (coinUIGroup != null) coinUIGroup.alpha = 0f;
    }

    private void UpdateHealth(int currentHealth, int maxHealth)
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (i < maxHealth)
            {
                heartImages[i].enabled = true;
                heartImages[i].color = (i < currentHealth) ? fullHeartColor : emptyHeartColor;
            }
            else heartImages[i].enabled = false;
        }
    }

    private void UpdateEnergy(int currentEnergy, int maxEnergy)
    {
        if (energyFillImage != null && maxEnergy > 0)
        {
            energyFillImage.fillAmount = (float)currentEnergy / maxEnergy;
        }
    }

    // ==========================================
    // KHU VỰC XỬ LÝ UI TIỀN
    // ==========================================

    // --- MỚI: Hàm ép UI Tiền hiện lên do ShopUIManager gọi ---
    public void ForceShowCoinUI(bool show)
    {
        keepCoinUIOpen = show; // Bật/tắt công tắc
        if (coinUIGroup == null) return;

        // Dừng tiến trình mờ đi (nếu đang chạy)
        if (coinFadeCoroutine != null) StopCoroutine(coinFadeCoroutine);

        if (show)
        {
            coinUIGroup.alpha = 1f; // Hiện rõ 100% ngay lập tức
        }
        else
        {
            // Khi đóng shop, mới cho phép mờ dần đi
            coinFadeCoroutine = StartCoroutine(ShowAndHideCoinUIRoutine());
        }
    }

    private void UpdateCoinDisplay(int newTotal)
    {
        if (coinText != null) coinText.text = newTotal.ToString();

        if (coinUIGroup != null)
        {
            if (coinFadeCoroutine != null) StopCoroutine(coinFadeCoroutine);

            // TỐI ƯU: Nếu đang mở Shop (keepCoinUIOpen = true) thì giữ nguyên alpha = 1, KHÔNG chạy Coroutine
            if (keepCoinUIOpen)
            {
                coinUIGroup.alpha = 1f;
            }
            else
            {
                // Nếu không mở Shop (nhặt xu ngoài đường) thì chạy hiệu ứng bình thường
                coinFadeCoroutine = StartCoroutine(ShowAndHideCoinUIRoutine());
            }
        }
    }

    private IEnumerator ShowAndHideCoinUIRoutine()
    {
        while (coinUIGroup.alpha < 1f)
        {
            coinUIGroup.alpha += Time.unscaledDeltaTime * fadeSpeed;
            yield return null;
        }
        coinUIGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(coinDisplayDuration);

        while (coinUIGroup.alpha > 0f)
        {
            coinUIGroup.alpha -= Time.unscaledDeltaTime * fadeSpeed;
            yield return null;
        }
        coinUIGroup.alpha = 0f;
    }
}