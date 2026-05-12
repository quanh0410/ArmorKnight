using UnityEngine;
using TMPro;
using System.Collections;
using System; // --- BẮT BUỘC CÓ: Để sử dụng Action (Callback) ---

public class DialogUIManager : MonoBehaviour
{
    public static DialogUIManager instance;

    [Header("UI Elements")]
    public GameObject dialogPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogText;

    [Header("Settings")]
    public float typingSpeed = 0.03f;

    private DialogData.DialogLine[] currentLines;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    public bool isDialogActive { get; private set; }

    // --- MỚI: Biến lưu trữ hành động cần làm sau khi hội thoại kết thúc ---
    private Action onDialogFinished;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        dialogPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isDialogActive) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            DisplayNextLine();
        }
    }

    // Cập nhật hàm StartDialog:
    public void StartDialog(DialogData.DialogLine[] lines, Action onComplete = null)
    {
        // Kiểm tra an toàn: Nếu không có câu thoại nào thì bỏ qua
        if (lines == null || lines.Length == 0) return;

        currentLines = lines;
        currentLineIndex = 0;
        isDialogActive = true;
        dialogPanel.SetActive(true);

        onDialogFinished = onComplete;

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.isInputLocked = true;

            // ==========================================
            // THÊM MỚI: ĐẠP PHANH GẤP KHI VÀO HỘI THOẠI
            // ==========================================
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Ép vận tốc trục X về 0 (đứng im), nhưng vẫn giữ nguyên trục Y (để rơi tự do nếu đang ở trên không)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

        }
            DisplayNextLine();
    }

    public void DisplayNextLine()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            // Thay currentDialog.lines bằng currentLines
            dialogText.text = currentLines[currentLineIndex].sentence;
            isTyping = false;
            currentLineIndex++;
            return;
        }

        // Thay currentDialog.lines.Length bằng currentLines.Length
        if (currentLineIndex < currentLines.Length)
        {
            typingCoroutine = StartCoroutine(TypeSentence(currentLines[currentLineIndex]));
        }
        else
        {
            EndDialog();
        }
    }

    private IEnumerator TypeSentence(DialogData.DialogLine line)
    {
        isTyping = true;
        nameText.text = line.speakerName;
        dialogText.text = "";

        foreach (char letter in line.sentence.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
        isTyping = false;
        currentLineIndex++;
    }

    private void EndDialog()
    {
        isDialogActive = false;
        dialogPanel.SetActive(false);

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) player.isInputLocked = false;

        // --- MỚI: Hội thoại kết thúc -> Kích hoạt lời nhắn và xóa nó đi ---
        onDialogFinished?.Invoke();
        onDialogFinished = null;
    }
}