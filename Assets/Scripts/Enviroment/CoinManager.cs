using UnityEngine;
using System;

public class CoinManager : MonoBehaviour
{
    // Singleton để truy cập từ bất kỳ đâu
    public static CoinManager Instance { get; private set; }

    public int totalCoins { get; private set; }

    // Sự kiện phát loa thông báo mỗi khi nhặt được tiền
    public static event Action<int> OnCoinCollected;

    private void Awake()
    {
        // Thiết lập Singleton
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
        // Phát tín hiệu cho UI cập nhật
        OnCoinCollected?.Invoke(totalCoins);

        Debug.Log("Tổng tiền hiện tại: " + totalCoins);
    }

    // Trong CoinManager.cs
    public void LoadData(int amount)
    {
        totalCoins = amount;
        // Cần gọi Invoke để UI cập nhật lại con số từ file Save
        OnCoinCollected?.Invoke(totalCoins);
    }
}