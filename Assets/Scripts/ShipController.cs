using System;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [System.Serializable]
    public class ShipPart
    {
        public string partId;        // Matches your quest ID or internal part ID
        public GameObject prefab;    // The prefab from your asset pack
        [HideInInspector] public GameObject instance;  // runtime instance
    }

    public ShipPart[] parts;

    void Start()
    {
        // Instantiate all parts but keep them disabled
        foreach (var p in parts)
        {
            if (p.prefab == null)
            {
                Debug.LogWarning($"Ship part '{p.partId}' has no prefab assigned.");
                continue;
            }

            // instantiate
            p.instance = Instantiate(p.prefab, transform);
            p.instance.SetActive(false);
        }

        // Load already unlocked parts
        ReloadParts();

        QuestManager.Instance.OnPartUnlocked += OnPartUnlocked;
    }

    void OnDestroy()
    {
        QuestManager.Instance.OnPartUnlocked -= OnPartUnlocked;
    }

    public void ReloadParts()
    {
        foreach (var p in parts)
        {
            bool unlocked = QuestManager.Instance.IsPartUnlocked(p.partId);
            Console.WriteLine($"Part '{p.partId}' unlocked: {unlocked}");
            if (p.instance != null)
                p.instance.SetActive(unlocked);
        }
    }

    void OnPartUnlocked(string id)
    {
        foreach (var p in parts)
        {
            if (p.partId == id && p.instance != null)
            {
                p.instance.SetActive(true);
                // Optional: play reveal animation here
            }
        }
    }
}
