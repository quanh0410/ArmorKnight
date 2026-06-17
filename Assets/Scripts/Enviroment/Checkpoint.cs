using UnityEngine;
using UnityEngine.SceneManagement;

public class Checkpoint : MonoBehaviour
{
    public string benchID; 
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
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null && !pc.isResting && !pc.isInputLocked)
        {
            pc.StartCoroutine(pc.WalkToBenchAndRest(transform, benchID));

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
        if (string.IsNullOrEmpty(benchID) && !UnityEditor.EditorUtility.IsPersistent(this))
        {
            benchID = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}