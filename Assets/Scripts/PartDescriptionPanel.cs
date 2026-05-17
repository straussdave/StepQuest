using TMPro;
using UnityEngine;

public class PartDescriptionPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text partNameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Behavior")]
    [SerializeField] private bool hideOnStart = true;

    private void Awake()
    {
        if (hideOnStart)
        {
            gameObject.SetActive(false);
        }
    }

    public void Show(string partName, string description)
    {
        gameObject.SetActive(true);

        if (partNameText != null)
        {
            partNameText.text = partName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}