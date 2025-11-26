using UnityEngine;
using UnityEngine.UI;

public class UIFadeIn : MonoBehaviour
{
    [SerializeField] float duration = 0.6f;
    CanvasGroup group;

    void Awake()
    {
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
    }

    void OnEnable()
    {
        StartCoroutine(Fade());
    }

    System.Collections.IEnumerator Fade()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }
        group.alpha = 1f;
    }
}
