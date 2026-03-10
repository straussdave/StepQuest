using UnityEngine;

public class SettingsController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;

    void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        panelRoot.SetActive(false);
    }

    public void OpenSettings()
    {
        panelRoot.SetActive(true);
    }

    public void CloseSettings()
    {
        panelRoot.SetActive(false);
    }

    public void ToggleSettings()
    {
        panelRoot.SetActive(!panelRoot.activeSelf);
    }
}
