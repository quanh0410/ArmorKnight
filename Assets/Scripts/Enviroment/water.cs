using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))] 
public class water : MonoBehaviour
{
    [Header("--- CÀI ??T N??C ---")]
    [Tooltip("T?c ?? ?i b? c?a nhân v?t khi ? d??i n??c")]
    [SerializeField] private float waterMoveSpeed = 2f;

    private float originalSpeed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null)
        {
            originalSpeed = player.moveSpeed;

            player.moveSpeed = waterMoveSpeed;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null)
        {
            player.moveSpeed = originalSpeed;
        }
    }
}