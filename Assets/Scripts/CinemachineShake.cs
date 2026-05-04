using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CinemachineShake : MonoBehaviour
{
    public static CinemachineShake Instance { get; private set; }
    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        Instance = this;
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    // 1. Hàm rung 1 lần (Dành cho đòn đánh, vụ nổ)
    public void ShakeCamera(float force)
    {
        if (impulseSource != null)
        {
            // Tạo một vector hướng ngẫu nhiên cho cả trục X và Y
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized;

            // Dùng GenerateImpulse thay vì GenerateImpulseWithForce để truyền cả hướng và lực
            impulseSource.GenerateImpulse(randomDirection * force);
        }
    }

    // 2. Hàm rung liên tục (Dành cho Động đất)
    public void ShakeCameraContinuous(float force, float duration)
    {
        StartCoroutine(ContinuousShakeRoutine(force, duration));
    }

    private IEnumerator ContinuousShakeRoutine(float force, float duration)
    {
        float elapsed = 0f;
        float shakeInterval = 0.1f;

        while (elapsed < duration)
        {
            if (impulseSource != null)
            {
                // Liên tục tạo ra các hướng ngẫu nhiên mới cho mỗi nhịp rung nhỏ
                Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized;
                impulseSource.GenerateImpulse(randomDirection * force);
            }

            yield return new WaitForSeconds(shakeInterval);
            elapsed += shakeInterval;
        }
    }
}