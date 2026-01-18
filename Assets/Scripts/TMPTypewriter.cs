using System.Collections;
using TMPro;
using UnityEngine;

public class TMPTypewriter : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float charsPerSecond = 45f;
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine routine;
    private string currentMessage = "";

    public bool IsTyping { get; private set; }

    public void Play(string message)
    {
        currentMessage = message;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(TypeRoutine(message));
    }

    public void Skip()
    {
        if (!IsTyping) return;

        // Show everything instantly
        text.text = currentMessage;
        text.ForceMeshUpdate();
        text.maxVisibleCharacters = text.textInfo.characterCount;

        if (routine != null) StopCoroutine(routine);
        routine = null;
        IsTyping = false;
    }

    private IEnumerator TypeRoutine(string message)
    {
        IsTyping = true;

        text.text = message;
        text.maxVisibleCharacters = 0;
        text.ForceMeshUpdate();

        int total = text.textInfo.characterCount;
        float delay = 1f / Mathf.Max(1f, charsPerSecond);

        for (int i = 0; i <= total; i++)
        {
            text.maxVisibleCharacters = i;

            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(delay);
            else
                yield return new WaitForSeconds(delay);
        }

        IsTyping = false;
        routine = null;
    }
}
