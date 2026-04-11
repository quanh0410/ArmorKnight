using UnityEngine;
using Unity.Cinemachine; // Hoặc 'using Cinemachine;' nếu bạn dùng Unity bản cũ hơn 2023

public class CinemachineShake : MonoBehaviour
{
    public static CinemachineShake Instance { get; private set; }
    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        Instance = this;
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    // Hàm gọi rung (force là độ mạnh)
    public void ShakeCamera(float force)
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulseWithForce(force);
        }
    }
}