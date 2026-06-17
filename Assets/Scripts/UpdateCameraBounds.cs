using UnityEngine;
using Unity.Cinemachine;

public class UpdateCameraBounds : MonoBehaviour
{
    private void Start()
    {
        Collider2D myBounds = GetComponent<Collider2D>();

        CinemachineConfiner2D confiner = FindObjectOfType<CinemachineConfiner2D>();

        if (confiner != null && myBounds != null)
        {
            confiner.BoundingShape2D = myBounds;
            confiner.InvalidateBoundingShapeCache();

            CinemachineCamera cineCam = confiner.GetComponent<CinemachineCamera>();
            if (cineCam != null)
            {
                cineCam.PreviousStateIsValid = false;
            }
        }
    }
}