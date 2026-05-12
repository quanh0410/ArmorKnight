using UnityEngine;

[CreateAssetMenu(fileName = "New NPC Dialog", menuName = "Dialog System/NPC Dialog Data")]
public class DialogData : ScriptableObject
{
    [System.Serializable]
    public struct DialogLine
    {
        public string speakerName;
        [TextArea(3, 5)]
        public string sentence;
    }

    [Header("--- HỘI THOẠI LẦN ĐẦU (Nhiệm vụ, tặng đồ) ---")]
    public DialogLine[] firstTimeLines;

    [Header("--- HỘI THOẠI MẶC ĐỊNH (Từ lần thứ 2) ---")]
    public DialogLine[] defaultLines;
}