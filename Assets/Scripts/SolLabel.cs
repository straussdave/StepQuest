using TMPro;
using UnityEngine;

public class SolLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text solText;

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        solText.text = $"Sol {DateUtil.GetCurrentSol()}";
    }
}