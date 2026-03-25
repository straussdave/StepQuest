using System.Collections;
using UnityEngine;

public class TabSlider : MonoBehaviour
{
    public enum Tab
    {
        Minigames = 0,
        Home = 1,
        Collection = 2
    }

    [Header("References")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private RectTransform homePanel;
    [SerializeField] private RectTransform collectionPanel;
    [SerializeField] private RectTransform minigamesPanel;

    [Header("Collection Logic")]
    [SerializeField] private ShipController shipController;
    [SerializeField] private ShipRotateInput shipRotateInput;

    [Header("Navbar Indicator")]
    [SerializeField] private RectTransform activeIndicator;
    [SerializeField] private RectTransform minigamesButton;
    [SerializeField] private RectTransform homeButton;
    [SerializeField] private RectTransform collectionButton;
    [SerializeField] private float indicatorWidthPadding = 24f;

    [Header("Navbar Icons")]
    [SerializeField] private RectTransform minigamesIcon;
    [SerializeField] private RectTransform homeIcon;
    [SerializeField] private RectTransform collectionIcon;
    [SerializeField] private float selectedIconScale = 1.12f;
    [SerializeField] private float unselectedIconScale = 1f;

    [Header("Animation")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Tab currentTab = Tab.Home;
    private bool isAnimating;

    void Start()
    {
        Canvas.ForceUpdateCanvases();
        ApplyInstantLayout(currentTab);
        ApplyTabSideEffects(currentTab);
        SnapIndicatorTo(currentTab);
        ApplyIconScalesInstant(currentTab);
    }

    public void ShowHome() => SwitchTo(Tab.Home);
    public void ShowCollection() => SwitchTo(Tab.Collection);
    public void ShowMinigames() => SwitchTo(Tab.Minigames);

    public void SwitchTo(Tab newTab)
    {
        if (isAnimating || newTab == currentTab)
            return;

        StopAllCoroutines();
        StartCoroutine(AnimateToTab(newTab));
    }

    private IEnumerator AnimateToTab(Tab newTab)
    {
        isAnimating = true;

        OnEnteringTab(newTab);

        float width = contentRoot.rect.width;

        Vector3 minigamesIconStart = minigamesIcon != null ? minigamesIcon.localScale : Vector3.one;
        Vector3 homeIconStart = homeIcon != null ? homeIcon.localScale : Vector3.one;
        Vector3 collectionIconStart = collectionIcon != null ? collectionIcon.localScale : Vector3.one;

        Vector3 minigamesIconTarget = Vector3.one * (newTab == Tab.Minigames ? selectedIconScale : unselectedIconScale);
        Vector3 homeIconTarget = Vector3.one * (newTab == Tab.Home ? selectedIconScale : unselectedIconScale);
        Vector3 collectionIconTarget = Vector3.one * (newTab == Tab.Collection ? selectedIconScale : unselectedIconScale);

        Vector2 homeStart = homePanel.anchoredPosition;
        Vector2 collectionStart = collectionPanel.anchoredPosition;
        Vector2 minigamesStart = minigamesPanel.anchoredPosition;

        Vector2 homeTarget = GetTargetPosition(Tab.Home, newTab, width);
        Vector2 collectionTarget = GetTargetPosition(Tab.Collection, newTab, width);
        Vector2 minigamesTarget = GetTargetPosition(Tab.Minigames, newTab, width);

        Vector2 indicatorStart = activeIndicator != null ? activeIndicator.anchoredPosition : Vector2.zero;
        Vector2 indicatorTarget = GetIndicatorTargetPosition(newTab);

        float startIndicatorWidth = activeIndicator != null ? activeIndicator.sizeDelta.x : 0f;
        float targetIndicatorWidth = GetIndicatorTargetWidth(newTab);

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float eased = curve.Evaluate(t);

            homePanel.anchoredPosition = Vector2.LerpUnclamped(homeStart, homeTarget, eased);
            collectionPanel.anchoredPosition = Vector2.LerpUnclamped(collectionStart, collectionTarget, eased);
            minigamesPanel.anchoredPosition = Vector2.LerpUnclamped(minigamesStart, minigamesTarget, eased);

            if (minigamesIcon != null)
                minigamesIcon.localScale = Vector3.LerpUnclamped(minigamesIconStart, minigamesIconTarget, eased);

            if (homeIcon != null)
                homeIcon.localScale = Vector3.LerpUnclamped(homeIconStart, homeIconTarget, eased);

            if (collectionIcon != null)
                collectionIcon.localScale = Vector3.LerpUnclamped(collectionIconStart, collectionIconTarget, eased);

            if (activeIndicator != null)
            {
                activeIndicator.anchoredPosition = Vector2.LerpUnclamped(indicatorStart, indicatorTarget, eased);

                Vector2 size = activeIndicator.sizeDelta;
                size.x = Mathf.LerpUnclamped(startIndicatorWidth, targetIndicatorWidth, eased);
                activeIndicator.sizeDelta = size;
            }

            yield return null;
        }

        homePanel.anchoredPosition = homeTarget;
        collectionPanel.anchoredPosition = collectionTarget;
        minigamesPanel.anchoredPosition = minigamesTarget;

        if (minigamesIcon != null) minigamesIcon.localScale = minigamesIconTarget;
        if (homeIcon != null) homeIcon.localScale = homeIconTarget;
        if (collectionIcon != null) collectionIcon.localScale = collectionIconTarget;

        if (activeIndicator != null)
        {
            activeIndicator.anchoredPosition = indicatorTarget;
            Vector2 size = activeIndicator.sizeDelta;
            size.x = targetIndicatorWidth;
            activeIndicator.sizeDelta = size;
        }

        currentTab = newTab;
        ApplyTabSideEffects(currentTab);
        isAnimating = false;
    }

    private void ApplyInstantLayout(Tab activeTab)
    {
        float width = contentRoot.rect.width;

        homePanel.anchoredPosition = GetTargetPosition(Tab.Home, activeTab, width);
        collectionPanel.anchoredPosition = GetTargetPosition(Tab.Collection, activeTab, width);
        minigamesPanel.anchoredPosition = GetTargetPosition(Tab.Minigames, activeTab, width);
    }

    private Vector2 GetTargetPosition(Tab panelTab, Tab activeTab, float width)
    {
        int offset = (int)panelTab - (int)activeTab;
        return new Vector2(offset * width, 0f);
    }

    private void ApplyTabSideEffects(Tab activeTab)
    {
        bool collectionActive = activeTab == Tab.Collection;

        if (shipRotateInput != null)
            shipRotateInput.enabled = collectionActive;
    }

    private void OnEnteringTab(Tab newTab)
    {
        if (newTab == Tab.Collection && shipController != null)
        {
            shipController.ReloadParts();
            shipController.PlayPendingUnlockAnimationIfNeeded();
        }
    }

    private RectTransform GetButtonRect(Tab tab)
    {
        switch (tab)
        {
            case Tab.Minigames: return minigamesButton;
            case Tab.Home: return homeButton;
            case Tab.Collection: return collectionButton;
            default: return homeButton;
        }
    }

    private Vector2 GetIndicatorTargetPosition(Tab tab)
    {
        RectTransform targetButton = GetButtonRect(tab);
        if (targetButton == null || activeIndicator == null)
            return Vector2.zero;

        RectTransform indicatorParent = activeIndicator.parent as RectTransform;
        if (indicatorParent == null)
            return activeIndicator.anchoredPosition;

        Vector3 worldCenter = targetButton.TransformPoint(targetButton.rect.center);
        Vector3 localCenter = indicatorParent.InverseTransformPoint(worldCenter);

        return new Vector2(localCenter.x, activeIndicator.anchoredPosition.y);
    }

    private float GetIndicatorTargetWidth(Tab tab)
    {
        RectTransform targetButton = GetButtonRect(tab);
        if (targetButton == null)
            return 0f;

        return Mathf.Max(0f, targetButton.rect.width - indicatorWidthPadding);
    }

    private void SnapIndicatorTo(Tab tab)
    {
        if (activeIndicator == null) return;

        activeIndicator.anchoredPosition = GetIndicatorTargetPosition(tab);

        Vector2 size = activeIndicator.sizeDelta;
        size.x = GetIndicatorTargetWidth(tab);
        activeIndicator.sizeDelta = size;
    }

    private RectTransform GetIconRect(Tab tab)
    {
        switch (tab)
        {
            case Tab.Minigames: return minigamesIcon;
            case Tab.Home: return homeIcon;
            case Tab.Collection: return collectionIcon;
            default: return homeIcon;
        }
    }

    private void ApplyIconScalesInstant(Tab activeTab)
    {
        SetIconScale(minigamesIcon, activeTab == Tab.Minigames ? selectedIconScale : unselectedIconScale);
        SetIconScale(homeIcon, activeTab == Tab.Home ? selectedIconScale : unselectedIconScale);
        SetIconScale(collectionIcon, activeTab == Tab.Collection ? selectedIconScale : unselectedIconScale);
    }

    private void SetIconScale(RectTransform icon, float scale)
    {
        if (icon == null) return;
        icon.localScale = new Vector3(scale, scale, 1f);
    }
}