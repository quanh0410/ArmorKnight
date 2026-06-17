using UnityEngine;
using System;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [field: SerializeField] public int totalCoins { get; private set; }
    // Sự kiện phát loa thông báo mỗi khi nhặt được tiền
    public static event Action<int> OnCoinCollected;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddCoins(int amount)
    {
        totalCoins += amount;
        OnCoinCollected?.Invoke(totalCoins);

        Debug.Log("Tổng tiền hiện tại: " + totalCoins);
    }

    public void LoadData(int amount)
    {
        totalCoins = amount;
        OnCoinCollected?.Invoke(totalCoins);
    }

    public bool SpendCoins(int amount)
    {
        if (totalCoins >= amount)
        {
            totalCoins -= amount;
            OnCoinCollected?.Invoke(totalCoins);
            Debug.Log("Đã tiêu: " + amount + ". Còn lại: " + totalCoins);
            return true;
        }
        else
        {
            Debug.Log("Không đủ tiền!");
            return false;
        } 
    }
}