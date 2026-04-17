using UnityEngine;
using UnityEngine.UI; // สำคัญ: ต้องใช้เพื่อจัดการ Image UI

public class TrashNodeUI : MonoBehaviour
{
    [Header("UI Fill Bars")]
    [Tooltip("หลอดแสดงค่าขยะจริง (Actual)")]
    public Image Bar_Actual;

    [Tooltip("หลอดแสดงค่าที่ AI มองเห็น (Perceived/Delay)")]
    public Image Bar_AI;

    [Header("Settings")]
    [Range(0, 1)] public float testFillActual = 1f;
    [Range(0, 1)] public float testFillAI = 1f;

    /// <summary>
    /// ฟังก์ชันสำหรับอัปเดตค่าหลอดขยะจริง
    /// </summary>
    /// <param name="value">ค่าระหว่าง 0.0 ถึง 1.0</param>
    public void UpdateActualBar(float value)
    {
        if (Bar_Actual != null)
            Bar_Actual.fillAmount = Mathf.Clamp01(value);
    }

    /// <summary>
    /// ฟังก์ชันสำหรับอัปเดตค่าหลอดที่ AI มองเห็น (ใช้สำหรับทำ Information Delay)
    /// </summary>
    /// <param name="value">ค่าระหว่าง 0.0 ถึง 1.0</param>
    public void UpdateAIBar(float value)
    {
        if (Bar_AI != null)
            Bar_AI.fillAmount = Mathf.Clamp01(value);
    }

    // ส่วนนี้เอาไว้ให้คุณเลื่อนทดสอบในหน้า Inspector ได้ทันที
    private void OnValidate()
    {
        UpdateActualBar(testFillActual);
        UpdateAIBar(testFillAI);
    }
}