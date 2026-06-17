using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D), typeof(Animator))]
public class Trap3 : MonoBehaviour
{
    [Header("Trap Settings")]
    public int damage = 1;
    public float loopDelay = 2f;

    [Header("--- THIẾT LẬP ÂM THANH ---")]
    [Tooltip("Khoảng cách tối đa (mét) bắt đầu nghe thấy tiếng bẫy hoạt động")]
    public float maxAudibleDistance = 12f;

    private AudioSource localAudioSource;
    private AudioClip openClip;
    private AudioClip closedClip;
    private Transform playerTransform;

    private SpriteRenderer spriteRenderer;
    private PolygonCollider2D polyCollider;
    private Animator animator;
    private Sprite lastSprite;

    private static readonly int ActTrigger = Animator.StringToHash("act");

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        polyCollider = GetComponent<PolygonCollider2D>();
        animator = GetComponent<Animator>();

        polyCollider.isTrigger = true;

        localAudioSource = gameObject.AddComponent<AudioSource>();

        if (AudioManager.instance != null)
        {
            localAudioSource.outputAudioMixerGroup = AudioManager.instance.sfxSource.outputAudioMixerGroup;

            var openSound = System.Array.Find(AudioManager.instance.sfxSounds, x => x.name == "Trap3Open");
            if (openSound != null) openClip = openSound.clip;

            var closedSound = System.Array.Find(AudioManager.instance.sfxSounds, x => x.name == "Trap3Closed");
            if (closedSound != null) closedClip = closedSound.clip;
        }

        InvokeRepeating(nameof(PlayAnimation), 0f, loopDelay);
        UpdateCollider();
    }

    void Update()
    {
        if (spriteRenderer.sprite != lastSprite)
        {
            UpdateCollider();
        }
    }

    void UpdateCollider()
    {
        lastSprite = spriteRenderer.sprite;
        polyCollider.pathCount = spriteRenderer.sprite.GetPhysicsShapeCount();
        List<Vector2> path = new List<Vector2>();

        for (int i = 0; i < polyCollider.pathCount; i++)
        {
            path.Clear();
            spriteRenderer.sprite.GetPhysicsShape(i, path);
            polyCollider.SetPath(i, path);
        }
    }

    void PlayAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(ActTrigger);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeTrapDamage(damage);
            }
        }
    }

    public void PlaySFXOpen()
    {
        PlaySpatialTrapSound(openClip);
    }

    public void PlaySFXClosed()
    {
        PlaySpatialTrapSound(closedClip);
    }

    private void PlaySpatialTrapSound(AudioClip clip)
    {
        if (clip == null || localAudioSource == null) return;

        if (playerTransform == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null) playerTransform = pc.transform;
        }

        if (playerTransform == null) return;

        // Tính khoảng cách hình học 2D thuần túy (bỏ qua trục Z)
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        // Nếu người chơi đứng ngoài tầm nghe -> Không phát nhạc, ngắt lệnh ngay để tối ưu RAM/CPU
        if (distance > maxAudibleDistance) return;

        // Công thức tính Fade: Gần bằng 0 mét -> volume = 1. Gần bằng maxAudibleDistance -> volume = 0
        float volumeFactor = 1f - (distance / maxAudibleDistance);

        // Bình phương volumeFactor để mô phỏng đường cong âm thanh logarit (đúng với cơ chế sinh học tai người)
        volumeFactor = volumeFactor * volumeFactor;

        // Phát âm thanh ra loa nội bộ với âm lượng đã được làm mịn
        localAudioSource.PlayOneShot(clip, volumeFactor);
    }
}