using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    // Từ điển chứa nhiều hàng đợi (Mỗi hàng đợi là một "ngăn tủ" cho 1 loại Prefab)
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Hàm gọi vật thể ra khỏi tủ (Thay thế hoàn toàn cho Instantiate)
    public GameObject Spawn(GameObject prefab, Vector2 position, Quaternion rotation)
    {
        string key = prefab.name; // Dùng tên Prefab làm chìa khóa tìm ngăn tủ

        // Nếu tủ chưa có ngăn này, tạo một ngăn mới
        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary[key] = new Queue<GameObject>();
        }

        // Nếu trong ngăn có đồ, lấy ra xài
        if (poolDictionary[key].Count > 0)
        {
            GameObject obj = poolDictionary[key].Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            return obj;
        }
        else
        {
            // Nếu ngăn trống (do gọi quá nhiều cùng lúc), bắt buộc tạo mới
            GameObject newObj = Instantiate(prefab, position, rotation);
            newObj.name = prefab.name; // Bắt buộc: Giữ nguyên tên để khi cất vào không bị nhầm
            return newObj;
        }
    }

    // Hàm cất vật thể vào lại tủ (Thay thế hoàn toàn cho Destroy)
    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false); // Giấu đi
        obj.transform.SetParent(transform); // Gỡ nó ra khỏi Player/Quái, gom gọn vào Manager

        string key = obj.name;

        // Đề phòng trường hợp lỗi chưa có ngăn tủ
        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary[key] = new Queue<GameObject>();
        }

        poolDictionary[key].Enqueue(obj); // Nhét lại vào tủ
    }
}