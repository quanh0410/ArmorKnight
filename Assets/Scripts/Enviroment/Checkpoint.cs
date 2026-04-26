using UnityEngine;
using UnityEngine.SceneManagement;

public class Checkpoint : MonoBehaviour
{
    public string benchID; // GUID riêng biệt của Ghế
    private bool isPlayerNearby = false;

    private void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.S))
        {
            Rest();
        }
    }

    private void Rest()
    {
        // Lấy script PlayerController
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null && !pc.isResting && !pc.isInputLocked)
        {
            // Ra lệnh tự động đi bộ và ngồi
            pc.StartCoroutine(pc.WalkToBenchAndRest(transform, benchID));

            // Ẩn UI tương tác "[S] Nghỉ ngơi" đi
            if (InteractionUI.instance != null) InteractionUI.instance.Hide();
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player")) { isPlayerNearby = true; InteractionUI.instance.Show(transform, "[S] Nghỉ ngơi"); }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Player")) { isPlayerNearby = false; InteractionUI.instance.Hide(); }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Tự động tạo GUID cho Ghế mà không cần SpawnPoint
        if (string.IsNullOrEmpty(benchID) && !UnityEditor.EditorUtility.IsPersistent(this))
        {
            benchID = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}