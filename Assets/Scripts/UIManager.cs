using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
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



    public void ForceShowCoinUI(bool show)
    {
        keepCoinUIOpen = show; 
        if (coinUIGroup == null) return;

        if (coinFadeCoroutine != null) StopCoroutine(coinFadeCoroutine);

        if (show)
        {
            coinUIGroup.alpha = 1f; 
        }
        else
        {
            coinFadeCoroutine = StartCoroutine(ShowAndHideCoinUIRoutine());
        }
    }

    private void UpdateCoinDisplay(int newTotal)
    {
        if (coinText != null) coinText.text = newTotal.ToString();

        if (coinUIGroup != null)
        {
            if (coinFadeCoroutine != null) StopCoroutine(coinFadeCoroutine);

            if (keepCoinUIOpen)
            {
                coinUIGroup.alpha = 1f;
            }
            else
            {
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