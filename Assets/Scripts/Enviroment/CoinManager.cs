using UnityEngine;
using System;

public class CoinManager : MonoBehaviour
{
    // Singleton để truy cập từ bất kỳ đâu
    public static CoinManager Instance { get; private set; }

    [field: SerializeField] public int totalCoins { get; private set; }
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

    public bool SpendCoins(int amount)
    {
        if (totalCoins >= amount)
        {
            totalCoins -= amount;
            // Phát tín hiệu để UI Tiền trên màn hình cập nhật lại con số
            OnCoinCollected?.Invoke(totalCoins);
            Debug.Log("Đã tiêu: " + amount + ". Còn lại: " + totalCoins);
            return true; // Giao dịch thành công
        }
        else
        {
            Debug.Log("Không đủ tiền!");
            return false; // Giao dịch thất bại
        }
    }
}