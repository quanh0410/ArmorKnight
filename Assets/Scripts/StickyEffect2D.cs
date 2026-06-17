using UnityEngine;

public class StickyEffect2D : MonoBehaviour
{
    [Header("Offset Settings")]
    public Vector2 positionOffset = Vector2.zero;

    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), Mathf.Abs(transform.localScale.z));
    }

    public void SetTarget(Transform targetParent)
    {
        transform.SetParent(targetParent, false);

        transform.localPosition = positionOffset;

        transform.localScale = initialScale;
    }
}
