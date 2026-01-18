using TMPro;
using UnityEngine;

public class QuestProgressBar : MonoBehaviour
{
    [SerializeField] RectTransform fillRect; // Fill image rect, Type=Sliced
    [SerializeField] RectTransform backgroundRect;
    [SerializeField] TextMeshProUGUI progressText;

    void Start()
    {
        var qm = QuestManager.Instance;
        if (qm == null) 
        { 
            enabled = false; 
            return; 
        }

        var target = (qm.CurrentQuest != null) ? qm.CurrentQuest.Steps : 0;
        UpdateUI(qm.CurrentSteps, target);
        qm.OnProgressChanged += UpdateUI;
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnProgressChanged -= UpdateUI;
        }
    }

    void UpdateUI(int current, int target)
    {
        float t = (target > 0) ? ((float)current / target) : 0f;
        t = Mathf.Clamp01(t);

        if (fillRect && backgroundRect)
        {
            float fullWidth = backgroundRect.rect.width;
            fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullWidth * t);
        }

        if (progressText)
        {
            progressText.text = $"{current} / {target}";
        }
    }
}
